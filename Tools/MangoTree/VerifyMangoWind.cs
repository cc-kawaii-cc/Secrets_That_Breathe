using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using SecretsThatBreathe.MangoTree;
using SecretsThatBreathe.MangoTree.Editor;

public static class VerifyMangoWind
{
    public static void ApplyTextureGeometryRevision()
    {
        string root = MangoTreeAssetBuilder.Root;
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string name in new[] { "Mango_Bark_BaseColor", "Mango_Bark_Normal", "Mango_Leaves_BaseColor" })
        {
            var path = root + "/Textures/" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool normal = name.EndsWith("Normal");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
        foreach (var name in new[] { "MangoTree", "MangoTree_NoFruit" })
        {
            string path = root + "/Prefabs/" + name + ".prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                prefab.GetComponent<CapsuleCollider>().radius = .19975f;
                prefab.GetComponent<LODGroup>().RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        AssetDatabase.SaveAssets();
        Run();
    }
    public static void ExportPackage()
    {
        AssetDatabase.Refresh();
        AssetDatabase.ExportPackage(MangoTreeAssetBuilder.Root,
            Path.Combine(Environment.GetEnvironmentVariable("MANGO_REPORT_DIR"), "MangoTree_URP.unitypackage"), ExportPackageOptions.Recurse);
        EditorApplication.Exit(0);
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static Texture2D Capture(Camera camera, MangoTreeWind tree, float time)
    {
        tree.ApplyWind(time);
        var rt = new RenderTexture(768, 768, 24);
        rt.Create(); camera.targetTexture = rt; camera.Render();
        var old = RenderTexture.active; RenderTexture.active = rt;
        var image = new Texture2D(768, 768, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 768, 768), 0, 0); image.Apply();
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
    public static void Run()
    {
        try
        {
            string root = MangoTreeAssetBuilder.Root;
            string report = Environment.GetEnvironmentVariable("MANGO_REPORT_DIR");
            var log = new StringBuilder("Unity " + Application.unityVersion + " / URP 17.3.0 wind verification\n");
            var bark = AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/Mango_Bark.mat");
            var texture = bark.GetTexture("_BaseMap"); var tiling = bark.GetTextureScale("_BaseMap"); var colour = bark.GetColor("_BaseColor");
            MangoTreeWindSetup.Upgrade();
            Check(bark.GetTexture("_BaseMap")==texture && bark.GetTextureScale("_BaseMap")==tiling && bark.GetColor("_BaseColor")==colour, "Material edits were lost");
            log.AppendLine("PASS: custom texture, tiling and colour preserved by upgrade.");
            foreach (string name in new[]{"MangoTree", "MangoTree_NoFruit"})
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/"+name+".prefab");
                Check(prefab.GetComponent<MangoTreeWind>(), "Missing wind component");
                Check(prefab.GetComponent<LODGroup>().lodCount==3, "Missing LODs");
                Check(prefab.GetComponent<CapsuleCollider>(), "Missing collider");
                foreach (var lod in prefab.GetComponent<LODGroup>().GetLODs())
                {
                    long triangleCount=0;
                    foreach(var lodRenderer in lod.renderers)
                    {
                        var mesh=lodRenderer.GetComponent<MeshFilter>().sharedMesh;
                        for(int s=0;s<mesh.subMeshCount;s++) triangleCount+=(long)mesh.GetIndexCount(s)/3;
                    }
                    log.AppendLine(name+" LOD triangles: "+triangleCount);
                }
                foreach (var r in prefab.GetComponentsInChildren<MeshRenderer>())
                {
                    Check(r.sharedMaterial.shader.name=="Secrets That Breathe/Mango Wind Lit", "Wrong wind shader");
                    if (r.name.StartsWith("Fruit")) Check(r.enabled==(name=="MangoTree"), "Fruit visibility changed");
                }
            }
            EditorSceneManager.OpenScene(root+"/Demo/MangoTree_Preview.unity");
            var tree = UnityEngine.Object.FindFirstObjectByType<MangoTreeWind>();
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var zone = UnityEngine.Object.FindFirstObjectByType<WindZone>();
            if (!zone) zone = new GameObject("Mango Wind Zone").AddComponent<WindZone>();
            zone.mode=WindZoneMode.Directional; zone.windMain=1; zone.windTurbulence=.5f;
            zone.windPulseMagnitude=.25f; zone.windPulseFrequency=.2f;
            zone.transform.rotation=Quaternion.Euler(0,70,0);
            tree.windZone=zone;

            zone.windPulseMagnitude=0;
            var sample=MangoTreeWind.SampleZone(zone,Vector3.zero,0);
            Check(Vector3.Distance(new Vector3(sample.x,sample.y,sample.z),zone.transform.forward)<.001f,"Direction mismatch");
            zone.windMain=2;
            var strong=MangoTreeWind.SampleZone(zone,Vector3.zero,0);
            Check(Math.Abs(new Vector3(strong.x,strong.y,strong.z).magnitude-2)<.001f,"Main strength ignored");
            zone.windPulseMagnitude=1; zone.windPulseFrequency=1;
            Check((MangoTreeWind.SampleZone(zone,Vector3.zero,.25f)-MangoTreeWind.SampleZone(zone,Vector3.zero,.75f)).sqrMagnitude>1,"Pulse ignored");
            zone.mode=WindZoneMode.Spherical; zone.radius=5;
            Check(MangoTreeWind.SampleZone(zone,new Vector3(6,0,0),0)==Vector4.zero,"Outside spherical radius");
            Check(MangoTreeWind.SampleZone(zone,new Vector3(1,0,0),0).x>0,"Spherical direction");
            Check(MangoTreeWind.SampleZone(zone,new Vector3(4,0,0),0).magnitude<MangoTreeWind.SampleZone(zone,new Vector3(1,0,0),0).magnitude,"Spherical attenuation");
            log.AppendLine("PASS: direction, main strength, turbulence input, pulse and spherical attenuation.");

            zone.mode=WindZoneMode.Directional; zone.windMain=1; zone.windPulseMagnitude=.25f; zone.windPulseFrequency=.2f;
            tree.windZone=null; // Verify automatic discovery for an ordinary dropped prefab.
            zone.gameObject.SetActive(false);
            tree.GetComponent<LODGroup>().ForceLOD(0);
            var still=Capture(camera,tree,1);
            zone.gameObject.SetActive(true);
            for(int lod=0;lod<3;lod++)
            {
                tree.GetComponent<LODGroup>().ForceLOD(lod);
                var a=Capture(camera,tree,1); var b=Capture(camera,tree,2.3f);
                int changed=Difference(a,b);
                Check(changed>150,"Wind failed on LOD"+lod+": "+changed);
                log.AppendLine("PASS: animated LOD"+lod+", changed pixels="+changed);
                if(lod==0)
                {
                    File.WriteAllBytes(Path.Combine(report,"MangoTree_Wind_A.png"),a.EncodeToPNG());
                    File.WriteAllBytes(Path.Combine(report,"MangoTree_Wind_B.png"),b.EncodeToPNG());
                }
                UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
            }
            tree.GetComponent<LODGroup>().ForceLOD(0);
            zone.gameObject.SetActive(false);
            var stopped=Capture(camera,tree,2.3f);
            Check(Difference(still,stopped)<20,"Disabled zone still deforms tree");
            zone.gameObject.SetActive(true); zone.windMain=0;
            var zero=Capture(camera,tree,4);
            Check(Difference(still,zero)<20,"Main zero still deforms tree");
            tree.enabled=false;
            var block=new MaterialPropertyBlock(); var renderer=tree.GetComponentInChildren<MeshRenderer>(); renderer.GetPropertyBlock(block);
            Check(block.GetVector("_MangoWind")==Vector4.zero,"Disable cleanup failed");
            tree.enabled=true; zone.windMain=1;
            log.AppendLine("PASS: inactive zone, Main=0 and disabled component stop movement.");
            foreach (var r in tree.GetComponentsInChildren<MeshRenderer>())
            {
                var m=r.sharedMaterial;
                for(int p=0;p<m.passCount;p++) m.SetPass(p);
                Check(!ShaderUtil.ShaderHasError(m.shader),"Wind shader compile failure");
            }
            log.AppendLine("PASS: Lit forward/deferred, shadow, depth, depth normals and motion-vector passes compile.");
            tree.GetComponent<LODGroup>().ForceLOD(-1);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(report,"Wind_validation.txt"),log.ToString());
            AssetDatabase.ExportPackage(root,Path.Combine(report,"MangoTree_URP.unitypackage"),ExportPackageOptions.Recurse);
            Debug.Log("MANGO_WIND_PASS\n"+log);
            EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
