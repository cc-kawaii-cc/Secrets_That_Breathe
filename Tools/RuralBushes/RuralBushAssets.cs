using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SecretsThatBreathe.RuralBushes;
public static class RuralBushAssets
{
    const string Root="Assets/SecretsThatBreathe/Environment/RuralBushes";
    static void Require(bool v,string message){if(!v)throw new Exception(message);}
    static string report="";
    public static void Build()
    {
        try {
            PlayerSettings.colorSpace=ColorSpace.Linear;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string folder in new[]{"Meshes","Materials","Prefabs","Demo","Preview"})Directory.CreateDirectory(Root+"/"+folder);
            var shader=Shader.Find("Secrets That Breathe/Rural Bush Wind Lit");Require(shader!=null,"Wind shader missing");
            var plants=new List<GameObject>();
            foreach(string name in new[]{"Khoi","SiamWeed","Lantana"}) {
                var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/"+name+"_Atlas.png");ti.maxTextureSize=512;ti.wrapMode=TextureWrapMode.Clamp;ti.SaveAndReimport();
                var mat=new Material(shader){name=name+"_Wind"};mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+name+"_Atlas.png"));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.12f);mat.SetFloat("_Metallic",0);mat.SetFloat("_Cull",0);mat.enableInstancing=true;
                AssetDatabase.CreateAsset(mat,Root+"/Materials/"+name+"_Wind.mat");
                var root=new GameObject(name+"_Bush");var lods=new LOD[2];float height=0;int total=0;
                for(int level=0;level<2;level++) {
                    string path=Root+"/Models/"+name+"_LOD"+level+".fbx";
                    var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.isReadable=true;importer.SaveAndReimport();
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var src=model.GetComponentInChildren<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(src.sharedMesh);mesh.name=name+"_LOD"+level;
                    var matrix=src.transform.localToWorldMatrix;var vertices=mesh.vertices;var normals=mesh.normals;
                    for(int i=0;i<vertices.Length;i++)vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);
                    for(int i=0;i<normals.Length;i++)normals[i]=matrix.MultiplyVector(normals[i]).normalized;
                    mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();mesh.RecalculateTangents();
                    Require(mesh.bounds.min.y>=-.001f&&mesh.bounds.size.y>.6f&&mesh.bounds.size.y<1.6f,"Incorrect plant scale/pivot: "+name+" "+mesh.bounds);
                    Require(mesh.uv.Length==mesh.vertexCount,"Foliage UV missing");
                    AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+name+"_LOD"+level+".asset");
                    var child=new GameObject("Leaves_"+name+"_LOD"+level);child.transform.SetParent(root.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;var r=child.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.lightProbeUsage=LightProbeUsage.BlendProbes;r.shadowCastingMode=ShadowCastingMode.TwoSided;
                    lods[level]=new LOD(level==0?.14f:.025f,new Renderer[]{r});height=Mathf.Max(height,mesh.bounds.size.y);
                    int triangles=mesh.triangles.Length/3;if(level==0)total=triangles;else Require(triangles<total*.4f,"LOD reduction insufficient");
                    report+=name+" LOD"+level+": "+triangles+" triangles, "+mesh.bounds.size+" m\n";
                }
                var group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();group.size+=.5f;
                var wind=root.AddComponent<RuralBushWind>();wind.bushHeight=height;wind.swayDistance=name=="Khoi"?.045f:.065f;
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+name+"_Bush.prefab");
                root.transform.position=new Vector3((plants.Count-1)*2.1f,0,0);plants.Add(root);
            }
            var zone=new GameObject("Wind Zone").AddComponent<WindZone>();zone.mode=WindZoneMode.Directional;zone.windMain=1;zone.windTurbulence=.45f;zone.windPulseMagnitude=.25f;zone.windPulseFrequency=.25f;zone.transform.rotation=Quaternion.Euler(0,55,0);
            PrefabUtility.SaveAsPrefabAsset(zone.gameObject,Root+"/Prefabs/RuralBush_WindZone.prefab");
            TestWind(plants,zone);
            SetupPreview();
            foreach(var p in plants)p.GetComponent<LODGroup>().ForceLOD(0);
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.gameObject.AddComponent<UniversalAdditionalCameraData>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.18f,.20f);camera.orthographic=true;camera.orthographicSize=1.65f;camera.nearClipPlane=.02f;camera.farClipPlane=50;camera.transform.position=new Vector3(2.5f,2.8f,6);camera.transform.LookAt(new Vector3(0,.6f,0));
            var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Preview ground";ground.transform.localScale=Vector3.one*2;
            var groundMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMat.SetColor("_BaseColor",new Color(.30f,.27f,.21f));AssetDatabase.CreateAsset(groundMat,Root+"/Demo/Ground.mat");ground.GetComponent<Renderer>().sharedMaterial=groundMat;
            foreach(var p in plants)p.GetComponent<RuralBushWind>().ApplyWind(0);
            var calm=Render(camera,"RuralBushes_Wind0");
            foreach(var p in plants)p.GetComponent<RuralBushWind>().ApplyWind(2.35f);
            var blowing=Render(camera,"RuralBushes_Wind1");
            int changed=Difference(calm,blowing);Require(changed>80,"GPU wind preview did not visibly change: "+changed);report+="GPU wind: "+changed+" pixels changed between deterministic wind samples.\n";
            zone.windMain=0;zone.windTurbulence=0;
            foreach(var p in plants)p.GetComponent<RuralBushWind>().ApplyWind(0);
            var still0=Render(camera,"RuralBushes_Still");
            foreach(var p in plants)p.GetComponent<RuralBushWind>().ApplyWind(2.35f);
            var still1=Render(camera,null);int stationary=Difference(still0,still1);Require(stationary<20,"Zero wind still moving: "+stationary);report+="Zero-wind GPU check: "+stationary+" pixels changed.\n";
            zone.windMain=1;zone.windTurbulence=.45f;
            for(int i=0;i<plants.Count;i++) {
                foreach(var p in plants)p.SetActive(false);plants[i].SetActive(true);plants[i].GetComponent<LODGroup>().ForceLOD(0);plants[i].GetComponent<RuralBushWind>().ApplyWind(0);
                camera.orthographicSize=.9f;camera.transform.position=plants[i].transform.position+new Vector3(1.6f,1.35f,3);camera.transform.LookAt(plants[i].transform.position+new Vector3(0,.60f,0));
                Render(camera,plants[i].name+"_Front");camera.transform.position=plants[i].transform.position+new Vector3(-1.6f,1.35f,-3);camera.transform.LookAt(plants[i].transform.position+new Vector3(0,.60f,0));Render(camera,plants[i].name+"_Back");
            }
            foreach(var p in plants){p.SetActive(true);p.GetComponent<LODGroup>().ForceLOD(-1);p.GetComponent<RuralBushWind>().windZone=null;}
            camera.orthographicSize=1.65f;camera.transform.position=new Vector3(2.5f,2.8f,6);camera.transform.LookAt(new Vector3(0,.6f,0));
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Demo/RuralBushes_WindDemo.unity");
            File.WriteAllText(Root+"/UnityVerification.txt",report);Debug.Log("RURAL_BUSHES_PASS\n"+report);EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void TestWind(List<GameObject> plants,WindZone zone)
    {
        zone.transform.rotation=Quaternion.identity;zone.windPulseMagnitude=0;zone.windMain=1;zone.windTurbulence=.5f;
        var one=RuralBushWind.SampleZone(zone,Vector3.zero,0);Require(Mathf.Abs(one.z-1)<.001f,"Directional force wrong");
        zone.windMain=2;var two=RuralBushWind.SampleZone(zone,Vector3.zero,0);Require(Mathf.Abs(two.z-2*one.z)<.001f,"Main scaling wrong");
        zone.windPulseMagnitude=1;zone.windPulseFrequency=1;var pulse0=RuralBushWind.SampleZone(zone,Vector3.zero,0);var pulse1=RuralBushWind.SampleZone(zone,Vector3.zero,.25f);Require(pulse1.z>pulse0.z,"Pulse wrong");
        zone.mode=WindZoneMode.Spherical;zone.radius=2;Require(RuralBushWind.SampleZone(zone,new Vector3(3,0,0),0)==Vector4.zero,"Spherical radius ignored");Require(RuralBushWind.SampleZone(zone,new Vector3(1,0,0),0).x>0,"Spherical direction wrong");
        zone.gameObject.SetActive(false);Require(RuralBushWind.SampleZone(zone,Vector3.zero,0)==Vector4.zero,"Inactive zone still blows");zone.gameObject.SetActive(true);zone.mode=WindZoneMode.Directional;zone.windMain=1;zone.windPulseMagnitude=.25f;zone.windPulseFrequency=.25f;zone.transform.rotation=Quaternion.Euler(0,55,0);
        foreach(var p in plants) {
            var c=p.GetComponent<RuralBushWind>();c.windZone=zone;c.ApplyWind(0);var r=p.GetComponentInChildren<MeshRenderer>();var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);Require(block.GetVector("_RuralBushWind").magnitude>0,"Zone not sent to shader");c.enabled=false;r.GetPropertyBlock(block);Require(block.GetVector("_RuralBushWind")==Vector4.zero,"Disabled component retained wind");c.enabled=true;c.windZone=zone;
        }
        report+="PASS: directional Main scaling, pulse, spherical radius/direction, inactive zone, per-renderer binding, component disable/reset.\n";
    }
    static int Difference(Color32[] a,Color32[] b){int n=0;for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>5)n++;return n;}
    static Color32[] Render(Camera c,string name)
    {
        var rt=new RenderTexture(1400,800,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);c.targetTexture=rt;c.Render();c.Render();RenderTexture.active=rt;var image=new Texture2D(1400,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,800),0,0);image.Apply();if(name!=null)File.WriteAllBytes(Root+"/Preview/"+name+".png",image.EncodeToPNG());var pixels=image.GetPixels32();RenderTexture.active=null;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);return pixels;
    }
    static void SetupPreview()
    {
        var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,Root+"/Demo/PreviewRenderer.asset");
        var pipeline=ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();var so=new SerializedObject(pipeline);so.FindProperty("m_RendererDataList").arraySize=1;so.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(pipeline,Root+"/Demo/PreviewPipeline.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.40f,.43f,.37f);
        var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.6f;sun.transform.rotation=Quaternion.Euler(40,-35,0);sun.shadows=LightShadows.Soft;
    }
}
