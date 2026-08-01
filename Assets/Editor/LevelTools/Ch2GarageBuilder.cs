using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// Procedural level builder – CHAPTER 2 : "RACE TOOL", the garage owned by Kem's friend.
    /// Kem brings the evidence he found in chapter 1 here so the two of them can examine it.
    /// The level is built around that loop: arrive -> talk -> lay the evidence out on the
    /// inspection bench -> magnifier / paint chart / parts database -> order records upstairs
    /// -> pin the result on the investigation board.
    /// Everything is authored at real world 1:1 scale, all units are metres.
    ///
    /// Building envelope (from the concept sheet):
    ///     front width  20.00 m   depth 14.00 m   parapet height 6.00 m
    ///     2 x sectional bay doors 3.20 m wide x 4.20 m high (7.00 m combined opening)
    ///     office glazing 6.20 m wide, mezzanine office above it
    ///
    /// Menu:  Tools > Secrets That Breathe > Build Chapter 2 Garage Scene
    /// </summary>
    public static partial class Ch2GarageBuilder
    {
        // ───────────────────────── master dimensions ─────────────────────────
        public const float BW = 20f;             // building width  (X)
        public const float BD = 14f;             // building depth  (Z)
        public const float BH = 6f;              // parapet height  (Y)
        public const float WT = 0.25f;           // wall thickness
        public const float X0 = -BW * 0.5f;      // -10  left  wall line
        public const float X1 = BW * 0.5f;       // +10  right wall line
        public const float Z0 = -BD * 0.5f;      //  -7  FRONT facade (faces the street, -Z)
        public const float Z1 = BD * 0.5f;       //  +7  back wall
        public const float FLR = 0.05f;          // interior slab top
        public const float PARTX = 3.8f;         // workshop | office partition
        public const float MEZZ = 3.15f;         // mezzanine walking level
        public const float DOOR_H = 4.2f;        // bay door clear height
        public const float LOT_HALF_X = 26f;     // site boundary
        public const float LOT_FRONT_Z = -24f;   // street side fence line
        public const float LOT_BACK_Z = 20f;     // rear fence line

        public const string ScenePath = "Assets/MainScenes/Main2_Garage.unity";
        public const string DataFolder = "Assets/MainScenes/Main2_Garage";
        public const string MatFolder = "Assets/MainScenes/Main2_Garage/Materials";

        // asset paths reused from the project
        const string P_PLAYER = "Assets/Champ&Kichzz/Prefab/Player/player.prefab";
        const string P_CAR_A = "Assets/Palm_Art/Model/Car01/Car01.1.fbx";
        const string P_CAR_B = "Assets/Palm_Art/Model/Car02/CarDE02.3.fbx";
        const string P_DRUM = "Assets/Champ&Kichzz/Street Assets/Prefabs/SA_OilDrum_01.prefab";
        const string P_DRUM2 = "Assets/Champ&Kichzz/Street Assets/Prefabs/SA_OilDrum_02.prefab";
        const string P_CONE = "Assets/Champ&Kichzz/Street Assets/Prefabs/SA_TrafficCone_01.prefab";
        const string P_GASCAN = "Assets/Champ&Kichzz/Street Assets/Prefabs/SA_GasCan_01.prefab";
        const string P_BARRIER = "Assets/Champ&Kichzz/Street Assets/Prefabs/SA_TrafficBarrier_01.prefab";

        static readonly Dictionary<string, Material> _mat = new Dictionary<string, Material>();
        static Transform _root;

        public enum Mood { Day, Evening }
        /// <summary>Chapter 2 plays out after closing time, so the level ships in Evening mood.</summary>
        public static Mood LightMood = Mood.Evening;

        // ───────────────────────── entry point ─────────────────────────
        [MenuItem("Tools/Secrets That Breathe/Build Chapter 2 Garage Scene", false, 10)]
        public static void BuildScene() { BuildScene(true); }

        /// <summary>Rebuilds the whole level from scratch. askToSave=false skips the "save current scene?" prompt.</summary>
        public static void BuildScene(bool askToSave)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Ch2Garage] Leave play mode before building.");
                return;
            }
            if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            BuildMaterialLibrary();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            _root = new GameObject("=== CH2 RACETOOL GARAGE ===").transform;

            BuildLighting();
            BuildGround();
            BuildShell();
            BuildFacade();
            BuildInteriorStructure();
            BuildWorkshopProps();
            BuildOffice();
            BuildExterior();
            BuildVehicles();
            BuildGameplay();
            ApplyMood();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Ch2Garage] Scene built and saved -> " + ScenePath);
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder))
                AssetDatabase.CreateFolder("Assets/MainScenes", "Main2_Garage");
            if (!AssetDatabase.IsValidFolder(MatFolder))
                AssetDatabase.CreateFolder(DataFolder, "Materials");
        }

        // ───────────────────────── material library ─────────────────────────
        static void BuildMaterialLibrary()
        {
            _mat.Clear();

            Mat("Asphalt", new Color(0.115f, 0.117f, 0.125f), 0f, 0.14f);
            Mat("AsphaltRoad", new Color(0.145f, 0.148f, 0.158f), 0f, 0.22f);
            Mat("Concrete", new Color(0.60f, 0.595f, 0.575f), 0f, 0.16f);
            Mat("ConcreteDark", new Color(0.30f, 0.30f, 0.295f), 0f, 0.18f);
            Mat("Curb", new Color(0.72f, 0.71f, 0.69f), 0f, 0.20f);
            Mat("EpoxyFloor", new Color(0.355f, 0.375f, 0.395f), 0f, 0.62f);
            Mat("PanelBlack", new Color(0.048f, 0.049f, 0.053f), 0.20f, 0.42f);
            Mat("PanelDark", new Color(0.095f, 0.098f, 0.105f), 0.25f, 0.45f);
            Mat("BrandRed", new Color(0.72f, 0.055f, 0.085f), 0.10f, 0.70f);
            Mat("White", new Color(0.87f, 0.87f, 0.855f), 0f, 0.45f);
            Mat("OffWhite", new Color(0.78f, 0.78f, 0.77f), 0f, 0.35f);
            Mat("Steel", new Color(0.55f, 0.56f, 0.575f), 0.90f, 0.55f);
            Mat("SteelDark", new Color(0.20f, 0.21f, 0.225f), 0.85f, 0.42f);
            Mat("Alu", new Color(0.70f, 0.715f, 0.735f), 0.88f, 0.72f);
            Mat("Rubber", new Color(0.042f, 0.042f, 0.046f), 0f, 0.26f);
            Mat("Yellow", new Color(0.82f, 0.60f, 0.04f), 0f, 0.42f);
            Mat("SafetyGreen", new Color(0.06f, 0.42f, 0.16f), 0f, 0.40f);
            Mat("LineWhite", new Color(0.82f, 0.82f, 0.80f), 0f, 0.30f);
            Mat("Wood", new Color(0.40f, 0.27f, 0.155f), 0f, 0.30f);
            Mat("ToolRed", new Color(0.55f, 0.075f, 0.07f), 0.35f, 0.58f);
            Mat("Cardboard", new Color(0.47f, 0.36f, 0.235f), 0f, 0.25f);
            Mat("Tarp", new Color(0.26f, 0.27f, 0.285f), 0f, 0.22f);
            Mat("Dirt", new Color(0.24f, 0.215f, 0.175f), 0f, 0.15f);
            Mat("Grass", new Color(0.145f, 0.215f, 0.105f), 0f, 0.15f);
            Mat("CarRed", new Color(0.46f, 0.015f, 0.025f), 0.30f, 0.88f);
            Mat("Rust", new Color(0.32f, 0.17f, 0.09f), 0.15f, 0.20f);
            Mat("Copper", new Color(0.55f, 0.30f, 0.15f), 0.85f, 0.60f);
            Mat("Stainless", new Color(0.76f, 0.77f, 0.79f), 1.0f, 0.78f);
            Mat("Cork", new Color(0.60f, 0.44f, 0.26f), 0f, 0.20f);
            Mat("Fabric", new Color(0.30f, 0.24f, 0.22f), 0f, 0.18f);
            Mat("Paper", new Color(0.92f, 0.91f, 0.88f), 0f, 0.30f);
            Mat("StringRed", new Color(0.68f, 0.05f, 0.06f), 0f, 0.30f);

            MatTransparent("Glass", new Color(0.52f, 0.63f, 0.66f, 0.26f), 0.95f);
            MatTransparent("GlassDark", new Color(0.10f, 0.135f, 0.15f, 0.62f), 0.92f);
            MatTransparent("GlassOffice", new Color(0.46f, 0.60f, 0.63f, 0.34f), 0.94f);

            MatEmissive("LampWhite", new Color(0.95f, 0.96f, 0.98f), new Color(1f, 0.98f, 0.92f) * 3.2f);
            MatEmissive("LampRed", new Color(0.70f, 0.05f, 0.06f), new Color(1f, 0.06f, 0.08f) * 3.0f);
            MatEmissive("LampGreen", new Color(0.10f, 0.55f, 0.18f), new Color(0.15f, 1f, 0.30f) * 2.2f);
            MatEmissive("ScreenBlue", new Color(0.12f, 0.18f, 0.26f), new Color(0.30f, 0.55f, 0.95f) * 1.6f);
            MatEmissive("SignFace", new Color(0.88f, 0.88f, 0.88f), new Color(1f, 0.97f, 0.90f) * 1.4f);
        }

        static Material Mat(string key, Color c, float metallic, float smooth)
        {
            var m = LoadOrCreate(key);
            m.SetColor("_BaseColor", c);
            m.SetColor("_Color", c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            _mat[key] = m;
            return m;
        }

        static Material MatTransparent(string key, Color c, float smooth)
        {
            var m = Mat(key, c, 0f, smooth);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_AlphaClip", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material MatEmissive(string key, Color baseColor, Color emission)
        {
            var m = Mat(key, baseColor, 0f, 0.5f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material LoadOrCreate(string key)
        {
            string path = MatFolder + "/M_G_" + key + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        public static Material M(string key)
        {
            Material m;
            if (_mat.TryGetValue(key, out m) && m != null) return m;
            m = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/M_G_" + key + ".mat");
            if (m != null) _mat[key] = m;
            return m;
        }

        // ───────────────────────── primitive helpers ─────────────────────────
        public static Transform Group(string name, Transform parent)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent == null ? _root : parent, false);
            return g.transform;
        }

        public static GameObject Box(string name, Transform parent, Vector3 centre, Vector3 size, string mat,
                                     Vector3 euler = default(Vector3), bool collider = true, bool markStatic = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = centre;
            g.transform.localEulerAngles = euler;
            g.transform.localScale = size;
            Paint(g, mat);
            if (!collider) StripCollider(g);
            if (markStatic) g.isStatic = true;
            return g;
        }

        /// <summary>Cylinder with real diameter/height (Unity cylinder is 1 wide, 2 tall).</summary>
        public static GameObject Cyl(string name, Transform parent, Vector3 centre, float diameter, float height,
                                     string mat, Vector3 euler = default(Vector3), bool collider = false, bool markStatic = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = centre;
            g.transform.localEulerAngles = euler;
            g.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            Paint(g, mat);
            StripCollider(g);
            if (collider)
            {
                var bc = g.AddComponent<BoxCollider>();
                bc.size = new Vector3(1f, 2f, 1f);
            }
            if (markStatic) g.isStatic = true;
            return g;
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 centre, float diameter, string mat,
                                        bool markStatic = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = centre;
            g.transform.localScale = Vector3.one * diameter;
            Paint(g, mat);
            StripCollider(g);
            if (markStatic) g.isStatic = true;
            return g;
        }

        /// <summary>Flat quad used for floor markings / decals. Lies in the XZ plane by default.</summary>
        public static GameObject Decal(string name, Transform parent, Vector3 centre, Vector2 size, string mat, float yaw = 0f)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = centre;
            g.transform.localEulerAngles = new Vector3(-90f, yaw, 0f);
            g.transform.localScale = new Vector3(size.x, size.y, 1f);
            Paint(g, mat);
            StripCollider(g);
            g.isStatic = true;
            return g;
        }

        public static void Paint(GameObject g, string mat)
        {
            var m = M(mat);
            if (m == null) return;
            var r = g.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }

        public static void StripCollider(GameObject g)
        {
            var c = g.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        public static GameObject Marker(string name, Transform parent, Vector3 pos, float yaw = 0f)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localEulerAngles = new Vector3(0f, yaw, 0f);
            return g;
        }

        /// <summary>Text sign built with TextMeshPro, auto fitted into a box of the given size.</summary>
        public static GameObject Sign(string name, Transform parent, Vector3 pos, Vector2 boxSize, string text,
                                      Color colour, float yaw = 180f, bool bold = true)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var tmp = g.AddComponent<TMPro.TextMeshPro>();
            if (tmp.font == null)
            {
                var f = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (f != null) tmp.font = f;
            }
            var rt = g.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = boxSize;
            tmp.text = text;
            tmp.color = colour;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.5f;
            tmp.fontSizeMax = 300f;
            tmp.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            tmp.characterSpacing = 4f;
            g.transform.localPosition = pos;
            // TMP 3D text is readable from its -Z side, so a "facing" yaw needs the extra half turn
            g.transform.localEulerAngles = new Vector3(0f, yaw + 180f, 0f);
            g.isStatic = true;
            return g;
        }

        /// <summary>Instantiates a project prefab/fbx, drops it on the ground and optionally rescales it to a target height.</summary>
        public static GameObject Place(string assetPath, Transform parent, Vector3 pos, float yaw = 0f,
                                       float targetHeight = 0f, float pitch = 0f, bool centreXZ = true)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (src == null)
            {
                Debug.LogWarning("[Ch2Garage] missing asset: " + assetPath);
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            if (go == null) return null;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localEulerAngles = new Vector3(pitch, yaw, 0f);

            Bounds b;
            if (!TryBounds(go, out b)) { go.transform.localPosition = pos; return go; }

            if (targetHeight > 0f && b.size.y > 0.0001f)
            {
                float k = targetHeight / b.size.y;
                go.transform.localScale = go.transform.localScale * k;
                TryBounds(go, out b);
            }

            // world-space bounds -> parent space, so nested/rotated groups still line up
            Vector3 cW = b.center;
            Vector3 baseW = new Vector3(b.center.x, b.min.y, b.center.z);
            Vector3 cL = parent != null ? parent.InverseTransformPoint(cW) : cW;
            Vector3 baseL = parent != null ? parent.InverseTransformPoint(baseW) : baseW;
            go.transform.localPosition += new Vector3(pos.x - cL.x, pos.y - baseL.y, pos.z - cL.z);
            return go;
        }

        static bool TryBounds(GameObject go, out Bounds b)
        {
            b = new Bounds(go.transform.position, Vector3.zero);
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return false;
            b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return true;
        }

        // ───────────────────────── lighting & atmosphere ─────────────────────────
        static void BuildLighting()
        {
            var g = Group("ENV_Lighting", _root);

            var sunGo = new GameObject("Sun (Directional)");
            sunGo.transform.SetParent(g, false);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            RenderSettings.sun = sun;

            // soft bounce fill so interiors are not pitch black without a bake
            var fillGo = new GameObject("Sky Fill (Directional)");
            fillGo.transform.SetParent(g, false);
            fillGo.transform.localEulerAngles = new Vector3(140f, 40f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.shadows = LightShadows.None;

            var sky = AssetDatabase.LoadAssetAtPath<Material>(DataFolder + "/M_G_Sky.mat");
            if (sky == null)
            {
                var sh = Shader.Find("Skybox/Procedural");
                if (sh != null)
                {
                    sky = new Material(sh);
                    AssetDatabase.CreateAsset(sky, DataFolder + "/M_G_Sky.mat");
                }
            }
            if (sky != null)
            {
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_AtmosphereThickness", 1.05f);
                sky.SetColor("_GroundColor", new Color(0.24f, 0.24f, 0.24f));
                RenderSettings.skybox = sky;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            var probeGo = new GameObject("Reflection Probe (Workshop)");
            probeGo.transform.SetParent(g, false);
            probeGo.transform.localPosition = new Vector3(-3f, 2.6f, 0f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.size = new Vector3(BW, BH, BD);
            probe.resolution = 128;
            probe.boxProjection = true;
        }

        [MenuItem("Tools/Secrets That Breathe/Ch2 Lighting/Evening (story default)", false, 30)]
        static void MoodEvening() { LightMood = Mood.Evening; ApplyMood(); }

        [MenuItem("Tools/Secrets That Breathe/Ch2 Lighting/Day", false, 31)]
        static void MoodDay() { LightMood = Mood.Day; ApplyMood(); }

        /// <summary>Re-times the whole scene without rebuilding it, so hand edits survive.</summary>
        public static void ApplyMood()
        {
            bool evening = LightMood == Mood.Evening;

            if (evening)
            {
                RenderSettings.ambientSkyColor = new Color(0.20f, 0.24f, 0.33f);
                RenderSettings.ambientEquatorColor = new Color(0.15f, 0.155f, 0.175f);
                RenderSettings.ambientGroundColor = new Color(0.07f, 0.068f, 0.065f);
                RenderSettings.fogColor = new Color(0.16f, 0.175f, 0.215f);
                RenderSettings.fogDensity = 0.0090f;
            }
            else
            {
                RenderSettings.ambientSkyColor = new Color(0.50f, 0.56f, 0.66f);
                RenderSettings.ambientEquatorColor = new Color(0.38f, 0.39f, 0.41f);
                RenderSettings.ambientGroundColor = new Color(0.18f, 0.175f, 0.17f);
                RenderSettings.fogColor = new Color(0.62f, 0.66f, 0.72f);
                RenderSettings.fogDensity = 0.0055f;
            }

            var sky = RenderSettings.skybox;
            if (sky != null)
            {
                sky.SetColor("_SkyTint", evening ? new Color(0.30f, 0.33f, 0.46f) : new Color(0.55f, 0.62f, 0.72f));
                sky.SetFloat("_Exposure", evening ? 0.55f : 1.15f);
                sky.SetFloat("_AtmosphereThickness", evening ? 1.6f : 1.05f);
                EditorUtility.SetDirty(sky);
            }

            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                var l = lights[i];
                string n = l.gameObject.name;
                string pn = l.transform.parent != null ? l.transform.parent.name : "";

                if (l.type == LightType.Directional)
                {
                    if (n.StartsWith("Sun"))
                    {
                        l.transform.eulerAngles = evening ? new Vector3(9f, -104f, 0f) : new Vector3(46f, -132f, 0f);
                        l.color = evening ? new Color(1f, 0.68f, 0.42f) : new Color(1f, 0.955f, 0.885f);
                        l.intensity = evening ? 0.42f : 1.45f;
                    }
                    else
                    {
                        l.color = evening ? new Color(0.34f, 0.42f, 0.62f) : new Color(0.62f, 0.70f, 0.86f);
                        l.intensity = evening ? 0.16f : 0.28f;
                    }
                    continue;
                }

                bool exterior = pn.StartsWith("Pole_") || pn.StartsWith("WallPack") || pn.Contains("Canopy");
                if (exterior)
                {
                    l.enabled = evening;
                    l.intensity = pn.StartsWith("Pole_") ? 3.4f : 2.4f;
                }
                else if (pn.StartsWith("Insp_"))          // the inspection bench task lights stay punchy
                {
                    l.intensity = evening ? 4.2f : 3.4f;
                }
                else
                {
                    l.intensity = evening ? 3.1f : 2.3f;  // shop + office fittings
                }
            }
            Debug.Log("[Ch2Garage] lighting mood -> " + LightMood);
        }

        // ───────────────────────── site ground ─────────────────────────
        static void BuildGround()
        {
            var g = Group("ENV_Ground", _root);

            // whole site + surroundings
            Box("Ground_Base", g, new Vector3(0f, -0.36f, -6f), new Vector3(160f, 0.6f, 140f), "Dirt");
            // asphalt yard
            Box("Yard_Asphalt", g, new Vector3(0f, -0.05f, -2.3f), new Vector3(56f, 0.1f, 47.4f), "Asphalt");
            // concrete apron / building pad (top = 0.03)
            Box("Pad_Concrete", g, new Vector3(0f, -0.02f, 0.6f), new Vector3(BW + 1.6f, 0.1f, BD + 3.4f), "Concrete");
            // apron slope in front of the doors
            Box("Apron_Front", g, new Vector3(-0.6f, -0.02f, Z0 - 3.4f), new Vector3(12.6f, 0.1f, 5f), "Concrete");
            // interior slab
            Box("Floor_Workshop", g, new Vector3(0f, FLR * 0.5f, 0f), new Vector3(BW - WT * 2f, FLR, BD - WT * 2f), "EpoxyFloor");
        }
    }
}
