#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BananaTree.Editor
{
    /// <summary>
    /// Tools > Banana Tree > 1. Setup  (also runs once, silently, after the package is imported)
    ///  - fixes the import settings of the textures and of both FBX files
    ///  - (re)creates the URP materials with the wind shader and hooks them into the FBX files
    ///  - builds BananaTree.prefab (green fruit) and BananaTree_Ripe.prefab (yellow fruit) with LODGroup
    /// The folder can be moved: everything is located relative to this script.
    /// </summary>
    public static class BananaTreeSetup
    {
        const string ShaderName = "Custom/Banana Tree (URP Wind)";
        const string AutoKeyVersion = "v3";

        static string Root
        {
            get
            {
                // .../BananaTree/Scripts/Editor/BananaTreeSetup.cs  ->  .../BananaTree
                foreach (var g in AssetDatabase.FindAssets("BananaTreeSetup t:MonoScript"))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (p.EndsWith("BananaTreeSetup.cs"))
                        return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(p))).Replace('\\', '/');
                }
                return "Assets/BananaTree";
            }
        }

        [InitializeOnLoadMethod]
        static void AutoSetupOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                string key = "BananaTree.AutoSetup." + AutoKeyVersion + "." + Application.dataPath;
                if (EditorPrefs.GetBool(key, false)) return;
                if (AssetDatabase.LoadAssetAtPath<Object>(Root + "/Models/BananaTree.fbx") == null) return;
                if (Shader.Find(ShaderName) == null) return;     // shader not imported yet: try again on the next reload
                EditorPrefs.SetBool(key, true);
                Setup(false);
            };
        }

        [MenuItem("Tools/Banana Tree/1. Setup (textures, materials, FBX, prefabs)")]
        public static void SetupMenu() => Setup(true);

        static void Setup(bool interactive)
        {
            string root = Root;
            string texDir = root + "/Textures";
            string matDir = root + "/Materials";

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                const string msg = "Shader '" + ShaderName + "' was not found or failed to compile. Check the Console for shader errors.";
                if (interactive) EditorUtility.DisplayDialog("Banana Tree", msg, "OK");
                else Debug.LogWarning("BananaTree: " + msg);
                return;
            }

            // ---- textures
            ImportTexture(texDir + "/banana_trunk_albedo.png", false, false);
            ImportTexture(texDir + "/banana_trunk_normal.png", true, false);
            ImportTexture(texDir + "/banana_leaf_albedo.png", false, true);
            ImportTexture(texDir + "/banana_fruit_albedo.png", false, false);
            ImportTexture(texDir + "/banana_fruit_ripe_albedo.png", false, false);

            // ---- materials (leaf = two sided + alpha clip)
            Directory.CreateDirectory(matDir);
            var trunk = MakeMaterial(matDir + "/BananaTrunk.mat", shader, texDir + "/banana_trunk_albedo.png",
                texDir + "/banana_trunk_normal.png", false, 0.18f, 2);
            var leaf = MakeMaterial(matDir + "/BananaLeaf.mat", shader, texDir + "/banana_leaf_albedo.png",
                null, true, 0.30f, 0);
            var fruit = MakeMaterial(matDir + "/BananaFruit.mat", shader, texDir + "/banana_fruit_albedo.png",
                null, false, 0.45f, 2);
            var ripe = MakeMaterial(matDir + "/BananaFruitRipe.mat", shader, texDir + "/banana_fruit_ripe_albedo.png",
                null, false, 0.50f, 2);

            // ---- the two models
            SetupModel(root, "BananaTree", trunk, leaf, fruit, "BananaFruit");
            SetupModel(root, "BananaTree_Ripe", trunk, leaf, ripe, "BananaFruitRipe");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BananaTree: setup finished (prefabs: " + root + "/BananaTree.prefab, " + root + "/BananaTree_Ripe.prefab)");
        }

        static void SetupModel(string root, string name, Material trunk, Material leaf, Material fruit, string fruitSlotName)
        {
            string fbxPath = root + "/Models/" + name + ".fbx";
            if (AssetDatabase.LoadAssetAtPath<Object>(fbxPath) == null)
            {
                Debug.LogWarning("BananaTree: FBX not found: " + fbxPath);
                return;
            }

            var mi = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (mi != null)
            {
                // the FBX itself uses our materials, so dropping the .fbx into a scene also works
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                mi.materialLocation = ModelImporterMaterialLocation.External;
                mi.materialName = ModelImporterMaterialName.BasedOnMaterialName;
                mi.materialSearch = ModelImporterMaterialSearch.RecursiveUp;
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "BananaTrunk"), trunk);
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "BananaLeaf"), leaf);
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), fruitSlotName), fruit);
                mi.importNormals = ModelImporterNormals.Import;
                mi.importTangents = ModelImporterTangents.CalculateMikk;
                mi.importBlendShapes = false;
                mi.importCameras = false;
                mi.importLights = false;
                mi.importAnimation = false;
                mi.animationType = ModelImporterAnimationType.None;
                mi.isReadable = false;
                mi.meshCompression = ModelImporterMeshCompression.Off;   // keeps the wind data in the vertex colours exact
                mi.generateSecondaryUV = false;
                mi.SaveAndReimport();
            }

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            inst.name = name;
            foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
            {
                var current = r.sharedMaterials;
                var arr = new Material[current.Length];
                for (int i = 0; i < arr.Length; i++)
                {
                    string n = current[i] != null ? current[i].name : "";
                    if (n.Contains("Leaf")) arr[i] = leaf;
                    else if (n.Contains("Fruit")) arr[i] = fruit;
                    else if (n.Contains("Trunk")) arr[i] = trunk;
                    else arr[i] = new[] { trunk, leaf, fruit }[Mathf.Min(i, 2)];     // fallback: slot order
                }
                r.sharedMaterials = arr;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
                r.lightProbeUsage = LightProbeUsage.BlendProbes;
                r.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            }

            var lodGroup = inst.GetComponent<LODGroup>();
            if (lodGroup == null) lodGroup = inst.GetComponentInChildren<LODGroup>();
            if (lodGroup != null)
            {
                var lods = lodGroup.GetLODs();
                float[] heights = { 0.30f, 0.10f, 0.03f };      // screen-relative height where each LOD ends
                for (int i = 0; i < lods.Length && i < heights.Length; i++)
                    lods[i].screenRelativeTransitionHeight = heights[i];
                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();
                lodGroup.fadeMode = LODFadeMode.None;
            }
            else
            {
                Debug.LogWarning("BananaTree: no LODGroup found on " + name + " (check the _LOD0/_LOD1/_LOD2 names).");
            }

            string prefabPath = root + "/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
            Object.DestroyImmediate(inst);
            if (name == "BananaTree") { Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab); }
        }

        [MenuItem("GameObject/Banana Tree/Wind Zone + Controller", false, 10)]
        public static void CreateWind()
        {
            var go = new GameObject("Wind Zone (Banana)");
            var zone = go.AddComponent<WindZone>();
            zone.mode = WindZoneMode.Directional;
            zone.windMain = 1f;
            zone.windTurbulence = 1f;
            zone.windPulseMagnitude = 0.5f;
            zone.windPulseFrequency = 0.01f;
            go.transform.rotation = Quaternion.Euler(0f, 60f, 0f);   // blows along the object's forward (blue) axis
            var ctrl = go.AddComponent<BananaWindController>();
            ctrl.windZone = zone;
            Undo.RegisterCreatedObjectUndo(go, "Create Banana Wind");
            Selection.activeGameObject = go;
        }

        // ------------------------------------------------------------------ helpers
        static void ImportTexture(string path, bool normalMap, bool cutout)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning("BananaTree: texture missing " + path); return; }
            ti.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !normalMap;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 4;
            ti.filterMode = FilterMode.Trilinear;
            ti.wrapMode = cutout ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            ti.maxTextureSize = 2048;
            if (cutout)
            {
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = true;
                ti.mipMapsPreserveCoverage = true;      // leaves do not thin out in the distance
                ti.alphaTestReferenceValue = 0.5f;
            }
            else if (!normalMap)
            {
                ti.alphaSource = TextureImporterAlphaSource.None;
            }
            ti.SaveAndReimport();
        }

        static Material MakeMaterial(string path, Shader shader, string albedo, string normal, bool alphaClip,
            float smoothness, int cull)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Cull", cull);                       // 0 = Off (two sided), 2 = Back
            m.SetFloat("_Cutoff", 0.5f);
            m.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
            if (alphaClip) m.EnableKeyword("_ALPHATEST_ON"); else m.DisableKeyword("_ALPHATEST_ON");
            if (!string.IsNullOrEmpty(normal))
            {
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                m.SetFloat("_UseNormalMap", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            else
            {
                m.SetFloat("_UseNormalMap", 0f);
                m.DisableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Translucency", alphaClip ? 0.12f : 0f);
            m.SetFloat("_WindOn", 1f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
#endif
