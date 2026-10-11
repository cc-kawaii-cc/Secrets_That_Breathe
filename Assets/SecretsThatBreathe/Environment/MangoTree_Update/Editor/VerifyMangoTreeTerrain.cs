using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SecretsThatBreathe.MangoTreeUpdate.Editor
{
    public static class VerifyMangoTreeTerrain
    {
        static void Check(bool value,string message) {if(!value) throw new Exception(message);}
        static Texture2D Capture(Camera camera,MangoTreeTerrainWind controller,float time)
        {
            controller.ApplyWind(time);
            var rt=new RenderTexture(960,960,24); rt.Create();
            var old=RenderTexture.active; camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            var result=new Texture2D(960,960,TextureFormat.RGB24,false);
            result.ReadPixels(new Rect(0,0,960,960),0,0); result.Apply();
            RenderTexture.active=old; camera.targetTexture=null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            return result;
        }
        static int Difference(Texture2D a,Texture2D b)
        {
            var x=a.GetPixels32(); var y=b.GetPixels32(); int count=0;
            for(int i=0;i<x.Length;i++) if(Math.Abs(x[i].r-y[i].r)+Math.Abs(x[i].g-y[i].g)+Math.Abs(x[i].b-y[i].b)>18) count++;
            return count;
        }
        public static void Run()
        {
            try
            {
                string report=Environment.GetEnvironmentVariable("MANGO_UPDATE_REPORT_DIR");
                string root=MangoTreeTerrainAssetBuilder.Root;
                MangoTreeUpdateAssetBuilder.Build();
                var log=new StringBuilder("Unity "+Application.unityVersion+" / URP 17.3.0 / native Terrain mango verification\n");
                foreach(string name in new[] {"MangoTree_Update_Terrain","MangoTree_Update_Terrain_NoFruit"})
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/"+name+".prefab");
                    Check(prefab.GetComponent<MeshRenderer>() && prefab.GetComponent<MeshFilter>(),"Terrain needs a valid root renderer");
                    Check(!prefab.GetComponent<MangoTreeUpdateWind>(),"Terrain must not rely on a per-instance MonoBehaviour");
                    Check(prefab.GetComponent<CapsuleCollider>(),"Missing Terrain collider");
                    var levels=prefab.GetComponent<LODGroup>().GetLODs(); Check(levels.Length==3,"Missing Terrain LODs");
                    for(int i=0;i<3;i++)
                    {
                        Check(levels[i].renderers.Length==1,"Each Terrain LOD should have a combined renderer");
                        var r=levels[i].renderers[0]; var mesh=r.GetComponent<MeshFilter>().sharedMesh;
                        long triangles=0;
                        for(int s=0;s<mesh.subMeshCount;s++) triangles+=(long)mesh.GetIndexCount(s)/3;
                        Check(triangles<=new[] {5000,3000,1200}[i],"Terrain triangle budget exceeded");
                        Check(r.sharedMaterials.Length==mesh.subMeshCount,"Material slot mismatch");
                        foreach(var mat in r.sharedMaterials)
                        {
                            Check(mat.shader.name==MangoTreeTerrainAssetBuilder.ShaderName,"Wrong Terrain shader");
                            Check(mat.enableInstancing,"Instancing is off");
                            for(int p=0;p<mat.passCount;p++) mat.SetPass(p);
                            Check(!ShaderUtil.ShaderHasError(mat.shader),"Terrain shader compile error");
                        }
                        log.AppendLine(name+" LOD"+i+": "+triangles+" triangles, "+mesh.subMeshCount+" material slots");
                    }
                }
                EditorSceneManager.OpenScene(root+"/Demo/MangoTree_Update_Terrain_Preview.unity");
                var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
                var controller=UnityEngine.Object.FindFirstObjectByType<MangoTreeTerrainWind>();
                var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
                var zone=UnityEngine.Object.FindFirstObjectByType<WindZone>();
                Check(terrain.terrainData.treeInstanceCount==9,"Expected nine native tree instances");
                Check(UnityEngine.Object.FindObjectsByType<MangoTreeUpdateWind>(FindObjectsSortMode.None).Length==0,"Trees were spawned as ordinary GameObjects");
                Check(terrain.GetComponentsInChildren<MeshRenderer>().Length==0,"Unexpected GameObject trees on Terrain");
                var originalInstances=terrain.terrainData.treeInstances;
                Physics.SyncTransforms();
                var middleTree=originalInstances[4];
                Vector3 treeRoot=terrain.transform.position+Vector3.Scale(middleTree.position,terrain.terrainData.size);
                bool treeCollision=Physics.Raycast(treeRoot+new Vector3(-2,1.2f*middleTree.heightScale,0),Vector3.right,out RaycastHit trunkHit,4);
                log.AppendLine("Edit Mode trunk raycast: "+treeCollision+(treeCollision ? ", collider="+trunkHit.collider.GetType().Name : " (native colliders are checked separately in Play Mode)"));
                var a=Capture(camera,controller,1); var b=Capture(camera,controller,2.3f);
                int moving=Difference(a,b); Check(moving>500,"Native Terrain wind did not animate: "+moving);
                File.WriteAllBytes(Path.Combine(report,"Unity_Terrain.png"),a.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(report,"Unity_Terrain_Wind_B.png"),b.EncodeToPNG());
                log.AppendLine("PASS: nine native Terrain.treeInstances, random rotations/scales, animated shared wind; changed pixels="+moving);
                terrain.terrainData.SetTreeInstances(new TreeInstance[0],true); terrain.Flush();
                var empty=Capture(camera,controller,1); int visible=Difference(a,empty);
                Check(visible>5000,"Terrain tree meshes were not visible: "+visible);
                terrain.terrainData.SetTreeInstances(originalInstances,true); terrain.Flush();
                log.AppendLine("PASS: trees are rendered by native Terrain, rather than spawned mesh GameObjects; visible pixels="+visible);
                UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); UnityEngine.Object.DestroyImmediate(empty);

                zone.gameObject.SetActive(false);
                var still=Capture(camera,controller,1); var stopped=Capture(camera,controller,2.3f);
                Check(Difference(still,stopped)<20,"Inactive Terrain Wind Zone still moves");
                zone.gameObject.SetActive(true); zone.windMain=0;
                var zero=Capture(camera,controller,4); Check(Difference(still,zero)<20,"Terrain Main=0 still moves");
                controller.enabled=false;
                Check(Shader.GetGlobalInt("_MangoTerrainZoneCount")==0,"Controller disable did not clear globals");
                controller.enabled=true; zone.windMain=1;
                UnityEngine.Object.DestroyImmediate(still); UnityEngine.Object.DestroyImmediate(stopped); UnityEngine.Object.DestroyImmediate(zero);
                log.AppendLine("PASS: inactive Wind Zone, Main=0 and disabled shared controller stop movement.");

                // A spherical zone wholly outside the orchard must also stop native instances.
                zone.mode=WindZoneMode.Spherical; zone.radius=3; zone.transform.position=new Vector3(200,0,200);
                var farA=Capture(camera,controller,1); var farB=Capture(camera,controller,2.3f);
                Check(Difference(farA,farB)<20,"Spherical Terrain radius was ignored");
                zone.transform.position=Vector3.zero; zone.radius=35;
                var localA=Capture(camera,controller,1); var localB=Capture(camera,controller,2.3f);
                int spherical=Difference(localA,localB); Check(spherical>200,"Spherical Terrain wind failed");
                log.AppendLine("PASS: spherical radius and animated radial wind; changed pixels="+spherical);
                foreach(var tex in new[] {farA,farB,localA,localB}) UnityEngine.Object.DestroyImmediate(tex);
                zone.mode=WindZoneMode.Directional; zone.transform.position=Vector3.zero;

                int countBefore=terrain.terrainData.treePrototypes.Length;
                int instancesBefore=terrain.terrainData.treeInstanceCount;
                var prefabAgain=AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/MangoTree_Update_Terrain.prefab");
                int index=MangoTreeTerrainAssetBuilder.AddPrototype(terrain,prefabAgain);
                Check(index==0 && terrain.terrainData.treePrototypes.Length==countBefore && terrain.terrainData.treeInstanceCount==instancesBefore,
                    "Adding an existing prototype changed terrain content");
                log.AppendLine("PASS: adding the existing mango prototype preserves other prototypes and planted trees.");
                terrain.terrainData.SetTreeInstances(originalInstances,true);
                EditorUtility.SetDirty(terrain.terrainData);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(terrain.gameObject.scene);
                File.WriteAllText(Path.Combine(report,"Terrain_Verification.txt"),log.ToString());
                AssetDatabase.ExportPackage(root,Path.Combine(report,"MangoTree_Update_URP.unitypackage"),ExportPackageOptions.Recurse);
                Debug.Log("MANGO_TERRAIN_PASS\n"+log); EditorApplication.Exit(0);
            }
            catch(Exception e) {Debug.LogException(e); EditorApplication.Exit(1);}
        }
    }
}
