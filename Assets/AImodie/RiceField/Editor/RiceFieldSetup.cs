// Rice Field asset pack - automatic setup for Unity 6 (URP)
// Creates URP Lit materials, Terrain Layers, LOD prefabs and fixes texture / model import settings.
// Menu: Tools > Rice Field > ...
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RiceField.EditorTools
{
    [InitializeOnLoad]
    public static class RiceFieldSetup
    {
        const string LitShader = "Universal Render Pipeline/Lit";
        const string TerrainShader = "Universal Render Pipeline/Terrain/Lit";
        public const string WindShader = "RiceField/Foliage Wind URP";
        static readonly string[] CardAtlases = { "RiceCards_Ripe", "RiceCards_AlmostRipe", "GrassCards" };

        // Low-poly card atlases (main models) + high-detail atlases (Models/HighDetail)
        public static readonly string[] FoliageAtlases =
            { "RiceCards_Ripe", "RiceCards_AlmostRipe", "GrassCards", "Rice_Ripe", "Rice_AlmostRipe", "FieldGrass" };

        public static readonly string[] GroundSets =
        {
            "Paddy_WetMud", "Paddy_DampSoil", "Paddy_Flooded", "Paddy_AlgaeMud", "Paddy_DryCracked",
            "Paddy_Stubble", "Paddy_StrawMat", "Paddy_Plowed", "Dike_GrassySoil"
        };

        // LOD screen-relative heights. 2 levels = low-poly card models, 3 levels = high-detail models
        static readonly float[] RiceLOD2 = { 0.05f, 0.004f };
        static readonly float[] GrassLOD2 = { 0.05f, 0.006f };
        static readonly float[] RiceLOD3 = { 0.30f, 0.10f, 0.008f };
        static readonly float[] GrassLOD3 = { 0.22f, 0.07f, 0.006f };

        static RiceFieldSetup()
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
            bool detailsMissing = AssetDatabase.LoadAssetAtPath<GameObject>(root + "/Prefabs/TerrainDetail/Rice_Ripe_A_Detail.prefab") == null;
            var cardMat = AssetDatabase.LoadAssetAtPath<Material>(root + "/Materials/M_RiceCards_Ripe.mat");
            var windShader = Shader.Find(WindShader);
            bool windNotApplied = cardMat != null && windShader != null && cardMat.shader != windShader
                                  && !ShaderUtil.ShaderHasError(windShader);
            if (!detailsMissing && !windNotApplied) return;
            if (SessionState.GetBool("RiceField.AutoSetupDone.v4", false)) return;
            SessionState.SetBool("RiceField.AutoSetupDone.v4", true);
            Debug.Log("[RiceField] First import detected - running automatic setup...");
            RunSetup();
        }

        /// <summary>Folder that contains Models/, Textures/, Editor/ (e.g. Assets/RiceField)</summary>
        public static string Root
        {
            get
            {
                foreach (var guid in AssetDatabase.FindAssets("RiceFieldSetup t:MonoScript"))
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    if (!p.EndsWith("/RiceFieldSetup.cs")) continue;
                    var editorDir = Path.GetDirectoryName(p)?.Replace('\\', '/');
                    var root = Path.GetDirectoryName(editorDir)?.Replace('\\', '/');
                    if (!string.IsNullOrEmpty(root)) return root;
                }
                return null;
            }
        }

        [MenuItem("Tools/Rice Field/1) Setup Materials, Prefabs & Terrain Layers", priority = 1)]
        public static void RunSetup()
        {
            string root = Root;
            if (root == null) { Debug.LogError("[RiceField] Cannot locate the RiceField folder."); return; }
            if (Shader.Find(LitShader) == null)
            {
                Debug.LogError("[RiceField] URP Lit shader not found. Make sure the project uses the Universal Render Pipeline.");
                return;
            }
            EnsureFolder(root, "Materials");
            EnsureFolder(root, "Prefabs");
            EnsureFolder(root, "TerrainLayers");

            try
            {
                AssetDatabase.StartAssetEditing();
                FixTextureImporters(root);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            var foliageMats = new Dictionary<string, Material>();
            foreach (var a in FoliageAtlases)
                foliageMats["M_" + a] = CreateFoliageMaterial(root, a);
            foreach (var g in GroundSets)
            {
                CreateGroundMaterial(root, g);
                CreateTerrainLayer(root, g);
            }
            AssetDatabase.SaveAssets();

            try
            {
                AssetDatabase.StartAssetEditing();
                FixModelImporters(root, foliageMats);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            CreatePrefabs(root);
            CreateDetailPrefabs(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RiceField] Setup complete: Materials/, Prefabs/, TerrainLayers/ are ready. " +
                      "Use Tools > Rice Field > 2) Create Demo Scene to see everything in action.");
        }

        // ------------------------------------------------------------------------------------
        static void EnsureFolder(string root, string name)
        {
            if (!AssetDatabase.IsValidFolder(root + "/" + name))
                AssetDatabase.CreateFolder(root, name);
        }

        static void FixTextureImporters(string root)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root + "/Textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                string file = Path.GetFileNameWithoutExtension(path);
                bool foliage = path.Contains("/Foliage/");
                bool changed = false;

                if (file.EndsWith("_Normal"))
                {
                    if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; changed = true; }
                }
                else if (file.EndsWith("_Mask"))
                {
                    if (ti.textureType != TextureImporterType.Default) { ti.textureType = TextureImporterType.Default; changed = true; }
                    if (ti.sRGBTexture) { ti.sRGBTexture = false; changed = true; }
                    if (ti.alphaSource != TextureImporterAlphaSource.FromInput) { ti.alphaSource = TextureImporterAlphaSource.FromInput; changed = true; }
                }
                else if (file.EndsWith("_Albedo"))
                {
                    if (!ti.sRGBTexture) { ti.sRGBTexture = true; changed = true; }
                    if (foliage)
                    {
                        if (ti.alphaSource != TextureImporterAlphaSource.FromInput) { ti.alphaSource = TextureImporterAlphaSource.FromInput; changed = true; }
                        if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
                        if (!ti.mipMapsPreserveCoverage) { ti.mipMapsPreserveCoverage = true; changed = true; }
                        if (Mathf.Abs(ti.alphaTestReferenceValue - 0.5f) > 0.001f) { ti.alphaTestReferenceValue = 0.5f; changed = true; }
                    }
                }
                if (ti.maxTextureSize != 2048) { ti.maxTextureSize = 2048; changed = true; }
                if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; changed = true; }
                if (ti.wrapMode != (foliage ? TextureWrapMode.Clamp : TextureWrapMode.Repeat))
                {
                    ti.wrapMode = foliage ? TextureWrapMode.Clamp : TextureWrapMode.Repeat; changed = true;
                }
                if (ti.anisoLevel < 4 && !foliage) { ti.anisoLevel = 4; changed = true; }
                if (changed) ti.SaveAndReimport();
            }
        }

        static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        static Texture2D FindTex(string root, string sub, string baseName, string kind)
        {
            foreach (var ext in new[] { ".png", ".jpg", ".tga" })
            {
                var t = Load<Texture2D>($"{root}/Textures/{sub}/T_{baseName}_{kind}{ext}");
                if (t != null) return t;
            }
            return null;
        }

        static Material GetOrCreateMaterial(string path, Shader shader)
        {
            var m = Load<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader)
            {
                m.shader = shader;
            }
            return m;
        }

        static void SetupLit(Material m, Texture2D albedo, Texture2D normal, Texture2D mask, float smoothScale,
                             float normalScale, bool alphaClip, bool doubleSided)
        {
            m.SetTexture("_BaseMap", albedo);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_WorkflowMode", 1f); // metallic
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", smoothScale);
            m.SetFloat("_SmoothnessTextureChannel", 0f);

            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", normalScale);
                m.EnableKeyword("_NORMALMAP");
            }
            if (mask != null)
            {
                // Mask: R metallic, G occlusion, B height, A smoothness -> works for both slots in URP Lit
                m.SetTexture("_MetallicGlossMap", mask);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetTexture("_OcclusionMap", mask);
                m.SetFloat("_OcclusionStrength", 1f);
                m.EnableKeyword("_OCCLUSIONMAP");
            }

            m.SetFloat("_Surface", 0f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 1f);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.Zero);
            m.SetFloat("_ReceiveShadows", 1f);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

            if (alphaClip)
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

            m.SetFloat("_Cull", doubleSided ? (float)CullMode.Off : (float)CullMode.Back);
            m.doubleSidedGI = doubleSided;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
        }

        static Material CreateFoliageMaterial(string root, string atlas)
        {
            // Low-poly card materials use the wind shader (falls back to URP Lit if it failed to compile)
            var shader = Shader.Find(LitShader);
            bool wind = false;
            if (System.Array.IndexOf(CardAtlases, atlas) >= 0)
            {
                var ws = Shader.Find(WindShader);
                if (ws != null && ws.isSupported && WindShaderCompiles(ws)) { shader = ws; wind = true; }
                else Debug.LogWarning("[RiceField] Wind shader not available - using URP Lit for " + atlas);
            }
            string path = $"{root}/Materials/M_{atlas}.mat";
            var existing = Load<Material>(path);
            bool switching = existing == null || existing.shader != shader;
            var m = GetOrCreateMaterial(path, shader);
            SetupLit(m, FindTex(root, "Foliage", atlas, "Albedo"), FindTex(root, "Foliage", atlas, "Normal"),
                     FindTex(root, "Foliage", atlas, "Mask"), smoothScale: 0.75f, normalScale: 0.6f,
                     alphaClip: true, doubleSided: true);
            if (wind && switching)
            {
                bool rice = atlas.StartsWith("Rice");
                // x = bend (m per unit WindZone Main), y = flutter (m), z = plant height (m), w = sway speed
                m.SetVector("_RFWind", rice ? new Vector4(0.12f, 0.012f, 1.0f, 1.6f) : new Vector4(0.10f, 0.015f, 0.6f, 2.0f));
                m.SetFloat("_RFWindWave", 0.25f);
                EditorUtility.SetDirty(m);
            }
            return m;
        }

        static int s_WindCheck; // 0 = not checked, 1 = ok, -1 = failed

        /// <summary>Compiles every pass of the wind shader once and logs any error (falls back to URP Lit on failure).</summary>
        static bool WindShaderCompiles(Shader s)
        {
            if (s_WindCheck != 0) return s_WindCheck > 0;
            bool ok = !ShaderUtil.ShaderHasError(s);
            foreach (var msg in ShaderUtil.GetShaderMessages(s))
                Debug.Log($"[RiceField] Wind shader message ({msg.severity}): {msg.message} {msg.messageDetails} ({msg.file}:{msg.line})");
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
                            Debug.LogError($"[RiceField] Wind shader {pass.Name}/{st}: {m.message} {m.messageDetails} (line {m.line})");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[RiceField] Shader compile check skipped: " + e.Message);
            }
            Debug.Log("[RiceField] Wind shader compile check: " + (ok ? "OK" : "FAILED (using URP Lit instead)"));
            s_WindCheck = ok ? 1 : -1;
            return ok;
        }

        [MenuItem("Tools/Rice Field/6) Re-check Wind Shader", priority = 40)]
        static void RecheckWind()
        {
            s_WindCheck = 0;
            RunSetup();
        }

        // Terrain "Paint Details" needs a single mesh at the prefab root (no LOD Group).
        static void CreateDetailPrefabs(string root)
        {
            EnsureFolder(root + "/Prefabs", "TerrainDetail");
            foreach (var path in ModelPaths(root))
            {
                if (path.Replace('\\', '/').Contains("/HighDetail/")) continue;
                var model = Load<GameObject>(path);
                if (model == null) continue;
                var lod0 = model.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name.EndsWith("_LOD0"));
                if (lod0 == null) continue;
                var mf = lod0.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                string name = Path.GetFileNameWithoutExtension(path) + "_Detail";
                var go = new GameObject(name);
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = lod0.sharedMaterial;
                mr.shadowCastingMode = ShadowCastingMode.On;
                PrefabUtility.SaveAsPrefabAsset(go, $"{root}/Prefabs/TerrainDetail/{name}.prefab");
                Object.DestroyImmediate(go);
            }
        }

        static Terrain SelectedTerrain()
        {
            var go = Selection.activeGameObject;
            var t = go != null ? go.GetComponent<Terrain>() : null;
            return t != null ? t : Terrain.activeTerrain;
        }

        [MenuItem("Tools/Rice Field/4) Add Low-poly Prefabs to Selected Terrain (Trees + Details)", priority = 20)]
        public static void AddToTerrain()
        {
            string root = Root;
            var terrain = SelectedTerrain();
            if (root == null || terrain == null)
            {
                EditorUtility.DisplayDialog("Rice Field", "Select a Terrain in the Hierarchy first.", "OK");
                return;
            }
            if (Load<GameObject>(root + "/Prefabs/TerrainDetail/Rice_Ripe_A_Detail.prefab") == null) RunSetup();
            var td = terrain.terrainData;
            Undo.RegisterCompleteObjectUndo(td, "Rice Field: add prototypes");

            // Trees (keep LOD Group) - for rows / Paint Trees / Mass Place Trees
            var trees = td.treePrototypes.ToList();
            int addedTrees = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { root + "/Prefabs" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.Contains("/HighDetail/") || p.Contains("/TerrainDetail/")) continue;
                var prefab = Load<GameObject>(p);
                if (prefab == null || trees.Any(t => t.prefab == prefab)) continue;
                trees.Add(new TreePrototype { prefab = prefab });
                addedTrees++;
            }
            td.treePrototypes = trees.ToArray();

            // Details (single mesh, GPU instanced) - for Paint Details
            var details = td.detailPrototypes.ToList();
            int addedDetails = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { root + "/Prefabs/TerrainDetail" }))
            {
                var prefab = Load<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null || details.Any(d => d.prototype == prefab)) continue;
                bool rice = prefab.name.StartsWith("Rice");
                details.Add(new DetailPrototype
                {
                    prototype = prefab,
                    usePrototypeMesh = true,
                    useInstancing = true,
                    renderMode = DetailRenderMode.VertexLit,
                    minWidth = 0.85f, maxWidth = 1.15f,
                    minHeight = rice ? 0.9f : 0.8f, maxHeight = rice ? 1.1f : 1.2f,
                    noiseSpread = 0.3f,
                    healthyColor = Color.white,
                    dryColor = Color.white
                });
                addedDetails++;
            }
            td.detailPrototypes = details.ToArray();
            td.RefreshPrototypes();
            if (terrain.detailObjectDistance < 120f) terrain.detailObjectDistance = 120f;
            terrain.Flush();
            EditorUtility.SetDirty(td);
            Debug.Log($"[RiceField] Added {addedTrees} tree prototypes and {addedDetails} detail prototypes to '{terrain.name}'.");
        }

        [MenuItem("Tools/Rice Field/5) Add Wind Zone to Scene", priority = 21)]
        public static void AddWindZone()
        {
            var go = new GameObject("Wind Zone (Rice Field)");
            var wz = go.AddComponent<WindZone>();
            wz.mode = WindZoneMode.Directional;
            wz.windMain = 0.6f;
            wz.windTurbulence = 0.5f;
            wz.windPulseMagnitude = 0.5f;
            wz.windPulseFrequency = 0.12f;
            go.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            Undo.RegisterCreatedObjectUndo(go, "Add Wind Zone");
            Selection.activeGameObject = go;
        }

        static Material CreateGroundMaterial(string root, string set)
        {
            var m = GetOrCreateMaterial($"{root}/Materials/M_Ground_{set}.mat", Shader.Find(LitShader));
            SetupLit(m, FindTex(root, "Ground", "Ground_" + set, "Albedo"), FindTex(root, "Ground", "Ground_" + set, "Normal"),
                     FindTex(root, "Ground", "Ground_" + set, "Mask"), smoothScale: 1f, normalScale: 1f,
                     alphaClip: false, doubleSided: false);
            // 1 texture tile = 2 m. Unity's default Plane is 10 m, so tile it 5x.
            m.SetTextureScale("_BaseMap", new Vector2(5, 5));
            return m;
        }

        public static TerrainLayer CreateTerrainLayer(string root, string set)
        {
            string path = $"{root}/TerrainLayers/TL_{set}.terrainlayer";
            var tl = Load<TerrainLayer>(path);
            bool isNew = tl == null;
            if (isNew) tl = new TerrainLayer();
            tl.diffuseTexture = FindTex(root, "Ground", "Ground_" + set, "Albedo");
            tl.normalMapTexture = FindTex(root, "Ground", "Ground_" + set, "Normal");
            tl.maskMapTexture = FindTex(root, "Ground", "Ground_" + set, "Mask");
            tl.tileSize = new Vector2(2f, 2f);
            tl.tileOffset = Vector2.zero;
            tl.normalScale = 1f;
            tl.metallic = 0f;
            tl.smoothness = 1f;
            tl.diffuseRemapMin = Vector4.zero;
            tl.diffuseRemapMax = Vector4.one;
            tl.maskMapRemapMin = Vector4.zero;
            tl.maskMapRemapMax = Vector4.one;
            if (isNew) AssetDatabase.CreateAsset(tl, path);
            EditorUtility.SetDirty(tl);
            return tl;
        }

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
                var model = Load<GameObject>(path);
                if (model == null) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                bool hd = path.Replace('\\', '/').Contains("/HighDetail/");
                string dir = hd ? root + "/Prefabs/HighDetail" : root + "/Prefabs";
                if (hd) EnsureFolder(root + "/Prefabs", "HighDetail");
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                int n = ConfigureLOD(inst, name.StartsWith("Rice"));
                foreach (var r in inst.GetComponentsInChildren<MeshRenderer>())
                {
                    // last LOD of the high-detail models does not cast shadows; low-poly LOD1 does (it is cheap)
                    r.shadowCastingMode = (n == 3 && r.name.EndsWith("_LOD2")) ? ShadowCastingMode.Off : ShadowCastingMode.On;
                    r.receiveShadows = true;
                }
                PrefabUtility.SaveAsPrefabAsset(inst, $"{dir}/{name}.prefab");
                Object.DestroyImmediate(inst);
            }
        }

        static int ConfigureLOD(GameObject go, bool rice)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            var byLod = new List<Renderer>();
            for (int i = 0; i < 8; i++)
            {
                var r = renderers.FirstOrDefault(x => x.name.EndsWith("_LOD" + i));
                if (r == null) break;
                byLod.Add(r);
            }
            if (byLod.Count < 2) return byLod.Count;
            float[] heights = byLod.Count >= 3 ? (rice ? RiceLOD3 : GrassLOD3) : (rice ? RiceLOD2 : GrassLOD2);
            var lg = go.GetComponent<LODGroup>();
            if (lg == null) lg = go.AddComponent<LODGroup>();
            int count = Mathf.Min(byLod.Count, heights.Length);
            var lods = new LOD[count];
            for (int i = 0; i < count; i++)
                lods[i] = new LOD(heights[i], new[] { byLod[i] });
            lg.SetLODs(lods);
            lg.fadeMode = LODFadeMode.None;
            lg.RecalculateBounds();
            return count;
        }

        // ------------------------------------------------------------------------------------
        // Demo scene
        // ------------------------------------------------------------------------------------
        [MenuItem("Tools/Rice Field/2) Create Demo Scene", priority = 2)]
        public static void CreateDemoScene()
        {
            string root = Root;
            if (root == null) return;
            if (Load<GameObject>(root + "/Prefabs/Rice_Ripe_A.prefab") == null) RunSetup();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(root, "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            const float size = 30f;
            const float height = 2f;

            var td = new TerrainData();
            td.heightmapResolution = 257;
            td.alphamapResolution = 512;
            td.baseMapResolution = 1024;
            td.size = new Vector3(size, height, size);
            var layers = GroundSets.Select(g => Load<TerrainLayer>($"{root}/TerrainLayers/TL_{g}.terrainlayer")).ToArray();
            td.terrainLayers = layers;
            AssetDatabase.CreateAsset(td, $"{root}/Scenes/RiceField_Demo_TerrainData.asset");

            // --- layout: dikes on the border + a cross, 4 paddies -------------------------------
            const float dike = 1.0f;
            float half = size * 0.5f;
            System.Func<float, float, float> dikeDist = (x, z) =>
            {
                float d = Mathf.Min(Mathf.Min(x, size - x), Mathf.Min(z, size - z));
                d = Mathf.Min(d, Mathf.Abs(x - half));
                d = Mathf.Min(d, Mathf.Abs(z - half));
                return d;
            };
            int hr = td.heightmapResolution;
            var heights = new float[hr, hr];
            for (int y = 0; y < hr; y++)
            for (int x = 0; x < hr; x++)
            {
                float wx = x / (float)(hr - 1) * size, wz = y / (float)(hr - 1) * size;
                float d = dikeDist(wx, wz);
                float dk = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(dike * 0.35f, dike * 0.75f, d));
                float n = Mathf.PerlinNoise(wx * 0.35f, wz * 0.35f) * 0.012f;
                heights[y, x] = 0.25f + dk * 0.15f + n;
            }
            td.SetHeights(0, 0, heights);

            int ar = td.alphamapResolution;
            int L = layers.Length;
            var maps = new float[ar, ar, L];
            int Idx(string n) => System.Array.IndexOf(GroundSets, n);
            for (int y = 0; y < ar; y++)
            for (int x = 0; x < ar; x++)
            {
                float wx = (x + 0.5f) / ar * size, wz = (y + 0.5f) / ar * size;
                var w = new float[L];
                float d = dikeDist(wx, wz);
                float n1 = Mathf.PerlinNoise(wx * 0.25f + 10, wz * 0.25f + 3);
                float n2 = Mathf.PerlinNoise(wx * 0.6f + 40, wz * 0.6f + 7);
                float dikeW = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(dike * 0.45f, dike * 0.9f, d + (n2 - 0.5f) * 0.25f));
                bool left = wx < half, bottom = wz < half;
                if (left && bottom)
                {   // ripe rice paddy: drained mud
                    w[Idx("Paddy_DampSoil")] = n1;
                    w[Idx("Paddy_WetMud")] = 1f - n1;
                }
                else if (!left && bottom)
                {   // almost-ripe paddy: still wet
                    w[Idx("Paddy_WetMud")] = n1;
                    w[Idx("Paddy_Flooded")] = Mathf.Clamp01(1f - n1 - 0.2f);
                    w[Idx("Paddy_AlgaeMud")] = Mathf.Clamp01(n2 - 0.55f) * 2f;
                }
                else if (left)
                {   // harvested: stubble, straw piles, dry cracks
                    w[Idx("Paddy_Stubble")] = 0.7f;
                    w[Idx("Paddy_StrawMat")] = Mathf.Clamp01(n2 - 0.6f) * 3f;
                    w[Idx("Paddy_DryCracked")] = Mathf.Clamp01(n1 - 0.45f) * 2f;
                }
                else
                {   // freshly plowed
                    w[Idx("Paddy_Plowed")] = 1f;
                    w[Idx("Paddy_DampSoil")] = Mathf.Clamp01(n1 - 0.6f) * 2f;
                }
                for (int i = 0; i < L; i++) w[i] *= (1f - dikeW);
                w[Idx("Dike_GrassySoil")] += dikeW;
                float sum = w.Sum();
                for (int i = 0; i < L; i++) maps[y, x, i] = sum > 0 ? w[i] / sum : (i == 0 ? 1 : 0);
            }
            td.SetAlphamaps(0, 0, maps);

            var terrainGO = Terrain.CreateTerrainGameObject(td);
            terrainGO.name = "RiceField_Terrain";
            var terrain = terrainGO.GetComponent<Terrain>();
            var tshader = Shader.Find(TerrainShader);
            if (tshader != null)
            {
                var tm = GetOrCreateMaterial($"{root}/Materials/M_Terrain_URP.mat", tshader);
                terrain.materialTemplate = tm;
            }
            terrain.treeDistance = 400f;
            terrain.treeBillboardDistance = 400f;
            terrain.treeMaximumFullLODCount = 100000;
            terrain.drawInstanced = true;

            // --- plants as terrain trees ---------------------------------------------------------
            var ripe = new[] { "Rice_Ripe_A", "Rice_Ripe_B", "Rice_Ripe_C" }.Select(n => Load<GameObject>($"{root}/Prefabs/{n}.prefab")).Where(p => p).ToList();
            var green = new[] { "Rice_AlmostRipe_A", "Rice_AlmostRipe_B", "Rice_AlmostRipe_C" }.Select(n => Load<GameObject>($"{root}/Prefabs/{n}.prefab")).Where(p => p).ToList();
            var dikeGrass = new[] { "Grass_DikeTuft_A", "Grass_DikeTuft_B", "Grass_Cogon_A", "Grass_Cogon_B", "Grass_Barnyard_A" }
                .Select(n => Load<GameObject>($"{root}/Prefabs/{n}.prefab")).Where(p => p).ToList();
            var weeds = new[] { "Grass_Barnyard_A", "Grass_Barnyard_B", "Grass_Sedge_A", "Grass_Sedge_B" }
                .Select(n => Load<GameObject>($"{root}/Prefabs/{n}.prefab")).Where(p => p).ToList();

            float m = dike * 0.9f;
            var rnd = new System.Random(1234);
            RiceFieldPlanting.PlantRows(terrain, ripe, new Rect(m, m, half - 2 * m, half - 2 * m), 0.30f, 0.28f, 0.035f, 0.9f, 1.1f, rnd);
            RiceFieldPlanting.PlantRows(terrain, green, new Rect(half + m, m, half - 2 * m, half - 2 * m), 0.30f, 0.28f, 0.035f, 0.9f, 1.1f, rnd);
            // weeds scattered in the paddies, tufts along the dikes
            RiceFieldPlanting.Scatter(terrain, weeds, new Rect(m, m, size - 2 * m, half - 2 * m), 40, 0.8f, 1.15f, rnd);
            RiceFieldPlanting.ScatterOnDikes(terrain, dikeGrass, dikeDist, dike * 0.5f, 650, 0.8f, 1.2f, rnd);
            RiceFieldPlanting.Scatter(terrain, weeds, new Rect(m, half + m, size - 2 * m, half - 2 * m), 25, 0.7f, 1.0f, rnd);
            terrain.Flush();

            // --- camera & light --------------------------------------------------------------------
            var cam = Camera.main;
            if (cam != null)
            {
                // standing on the dike crossing, looking over the ripe paddy
                cam.transform.position = new Vector3(half - 0.3f, height * 0.4f + 1.6f, half - 0.3f);
                cam.transform.rotation = Quaternion.Euler(16f, 225f, 0f);
                cam.farClipPlane = 500f;
            }
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(32f, 230f, 0f);
                sun.color = new Color(1f, 0.93f, 0.82f);
                sun.intensity = 1.6f;
                sun.shadows = LightShadows.Soft;
            }
            AddWindZone();
            string scenePath = $"{root}/Scenes/RiceField_Demo.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Selection.activeGameObject = terrainGO;
            Debug.Log("[RiceField] Demo scene saved to " + scenePath);
        }

        [MenuItem("Tools/Rice Field/3) Open Planter Window", priority = 3)]
        static void OpenPlanter() => RiceFieldPlanter.Open();
    }

    // ----------------------------------------------------------------------------------------
    public static class RiceFieldPlanting
    {
        static int ProtoIndex(TerrainData td, GameObject prefab)
        {
            var protos = td.treePrototypes.ToList();
            int i = protos.FindIndex(p => p.prefab == prefab);
            if (i >= 0) return i;
            protos.Add(new TreePrototype { prefab = prefab });
            td.treePrototypes = protos.ToArray();
            td.RefreshPrototypes();
            return protos.Count - 1;
        }

        static TreeInstance Make(Terrain t, int proto, float wx, float wz, float scale, System.Random rnd)
        {
            var size = t.terrainData.size;
            var o = t.transform.position;
            return new TreeInstance
            {
                prototypeIndex = proto,
                position = new Vector3((wx - o.x) / size.x, 0f, (wz - o.z) / size.z),
                widthScale = scale,
                heightScale = scale * (0.95f + 0.1f * (float)rnd.NextDouble()),
                rotation = (float)(rnd.NextDouble() * Mathf.PI * 2.0),
                color = Color.white,
                lightmapColor = Color.white
            };
        }

        static void Append(Terrain t, List<TreeInstance> add)
        {
            var td = t.terrainData;
            var all = td.treeInstances.ToList();
            all.AddRange(add);
            td.SetTreeInstances(all.ToArray(), true);
        }

        /// <summary>Rows of rice hills inside a rect given in terrain-local metres (x,z).</summary>
        public static int PlantRows(Terrain t, IList<GameObject> prefabs, Rect area, float rowSpacing, float hillSpacing,
                                    float jitter, float minScale, float maxScale, System.Random rnd)
        {
            if (prefabs == null || prefabs.Count == 0) return 0;
            var td = t.terrainData;
            var idx = prefabs.Select(p => ProtoIndex(td, p)).ToArray();
            var o = t.transform.position;
            var add = new List<TreeInstance>();
            for (float z = area.yMin + rowSpacing * 0.5f; z < area.yMax; z += rowSpacing)
            for (float x = area.xMin + hillSpacing * 0.5f; x < area.xMax; x += hillSpacing)
            {
                float jx = ((float)rnd.NextDouble() - 0.5f) * 2f * jitter;
                float jz = ((float)rnd.NextDouble() - 0.5f) * 2f * jitter;
                float s = Mathf.Lerp(minScale, maxScale, (float)rnd.NextDouble());
                add.Add(Make(t, idx[rnd.Next(idx.Length)], o.x + x + jx, o.z + z + jz, s, rnd));
            }
            Append(t, add);
            return add.Count;
        }

        public static int Scatter(Terrain t, IList<GameObject> prefabs, Rect area, int count, float minScale, float maxScale, System.Random rnd)
        {
            if (prefabs == null || prefabs.Count == 0) return 0;
            var td = t.terrainData;
            var idx = prefabs.Select(p => ProtoIndex(td, p)).ToArray();
            var o = t.transform.position;
            var add = new List<TreeInstance>();
            for (int i = 0; i < count; i++)
            {
                float x = area.xMin + (float)rnd.NextDouble() * area.width;
                float z = area.yMin + (float)rnd.NextDouble() * area.height;
                add.Add(Make(t, idx[rnd.Next(idx.Length)], o.x + x, o.z + z, Mathf.Lerp(minScale, maxScale, (float)rnd.NextDouble()), rnd));
            }
            Append(t, add);
            return add.Count;
        }

        public static int ScatterOnDikes(Terrain t, IList<GameObject> prefabs, System.Func<float, float, float> dikeDist,
                                         float maxDist, int count, float minScale, float maxScale, System.Random rnd)
        {
            if (prefabs == null || prefabs.Count == 0) return 0;
            var td = t.terrainData;
            var idx = prefabs.Select(p => ProtoIndex(td, p)).ToArray();
            var size = td.size;
            var o = t.transform.position;
            var add = new List<TreeInstance>();
            int guard = 0;
            while (add.Count < count && guard++ < count * 50)
            {
                float x = (float)rnd.NextDouble() * size.x, z = (float)rnd.NextDouble() * size.z;
                if (dikeDist(x, z) > maxDist) continue;
                add.Add(Make(t, idx[rnd.Next(idx.Length)], o.x + x, o.z + z, Mathf.Lerp(minScale, maxScale, (float)rnd.NextDouble()), rnd));
            }
            Append(t, add);
            return add.Count;
        }

        public static int ClearArea(Terrain t, Rect area)
        {
            var td = t.terrainData;
            var size = td.size;
            var keep = td.treeInstances.Where(ti =>
            {
                float x = ti.position.x * size.x, z = ti.position.z * size.z;
                return !area.Contains(new Vector2(x, z));
            }).ToArray();
            int removed = td.treeInstances.Length - keep.Length;
            td.SetTreeInstances(keep, true);
            return removed;
        }
    }
}
#endif
