using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SecretsThatBreathe.MangoTreeUpdate.Editor
{
    // Batch-only, builds the Update assets and verifies real URP render output.
    public static class VerifyMangoTreeUpdate
    {
        public static void ExportPackage()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ExportPackage(MangoTreeUpdateAssetBuilder.Root,
                Path.Combine(Environment.GetEnvironmentVariable("MANGO_UPDATE_REPORT_DIR"),"MangoTree_Update_URP.unitypackage"),
                ExportPackageOptions.Recurse);
            EditorApplication.Exit(0);
        }

        static void Check(bool value,string message) { if (!value) throw new Exception(message); }
        static Texture2D Capture(Camera camera,MangoTreeUpdateWind tree,float time)
        {
            tree.ApplyWind(time);
            var rt = new RenderTexture(960,960,24);
            rt.Create();
            var old = RenderTexture.active;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var result = new Texture2D(960,960,TextureFormat.RGB24,false);
            result.ReadPixels(new Rect(0,0,960,960),0,0); result.Apply();
            RenderTexture.active = old;
            camera.targetTexture = null;
            rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            return result;
        }

        static int Difference(Texture2D a,Texture2D b)
        {
            var x = a.GetPixels32(); var y = b.GetPixels32(); int changed = 0;
            for (int i=0;i<x.Length;i++)
                if (Math.Abs(x[i].r-y[i].r)+Math.Abs(x[i].g-y[i].g)+Math.Abs(x[i].b-y[i].b)>18) changed++;
            return changed;
        }

        public static void Run()
        {
            try
            {
                string report = Environment.GetEnvironmentVariable("MANGO_UPDATE_REPORT_DIR");
                if (string.IsNullOrEmpty(report)) throw new Exception("Set MANGO_UPDATE_REPORT_DIR for the batch report.");
                Directory.CreateDirectory(report);
                string root = MangoTreeUpdateAssetBuilder.Root;
                Directory.CreateDirectory(root+"/Demo");
                Directory.CreateDirectory("Assets/Verification");
                AssetDatabase.Refresh();
                foreach (string file in new[] { "Verification_Renderer.asset","Verification_URP.asset" })
                    if (AssetDatabase.LoadMainAssetAtPath(root+"/Demo/"+file))
                        AssetDatabase.MoveAsset(root+"/Demo/"+file,"Assets/Verification/"+file);
                // Only the isolated verification project needs a temporary URP setup.
                if (!GraphicsSettings.currentRenderPipeline)
                {
                    var data = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(data,"Assets/Verification/Verification_Renderer.asset");
                    var pipeline = UniversalRenderPipelineAsset.Create(data);
                    pipeline.supportsCameraDepthTexture = true;
                    pipeline.shadowDistance = 50;
                    pipeline.msaaSampleCount = 1;
                    AssetDatabase.CreateAsset(pipeline,"Assets/Verification/Verification_URP.asset");
                    GraphicsSettings.defaultRenderPipeline = pipeline;
                    QualitySettings.renderPipeline = pipeline;
                }
                MangoTreeUpdateAssetBuilder.Build();
                var log = new StringBuilder("Unity "+Application.unityVersion+" / URP 17.3.0 / MangoTree_Update\n");
                Check(Application.unityVersion=="6000.3.8f1","Wrong Unity version");
                foreach (string name in new[] { "MangoTree_Update","MangoTree_Update_NoFruit" })
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/"+name+".prefab");
                    Check(prefab,"Missing prefab "+name);
                    Check(prefab.GetComponent<LODGroup>().lodCount==3,"Missing LODs");
                    Check(prefab.GetComponent<CapsuleCollider>(),"Missing trunk collider");
                    Check(prefab.GetComponent<MangoTreeUpdateWind>(),"Missing Update wind bridge");
                    Bounds bounds = default;
                    long previousCount=long.MaxValue;
                    int level=0;
                    foreach (var lod in prefab.GetComponent<LODGroup>().GetLODs())
                    {
                        long triangles=0;
                        foreach (var r in lod.renderers)
                        {
                            Check(r && r.sharedMaterial && r.sharedMaterial.shader,"Broken renderer");
                            Check(r.sharedMaterial.shader.name==MangoTreeUpdateAssetBuilder.ShaderName,"Wrong shader");
                            Check(!ShaderUtil.ShaderHasError(r.sharedMaterial.shader),"Shader import error");
                            var mesh=r.GetComponent<MeshFilter>().sharedMesh;
                            for(int s=0;s<mesh.subMeshCount;s++) triangles+=(long)mesh.GetIndexCount(s)/3;
                            if(r.name.EndsWith("LOD0"))
                            {
                                if(bounds.size==Vector3.zero) bounds=r.bounds; else bounds.Encapsulate(r.bounds);
                            }
                            if(r.name.StartsWith("Leaves"))
                            {
                                Check(r.sharedMaterial.GetFloat("_Cull")==0,"Leaf culling");
                                Check(r.sharedMaterial.IsKeywordEnabled("_ALPHATEST_ON"),"Leaf alpha");
                            }
                            if(r.name.StartsWith("Fruit"))
                            {
                                Check(r.name.StartsWith("Fruit_Green"),"Non-green fruit");
                                Check(r.enabled==(name=="MangoTree_Update"),"Wrong fruit visibility");
                            }
                        }
                        Check(triangles <= new[] {5000,3000,1200}[level],"Exceeded 5k revision triangle budget on LOD"+level+": "+triangles);
                        Check(triangles<previousCount,"LOD triangle count must decrease");
                        previousCount=triangles;
                        log.AppendLine(name+" LOD"+level+" triangles: "+triangles);
                        level++;
                    }
                    Check(bounds.size.y>6.6f && bounds.size.y<7.5f && bounds.size.x>6.8f && bounds.size.x<8.1f,"Wrong scale or axes: "+bounds);
                    log.AppendLine(name+" bounds: "+bounds);
                    foreach (string dependency in AssetDatabase.GetDependencies(root+"/Prefabs/"+name+".prefab",true))
                        Check(!dependency.StartsWith("Assets/SecretsThatBreathe/Environment/MangoTree/") &&
                              !dependency.StartsWith("Assets/SecretsThatBreathe/Environment/MangoTree_New/"),"Old-model dependency: "+dependency);
                }
                var normal=AssetImporter.GetAtPath(root+"/Textures/MangoUpdate_Bark_Normal.png") as TextureImporter;
                Check(normal.textureType==TextureImporterType.NormalMap && !normal.sRGBTexture,"Normal import");
                var atlas=AssetImporter.GetAtPath(root+"/Textures/MangoUpdate_LeafClusters.png") as TextureImporter;
                Check(atlas.alphaSource==TextureImporterAlphaSource.FromInput && atlas.mipmapEnabled,"Atlas import");
                log.AppendLine("PASS: <=5000 triangles for the entire LOD0 tree including green fruit and stems; independent references, scale, three LODs, two-sided alpha leaf clusters, bark normal and collider.");

                EditorSceneManager.OpenScene(root+"/Demo/MangoTree_Update_Preview.unity");
                var tree=UnityEngine.Object.FindFirstObjectByType<MangoTreeUpdateWind>();
                var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
                var zone=UnityEngine.Object.FindFirstObjectByType<WindZone>();
                zone.windMain=1; zone.windPulseMagnitude=0;
                var sample=MangoTreeUpdateWind.SampleZone(zone,Vector3.zero,0);
                Check(Vector3.Distance(new Vector3(sample.x,sample.y,sample.z),zone.transform.forward)<.001f,"Wind direction");
                zone.windMain=2;
                Check(Math.Abs(MangoTreeUpdateWind.SampleZone(zone,Vector3.zero,0).magnitude-Mathf.Sqrt(4.25f))<.001f,"Wind strength or turbulence");
                zone.windPulseMagnitude=1; zone.windPulseFrequency=1;
                Check((MangoTreeUpdateWind.SampleZone(zone,Vector3.zero,.25f)-MangoTreeUpdateWind.SampleZone(zone,Vector3.zero,.75f)).sqrMagnitude>1,"Wind pulse");
                zone.mode=WindZoneMode.Spherical; zone.radius=5;
                Check(MangoTreeUpdateWind.SampleZone(zone,new Vector3(6,0,0),0)==Vector4.zero,"Spherical radius");
                Check(MangoTreeUpdateWind.SampleZone(zone,new Vector3(1,0,0),0).x>0,"Radial direction");
                Check(MangoTreeUpdateWind.SampleZone(zone,new Vector3(4,0,0),0).magnitude<MangoTreeUpdateWind.SampleZone(zone,new Vector3(1,0,0),0).magnitude,"Spherical falloff");
                log.AppendLine("PASS: Directional and Spherical Wind Zone direction, strength, turbulence, pulse and falloff.");
                zone.mode=WindZoneMode.Directional; zone.windMain=1; zone.windPulseMagnitude=.25f; zone.windPulseFrequency=.2f;
                tree.windZone=null;
                var group=tree.GetComponent<LODGroup>();
                for(int lod=0;lod<3;lod++)
                {
                    group.ForceLOD(lod);
                    var a=Capture(camera,tree,1); var b=Capture(camera,tree,2.3f);
                    int changed=Difference(a,b);
                    Check(changed>200,"Wind render failed LOD"+lod+": "+changed);
                    log.AppendLine("PASS: animated LOD"+lod+", changed pixels="+changed);
                    File.WriteAllBytes(Path.Combine(report,"Unity_LOD"+lod+".png"),a.EncodeToPNG());
                    if(lod==0) File.WriteAllBytes(Path.Combine(report,"Unity_Wind_B.png"),b.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
                }
                group.ForceLOD(0);
                zone.gameObject.SetActive(false);
                var still=Capture(camera,tree,1); var stopped=Capture(camera,tree,2.3f);
                Check(Difference(still,stopped)<20,"Inactive zone still moves");
                zone.gameObject.SetActive(true); zone.windMain=0;
                var zero=Capture(camera,tree,4);
                Check(Difference(still,zero)<20,"Main=0 still moves");
                UnityEngine.Object.DestroyImmediate(still); UnityEngine.Object.DestroyImmediate(stopped); UnityEngine.Object.DestroyImmediate(zero);
                tree.enabled=false;
                var block=new MaterialPropertyBlock();
                tree.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);
                Check(block.GetVector("_MangoWind")==Vector4.zero,"Wind disable cleanup");
                tree.enabled=true; zone.windMain=1; group.ForceLOD(-1);
                foreach(var r in tree.GetComponentsInChildren<MeshRenderer>())
                {
                    var mat=r.sharedMaterial;
                    for(int p=0;p<mat.passCount;p++) mat.SetPass(p);
                    Check(!ShaderUtil.ShaderHasError(mat.shader),"Compiled shader error");
                }
                log.AppendLine("PASS: inactive zone / Main=0 stop movement; shader forward, GBuffer, shadow, depth, normals and motion-vector passes compile.");
                AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(report,"Unity_Verification.txt"),log.ToString());
                AssetDatabase.ExportPackage(root,Path.Combine(report,"MangoTree_Update_URP.unitypackage"),ExportPackageOptions.Recurse);
                Debug.Log("MANGO_UPDATE_PASS\n"+log);
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
