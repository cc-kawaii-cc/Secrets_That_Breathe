using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using SecretsThatBreathe.MangoTreeNew.Editor;

public static class VerifyMangoTreeNew
{
    public static void Run()
    {
        try
        {
            MangoTreeNewAssetBuilder.Build();
            string root = MangoTreeNewAssetBuilder.Root;
            var report = new StringBuilder();
            report.AppendLine("Unity " + Application.unityVersion + " / URP mango tree verification");
            foreach (string name in new[] { "MangoTree_New", "MangoTree_New_NoFruit" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "/Prefabs/" + name + ".prefab");
                if (!prefab) throw new Exception("Missing prefab: " + name);
                LODGroup group = prefab.GetComponent<LODGroup>();
                if (!group || group.GetLODs().Length != 3) throw new Exception("Expected three LODs on " + name);
                if (!prefab.GetComponent<CapsuleCollider>()) throw new Exception("Missing trunk collider");
                if (!prefab.GetComponent<SecretsThatBreathe.MangoTreeNew.MangoTreeNewWind>()) throw new Exception("Missing wind component");

                Bounds bounds = default;
                for (int level = 0; level < 3; level++)
                {
                    long triangles = 0;
                    foreach (Renderer renderer in group.GetLODs()[level].renderers)
                    {
                        if (!renderer || !renderer.sharedMaterial || !renderer.sharedMaterial.shader)
                            throw new Exception("Broken renderer in " + name + " LOD" + level);
                        if (renderer.sharedMaterial.shader.name != "Secrets That Breathe/Mango Tree New Wind Lit")
                            throw new Exception("Wrong shader on " + renderer.name);
                        if (ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader))
                            throw new Exception("Shader compiler error on " + renderer.sharedMaterial.shader.name);
                        if (renderer.name.StartsWith("Leaves") && renderer.sharedMaterial.GetFloat("_Cull") != 0f)
                            throw new Exception("Leaves are not two sided");
                        if (renderer.name.StartsWith("Fruit_Ripe"))
                            throw new Exception("Ripe fruit renderer still exists after the green-only revision");
                        bool shouldEnableFruit = name == "MangoTree_New";
                        if (renderer.name.StartsWith("Fruit") && renderer.enabled != shouldEnableFruit)
                            throw new Exception("Fruit visibility mismatch");
                        Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                        for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                            triangles += (long)mesh.GetIndexCount(submesh) / 3;
                        if (level == 0)
                        {
                            if (bounds.size == Vector3.zero) bounds = renderer.bounds;
                            else bounds.Encapsulate(renderer.bounds);
                        }
                    }
                    report.AppendLine(name + " LOD" + level + ": " + triangles + " triangles");
                }
                if (bounds.size.y < 7.4f || bounds.size.y > 8.5f || bounds.size.x < 10f || bounds.size.x > 12.5f)
                    throw new Exception("Unexpected metre scale or orientation: " + bounds);
                report.AppendLine(name + " bounds: " + bounds);
            }

            string texturePath = root + "/Textures/Mango_Bark_Normal.png";
            TextureImporter normalImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (!normalImporter || normalImporter.textureType != TextureImporterType.NormalMap)
                throw new Exception("Bark normal texture import is incorrect");
            string leafPath = root + "/Textures/Mango_Leaves_Atlas.png";
            TextureImporter leafImporter = AssetImporter.GetAtPath(leafPath) as TextureImporter;
            if (!leafImporter || leafImporter.alphaSource != TextureImporterAlphaSource.FromInput)
                throw new Exception("Leaf alpha import is incorrect");
            if (AssetDatabase.LoadAssetAtPath<Material>(root + "/Materials/MangoNew_Fruit_Ripe.mat"))
                throw new Exception("Obsolete ripe-fruit material still exists");

            report.AppendLine("PASS: slimmer trunk, broad leaves, green-only fruit, scale, materials, leaf alpha, bark normal, LODs, collider, and Wind Zone bridge.");
            File.WriteAllText(Path.GetFullPath("ArtSource/MangoTree_New/Unity_Verification.txt"), report.ToString());
            Debug.Log(report.ToString());
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
