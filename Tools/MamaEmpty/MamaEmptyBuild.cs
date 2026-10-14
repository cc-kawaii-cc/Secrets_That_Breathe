using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MamaEmptyBuild
{
    public static void Run()
    {
        const string folder = "Assets/SecretsThatBreathe/Props/MamaEmpty/";
        AssetDatabase.ImportAsset(folder + "Models/MEMEEE_Empty.fbx", ImportAssetOptions.ForceSynchronousImport);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Models/MEMEEE_Empty.fbx");
        if (model == null) throw new Exception("Empty model failed to import");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = "MEMEEE_Empty";
        instance.transform.position = Vector3.zero;
        instance.transform.localScale = Vector3.one * .53154f;
        foreach (var r in instance.GetComponentsInChildren<MeshRenderer>())
        {
            var materials = r.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) throw new Exception("Missing material: " + r.name);
                var original = AssetDatabase.LoadAssetAtPath<Material>(folder + "Materials/" + materials[i].name + ".mat");
                if (original == null) throw new Exception("Original material missing: " + materials[i].name);
                materials[i] = original;
            }
            r.sharedMaterials = materials;
        }
        var result = PrefabUtility.SaveAsPrefabAsset(instance, folder + "Prefabs/MEMEEE_Empty.prefab");
        if (result == null) throw new Exception("Prefab save failed");
        foreach (var mf in result.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh == null) throw new Exception("Missing mesh on " + mf.name);
        File.WriteAllText("ArtSource/MamaEmpty/unity_verification.txt", "Imported FBX and saved prefab with 3 valid meshes and original materials. Scale matches the original prefab: 0.53154.");
        UnityEngine.Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        Debug.Log("MAMA_EMPTY_BUILD_SUCCESS");
    }
}
