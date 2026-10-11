using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.MangoTreeUpdate.Editor
{
    public static class MangoTreeUpdateAssetBuilder
    {
        public const string Root = "Assets/SecretsThatBreathe/Environment/MangoTree_Update";
        public const string ShaderName = "Secrets That Breathe/Mango Tree Update Wind Lit";

        [MenuItem("Tools/Mango Tree Update/Rebuild URP Assets")]
        static void RebuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path) &&
                !EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) return;
            Build();
        }

        public static void Build()
        {
            foreach (string folder in new[] { "Materials", "Prefabs", "Demo" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Shader shader = Shader.Find(ShaderName);
            if (!shader) throw new InvalidOperationException("Update wind shader is missing.");
            Material bark = MakeMaterial("MangoUpdate_Bark", shader, Color.white, .18f, false);
            bark.SetTexture("_BaseMap", Texture("MangoUpdate_Bark_BaseColor.png", false, false));
            bark.SetTexture("_BumpMap", Texture("MangoUpdate_Bark_Normal.png", true, false));
            bark.SetFloat("_BumpScale", .65f);
            bark.EnableKeyword("_NORMALMAP");
            Material leaves = MakeMaterial("MangoUpdate_Leaves", shader, Color.white, .25f, true);
            leaves.SetTexture("_BaseMap", Texture("MangoUpdate_LeafClusters.png", false, true));
            leaves.SetFloat("_Cutoff", .42f);
            leaves.SetFloat("_AlphaClip", 1);
            leaves.EnableKeyword("_ALPHATEST_ON");
            leaves.renderQueue = (int)RenderQueue.AlphaTest;
            leaves.SetOverrideTag("RenderType", "TransparentCutout");
            Material green = MakeMaterial("MangoUpdate_Fruit_Green", shader, new Color(.43f,.65f,.12f), .34f, false);
            Material[] materials = { bark, leaves, green };
            for (int lod = 0; lod < 3; lod++)
            {
                string path = Root + "/Models/MangoTree_Update_LOD" + lod + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (!importer) throw new InvalidOperationException("Missing model: " + path);
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
                foreach (var remap in importer.GetExternalObjectMap()) importer.RemoveRemap(remap.Key);
                foreach (Material mat in materials)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),mat.name), mat);
                importer.SaveAndReimport();
            }
            BuildPrefab("MangoTree_Update", true, materials);
            BuildPrefab("MangoTree_Update_NoFruit", false, materials);
            MangoTreeTerrainAssetBuilder.Build();
            BuildDemo();
            AssetDatabase.SaveAssets();
            Debug.Log("MangoTree_Update assets built in " + Root);
        }

        static Texture2D Texture(string name, bool normal, bool alpha)
        {
            string path = Root + "/Textures/" + name;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Missing texture: " + path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = alpha;
            importer.mipmapEnabled = true;
            importer.mipMapsPreserveCoverage = alpha;
            importer.alphaTestReferenceValue = .42f;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.anisoLevel = 8;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material MakeMaterial(string name, Shader shader, Color color, float smoothness, bool twoSided)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat,path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor",color);
            mat.SetFloat("_Smoothness",smoothness);
            mat.SetFloat("_Metallic",0);
            mat.SetFloat("_Surface",0);
            mat.SetFloat("_Cull",twoSided ? 0 : 2);
            mat.SetFloat("_ZWrite",1);
            mat.doubleSidedGI = twoSided;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void BuildPrefab(string name, bool fruit, Material[] materials)
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root,scene);
            try
            {
                var group = root.AddComponent<LODGroup>();
                var lods = new LOD[3];
                float[] transitions = { .50f,.20f,.035f };
                for (int level = 0; level < 3; level++)
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/MangoTree_Update_LOD"+level+".fbx");
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model,scene);
                    instance.name = "LOD" + level;
                    instance.transform.SetParent(root.transform,false);
                    PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                    var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        bool isLeaf = renderer.name.StartsWith("Leaves");
                        bool isFruit = renderer.name.StartsWith("Fruit");
                        renderer.sharedMaterial = materials[isLeaf ? 1 : isFruit ? 2 : 0];
                        renderer.enabled = !isFruit || fruit;
                        renderer.shadowCastingMode = isLeaf ? ShadowCastingMode.TwoSided : ShadowCastingMode.On;
                        renderer.receiveShadows = true;
                        renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                        renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
                    }
                    lods[level] = new LOD(transitions[level],renderers);
                }
                group.SetLODs(lods);
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                group.RecalculateBounds();
                var collider = root.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(.01f,1.16f,0);
                collider.radius = .245f;
                collider.height = 2.42f;
                root.AddComponent<MangoTreeUpdateWind>();
                PrefabUtility.SaveAsPrefabAsset(root,Root + "/Prefabs/" + name + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static void BuildDemo()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/MangoTree_Update.prefab"),scene);
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = new Vector3(0,-.055f,0);
                ground.transform.localScale = Vector3.one * 4;
                ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial("PreviewGround",Shader.Find("Universal Render Pipeline/Lit"),new Color(.22f,.27f,.18f),.05f,false);
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.2f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(43,-35,0);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.42f,.48f,.52f);
                RenderSettings.skybox = null;
                var zone = new GameObject("Wind Zone (change Main to test)").AddComponent<WindZone>();
                zone.mode = WindZoneMode.Directional;
                zone.windMain = 1;
                zone.windTurbulence = .5f;
                zone.windPulseMagnitude = .25f;
                zone.windPulseFrequency = .2f;
                zone.transform.rotation = Quaternion.Euler(0,55,0);
                var camera = new GameObject("Preview Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(10,5.8f,-14);
                camera.transform.LookAt(new Vector3(0,3.5f,0));
                camera.orthographic = true;
                camera.orthographicSize = 4.3f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 100;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.14f,.18f,.21f);
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                EditorSceneManager.SaveScene(scene,Root + "/Demo/MangoTree_Update_Preview.unity");
            }
            finally
            {
                if (!Application.isBatchMode)
                {
                    EditorSceneManager.CloseScene(scene,true);
                    if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                }
            }
        }
    }
}
