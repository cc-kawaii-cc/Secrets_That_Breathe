using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class DoorStickerPrefabSetup
    {
        private const string Folder = "Assets/SecretsThatBreathe/Props/DoorStickers";

        static DoorStickerPrefabSetup() => EditorApplication.delayCall += CreatePrefabs;

        private static void CreatePrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Create("Push");
            Create("Pull");
            AssetDatabase.SaveAssets();
        }

        private static void Create(string name)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{name}.png");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (texture == null || shader == null) return;
            string materialPath = $"{Folder}/{name} Sticker.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = name + " Sticker" };
                material.SetTexture("_BaseMap", texture);
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", .5f);
                material.SetFloat("_Cull", 0f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                material.SetOverrideTag("RenderType", "TransparentCutout");
                AssetDatabase.CreateAsset(material, materialPath);
            }
            string prefabPath = $"{Folder}/{name} Sticker.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
            var sticker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            try
            {
                sticker.name = name + " Sticker";
                sticker.transform.localScale = new Vector3(.18f, .18f, 1f);
                Object.DestroyImmediate(sticker.GetComponent<Collider>());
                sticker.GetComponent<MeshRenderer>().sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(sticker, prefabPath);
            }
            finally { Object.DestroyImmediate(sticker); }
        }
    }
}
