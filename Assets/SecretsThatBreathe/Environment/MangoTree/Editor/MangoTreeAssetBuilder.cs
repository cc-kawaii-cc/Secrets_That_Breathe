using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.MangoTree.Editor
{
    /// <summary>Rebuilds only the assets in this tree's folder.</summary>
    public static class MangoTreeAssetBuilder
    {
        public const string Root = "Assets/SecretsThatBreathe/Environment/MangoTree";

        [MenuItem("Tools/Mango Tree/Rebuild URP Prefabs")]
        public static void Build()
        {
            var shader = Shader.Find("Secrets That Breathe/Mango Wind Lit");
            if (!shader) throw new InvalidOperationException("URP Lit shader is required.");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            var bark = MakeMaterial(shader, "Mango_Bark", "Mango_Bark_BaseColor", .12f, false);
            var leaves = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/MangoNew_Leaves.mat");
            if (!leaves) throw new InvalidOperationException("MangoNew_Leaves material copy is missing.");
            var fruit = MakeMaterial(shader, "Mango_Fruit", "Mango_Fruit_BaseColor", .32f, false);
            var normalPath = Root + "/Textures/Mango_Bark_Normal.png";
            var normalImporter = (TextureImporter)AssetImporter.GetAtPath(normalPath);
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.sRGBTexture = false;
            normalImporter.SaveAndReimport();
            bark.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            bark.SetFloat("_BumpScale", .6f);
            bark.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(bark);
            var materials = new[] { bark, leaves, fruit };
            for (int lod = 0; lod < 3; lod++)
            {
                string path = Root + "/Models/MangoTree_LOD" + lod + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.globalScale = 1;
                importer.useFileScale = true;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importAnimation = false;
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Mango_Bark"), bark);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Mango_Leaves"), leaves);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Mango_Fruit"), fruit);
                importer.SaveAndReimport();
            }

            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("MangoTree");
            SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var group = root.AddComponent<LODGroup>();
                var lods = new LOD[3];
                float[] transitions = { .42f, .19f, .035f };
                for (int i = 0; i < 3; i++)
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/MangoTree_LOD" + i + ".fbx");
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                    instance.name = "LOD" + i;
                    instance.transform.SetParent(root.transform, false);
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.sharedMaterial = renderer.name.StartsWith("Bark") ? bark : renderer.name.StartsWith("Leaves") ? leaves : fruit;
                        renderer.shadowCastingMode = renderer.name.StartsWith("Leaves") ? ShadowCastingMode.TwoSided : ShadowCastingMode.On;
                        renderer.receiveShadows = true;
                        renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                        renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                    }
                    lods[i] = new LOD(transitions[i], renderers);
                }
                group.SetLODs(lods);
                group.fadeMode = LODFadeMode.None;
                group.RecalculateBounds();
                var collider = root.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0, .86f, 0);
                collider.radius = .19975f;
                collider.height = 1.78f;
                collider.direction = 1;
                root.AddComponent<MangoTreeWind>();
                PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/MangoTree.prefab");
                root.name = "MangoTree_NoFruit";
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.name.StartsWith("Fruit")) renderer.enabled = false;
                PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/MangoTree_NoFruit.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Mango tree URP prefabs built successfully: " + Root + "/Prefabs");
        }

        private static Material MakeMaterial(Shader shader, string name, string textureName, float smoothness, bool twoSided)
        {
            var texPath = Root + "/Textures/" + textureName + ".png";
            var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.mipmapEnabled = true;
            ti.alphaSource = TextureImporterAlphaSource.None;
            ti.wrapMode = twoSided ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            ti.anisoLevel = 4;
            ti.maxTextureSize = 2048;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.SaveAndReimport();
            string path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Surface", 0);
            mat.SetFloat("_AlphaClip", 0);
            mat.SetFloat("_Cull", twoSided ? 0 : 2);
            mat.SetFloat("_ZWrite", 1);
            mat.doubleSidedGI = twoSided;
            mat.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
