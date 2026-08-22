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
        // The workshop half was extended 2.5 m west so the pedestrian walkway along the left
        // wall is a real route rather than a 1.9 m squeeze between the bench run and the lift
        // bays. The office half (x >= PARTX) keeps every coordinate it had.
        public const float X0 = -12.5f;          // left  wall line (workshop side)
        public const float X1 = 10f;             // right wall line (office side)
        public const float Z0 = -7f;             // FRONT facade (faces the street, -Z)
        public const float Z1 = 7f;              // back wall
        public const float BW = X1 - X0;         // 22.5  building width  (X)
        public const float BD = Z1 - Z0;         // 14    building depth  (Z)
        public const float CX = (X0 + X1) * 0.5f;// -1.25 building centre in X
        public const float BH = 6f;              // parapet height  (Y)
        public const float WT = 0.25f;           // wall thickness
        /// <summary>How far the left wall moved. Props that hug that wall shift with it.</summary>
        public const float WSHIFT = X0 + 10f;    // -2.5
        public const float FLR = 0.05f;          // interior slab top
        public const float PARTX = 3.8f;         // workshop | office partition
        public const float MEZZ = 3.15f;         // mezzanine walking level
        public const float DOOR_H = 4.2f;        // bay door clear height
        public const float LOT_HALF_X = 26f;     // site boundary
        public const float LOT_FRONT_Z = -24f;   // street side fence line
        public const float LOT_BACK_Z = 20f;     // rear fence line

        public const string ScenePath = "Assets/MainScenes/Main2_Garage/Main2_Garage.unity";
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

        static Transform _root;
        static Transform _env, _struct, _circ, _dress, _light, _actors, _play;

        /// <summary>Circulation folder: doors, stairs, ramps. Everything the player walks through.</summary>
        public static Transform Circulation { get { return _circ; } }
        public static Transform Actors { get { return _actors; } }
        public static Transform Play { get { return _play; } }
        public static Transform Dressing { get { return _dress; } }
        public static Transform Lighting { get { return _light; } }
        public static Transform Structure { get { return _struct; } }

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
            LevelKit.ResetPlaced();
            BuildMaterialLibrary();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            _root = new GameObject("=== CH2 RACETOOL GARAGE ===").transform;
            LevelKit.BuildCategories(_root);
            _env = LevelKit.Category(_root, LevelKit.Cat.Env);
            _struct = LevelKit.Category(_root, LevelKit.Cat.Structure);
            _circ = LevelKit.Category(_root, LevelKit.Cat.Circulation);
            _dress = LevelKit.Category(_root, LevelKit.Cat.Dressing);
            _light = LevelKit.Category(_root, LevelKit.Cat.Lighting);
            _actors = LevelKit.Category(_root, LevelKit.Cat.Actors);
            _play = LevelKit.Category(_root, LevelKit.Cat.Gameplay);

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
            Ch2Act2Wiring.WireGarage(_root);

            GroundProps();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Ch2Garage] Scene built and saved -> " + ScenePath);
            Debug.Log(LevelAudit.Format("CH2 RACETOOL GARAGE", LevelAudit.Run(_root, 0.45f, BH)));
        }

        /// <summary>Sits every placed prefab exactly on the slab or ground under it.</summary>
        static void GroundProps()
        {
            Physics.SyncTransforms();
            var placed = LevelKit.Placed;
            for (int i = 0; i < placed.Count; i++) LevelKit.SnapDown(placed[i]);
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
            LevelKit.UseLibrary(MatFolder, "M_G_");

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

        // ── thin wrappers over LevelKit so every builder shares one implementation ──
        static Material Mat(string key, Color c, float metallic, float smooth) { return LevelKit.Mat(key, c, metallic, smooth); }
        static Material MatTransparent(string key, Color c, float smooth) { return LevelKit.MatTransparent(key, c, smooth); }
        static Material MatEmissive(string key, Color baseColor, Color emission) { return LevelKit.MatEmissive(key, baseColor, emission); }
        public static Material M(string key) { return LevelKit.M(key); }

        public static Transform Group(string name, Transform parent) { return LevelKit.Group(name, parent == null ? _root : parent); }

        public static GameObject Box(string name, Transform parent, Vector3 centre, Vector3 size, string mat,
                                     Vector3 euler = default(Vector3), bool collider = true, bool markStatic = true)
        { return LevelKit.Box(name, parent, centre, size, mat, euler, collider, markStatic); }

        public static GameObject Cyl(string name, Transform parent, Vector3 centre, float diameter, float height,
                                     string mat, Vector3 euler = default(Vector3), bool collider = false, bool markStatic = true)
        { return LevelKit.Cyl(name, parent, centre, diameter, height, mat, euler, collider, markStatic); }

        public static GameObject Sphere(string name, Transform parent, Vector3 centre, float diameter, string mat, bool markStatic = true)
        { return LevelKit.Sphere(name, parent, centre, diameter, mat, markStatic); }

        public static GameObject Decal(string name, Transform parent, Vector3 centre, Vector2 size, string mat, float yaw = 0f)
        { return LevelKit.Decal(name, parent, centre, size, mat, yaw); }

        public static void Paint(GameObject g, string mat) { LevelKit.Paint(g, mat); }
        public static void StripCollider(GameObject g) { LevelKit.StripCollider(g); }

        public static GameObject Marker(string name, Transform parent, Vector3 pos, float yaw = 0f)
        { return LevelKit.Marker(name, parent, pos, yaw); }

        public static GameObject Sign(string name, Transform parent, Vector3 pos, Vector2 boxSize, string text,
                                      Color colour, float yaw = 180f, bool bold = true)
        { return LevelKit.Sign(name, parent, pos, boxSize, text, colour, yaw, bold); }

        public static GameObject Place(string assetPath, Transform parent, Vector3 pos, float yaw = 0f,
                                       float targetHeight = 0f, float pitch = 0f)
        { return LevelKit.Place(assetPath, parent, pos, yaw, targetHeight, pitch); }

        // ───────────────────────── lighting & atmosphere ─────────────────────────
        static void BuildLighting()
        {
            var g = Group("Atmosphere", _env);

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
            // เต็มความเข้มทำให้ผนังในเวิร์กชอปรับแสงสะท้อนจากท้องฟ้าจนแบนและซีด
            RenderSettings.reflectionIntensity = 0.45f;

            var probeGo = new GameObject("Reflection Probe (Workshop)");
            probeGo.transform.SetParent(g, false);
            probeGo.transform.localPosition = new Vector3((X0 + PARTX) * 0.5f, 2.6f, 0f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.size = new Vector3(PARTX - X0, BH, BD);
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
            var g = Group("Ground", _struct);

            // whole site + surroundings
            Box("Ground_Base", g, new Vector3(0f, -0.36f, -6f), new Vector3(160f, 0.6f, 140f), "Dirt");
            // asphalt yard
            Box("Yard_Asphalt", g, new Vector3(0f, -0.05f, -2.3f), new Vector3(56f, 0.1f, 47.4f), "Asphalt");
            // concrete apron / building pad (top = 0.03)
            Box("Pad_Concrete", g, new Vector3(CX, -0.02f, 0.6f), new Vector3(BW + 1.6f, 0.1f, BD + 3.4f), "Concrete");
            // apron slope in front of the doors
            Box("Apron_Front", g, new Vector3(-0.6f, -0.02f, Z0 - 3.4f), new Vector3(12.6f, 0.1f, 5f), "Concrete");
            // interior slab
            Box("Floor_Workshop", g, new Vector3(CX, FLR * 0.5f, 0f), new Vector3(BW - WT * 2f, FLR, BD - WT * 2f), "EpoxyFloor");
        }
    }
}
