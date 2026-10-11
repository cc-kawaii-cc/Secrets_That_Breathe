using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SecretsThatBreathe.MangoTree.Editor;

public static class VerifyMangoTree
{
    public static void Run()
    {
        try
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            MangoTreeAssetBuilder.Build();
            string root = MangoTreeAssetBuilder.Root;
            var log = new StringBuilder("Unity " + Application.unityVersion + " / URP 17.3.0\n");
            foreach (string name in new[] { "MangoTree", "MangoTree_NoFruit" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "/Prefabs/" + name + ".prefab");
                if (!prefab) throw new Exception("Missing prefab " + name);
                var lods = prefab.GetComponent<LODGroup>().GetLODs();
                if (lods.Length != 3) throw new Exception("Expected 3 LODs");
                if (!prefab.GetComponent<CapsuleCollider>()) throw new Exception("Missing collider");
                Bounds bounds = default;
                for (int l = 0; l < 3; l++)
                {
                    long triangles = 0;
                    if (lods[l].renderers.Length != 3) throw new Exception("Expected bark, leaves, fruit");
                    foreach (var r in lods[l].renderers)
                    {
                        if (!r || !r.sharedMaterial || !r.sharedMaterial.shader) throw new Exception("Missing renderer/material/shader");
                        string expectedShader = r.name.StartsWith("Leaves")
                            ? "Secrets That Breathe/Mango Tree New Wind Lit"
                            : "Secrets That Breathe/Mango Wind Lit";
                        if (r.sharedMaterial.shader.name != expectedShader) throw new Exception("Wrong shader on " + r.name);
                        if (!r.sharedMaterial.GetTexture("_BaseMap")) throw new Exception("Missing texture");
                        if (ShaderUtil.ShaderHasError(r.sharedMaterial.shader)) throw new Exception("Shader compile error");
                        if (r.name.StartsWith("Leaves") && r.sharedMaterial.GetFloat("_Cull") != 0) throw new Exception("Leaves must be double sided");
                        if (r.name.StartsWith("Fruit") && r.enabled != (name == "MangoTree")) throw new Exception("Incorrect fruit variant");
                        var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                        for (int s = 0; s < mesh.subMeshCount; s++) triangles += (long)mesh.GetIndexCount(s) / 3;
                        if (l == 0) { if (bounds.size == Vector3.zero) bounds = r.bounds; else bounds.Encapsulate(r.bounds); }
                    }
                    log.AppendLine(name + " LOD" + l + ": " + triangles + " triangles / transition " + lods[l].screenRelativeTransitionHeight);
                }
                if (bounds.size.y < 3.5f || bounds.size.y > 5f || bounds.size.x < 4f || bounds.size.x > 7f) throw new Exception("Unexpected metre scale / orientation: " + bounds);
                log.AppendLine("Bounds: " + bounds + "; collision, materials, textures, LOD, fruit visibility PASS");
            }
            // Standalone demonstration scene; it uses the host project's URP pipeline.
            Directory.CreateDirectory(root + "/Demo");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var tree = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(root + "/Prefabs/MangoTree.prefab"));
            tree.GetComponent<LODGroup>().ForceLOD(0);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Preview Ground"; ground.transform.position = new Vector3(0, -.07f, 0); ground.transform.localScale = Vector3.one * 15;
            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.SetColor("_BaseColor", new Color(.29f,.32f,.28f)); groundMat.SetFloat("_Smoothness", .05f);
            AssetDatabase.CreateAsset(groundMat, root + "/Demo/PreviewGround.mat"); ground.GetComponent<Renderer>().sharedMaterial = groundMat;
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(42, -38, 0); light.intensity = 1.5f; light.shadows = LightShadows.Soft;
            var fill = new GameObject("Fill").AddComponent<Light>(); fill.type = LightType.Directional;
            fill.transform.rotation = Quaternion.Euler(35, 145, 0); fill.intensity = .5f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.48f,.53f,.58f);
            RenderSettings.ambientEquatorColor = new Color(.3f,.34f,.27f);
            RenderSettings.ambientGroundColor = new Color(.19f,.17f,.14f);
            var camera = new GameObject("Preview Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(7, 5.4f, -10);
            camera.transform.LookAt(new Vector3(0, 2.04f, 0));
            camera.orthographic = true; camera.orthographicSize = 3.05f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.39f,.43f,.42f);
            camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            var volume = new GameObject("Preview Tonemapping").AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.ACES);
            AssetDatabase.CreateAsset(profile, root + "/Demo/PreviewTonemapping.asset");
            AssetDatabase.AddObjectToAsset(tonemapping, profile);
            volume.sharedProfile = profile;
            tree.GetComponent<LODGroup>().ForceLOD(-1);
            EditorSceneManager.SaveScene(scene, root + "/Demo/MangoTree_Preview.unity");
            // These settings are created only in the disposable validation project.
            var data = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(data, "Assets/VerificationRenderer.asset");
            var pipeline = UniversalRenderPipelineAsset.Create(data);
            pipeline.shadowDistance = 35; pipeline.msaaSampleCount = 4;
            pipeline.mainLightShadowmapResolution = 2048;
            var pipelineSettings = new SerializedObject(pipeline);
            pipelineSettings.FindProperty("m_SoftShadowsSupported").boolValue = true;
            pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
            pipeline.shadowCascadeCount = 4;
            AssetDatabase.CreateAsset(pipeline, "Assets/VerificationPipeline.asset");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            tree.GetComponent<LODGroup>().ForceLOD(0);
            var rt = new RenderTexture(1400, 1400, 24, RenderTextureFormat.ARGB32);
            rt.Create();
            camera.targetTexture = rt;
            camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var img = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            img.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); img.Apply();
            string reportDir = Environment.GetEnvironmentVariable("MANGO_REPORT_DIR");
            File.WriteAllBytes(Path.Combine(reportDir, "MangoTree_Unity_URP.png"), img.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; rt.Release();
            UnityEngine.Object.DestroyImmediate(img);
            log.AppendLine("PASS: prefab references, 3 LOD levels, metre scale, Y-up, 2-sided leaves, URP shaders, trunk collider.");
            File.WriteAllText(Path.Combine(reportDir, "Unity_validation.txt"), log.ToString());
            AssetDatabase.ExportPackage(root, Path.Combine(reportDir, "MangoTree_URP.unitypackage"), ExportPackageOptions.Recurse);
            Debug.Log("MANGO_VALIDATION_PASS\n" + log);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
