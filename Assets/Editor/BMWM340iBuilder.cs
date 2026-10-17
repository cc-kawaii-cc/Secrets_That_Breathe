using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BMWM340iBuilder
{
    const string Root = "Assets/SecretsThatBreathe/Props/BMWM340i";
    [MenuItem("Tools/Secrets That Breathe/Build BMW M340i")]
    public static void Build()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(Root + "/Materials");
            var path = Root + "/Models/BMW_M340i_LowPoly.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation = false;
            importer.generateSecondaryUV = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
            go.name = "BMW_M340i_LowPoly";
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; ++i)
                {
                    var original = materials[i];
                    string name = original.name;
                    string target = Root + "/Materials/" + name + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(target);
                    if (!material)
                    {
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        material.name = name;
                        Color color = original.HasProperty("_Color") ? original.color : Color.gray;
                        material.SetColor("_BaseColor", color);
                        float metallic = name == "SilverPaint" ? .72f : name == "MachinedAlloy" ? .9f : name == "BrakeDisc" ? .8f : name == "TintedGlass" ? .48f : .12f;
                        material.SetFloat("_Metallic", metallic);
                        material.SetFloat("_Smoothness", name == "Tire" ? .17f : name == "TintedGlass" ? .82f : .65f);
                        if (name.StartsWith("LED"))
                        {
                            material.EnableKeyword("_EMISSION");
                            material.SetColor("_EmissionColor", color * 1.5f);
                        }
                        AssetDatabase.CreateAsset(material, target);
                    }
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            if (bounds.size.z < 4.5f || bounds.size.z > 5.1f || bounds.size.y < 1.3f || bounds.size.y > 1.7f)
                throw new Exception("Unexpected imported size: " + bounds.size);
            var chassis = go.AddComponent<BoxCollider>();
            chassis.center = new Vector3(0, .62f, 0); chassis.size = new Vector3(1.8f, .82f, 4.7f);
            var cabin = go.AddComponent<BoxCollider>();
            cabin.center = new Vector3(0, 1.18f, -.25f); cabin.size = new Vector3(1.48f, .46f, 2.12f);
            int triangles = go.GetComponentsInChildren<MeshFilter>().Sum(m => m.sharedMesh.triangles.Length / 3);
            var wheels = go.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Wheel_")).ToArray();
            if (wheels.Length != 4 || triangles > 16000) throw new Exception("Invalid wheel count or triangle budget");
            foreach (var wheel in wheels)
                wheel.name = "Wheel_" + (wheel.position.z > 0 ? "Front_" : "Rear_") + (wheel.position.x < 0 ? "L" : "R");
            PrefabUtility.SaveAsPrefabAsset(go, Root + "/BMW_M340i_LowPoly.prefab");
            string wheelInfo = string.Join("\n", wheels.Select(w => w.name + " pivot=" + w.position.ToString("F3")));
            File.WriteAllText(Root + "/Verification.txt", "PASS\nTriangles: " + triangles + "\nSize (metres): " + bounds.size.ToString("F3") + "\nFloor: " + bounds.min.y + "\nFour separate wheel pivots; URP materials; lightmap UV2; two box colliders.\n" + wheelInfo);
            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            Debug.Log("BMW_M340I_BUILD_PASS triangles=" + triangles);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }
}
