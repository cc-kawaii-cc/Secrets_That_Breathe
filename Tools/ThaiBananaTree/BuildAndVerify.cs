using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SecretsThatBreathe.ThaiBanana;

public static class ThaiBananaBuildAndVerify
{
    const string Root="Assets/SecretsThatBreathe/Environment/ThaiBananaTree";
    const string Report="ArtSource/ThaiBananaTree";
    static StringBuilder log=new StringBuilder();
    static void Check(bool condition,string message) { if(!condition) throw new Exception(message); log.AppendLine("PASS "+message); }
    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(Report); PlayerSettings.colorSpace=ColorSpace.Linear;
            var shader=Shader.Find("Secrets That Breathe/ThaiBanana Tree Wind Lit");
            Check(shader && !ShaderUtil.ShaderHasError(shader),"URP wind shader imported");
            foreach(var file in Directory.GetFiles(Root+"/Textures").Where(p=>!p.EndsWith(".meta")))
            {
                var importer=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;
                if(!importer) continue;
                bool normal=file.Contains("Normal");
                importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=!normal; importer.mipmapEnabled=true;
                importer.maxTextureSize=file.Contains("Trunk")?1024:512;
                importer.wrapMode=file.Contains("Trunk")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
                importer.anisoLevel=2; importer.filterMode=FilterMode.Trilinear;
                importer.textureCompression=TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            var materials=new Material[3];
            string[] names={"Trunk","Leaf","Fruit"};
            string[] textures={"Trunk_Albedo.png","Leaf_Photo.png","Fruit_PhotoAtlas.png"};
            for(int i=0;i<3;i++)
            {
                string path=Root+"/Materials/ThaiBanana_"+names[i]+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!mat) { mat=new Material(shader);AssetDatabase.CreateAsset(mat,path); }
                mat.shader=shader; mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+textures[i]));
                mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",i==2?.22f:.12f);mat.SetFloat("_Metallic",0);
                mat.SetFloat("_Cull",i==1?0:2); mat.SetFloat("_AlphaClip",0); mat.SetFloat("_ThaiBananaLeafFlutter",1);
                if(i==0) {mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Trunk_Normal.png"));mat.SetFloat("_BumpScale",.55f);mat.EnableKeyword("_NORMALMAP");}
                mat.enableInstancing=true;EditorUtility.SetDirty(mat);materials[i]=mat;
            }
            foreach(string variant in new[]{"Green","Ripening","Ripe"})
            {
                string fbx=Root+"/Models/ThaiBanana_"+variant+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);
                importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;
                importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
                importer.meshCompression=ModelImporterMeshCompression.Off;
                importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                for(int i=0;i<3;i++) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"ThaiBanana_"+names[i]),materials[i]);
                importer.SaveAndReimport();
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                var root=new GameObject("ThaiBanana_"+variant);
                var inst=(GameObject)PrefabUtility.InstantiatePrefab(model);inst.name="Model";inst.transform.SetParent(root.transform,false);
                foreach(var group in inst.GetComponentsInChildren<LODGroup>()) UnityEngine.Object.DestroyImmediate(group);
                var all=inst.GetComponentsInChildren<MeshRenderer>(true);
                var lods=new LOD[3];float[] transitions={.28f,.12f,.035f};
                for(int lod=0;lod<3;lod++)
                {
                    int level=lod;
                    var renderers=all.Where(r=>r.name.Contains("_LOD"+level)).ToArray();
                    Check(renderers.Length==3,variant+" LOD"+lod+" three material renderers");
                    int triangles=0;Bounds bounds=new Bounds();bool first=true;
                    foreach(var renderer in renderers)
                    {
                        renderer.enabled=true;renderer.gameObject.SetActive(true);
                        int index=renderer.name.StartsWith("Trunk")?0:renderer.name.StartsWith("Leaves")?1:2;
                        renderer.sharedMaterial=materials[index];renderer.shadowCastingMode=index==1?ShadowCastingMode.TwoSided:ShadowCastingMode.On;
                        var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                        Check(mesh.colors.Length==mesh.vertexCount,renderer.name+" wind masks retained by FBX");
                        Check(mesh.uv.Length==mesh.vertexCount,renderer.name+" UV mapping retained");
                        triangles+=mesh.triangles.Length/3;
                        if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
                    }
                    Check(bounds.min.y>-.01f && bounds.max.y>3 && bounds.max.y<4.5f,variant+" metre scale and grounded Y-up LOD"+lod);
                    log.AppendLine(variant+" LOD"+lod+": "+triangles+" triangles, height "+bounds.size.y.ToString("F3")+" m");
                    lods[lod]=new LOD(transitions[lod],renderers);
                }
                var collider=root.AddComponent<CapsuleCollider>();collider.radius=.18f;collider.height=2.45f;collider.center=new Vector3(0,1.225f,0);
                var lodGroup=root.AddComponent<LODGroup>();lodGroup.SetLODs(lods);lodGroup.fadeMode=LODFadeMode.None;lodGroup.RecalculateBounds();
                root.AddComponent<ThaiBananaWind>();
                string path=Root+"/Prefabs/ThaiBanana_"+variant+".prefab";
                PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);
                importer.isReadable=false;importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            RenderAndTest();
            Check(!ShaderUtil.ShaderHasError(shader),"No URP shader errors after rendering");
            File.WriteAllText(Report+"/Unity-validation.txt",log.ToString());
            Debug.Log("THAI_BANANA_PASS\n"+log);EditorApplication.Exit(0);
        }
        catch(Exception e) { File.WriteAllText(Report+"/Unity-validation.txt",log+"\nFAIL "+e);Debug.LogException(e);EditorApplication.Exit(1); }
    }
    static void RenderAndTest()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var data=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(data,"Assets/ThaiBananaVerificationRenderer.asset");
        var pipeline=UniversalRenderPipelineAsset.Create(data);pipeline.shadowDistance=25;pipeline.msaaSampleCount=4;pipeline.mainLightShadowmapResolution=2048;
        AssetDatabase.CreateAsset(pipeline,"Assets/ThaiBananaVerificationPipeline.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
        var tree=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ThaiBanana_Ripening.prefab"));
        tree.GetComponent<LODGroup>().ForceLOD(0);var wind=tree.GetComponent<ThaiBananaWind>();
        var zone=new GameObject("Wind Zone").AddComponent<WindZone>();zone.mode=WindZoneMode.Directional;zone.windMain=1;zone.windTurbulence=.7f;zone.windPulseMagnitude=.2f;zone.windPulseFrequency=.25f;
        zone.transform.rotation=Quaternion.Euler(0,40,0);
        // Leave the reference empty: exercise automatic Wind Zone discovery.
        wind.ApplyWind(0);
        var renderer=tree.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("Leaves_LOD0"));
        var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
        Check(block.GetVector("_ThaiBananaWind").sqrMagnitude>.1f,"Existing directional Wind Zone discovered without a separate controller");
        zone.windMain=0;wind.ApplyWind(1);renderer.GetPropertyBlock(block);
        Vector4 force=block.GetVector("_ThaiBananaWind");Check(new Vector3(force.x,force.y,force.z).magnitude<.0001f,"Wind Main zero stops all shader motion");
        zone.mode=WindZoneMode.Spherical;zone.radius=2;zone.transform.position=new Vector3(10,0,0);
        Check(ThaiBananaWind.SampleZone(zone,tree.transform.position,1)==Vector4.zero,"Spherical Wind Zone radius respected");
        zone.transform.position=new Vector3(-1,0,0);zone.windMain=1;
        Check(ThaiBananaWind.SampleZone(zone,tree.transform.position,1).x>0,"Spherical wind blows outward with distance falloff");
        zone.gameObject.SetActive(false);wind.ApplyWind(1);renderer.GetPropertyBlock(block);force=block.GetVector("_ThaiBananaWind");Check(force==Vector4.zero,"Inactive Wind Zones do not move trees");
        zone.gameObject.SetActive(true);zone.mode=WindZoneMode.Directional;zone.transform.rotation=Quaternion.Euler(0,40,0);zone.windMain=1;
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.transform.localScale=Vector3.one;ground.transform.position=new Vector3(0,-.02f,0);
        var groundMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMat.color=new Color(.20f,.23f,.14f);groundMat.SetFloat("_Smoothness",.05f);ground.GetComponent<Renderer>().sharedMaterial=groundMat;
        var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(45,-30,0);
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.45f,.51f,.58f);RenderSettings.ambientEquatorColor=new Color(.25f,.29f,.22f);RenderSettings.ambientGroundColor=new Color(.12f,.11f,.085f);
        var camera=new GameObject("Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=2.65f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.31f,.35f,.37f);camera.nearClipPlane=.1f;camera.farClipPlane=50;
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>();camera.transform.position=new Vector3(5,3.8f,-7);camera.transform.LookAt(new Vector3(0,1.85f,0));
        wind.strength=0;wind.ApplyWind(0);var still=Capture(camera,Report+"/Unity_Still.png");
        wind.strength=1;wind.ApplyWind(2.25f);var moving=Capture(camera,Report+"/Unity_Wind_A.png");
        wind.ApplyWind(3.75f);var moving2=Capture(camera,Report+"/Unity_Wind_B.png");
        Check(Difference(still,moving)>100,"GPU wind visibly changes the rendered leaves");
        Check(Difference(moving,moving2)>100,"GPU motion changes over time");
        camera.transform.position=new Vector3(-5,3.8f,7);camera.transform.LookAt(new Vector3(0,1.85f,0));wind.strength=0;wind.ApplyWind(0);Capture(camera,Report+"/Unity_Back.png");
        for(int lod=1;lod<3;lod++){tree.GetComponent<LODGroup>().ForceLOD(lod);Capture(camera,Report+"/Unity_LOD"+lod+".png");}
        wind.enabled=false;renderer.GetPropertyBlock(block);Check(block.GetVector("_ThaiBananaWind")==Vector4.zero,"Disabling the component clears its wind without altering shared materials");
        // Render the green variant as well.
        UnityEngine.Object.DestroyImmediate(tree);
        tree=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ThaiBanana_Green.prefab"));tree.GetComponent<LODGroup>().ForceLOD(0);tree.GetComponent<ThaiBananaWind>().strength=0;tree.GetComponent<ThaiBananaWind>().ApplyWind(0);
        camera.transform.position=new Vector3(5,3.8f,-7);camera.transform.LookAt(new Vector3(0,1.85f,0));Capture(camera,Report+"/Unity_Green.png");
        UnityEngine.Object.DestroyImmediate(tree);
        tree=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ThaiBanana_Ripe.prefab"));tree.GetComponent<LODGroup>().ForceLOD(0);tree.GetComponent<ThaiBananaWind>().strength=0;tree.GetComponent<ThaiBananaWind>().ApplyWind(0);
        camera.transform.position=new Vector3(-5,3.8f,7);camera.transform.LookAt(new Vector3(0,1.85f,0));Capture(camera,Report+"/Unity_Ripe.png");
        // Standalone editable demonstration, saved only inside the new asset folder.
        Directory.CreateDirectory(Root+"/Demo");AssetDatabase.Refresh();
        AssetDatabase.CreateAsset(groundMat,Root+"/Demo/Ground.mat");
        UnityEngine.Object.DestroyImmediate(tree);
        foreach(var pair in new[]{("Green",-2f),("Ripening",0f),("Ripe",2f)})
        {
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ThaiBanana_"+pair.Item1+".prefab"));
            actor.transform.position=new Vector3(pair.Item2*1.6f,0,0);actor.GetComponent<ThaiBananaWind>().ApplyWind(0);
        }
        camera.orthographicSize=5.8f;camera.transform.position=new Vector3(8,5,-12);camera.transform.LookAt(new Vector3(0,1.8f,0));
        EditorSceneManager.SaveScene(scene,Root+"/Demo/ThaiBanana_WindDemo.unity");
    }
    static Color32[] Capture(Camera camera,string path)
    {
        var rt=new RenderTexture(1000,1000,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();camera.targetTexture=rt;
        camera.Render();camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
        File.WriteAllBytes(path,image.EncodeToPNG());var pixels=image.GetPixels32();UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);return pixels;
    }
    static int Difference(Color32[] a,Color32[] b)
    { int count=0;for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>18)count++;log.AppendLine("Changed pixels: "+count);return count; }
}
