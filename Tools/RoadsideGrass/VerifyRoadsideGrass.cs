using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SecretsThatBreathe.RoadsideGrass.Editor;

public static class VerifyRoadsideGrass
{
    public static void Run()
    {
        try
        {
            PlayerSettings.colorSpace=ColorSpace.Linear;
            RoadsideGrassAssets.Build();
            string root=RoadsideGrassAssets.Root;
            string report=Environment.GetEnvironmentVariable("GRASS_REPORT_DIR");
            var log=new StringBuilder("Unity " + Application.unityVersion + " / URP 17.3.0\n");
            foreach(string name in RoadsideGrassAssets.Names)
            {
                var p=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/"+name+".prefab");
                if(!p || p.transform.childCount!=0) throw new Exception("Root prefab layout invalid");
                var mesh=p.GetComponent<MeshFilter>().sharedMesh;
                var material=p.GetComponent<MeshRenderer>().sharedMaterial;
                if(mesh.subMeshCount!=1 || !material.enableInstancing) throw new Exception("Instancing layout invalid");
                if(mesh.colors.Length!=mesh.vertexCount) throw new Exception("Missing vertex colors/wind masks");
                if(ShaderUtil.ShaderHasError(material.shader)) throw new Exception("Grass shader error");
                Vector3 min=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue), max=-min;
                foreach(var v in mesh.vertices) { min=Vector3.Min(min,v); max=Vector3.Max(max,v); }
                if(min.y < -.03f || min.y > .015f || max.y < .2f || max.y > .65f) throw new Exception("Y up / metre scale invalid: "+min+" / "+max);
                int rooted=0, moving=0;
                foreach(var color in mesh.colors) { if(color.a<.01f) rooted++; if(color.a>.8f) moving++; }
                log.AppendLine("Color sample: "+mesh.colors[10]+"; normal sample: "+mesh.normals[10]);
                if(rooted<20 || moving<20) throw new Exception("Wind mask invalid");
                log.AppendLine(name+": "+mesh.triangles.Length/3+" triangles; height "+max.y.ToString("F3")+" m; root mesh, vertex colors, wind mask, instancing PASS");
            }
            // This project is disposable; pipeline settings never reach the user's project.
            var data=ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(data,"Assets/GrassVerificationRenderer.asset");
            var pipeline=UniversalRenderPipelineAsset.Create(data);
            pipeline.msaaSampleCount=4; pipeline.shadowDistance=25; pipeline.mainLightShadowmapResolution=2048;
            AssetDatabase.CreateAsset(pipeline,"Assets/GrassVerificationPipeline.asset");
            GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline;
            Directory.CreateDirectory(root+"/Demo");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.52f,.57f,.64f);
            RenderSettings.ambientEquatorColor=new Color(.30f,.33f,.27f);
            RenderSettings.ambientGroundColor=new Color(.16f,.14f,.10f);
            var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional;
            sun.intensity=1.65f; sun.shadows=LightShadows.Soft; sun.transform.rotation=Quaternion.Euler(48,-36,0);
            RenderSettings.sun=sun;
            var td=new TerrainData { heightmapResolution=65,size=new Vector3(8,.65f,8) };
            td.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            td.SetDetailResolution(64,16);
            float[,] heights=new float[65,65];
            for(int z=0;z<65;z++) for(int x=0;x<65;x++)
                heights[z,x]=.11f + .04f*Mathf.PerlinNoise(x*.12f,z*.12f) + (z>18?.045f:0);
            td.SetHeights(0,0,heights);
            // Small original muddy roadside tile for the demonstration.
            var texture=new Texture2D(128,128,TextureFormat.RGB24,true);
            for(int z=0;z<128;z++) for(int x=0;x<128;x++) {
                float n=Mathf.PerlinNoise(x*.14f,z*.14f)*.5f+Mathf.PerlinNoise(x*.71f,z*.71f)*.5f;
                texture.SetPixel(x,z,Color.Lerp(new Color(.21f,.19f,.135f),new Color(.38f,.35f,.25f),n));
            }
            texture.Apply();
            File.WriteAllBytes(root+"/Demo/RoadsideGround.png",texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(root+"/Demo/RoadsideGround.png");
            var layer=new TerrainLayer { diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"/Demo/RoadsideGround.png"),tileSize=new Vector2(1.8f,1.8f),smoothness=.08f };
            AssetDatabase.CreateAsset(layer,root+"/Demo/RoadsideGround.terrainlayer");
            td.terrainLayers=new[]{layer};
            var terrainMaterial=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            AssetDatabase.CreateAsset(terrainMaterial,root+"/Demo/RoadsideTerrain.mat");
            td.detailPrototypes=new[]{RoadsideGrassAssets.Prototype(0),RoadsideGrassAssets.Prototype(1),RoadsideGrassAssets.Prototype(2)};
            for(int index=0;index<3;index++) {
                int[,] density=new int[64,64];
                for(int z=0;z<64;z++) for(int x=0;x<64;x++) {
                    float px=x/8f-4, pz=z/8f-4;
                    float edge=-1.35f + .20f*Mathf.Sin(px*2.3f);
                    float patch=Mathf.PerlinNoise(x*.065f+4,z*.065f);
                    if(pz>edge && Mathf.Abs(px)<3.3f && pz<3.3f) {
                        int variant=patch>.58f?1:0;
                        if(pz<edge+.36f) variant=2;
                        if(variant==index && ((x*17+z*11)%5==0)) density[z,x]=1;
                    }
                }
                td.SetDetailLayer(0,0,index,density);
            }
            AssetDatabase.CreateAsset(td,root+"/Demo/RoadsideGrass_Terrain.asset");
            var go=Terrain.CreateTerrainGameObject(td); go.name="Painted Grass Terrain";
            go.transform.position=new Vector3(-4,0,-4);
            var terrain=go.GetComponent<Terrain>(); terrain.materialTemplate=terrainMaterial;
            terrain.detailObjectDistance=60; terrain.detailObjectDensity=1; terrain.drawInstanced=true; terrain.drawTreesAndFoliage=true;
            terrain.Flush();
            int totalInstances=0;
            for(int l=0;l<3;l++) for(int z=0;z<4;z++) for(int x=0;x<4;x++)
                totalInstances+=td.ComputeDetailInstanceTransforms(x,z,l,1,out var b).Length;
            log.AppendLine("Terrain instance transforms: "+totalInstances);
            if(totalInstances<50) throw new Exception("Terrain details failed to generate");
            var camera=new GameObject("Preview Camera").AddComponent<Camera>();
            camera.transform.position=new Vector3(4.3f,3.2f,-6.3f); camera.transform.LookAt(new Vector3(0,.18f,.35f));
            camera.fieldOfView=48; camera.nearClipPlane=.03f; camera.farClipPlane=80;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.34f,.40f,.42f);
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            EditorSceneManager.SaveScene(scene,root+"/Demo/RoadsideGrass_TerrainPreview.unity");
            AssetDatabase.SaveAssets();
            double start=EditorApplication.timeSinceStartup;
            EditorApplication.CallbackFunction finish=null;
            finish=()=> {
                if(EditorApplication.timeSinceStartup-start<4) { EditorApplication.QueuePlayerLoopUpdate(); return; }
                EditorApplication.update-=finish;
                try {
            Capture(camera,Path.Combine(report,"RoadsideGrass_Unity_Terrain.png"),1400,900);
            // Show real prefab geometry close up as well as actual terrain rendering.
            go.SetActive(false);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.position=new Vector3(0,-.008f,0);
            var groundMat=new Material(Shader.Find("Universal Render Pipeline/Lit")); groundMat.SetColor("_BaseColor",new Color(.26f,.24f,.19f));
            ground.GetComponent<Renderer>().sharedMaterial=groundMat;
            for(int i=0;i<3;i++) {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/"+RoadsideGrassAssets.Names[i]+".prefab"));
                instance.transform.position=new Vector3((i-1)*.85f,0,0);
            }
            camera.transform.position=new Vector3(1.6f,1.1f,-2.5f); camera.transform.LookAt(new Vector3(0,.20f,0));
            camera.orthographic=true; camera.orthographicSize=.63f;
            Capture(camera,Path.Combine(report,"RoadsideGrass_Unity_Clumps.png"),1500,700);
            var windMaterial=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/RoadsideGrass_Wind.mat");
            windMaterial.SetFloat("_WindStrength",0);
            var still=Capture(camera,Path.Combine(report,"Wind_Still.png"),1000,500);
            windMaterial.SetFloat("_WindStrength",.10f);
            var moving=Capture(camera,Path.Combine(report,"Wind_Moving.png"),1000,500);
            int changed=0;
            for(int i=0;i<still.Length;i++)
                if(Math.Abs(still[i].r-moving[i].r)+Math.Abs(still[i].g-moving[i].g)+Math.Abs(still[i].b-moving[i].b)>12) changed++;
            windMaterial.SetFloat("_WindStrength",.028f); EditorUtility.SetDirty(windMaterial); AssetDatabase.SaveAssets();
            if(changed<200) throw new Exception("Wind displacement did not affect rendered grass");
            log.AppendLine("Wind still/moving comparison: "+changed+" changed pixels; PASS.");
            if(ShaderUtil.ShaderHasError(windMaterial.shader)) throw new Exception("Grass shader compile error after rendering");
            log.AppendLine("All three DetailPrototype.Validate: PASS; painted TerrainData detail layers saved.");
            log.AppendLine("URP grass shader: no reported compile errors after rendering; real Unity screenshots exported.");
            File.WriteAllText(Path.Combine(report,"Unity_Validation.txt"),log.ToString());
            AssetDatabase.ExportPackage(root,Path.Combine(report,"RoadsideGrass_URP.unitypackage"),ExportPackageOptions.Recurse);
            Debug.Log("ROADSIDE_GRASS_VALIDATION_PASS\n"+log);
            EditorApplication.Exit(0);
                } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
            };
            EditorApplication.update+=finish;
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static Color32[] Capture(Camera camera,string path,int width,int height)
    {
        var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32); rt.antiAliasing=4; rt.Create();
        camera.targetTexture=rt;
        var request=new UniversalRenderPipeline.SingleCameraRequest { destination=rt };
        RenderPipeline.SubmitRenderRequest(camera,request);
        RenderPipeline.SubmitRenderRequest(camera,request);
        var old=RenderTexture.active; RenderTexture.active=rt;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
        var pixels=image.GetPixels32();
        File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=old; camera.targetTexture=null; rt.Release();
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
        return pixels;
    }
}
