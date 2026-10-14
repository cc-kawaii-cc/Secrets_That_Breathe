using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class VerifyIndependent
{
    public static void Run()
    {
        const string folder = "Assets/SecretsThatBreathe/Props/MamaEmpty/";
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Prefabs/MEMEEE_Empty.prefab");
        if (!prefab || prefab.GetComponentsInChildren<MeshFilter>().Length != 3)
            throw new Exception("Missing prefab or meshes");
        foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>())
            if (!mf.sharedMesh) throw new Exception("Missing mesh");
        foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
            foreach (var mat in renderer.sharedMaterials)
            {
                if (!mat || !AssetDatabase.GetAssetPath(mat).StartsWith(folder + "Materials/"))
                    throw new Exception("Shared or missing material");
                foreach (var property in mat.GetTexturePropertyNames())
                {
                    var tex = mat.GetTexture(property);
                    if (tex && !AssetDatabase.GetAssetPath(tex).StartsWith(folder + "Textures/"))
                        throw new Exception("Shared texture");
                }
            }
        foreach (var dep in AssetDatabase.GetDependencies(folder + "Prefabs/MEMEEE_Empty.prefab", true))
            if (dep.StartsWith("Assets/GUY/")) throw new Exception("Original dependency: " + dep);
        File.WriteAllText("ArtSource/MamaEmpty/independence_verification.txt", "Unity loaded prefab with 3 valid meshes. All materials and textures are independent copies in Assets/SecretsThatBreathe/Props/MamaEmpty. No dependencies on Assets/GUY.");
        Debug.Log("MAMA_EMPTY_INDEPENDENCE_SUCCESS");
    }
}
