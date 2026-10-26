using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Explicit, one-shot installation. No automatic scene mutation on import.
[InitializeOnLoad]
public static class PorscheLowBeamCookieSetup
{
    const string ScenePath = "Assets/Nemo_Upgrade/MainScene_2_Evening.unity";
    const string CookiePath = "Assets/SecretsThatBreathe/Lighting/Cookies/Porsche_LowBeam_LHT.png";
    const string Work = "Tools/PorscheHeadlights/LowBeamCookie";
    const string Request = Work + "/install.request";
    const int Size = 1024;
    static bool busy;
    static bool softenOnly;

    static PorscheLowBeamCookieSetup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        busy = true;
        softenOnly = File.ReadAllText(Request).Trim() == "soften";
        File.Move(Request, Work + "/request-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + ".consumed");
        try { Install(); }
        catch (Exception e) { File.WriteAllText(Work + "/result.json", JsonUtility.ToJson(new Report { error = e.ToString() }, true)); Debug.LogException(e); }
        finally { busy = false; softenOnly = false; }
    }

    [Serializable] public class Report
    {
        public bool success;
        public string error, cookie, scene, backup, cookieGuid;
        public int lights, resolution;
        public float outerAngle, downwardAim, peak, aboveCutoffMaximum;
        public bool sceneWasDirty;
        public string[] lightNames;
    }

