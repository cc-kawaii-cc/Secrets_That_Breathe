// Mango Tree asset pack - automatic setup for Unity 6 (URP)
// Fixes texture / model import settings, creates the materials (wind shader), LOD prefabs and a demo scene.
// Menu: Tools > Mango Tree > ...
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MangoTreePack.EditorTools
{
    [InitializeOnLoad]
    public static class MangoTreeSetup
    {
        const string LitShader = "Universal Render Pipeline/Lit";
        public const string WindShader = "MangoTree/Wind URP";
        const string SessionKey = "MangoTree.AutoSetupDone.v1";

        public static readonly string[] Variants = { "MangoTree_NoFruit", "MangoTree_NearlyRipe", "MangoTree_Ripe", "MangoTree_Mixed" };

        // Screen-relative height at which each LOD starts (LOD0, LOD1, LOD2). Below the last value the tree is culled.
        static readonly float[] LodHeights = { 0.22f, 0.07f, 0.02f };

        // Tree size (metres) used for the wind weights. Bend is the same for every material so bark, leaves and fruit move together.
        const float TreeHeight = 5.5f;
        const float CrownRadius = 2.6f;
        const float WindBend = 0.18f;

        static MangoTreeSetup()
        {
            EditorApplication.delayCall += AutoSetupOnce;
        }

        static void AutoSetupOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoSetupOnce;
                return;
            }
            string root = Root;
            if (root == null) return;
            // prefabs are only created when missing -> never overwrites the user's changes
            bool missing = AssetDatabase.LoadAssetAtPath<GameObject>(root + "/Prefabs/MangoTree_Mixed.prefab") == null
                           && AssetDatabase.LoadAssetAtPath<GameObject>(root + "/Models/MangoTree_Mixed.fbx") != null;
            if (!missing) return;
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            Debug.Log("[MangoTree] First import detected - running automatic setup...");
            RunSetup();
        }

        /// <summary>Folder that contains Models/, Textures/, Editor/ (e.g. Assets/AImodie/MangoTree)</summary>
        public static string Root
        {
            get
            {
                foreach (var guid in AssetDatabase.FindAssets("MangoTreeSetup t:MonoScript"))
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    if (!p.EndsWith("/MangoTreeSetup.cs")) continue;
                    var editorDir = Path.GetDirectoryName(p)?.Replace('\\', '/');
                    var root = Path.GetDirectoryName(editorDir)?.Replace('\\', '/');
                    if (!string.IsNullOrEmpty(root)) return root;
                }
                return null;
            }
        }

        [MenuItem("Tools/Mango Tree/1) Setup Materials & Prefabs", priority = 1)]
        public static void RunSetup()
        {
            string root = Root;
            if (root == null) { Debug.LogError("[MangoTree] Cannot locate the MangoTree folder."); return; }
            if (Shader.Find(LitShader) == null)
            {
                Debug.LogError("[MangoTree] URP Lit shader not found. Make sure the project uses the Universal Render Pipeline.");
                return;
            }
            EnsureFolder(root, "Materials");
            EnsureFolder(root, "Prefabs");

            try
            {
                AssetDatabase.StartAssetEditing();
                FixTextureImporters(root);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            var mats = new Dictionary<string, Material>
            {
                ["M_Mango_Bark"]            = CreateMaterial(root, "M_Mango_Bark",            "Bark",  "Bark",       false, 0.08f, 1.2f, 0.00f),
                ["M_Mango_Leaf"]            = CreateMaterial(root, "M_Mango_Leaf",            "Leaf",  "Leaf",       true,  0.35f, 0.6f, 0.03f),
                ["M_Mango_Fruit_Ripe"]      = CreateMaterial(root, "M_Mango_Fruit_Ripe",      "Fruit", "Fruit_Ripe", false, 0.55f, 1.0f, 0.008f),
                ["M_Mango_Fruit_NearlyRipe"] = CreateMaterial(root, "M_Mango_Fruit_NearlyRipe", "Fruit", "Fruit_NearlyRipe", false, 0.45f, 1.0f, 0.008f),
            };
            AssetDatabase.SaveAssets();

            try
            {
                AssetDatabase.StartAssetEditing();
                FixModelImporters(root, mats);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            CreatePrefabs(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MangoTree] Setup complete: Materials/ and Prefabs/ are ready. Tools > Mango Tree > 2) Create Demo Scene to test.");
        }

        // ------------------------------------------------------------------------------------
        static void EnsureFolder(string root, string name)
        {
            if (!AssetDatabase.IsValidFolder(root + "/" + name))
                AssetDatabase.CreateFolder(root, name);
        }

        static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        static void FixTextureImporters(string root)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root + "/Textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                string file = Path.GetFileNameWithoutExtension(path);
                bool leaf = file.Contains("_Leaf_");
                bool bark = file.Contains("_Bark_");
                bool changed = false;

                if (file.EndsWith("_Normal"))
                {
                    if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; changed = true; }
                }
                else if (file.EndsWith("_Albedo"))
                {
                    if (!ti.sRGBTexture) { ti.sRGBTexture = true; changed = true; }
                    if (leaf)
                    {
                        if (ti.alphaSource != TextureImporterAlphaSource.FromInput) { ti.alphaSource = TextureImporterAlphaSource.FromInput; changed = true; }
                        if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
                        if (!ti.mipMapsPreserveCoverage) { ti.mipMapsPreserveCoverage = true; changed = true; }
                        if (Mathf.Abs(ti.alphaTestReferenceValue - 0.5f) > 0.001f) { ti.alphaTestReferenceValue = 0.5f; changed = true; }
                    }
                    else if (ti.alphaSource != TextureImporterAlphaSource.None)
                    {
                        ti.alphaSource = TextureImporterAlphaSource.None; changed = true;
                    }
                }
                if (ti.maxTextureSize != 2048) { ti.maxTextureSize = 2048; changed = true; }
                if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; changed = true; }
                // bark tiles along the trunk; leaf atlas must not bleed between cells
                var wrap = leaf ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                if (ti.wrapMode != wrap) { ti.wrapMode = wrap; changed = true; }
                if (ti.anisoLevel < 4) { ti.anisoLevel = 4; changed = true; }
                if (changed) ti.SaveAndReimport();
            }
        }

        static Texture2D FindTex(string root, string name)
        {
            return Load<Texture2D>($"{root}/Textures/{name}.png");
        }

        static Material CreateMaterial(string root, string matName, string texSet, string texName,
                                       bool leaf, float smoothness, float bump, float flutter)
        {
            var shader = Shader.Find(LitShader);
            bool wind = false;
            var ws = Shader.Find(WindShader);
            if (ws != null && ws.isSupported && WindShaderCompiles(ws)) { shader = ws; wind = true; }
            else Debug.LogWarning("[MangoTree] Wind shader not available - using URP Lit for " + matName);

            string path = $"{root}/Materials/{matName}.mat";
            var m = Load<Material>(path);
            bool isNew = m == null;
            if (isNew)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            bool switched = !isNew && m.shader != shader;
            if (switched) m.shader = shader;

            var albedo = FindTex(root, $"T_Mango_{texName}_Albedo");
            Texture2D normal = null;
            if (texSet == "Bark" || texSet == "Leaf") normal = FindTex(root, $"T_Mango_{texSet}_Normal");

            m.SetTexture("_BaseMap", albedo);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_WorkflowMode", 1f);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_SmoothnessTextureChannel", 0f);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", bump);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 1f);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_ReceiveShadows", 1f);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (leaf)
            {
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                m.SetFloat("_AlphaClip", 0f);
                m.DisableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "Opaque");
                m.renderQueue = (int)RenderQueue.Geometry;
            }
            m.SetFloat("_Cull", leaf ? (float)CullMode.Off : (float)CullMode.Back);
            m.doubleSidedGI = leaf;
            m.enableInstancing = true;

            if (wind && (isNew || switched))
            {
                // x = bend (m per unit WindZone Main), y = flutter (m), z = tree height (m), w = sway speed
                m.SetVector("_MGWind", new Vector4(WindBend, flutter, TreeHeight, 1.5f));
                // x = crown radius (m), y = height where bending starts (m), z = flutter speed, w = gust wave scale
                m.SetVector("_MGWind2", new Vector4(CrownRadius, 1.0f, 1.0f, 0.25f));
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static int s_WindCheck; // 0 = not checked, 1 = ok, -1 = failed

        /// <summary>Compiles every pass of the wind shader once and logs any error (falls back to URP Lit on failure).</summary>
        static bool WindShaderCompiles(Shader s)
        {
            if (s_WindCheck != 0) return s_WindCheck > 0;
            bool ok = !ShaderUtil.ShaderHasError(s);
            foreach (var msg in ShaderUtil.GetShaderMessages(s))
                Debug.Log($"[MangoTree] Wind shader message ({msg.severity}): {msg.message} {msg.messageDetails} ({msg.file}:{msg.line})");
            try
            {
                var sub = ShaderUtil.GetShaderData(s).GetSubshader(0);
                var stages = new[] { UnityEditor.Rendering.ShaderType.Vertex, UnityEditor.Rendering.ShaderType.Fragment };
                for (int p = 0; p < sub.PassCount; p++)
                {
                    var pass = sub.GetPass(p);
                    foreach (var st in stages)
                    {
                        if (!pass.HasShaderStage(st)) continue;
                        var info = pass.CompileVariant(st, new string[0],
                            UnityEditor.Rendering.ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64);
                        if (info.Success) continue;
                        ok = false;
                        foreach (var m in info.Messages)
                            Debug.LogError($"[MangoTree] Wind shader {pass.Name}/{st}: {m.message} {m.messageDetails} (line {m.line})");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[MangoTree] Shader compile check skipped: " + e.Message);
            }
            Debug.Log("[MangoTree] Wind shader compile check: " + (ok ? "OK" : "FAILED (using URP Lit instead)"));
            s_WindCheck = ok ? 1 : -1;
            return ok;
        }

        [MenuItem("Tools/Mango Tree/4) Re-check Wind Shader", priority = 40)]
        static void RecheckWind()
        {
            s_WindCheck = 0;
            RunSetup();
        }

        // ------------------------------------------------------------------------------------
        static IEnumerable<string> ModelPaths(string root)
        {
            return AssetDatabase.FindAssets("t:Model", new[] { root + "/Models" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                .Distinct();
        }

        static void FixModelImporters(string root, Dictionary<string, Material> mats)
        {
            foreach (var path in ModelPaths(root))
            {
                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null) continue;
                mi.globalScale = 1f;
                mi.useFileScale = true;
                mi.importCameras = false;
                mi.importLights = false;
                mi.importVisibility = false;
                mi.importBlendShapes = false;
                mi.importAnimation = false;
                mi.animationType = ModelImporterAnimationType.None;
                mi.importNormals = ModelImporterNormals.Import;
                mi.importTangents = ModelImporterTangents.CalculateMikk;
                mi.meshCompression = ModelImporterMeshCompression.Off;
                mi.isReadable = false;
                mi.addCollider = false;
                mi.generateSecondaryUV = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                foreach (var kv in mats)
                {
                    if (kv.Value == null) continue;
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                }
                mi.SaveAndReimport();
            }
        }

        static void CreatePrefabs(string root)
        {
            foreach (var path in ModelPaths(root))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                string prefabPath = $"{root}/Prefabs/{name}.prefab";
                if (Load<GameObject>(prefabPath) != null) continue; // never overwrite
                var model = Load<GameObject>(path);
                if (model == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                ConfigureLOD(inst);
                foreach (var r in inst.GetComponentsInChildren<MeshRenderer>())
                {
                    r.shadowCastingMode = r.name.EndsWith("_LOD2") ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    r.receiveShadows = true;
                }
                // trunk collider
                if (inst.GetComponent<CapsuleCollider>() == null)
                {
                    var cc = inst.AddComponent<CapsuleCollider>();
                    cc.direction = 1; // Y
                    cc.radius = 0.28f;
                    cc.height = 2.4f;
                    cc.center = new Vector3(0f, 1.2f, 0f);
                }
                PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
                Object.DestroyImmediate(inst);
            }
        }

        static void ConfigureLOD(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            var byLod = new List<Renderer>();
            for (int i = 0; i < 8; i++)
            {
                var r = renderers.FirstOrDefault(x => x.name.EndsWith("_LOD" + i));
                if (r == null) break;
                byLod.Add(r);
            }
            if (byLod.Count < 2) return;
            var lg = go.GetComponent<LODGroup>();
            if (lg == null) lg = go.AddComponent<LODGroup>();
            int count = Mathf.Min(byLod.Count, LodHeights.Length);
            var lods = new LOD[count];
            for (int i = 0; i < count; i++)
                lods[i] = new LOD(LodHeights[i], new[] { byLod[i] });
            lg.SetLODs(lods);
            lg.fadeMode = LODFadeMode.CrossFade;
            lg.animateCrossFading = true;
            lg.RecalculateBounds();
        }

        // ------------------------------------------------------------------------------------
        [MenuItem("Tools/Mango Tree/3) Add Wind Zone to Scene", priority = 21)]
        public static void AddWindZone()
        {
            var go = new GameObject("Wind Zone (Mango Tree)");
            var wz = go.AddComponent<WindZone>();
            wz.mode = WindZoneMode.Directional;
            wz.windMain = 0.7f;
            wz.windTurbulence = 0.6f;
            wz.windPulseMagnitude = 0.5f;
            wz.windPulseFrequency = 0.15f;
            go.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
            Undo.RegisterCreatedObjectUndo(go, "Add Wind Zone");
            Selection.activeGameObject = go;
        }

        [MenuItem("Tools/Mango Tree/2) Create Demo Scene", priority = 2)]
        public static void CreateDemoScene()
        {
            string root = Root;
            if (root == null) return;
            if (Load<GameObject>(root + "/Prefabs/MangoTree_Mixed.prefab") == null) RunSetup();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(root, "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            var gm = Load<Material>(root + "/Materials/M_Mango_Ground.mat");
            if (gm == null)
            {
                gm = new Material(Shader.Find(LitShader));
                gm.SetColor("_BaseColor", new Color(0.30f, 0.38f, 0.16f));
                gm.SetFloat("_Smoothness", 0.05f);
                AssetDatabase.CreateAsset(gm, root + "/Materials/M_Mango_Ground.mat");
            }
            ground.GetComponent<MeshRenderer>().sharedMaterial = gm;

            // the four variants side by side
            const float spacing = 7f;
            for (int i = 0; i < Variants.Length; i++)
            {
                var prefab = Load<GameObject>($"{root}/Prefabs/{Variants[i]}.prefab");
                if (prefab == null) continue;
                var t = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                t.transform.position = new Vector3((i - (Variants.Length - 1) * 0.5f) * spacing, 0f, 0f);
                t.transform.rotation = Quaternion.Euler(0f, 25f * i, 0f);
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 2.8f, -17f);
                cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
                cam.fieldOfView = 50f;
                cam.farClipPlane = 200f;
            }
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(42f, -30f, 0f);
                sun.color = new Color(1f, 0.95f, 0.85f);
                sun.intensity = 1.6f;
                sun.shadows = LightShadows.Soft;
            }
            AddWindZone();
            string scenePath = $"{root}/Scenes/MangoTree_Demo.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[MangoTree] Demo scene saved to " + scenePath);
        }

        [MenuItem("Tools/Mango Tree/5) Add Tree Prefabs to Selected Terrain", priority = 22)]
        public static void AddToTerrain()
        {
            string root = Root;
            var go = Selection.activeGameObject;
            var terrain = go != null ? go.GetComponent<Terrain>() : null;
            if (terrain == null) terrain = Terrain.activeTerrain;
            if (root == null || terrain == null)
            {
                EditorUtility.DisplayDialog("Mango Tree", "Select a Terrain in the Hierarchy first.", "OK");
                return;
            }
            if (Load<GameObject>(root + "/Prefabs/MangoTree_Mixed.prefab") == null) RunSetup();
            var td = terrain.terrainData;
            Undo.RegisterCompleteObjectUndo(td, "Mango Tree: add prototypes");
            var trees = td.treePrototypes.ToList();
            int added = 0;
            foreach (var v in Variants)
            {
                var prefab = Load<GameObject>($"{root}/Prefabs/{v}.prefab");
                if (prefab == null || trees.Any(t => t.prefab == prefab)) continue;
                trees.Add(new TreePrototype { prefab = prefab });
                added++;
            }
            td.treePrototypes = trees.ToArray();
            td.RefreshPrototypes();
            terrain.Flush();
            EditorUtility.SetDirty(td);
            Debug.Log($"[MangoTree] Added {added} tree prototypes to '{terrain.name}'. Use the Paint Trees tool.");
        }
    }
}
#endif
