using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using K = SecretsThatBreathe.LevelTools.LevelKit;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// CHAPTER 2 – QUD CLUB interior. Entered from the B1 car park scene.
    ///
    ///        ┌──────────── ENTRANCE (north) ────────────┐
    ///   VIP  │                                          │  STAGE
    ///  ZONE  │            ◯ DANCE FLOOR                 │  ZONE
    ///  (west)│                                          │  (east)
    ///        └──────────── BAR ZONE (south) ────────────┘
    ///
    /// Menu: Tools ▸ Secrets That Breathe ▸ Build Chapter 2 Club
    /// </summary>
    public static class Ch2ClubBuilder
    {
        // ── master dimensions (metres) ──
        public const float HX = 15f;         // half width  -> 30 m
        public const float HZ = 13f;         // half depth  -> 26 m
        public const float CH = 8.0f;        // main hall height
        public const float BAL = 4.2f;       // balcony walking level
        public const float DF_R = 5.0f;      // dance floor radius
        public static readonly Vector3 DF = new Vector3(0f, 0f, -1f);   // dance floor centre
        public const float VIP_X = -8.5f;    // east edge of the VIP platform
        public const float VIP_Y = 0.45f;
        public const float STAGE_X = 9f;
        public const float STAGE_Y = 0.9f;

        public const string ScenePath = "Assets/MainScenes/Main2_Club.unity";
        public const string DataFolder = "Assets/MainScenes/Main2_Club";
        public const string MatFolder = DataFolder + "/Materials";

        const string P_PLAYER = "Assets/Champ&Kichzz/Prefab/Player/player.prefab";
        const string P_NPC = "Assets/Champ&Kichzz/Prefab/Npc/NPC.prefab";

        static Transform _root;

        [MenuItem("Tools/Secrets That Breathe/Build Chapter 2 Club", false, 12)]
        public static void BuildScene() { BuildScene(true); }

        public static void BuildScene(bool askToSave)
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Ch2Club] leave play mode first."); return; }
            if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            K.EnsureFolder(MatFolder);
            Materials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _root = new GameObject("=== CH2 QUD CLUB ===").transform;

            Atmosphere();
            Shell();
            Balcony();
            DanceFloor();
            BarZone();
            StageZone();
            VipZone();
            Entrance();
            BackOfHouse();
            Rigging();
            Crowd();
            Gameplay();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Ch2Club] built -> " + ScenePath);
        }

        static void Materials()
        {
            K.UseLibrary(MatFolder, "M_C_");
            K.Mat("Floor", new Color(0.055f, 0.05f, 0.065f), 0.15f, 0.72f);      // dark polished
            K.Mat("FloorVip", new Color(0.10f, 0.045f, 0.06f), 0f, 0.2f);        // carpet
            K.Mat("Wall", new Color(0.075f, 0.062f, 0.085f), 0f, 0.25f);
            K.Mat("WallPanel", new Color(0.11f, 0.055f, 0.09f), 0.1f, 0.4f);
            K.Mat("Ceiling", new Color(0.035f, 0.032f, 0.042f), 0f, 0.15f);
            K.Mat("Marble", new Color(0.16f, 0.12f, 0.17f), 0.2f, 0.85f);
            K.Mat("Brass", new Color(0.66f, 0.48f, 0.17f), 0.95f, 0.72f);
            K.Mat("BrassDark", new Color(0.34f, 0.24f, 0.09f), 0.9f, 0.5f);
            K.Mat("Velvet", new Color(0.32f, 0.03f, 0.11f), 0f, 0.10f);
            K.Mat("VelvetPink", new Color(0.46f, 0.06f, 0.22f), 0f, 0.12f);
            K.Mat("Leather", new Color(0.13f, 0.03f, 0.07f), 0f, 0.35f);
            K.Mat("Steel", new Color(0.42f, 0.43f, 0.46f), 0.9f, 0.5f);
            K.Mat("SteelDark", new Color(0.10f, 0.10f, 0.12f), 0.85f, 0.4f);
            K.Mat("Truss", new Color(0.34f, 0.35f, 0.38f), 0.85f, 0.45f);
            K.Mat("Black", new Color(0.028f, 0.028f, 0.032f), 0f, 0.3f);
            K.Mat("Mirror", new Color(0.72f, 0.72f, 0.78f), 1f, 0.95f);
            K.Mat("Wood", new Color(0.20f, 0.10f, 0.07f), 0f, 0.45f);

            K.MatTransparent("Glass", new Color(0.45f, 0.30f, 0.55f, 0.28f), 0.95f);
            K.MatTransparent("Bottle", new Color(0.45f, 0.20f, 0.55f, 0.55f), 0.92f);

            K.MatEmissive("NeonMagenta", new Color(0.80f, 0.10f, 0.55f), new Color(1f, 0.05f, 0.60f) * 6.0f);
            K.MatEmissive("NeonPink", new Color(0.90f, 0.25f, 0.55f), new Color(1f, 0.28f, 0.66f) * 5.0f);
            K.MatEmissive("NeonCyan", new Color(0.10f, 0.65f, 0.82f), new Color(0.08f, 0.82f, 1f) * 5.0f);
            K.MatEmissive("NeonPurple", new Color(0.42f, 0.10f, 0.85f), new Color(0.52f, 0.12f, 1f) * 5.0f);
            K.MatEmissive("NeonBlue", new Color(0.12f, 0.20f, 0.85f), new Color(0.18f, 0.30f, 1f) * 4.5f);
            K.MatEmissive("LedFloorA", new Color(0.30f, 0.04f, 0.20f), new Color(1f, 0.10f, 0.58f) * 0.95f);
            K.MatEmissive("LedFloorB", new Color(0.04f, 0.14f, 0.26f), new Color(0.12f, 0.55f, 1f) * 0.85f);
            K.MatEmissive("LedWall", new Color(0.30f, 0.10f, 0.42f), new Color(0.75f, 0.20f, 1f) * 2.6f);
            K.MatEmissive("LampWarm", new Color(0.85f, 0.62f, 0.35f), new Color(1f, 0.62f, 0.28f) * 2.4f);
            K.MatEmissive("ExitGreen", new Color(0.10f, 0.5f, 0.2f), new Color(0.2f, 1f, 0.4f) * 2.2f);
        }

        static void Atmosphere()
        {
            var g = K.Group("ENV_Lighting", _root);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.115f, 0.045f, 0.135f);
            RenderSettings.ambientEquatorColor = new Color(0.085f, 0.035f, 0.10f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.02f, 0.05f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.16f, 0.04f, 0.18f);
            RenderSettings.fogDensity = 0.020f;

            var probe = new GameObject("Reflection Probe");
            probe.transform.SetParent(g, false);
            probe.transform.localPosition = new Vector3(0f, 2f, 0f);
            var rp = probe.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Realtime;
            rp.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            rp.size = new Vector3(HX * 2f, CH, HZ * 2f);
            rp.boxProjection = true;
            rp.resolution = 128;
        }

        // ───────────────────────── shell ─────────────────────────
        static void Shell()
        {
            var g = K.Group("STRUCT", _root);

            K.Box("Floor", g, new Vector3(0f, -0.2f, 0f), new Vector3(HX * 2f, 0.4f, HZ * 2f), "Floor");
            K.Box("Ceiling", g, new Vector3(0f, CH + 0.25f, 0f), new Vector3(HX * 2f, 0.5f, HZ * 2f), "Ceiling");
            K.Box("Wall_W", g, new Vector3(-HX - 0.2f, CH * 0.5f, 0f), new Vector3(0.4f, CH, HZ * 2f), "Wall");
            K.Box("Wall_E", g, new Vector3(HX + 0.2f, CH * 0.5f, 0f), new Vector3(0.4f, CH, HZ * 2f), "Wall");
            K.Box("Wall_N", g, new Vector3(0f, CH * 0.5f, HZ + 0.2f), new Vector3(HX * 2f + 0.8f, CH, 0.4f), "Wall");
            K.Box("Wall_S", g, new Vector3(0f, CH * 0.5f, -HZ - 0.2f), new Vector3(HX * 2f + 0.8f, CH, 0.4f), "Wall");

            // panelled dado + LED coving around the room
            var p = K.Group("WallPanels", g);
            for (int i = 0; i < 14; i++)
            {
                float z = -HZ + 1f + i * 1.9f;
                K.Box("Panel_W_" + i, p, new Vector3(-HX + 0.12f, 2.0f, z), new Vector3(0.1f, 3.4f, 1.55f), "WallPanel", default(Vector3), false);
                K.Box("Panel_E_" + i, p, new Vector3(HX - 0.12f, 2.0f, z), new Vector3(0.1f, 3.4f, 1.55f), "WallPanel", default(Vector3), false);
            }
            K.NeonStrip("Cove_W", p, new Vector3(-HX + 0.16f, 3.78f, 0f), new Vector3(0.08f, 0.08f, HZ * 2f - 1f), "NeonMagenta", new Color(1f, 0.08f, 0.6f), 2.2f, 12f);
            K.NeonStrip("Cove_E", p, new Vector3(HX - 0.16f, 3.78f, 0f), new Vector3(0.08f, 0.08f, HZ * 2f - 1f), "NeonMagenta", new Color(1f, 0.08f, 0.6f), 2.2f, 12f);
            K.NeonStrip("Cove_S", p, new Vector3(0f, 3.78f, -HZ + 0.16f), new Vector3(HX * 2f - 1f, 0.08f, 0.08f), "NeonCyan", new Color(0.1f, 0.8f, 1f), 2.0f, 12f);
            K.NeonStrip("Cove_N", p, new Vector3(0f, 3.78f, HZ - 0.16f), new Vector3(HX * 2f - 1f, 0.08f, 0.08f), "NeonCyan", new Color(0.1f, 0.8f, 1f), 2.0f, 12f);

            // upper wall LED wash panels (the pink glow in the reference)
            for (int i = 0; i < 8; i++)
            {
                float z = -HZ + 2.5f + i * 3.2f;
                K.Box("Wash_W_" + i, g, new Vector3(-HX + 0.1f, 6.0f, z), new Vector3(0.06f, 2.4f, 1.1f), "LedWall", default(Vector3), false);
                K.Box("Wash_E_" + i, g, new Vector3(HX - 0.1f, 6.0f, z), new Vector3(0.06f, 2.4f, 1.1f), "LedWall", default(Vector3), false);
            }
        }

        // ───────────────────────── balcony (2nd floor ring) ─────────────────────────
        static void Balcony()
        {
            var g = K.Group("BALCONY", _root);

            // north run, full width
            K.Box("Slab_N", g, new Vector3(0f, BAL - 0.15f, HZ - 2.5f), new Vector3(HX * 2f, 0.3f, 5f), "Marble");
            // west return
            K.Box("Slab_W", g, new Vector3(-HX + 2.0f, BAL - 0.15f, -1.5f), new Vector3(4f, 0.3f, 17f), "Marble");

            // support columns
            float[] cx = { -12f, -6f, 0f, 6f, 12f };
            for (int i = 0; i < cx.Length; i++) Column(g, "Col_N_" + i, new Vector3(cx[i], 0f, HZ - 5.0f));
            for (int i = 0; i < 3; i++) Column(g, "Col_W_" + i, new Vector3(-HX + 4.0f, 0f, 3.5f - i * 6f));

            // railings – brass posts with glass infill
            Railing(g, "Rail_N", new Vector3(0f, BAL, HZ - 5.0f), HX * 2f, 0f);
            Railing(g, "Rail_W", new Vector3(-HX + 4.0f, BAL, -1.5f), 17f, 90f);

            // balcony booths + LED edge
            K.NeonStrip("Edge_N", g, new Vector3(0f, BAL - 0.33f, HZ - 5.02f), new Vector3(HX * 2f, 0.09f, 0.09f), "NeonPurple", new Color(0.55f, 0.12f, 1f), 2.6f, 13f);
            K.NeonStrip("Edge_W", g, new Vector3(-HX + 3.98f, BAL - 0.33f, -1.5f), new Vector3(0.09f, 0.09f, 17f), "NeonPurple", new Color(0.55f, 0.12f, 1f), 2.6f, 13f);
            for (int i = 0; i < 4; i++)
                Booth(g, "Booth_Balcony_" + i, new Vector3(-9f + i * 6f, BAL, HZ - 1.6f), 180f, 0.75f);

            // stair up, north-east corner
            var st = K.Group("Stairs_ToBalcony", g);
            const float rise = BAL / 22f;
            for (int i = 0; i < 22; i++)
                K.Box("Step_" + i, st, new Vector3(HX - 1.6f, (i + 1) * rise - rise * 0.5f, HZ - 6.2f - i * 0.29f),
                      new Vector3(2.2f, rise, 0.29f), "Marble");
            K.Box("Stringer", st, new Vector3(HX - 2.75f, BAL * 0.5f, HZ - 9.3f), new Vector3(0.12f, 1.0f, 7.4f), "Brass", new Vector3(29f, 0f, 0f), false);
            K.NeonStrip("Stair_Neon", st, new Vector3(HX - 2.72f, BAL * 0.5f + 0.55f, HZ - 9.3f), new Vector3(0.06f, 0.06f, 7.4f), "NeonPink",
                        new Color(1f, 0.3f, 0.66f), 1.8f, 7f);
            K.Marker("NAV_BalconyStairFoot", st, new Vector3(HX - 1.6f, 0f, HZ - 12.5f));
            K.Marker("NAV_BalconyTop", st, new Vector3(HX - 1.6f, BAL, HZ - 5.5f));
        }

        static void Column(Transform parent, string name, Vector3 p)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Box("Base", g, new Vector3(0f, 0.12f, 0f), new Vector3(0.78f, 0.24f, 0.78f), "Brass", default(Vector3), false);
            K.Box("Shaft", g, new Vector3(0f, BAL * 0.5f, 0f), new Vector3(0.58f, BAL, 0.58f), "WallPanel");
            K.Box("Cap", g, new Vector3(0f, BAL - 0.1f, 0f), new Vector3(0.8f, 0.2f, 0.8f), "Brass", default(Vector3), false);
            K.Box("Neon", g, new Vector3(0f, BAL * 0.5f, -0.31f), new Vector3(0.06f, BAL - 0.7f, 0.04f), "NeonMagenta", default(Vector3), false);
            K.Box("Neon2", g, new Vector3(0f, BAL * 0.5f, 0.31f), new Vector3(0.06f, BAL - 0.7f, 0.04f), "NeonMagenta", default(Vector3), false);
            // upper column continues to the ceiling
            K.Box("Upper", g, new Vector3(0f, (BAL + CH) * 0.5f, 0f), new Vector3(0.5f, CH - BAL, 0.5f), "WallPanel", default(Vector3), false);
        }

        static void Railing(Transform parent, string name, Vector3 p, float length, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Handrail", g, new Vector3(0f, 1.05f, 0f), new Vector3(length, 0.08f, 0.1f), "Brass", default(Vector3), false);
            K.Box("Kick", g, new Vector3(0f, 0.09f, 0f), new Vector3(length, 0.18f, 0.08f), "Brass", default(Vector3), false);
            K.Box("Glass", g, new Vector3(0f, 0.58f, 0f), new Vector3(length - 0.1f, 0.9f, 0.03f), "Glass", default(Vector3), false);
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 2f));
            for (int i = 0; i <= posts; i++)
                K.Box("Post_" + i, g, new Vector3(-length * 0.5f + i * (length / posts), 0.55f, 0f), new Vector3(0.06f, 1.1f, 0.06f), "Brass", default(Vector3), false);
        }

        // ───────────────────────── dance floor ─────────────────────────
        static void DanceFloor()
        {
            var g = K.Group("ZONE_DanceFloor", _root);
            g.localPosition = DF;

            K.Cyl("Platform", g, new Vector3(0f, 0.07f, 0f), DF_R * 2f, 0.15f, "Black", default(Vector3), true);
            // LED tile rings
            for (int ring = 0; ring < 4; ring++)
            {
                float r = 1.3f + ring * 1.15f;
                int n = 8 + ring * 6;
                for (int i = 0; i < n; i++)
                {
                    float a = i * Mathf.PI * 2f / n;
                    K.Box("Tile_" + ring + "_" + i, g, new Vector3(Mathf.Cos(a) * r, 0.152f, Mathf.Sin(a) * r),
                          new Vector3(0.62f, 0.012f, 0.62f), ((ring + i) % 2 == 0) ? "LedFloorA" : "LedFloorB",
                          new Vector3(0f, -a * Mathf.Rad2Deg, 0f), false);
                }
            }
            K.Cyl("Centre_Tile", g, new Vector3(0f, 0.152f, 0f), 1.6f, 0.012f, "LedFloorA");
            // rim light around the platform edge (segments, not a disc)
            var rim = K.Group("Edge_Ring", g);
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2f / 48f;
                K.Box("Seg_" + i, rim, new Vector3(Mathf.Cos(a) * (DF_R + 0.04f), 0.11f, Mathf.Sin(a) * (DF_R + 0.04f)),
                      new Vector3(0.1f, 0.07f, 0.72f), "NeonCyan", new Vector3(0f, -a * Mathf.Rad2Deg, 0f), false);
            }

            // dance floor wash lights
            K.AddLight(g, "Wash_A", new Vector3(0f, 3.2f, 0f), new Vector3(90f, 0f, 0f), LightType.Spot, new Color(1f, 0.15f, 0.62f), 6f, 9f, 100f, true);
            K.AddLight(g, "Wash_B", new Vector3(-3f, 2.6f, 2f), new Vector3(90f, 0f, 0f), LightType.Point, new Color(0.35f, 0.15f, 1f), 3.2f, 7f);
            K.AddLight(g, "Wash_C", new Vector3(3f, 2.6f, -2f), new Vector3(90f, 0f, 0f), LightType.Point, new Color(0.1f, 0.75f, 1f), 3.2f, 7f);

            // mirror ball
            var mb = K.Group("MirrorBall", g);
            K.Box("Drop", mb, new Vector3(0f, 6.4f, 0f), new Vector3(0.04f, 1.6f, 0.04f), "SteelDark", default(Vector3), false);
            K.Sphere("Ball", mb, new Vector3(0f, 5.4f, 0f), 1.1f, "Mirror");
            K.AddLight(mb, "BallLight", new Vector3(0f, 5.4f, 0f), Vector3.zero, LightType.Point, new Color(0.85f, 0.85f, 1f), 2.2f, 9f);

            K.Marker("NAV_DanceFloor", g, Vector3.zero);
            K.Marker("INTERACT_DanceFloor", g, new Vector3(0f, 0.15f, -2.5f));
        }

        // ───────────────────────── bar (south) ─────────────────────────
        static void BarZone()
        {
            var g = K.Group("ZONE_Bar", _root);
            Vector3 c = new Vector3(0f, 0f, -15f);      // arc centre, bar bulges north
            // arc bulges north, so a bigger radius sits closer to the dance floor:
            // customers (rStool) -> counter (rBar) -> back bar (rBack)
            const float rBar = 7.0f, rBack = 5.4f, rStool = 8.2f;
            const int seg = 16;
            const float span = 110f;

            var counter = K.Group("Counter", g);
            for (int i = 0; i < seg; i++)
            {
                float a = (-span * 0.5f + span * i / (seg - 1f)) * Mathf.Deg2Rad + Mathf.PI * 0.5f;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * rBar, 0f, Mathf.Sin(a) * rBar);
                float yaw = -a * Mathf.Rad2Deg + 90f;
                K.Box("Body_" + i, counter, new Vector3(p.x, 0.55f, p.z), new Vector3(1.45f, 1.1f, 0.7f), "WallPanel", new Vector3(0f, yaw, 0f));
                K.Box("Top_" + i, counter, new Vector3(p.x, 1.13f, p.z), new Vector3(1.5f, 0.07f, 0.92f), "Marble", new Vector3(0f, yaw, 0f), false);
                K.Box("Led_" + i, counter, new Vector3(p.x, 0.28f, p.z), new Vector3(1.45f, 0.07f, 0.72f), "NeonPink", new Vector3(0f, yaw, 0f), false);
                K.Box("Kick_" + i, counter, new Vector3(p.x, 0.06f, p.z), new Vector3(1.45f, 0.12f, 0.78f), "Brass", new Vector3(0f, yaw, 0f), false);
            }

            // back bar: bottle shelves with LED backlight
            var back = K.Group("BackBar", g);
            for (int i = 0; i < seg; i++)
            {
                float a = (-span * 0.5f + span * i / (seg - 1f)) * Mathf.Deg2Rad + Mathf.PI * 0.5f;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * rBack, 0f, Mathf.Sin(a) * rBack);
                float yaw = -a * Mathf.Rad2Deg + 90f;
                K.Box("Case_" + i, back, new Vector3(p.x, 1.7f, p.z), new Vector3(1.5f, 3.4f, 0.45f), "Black", new Vector3(0f, yaw, 0f), false);
                for (int s = 0; s < 4; s++)
                {
                    K.Box("Shelf_" + i + "_" + s, back, new Vector3(p.x, 0.8f + s * 0.62f, p.z), new Vector3(1.45f, 0.05f, 0.4f), "Brass", new Vector3(0f, yaw, 0f), false);
                    K.Box("ShelfLed_" + i + "_" + s, back, new Vector3(p.x, 0.86f + s * 0.62f, p.z), new Vector3(1.45f, 0.04f, 0.36f),
                          (s % 2 == 0) ? "NeonMagenta" : "NeonPurple", new Vector3(0f, yaw, 0f), false);
                    for (int b = 0; b < 4; b++)
                    {
                        Vector3 off = Quaternion.Euler(0f, yaw, 0f) * new Vector3(-0.55f + b * 0.36f, 0f, -0.02f);
                        K.Box("Bottle_" + i + "_" + s + "_" + b, back, new Vector3(p.x + off.x, 1.02f + s * 0.62f, p.z + off.z),
                              new Vector3(0.11f, 0.3f, 0.11f), "Bottle", new Vector3(0f, yaw, 0f), false);
                    }
                }
            }

            // stools on the customer side
            for (int i = 0; i < 11; i++)
            {
                float a = (-span * 0.46f + span * 0.92f * i / 10f) * Mathf.Deg2Rad + Mathf.PI * 0.5f;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * rStool, 0f, Mathf.Sin(a) * rStool);
                Stool(g, "Stool_" + i, p);
            }

            // service well + till
            K.Box("Till", g, new Vector3(0f, 1.3f, -8.2f), new Vector3(0.4f, 0.32f, 0.3f), "SteelDark", default(Vector3), false);
            K.Box("Till_Screen", g, new Vector3(0f, 1.34f, -8.0f), new Vector3(0.34f, 0.24f, 0.02f), "NeonCyan", default(Vector3), false);

            K.AddLight(g, "BarGlow_L", new Vector3(-4.5f, 2.4f, -10.5f), Vector3.zero, LightType.Point, new Color(1f, 0.2f, 0.6f), 3.4f, 8f);
            K.AddLight(g, "BarGlow_R", new Vector3(4.5f, 2.4f, -10.5f), Vector3.zero, LightType.Point, new Color(1f, 0.2f, 0.6f), 3.4f, 8f);
            K.AddLight(g, "BarGlow_C", new Vector3(0f, 2.4f, -9.4f), Vector3.zero, LightType.Point, new Color(0.6f, 0.2f, 1f), 3.0f, 8f);

            K.Marker("NAV_Bar", g, new Vector3(0f, 0f, -6.4f));
            K.Marker("INTERACT_Bartender", g, new Vector3(0f, 0f, -6.6f));
        }

        static void Stool(Transform parent, string name, Vector3 p)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Cyl("Seat", g, new Vector3(0f, 0.72f, 0f), 0.4f, 0.14f, "VelvetPink");
            K.Cyl("Post", g, new Vector3(0f, 0.35f, 0f), 0.07f, 0.72f, "Brass");
            K.Cyl("Base", g, new Vector3(0f, 0.03f, 0f), 0.42f, 0.06f, "Brass");
            K.Cyl("FootRing", g, new Vector3(0f, 0.24f, 0f), 0.32f, 0.04f, "Brass");
        }

        // ───────────────────────── stage (east) ─────────────────────────
        static void StageZone()
        {
            var g = K.Group("ZONE_Stage", _root);

            K.Box("Deck", g, new Vector3(11.8f, STAGE_Y * 0.5f, 0f), new Vector3(6f, STAGE_Y, 12f), "Black");
            K.Box("Deck_Top", g, new Vector3(11.8f, STAGE_Y + 0.01f, 0f), new Vector3(6f, 0.03f, 12f), "Wood", default(Vector3), false);
            K.NeonStrip("Deck_Edge", g, new Vector3(8.82f, STAGE_Y - 0.12f, 0f), new Vector3(0.08f, 0.1f, 12f), "NeonCyan", new Color(0.1f, 0.8f, 1f), 3f, 10f);
            for (int i = 0; i < 3; i++)
                K.Box("Step_" + i, g, new Vector3(8.5f - i * 0.35f, STAGE_Y - 0.15f - i * 0.3f, 0f), new Vector3(0.35f, 0.3f, 4f), "Black");

            // LED backdrop wall
            K.Box("Backdrop", g, new Vector3(14.6f, 4.2f, 0f), new Vector3(0.3f, 7.2f, 11.6f), "Black");
            for (int i = 0; i < 6; i++)
                for (int k = 0; k < 3; k++)
                    K.Box("Led_" + i + "_" + k, g, new Vector3(14.4f, 1.8f + k * 2.1f, -4.6f + i * 1.85f),
                          new Vector3(0.06f, 1.9f, 1.7f), (i + k) % 3 == 0 ? "NeonMagenta" : ((i + k) % 3 == 1 ? "NeonPurple" : "NeonBlue"), default(Vector3), false);

            // DJ booth
            var dj = K.Group("DJBooth", g);
            dj.localPosition = new Vector3(11.8f, STAGE_Y, 0f);
            K.Box("Body", dj, new Vector3(0f, 0.55f, 0f), new Vector3(2.6f, 1.1f, 1.0f), "Black");
            K.Box("Top", dj, new Vector3(0f, 1.13f, 0f), new Vector3(2.8f, 0.07f, 1.2f), "Marble", default(Vector3), false);
            K.Box("Face_Led", dj, new Vector3(-0.52f, 0.55f, 0f), new Vector3(0.05f, 0.9f, 0.9f), "NeonMagenta", new Vector3(0f, 90f, 0f), false);
            K.Box("Deck_L", dj, new Vector3(0f, 1.2f, -0.55f), new Vector3(0.5f, 0.08f, 0.42f), "SteelDark", default(Vector3), false);
            K.Box("Deck_R", dj, new Vector3(0f, 1.2f, 0.55f), new Vector3(0.5f, 0.08f, 0.42f), "SteelDark", default(Vector3), false);
            K.Box("Mixer", dj, new Vector3(0f, 1.2f, 0f), new Vector3(0.4f, 0.09f, 0.5f), "SteelDark", default(Vector3), false);
            K.Marker("NPC_DJ", dj, new Vector3(0.9f, 0f, 0f));

            // speaker stacks
            for (int i = 0; i < 2; i++)
            {
                float z = i == 0 ? -5.2f : 5.2f;
                var sp = K.Group("Speakers_" + i, g);
                sp.localPosition = new Vector3(9.6f, 0f, z);
                K.Box("Sub", sp, new Vector3(0f, 0.5f, 0f), new Vector3(1.1f, 1.0f, 1.2f), "Black");
                K.Box("Top", sp, new Vector3(0f, 1.6f, 0f), new Vector3(0.9f, 1.2f, 1.0f), "Black");
                K.Cyl("Driver", sp, new Vector3(-0.47f, 0.5f, 0f), 0.7f, 0.06f, "SteelDark", new Vector3(0f, 0f, 90f));
                K.Cyl("Driver2", sp, new Vector3(-0.47f, 1.6f, 0f), 0.5f, 0.06f, "SteelDark", new Vector3(0f, 0f, 90f));
            }

            // stage lighting
            K.AddLight(g, "StageKey", new Vector3(10.5f, 5.4f, 0f), new Vector3(62f, -90f, 0f), LightType.Spot, new Color(1f, 0.25f, 0.7f), 6f, 14f, 70f, true);
            K.AddLight(g, "StageFill_A", new Vector3(12f, 4.6f, -4f), new Vector3(50f, -90f, 0f), LightType.Spot, new Color(0.5f, 0.15f, 1f), 4f, 11f, 65f);
            K.AddLight(g, "StageFill_B", new Vector3(12f, 4.6f, 4f), new Vector3(50f, -90f, 0f), LightType.Spot, new Color(0.1f, 0.7f, 1f), 4f, 11f, 65f);

            K.Marker("NAV_Stage", g, new Vector3(10.5f, STAGE_Y, 0f));
        }

        // ───────────────────────── VIP lounge (west) ─────────────────────────
        static void VipZone()
        {
            var g = K.Group("ZONE_VIP", _root);

            K.Box("Platform", g, new Vector3(-11.6f, VIP_Y * 0.5f, 0f), new Vector3(6.8f, VIP_Y, 20f), "FloorVip");
            K.Box("Platform_Edge", g, new Vector3(-8.25f, VIP_Y * 0.5f, 0f), new Vector3(0.12f, VIP_Y, 20f), "Brass", default(Vector3), false);
            K.NeonStrip("Platform_Led", g, new Vector3(-8.18f, VIP_Y - 0.1f, 0f), new Vector3(0.06f, 0.07f, 20f), "NeonPink", new Color(1f, 0.3f, 0.66f), 2.6f, 11f);
            // steps up at two points
            for (int s = 0; s < 2; s++)
            {
                float z = s == 0 ? 5f : -5f;
                K.Box("Step_" + s, g, new Vector3(-8.0f, VIP_Y * 0.5f, z), new Vector3(0.7f, VIP_Y * 0.55f, 2.4f), "Marble");
                Stanchion(g, "Rope_" + s + "_A", new Vector3(-7.6f, 0f, z + 1.6f));
                Stanchion(g, "Rope_" + s + "_B", new Vector3(-7.6f, 0f, z - 1.6f));
            }

            // four booths against the west wall
            for (int i = 0; i < 4; i++)
                Booth(g, "Booth_VIP_" + i, new Vector3(-13.4f, VIP_Y, -6.9f + i * 4.6f), 90f, 1f);

            // velvet curtains between the booths
            for (int i = 0; i < 5; i++)
                K.Box("Curtain_" + i, g, new Vector3(-14.55f, VIP_Y + 1.7f, -9.2f + i * 4.6f), new Vector3(0.16f, 3.4f, 0.9f), "Velvet", default(Vector3), false);

            // low tables + bottle service
            for (int i = 0; i < 4; i++)
            {
                float z = -6.9f + i * 4.6f;
                K.Cyl("Table_" + i, g, new Vector3(-11.5f, VIP_Y + 0.5f, z), 0.9f, 0.06f, "Marble", default(Vector3), true);
                K.Cyl("TableLeg_" + i, g, new Vector3(-11.5f, VIP_Y + 0.25f, z), 0.12f, 0.5f, "Brass");
                K.Cyl("TableLed_" + i, g, new Vector3(-11.5f, VIP_Y + 0.03f, z), 0.7f, 0.04f, "NeonPurple");
                if (i % 2 == 0)
                {
                    K.Box("IceBucket_" + i, g, new Vector3(-11.5f, VIP_Y + 0.68f, z), new Vector3(0.3f, 0.3f, 0.3f), "Brass", default(Vector3), false);
                    K.Box("Bottle_" + i, g, new Vector3(-11.5f, VIP_Y + 0.95f, z), new Vector3(0.12f, 0.36f, 0.12f), "NeonMagenta", default(Vector3), false);
                }
                for (int k = 0; k < 3; k++)
                    K.Box("Glass_" + i + "_" + k, g, new Vector3(-11.1f + k * 0.22f, VIP_Y + 0.6f, z + 0.3f), new Vector3(0.07f, 0.14f, 0.07f), "Glass", default(Vector3), false);
            }

            // private bar niche at the far end
            K.Box("Niche", g, new Vector3(-13.9f, VIP_Y + 1.4f, 9.2f), new Vector3(1.8f, 2.8f, 2.2f), "Black");
            for (int s = 0; s < 3; s++)
                K.Box("NicheShelf_" + s, g, new Vector3(-13.4f, VIP_Y + 0.9f + s * 0.55f, 9.2f), new Vector3(0.6f, 0.04f, 2.0f), "NeonPurple", default(Vector3), false);

            K.AddLight(g, "VipGlow_A", new Vector3(-11.5f, 2.6f, -5f), Vector3.zero, LightType.Point, new Color(1f, 0.25f, 0.55f), 3.0f, 9f);
            K.AddLight(g, "VipGlow_B", new Vector3(-11.5f, 2.6f, 5f), Vector3.zero, LightType.Point, new Color(0.7f, 0.15f, 1f), 3.0f, 9f);

            K.Marker("NAV_VIP", g, new Vector3(-11f, VIP_Y, 0f));
            K.Marker("TARGET_VipBooth", g, new Vector3(-12.6f, VIP_Y, -6.9f));
            K.Marker("EAVESDROP_Position", g, new Vector3(-7.9f, 0f, -8.6f));
        }

        /// <summary>Curved banquette booth: back, seat, arms.</summary>
        static void Booth(Transform parent, string name, Vector3 p, float yaw, float scale)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            g.localScale = Vector3.one * scale;
            K.Box("Seat", g, new Vector3(0f, 0.24f, 0f), new Vector3(1.0f, 0.48f, 3.2f), "Leather");
            K.Box("Cushion", g, new Vector3(0.06f, 0.5f, 0f), new Vector3(0.95f, 0.12f, 3.1f), "VelvetPink", default(Vector3), false);
            K.Box("Back", g, new Vector3(-0.42f, 0.85f, 0f), new Vector3(0.2f, 1.3f, 3.2f), "VelvetPink", default(Vector3), false);
            K.Box("Back_Trim", g, new Vector3(-0.32f, 1.48f, 0f), new Vector3(0.1f, 0.08f, 3.2f), "Brass", default(Vector3), false);
            K.Box("Arm_A", g, new Vector3(0f, 0.62f, -1.55f), new Vector3(1.0f, 0.3f, 0.18f), "Leather", default(Vector3), false);
            K.Box("Arm_B", g, new Vector3(0f, 0.62f, 1.55f), new Vector3(1.0f, 0.3f, 0.18f), "Leather", default(Vector3), false);
            K.Box("Under_Led", g, new Vector3(0.1f, 0.04f, 0f), new Vector3(0.9f, 0.05f, 3.0f), "NeonMagenta", default(Vector3), false);
        }

        static void Stanchion(Transform parent, string name, Vector3 p)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Cyl("Base", g, new Vector3(0f, 0.03f, 0f), 0.32f, 0.06f, "Brass");
            K.Cyl("Post", g, new Vector3(0f, 0.48f, 0f), 0.07f, 0.9f, "Brass");
            K.Sphere("Cap", g, new Vector3(0f, 0.97f, 0f), 0.11f, "Brass");
        }

        // ───────────────────────── entrance (north) ─────────────────────────
        static void Entrance()
        {
            var g = K.Group("ZONE_Entrance", _root);

            // vestibule walls forming a short lobby inside the north wall
            K.Box("Lobby_W", g, new Vector3(-3.2f, 2.1f, HZ - 3.2f), new Vector3(0.3f, 4.2f, 3.6f), "WallPanel");
            K.Box("Lobby_E", g, new Vector3(3.2f, 2.1f, HZ - 3.2f), new Vector3(0.3f, 4.2f, 3.6f), "WallPanel");
            K.Box("Lobby_Head", g, new Vector3(0f, 3.4f, HZ - 3.2f), new Vector3(6.4f, 1.6f, 3.6f), "WallPanel", default(Vector3), false);

            // doors from the car park
            K.Box("DoorFrame", g, new Vector3(0f, 1.35f, HZ - 0.2f), new Vector3(4.4f, 2.7f, 0.2f), "Brass");
            K.Box("Door_L", g, new Vector3(-1.0f, 1.2f, HZ - 0.32f), new Vector3(1.8f, 2.4f, 0.08f), "Glass", default(Vector3), false);
            K.Box("Door_R", g, new Vector3(1.0f, 1.2f, HZ - 0.32f), new Vector3(1.8f, 2.4f, 0.08f), "Glass", default(Vector3), false);
            K.NeonStrip("Door_Neon", g, new Vector3(0f, 2.78f, HZ - 0.42f), new Vector3(4.6f, 0.09f, 0.09f), "NeonMagenta", new Color(1f, 0.08f, 0.6f), 3.4f, 9f);
            K.Marker("DOOR_ToParkingScene", g, new Vector3(0f, 0f, HZ - 0.4f));

            // beaded curtains tied back either side – the middle stays clear for sight lines
            for (int s = 0; s < 2; s++)
                for (int i = 0; i < 5; i++)
                {
                    float x = (s == 0 ? -2.75f : 1.85f) + i * 0.22f;
                    K.Box("Bead_" + s + "_" + i, g, new Vector3(x, 2.0f, HZ - 5.1f), new Vector3(0.025f, 3.0f, 0.025f), "BrassDark", default(Vector3), false);
                }
            K.Box("Pelmet", g, new Vector3(0f, 3.6f, HZ - 5.1f), new Vector3(6.4f, 0.4f, 0.16f), "Velvet", default(Vector3), false);

            // coat check
            var cc = K.Group("CoatCheck", g);
            cc.localPosition = new Vector3(-4.8f, 0f, HZ - 2.2f);
            K.Box("Counter", cc, new Vector3(0f, 0.55f, 0f), new Vector3(2.4f, 1.1f, 0.7f), "WallPanel");
            K.Box("Top", cc, new Vector3(0f, 1.13f, 0f), new Vector3(2.5f, 0.07f, 0.8f), "Marble", default(Vector3), false);
            K.Box("Led", cc, new Vector3(0f, 0.3f, -0.37f), new Vector3(2.3f, 0.06f, 0.03f), "NeonCyan", default(Vector3), false);
            K.Sign("Sign", cc, new Vector3(0f, 1.7f, -0.1f), new Vector2(2.0f, 0.28f), "COAT CHECK", new Color(0.95f, 0.8f, 0.95f), 0f);
            K.Marker("NPC_CoatCheck", cc, new Vector3(0f, 0f, 0.7f));

            // door host + rope
            Stanchion(g, "Host_Rope_A", new Vector3(-2.4f, 0f, HZ - 6.2f));
            Stanchion(g, "Host_Rope_B", new Vector3(2.4f, 0f, HZ - 6.2f));
            K.AddLight(g, "LobbyLight", new Vector3(0f, 3.2f, HZ - 3.2f), new Vector3(90f, 0f, 0f), LightType.Spot, new Color(1f, 0.35f, 0.7f), 4f, 8f, 90f);

            K.Marker("NAV_Entrance", g, new Vector3(0f, 0f, HZ - 6.5f));
        }

        static void BackOfHouse()
        {
            var g = K.Group("BACK_OF_HOUSE", _root);

            // toilets corridor, north-west
            K.Box("Corridor_Wall", g, new Vector3(-7.6f, 2.1f, HZ - 3.4f), new Vector3(0.3f, 4.2f, 4f), "WallPanel");
            K.Box("WC_Door_M", g, new Vector3(-7.4f, 1.05f, HZ - 4.6f), new Vector3(0.08f, 2.1f, 0.9f), "Wood", default(Vector3), false);
            K.Box("WC_Door_F", g, new Vector3(-7.4f, 1.05f, HZ - 2.4f), new Vector3(0.08f, 2.1f, 0.9f), "Wood", default(Vector3), false);
            K.Sign("WC_Sign", g, new Vector3(-7.3f, 2.4f, HZ - 3.5f), new Vector2(1.4f, 0.24f), "REST ROOMS", new Color(0.9f, 0.8f, 0.95f), -90f);

            // staff door – the route deeper into the building for later chapters
            K.Box("StaffDoor", g, new Vector3(-14.6f, 1.15f, -11.4f), new Vector3(0.12f, 2.3f, 1.1f), "SteelDark");
            K.Box("StaffDoor_Bar", g, new Vector3(-14.45f, 1.05f, -11.4f), new Vector3(0.06f, 0.08f, 0.9f), "Brass", default(Vector3), false);
            K.Sign("StaffSign", g, new Vector3(-14.4f, 2.6f, -11.4f), new Vector2(1.6f, 0.24f), "STAFF ONLY", new Color(0.95f, 0.3f, 0.35f), 90f);
            K.Box("Exit_Light", g, new Vector3(-14.3f, 2.72f, -11.4f), new Vector3(0.06f, 0.24f, 0.66f), "ExitGreen", default(Vector3), false);
            K.Marker("DOOR_StaffOnly", g, new Vector3(-14.2f, 0f, -11.4f));

            // scattered cocktail tables around the floor
            float[,] tp = { { -5.5f, 5.5f }, { 5.5f, 6.5f }, { -6.5f, -6.0f }, { 6.2f, -5.2f }, { 0f, 7.6f }, { -3.4f, 8.6f }, { 3.6f, 8.8f } };
            for (int i = 0; i < tp.GetLength(0); i++)
                CocktailTable(g, "Table_" + i, new Vector3(tp[i, 0], 0f, tp[i, 1]));
        }

        static void CocktailTable(Transform parent, string name, Vector3 p)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Cyl("Top", g, new Vector3(0f, 1.06f, 0f), 0.7f, 0.05f, "Marble", default(Vector3), true);
            K.Cyl("Post", g, new Vector3(0f, 0.53f, 0f), 0.09f, 1.06f, "Brass");
            K.Cyl("Base", g, new Vector3(0f, 0.03f, 0f), 0.5f, 0.06f, "Brass");
            K.Cyl("Led", g, new Vector3(0f, 1.02f, 0f), 0.55f, 0.03f, "NeonPurple");
            for (int i = 0; i < 2; i++)
                K.Box("Glass_" + i, g, new Vector3(-0.12f + i * 0.24f, 1.16f, 0.05f), new Vector3(0.07f, 0.16f, 0.07f), "Glass", default(Vector3), false);
        }

        // ───────────────────────── rigging & atmosphere hardware ─────────────────────────
        static void Rigging()
        {
            var g = K.Group("RIGGING", _root);

            // truss grid over the dance floor
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -DF_R - 0.6f : DF_R + 0.6f;
                Truss(g, "Truss_X" + i, new Vector3(x + DF.x, 6.6f, DF.z), 14f, 0f);
            }
            Truss(g, "Truss_Z0", new Vector3(DF.x, 6.9f, DF.z - DF_R - 0.6f), 14f, 90f);
            Truss(g, "Truss_Z1", new Vector3(DF.x, 6.9f, DF.z + DF_R + 0.6f), 14f, 90f);

            // moving heads hanging off the truss
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Vector3 p = new Vector3(DF.x + Mathf.Cos(a) * (DF_R + 0.6f), 6.3f, DF.z + Mathf.Sin(a) * (DF_R + 0.6f));
                MovingHead(g, "Head_" + i, p, (i % 2 == 0) ? "NeonMagenta" : "NeonCyan", i % 3 == 0);
            }

            // ceiling LED bars
            for (int i = 0; i < 6; i++)
                K.Box("CeilBar_" + i, g, new Vector3(-11f + i * 4.4f, CH - 0.15f, 0f), new Vector3(0.12f, 0.1f, HZ * 1.5f),
                      (i % 2 == 0) ? "NeonPurple" : "NeonBlue", default(Vector3), false);

            // hanging speaker clusters (the reference has them over the floor)
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var sp = K.Group("FlySpeaker_" + i, g);
                sp.localPosition = new Vector3(DF.x + Mathf.Cos(a) * 6.8f, 5.6f, DF.z + Mathf.Sin(a) * 6.8f);
                K.Box("Rod", sp, new Vector3(0f, 1.4f, 0f), new Vector3(0.04f, 2.8f, 0.04f), "SteelDark", default(Vector3), false);
                K.Box("Box", sp, Vector3.zero, new Vector3(0.7f, 0.8f, 0.7f), "Black", default(Vector3), false);
                K.Cyl("Cone", sp, new Vector3(0f, -0.42f, 0f), 0.55f, 0.06f, "SteelDark");
            }
        }

        static void Truss(Transform parent, string name, Vector3 p, float length, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            for (int i = 0; i < 4; i++)
                K.Box("Chord_" + i, g, new Vector3(i < 2 ? -0.18f : 0.18f, i % 2 == 0 ? -0.18f : 0.18f, 0f),
                      new Vector3(0.06f, 0.06f, length), "Truss", default(Vector3), false);
            int n = Mathf.RoundToInt(length / 0.9f);
            for (int i = 0; i <= n; i++)
                K.Box("Web_" + i, g, new Vector3(0f, 0f, -length * 0.5f + i * (length / n)), new Vector3(0.42f, 0.42f, 0.04f), "Truss", new Vector3(0f, 0f, 45f), false);
            K.Box("Drop_A", g, new Vector3(0f, 0.8f, -length * 0.35f), new Vector3(0.03f, 1.6f, 0.03f), "SteelDark", default(Vector3), false);
            K.Box("Drop_B", g, new Vector3(0f, 0.8f, length * 0.35f), new Vector3(0.03f, 1.6f, 0.03f), "SteelDark", default(Vector3), false);
        }

        static void MovingHead(Transform parent, string name, Vector3 p, string lens, bool realLight)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Box("Yoke", g, new Vector3(0f, 0.12f, 0f), new Vector3(0.3f, 0.24f, 0.14f), "SteelDark", default(Vector3), false);
            K.Box("Head", g, Vector3.zero, new Vector3(0.22f, 0.3f, 0.22f), "Black", default(Vector3), false);
            K.Cyl("Lens", g, new Vector3(0f, -0.17f, 0f), 0.18f, 0.05f, lens);
            if (!realLight) return;
            Color c = lens == "NeonMagenta" ? new Color(1f, 0.12f, 0.6f) : new Color(0.1f, 0.8f, 1f);
            K.AddLight(g, "Beam", new Vector3(0f, -0.2f, 0f), new Vector3(70f, 0f, 0f), LightType.Spot, c, 5f, 12f, 26f);
        }

        // ───────────────────────── crowd ─────────────────────────
        static void Crowd()
        {
            var g = K.Group("CROWD", _root);
            // dancers
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f + 0.3f;
                float r = 1.6f + (i % 3) * 1.3f;
                var npc = K.PlaceHuman(P_NPC, g, new Vector3(DF.x + Mathf.Cos(a) * r, 0.15f, DF.z + Mathf.Sin(a) * r), -a * Mathf.Rad2Deg);
                if (npc != null) npc.name = "NPC_Dancer_" + i.ToString("00");
            }
            // drinkers at the bar
            for (int i = 0; i < 6; i++)
            {
                float a = (-46f + 92f * i / 5f) * Mathf.Deg2Rad + Mathf.PI * 0.5f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 8.7f, 0f, -15f + Mathf.Sin(a) * 8.7f);
                var npc = K.PlaceHuman(P_NPC, g, p, -a * Mathf.Rad2Deg + 180f);
                if (npc != null) npc.name = "NPC_BarPatron_" + i.ToString("00");
            }
            // standing groups
            float[,] sp = { { -5.5f, 6.4f }, { 5.6f, 7.2f }, { -6.8f, -6.5f }, { 6.6f, -5.6f }, { 0.8f, 8.4f } };
            for (int i = 0; i < sp.GetLength(0); i++)
            {
                var npc = K.PlaceHuman(P_NPC, g, new Vector3(sp[i, 0], 0f, sp[i, 1]), i * 47f);
                if (npc != null) npc.name = "NPC_Guest_" + i.ToString("00");
            }
            // bartenders
            for (int i = 0; i < 2; i++)
            {
                var npc = K.PlaceHuman(P_NPC, g, new Vector3(-2.2f + i * 4.4f, 0f, -8.9f), 0f);
                if (npc != null) npc.name = "NPC_Bartender_" + i;
            }
        }

        // ───────────────────────── gameplay ─────────────────────────
        static void Gameplay()
        {
            var g = K.Group("GAMEPLAY", _root);

            K.Marker("PlayerSpawn_Entrance", g, new Vector3(0f, 0.2f, HZ - 1.5f));
            var player = K.Place(P_PLAYER, g, new Vector3(0f, 1.2f, HZ - 1.5f), 180f);
            if (player != null) player.name = "player";
            K.Marker("PlayerSpawn_DanceFloor", g, new Vector3(0f, 0.2f, 4f));

            // security, per the level sketch: three guards patrolling the VIP edge
            var gd = K.Group("GUARDS", g);
            Guard(gd, "Guard_01_VipNorth", new Vector3(-7.9f, 0f, 6.5f), 90f,
                  new Vector3[] { new Vector3(-7.9f, 0f, 6.5f), new Vector3(-4.5f, 0f, 3.5f) });
            Guard(gd, "Guard_02_VipMid", new Vector3(-7.9f, 0f, 0f), 90f,
                  new Vector3[] { new Vector3(-7.9f, 0f, 0f), new Vector3(-4.2f, 0f, -2.5f) });
            Guard(gd, "Guard_03_VipSouth", new Vector3(-7.9f, 0f, -6.5f), 110f,
                  new Vector3[] { new Vector3(-7.9f, 0f, -6.5f), new Vector3(-5.0f, 0f, -8.5f) });
            Guard(gd, "Guard_04_Door", new Vector3(2.8f, 0f, HZ - 6.4f), 180f,
                  new Vector3[] { new Vector3(2.8f, 0f, HZ - 6.4f) });

            // the two men Kem overhears talking about the SD card
            var tgt = K.Group("TARGETS", g);
            var a = K.PlaceHuman(P_NPC, tgt, new Vector3(-11.2f, VIP_Y, -6.2f), 200f);
            if (a != null) a.name = "NPC_TARGET_Suit_A";
            var b = K.PlaceHuman(P_NPC, tgt, new Vector3(-12.2f, VIP_Y, -7.6f), 20f);
            if (b != null) b.name = "NPC_TARGET_Suit_B";
            K.Marker("DIALOGUE_SDCardTalk", tgt, new Vector3(-11.7f, VIP_Y + 1.6f, -6.9f));

            // cover / hiding
            var hide = K.Group("CoverPoints", g);
            K.Marker("Hide_Column_W0", hide, new Vector3(-10.4f, 0f, 3.5f));
            K.Marker("Hide_Column_W1", hide, new Vector3(-10.4f, 0f, -2.5f));
            K.Marker("Hide_Curtain", hide, new Vector3(-8.9f, 0f, -9.2f));
            K.Marker("Hide_BehindBar", hide, new Vector3(0f, 0f, -10.4f));
            K.Marker("Hide_Crowd", hide, new Vector3(0f, 0.15f, -1f));
            K.Marker("Hide_Speakers", hide, new Vector3(9.6f, 0f, -5.2f));
            K.Marker("Hide_CoatCheck", hide, new Vector3(-5.4f, 0f, HZ - 3.4f));

            // objectives
            var obj = K.Group("Objectives", g);
            K.Marker("OBJ_01_EnterClub", obj, new Vector3(0f, 0f, HZ - 6.5f));
            K.Marker("OBJ_02_CrossDanceFloor", obj, DF);
            K.Marker("OBJ_03_ReachVipEdge", obj, new Vector3(-7.9f, 0f, -8.6f));
            K.Marker("OBJ_04_OverhearSuits", obj, new Vector3(-8.6f, 0f, -7.4f));
            K.Marker("OBJ_05_LeaveViaStaffDoor", obj, new Vector3(-14.2f, 0f, -11.4f));

            // camera anchors
            var cam = K.Group("CutsceneCameras", g);
            CamAnchor(cam, "CUT_01_EnterHall", new Vector3(0f, 1.75f, 9.5f), new Vector3(2f, 180f, 0f));
            CamAnchor(cam, "CUT_02_DanceFloorReveal", new Vector3(6.5f, 2.6f, 7.5f), new Vector3(10f, -145f, 0f));
            CamAnchor(cam, "CUT_03_VipApproach", new Vector3(-5.5f, 1.7f, -4.5f), new Vector3(2f, -70f, 0f));
            CamAnchor(cam, "CUT_04_Eavesdrop", new Vector3(-8.6f, 1.6f, -8.8f), new Vector3(2f, -32f, 0f));
            CamAnchor(cam, "CUT_05_StageWide", new Vector3(-2f, 3.2f, -2f), new Vector3(6f, 78f, 0f));
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
            r.localEulerAngles = new Vector3(0f, -yaw, 0f);
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