    [MenuItem("Tools/Secrets That Breathe/Porsche/Install Low Beam Cookie (Evening Scene)")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open MainScene_2_Evening first.");
        GameObject car = scene.GetRootGameObjects().Single(o => o.name == "PorscheV.2");
        Light[] lights = car.GetComponentsInChildren<Light>(true)
            .Where(l => l.type == LightType.Spot && (l.name == "Spot Light" || l.name == "Spot Light (1)")).ToArray();
        if (lights.Length != 2) throw new InvalidOperationException("Expected exactly the two existing Porsche headlight spots.");
        Directory.CreateDirectory(Work);
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        string backup = Work + "/before-" + stamp + ".unity";
        bool wasDirty = scene.isDirty;
        // Copy the LIVE scene too, so unsaved user changes are recoverable.
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Live scene backup failed.");
        File.Copy(ScenePath, Work + "/on-disk-before-" + stamp + ".unity");
        if (File.Exists(CookiePath)) File.Copy(CookiePath, Work + "/cookie-before-" + stamp + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(CookiePath));
        Texture2D generated = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
        Color32[] pixels = new Color32[Size * Size];
        float peak = 0, above = 0;
        for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
        {
            float u = (x + .5f) / Size * 2 - 1, v = (y + .5f) / Size * 2 - 1;
            // Driver's left is texture left in URP's spot-cookie projection.
            // A bounded 15-degree upsweep toward the left shoulder; flat oncoming side.
            float cutoff = Mathf.Clamp((-u - .015f) * .267949f, 0, .026f);
            // About seven texels of feathering, entirely below the cutoff.
            float edge = Mathf.SmoothStep(0, 1, (cutoff - v) / .014f);
            float hot = Mathf.Exp(-.5f * (Sq((u + .025f) / .17f) + Sq((v + .045f) / .049f)));
            float spread = .38f * Mathf.Exp(-.5f * (Sq(u / .47f) + Sq((v + .17f) / .18f)));
            float foreground = .10f * Mathf.Exp(-.5f * (Sq(u / .65f) + Sq((v + .43f) / .25f)));
            float border = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.82f, .98f, Mathf.Max(Mathf.Abs(u), Mathf.Abs(v))));
            float value = Mathf.Clamp01((hot + spread + foreground) / 1.30f * edge * border);
            peak = Mathf.Max(peak, value);
            if (v > cutoff + .004f) above = Mathf.Max(above, value);
            byte b = (byte)Mathf.RoundToInt(value * 255);
            pixels[y * Size + x] = new Color32(b, b, b, b);
        }
        generated.SetPixels32(pixels); generated.Apply();
        File.WriteAllBytes(CookiePath, generated.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(generated);
        AssetDatabase.ImportAsset(CookiePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(CookiePath);
        // URP samples RGB; alpha duplicates luminance for portability.
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = Size;
        importer.isReadable = false;
        importer.SaveAndReimport();
        Texture2D cookie = AssetDatabase.LoadAssetAtPath<Texture2D>(CookiePath);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Porsche low beam cutoff cookie");
        foreach (Light light in lights)
        {
            Undo.RecordObjects(new UnityEngine.Object[] { light, light.transform }, "Set low beam cookie and aim");
            light.cookie = cookie;
            if (!softenOnly)
            {
                light.spotAngle = 70;
                light.innerSpotAngle = 62;
                light.transform.localRotation = Quaternion.Euler(.8f, 0, 0);
            }
            // Keep the user's intensity, colour, range, shadow setting and positions.
            PrefabUtility.RecordPrefabInstancePropertyModifications(light);
            PrefabUtility.RecordPrefabInstancePropertyModifications(light.transform);
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the updated scene.");
        RenderRoad(scene, car.transform);
        RenderWall(lights, car.transform);
        // Temporary verification cameras are destroyed by this point.
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the scene after verification.");
        File.WriteAllText(Work + "/result.json", JsonUtility.ToJson(new Report {
            success = lights.All(l => l.cookie == cookie), cookie = CookiePath,
            scene = scene.path, backup = backup, cookieGuid = AssetDatabase.AssetPathToGUID(CookiePath),
            lights = lights.Length, resolution = Size, outerAngle = 70, downwardAim = .8f,
            peak = peak, aboveCutoffMaximum = above, sceneWasDirty = wasDirty,
            lightNames = lights.Select(l => l.name).ToArray()
        }, true));
        Selection.objects = lights.Cast<UnityEngine.Object>().ToArray();
        SceneView.RepaintAll();
        Debug.Log("Porsche low beam cookie installed on the two Evening scene headlights. " + CookiePath);
    }

    static float Sq(float n) { return n * n; }
    static void Capture(Camera camera, string path)
    {
        var rt = new RenderTexture(1400, 800, 24, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var texture = new Texture2D(1400, 800, TextureFormat.RGB24, false);
        try
        {
            rt.Create(); camera.targetTexture = rt;
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 1400, 800), 0, 0); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous;
            rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    static void RenderRoad(Scene scene, Transform car)
    {
        var go = new GameObject("CookieVerificationCamera") { hideFlags = HideFlags.HideAndDontSave };
        SceneManager.MoveGameObjectToScene(go, scene);
        try
        {
            var cam = go.AddComponent<Camera>(); cam.enabled = false;
            cam.transform.position = car.TransformPoint(new Vector3(0, 1.35f, 2.45f));
            cam.transform.rotation = car.rotation * Quaternion.Euler(7, 0, 0);
            cam.fieldOfView = 65; cam.nearClipPlane = .05f; cam.farClipPlane = 180;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            Capture(cam, Work + "/road-preview.png");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    static void RenderWall(Light[] sources, Transform car)
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        Material material = null;
        try
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(wall, preview);
            wall.transform.position = new Vector3(0, 0, 10);
            wall.transform.localScale = new Vector3(18, 10, .1f);
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", Color.white); material.SetFloat("_Smoothness", 0);
            wall.GetComponent<Renderer>().sharedMaterial = material;
            foreach (Light source in sources)
            {
                var go = new GameObject("Preview " + source.name); SceneManager.MoveGameObjectToScene(go, preview);
                var l = go.AddComponent<Light>(); EditorUtility.CopySerialized(source, l);
                go.transform.position = new Vector3(car.InverseTransformPoint(source.transform.position).x, 0, 0);
                go.transform.rotation = Quaternion.Euler(.8f, 0, 0);
                l.intensity = 120; l.lightmapBakeType = LightmapBakeType.Realtime; l.shadows = LightShadows.None;
                go.AddComponent<UniversalAdditionalLightData>();
            }
            var cameraObject = new GameObject("Wall preview camera"); SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.scene = preview;
            camera.transform.position = new Vector3(0, 0, -1);
            camera.orthographic = true; camera.orthographicSize = 4;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            Capture(camera, Work + "/wall-preview.png");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); if (material != null) UnityEngine.Object.DestroyImmediate(material); }
    }
}
