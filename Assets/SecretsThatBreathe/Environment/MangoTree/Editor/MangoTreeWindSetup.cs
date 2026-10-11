using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SecretsThatBreathe.MangoTree.Editor
{
    public static class MangoTreeWindSetup
    {
        [MenuItem("Tools/Mango Tree/Enable Wind on Existing Prefabs")]
        public static void Upgrade()
        {
            string root = MangoTreeAssetBuilder.Root;
            var shader = Shader.Find("Secrets That Breathe/Mango Wind Lit");
            if (!shader) throw new InvalidOperationException("Mango Wind Lit shader was not imported.");
            foreach (var name in new[] { "Mango_Bark", "Mango_Leaves", "Mango_Fruit" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(root + "/Materials/" + name + ".mat");
                // All Lit property names are retained, including custom textures, colours and tiling.
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
            foreach (var name in new[] { "MangoTree", "MangoTree_NoFruit" })
            {
                string path = root + "/Prefabs/" + name + ".prefab";
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (!prefab.GetComponent<MangoTreeWind>()) prefab.AddComponent<MangoTreeWind>();
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
