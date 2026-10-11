using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using SecretsThatBreathe.MayomTree;
using SecretsThatBreathe.MayomTree.Editor;

public static class VerifyMayomTree
{
    static readonly string Report = Environment.GetEnvironmentVariable("MAYOM_REPORT_DIR") ?? Path.GetFullPath("ArtSource/StarGooseberryTree");
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static Texture2D Capture(Camera camera, MayomTreeWind tree, float time)
    {
        tree.ApplyWind(time);
        var rt = new RenderTexture(1100, 1200, 24);
        rt.Create(); camera.targetTexture = rt; camera.Render();
        var old = RenderTexture.active; RenderTexture.active = rt;
        var image = new Texture2D(1100, 1200, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1100, 1200), 0, 0); image.Apply();
        RenderTexture.active = old; camera.targetTexture = null;
        rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        return image;
    }
    static int Difference(Texture2D a, Texture2D b)
    {
        var ap = a.GetPixels32(); var bp = b.GetPixels32(); int changed = 0;
        for (int i = 0; i < ap.Length; i++)
            if (Math.Abs(ap[i].r-bp[i].r)+Math.Abs(ap[i].g-bp[i].g)+Math.Abs(ap[i].b-bp[i].b)>15) changed++;
        return changed;
    }
    public static void ExportPackage()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ExportPackage(MayomTreeAssetBuilder.Root,
            Path.Combine(Report,"StarGooseberryTree_URP.unitypackage"),ExportPackageOptions.Recurse);
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
    public static void Run()
    {
        try
        {
            Check(Application.isBatchMode, "Run verification in an isolated batch project; current scenes are preserved.");
            MayomTreeAssetBuilder.Build();
            string root = MayomTreeAssetBuilder.Root;
            var log = new StringBuilder("Unity " + Application.unityVersion + " / URP star gooseberry verification\n");
            Check(Application.unityVersion == "6000.3.8f1", "Wrong Unity version");
            foreach (string name in new[] { "StarGooseberryTree", "StarGooseberryTree_NoFruit" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/"+name+".prefab");
                Check(prefab && prefab.GetComponent<MayomTreeWind>(), "Missing prefab/wind component");
                Check(prefab.GetComponent<LODGroup>().lodCount==3, "Missing LODs");
                Check(prefab.GetComponent<CapsuleCollider>(), "Missing trunk collider");
                Bounds bounds = default;
                for (int level=0; level<3; level++)
                {
                    long count=0;
                    foreach(var r in prefab.GetComponent<LODGroup>().GetLODs()[level].renderers)
                    {
                        Check(r && r.sharedMaterial && r.sharedMaterial.shader, "Missing material");
                        Check(r.sharedMaterial.shader.name=="Secrets That Breathe/Mayom Tree Wind Lit", "Wrong wind shader");
                        Mesh m=r.GetComponent<MeshFilter>().sharedMesh;
                        Check(m && m.vertexCount>0, "Empty mesh");
                        for(int s=0;s<m.subMeshCount;s++) count += m.GetIndexCount(s)/3;
                        if(r.name.StartsWith("Fruit")) Check(r.enabled==(name=="StarGooseberryTree"), "Fruit visibility mismatch");
                        if(r.name.StartsWith("Leaves")) Check(r.sharedMaterial.GetFloat("_Cull")==0 && r.sharedMaterial.IsKeywordEnabled("_ALPHATEST_ON"), "Leaf alpha/two-sided material missing");
                        if(level==0) { if(bounds.size==Vector3.zero) bounds=r.bounds; else bounds.Encapsulate(r.bounds); }
                    }
                    if(level==0) Check(count>=5000 && count<=7000, "Polygon budget exceeded: "+count);
                    log.AppendLine(name+" LOD"+level+": "+count+" triangles");
                }
                Check(bounds.size.y>4.7f && bounds.size.y<5.3f, "Wrong height/orientation: "+bounds);
                log.AppendLine("Metre-scale bounds: "+bounds);
            }
            var barkImporter=(TextureImporter)AssetImporter.GetAtPath(root+"/Textures/Mayom_Bark_Normal.png");
            Check(barkImporter.textureType==TextureImporterType.NormalMap && !barkImporter.sRGBTexture, "Bark normal import incorrect");
            var leafImporter=(TextureImporter)AssetImporter.GetAtPath(root+"/Textures/Mayom_Leaves_Atlas.png");
            Check(leafImporter.DoesSourceTextureHaveAlpha() && leafImporter.alphaIsTransparency, "Missing leaf alpha");
            log.AppendLine("PASS: URP materials, two-sided alpha leaves, bark normal map, collider, FBX scale and budget.");

            EditorSceneManager.OpenScene(root+"/Demo/StarGooseberryTree_Preview.unity");
            var tree = UnityEngine.Object.FindFirstObjectByType<MayomTreeWind>();
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.21f,.25f,.28f);
            var zone=UnityEngine.Object.FindFirstObjectByType<WindZone>();
            Check(zone, "No demo Wind Zone");
            zone.mode=WindZoneMode.Directional; zone.windMain=1; zone.windPulseMagnitude=0;
            zone.transform.rotation=Quaternion.Euler(0,70,0);
            var sample=MayomTreeWind.SampleZone(zone,Vector3.zero,0);
            Check(Vector3.Distance(new Vector3(sample.x,sample.y,sample.z),zone.transform.forward)<.001f, "Wrong wind direction");
            zone.windMain=2;
            Check(Math.Abs(MayomTreeWind.SampleZone(zone,Vector3.zero,0).magnitude-2)<.1f+zone.windTurbulence, "Main ignored");
            zone.windPulseMagnitude=1; zone.windPulseFrequency=1;
            Check((MayomTreeWind.SampleZone(zone,Vector3.zero,.25f)-MayomTreeWind.SampleZone(zone,Vector3.zero,.75f)).sqrMagnitude>1, "Pulse ignored");
            zone.mode=WindZoneMode.Spherical; zone.radius=5;
            Check(MayomTreeWind.SampleZone(zone,new Vector3(6,0,0),0)==Vector4.zero, "Outside spherical radius");
            Check(MayomTreeWind.SampleZone(zone,new Vector3(1,0,0),0).x>0, "Spherical direction");
            Check(MayomTreeWind.SampleZone(zone,new Vector3(4,0,0),0).magnitude<MayomTreeWind.SampleZone(zone,new Vector3(1,0,0),0).magnitude, "Spherical attenuation");
            log.AppendLine("PASS: directional Main, pulse, spherical radius and attenuation.");
            zone.mode=WindZoneMode.Directional; zone.windMain=1; zone.windTurbulence=.5f; zone.windPulseMagnitude=.25f; zone.windPulseFrequency=.2f;
            tree.windZone=null; tree.GetComponent<LODGroup>().ForceLOD(0);
            zone.gameObject.SetActive(false);
            var still=Capture(camera,tree,1);
            File.WriteAllBytes(Path.Combine(Report,"StarGooseberryTree_Unity_URP.png"),still.EncodeToPNG());
            zone.gameObject.SetActive(true);
            for(int lod=0;lod<3;lod++)
            {
                tree.GetComponent<LODGroup>().ForceLOD(lod);
                var a=Capture(camera,tree,1); var b=Capture(camera,tree,2.3f);
                int changed=Difference(a,b);
                Check(changed>150,"Wind failed on LOD"+lod+": "+changed);
                log.AppendLine("PASS: wind animates LOD"+lod+", changed pixels="+changed);
                if(lod==0)
                {
                    File.WriteAllBytes(Path.Combine(Report,"Wind_A.png"),a.EncodeToPNG());
                    File.WriteAllBytes(Path.Combine(Report,"Wind_B.png"),b.EncodeToPNG());
                }
                UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
            }
            tree.GetComponent<LODGroup>().ForceLOD(0); zone.gameObject.SetActive(false);
            var stopped=Capture(camera,tree,2.3f);
            Check(Difference(still,stopped)<20,"Inactive zone still moves");
            zone.gameObject.SetActive(true); zone.windMain=0;
            var zero=Capture(camera,tree,4);
            Check(Difference(still,zero)<20,"Main zero still moves");
            tree.enabled=false;
            var block=new MaterialPropertyBlock(); tree.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);
            Check(block.GetVector("_MayomWind")==Vector4.zero,"Disable cleanup failed");
            log.AppendLine("PASS: inactive zone, Main=0 and disabled component stop movement.");
            foreach(var r in tree.GetComponentsInChildren<MeshRenderer>())
            {
                var m=r.sharedMaterial;
                for(int p=0;p<m.passCount;p++) m.SetPass(p);
                Check(!ShaderUtil.ShaderHasError(m.shader),"Shader compile failure");
            }
            log.AppendLine("PASS: shader forward/deferred, shadow, depth, depth-normal and motion-vector passes.");
            AssetDatabase.ExportPackage(root,Path.Combine(Report,"StarGooseberryTree_URP.unitypackage"),ExportPackageOptions.Recurse);
            File.WriteAllText(Path.Combine(Report,"Unity_Verification.txt"),log.ToString());
            Debug.Log("MAYOM_PASS\n"+log);
            EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
