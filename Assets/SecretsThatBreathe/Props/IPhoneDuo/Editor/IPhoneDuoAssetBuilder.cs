using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SecretsThatBreathe.IPhoneDuo.Editor
{
    public static class IPhoneDuoAssetBuilder
    {
        public const string Root = "Assets/SecretsThatBreathe/Props/IPhoneDuo";
        private const string ModelPath = Root + "/Models/IPhoneDuo.fbx";
        private static readonly string[] MaterialNames =
        {
            "DUO_PolishedTitanium", "DUO_CeramicWhite", "DUO_Graphite", "DUO_Antenna",
            "DUO_LensGlass", "DUO_LensCoating", "DUO_Flash", "DUO_Logo",
            "DUO_InnerDisplay", "DUO_CoverDisplay"
        };

        [MenuItem("Tools/Secrets That Breathe/iPhone DUO/Build prefabs")]
        public static void Build()
        {
            foreach (string folder in new[] { "Materials", "Prefabs", "Preview", "Demo" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var materials = BuildMaterials();
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (!importer) throw new InvalidOperationException("Missing model: " + ModelPath);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.preserveHierarchy = true;
            importer.sortHierarchyByName = false;
            importer.optimizeGameObjects = false;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            // This articulated prop uses the authored material UVs and is not marked static for baking.
            importer.generateSecondaryUV = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var pair in materials)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);
            importer.SaveAndReimport();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!source) throw new InvalidOperationException("FBX did not import as a GameObject.");
            var root = new GameObject("IPhoneDuo");
            try
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                var hinge = Named(visual, "HingePivot");
                var left = Named(visual, "LeftLeaf");
                var right = Named(visual, "RightLeaf");
                if (!right.IsChildOf(hinge)) throw new InvalidOperationException("RightLeaf must remain under HingePivot.");
                OrientVisual(visual.transform, hinge, left);
                foreach (var renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (!slots[i]) throw new InvalidOperationException("Unassigned FBX material on " + renderer.name);
                        string name = slots[i].name.Replace(" (Instance)", string.Empty);
                        if (!materials.TryGetValue(name, out slots[i]))
                            throw new InvalidOperationException("Unexpected FBX material: " + name);
                    }
                    renderer.sharedMaterials = slots;
                }
                AddLeafCollider(left);
                AddLeafCollider(right);
                Anchor(root.transform, "GripAnchor", Vector3.zero);
                Anchor(root.transform, "HingeAnchor", root.transform.InverseTransformPoint(hinge.position));
                Anchor(root.transform, "DisplayAnchor", new Vector3(0f, 0f, .0028f));
                var fold = root.AddComponent<IPhoneDuoFold>();
                Vector3 movingOffset = right.GetComponentsInChildren<MeshRenderer>(true)[0].bounds.center
                    - left.GetComponentsInChildren<MeshRenderer>(true)[0].bounds.center;
                float foldDirection = Mathf.Sign(movingOffset.x);
                fold.Configure(hinge, hinge.InverseTransformDirection(Vector3.up * foldDirection));
                var report = Validate(root, fold);
                SaveState(root, fold, 0f, "IPhoneDuo_Open");
                SaveState(root, fold, 90f, "IPhoneDuo_HalfOpen");
                SaveState(root, fold, 180f, "IPhoneDuo_Closed");
                fold.Open();
                AssetDatabase.SaveAssets();
                File.WriteAllText(Root + "/UnityVerification.json", JsonUtility.ToJson(report, true));
                string reportDirectory = Environment.GetEnvironmentVariable("IPHONE_DUO_REPORT_DIR");
                if (!string.IsNullOrEmpty(reportDirectory))
                {
                    Directory.CreateDirectory(reportDirectory);
                    File.WriteAllText(Path.Combine(reportDirectory, "UnityVerification.json"), JsonUtility.ToJson(report, true));
                }
                Debug.Log("IPHONE_DUO_BUILD_PASS: " + report.triangles + " triangles; " + report.meshRenderers + " renderers; " + report.openSizeMetres + "; articulated hinge and leaf colliders.");
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [MenuItem("Tools/Secrets That Breathe/iPhone DUO/Build prefabs and previews")]
        public static void BuildWithPreviews()
        {
            Build();
            GeneratePreviews();
        }

        public static void BatchBuild()
        {
            try
            {
                BuildWithPreviews();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static Dictionary<string, Material> BuildMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit shader is unavailable.");
            var colors = new[]
            {
                new Color(.67f,.69f,.70f), new Color(.91f,.90f,.86f), new Color(.012f,.015f,.019f),
                new Color(.50f,.50f,.47f), new Color(.008f,.014f,.022f), new Color(.035f,.075f,.12f),
                new Color(.91f,.88f,.76f), new Color(.69f,.70f,.68f), Color.white, Color.white
            };
            float[] metallic = { .95f,.05f,.35f,0f,.15f,.35f,.05f,.40f,0f,0f };
            float[] smoothness = { .91f,.62f,.56f,.31f,.96f,.94f,.60f,.65f,.78f,.78f };
            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            for (int i = 0; i < MaterialNames.Length; i++)
            {
                string name = MaterialNames[i];
                string path = Root + "/Materials/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
                material.name = name;
                material.shader = shader;
                material.SetColor("_BaseColor", colors[i]);
                material.SetFloat("_Metallic", metallic[i]);
                material.SetFloat("_Smoothness", smoothness[i]);
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_Cull", 2f);
                material.enableInstancing = true;
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                if (i >= 8)
                {
                    string texturePath = Root + "/Textures/" + (i == 8 ? "Inner_Display.png" : "Cover_Display.png");
                    var textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                    if (!textureImporter) throw new InvalidOperationException("Display texture missing: " + texturePath);
                    textureImporter.textureType = TextureImporterType.Default;
                    textureImporter.sRGBTexture = true;
                    textureImporter.maxTextureSize = 2048;
                    textureImporter.mipmapEnabled = true;
                    textureImporter.wrapMode = TextureWrapMode.Clamp;
                    textureImporter.filterMode = FilterMode.Trilinear;
                    textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                    textureImporter.SaveAndReimport();
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    material.SetTexture("_BaseMap", texture);
                    material.SetTexture("_EmissionMap", texture);
                    // OLED content is mostly self-lit; a direct studio reflection must not erase it.
                    material.SetColor("_BaseColor", new Color(.025f,.025f,.025f,1f));
                    material.SetColor("_EmissionColor", new Color(.95f,.95f,.95f,1f));
                    material.SetFloat("_Smoothness", .35f);
                    material.SetFloat("_SpecularHighlights", 0f);
                    material.SetFloat("_EnvironmentReflections", 0f);
                    material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                    material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                    material.EnableKeyword("_EMISSION");
                    // URP's material validator disables _EMISSION when no emissive GI flag is set.
                    // BakedEmissive retains visible emission; these articulated renderers are not static.
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                }
                EditorUtility.SetDirty(material);
                result.Add(name, material);
            }
            return result;
        }

        private static Transform Named(GameObject root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one " + name + "; got " + matches.Length);
            return matches[0];
        }

        private static void OrientVisual(Transform visual, Transform hinge, Transform left)
        {
            // The authored hinge is offset 3.05 mm toward Blender +Z from the centred root.
            // Recover that normal after FBX axis conversion; retain every source transform below Visual.
            Vector3 normal = hinge.position - visual.position;
            if (normal.magnitude < .001f || normal.magnitude > .006f)
                throw new InvalidOperationException("Unexpected hinge offset: " + normal.ToString("F6"));
            normal.Normalize();
            Vector3 up = Vector3.ProjectOnPlane(left.up, normal).normalized;
            if (up.sqrMagnitude < .9f) throw new InvalidOperationException("Could not recover the authored screen plane.");
            visual.localRotation = Quaternion.Inverse(Quaternion.LookRotation(normal, up)) * visual.localRotation;
            Debug.Log("IPHONE_DUO_ORIENTATION: authored top=" + left.up.ToString("F4")
                + "; hinge=" + hinge.position.ToString("F6") + "; Visual rotation=" + visual.localEulerAngles.ToString("F3"));
        }

        private static void Anchor(Transform root, string name, Vector3 localPosition)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(root, false);
            anchor.localPosition = localPosition;
        }

        private static void AddLeafCollider(Transform leaf)
        {
            var renderers = leaf.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Leaf has no meshes: " + leaf.name);
            Bounds bounds = new Bounds(leaf.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (var renderer in renderers)
            {
                Bounds world = renderer.bounds;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                            bounds.Encapsulate(leaf.InverseTransformPoint(world.center + Vector3.Scale(world.extents, new Vector3(x,y,z))));
            }
            var collider = leaf.gameObject.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
        }

        private static void SaveState(GameObject root, IPhoneDuoFold fold, float angle, string name)
        {
            fold.SetFoldAngle(angle);
            root.name = name;
            var saved = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab");
            if (!saved || !saved.GetComponent<IPhoneDuoFold>()) throw new InvalidOperationException("Prefab save failed: " + name);
        }

        private static Bounds MeshBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("No renderable model geometry.");
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static VerificationReport Validate(GameObject root, IPhoneDuoFold fold)
        {
            fold.Open();
            Bounds open = MeshBounds(root);
            if (Mathf.Abs(open.size.x - .1646f) > .002f || Mathf.Abs(open.size.y - .1178f) > .002f)
                throw new InvalidOperationException("Incorrect metre scale or portrait orientation: " + open.size.ToString("F6"));
            int triangles = 0;
            int meshCount = 0;
            var uniqueMaterials = new HashSet<Material>();
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh) throw new InvalidOperationException("Missing mesh on " + renderer.name);
                var mesh = filter.sharedMesh;
                for (int i = 0; i < mesh.subMeshCount; i++) triangles += (int)mesh.GetIndexCount(i) / 3;
                if (!mesh.HasVertexAttribute(VertexAttribute.Normal) || !mesh.HasVertexAttribute(VertexAttribute.TexCoord0))
                    throw new InvalidOperationException("Missing authored normal or UV0 on " + renderer.name);
                if (renderer.sharedMaterials.Length != mesh.subMeshCount) throw new InvalidOperationException("Material slot mismatch: " + renderer.name);
                foreach (var material in renderer.sharedMaterials)
                {
                    if (!material || material.shader.name != "Universal Render Pipeline/Lit" || ShaderUtil.ShaderHasError(material.shader))
                        throw new InvalidOperationException("Material/shader failure on " + renderer.name);
                    uniqueMaterials.Add(material);
                    if (material.name.EndsWith("Display", StringComparison.Ordinal)
                        && (!material.IsKeywordEnabled("_EMISSION") || !material.GetTexture("_EmissionMap")))
                        throw new InvalidOperationException("Display emission was disabled during material import: " + material.name);
                }
                meshCount++;
            }
            if (triangles > 60000) throw new InvalidOperationException("The prop exceeds the 60,000-triangle close-up budget.");
            var colliders = root.GetComponentsInChildren<BoxCollider>();
            if (colliders.Length != 2 || colliders.Any(c => c.size.x <= 0f || c.size.y <= 0f || c.size.z <= 0f))
                throw new InvalidOperationException("Expected two positive-size leaf colliders.");
            var right = Named(root, "RightLeaf");
            Vector3 restPosition = fold.Hinge.position;
            Vector3 rightLocalNormal = right.InverseTransformDirection(Vector3.forward);
            for (int angle = 0; angle <= 180; angle += 15)
            {
                fold.SetFoldAngle(angle);
                if (Vector3.Distance(fold.Hinge.position, restPosition) > .000001f)
                    throw new InvalidOperationException("Hinge position drifts during folding.");
                if (!right.IsChildOf(fold.Hinge)) throw new InvalidOperationException("Articulated hierarchy is broken.");
            }
            Bounds closed = MeshBounds(root);
            if (Vector3.Dot(right.TransformDirection(rightLocalNormal).normalized, Vector3.forward) > -.99f)
                throw new InvalidOperationException("The moving display does not face inward when closed.");
            if (closed.size.x > .090f || closed.size.x < .079f || closed.size.z < .010f || closed.size.z > .025f)
                throw new InvalidOperationException("Unexpected folded silhouette: " + closed.size.ToString("F6"));
            fold.SetFoldAngle(90f);
            Bounds half = MeshBounds(root);
            fold.Open();
            return new VerificationReport
            {
                status = "PASS", unityVersion = Application.unityVersion, colorSpace = QualitySettings.activeColorSpace.ToString(), triangles = triangles,
                meshRenderers = meshCount, materialAssets = uniqueMaterials.Count, leafColliders = colliders.Length,
                openSizeMetres = open.size, closedSizeMetres = closed.size, halfOpenSizeMetres = half.size,
                hingePositionMetres = restPosition, hingeLocalAxis = fold.LocalFoldAxis,
                foldSamples = 13, normalAndUV0Attributes = true, displayEmissionVerified = true,
                notes = "FBX reimported; metre scale; +Y top / +Z open display; original source hierarchy retained. Fold 0..180 at 15-degree intervals checks pivot stability and inward-facing closed display. The two BoxColliders are interaction approximations. No physics collision simulation or gameplay pickup/input binding. No lightmap UV2 generated for this moving prop. Camera bump is included in reported overall thickness."
            };
        }

        private static void GeneratePreviews()
        {
            var previousScene = SceneManager.GetActiveScene();
            var oldDefault = GraphicsSettings.defaultRenderPipeline;
            var oldQuality = QualitySettings.renderPipeline;
            var oldAmbientMode = RenderSettings.ambientMode;
            Color oldAmbientLight = RenderSettings.ambientLight;
            // The isolated CLI project starts with an untitled scene and may replace it.
            // Interactive builds use an additive scene so an existing user scene is retained.
            if (!Application.isBatchMode && string.IsNullOrEmpty(previousScene.path))
                throw new InvalidOperationException("The current scene must be saved before additive preview generation. The independent CLI build can generate previews without changing it.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root + "/Demo/DUO_PreviewRenderer.asset");
                if (!data) { data = ScriptableObject.CreateInstance<UniversalRendererData>(); AssetDatabase.CreateAsset(data, Root + "/Demo/DUO_PreviewRenderer.asset"); }
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root + "/Demo/DUO_PreviewPipeline.asset");
                if (!pipeline) { pipeline = UniversalRenderPipelineAsset.Create(data); AssetDatabase.CreateAsset(pipeline, Root + "/Demo/DUO_PreviewPipeline.asset"); }
                pipeline.msaaSampleCount = 4;
                pipeline.supportsHDR = true;
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.28f,.31f,.35f);
                var key = new GameObject("Studio Key").AddComponent<Light>();
                key.type = LightType.Directional; key.intensity = .7f;
                key.transform.rotation = Quaternion.Euler(35f,-35f,0f);
                var fill = new GameObject("Studio Fill").AddComponent<Light>();
                fill.type = LightType.Directional; fill.intensity = .4f;
                fill.transform.rotation = Quaternion.Euler(-25f,135f,0f);
                var rim = new GameObject("Studio Rim").AddComponent<Light>();
                rim.type = LightType.Directional; rim.intensity = .5f;
                rim.transform.rotation = Quaternion.Euler(35f,145f,0f);
                var camera = new GameObject("Preview Camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.055f,.066f,.082f);
                camera.orthographic = true; camera.nearClipPlane = .001f; camera.farClipPlane = 5f;
                camera.allowHDR = true; camera.allowMSAA = true;
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
                var volume = new GameObject("Studio Tonemapping").AddComponent<Volume>();
                volume.isGlobal = true;
                string profilePath = Root + "/Demo/DUO_PreviewVolume.asset";
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
                if (!profile) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, profilePath); }
                if (!profile.TryGet<Tonemapping>(out var tonemapping))
                {
                    tonemapping = profile.Add<Tonemapping>(true);
                    AssetDatabase.AddObjectToAsset(tonemapping, profile);
                }
                tonemapping.mode.Override(TonemappingMode.ACES);
                volume.sharedProfile = profile;
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/IPhoneDuo_Open.prefab");
                var phone = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
                var fold = phone.GetComponent<IPhoneDuoFold>();
                RenderView(phone, fold, camera, 0f, new Vector3(.16f,.11f,.26f), "Unity_Open_Front.png");
                RenderView(phone, fold, camera, 0f, new Vector3(-.15f,.10f,-.26f), "Unity_Open_Back.png");
                RenderView(phone, fold, camera, 90f, new Vector3(.20f,.13f,.23f), "Unity_HalfOpen.png");
                RenderView(phone, fold, camera, 180f, new Vector3(-.13f,.08f,-.23f), "Unity_Closed_Cover.png");
                RenderView(phone, fold, camera, 180f, new Vector3(.14f,.10f,.24f), "Unity_Closed_Back.png");
                RenderView(phone, fold, camera, 0f, new Vector3(.12f,.20f,.025f), "Unity_Hinge_Edge.png");
                fold.HalfOpen();
                Bounds demoBounds = MeshBounds(phone);
                camera.transform.position = demoBounds.center + new Vector3(.24f,.14f,.29f);
                camera.transform.LookAt(demoBounds.center);
                camera.orthographicSize = .105f;
                EditorSceneManager.SaveScene(scene, Root + "/Demo/IPhoneDuo_Preview.unity");
                AssetDatabase.SaveAssets();
                Debug.Log("IPHONE_DUO_PREVIEW_PASS: six URP renders and an independent demo scene saved.");
            }
            finally
            {
                GraphicsSettings.defaultRenderPipeline = oldDefault;
                QualitySettings.renderPipeline = oldQuality;
                if (previousScene.IsValid() && previousScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousScene);
                    EditorSceneManager.CloseScene(scene, true);
                    RenderSettings.ambientMode = oldAmbientMode;
                    RenderSettings.ambientLight = oldAmbientLight;
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void RenderView(GameObject phone, IPhoneDuoFold fold, Camera camera, float angle, Vector3 cameraOffset, string filename)
        {
            fold.SetFoldAngle(angle);
            Bounds bounds = MeshBounds(phone);
            camera.transform.position = bounds.center + cameraOffset;
            camera.transform.LookAt(bounds.center);
            camera.aspect = 1400f / 1100f;
            Vector3 right = camera.transform.right;
            Vector3 up = camera.transform.up;
            float halfWidth = Mathf.Abs(right.x) * bounds.extents.x + Mathf.Abs(right.y) * bounds.extents.y + Mathf.Abs(right.z) * bounds.extents.z;
            float halfHeight = Mathf.Abs(up.x) * bounds.extents.x + Mathf.Abs(up.y) * bounds.extents.y + Mathf.Abs(up.z) * bounds.extents.z;
            camera.orthographicSize = Mathf.Max(halfHeight, halfWidth / camera.aspect) * 1.18f;
            // An sRGB LDR target gives PNG readback the same display transfer as the Game view.
            var texture = new RenderTexture(1400, 1100, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(1400, 1100, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                texture.Create(); camera.targetTexture = texture;
                // Warm the newly selected URP pipeline before readback of its first camera.
                camera.Render(); camera.Render(); RenderTexture.active = texture;
                image.ReadPixels(new Rect(0,0,1400,1100),0,0); image.Apply();
                File.WriteAllBytes(Root + "/Preview/" + filename, image.EncodeToPNG());
                string reportDirectory = Environment.GetEnvironmentVariable("IPHONE_DUO_REPORT_DIR");
                if (!string.IsNullOrEmpty(reportDirectory))
                    File.WriteAllBytes(Path.Combine(reportDirectory, filename), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                texture.Release(); Object.DestroyImmediate(texture); Object.DestroyImmediate(image);
            }
        }

        [Serializable]
        private sealed class VerificationReport
        {
            public string status;
            public string unityVersion;
            public string colorSpace;
            public int triangles;
            public int meshRenderers;
            public int materialAssets;
            public int leafColliders;
            public Vector3 openSizeMetres;
            public Vector3 closedSizeMetres;
            public Vector3 halfOpenSizeMetres;
            public Vector3 hingePositionMetres;
            public Vector3 hingeLocalAxis;
            public int foldSamples;
            public bool normalAndUV0Attributes;
            public bool displayEmissionVerified;
            public string notes;
        }
    }
}
