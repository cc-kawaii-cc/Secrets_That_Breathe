using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-use requests only. Never saves the user's open scene.
[InitializeOnLoad]
public static class PorscheTaillightTools
{
    const string Root = "Assets/Model Palm/Porsche V.2";
    const string Work = "Tools/PorscheTaillights";
    [Serializable] class Request { public string id; public string operation; }
    static PorscheTaillightTools() { EditorApplication.update += Poll; }
    static void Poll()
    {
        string path = Work + "/request.json";
        if (!File.Exists(path) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var req = JsonUtility.FromJson<Request>(File.ReadAllText(path));
        File.Move(path, Work + "/" + req.id + ".consumed");
        try
        {
            if (req.operation != "inspect" && req.operation != "apply") throw new Exception("Unsupported operation");
            if (req.operation == "apply") Apply();
            Capture(req.operation + "_rear", new Vector3(0, 1.25f, -5.8f), new Vector3(0,.73f,-1.5f), 2.3f);
            Capture(req.operation + "_angle", new Vector3(3, 1.8f, -5), new Vector3(0,.7f,-.5f), 4.8f);
            Capture(req.operation + "_close", new Vector3(0, .79f, -5), new Vector3(0,.765f,-2), 1.7f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/PorscheV.2.prefab");
            var mf = prefab.transform.Find(req.operation == "apply" ? "Taillights_Emission" : "Light").GetComponent<MeshFilter>();
            var mat = mf.GetComponent<Renderer>().sharedMaterial;
            File.WriteAllText(Work + "/result.json", "{\"success\":true,\"operation\":\"" + req.operation + "\",\"material\":\"" + mat.name + "\",\"emission\":" + mat.IsKeywordEnabled("_EMISSION").ToString().ToLower() + ",\"triangles\":" + mf.sharedMesh.triangles.Length/3 + "}");
        }
        catch (Exception ex) { File.WriteAllText(Work + "/result.json", JsonUtility.ToJson(new Error { error=ex.ToString() }, true)); Debug.LogException(ex); }
    }
    [Serializable] class Error { public string error; }
    static void Apply()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == Root + "/PorscheV.2.prefab" && stage.scene.isDirty) throw new Exception("Porsche prefab has unsaved edits");
        var matPath = Root + "/Material/Taillights_Emission.mat";
        AssetDatabase.ImportAsset(Root + "/Material/PorscheTaillights.shader", ImportAssetOptions.ForceSynchronousImport);
        var shader=Shader.Find("Secrets That Breathe/Porsche Taillights");
        if(!shader) throw new Exception("Taillight shader is missing");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        var clean=new Material(shader) {name="Taillights_Emission"};
        if (!mat) { mat=clean; AssetDatabase.CreateAsset(mat,matPath); }
        else { EditorUtility.CopySerialized(clean,mat); UnityEngine.Object.DestroyImmediate(clean); }
        if (!mat.shader) throw new Exception("Porsche taillight shader was not imported");
        mat.SetColor("_EmissionColor", new Color(5,.015f,.03f,1)); mat.SetFloat("_LowerStrength",.65f);
        mat.EnableKeyword("_EMISSION"); mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        EditorUtility.SetDirty(mat); AssetDatabase.SaveAssetIfDirty(mat); AssetDatabase.ImportAsset(matPath, ImportAssetOptions.ForceSynchronousImport);
        if (!File.Exists(Work + "/PorscheV.2_before.prefab")) File.Copy(Root + "/PorscheV.2.prefab", Work + "/PorscheV.2_before.prefab", false);
        var contents = PrefabUtility.LoadPrefabContents(Root + "/PorscheV.2.prefab");
        try {
            var original=AssetDatabase.LoadAssetAtPath<Material>(Root + "/Material/tripo_mat_c3595359.001.mat");
            contents.transform.Find("Light").GetComponent<Renderer>().sharedMaterial=original;
            var body=contents.transform.Find("Body").GetComponent<Renderer>(); var slots=body.sharedMaterials; slots[0]=original; body.sharedMaterials=slots;
            var mesh=BuildSmoothLights();
            var meshPath=Root + "/Meshes/Taillights_EmissionOverlay.asset"; var stored=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(stored) { stored.Clear(); stored.name=mesh.name; stored.vertices=mesh.vertices; stored.normals=mesh.normals; stored.colors=mesh.colors; stored.uv=mesh.uv; stored.triangles=mesh.triangles; stored.RecalculateBounds(); stored.UploadMeshData(false); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(stored); AssetDatabase.SaveAssetIfDirty(stored); } else { AssetDatabase.CreateAsset(mesh,meshPath); stored=mesh; }
            var child=contents.transform.Find("Taillights_Emission"); if(!child) { var go=new GameObject("Taillights_Emission"); child=go.transform; child.SetParent(contents.transform,false); go.AddComponent<MeshFilter>(); go.AddComponent<MeshRenderer>(); }
            child.GetComponent<MeshFilter>().sharedMesh=stored; var renderer=child.GetComponent<MeshRenderer>(); renderer.sharedMaterial=mat; renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            PrefabUtility.SaveAsPrefabAsset(contents, Root + "/PorscheV.2.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        if (!mat.IsKeywordEnabled("_EMISSION") || ShaderUtil.ShaderHasError(mat.shader)) throw new Exception("Emission shader validation failed after reimport");
    }
    // Analytic light guides: original triangulation and UVs do not shape these.
    static float RearCurve(float x) { float t=x/.75f; return -2.220f+.100f*t*t+.210f*Mathf.Pow(t,6); }
    static Mesh BuildSmoothLights()
    {
        var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var colors=new List<Color>(); var uv=new List<Vector2>(); var triangles=new List<int>();
        Action<float,float,float,float,int,bool> strip=(left,right,height,radius,steps,lower)=> {
            const int sides=8; int start=vertices.Count;
            for(int i=0;i<=steps;i++) {
                float t=(float)i/steps; float x=Mathf.Lerp(left,right,t);
                // Smooth plan curve only; every point on a guide has the same height.
                float z=RearCurve(x)-(lower?.010f:0);
                float dz=(RearCurve(x+.0001f)-RearCurve(x-.0001f))/.0002f;
                var outward=new Vector3(dz,0,-1).normalized;
                for(int j=0;j<sides;j++) {
                    float angle=j*Mathf.PI*2/sides;
                    vertices.Add(new Vector3(x,height,z)+Vector3.up*(Mathf.Cos(angle)*radius)+outward*(Mathf.Sin(angle)*.003f));
                    normals.Add((Vector3.up*Mathf.Cos(angle)+outward*Mathf.Sin(angle)).normalized);
                    colors.Add(new Color(lower?0:1,0,0,1)); uv.Add(new Vector2(t,(float)j/sides));
                }
            }
            for(int i=0;i<steps;i++) for(int j=0;j<sides;j++) {
                int a=start+i*sides+j, b=start+(i+1)*sides+j, c=start+(i+1)*sides+(j+1)%sides, d=start+i*sides+(j+1)%sides;
                triangles.AddRange(new[]{a,b,c,a,c,d});
            }
            // Close both ends with triangle fans; separate normals keep the cap tidy.
            for(int end=0;end<2;end++) {
                int ring=start+(end==0?0:steps*sides); int center=vertices.Count;
                float x=end==0?left:right;
                vertices.Add(new Vector3(x,height,RearCurve(x)-(lower?.010f:0))); normals.Add(end==0?Vector3.left:Vector3.right); colors.Add(new Color(lower?0:1,0,0,1)); uv.Add(new Vector2(end,.5f));
                for(int j=0;j<sides;j++) if(end==0) triangles.AddRange(new[]{center,ring+j,ring+(j+1)%sides}); else triangles.AddRange(new[]{center,ring+(j+1)%sides,ring+j});
            }
        };
        strip(-.744f,.744f,.783f,.0045f,96,false);
        strip(-.680f,-.565f,.754f,.0028f,12,true);
        strip(-.535f,-.425f,.754f,.0028f,12,true);
        strip(.425f,.535f,.754f,.0028f,12,true);
        strip(.565f,.680f,.754f,.0028f,12,true);
        var mesh=new Mesh {name="Taillights_SmoothLightGuides"}; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
        return mesh;
    }
    static void Capture(string name, Vector3 eye, Vector3 target, float scale)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        RenderTexture rt = null; Texture2D image = null; var previous=RenderTexture.active;
        try
        {
            var car = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/PorscheV.2.prefab"));
            SceneManager.MoveGameObjectToScene(car, scene); car.transform.position=Vector3.zero; car.transform.rotation=Quaternion.identity;
            foreach (var light in car.GetComponentsInChildren<Light>(true)) light.enabled=false;
            var lamp=new GameObject("Diagnostic key"); SceneManager.MoveGameObjectToScene(lamp,scene); lamp.transform.rotation=Quaternion.Euler(35,145,0); var key=lamp.AddComponent<Light>(); key.type=LightType.Directional; key.intensity=1.1f;
            var cg=new GameObject("Diagnostic camera"); SceneManager.MoveGameObjectToScene(cg,scene); var cam=cg.AddComponent<Camera>(); cam.enabled=false; cam.scene=scene;
            cam.transform.position=eye; cam.transform.LookAt(target); cam.orthographic=true; cam.orthographicSize=scale/2; cam.nearClipPlane=.01f; cam.farClipPlane=30;
            cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.035f,.035f,.045f); cam.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            rt = new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32); rt.Create();
            for(int i=0;i<2;i++) RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest { destination=rt });
            RenderTexture.active=rt; image=new Texture2D(1600,900,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1600,900),0,0); image.Apply(); File.WriteAllBytes(Work + "/" + name + ".png",image.EncodeToPNG());
        }
        finally { RenderTexture.active=previous; if(image) UnityEngine.Object.DestroyImmediate(image); if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);} EditorSceneManager.ClosePreviewScene(scene); }
    }
}
