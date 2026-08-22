using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using K = SecretsThatBreathe.LevelTools.LevelKit;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// CHAPTER 2 – UNDERGROUND CAR PARK (B1) beneath the QUD CLUB.
    /// The player drives/walks down the ramp, sneaks north through the parking rows past
    /// the club's security, examines the target car, and reaches the club entrance at the
    /// far end – which is the load point into the club scene.
    ///
    /// Layout (looking down, +Z = club entrance):
    ///     x -22..-17 bays A   | -17..-11 aisle W | -11..-6 bays B |
    ///     x  -6.. 6  MAIN APPROACH LANE (12 m)                    |
    ///     x   6..11  bays B'  |  11..17 aisle E  |  17..22 bays A'
    ///     z -18 ramp foot ................................ z +20 club entrance
    ///
    /// Menu: Tools ▸ Secrets That Breathe ▸ Build Chapter 2 Parking B1
    /// </summary>
    public static class Ch2ParkingBuilder
    {
        // ── master dimensions (metres) ──
        public const float HX = 22f;          // half width  -> 44 m
        public const float ZS = -18f;         // south edge (ramp foot)
        public const float ZN = 20f;          // north edge (club facade)
        public const float CH = 3.4f;         // clear ceiling height (beam soffits still clear 2.8 m)
        public const float SLAB = 0.45f;      // structural slab above
        public const float LANE = 6f;         // half width of the main approach lane
        public const float BAY_W = 2.6f;      // parking bay width
        public const float BAY_D = 5.0f;      // parking bay depth

        // ramp
        /// <summary>Column grid. Kept clear of the 5 m bays so cars never intersect a column.</summary>
        public static readonly float[] COL_X = { -16.3f, -5.3f, 5.3f, 16.3f };
        public static readonly float[] COL_Z = { -14f, -7f, 0f, 7f, 13.5f };

        public const float RAMP_X0 = 14f, RAMP_X1 = 21f;
        public const float RAMP_Z_BOT = -18f, RAMP_Z_TOP = -40f;
        public const float STREET_Y = CH + SLAB;   // the ramp has to climb the full structural depth

        public const string ScenePath = "Assets/MainScenes/Main2_ParkingB1/Main2_ParkingB1.unity";
        public const string DataFolder = "Assets/MainScenes/Main2_ParkingB1";
        public const string MatFolder = DataFolder + "/Materials";

        const string P_CAR_A = "Assets/Palm_Art/Model/Car01/Car01.1.fbx";
        const string P_CAR_B = "Assets/Palm_Art/Model/Car02/CarDE02.3.fbx";
        const string P_PLAYER = "Assets/Champ&Kichzz/Prefab/Player/player.prefab";
        const string P_NPC = "Assets/Champ&Kichzz/Prefab/Npc/NPC.prefab";
        const string P_CONE = "Assets/Champ&Kichzz/Street Assets/Prefabs/SA_TrafficCone_01.prefab";
        const string P_BARRIER = "Assets/Champ&Kichzz/Street Assets/Prefabs/SA_TrafficBarrier_01.prefab";

        static Transform _root;
        static Transform _env, _struct, _circ, _dress, _light, _actors, _play;

        [MenuItem("Tools/Secrets That Breathe/Build Chapter 2 Parking B1", false, 11)]
        public static void BuildScene() { BuildScene(true); }

        public static void BuildScene(bool askToSave)
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Ch2Parking] leave play mode first."); return; }
            if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            K.EnsureFolder(MatFolder);
            K.ResetPlaced();
            Materials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _root = new GameObject("=== CH2 PARKING B1 ===").transform;
            K.BuildCategories(_root);
            _env = K.Category(_root, K.Cat.Env);
            _struct = K.Category(_root, K.Cat.Structure);
            _circ = K.Category(_root, K.Cat.Circulation);
            _dress = K.Category(_root, K.Cat.Dressing);
            _light = K.Category(_root, K.Cat.Lighting);
            _actors = K.Category(_root, K.Cat.Actors);
            _play = K.Category(_root, K.Cat.Gameplay);

            Atmosphere();
            Structure();
            Ramp();
            ParkingRows();
            FloorGraphics();
            Services();
            ClubEntrance();
            Vehicles();
            Gameplay();
            Ch2Act2Wiring.WireParking(_root);

            GroundProps();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Ch2Parking] built -> " + ScenePath);
            Debug.Log(LevelAudit.Format("CH2 PARKING B1", LevelAudit.Run(_root, 0.45f, STREET_Y + 0.4f)));
        }

        /// <summary>Sits every placed prefab exactly on the slab it is standing on.</summary>
        static void GroundProps()
        {
            Physics.SyncTransforms();
            var placed = K.Placed;
            for (int i = 0; i < placed.Count; i++) K.SnapDown(placed[i]);
        }

        static void Materials()
        {
            K.UseLibrary(MatFolder, "M_P_");
            K.Mat("Floor", new Color(0.105f, 0.108f, 0.118f), 0.05f, 0.60f);   // wet polished concrete
            K.Mat("FloorDry", new Color(0.155f, 0.155f, 0.16f), 0f, 0.28f);
            K.Mat("Wall", new Color(0.235f, 0.235f, 0.245f), 0f, 0.22f);
            K.Mat("WallDirty", new Color(0.165f, 0.165f, 0.175f), 0f, 0.18f);
            K.Mat("Ceiling", new Color(0.135f, 0.135f, 0.145f), 0f, 0.15f);
            K.Mat("Column", new Color(0.28f, 0.28f, 0.29f), 0f, 0.25f);
            K.Mat("Steel", new Color(0.5f, 0.51f, 0.53f), 0.9f, 0.5f);
            K.Mat("SteelDark", new Color(0.16f, 0.165f, 0.18f), 0.8f, 0.4f);
            K.Mat("Alu", new Color(0.68f, 0.69f, 0.71f), 0.85f, 0.7f);
            K.Mat("Rubber", new Color(0.04f, 0.04f, 0.045f), 0f, 0.25f);
            K.Mat("LineWhite", new Color(0.80f, 0.80f, 0.78f), 0f, 0.35f);
            K.Mat("LineYellow", new Color(0.78f, 0.60f, 0.06f), 0f, 0.35f);
            K.Mat("HazardBlack", new Color(0.05f, 0.05f, 0.055f), 0f, 0.3f);
            K.Mat("Carpet", new Color(0.33f, 0.02f, 0.04f), 0f, 0.14f);
            K.Mat("Granite", new Color(0.055f, 0.055f, 0.065f), 0.1f, 0.62f);
            K.Mat("Brass", new Color(0.62f, 0.45f, 0.16f), 0.9f, 0.68f);
            K.Mat("Velvet", new Color(0.30f, 0.02f, 0.10f), 0f, 0.12f);
            K.Mat("CarRed", new Color(0.44f, 0.012f, 0.02f), 0.3f, 0.9f);
            K.Mat("Puddle", new Color(0.085f, 0.088f, 0.10f), 0.25f, 0.96f);
            K.Mat("Dirt", new Color(0.14f, 0.135f, 0.13f), 0f, 0.12f);
            K.Mat("Asphalt", new Color(0.12f, 0.12f, 0.13f), 0f, 0.2f);

            K.MatTransparent("Glass", new Color(0.35f, 0.42f, 0.5f, 0.3f), 0.94f);
            K.MatTransparent("GlassDark", new Color(0.06f, 0.07f, 0.09f, 0.66f), 0.92f);

            K.MatEmissive("NeonMagenta", new Color(0.75f, 0.10f, 0.55f), new Color(1f, 0.06f, 0.62f) * 5.0f);
            K.MatEmissive("NeonCyan", new Color(0.10f, 0.62f, 0.78f), new Color(0.10f, 0.80f, 1f) * 4.2f);
            K.MatEmissive("NeonPurple", new Color(0.42f, 0.12f, 0.80f), new Color(0.55f, 0.15f, 1f) * 4.0f);
            K.MatEmissive("NeonRed", new Color(0.72f, 0.06f, 0.08f), new Color(1f, 0.08f, 0.10f) * 4.0f);
            K.MatEmissive("LampWhite", new Color(0.92f, 0.93f, 0.96f), new Color(0.85f, 0.92f, 1f) * 2.6f);
            K.MatEmissive("ExitGreen", new Color(0.10f, 0.55f, 0.20f), new Color(0.18f, 1f, 0.35f) * 2.4f);
            K.MatEmissive("SignAmber", new Color(0.70f, 0.45f, 0.06f), new Color(1f, 0.62f, 0.10f) * 2.0f);
        }

        // ───────────────────────── atmosphere ─────────────────────────
        static void Atmosphere()
        {
            var g = K.Group("Atmosphere", _env);

            // faint night sky spilling down the ramp only
            var moon = K.AddLight(g, "RampSpill (Directional)", new Vector3(0f, 8f, -34f), new Vector3(58f, 12f, 0f),
                                  LightType.Directional, new Color(0.42f, 0.52f, 0.78f), 0.35f, 40f, 60f, true);
            RenderSettings.sun = moon;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.085f, 0.075f, 0.115f);
            RenderSettings.ambientEquatorColor = new Color(0.065f, 0.06f, 0.082f);
            RenderSettings.ambientGroundColor = new Color(0.035f, 0.032f, 0.042f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.075f, 0.055f, 0.11f);
            RenderSettings.fogDensity = 0.017f;
            // ค่า default 1 ทำให้ทุกพื้นผิวรับแสงสะท้อนจาก environment เต็มๆ
            // ลานจอดใต้ดินตอนกลางคืนเลยสว่างเท่ากลางวัน และความมืดที่การลอบเร้นต้องใช้หายไปหมด
            RenderSettings.reflectionIntensity = 0.12f;

            // a very dark night sky, only ever seen up the ramp mouth
            var sky = AssetDatabase.LoadAssetAtPath<Material>(DataFolder + "/M_P_NightSky.mat");
            if (sky == null)
            {
                var sh = Shader.Find("Skybox/Procedural");
                if (sh != null) { sky = new Material(sh); AssetDatabase.CreateAsset(sky, DataFolder + "/M_P_NightSky.mat"); }
            }
            if (sky != null)
            {
                sky.SetFloat("_SunSize", 0.01f);
                sky.SetFloat("_AtmosphereThickness", 0.4f);
                sky.SetColor("_SkyTint", new Color(0.10f, 0.09f, 0.18f));
                sky.SetColor("_GroundColor", new Color(0.04f, 0.04f, 0.05f));
                sky.SetFloat("_Exposure", 0.35f);
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
            }

            var probe = new GameObject("Reflection Probe");
            probe.transform.SetParent(g, false);
            probe.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var rp = probe.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Realtime;
            rp.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            rp.size = new Vector3(HX * 2f, CH, ZN - ZS);
            rp.boxProjection = true;
            rp.resolution = 128;
            // ด่านนี้มีไฟกว่า 30 ดวง probe เต็มความเข้มจะสะท้อนกลับมาจนลานจอดสว่างเท่ากลางวัน
            // หรี่ลงเพื่อคงความมืดที่การลอบเร้นต้องพึ่ง
            rp.intensity = 0.32f;
        }

        // ───────────────────────── shell ─────────────────────────
        static void Structure()
        {
            var g = K.Group("Shell", _struct);
            float cz = (ZS + ZN) * 0.5f, dz = ZN - ZS;

            K.Box("Floor_Slab", g, new Vector3(0f, -0.2f, cz), new Vector3(HX * 2f, 0.4f, dz), "Floor");
            K.Box("Ceiling_Slab", g, new Vector3(0f, CH + SLAB * 0.5f, cz), new Vector3(HX * 2f, SLAB, dz), "Ceiling");

            K.Box("Wall_W", g, new Vector3(-HX - 0.15f, CH * 0.5f, cz), new Vector3(0.3f, CH + SLAB, dz), "Wall");
            K.Box("Wall_E", g, new Vector3(HX + 0.15f, CH * 0.5f, cz), new Vector3(0.3f, CH + SLAB, dz), "Wall");
            // the north wall is punched for the club entrance, matching the hole in the facade
            K.WallWithOpening("Wall_N", g, new Vector3(0f, 0f, ZN + 0.15f), HX * 2f + 0.6f, CH + SLAB, 0.3f, "Wall",
                              0f, ENTRY_CLEAR_W, ENTRY_CLEAR_H);
            // south wall, with the ramp opening at x 14..21
            K.Box("Wall_S_A", g, new Vector3(-4.5f, CH * 0.5f, ZS - 0.15f), new Vector3(35f, CH + SLAB, 0.3f), "Wall");
            K.Box("Wall_S_B", g, new Vector3(21.5f, CH * 0.5f, ZS - 0.15f), new Vector3(1f, CH + SLAB, 0.3f), "Wall");

            // Columns sit in the aisles and the lane, never inside a bay. At the old x the
            // column head overlapped the last half metre of every bay, so each parked car
            // ended up buried in one.
            var col = K.Group("Columns", g);
            float[] xs = COL_X;
            float[] zs = COL_Z;
            for (int a = 0; a < xs.Length; a++)
                for (int b = 0; b < zs.Length; b++)
                    Column(col, "Col_" + a + "_" + b, new Vector3(xs[a], 0f, zs[b]));

            // downstand beams between the columns
            var bm = K.Group("Beams", g);
            for (int b = 0; b < zs.Length; b++)
                K.Box("Beam_Z" + b, bm, new Vector3(0f, CH - 0.28f, zs[b]), new Vector3(HX * 2f, 0.55f, 0.5f), "Ceiling", default(Vector3), false);
            for (int a = 0; a < xs.Length; a++)
                K.Box("Beam_X" + a, bm, new Vector3(xs[a], CH - 0.18f, cz), new Vector3(0.45f, 0.35f, dz), "Ceiling", default(Vector3), false);

            // wall skirt + dirt band
            K.Box("Skirt_W", g, new Vector3(-HX + 0.02f, 0.5f, cz), new Vector3(0.06f, 1.0f, dz), "WallDirty", default(Vector3), false);
            K.Box("Skirt_E", g, new Vector3(HX - 0.02f, 0.5f, cz), new Vector3(0.06f, 1.0f, dz), "WallDirty", default(Vector3), false);
        }

        static void Column(Transform parent, string name, Vector3 p)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Box("Shaft", g, new Vector3(0f, CH * 0.5f, 0f), new Vector3(0.65f, CH, 0.65f), "Column");
            K.Box("Head", g, new Vector3(0f, CH - 0.12f, 0f), new Vector3(0.95f, 0.24f, 0.95f), "Column", default(Vector3), false);
            // yellow/black hazard wrap
            for (int i = 0; i < 5; i++)
                K.Box("Hazard_" + i, g, new Vector3(0f, 0.22f + i * 0.16f, 0f), new Vector3(0.68f, 0.08f, 0.68f),
                      i % 2 == 0 ? "LineYellow" : "HazardBlack", default(Vector3), false);
            K.Box("Kerb", g, new Vector3(0f, 0.06f, 0f), new Vector3(0.95f, 0.12f, 0.95f), "WallDirty", default(Vector3), false);
        }

        // ───────────────────────── entry ramp ─────────────────────────
        static void Ramp()
        {
            var g = K.Group("Ramp", _circ);
            float len = RAMP_Z_BOT - RAMP_Z_TOP;                 // 22 m
            float cxr = (RAMP_X0 + RAMP_X1) * 0.5f;
            float czr = (RAMP_Z_BOT + RAMP_Z_TOP) * 0.5f;
            float pitch = Mathf.Atan2(STREET_Y, len) * Mathf.Rad2Deg;   // ≈ 8.9°, ~15 %

            K.Box("Ramp_Deck", g, new Vector3(cxr, STREET_Y * 0.5f - 0.1f, czr),
                  new Vector3(RAMP_X1 - RAMP_X0, 0.35f, Mathf.Sqrt(len * len + STREET_Y * STREET_Y)),
                  "Asphalt", new Vector3(-pitch, 0f, 0f));
            K.Box("Ramp_Wall_W", g, new Vector3(RAMP_X0 - 0.2f, STREET_Y * 0.5f + 0.6f, czr),
                  new Vector3(0.4f, 2.4f, Mathf.Sqrt(len * len + STREET_Y * STREET_Y)), "Wall", new Vector3(-pitch, 0f, 0f));
            K.Box("Ramp_Wall_E", g, new Vector3(RAMP_X1 + 0.2f, STREET_Y * 0.5f + 0.6f, czr),
                  new Vector3(0.4f, 2.4f, Mathf.Sqrt(len * len + STREET_Y * STREET_Y)), "Wall", new Vector3(-pitch, 0f, 0f));
            K.Box("Ramp_Soffit", g, new Vector3(cxr, STREET_Y * 0.5f + 2.9f, czr),
                  new Vector3(RAMP_X1 - RAMP_X0 + 1f, 0.4f, Mathf.Sqrt(len * len + STREET_Y * STREET_Y)), "Ceiling",
                  new Vector3(-pitch, 0f, 0f), false);

            // traction grooves
            for (int i = 0; i < 22; i++)
            {
                float t = i / 21f;
                K.Box("Groove_" + i, g, new Vector3(cxr, STREET_Y * t + 0.06f, Mathf.Lerp(RAMP_Z_TOP, RAMP_Z_BOT, t)),
                      new Vector3(RAMP_X1 - RAMP_X0 - 0.3f, 0.03f, 0.12f), "SteelDark", default(Vector3), false);
            }

            // street apron at the top of the ramp
            var st = K.Group("Street_Apron", g);
            K.Box("Apron", st, new Vector3(cxr, STREET_Y - 0.1f, RAMP_Z_TOP - 4f), new Vector3(16f, 0.2f, 9f), "Asphalt");
            K.Box("Kerb_W", st, new Vector3(cxr - 8.2f, STREET_Y + 0.5f, RAMP_Z_TOP - 4f), new Vector3(0.4f, 1.2f, 9f), "Wall");
            K.Box("Kerb_E", st, new Vector3(cxr + 8.2f, STREET_Y + 0.5f, RAMP_Z_TOP - 4f), new Vector3(0.4f, 1.2f, 9f), "Wall");
            K.Box("Backdrop", st, new Vector3(cxr, STREET_Y + 4f, RAMP_Z_TOP - 8.6f), new Vector3(17f, 8f, 0.5f), "WallDirty");
            K.Box("Portal_Header", g, new Vector3(cxr, STREET_Y + 1.9f, RAMP_Z_TOP + 0.3f), new Vector3(8.4f, 1.0f, 0.6f), "Granite");
            K.Sign("Portal_Sign", g, new Vector3(cxr, STREET_Y + 1.9f, RAMP_Z_TOP - 0.05f), new Vector2(6.6f, 0.7f),
                   "P   QUD CLUB   B1", new Color(0.95f, 0.95f, 0.95f), 0f);
            K.NeonStrip("Neon_Portal", g, new Vector3(cxr, STREET_Y + 1.3f, RAMP_Z_TOP - 0.02f),
                        new Vector3(8.2f, 0.09f, 0.09f), "NeonMagenta", new Color(1f, 0.1f, 0.6f), 4f, 9f);
            // height limit bar
            K.Box("HeightBar", g, new Vector3(cxr, STREET_Y + 1.15f, RAMP_Z_TOP + 1.2f), new Vector3(7.4f, 0.16f, 0.1f), "LineYellow", default(Vector3), false);

            // barrier + ticket machine at the foot
            var bar = K.Group("Barrier", g);
            bar.localPosition = new Vector3(RAMP_X0 - 0.9f, 0f, RAMP_Z_BOT + 2.5f);
            K.Box("Cabinet", bar, new Vector3(0f, 0.55f, 0f), new Vector3(0.4f, 1.1f, 0.45f), "SteelDark");
            K.Box("Boom", bar, new Vector3(2.4f, 1.02f, 0f), new Vector3(4.6f, 0.1f, 0.1f), "LineWhite", default(Vector3), false);
            for (int i = 0; i < 5; i++)
                K.Box("BoomStripe_" + i, bar, new Vector3(0.6f + i * 0.9f, 1.02f, 0f), new Vector3(0.45f, 0.11f, 0.11f), "NeonRed", default(Vector3), false);
            K.Box("Lamp", bar, new Vector3(0f, 1.2f, 0f), new Vector3(0.16f, 0.16f, 0.16f), "SignAmber", default(Vector3), false);
            K.Box("Ticket", bar, new Vector3(-0.1f, 0.7f, 1.6f), new Vector3(0.35f, 1.4f, 0.35f), "SteelDark");
            K.Box("Ticket_Face", bar, new Vector3(-0.1f, 1.15f, 1.42f), new Vector3(0.24f, 0.3f, 0.03f), "LampWhite", default(Vector3), false);

            K.Marker("ENTRY_RampTop", g, new Vector3(cxr, STREET_Y, RAMP_Z_TOP - 2f));
            K.Marker("ENTRY_RampFoot", g, new Vector3(cxr, 0f, RAMP_Z_BOT + 4f));
        }

        // ───────────────────────── parking rows ─────────────────────────
        static void ParkingRows()
        {
            var g = K.Group("Parking_Bays", _dress);
            // 4 rows: A(west wall) B(lane west) B'(lane east) A'(east wall)
            BayRow(g, "Row_A_West", -HX, BAY_D, 11, -16f, false);
            BayRow(g, "Row_B_West", -LANE - BAY_D, BAY_D, 11, -16f, true);
            BayRow(g, "Row_B_East", LANE, BAY_D, 11, -16f, false);
            BayRow(g, "Row_A_East", HX - BAY_D, BAY_D, 11, -16f, true);
        }

        /// <summary>Paints one row of bays. xStart is the west edge of the row.</summary>
        static void BayRow(Transform parent, string name, float xStart, float depth, int count, float zStart, bool stopsAtWest)
        {
            var g = K.Group(name, parent);
            float y = 0.012f;
            for (int i = 0; i <= count; i++)
            {
                float z = zStart + i * BAY_W;
                K.Box("Line_" + i, g, new Vector3(xStart + depth * 0.5f, y, z), new Vector3(depth, 0.014f, 0.11f), "LineWhite", default(Vector3), false);
            }
            // head line + wheel stops
            float xHead = stopsAtWest ? xStart + 0.35f : xStart + depth - 0.35f;
            K.Box("HeadLine", g, new Vector3(xHead, y, zStart + count * BAY_W * 0.5f), new Vector3(0.11f, 0.014f, count * BAY_W), "LineWhite", default(Vector3), false);
            for (int i = 0; i < count; i++)
                K.Box("Stop_" + i, g, new Vector3(xHead, 0.06f, zStart + (i + 0.5f) * BAY_W), new Vector3(0.3f, 0.12f, 1.7f), "WallDirty", default(Vector3), false);
        }

        static void FloorGraphics()
        {
            var g = K.Group("Floor_Graphics", _dress);
            float y = 0.014f;

            // centre line + lane edges on the approach lane
            for (int i = 0; i < 18; i++)
                K.Box("Centre_" + i, g, new Vector3(0f, y, ZS + 1.5f + i * 2.1f), new Vector3(0.16f, 0.014f, 1.2f), "LineYellow", default(Vector3), false);
            K.Box("LaneEdge_W", g, new Vector3(-LANE + 0.2f, y, 1f), new Vector3(0.12f, 0.014f, 34f), "LineYellow", default(Vector3), false);
            K.Box("LaneEdge_E", g, new Vector3(LANE - 0.2f, y, 1f), new Vector3(0.12f, 0.014f, 34f), "LineYellow", default(Vector3), false);

            // direction arrows
            for (int i = 0; i < 5; i++) Arrow(g, "Arrow_N_" + i, new Vector3(-3f, y, -13f + i * 7f), 0f);
            for (int i = 0; i < 5; i++) Arrow(g, "Arrow_S_" + i, new Vector3(3f, y, -11f + i * 7f), 180f);
            Arrow(g, "Arrow_AisleW", new Vector3(-14f, y, -6f), 0f);
            Arrow(g, "Arrow_AisleE", new Vector3(14f, y, -6f), 180f);

            // no-parking hatch in front of the club entrance
            for (int i = 0; i < 12; i++)
                K.Box("Hatch_" + i, g, new Vector3(-5.5f + i * 1.0f, y, 14.4f), new Vector3(0.1f, 0.014f, 1.9f), "LineYellow", new Vector3(0f, 30f, 0f), false);
            K.Box("Hatch_Edge_N", g, new Vector3(0f, y, 15.2f), new Vector3(11.6f, 0.014f, 0.1f), "LineYellow", default(Vector3), false);
            K.Box("Hatch_Edge_S", g, new Vector3(0f, y, 13.6f), new Vector3(11.6f, 0.014f, 0.1f), "LineYellow", default(Vector3), false);

            // bay numbers
            for (int i = 0; i < 6; i++)
            {
                var s = K.Sign("BayNo_W_" + i, g, new Vector3(-7.2f, y, -14.7f + i * 5.2f), new Vector2(1.5f, 0.5f),
                               "B" + (i * 2 + 1).ToString("00"), new Color(0.68f, 0.68f, 0.66f), 0f);
                s.transform.localEulerAngles = new Vector3(90f, 90f, 0f);
                var s2 = K.Sign("BayNo_E_" + i, g, new Vector3(7.2f, y, -14.7f + i * 5.2f), new Vector2(1.5f, 0.5f),
                                "B" + (i * 2 + 2).ToString("00"), new Color(0.68f, 0.68f, 0.66f), 0f);
                s2.transform.localEulerAngles = new Vector3(90f, -90f, 0f);
            }

            // puddles for the wet neon look – few and wide so the edges stay unread
            for (int i = 0; i < 9; i++)
            {
                float px = Mathf.Lerp(-18f, 18f, ((i * 5) % 9) / 8f);
                float pz = Mathf.Lerp(-15f, 16f, ((i * 4) % 9) / 8f);
                K.Decal("Puddle_" + i, g, new Vector3(px, 0.006f, pz), new Vector2(4.5f + (i % 3) * 1.8f, 3.0f + (i % 4) * 1.2f), "Puddle", i * 37f);
            }
            for (int i = 0; i < 8; i++)
                K.Decal("Stain_" + i, g, new Vector3(-16f + i * 4.5f, 0.005f, -8f + (i % 3) * 6f), new Vector2(1.4f, 1.0f), "Dirt", i * 41f);

            // drainage channel down the middle of the lane
            K.Box("Drain", g, new Vector3(0f, -0.01f, 1f), new Vector3(0.3f, 0.06f, 34f), "HazardBlack", default(Vector3), false);
            for (int i = 0; i < 56; i++)
                K.Box("Grate_" + i, g, new Vector3(0f, 0.008f, -16f + i * 0.62f), new Vector3(0.28f, 0.02f, 0.3f), "Steel", default(Vector3), false);
        }

        static void Arrow(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Shaft", g, new Vector3(0f, 0f, -0.25f), new Vector3(0.2f, 0.014f, 2.0f), "LineWhite", default(Vector3), false);
            K.Box("Head_L", g, new Vector3(-0.26f, 0f, 0.5f), new Vector3(0.2f, 0.014f, 1.0f), "LineWhite", new Vector3(0f, -32f, 0f), false);
            K.Box("Head_R", g, new Vector3(0.26f, 0f, 0.5f), new Vector3(0.2f, 0.014f, 1.0f), "LineWhite", new Vector3(0f, 32f, 0f), false);
        }

        // ───────────────────────── lighting, signage, services ─────────────────────────
        static void Services()
        {
            var g = K.Group("Services", _dress);
            var lg = K.Group("Fixtures", _light);

            // ceiling batten lights over the aisles and lane
            float[] lx = { -14f, 0f, 14f };
            for (int a = 0; a < lx.Length; a++)
                for (int i = 0; i < 6; i++)
                {
                    float z = -15f + i * 6.2f;
                    bool real = (i % 2 == 0) || a == 1;
                    Batten(lg, "Light_" + a + "_" + i, new Vector3(lx[a], CH - 0.42f, z), real);
                }

            // neon wall strips – the colour signature of the club level
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -HX + 0.25f : HX - 0.25f;
                K.NeonStrip("Neon_Wall_M_" + i, lg, new Vector3(x, 2.45f, 1f), new Vector3(0.1f, 0.1f, 34f), "NeonMagenta",
                            new Color(1f, 0.08f, 0.6f), 2.4f, 13f);
                K.NeonStrip("Neon_Wall_C_" + i, lg, new Vector3(x, 2.1f, 1f), new Vector3(0.08f, 0.06f, 34f), "NeonCyan",
                            new Color(0.1f, 0.8f, 1f), 1.6f, 11f);
            }
            // neon accents on the column heads facing the lane
            for (int i = 0; i < COL_Z.Length; i++)
            {
                float z = COL_Z[i];
                K.Box("Neon_Col_W" + i, lg, new Vector3(COL_X[1] - 0.35f, CH - 0.8f, z), new Vector3(0.06f, 0.07f, 0.75f), "NeonCyan", default(Vector3), false);
                K.Box("Neon_Col_E" + i, lg, new Vector3(COL_X[2] + 0.35f, CH - 0.8f, z), new Vector3(0.06f, 0.07f, 0.75f), "NeonCyan", default(Vector3), false);
            }

            // ducts, pipes, sprinkler and cable tray under the slab
            var mep = K.Group("MEP", g);
            K.Box("Duct_Main", mep, new Vector3(-9f, CH - 0.55f, 1f), new Vector3(1.1f, 0.7f, 34f), "Alu", default(Vector3), false);
            K.Box("Duct_Branch", mep, new Vector3(9f, CH - 0.6f, 1f), new Vector3(0.8f, 0.5f, 34f), "Alu", default(Vector3), false);
            K.Cyl("Pipe_Red", mep, new Vector3(-11.5f, CH - 0.35f, 1f), 0.16f, 34f, "NeonRed", new Vector3(90f, 0f, 0f));
            K.Cyl("Pipe_Grey", mep, new Vector3(11.5f, CH - 0.35f, 1f), 0.12f, 34f, "Steel", new Vector3(90f, 0f, 0f));
            K.Box("Tray", mep, new Vector3(-3.2f, CH - 0.3f, 1f), new Vector3(0.36f, 0.1f, 34f), "SteelDark", default(Vector3), false);
            for (int i = 0; i < 18; i++)
                K.Cyl("Sprinkler_" + i, mep, new Vector3(-11.5f + (i % 3) * 11.5f, CH - 0.5f, -15f + (i / 3) * 6.2f), 0.05f, 0.22f, "Brass");

            // wayfinding + exits
            HangingSign(g, "Sign_ToClub", new Vector3(0f, CH - 0.62f, 9f), "QUD CLUB  →  ENTRANCE", "NeonMagenta");
            HangingSign(g, "Sign_Level", new Vector3(0f, CH - 0.62f, -12f), "LEVEL B1   ·   NO PARKING IN AISLES", "SignAmber");
            ExitSign(lg, "Exit_W", new Vector3(-HX + 0.45f, 2.35f, 6.5f), 90f);
            ExitSign(lg, "Exit_E", new Vector3(HX - 0.45f, 2.35f, -6.5f), -90f);

            // lift + stair lobby on the west wall
            var lob = K.Group("LiftLobby", g);
            lob.localPosition = new Vector3(-HX + 0.35f, 0f, 5.5f);
            K.Box("Front", lob, new Vector3(0f, 1.4f, 0f), new Vector3(0.4f, 2.8f, 5.2f), "Granite");
            for (int i = 0; i < 2; i++)
            {
                K.Box("LiftDoor_" + i, lob, new Vector3(0.22f, 1.1f, -1.2f + i * 2.4f), new Vector3(0.08f, 2.2f, 1.7f), "Alu", default(Vector3), false);
                K.Box("LiftSplit_" + i, lob, new Vector3(0.27f, 1.1f, -1.2f + i * 2.4f), new Vector3(0.02f, 2.2f, 0.04f), "SteelDark", default(Vector3), false);
                K.Box("LiftCall_" + i, lob, new Vector3(0.27f, 1.15f, -0.2f + i * 2.4f), new Vector3(0.03f, 0.2f, 0.12f), "SignAmber", default(Vector3), false);
            }
            K.NeonStrip("Neon_Lobby", lob, new Vector3(0.24f, 2.45f, 0f), new Vector3(0.06f, 0.08f, 5.0f), "NeonPurple", new Color(0.6f, 0.15f, 1f), 2.2f, 8f);
            K.Sign("Lobby_Text", lob, new Vector3(0.3f, 2.05f, 0f), new Vector2(2.4f, 0.3f), "LIFT  ·  B1", Color.white, -90f);

            // fire point + misc clutter
            K.Box("FireCabinet", g, new Vector3(HX - 0.32f, 1.15f, 11f), new Vector3(0.28f, 1.1f, 0.8f), "NeonRed");
            K.Box("Bin", g, new Vector3(-6.9f, 0.45f, -15.5f), new Vector3(0.6f, 0.9f, 0.6f), "SteelDark");
            K.Place(P_CONE, g, new Vector3(-6.5f, 0f, 12.5f), 0f);
            K.Place(P_CONE, g, new Vector3(6.5f, 0f, 12.5f), 0f);
            K.Place(P_BARRIER, g, new Vector3(-8.6f, 0f, 15.4f), 0f);
            K.Place(P_BARRIER, g, new Vector3(8.6f, 0f, 15.4f), 0f);

            // cctv
            Cctv(g, "CCTV_Lane_N", new Vector3(-6.4f, 2.7f, 13.2f), 200f);
            Cctv(g, "CCTV_Lane_S", new Vector3(6.4f, 2.7f, -13.8f), 20f);
            Cctv(g, "CCTV_Ramp", new Vector3(RAMP_X0 - 0.4f, 2.7f, -16.5f), 120f);
        }

        static void Batten(Transform parent, string name, Vector3 p, bool realLight)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Box("Housing", g, new Vector3(0f, 0.06f, 0f), new Vector3(0.22f, 0.1f, 2.4f), "Alu", default(Vector3), false);
            K.Box("Tube", g, Vector3.zero, new Vector3(0.16f, 0.07f, 2.3f), "LampWhite", default(Vector3), false);
            if (!realLight) return;
            K.AddLight(g, "Light", new Vector3(0f, -0.2f, 0f), Vector3.zero, LightType.Point,
                       new Color(0.78f, 0.84f, 1f), 1.7f, 9.5f);
        }

        static void HangingSign(Transform parent, string name, Vector3 p, string text, string neon)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Box("Rod_L", g, new Vector3(-2.2f, 0.42f, 0f), new Vector3(0.04f, 0.85f, 0.04f), "SteelDark", default(Vector3), false);
            K.Box("Rod_R", g, new Vector3(2.2f, 0.42f, 0f), new Vector3(0.04f, 0.85f, 0.04f), "SteelDark", default(Vector3), false);
            K.Box("Face", g, Vector3.zero, new Vector3(5.0f, 0.62f, 0.08f), "Granite", default(Vector3), false);
            // neon border rather than a solid glowing slab
            K.Box("Trim_T", g, new Vector3(0f, 0.33f, -0.05f), new Vector3(5.1f, 0.05f, 0.05f), neon, default(Vector3), false);
            K.Box("Trim_B", g, new Vector3(0f, -0.33f, -0.05f), new Vector3(5.1f, 0.05f, 0.05f), neon, default(Vector3), false);
            K.Box("Trim_L", g, new Vector3(-2.53f, 0f, -0.05f), new Vector3(0.05f, 0.68f, 0.05f), neon, default(Vector3), false);
            K.Box("Trim_R", g, new Vector3(2.53f, 0f, -0.05f), new Vector3(0.05f, 0.68f, 0.05f), neon, default(Vector3), false);
            K.AddLight(g, "Glow", new Vector3(0f, -0.3f, -0.3f), Vector3.zero, LightType.Point,
                       neon == "NeonMagenta" ? new Color(1f, 0.15f, 0.6f) : new Color(1f, 0.6f, 0.15f), 2.2f, 7f);
            K.Sign("Text_F", g, new Vector3(0f, 0f, -0.08f), new Vector2(4.7f, 0.42f), text, Color.white);
            K.Sign("Text_B", g, new Vector3(0f, 0f, 0.08f), new Vector2(4.7f, 0.42f), text, Color.white, 0f);
        }

        static void ExitSign(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Box", g, Vector3.zero, new Vector3(0.06f, 0.26f, 0.7f), "ExitGreen", default(Vector3), false);
            K.Sign("Text", g, new Vector3(-0.05f, 0f, 0f), new Vector2(0.62f, 0.2f), "EXIT", Color.white, -90f);
            K.AddLight(g, "Light", new Vector3(-0.2f, 0f, 0f), Vector3.zero, LightType.Point, new Color(0.25f, 1f, 0.4f), 0.8f, 4f);
        }

        static void Cctv(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Bracket", g, new Vector3(0f, 0.1f, 0.12f), new Vector3(0.05f, 0.05f, 0.24f), "SteelDark", default(Vector3), false);
            var body = K.Group("Body", g);
            body.localEulerAngles = new Vector3(24f, 0f, 0f);
            K.Box("Case", body, new Vector3(0f, 0f, -0.16f), new Vector3(0.12f, 0.12f, 0.34f), "SteelDark", default(Vector3), false);
            K.Cyl("Lens", body, new Vector3(0f, 0f, -0.34f), 0.09f, 0.05f, "HazardBlack", new Vector3(90f, 0f, 0f));
            K.Box("LED", body, new Vector3(0.04f, 0.05f, -0.33f), new Vector3(0.02f, 0.02f, 0.02f), "NeonRed", default(Vector3), false);
            K.Marker("VIEW_" + name, body, new Vector3(0f, 0f, -0.4f));
        }

        // ───────────────────────── club entrance (the far end) ─────────────────────────
        // the doorway into the club scene, sized well past the player capsule
        const float ENTRY_CLEAR_W = 3.6f;
        const float ENTRY_CLEAR_H = 2.6f;

        static void ClubEntrance()
        {
            var g = K.Group("Club_Entrance", _circ);
            float z = ZN - 0.35f;

            // The facade is punched, not painted on: the opening is a genuine hole the player
            // can walk into, and the glass leaves are hung open so they never block it.
            K.WallWithOpening("Facade", g, new Vector3(0f, 0f, z + 0.2f), 21f, 3.0f, 0.5f, "Granite",
                              0f, ENTRY_CLEAR_W, ENTRY_CLEAR_H);
            for (int i = 0; i < 2; i++)
            {
                float bw = (21f - ENTRY_CLEAR_W) * 0.5f;
                float bx = (i == 0 ? -1f : 1f) * (ENTRY_CLEAR_W * 0.5f + bw * 0.5f);
                K.Box("Facade_Base_" + i, g, new Vector3(bx, 0.15f, z - 0.1f), new Vector3(bw, 0.3f, 0.35f), "Brass", default(Vector3), false);
            }

            K.DoorFrame("DoorFrame", g, new Vector3(0f, 0f, z - 0.06f), ENTRY_CLEAR_W, ENTRY_CLEAR_H, 0.2f, 0.16f, "SteelDark");
            K.DoorLeaf("Door_L", g, new Vector3(-ENTRY_CLEAR_W * 0.5f, 0f, z - 0.18f), ENTRY_CLEAR_W * 0.5f, ENTRY_CLEAR_H - 0.1f, 0.08f, "GlassDark", 180f, 100f);
            K.DoorLeaf("Door_R", g, new Vector3(ENTRY_CLEAR_W * 0.5f, 0f, z - 0.18f), ENTRY_CLEAR_W * 0.5f, ENTRY_CLEAR_H - 0.1f, 0.08f, "GlassDark", 0f, -100f);

            // QUD CLUB neon
            var sg = K.Group("NeonSign", g);
            K.Box("Backer", sg, new Vector3(0f, 2.45f, z - 0.12f), new Vector3(9.5f, 0.95f, 0.1f), "HazardBlack", default(Vector3), false);
            K.Sign("Text", sg, new Vector3(0f, 2.45f, z - 0.2f), new Vector2(8.6f, 0.78f), "QUD CLUB", new Color(1f, 0.35f, 0.75f));
            K.NeonStrip("Neon_Top", sg, new Vector3(0f, 2.96f, z - 0.2f), new Vector3(9.6f, 0.09f, 0.09f), "NeonMagenta", new Color(1f, 0.1f, 0.6f), 5f, 12f);
            K.NeonStrip("Neon_Bot", sg, new Vector3(0f, 1.94f, z - 0.2f), new Vector3(9.6f, 0.07f, 0.07f), "NeonCyan", new Color(0.1f, 0.8f, 1f), 4f, 11f);
            for (int i = 0; i < 12; i++)
                K.Box("Tick_" + i, sg, new Vector3(-5.5f + i * 1.0f, 2.45f, z - 0.06f), new Vector3(0.05f, 0.85f, 0.04f), "NeonPurple", default(Vector3), false);

            // red carpet run-up with rope line
            K.Box("Carpet", g, new Vector3(0f, 0.012f, 17.2f), new Vector3(4.4f, 0.02f, 5.6f), "Carpet", default(Vector3), false);
            for (int i = 0; i < 5; i++)
            {
                float pz = 14.8f + i * 1.25f;
                Stanchion(g, "Rope_W_" + i, new Vector3(-2.5f, 0f, pz), i < 4);
                Stanchion(g, "Rope_E_" + i, new Vector3(2.5f, 0f, pz), i < 4);
            }

            // canopy + downlights (kept low so it never clips the neon)
            K.Box("Canopy", g, new Vector3(0f, 2.52f, 18.5f), new Vector3(6.0f, 0.14f, 1.7f), "HazardBlack", default(Vector3), false);
            K.Box("Canopy_Edge", g, new Vector3(0f, 2.46f, 17.68f), new Vector3(6.0f, 0.1f, 0.08f), "NeonMagenta", default(Vector3), false);
            for (int i = 0; i < 3; i++)
                K.AddLight(g, "EntranceLight_" + i, new Vector3(-2.4f + i * 2.4f, 2.42f, 18.2f), new Vector3(90f, 0f, 0f),
                           LightType.Spot, new Color(1f, 0.45f, 0.72f), 5.5f, 10f, 88f, i == 1);
            // wall washers either side of the facade
            for (int i = 0; i < 2; i++)
                K.AddLight(g, "FacadeWash_" + i, new Vector3(i == 0 ? -7f : 7f, 2.6f, 17.6f), new Vector3(28f, 180f, 0f),
                           LightType.Spot, new Color(0.55f, 0.25f, 0.95f), 4.0f, 12f, 70f);

            // the transition trigger sits on open floor in front of the doors, not inside them
            K.Marker("DOOR_ToClubScene", g, new Vector3(0f, 0f, ZN + 1.6f));
            // A short lobby behind the doors. Without it the doorway opened onto the back of
            // the north wall and the player could never stand in it.
            var lob = K.Group("Entry_Lobby", g);
            float vd = 3.0f, vw = ENTRY_CLEAR_W + 2.6f;
            float vz = ZN + 0.3f + vd * 0.5f;
            K.Box("Floor", lob, new Vector3(0f, -0.2f, vz), new Vector3(vw, 0.4f, vd + 0.6f), "Granite");
            K.Box("Ceiling", lob, new Vector3(0f, CH + SLAB * 0.5f, vz), new Vector3(vw + 0.6f, SLAB, vd + 0.6f), "Ceiling");
            K.Box("Wall_W", lob, new Vector3(-vw * 0.5f - 0.15f, CH * 0.5f, vz), new Vector3(0.3f, CH, vd + 0.6f), "Wall");
            K.Box("Wall_E", lob, new Vector3(vw * 0.5f + 0.15f, CH * 0.5f, vz), new Vector3(0.3f, CH, vd + 0.6f), "Wall");
            K.Box("Wall_Back", lob, new Vector3(0f, CH * 0.5f, ZN + 0.3f + vd + 0.15f), new Vector3(vw + 0.6f, CH, 0.3f), "Granite");
            K.Box("Carpet_Lobby", lob, new Vector3(0f, 0.012f, vz), new Vector3(vw - 0.6f, 0.02f, vd), "Carpet", default(Vector3), false);
            K.NeonStrip("Lobby_Neon", lob, new Vector3(0f, CH - 0.55f, vz), new Vector3(vw - 0.4f, 0.08f, 0.08f), "NeonMagenta", new Color(1f, 0.1f, 0.6f), 3.2f, 9f);
            K.AddLight(lob, "Lobby_Light", new Vector3(0f, CH - 0.7f, vz), new Vector3(90f, 0f, 0f), LightType.Spot, new Color(1f, 0.5f, 0.78f), 4.5f, 9f, 95f);

            K.Marker("OBJ_ReachClubEntrance", g, new Vector3(0f, 0f, 16.5f));
        }

        static void Stanchion(Transform parent, string name, Vector3 p, bool rope)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Cyl("Base", g, new Vector3(0f, 0.03f, 0f), 0.34f, 0.06f, "Brass");
            K.Cyl("Post", g, new Vector3(0f, 0.48f, 0f), 0.07f, 0.9f, "Brass");
            K.Sphere("Cap", g, new Vector3(0f, 0.97f, 0f), 0.11f, "Brass");
            if (rope) K.Box("Rope", g, new Vector3(0f, 0.78f, 0.62f), new Vector3(0.06f, 0.06f, 1.25f), "Velvet", new Vector3(9f, 0f, 0f), false);
        }

        // ───────────────────────── vehicles ─────────────────────────
        static void Vehicles()
        {
            var g = K.Group("Vehicles", _dress);
            // scattered parked cars – gaps left on purpose so the player can slip between rows
            float[] rowX = { -19.5f, -8.5f, 8.5f, 19.5f };
            float[] rowYaw = { 90f, 90f, -90f, -90f };
            int n = 0;
            for (int r = 0; r < rowX.Length; r++)
                for (int i = 0; i < 11; i++)
                {
                    if ((i + r) % 3 == 1) continue;               // empty bays
                    if (r == 2 && i >= 9) continue;               // keep the VIP bay clear
                    float z = -16f + (i + 0.5f) * BAY_W;
                    var parked = K.Place((n % 2 == 0) ? P_CAR_A : P_CAR_B, g, new Vector3(rowX[r], 0f, z), rowYaw[r]);
                    // the car FBX imports with addColliders off, so without this the player
                    // walks straight through every parked car in the level
                    K.FitBoxCollider(parked, 0.06f);
                    n++;
                }

            // THE TARGET CAR – reserved bay next to the club entrance, lit and roped off
            var vip = K.Group("Vehicle_TARGET", _dress);
            vip.localPosition = new Vector3(8.6f, 0f, 12.6f);
            var car = K.Place(P_CAR_A, vip, Vector3.zero, -90f);
            if (car != null)
            {
                car.name = "Car_TARGET_RedSports";
                PaintBody(car, "CarRed");
                K.FitBoxCollider(car, 0.06f);
            }
            K.Box("Bay_Paint", vip, new Vector3(0f, 0.014f, 0f), new Vector3(5.4f, 0.014f, 2.9f), "HazardBlack", default(Vector3), false);
            K.Box("Bay_Edge", vip, new Vector3(0f, 0.016f, 1.5f), new Vector3(5.4f, 0.014f, 0.12f), "LineYellow", default(Vector3), false);
            K.Box("Bay_Edge2", vip, new Vector3(0f, 0.016f, -1.5f), new Vector3(5.4f, 0.014f, 0.12f), "LineYellow", default(Vector3), false);
            K.Sign("Bay_Text", vip, new Vector3(0f, 0.018f, -1.05f), new Vector2(2.6f, 0.36f), "RESERVED", new Color(0.85f, 0.72f, 0.1f), 0f)
             .transform.localEulerAngles = new Vector3(90f, -90f, 0f);
            K.AddLight(vip, "SpotOnCar", new Vector3(0f, 2.85f, 0f), new Vector3(90f, 0f, 0f), LightType.Spot,
                       new Color(1f, 0.85f, 0.8f), 5.5f, 7f, 62f, true);
            Stanchion(vip, "Rope_A", new Vector3(-2.4f, 0f, -1.9f), false);
            Stanchion(vip, "Rope_B", new Vector3(2.4f, 0f, -1.9f), false);
            K.Marker("INTERACT_ExamineCar", vip, new Vector3(0f, 0f, -2.4f));
        }

        /// <summary>Swaps the car's main body material so the target reads as the red sports car.</summary>
        static void PaintBody(GameObject car, string matKey)
        {
            var mat = K.M(matKey);
            if (mat == null) return;
            var rs = car.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
                if (rs[i].name.StartsWith("Body"))
                {
                    var arr = rs[i].sharedMaterials;
                    for (int k = 0; k < arr.Length; k++) arr[k] = mat;
                    rs[i].sharedMaterials = arr;
                }
        }

        // ───────────────────────── gameplay ─────────────────────────
        static void Gameplay()
        {
            var g = _play;

            K.Marker("PlayerSpawn_RampFoot", g, new Vector3(17.5f, 0.2f, -15.5f));
            // ถอยจากป้ายแขวนที่ z -12 มาหน่อย ไม่งั้นเปิดเกมมาป้ายจ่อหน้าเลย
            K.Marker("PlayerSpawn_LaneSouth", g, new Vector3(-3.4f, 0.2f, -16f));
            K.PlacePlayer(P_PLAYER, g, new Vector3(-3.4f, 0.02f, -16f), 0f);

            // guards, laid out per the level sketch
            var gd = K.Group("GUARDS", _actors);
            Guard(gd, "Guard_01_EntranceW", new Vector3(-7.5f, 0f, 15.5f), 180f,
                  new Vector3[] { new Vector3(-7.5f, 0f, 15.5f), new Vector3(-17f, 0f, 15.5f), new Vector3(-17f, 0f, 10f) });
            Guard(gd, "Guard_02_EntranceE", new Vector3(7.5f, 0f, 15.5f), 180f,
                  new Vector3[] { new Vector3(7.5f, 0f, 15.5f), new Vector3(17f, 0f, 15.5f), new Vector3(17f, 0f, 10f) });
            Guard(gd, "Guard_03_LaneW", new Vector3(-5.2f, 0f, 1.0f), 90f,
                  new Vector3[] { new Vector3(-5.2f, 0f, 1.0f), new Vector3(-5.2f, 0f, -8f) });
            Guard(gd, "Guard_04_LaneE", new Vector3(5.2f, 0f, -1.0f), -90f,
                  new Vector3[] { new Vector3(5.2f, 0f, -1.0f), new Vector3(5.2f, 0f, 8f) });
            Guard(gd, "Guard_05_AisleW", new Vector3(-14f, 0f, -4f), 0f,
                  new Vector3[] { new Vector3(-14f, 0f, -12f), new Vector3(-14f, 0f, 8f) });

            // cover / hiding
            var hide = K.Group("CoverPoints", g);
            for (int a = 0; a < COL_X.Length; a++)
                for (int b = 0; b < COL_Z.Length; b++)
                    K.Marker("Cover_Column_" + a + b, hide, new Vector3(COL_X[a] + (a < 2 ? -0.9f : 0.9f), 0f, COL_Z[b]));
            K.Marker("Cover_BehindCar_W", hide, new Vector3(-9.4f, 0f, -3f));
            K.Marker("Cover_BehindCar_E", hide, new Vector3(9.4f, 0f, 4f));
            K.Marker("Cover_LiftLobby", hide, new Vector3(-19.5f, 0f, 5.5f));
            K.Marker("Cover_Bin", hide, new Vector3(-6.9f, 0f, -15.5f));

            // objectives
            var obj = K.Group("Objectives", g);
            K.Marker("OBJ_01_EnterGarage", obj, new Vector3(17.5f, 0f, -15.5f));
            K.Marker("OBJ_02_LearnCrouch", obj, new Vector3(-6.4f, 0f, -7f));
            K.Marker("OBJ_03_ExamineTargetCar", obj, new Vector3(8.6f, 0f, 10.4f));
            K.Marker("OBJ_04_AvoidGuards", obj, new Vector3(0f, 0f, 8f));
            K.Marker("OBJ_05_EnterClub", obj, new Vector3(0f, 0f, 17.5f));

            // camera anchors
            var cam = K.Group("CutsceneCameras", g);
            CamAnchor(cam, "CUT_01_ArriveRamp", new Vector3(17.5f, 1.8f, -14f), new Vector3(4f, 20f, 0f));
            CamAnchor(cam, "CUT_02_LaneReveal", new Vector3(-2.4f, 1.7f, -12f), new Vector3(2f, 8f, 0f));
            CamAnchor(cam, "CUT_03_TargetCar", new Vector3(4.6f, 1.5f, 9.4f), new Vector3(4f, 42f, 0f));
            CamAnchor(cam, "CUT_04_ClubDoors", new Vector3(0f, 1.7f, 11.5f), new Vector3(2f, 0f, 0f));
        }

        static void Guard(Transform parent, string name, Vector3 p, float yaw, Vector3[] route)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            var npc = K.PlaceHuman(P_NPC, g, Vector3.zero, 0f);
            if (npc != null) npc.name = "NPC_Guard_PLACEHOLDER";
            K.Marker("FACING", g, new Vector3(0f, 1.6f, 2.5f));
            var r = K.Group("PatrolRoute", g);
            r.localEulerAngles = new Vector3(0f, -yaw, 0f);      // route points are authored in world space
            for (int i = 0; i < route.Length; i++)
                K.Marker("Point_" + i.ToString("00"), r, route[i] - p);
        }

        static void CamAnchor(Transform parent, string name, Vector3 pos, Vector3 euler)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localEulerAngles = euler;
        }
    }
}
