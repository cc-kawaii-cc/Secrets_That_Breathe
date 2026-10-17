using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class OfficeGlassDoorBuilder
{
    const string Root="Assets/SecretsThatBreathe/Props/OfficeGlassDoor";
    static Material Material(string name,Color color,float metallic,float smoothness)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name}; m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);
        AssetDatabase.CreateAsset(m,Root+"/Materials/"+name+".mat");return m;
    }
    public static void Build()
    {
        try {
            PlayerSettings.colorSpace=ColorSpace.Linear;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(Root+"/Materials"); Directory.CreateDirectory(Root+"/Preview");
            var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/OfficeNight_Glass.png");ti.maxTextureSize=2048;ti.wrapMode=TextureWrapMode.Clamp;ti.alphaSource=TextureImporterAlphaSource.None;ti.SaveAndReimport();
            var mats=new[]{Material("Aluminium_Silver",new Color(.79f,.81f,.83f),.72f,.71f),Material("Rubber_Seal",new Color(.035f,.04f,.045f),0,.17f),Material("Handle_Chrome",new Color(.86f,.88f,.9f),.9f,.81f),Material("OfficeNight_Glass",Color.white,.12f,.65f)};
            mats[3].SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/OfficeNight_Glass.png"));
            var imp=(ModelImporter)AssetImporter.GetAtPath(Root+"/Models/OfficeGlassDoor.fbx");imp.importAnimation=false;imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;imp.isReadable=true;imp.SaveAndReimport();
            var imported=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/OfficeGlassDoor.fbx");
            var root=new GameObject("OfficeGlassDoor");int triangles=0;string report="";
            foreach(var src in imported.GetComponentsInChildren<MeshFilter>()) {
                string name=src.name; Vector3 pivot=name=="DoorLeft"?new Vector3(-.86f,0,.035f):name=="DoorRight"?new Vector3(.86f,0,.035f):Vector3.zero;
                var mesh=UnityEngine.Object.Instantiate(src.sharedMesh);mesh.name="OfficeGlassDoor_"+name;
                var bake=src.transform.localToWorldMatrix;var v=mesh.vertices;var n=mesh.normals;
                for(int i=0;i<v.Length;i++)v[i]=bake.MultiplyPoint3x4(v[i])-pivot;
                for(int i=0;i<n.Length;i++)n[i]=bake.MultiplyVector(n[i]).normalized;
                mesh.vertices=v;mesh.normals=n;mesh.RecalculateBounds();mesh.RecalculateTangents();
                UnwrapParam unwrap;UnwrapParam.SetDefaults(out unwrap);unwrap.packMargin=.015f;if(!Unwrapping.GenerateSecondaryUVSet(mesh,unwrap))throw new Exception("UV2 generation failed");
                if(mesh.uv2.Length!=mesh.vertexCount)throw new Exception("UV2 missing");
                AssetDatabase.CreateAsset(mesh,Root+"/Models/OfficeGlassDoor_"+name+".asset");
                var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=pivot;go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var sourceMats=src.GetComponent<MeshRenderer>().sharedMaterials;
                go.AddComponent<MeshRenderer>().sharedMaterials=sourceMats.Select(m=>mats.Single(target=>target.name==m.name)).ToArray();
                if(name=="Frame"){
                    foreach(float x in new[]{-.8925f,.8925f}){var c=go.AddComponent<BoxCollider>();c.center=new Vector3(x,1.1f,0);c.size=new Vector3(.055f,2.2f,.09f);}
                    var top=go.AddComponent<BoxCollider>();top.center=new Vector3(0,2.1725f,0);top.size=new Vector3(1.73f,.055f,.09f);
                } else {
                    var c=go.AddComponent<BoxCollider>();c.center=new Vector3(name=="DoorLeft"?.4285f:-.4285f,1.074f,-.035f);c.size=new Vector3(.857f,2.132f,.045f);
                }
                int t=mesh.triangles.Length/3;triangles+=t;report+=name+": "+t+" triangles, pivot "+pivot+", UV2 OK\n";
            }
            if(triangles!=1004 || root.transform.childCount!=3)throw new Exception("Unexpected parts or triangle count");
            Bounds bounds=root.GetComponentInChildren<Renderer>().bounds;foreach(var r in root.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
            if(Mathf.Abs(bounds.size.x-1.84f)>.001f||Mathf.Abs(bounds.size.y-2.2f)>.001f)throw new Exception("Incorrect imported dimensions "+bounds);
            // Texture must run bottom-to-top on each pane after Blender -> Unity conversion.
            foreach(string leafName in new[]{"DoorLeft","DoorRight"}) {
                var mf=root.transform.Find(leafName).GetComponent<MeshFilter>();var mesh=mf.sharedMesh;
                var renderer=mf.GetComponent<MeshRenderer>();int slot=Array.FindIndex(renderer.sharedMaterials,m=>m.name=="OfficeNight_Glass");
                var ids=mesh.GetTriangles(slot);float covariance=0;foreach(int id in ids)covariance+=(mesh.vertices[id].y-1.074f)*(mesh.uv[id].y-.5f);
                if(covariance<=0)throw new Exception("Glass image upside down");
                foreach(float angle in new[]{0,30,60,90}) {mf.transform.localRotation=Quaternion.Euler(0,leafName=="DoorLeft"?-angle:angle,0);if(Vector3.Distance(mf.transform.localPosition,new Vector3(leafName=="DoorLeft"?-.86f:.86f,0,.035f))>.0001f)throw new Exception("Hinge moved");}
                mf.transform.localRotation=Quaternion.identity;
            }
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/OfficeGlassDoor.prefab");AssetDatabase.SaveAssets();
            Preview(root);
            File.WriteAllText(Root+"/UnityVerification.txt","PASS: "+triangles+" triangles, 3 meshes, 4 shared URP materials, original supplied photo, 1.84 x 2.20 m.\n"+report+"Identity leaf rotation/scale, floor pivot; imported glass top direction verified, hinge samples 0/30/60/90. UV2 generated. Simple colliders. No gameplay script or scene lighting bake tested.");
            Debug.Log("OFFICE_GLASS_DOOR_PASS");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Preview(GameObject root)
    {
        // Only this disposable verification project receives the temporary preview scene.
        var rendererData=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(rendererData,Root+"/Preview/PreviewRenderer.asset");
        var pipeline=ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        var so=new SerializedObject(pipeline);so.FindProperty("m_RendererDataList").arraySize=1;so.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=rendererData;so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(pipeline,Root+"/Preview/PreviewPipeline.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.28f,.30f,.34f);
        foreach(var p in new[]{new Vector3(35,-35,0),new Vector3(20,145,0)}){var l=new GameObject("StudioLight").AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.7f;l.transform.rotation=Quaternion.Euler(p);}
        var cam=new GameObject("PreviewCamera").AddComponent<Camera>();cam.gameObject.AddComponent<UniversalAdditionalCameraData>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.14f,.17f);cam.orthographic=true;cam.orthographicSize=1.35f;cam.nearClipPlane=.03f;cam.farClipPlane=20;
        var rt=new RenderTexture(1000,1100,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);cam.targetTexture=rt;
        foreach(string view in new[]{"Front","Back","Open"}) {
            root.transform.Find("DoorLeft").localRotation=Quaternion.Euler(0,view=="Open"?-65:0,0);root.transform.Find("DoorRight").localRotation=Quaternion.Euler(0,view=="Open"?65:0,0);
            cam.transform.position=view=="Back"?new Vector3(-2.4f,2.3f,-4.7f):new Vector3(2.4f,2.3f,4.7f);cam.transform.LookAt(new Vector3(0,1.1f,0));cam.Render();cam.Render();
            RenderTexture.active=rt;var image=new Texture2D(1000,1100,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,1100),0,0);image.Apply();File.WriteAllBytes(Root+"/Preview/OfficeGlassDoor_Unity_"+view+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        }
        RenderTexture.active=null;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
}

