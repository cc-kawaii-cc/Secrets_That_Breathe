using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.MangoTreeNew.Editor
{
    /// <summary>Builds only the independent MangoTree_New materials, prefabs, and demo scene.</summary>
    public static class MangoTreeNewAssetBuilder
    {
        public const string Root = "Assets/SecretsThatBreathe/Environment/MangoTree_New";

        [MenuItem("Tools/Mango Tree New/Rebuild URP Assets")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Demo");
            AssetDatabase.Refresh();

            Shader shader = Shader.Find("Secrets That Breathe/Mango Tree New Wind Lit");
            if (!shader)
                throw new InvalidOperationException("Mango Tree New wind shader did not import.");

            Texture2D barkColor = ConfigureTexture("Mango_Bark_BaseColor.png", false, false, 2048);
            Texture2D barkNormal = ConfigureTexture("Mango_Bark_Normal.png", true, false, 2048);
            Texture2D leafAtlas = ConfigureTexture("Mango_Leaves_Atlas.png", false, true, 2048);

            Material bark = MakeMaterial("MangoNew_Bark", shader, new Color(0.92f, 0.92f, 0.92f), 0.18f, false);
            bark.SetTexture("_BaseMap", barkColor);
            bark.SetTexture("_BumpMap", barkNormal);
            bark.SetFloat("_BumpScale", 0.78f);
            bark.EnableKeyword("_NORMALMAP");

            Material leaves = MakeMaterial("MangoNew_Leaves", shader, Color.white, 0.38f, true);
            leaves.SetTexture("_BaseMap", leafAtlas);
            leaves.SetFloat("_Cutoff", 0.34f);
            leaves.SetFloat("_AlphaClip", 1f);
            leaves.EnableKeyword("_ALPHATEST_ON");
            leaves.renderQueue = (int)RenderQueue.AlphaTest;
            leaves.SetOverrideTag("RenderType", "TransparentCutout");

            Material greenFruit = MakeMaterial("MangoNew_Fruit_Green", shader, new Color(0.44f, 0.68f, 0.10f), 0.44f, false);
            string obsoleteRipeMaterial = Root + "/Materials/MangoNew_Fruit_Ripe.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(obsoleteRipeMaterial))
                AssetDatabase.DeleteAsset(obsoleteRipeMaterial);
            Material[] materials = { bark, leaves, greenFruit };
            for (int lod = 0; lod < 3; lod++)
            {
                string modelPath = Root + "/Models/MangoTree_New_LOD" + lod + ".fbx";
                ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (!importer)
                    throw new InvalidOperationException("Missing FBX: " + modelPath);
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importAnimation = false;
                importer.isReadable = false;
                importer.meshCompression = lod == 0 ? ModelImporterMeshCompression.Off : ModelImporterMeshCompression.Low;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                foreach (var remap in importer.GetExternalObjectMap())
                    importer.RemoveRemap(remap.Key);
                foreach (Material material in materials)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name), material);
                importer.SaveAndReimport();
            }

            BuildPrefab("MangoTree_New", true, bark, leaves, greenFruit);
            BuildPrefab("MangoTree_New_NoFruit", false, bark, leaves, greenFruit);
            BuildDemoScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MangoTree_New URP assets rebuilt successfully at " + Root);
        }

        static Texture2D ConfigureTexture(string fileName, bool normalMap, bool alpha, int maxSize)
        {
            string path = Root + "/Textures/" + fileName;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer)
                throw new InvalidOperationException("Missing texture: " + path);
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normalMap;
            importer.mipmapEnabled = true;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = alpha;
            importer.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.anisoLevel = 6;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material MakeMaterial(string name, Shader shader, Color color, float smoothness, bool twoSided)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Cull", twoSided ? 0f : 2f);
            material.SetFloat("_ZWrite", 1f);
            material.doubleSidedGI = twoSided;
            EditorUtility.SetDirty(material);
            return material;
        }

        static void BuildPrefab(string prefabName, bool fruitEnabled, params Material[] materials)
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject root = new GameObject(prefabName);
            SceneManager.MoveGameObjectToScene(root, previewScene);
            try
            {
                LODGroup group = root.AddComponent<LODGroup>();
                LOD[] lods = new LOD[3];
                float[] transitions = { 0.48f, 0.19f, 0.035f };
                for (int lod = 0; lod < 3; lod++)
                {
                    string path = Root + "/Models/MangoTree_New_LOD" + lod + ".fbx";
                    GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, previewScene);
                    instance.name = "LOD" + lod;
                    instance.transform.SetParent(root.transform, false);
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer renderer in renderers)
                    {
                        if (renderer.name.StartsWith("Bark")) renderer.sharedMaterial = materials[0];
                        else if (renderer.name.StartsWith("Leaves")) renderer.sharedMaterial = materials[1];
                        else if (renderer.name.StartsWith("Fruit_Green")) renderer.sharedMaterial = materials[2];
                        bool isLeaf = renderer.name.StartsWith("Leaves");
                        bool isFruit = renderer.name.StartsWith("Fruit");
                        renderer.enabled = !isFruit || fruitEnabled;
                        renderer.shadowCastingMode = isLeaf ? ShadowCastingMode.TwoSided : ShadowCastingMode.On;
                        renderer.receiveShadows = true;
                        renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                        renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
                    }
                    lods[lod] = new LOD(transitions[lod], renderers);
                }
                group.SetLODs(lods);
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                group.RecalculateBounds();

                CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 1.30f, 0f);
                collider.radius = 0.58f;
                collider.height = 2.75f;
                collider.direction = 1;
                root.AddComponent<SecretsThatBreathe.MangoTreeNew.MangoTreeNewWind>();
                PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + prefabName + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        static void BuildDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/MangoTree_New.prefab");
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tree.name = "MangoTree_New (Wind Preview)";

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Preview Ground";
            ground.transform.position = new Vector3(0f, -0.08f, 0f);
            ground.transform.localScale = Vector3.one * 3f;
            Material groundMaterial = MakeMaterial("PreviewGround", Shader.Find("Universal Render Pipeline/Lit"), new Color(0.19f, 0.24f, 0.17f), 0.08f, false);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            GameObject sunObject = new GameObject("Sun");
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.45f;
            sun.color = new Color(1f, 0.91f, 0.78f);
            sunObject.transform.rotation = Quaternion.Euler(46f, -32f, 0f);

            GameObject windObject = new GameObject("Wind Zone - Directional");
            WindZone zone = windObject.AddComponent<WindZone>();
            zone.mode = WindZoneMode.Directional;
            zone.windMain = 1.0f;
            zone.windTurbulence = 0.52f;
            zone.windPulseMagnitude = 0.28f;
            zone.windPulseFrequency = 0.22f;
            windObject.transform.rotation = Quaternion.Euler(0f, 32f, 0f);

            GameObject cameraObject = new GameObject("Preview Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(13f, 6.8f, -15f);
            cameraObject.transform.LookAt(new Vector3(0f, 3.8f, 0f));
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.Skybox;

            EditorSceneManager.SaveScene(scene, Root + "/Demo/MangoTree_New_Preview.unity");
        }
    }
}
