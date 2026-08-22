using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using K = SecretsThatBreathe.LevelTools.LevelKit;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// CHAPTER 2 - QUD CLUB interior. Entered from the B1 car park scene.
    ///
    ///        +------------- ENTRANCE (north, z +16) -------------+
    ///   VIP  |                                                   |  STAGE
    ///  ZONE  |               ( ) DANCE FLOOR                     |  ZONE
    ///  (west)|                                                   |  (east)
    ///        +--------------- BAR ZONE (south) ------------------+
    ///
    /// The room is 36 x 32 m. Every gap between two zones is at least
    /// <see cref="LevelKit.Nav.PathClear"/> wide and every doorway at least
    /// <see cref="LevelKit.Nav.DoorClear"/>, so the player capsule never wedges.
    ///
    /// Menu: Tools > Secrets That Breathe > Build Chapter 2 Club
    /// </summary>
    public static class Ch2ClubBuilder
    {
        // ── master dimensions (metres) ──
        public const float HX = 18f;         // half width  -> 36 m
        public const float HZ = 16f;         // half depth  -> 32 m
        public const float CH = 8.0f;        // main hall height
        public const float BAL = 4.2f;       // balcony walking level

        // dance floor, pushed north so the bar arc no longer crowds it
        public const float DF_R = 5.5f;
        public static readonly Vector3 DF = new Vector3(0f, 0f, 2f);

        // VIP platform occupies the west band, stage the east band
        public const float VIP_EDGE = -11.2f;         // east edge of the VIP platform
        public const float VIP_CX = -14.6f;           // platform centre
        public const float VIP_HALF_Z = 9f;
        public const float VIP_Y = 0.45f;

        public const float STAGE_CX = 14.8f;          // stage deck centre
        public const float STAGE_EDGE = 11.8f;        // west edge of the deck
        public const float STAGE_HALF_Z = 5f;
        public const float STAGE_Y = 0.9f;

        // bar arc centre, well south of the dance floor
        public static readonly Vector3 BAR_C = new Vector3(0f, 0f, -19f);

        // staff exit recess in the west wall
        public const float STAFF_DOOR_Z = -13f;

        // balcony: north run plus a west return
        public const float BAL_N_Z = 12f;             // south edge of the north balcony slab
        public const float BAL_W_X = -14f;            // east edge of the west balcony slab

        public const string ScenePath = "Assets/MainScenes/Main2_Club/Main2_Club.unity";
        public const string DataFolder = "Assets/MainScenes/Main2_Club";
        public const string MatFolder = DataFolder + "/Materials";

        const string P_PLAYER = "Assets/Champ&Kichzz/Prefab/Player/player.prefab";
        const string P_NPC = "Assets/Champ&Kichzz/Prefab/Npc/NPC.prefab";

        static Transform _root;
        static Transform _env, _struct, _circ, _dress, _light, _actors, _play;

        [MenuItem("Tools/Secrets That Breathe/Build Chapter 2 Club", false, 12)]
        public static void BuildScene() { BuildScene(true); }

        public static void BuildScene(bool askToSave)
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Ch2Club] leave play mode first."); return; }
            if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            K.EnsureFolder(MatFolder);
            K.ResetPlaced();
            Materials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _root = new GameObject("=== CH2 QUD CLUB ===").transform;
            K.BuildCategories(_root);
            _env = K.Category(_root, K.Cat.Env);
            _struct = K.Category(_root, K.Cat.Structure);
            _circ = K.Category(_root, K.Cat.Circulation);
            _dress = K.Category(_root, K.Cat.Dressing);
            _light = K.Category(_root, K.Cat.Lighting);
            _actors = K.Category(_root, K.Cat.Actors);
            _play = K.Category(_root, K.Cat.Gameplay);

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
            Ch2Act2Wiring.WireClub(_root);

            GroundProps();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Ch2Club] built -> " + ScenePath);
            Debug.Log(LevelAudit.Format("CH2 QUD CLUB", LevelAudit.Run(_root, 0.4f, CH)));
        }

        /// <summary>Sits every placed prefab exactly on whatever it is standing on.</summary>
        static void GroundProps()
        {
            Physics.SyncTransforms();
            var placed = K.Placed;
            for (int i = 0; i < placed.Count; i++) K.SnapDown(placed[i]);
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
            K.Mat("Alu", new Color(0.70f, 0.715f, 0.735f), 0.88f, 0.72f);

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
            var g = K.Group("Atmosphere", _env);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.115f, 0.045f, 0.135f);
            RenderSettings.ambientEquatorColor = new Color(0.085f, 0.035f, 0.10f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.02f, 0.05f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.16f, 0.04f, 0.18f);
            RenderSettings.fogDensity = 0.017f;
            // เหลือไว้นิดหน่อยให้พื้นขัดมันกับกระจกยังมีประกาย แต่ไม่กลบนีออน
            RenderSettings.reflectionIntensity = 0.18f;

            var probe = new GameObject("Reflection Probe");
            probe.transform.SetParent(g, false);
            probe.transform.localPosition = new Vector3(0f, 2f, 0f);
            var rp = probe.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Realtime;
            rp.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            rp.size = new Vector3(HX * 2f, CH, HZ * 2f);
            rp.boxProjection = true;
            rp.resolution = 128;
            rp.intensity = 0.4f;   // นีออนเยอะ ปล่อยเต็มความเข้มแล้วห้องกลายเป็นสีขาว
        }

        // ───────────────────────── shell ─────────────────────────
        static void Shell()
        {
            var g = K.Group("Shell", _struct);

            K.Box("Floor", g, new Vector3(0f, -0.2f, 0f), new Vector3(HX * 2f, 0.4f, HZ * 2f), "Floor");
            K.Box("Ceiling", g, new Vector3(0f, CH + 0.25f, 0f), new Vector3(HX * 2f, 0.5f, HZ * 2f), "Ceiling");
            // west wall carries the staff exit, so it is punched rather than solid
            // (yaw 90 maps local +X onto world -Z, hence the sign on the opening centre)
            K.WallWithOpening("Wall_W", g, new Vector3(-HX - 0.2f, 0f, 0f), HZ * 2f, CH, 0.4f, "Wall",
                              -STAFF_DOOR_Z, K.Nav.DoorClear, K.Nav.DoorHeight, 90f);
            K.Box("Wall_E", g, new Vector3(HX + 0.2f, CH * 0.5f, 0f), new Vector3(0.4f, CH, HZ * 2f), "Wall");
            K.Box("Wall_S", g, new Vector3(0f, CH * 0.5f, -HZ - 0.2f), new Vector3(HX * 2f + 0.8f, CH, 0.4f), "Wall");

            // north wall carries the real doorway back to the car park - punched, not painted on
            K.WallWithOpening("Wall_N", g, new Vector3(0f, 0f, HZ + 0.2f), HX * 2f + 0.8f, CH, 0.4f, "Wall",
                              0f, ENTRY_CLEAR_W, ENTRY_CLEAR_H);

            // panelled dado + LED coving around the room
            var p = K.Group("WallPanels", _dress);
            int panels = Mathf.FloorToInt((HZ * 2f - 2f) / 1.9f);
            for (int i = 0; i < panels; i++)
            {
                float z = -HZ + 1f + i * 1.9f;
                if (Mathf.Abs(z - STAFF_DOOR_Z) > K.Nav.DoorClear * 0.5f + 0.8f)
                    K.Box("Panel_W_" + i, p, new Vector3(-HX + 0.12f, 2.0f, z), new Vector3(0.1f, 3.4f, 1.55f), "WallPanel", default(Vector3), false);
                K.Box("Panel_E_" + i, p, new Vector3(HX - 0.12f, 2.0f, z), new Vector3(0.1f, 3.4f, 1.55f), "WallPanel", default(Vector3), false);
            }
            K.NeonStrip("Cove_W", _light, new Vector3(-HX + 0.16f, 3.78f, 0f), new Vector3(0.08f, 0.08f, HZ * 2f - 1f), "NeonMagenta", new Color(1f, 0.08f, 0.6f), 2.2f, 14f);
            K.NeonStrip("Cove_E", _light, new Vector3(HX - 0.16f, 3.78f, 0f), new Vector3(0.08f, 0.08f, HZ * 2f - 1f), "NeonMagenta", new Color(1f, 0.08f, 0.6f), 2.2f, 14f);
            K.NeonStrip("Cove_S", _light, new Vector3(0f, 3.78f, -HZ + 0.16f), new Vector3(HX * 2f - 1f, 0.08f, 0.08f), "NeonCyan", new Color(0.1f, 0.8f, 1f), 2.0f, 14f);
            K.NeonStrip("Cove_N", _light, new Vector3(0f, 3.78f, HZ - 0.16f), new Vector3(HX * 2f - 1f, 0.08f, 0.08f), "NeonCyan", new Color(0.1f, 0.8f, 1f), 2.0f, 14f);

            // upper wall LED wash panels (the pink glow in the reference)
            int washes = Mathf.FloorToInt((HZ * 2f - 5f) / 3.2f);
            for (int i = 0; i < washes; i++)
            {
                float z = -HZ + 2.5f + i * 3.2f;
                K.Box("Wash_W_" + i, p, new Vector3(-HX + 0.1f, 6.0f, z), new Vector3(0.06f, 2.4f, 1.1f), "LedWall", default(Vector3), false);
                K.Box("Wash_E_" + i, p, new Vector3(HX - 0.1f, 6.0f, z), new Vector3(0.06f, 2.4f, 1.1f), "LedWall", default(Vector3), false);
            }
        }

        // ───────────────────────── balcony (2nd floor ring) ─────────────────────────
        static void Balcony()
        {
            var g = K.Group("Balcony", _struct);

            // north run: z 12..16, full width
            float nDepth = HZ - BAL_N_Z;                                    // 4 m
            K.Box("Slab_N", g, new Vector3(0f, BAL - 0.15f, BAL_N_Z + nDepth * 0.5f), new Vector3(HX * 2f, 0.3f, nDepth), "Marble");
            // west return: x -18..-14, stopping short of the north run
            float wWidth = BAL_W_X + HX;                                    // 4 m
            float wZ0 = -VIP_HALF_Z, wZ1 = BAL_N_Z;
            K.Box("Slab_W", g, new Vector3(-HX + wWidth * 0.5f, BAL - 0.15f, (wZ0 + wZ1) * 0.5f),
                  new Vector3(wWidth, 0.3f, wZ1 - wZ0), "Marble");

            // Support columns, kept out of every walking route. In particular there is no column
            // on x = 0: that is the entrance axis, and one used to stand squarely in it.
            float[] cx = { -14.5f, -8.5f, -4.5f, 4.5f, 8.5f, 14.5f };
            for (int i = 0; i < cx.Length; i++) Column(g, "Col_N_" + i, new Vector3(cx[i], 0f, BAL_N_Z));
            for (int i = 0; i < 4; i++) Column(g, "Col_W_" + i, new Vector3(BAL_W_X, 0f, 8.5f - i * 5.8f));

            // railings - brass posts with glass infill
            var rails = K.Group("Balcony_Rails", _circ);
            Railing(rails, "Rail_N", new Vector3(0f, BAL, BAL_N_Z), HX * 2f, 0f);
            Railing(rails, "Rail_W", new Vector3(BAL_W_X, BAL, (wZ0 + wZ1) * 0.5f), wZ1 - wZ0, 90f);

            K.NeonStrip("Edge_N", _light, new Vector3(0f, BAL - 0.33f, BAL_N_Z - 0.02f), new Vector3(HX * 2f, 0.09f, 0.09f), "NeonPurple", new Color(0.55f, 0.12f, 1f), 2.6f, 14f);
            K.NeonStrip("Edge_W", _light, new Vector3(BAL_W_X - 0.02f, BAL - 0.33f, (wZ0 + wZ1) * 0.5f), new Vector3(0.09f, 0.09f, wZ1 - wZ0), "NeonPurple", new Color(0.55f, 0.12f, 1f), 2.6f, 14f);

            // balcony booths along the north wall, backs to the wall
            for (int i = 0; i < 4; i++)
                Booth(_dress, "Booth_Balcony_" + i, new Vector3(-10.5f + i * 7f, BAL, HZ - 1.5f), 180f, 0.75f);

            BalconyStair();
        }

        /// <summary>
        /// Straight flight up the east wall, north of the stage. 24 risers at 175 mm, well under the
        /// controller step offset, and it lands on the balcony slab instead of under it.
        /// </summary>
        static void BalconyStair()
        {
            var st = K.Group("Stair_ToBalcony", _circ);
            const int steps = 24;
            const float tread = 0.28f;
            float rise = BAL / steps;                          // 0.175 m
            float x = HX - 1.9f;                               // 16.1
            float zTop = BAL_N_Z;                              // lands level with the slab edge
            float zFoot = zTop - steps * tread;                // 5.28  (clear of the stage at z 5)

            for (int i = 0; i < steps; i++)
                K.Box("Step_" + i, st, new Vector3(x, (i + 1) * rise - rise * 0.5f, zFoot + i * tread + tread * 0.5f),
                      new Vector3(K.Nav.DoorClear, rise, tread), "Marble");

            K.Box("Stringer", st, new Vector3(x - K.Nav.DoorClear * 0.5f - 0.08f, BAL * 0.5f, (zFoot + zTop) * 0.5f),
                  new Vector3(0.12f, 1.0f, Mathf.Sqrt(Mathf.Pow(zTop - zFoot, 2f) + BAL * BAL)), "Brass",
                  new Vector3(-Mathf.Atan2(BAL, zTop - zFoot) * Mathf.Rad2Deg, 0f, 0f), false);
            K.NeonStrip("Stair_Neon", _light, new Vector3(x - K.Nav.DoorClear * 0.5f - 0.05f, BAL * 0.5f + 0.55f, (zFoot + zTop) * 0.5f),
                        new Vector3(0.06f, 0.06f, Mathf.Sqrt(Mathf.Pow(zTop - zFoot, 2f) + BAL * BAL)), "NeonPink",
                        new Color(1f, 0.3f, 0.66f), 1.8f, 8f);

            K.Marker("NAV_BalconyStairFoot", st, new Vector3(x, 0f, zFoot - 1.2f));
            K.Marker("NAV_BalconyTop", st, new Vector3(x, BAL, zTop + 1.2f));
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
            var g = K.Group("ZONE_DanceFloor", _dress);
            g.localPosition = DF;

            // 150 mm platform: half the step offset, so walking on and off never catches
            K.Cyl("Platform", g, new Vector3(0f, 0.07f, 0f), DF_R * 2f, 0.15f, "Black", default(Vector3), true);
            for (int ring = 0; ring < 4; ring++)
            {
                float r = 1.4f + ring * 1.25f;
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
            var rim = K.Group("Edge_Ring", g);
            for (int i = 0; i < 52; i++)
            {
                float a = i * Mathf.PI * 2f / 52f;
                K.Box("Seg_" + i, rim, new Vector3(Mathf.Cos(a) * (DF_R + 0.04f), 0.11f, Mathf.Sin(a) * (DF_R + 0.04f)),
                      new Vector3(0.1f, 0.07f, 0.72f), "NeonCyan", new Vector3(0f, -a * Mathf.Rad2Deg, 0f), false);
            }

            var lg = K.Group("DanceFloor_Lights", _light);
            lg.localPosition = DF;
            K.AddLight(lg, "Wash_A", new Vector3(0f, 3.2f, 0f), new Vector3(90f, 0f, 0f), LightType.Spot, new Color(1f, 0.15f, 0.62f), 6f, 10f, 100f, true);
            K.AddLight(lg, "Wash_B", new Vector3(-3f, 2.6f, 2f), new Vector3(90f, 0f, 0f), LightType.Point, new Color(0.35f, 0.15f, 1f), 3.2f, 8f);
            K.AddLight(lg, "Wash_C", new Vector3(3f, 2.6f, -2f), new Vector3(90f, 0f, 0f), LightType.Point, new Color(0.1f, 0.75f, 1f), 3.2f, 8f);

            var mb = K.Group("MirrorBall", g);
            K.Box("Drop", mb, new Vector3(0f, 6.4f, 0f), new Vector3(0.04f, 1.6f, 0.04f), "SteelDark", default(Vector3), false);
            K.Sphere("Ball", mb, new Vector3(0f, 5.4f, 0f), 1.1f, "Mirror");
            K.AddLight(mb, "BallLight", new Vector3(0f, 5.4f, 0f), Vector3.zero, LightType.Point, new Color(0.85f, 0.85f, 1f), 2.2f, 9f);

            K.Marker("NAV_DanceFloor", _play, DF);
            K.Marker("INTERACT_DanceFloor", _play, DF + new Vector3(0f, 0.15f, -2.5f));
        }

        // ───────────────────────── bar (south) ─────────────────────────
        static void BarZone()
        {
            var g = K.Group("ZONE_Bar", _dress);
            Vector3 c = BAR_C;                          // arc centre, the bar bulges north
            // customers (rStool) -> counter (rBar) -> back bar (rBack)
            const float rBar = 7.0f, rBack = 5.4f, rStool = 8.4f;
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

            // stools spaced so the player can always walk between two of them
            for (int i = 0; i < 11; i++)
            {
                float a = (-span * 0.46f + span * 0.92f * i / 10f) * Mathf.Deg2Rad + Mathf.PI * 0.5f;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * rStool, 0f, Mathf.Sin(a) * rStool);
                Stool(g, "Stool_" + i, p);
            }

            K.Box("Till", g, new Vector3(0f, 1.3f, c.z + rBar - 0.6f), new Vector3(0.4f, 0.32f, 0.3f), "SteelDark", default(Vector3), false);
            K.Box("Till_Screen", g, new Vector3(0f, 1.34f, c.z + rBar - 0.4f), new Vector3(0.34f, 0.24f, 0.02f), "NeonCyan", default(Vector3), false);

            var bl = K.Group("Bar_Lights", _light);
            K.AddLight(bl, "BarGlow_L", new Vector3(-4.5f, 2.4f, c.z + 8.5f), Vector3.zero, LightType.Point, new Color(1f, 0.2f, 0.6f), 3.4f, 9f);
            K.AddLight(bl, "BarGlow_R", new Vector3(4.5f, 2.4f, c.z + 8.5f), Vector3.zero, LightType.Point, new Color(1f, 0.2f, 0.6f), 3.4f, 9f);
            K.AddLight(bl, "BarGlow_C", new Vector3(0f, 2.4f, c.z + 9.6f), Vector3.zero, LightType.Point, new Color(0.6f, 0.2f, 1f), 3.0f, 9f);

            K.Marker("NAV_Bar", _play, new Vector3(0f, 0f, c.z + rStool + 1.4f));
            K.Marker("INTERACT_Bartender", _play, new Vector3(0f, 0f, c.z + rStool + 1.2f));
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
            var g = K.Group("ZONE_Stage", _dress);
            float dz = STAGE_HALF_Z * 2f;

            K.Box("Deck", _struct, new Vector3(STAGE_CX, STAGE_Y * 0.5f, 0f), new Vector3(6f, STAGE_Y, dz), "Black");
            K.Box("Deck_Top", g, new Vector3(STAGE_CX, STAGE_Y + 0.01f, 0f), new Vector3(6f, 0.03f, dz), "Wood", default(Vector3), false);
            K.NeonStrip("Deck_Edge", _light, new Vector3(STAGE_EDGE + 0.02f, STAGE_Y - 0.12f, 0f), new Vector3(0.08f, 0.1f, dz), "NeonCyan", new Color(0.1f, 0.8f, 1f), 3f, 11f);

            // three shallow steps up, wide enough to walk not vault
            var stp = K.Group("Stage_Steps", _circ);
            for (int i = 0; i < 3; i++)
                K.Box("Step_" + i, stp, new Vector3(STAGE_EDGE - 0.2f - i * 0.4f, STAGE_Y - 0.15f - i * 0.3f, 0f),
                      new Vector3(0.4f, 0.3f, 4.5f), "Black");

            K.Box("Backdrop", _struct, new Vector3(HX - 0.4f, 4.2f, 0f), new Vector3(0.3f, 7.2f, dz + 1.2f), "Black");
            for (int i = 0; i < 6; i++)
                for (int k = 0; k < 3; k++)
                    K.Box("Led_" + i + "_" + k, g, new Vector3(HX - 0.6f, 1.8f + k * 2.1f, -3.8f + i * 1.55f),
                          new Vector3(0.06f, 1.9f, 1.4f), (i + k) % 3 == 0 ? "NeonMagenta" : ((i + k) % 3 == 1 ? "NeonPurple" : "NeonBlue"), default(Vector3), false);

            var dj = K.Group("DJBooth", g);
            dj.localPosition = new Vector3(STAGE_CX, STAGE_Y, 0f);
            K.Box("Body", dj, new Vector3(0f, 0.55f, 0f), new Vector3(2.6f, 1.1f, 1.0f), "Black");
            K.Box("Top", dj, new Vector3(0f, 1.13f, 0f), new Vector3(2.8f, 0.07f, 1.2f), "Marble", default(Vector3), false);
            K.Box("Face_Led", dj, new Vector3(-0.52f, 0.55f, 0f), new Vector3(0.05f, 0.9f, 0.9f), "NeonMagenta", new Vector3(0f, 90f, 0f), false);
            K.Box("Deck_L", dj, new Vector3(0f, 1.2f, -0.55f), new Vector3(0.5f, 0.08f, 0.42f), "SteelDark", default(Vector3), false);
            K.Box("Deck_R", dj, new Vector3(0f, 1.2f, 0.55f), new Vector3(0.5f, 0.08f, 0.42f), "SteelDark", default(Vector3), false);
            K.Box("Mixer", dj, new Vector3(0f, 1.2f, 0f), new Vector3(0.4f, 0.09f, 0.5f), "SteelDark", default(Vector3), false);
            K.Marker("NPC_DJ", dj, new Vector3(0.9f, 0f, 0f));

            // speaker stacks stand off the deck edge so they never block the steps
            for (int i = 0; i < 2; i++)
            {
                float z = i == 0 ? -STAGE_HALF_Z + 0.9f : STAGE_HALF_Z - 0.9f;
                var sp = K.Group("Speakers_" + i, g);
                sp.localPosition = new Vector3(STAGE_EDGE + 1.2f, STAGE_Y, z);
                K.Box("Sub", sp, new Vector3(0f, 0.5f, 0f), new Vector3(1.1f, 1.0f, 1.2f), "Black");
                K.Box("Top", sp, new Vector3(0f, 1.6f, 0f), new Vector3(0.9f, 1.2f, 1.0f), "Black");
                K.Cyl("Driver", sp, new Vector3(-0.47f, 0.5f, 0f), 0.7f, 0.06f, "SteelDark", new Vector3(0f, 0f, 90f));
                K.Cyl("Driver2", sp, new Vector3(-0.47f, 1.6f, 0f), 0.5f, 0.06f, "SteelDark", new Vector3(0f, 0f, 90f));
            }

            var sl = K.Group("Stage_Lights", _light);
            K.AddLight(sl, "StageKey", new Vector3(STAGE_EDGE + 1.5f, 5.4f, 0f), new Vector3(62f, -90f, 0f), LightType.Spot, new Color(1f, 0.25f, 0.7f), 6f, 15f, 70f, true);
            K.AddLight(sl, "StageFill_A", new Vector3(STAGE_CX, 4.6f, -4f), new Vector3(50f, -90f, 0f), LightType.Spot, new Color(0.5f, 0.15f, 1f), 4f, 12f, 65f);
            K.AddLight(sl, "StageFill_B", new Vector3(STAGE_CX, 4.6f, 4f), new Vector3(50f, -90f, 0f), LightType.Spot, new Color(0.1f, 0.7f, 1f), 4f, 12f, 65f);

            K.Marker("NAV_Stage", _play, new Vector3(STAGE_EDGE + 1.6f, 0f, 0f));
        }

        // ───────────────────────── VIP lounge (west) ─────────────────────────
        static void VipZone()
        {
            var g = K.Group("ZONE_VIP", _dress);
            float dz = VIP_HALF_Z * 2f;
            float w = VIP_EDGE - (-HX);                       // 6.8 m

            K.Box("Platform", _struct, new Vector3(VIP_CX, VIP_Y * 0.5f, 0f), new Vector3(w, VIP_Y, dz), "FloorVip");
            K.Box("Platform_Edge", g, new Vector3(VIP_EDGE - 0.06f, VIP_Y * 0.5f, 0f), new Vector3(0.12f, VIP_Y, dz), "Brass", default(Vector3), false);
            K.NeonStrip("Platform_Led", _light, new Vector3(VIP_EDGE + 0.02f, VIP_Y - 0.1f, 0f), new Vector3(0.06f, 0.07f, dz), "NeonPink", new Color(1f, 0.3f, 0.66f), 2.6f, 12f);

            // two step-up points, each a full walking width
            var stp = K.Group("VIP_Steps", _circ);
            for (int s = 0; s < 2; s++)
            {
                float z = s == 0 ? 5f : -5f;
                K.Box("Step_" + s, stp, new Vector3(VIP_EDGE + 0.35f, VIP_Y * 0.5f, z), new Vector3(0.7f, VIP_Y * 0.55f, 2.6f), "Marble");
                Stanchion(g, "Rope_" + s + "_A", new Vector3(VIP_EDGE + 0.8f, 0f, z + 1.9f));
                Stanchion(g, "Rope_" + s + "_B", new Vector3(VIP_EDGE + 0.8f, 0f, z - 1.9f));
            }

            // four booths against the west wall
            for (int i = 0; i < 4; i++)
                Booth(g, "Booth_VIP_" + i, new Vector3(-HX + 1.6f, VIP_Y, -6.3f + i * 4.2f), 90f, 1f);

            for (int i = 0; i < 5; i++)
                K.Box("Curtain_" + i, g, new Vector3(-HX + 0.45f, VIP_Y + 1.7f, -8.4f + i * 4.2f), new Vector3(0.16f, 3.4f, 0.9f), "Velvet", default(Vector3), false);

            // low tables, offset east of the booths so the aisle between them stays clear
            for (int i = 0; i < 4; i++)
            {
                float z = -6.3f + i * 4.2f;
                float tx = VIP_CX + 0.9f;
                K.Cyl("Table_" + i, g, new Vector3(tx, VIP_Y + 0.5f, z), 0.9f, 0.06f, "Marble", default(Vector3), true);
                K.Cyl("TableLeg_" + i, g, new Vector3(tx, VIP_Y + 0.25f, z), 0.12f, 0.5f, "Brass");
                K.Cyl("TableLed_" + i, g, new Vector3(tx, VIP_Y + 0.03f, z), 0.7f, 0.04f, "NeonPurple");
                if (i % 2 == 0)
                {
                    K.Box("IceBucket_" + i, g, new Vector3(tx, VIP_Y + 0.68f, z), new Vector3(0.3f, 0.3f, 0.3f), "Brass", default(Vector3), false);
                    K.Box("Bottle_" + i, g, new Vector3(tx, VIP_Y + 0.95f, z), new Vector3(0.12f, 0.36f, 0.12f), "NeonMagenta", default(Vector3), false);
                }
                for (int k = 0; k < 3; k++)
                    K.Box("Glass_" + i + "_" + k, g, new Vector3(tx + 0.4f + k * 0.22f, VIP_Y + 0.6f, z + 0.3f), new Vector3(0.07f, 0.14f, 0.07f), "Glass", default(Vector3), false);
            }

            K.Box("Niche", g, new Vector3(-HX + 1.1f, VIP_Y + 1.4f, VIP_HALF_Z - 1.2f), new Vector3(1.8f, 2.8f, 2.2f), "Black");
            for (int s = 0; s < 3; s++)
                K.Box("NicheShelf_" + s, g, new Vector3(-HX + 1.6f, VIP_Y + 0.9f + s * 0.55f, VIP_HALF_Z - 1.2f), new Vector3(0.6f, 0.04f, 2.0f), "NeonPurple", default(Vector3), false);

            var vl = K.Group("VIP_Lights", _light);
            K.AddLight(vl, "VipGlow_A", new Vector3(VIP_CX, 2.6f, -5f), Vector3.zero, LightType.Point, new Color(1f, 0.25f, 0.55f), 3.0f, 10f);
            K.AddLight(vl, "VipGlow_B", new Vector3(VIP_CX, 2.6f, 5f), Vector3.zero, LightType.Point, new Color(0.7f, 0.15f, 1f), 3.0f, 10f);

            K.Marker("NAV_VIP", _play, new Vector3(VIP_CX + 1.8f, VIP_Y, 0f));
            K.Marker("TARGET_VipBooth", _play, new Vector3(VIP_CX - 0.6f, VIP_Y, -6.3f));
            K.Marker("EAVESDROP_Position", _play, new Vector3(VIP_EDGE + 1.2f, 0f, -7.4f));
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
        const float ENTRY_CLEAR_W = 3.6f;      // twice the minimum doorway, this is the main entrance
        const float ENTRY_CLEAR_H = 2.8f;

        static void Entrance()
        {
            var g = K.Group("Entrance", _circ);
            float lobbyHalf = 3.4f;

            // vestibule side walls, leaving a 6.8 m lobby between them
            K.Box("Lobby_W", g, new Vector3(-lobbyHalf, 2.1f, HZ - 2.2f), new Vector3(0.3f, 4.2f, 4.4f), "WallPanel");
            K.Box("Lobby_E", g, new Vector3(lobbyHalf, 2.1f, HZ - 2.2f), new Vector3(0.3f, 4.2f, 4.4f), "WallPanel");
            K.Box("Lobby_Head", g, new Vector3(0f, 3.6f, HZ - 2.2f), new Vector3(lobbyHalf * 2f, 1.2f, 4.4f), "WallPanel", default(Vector3), false);

            // real frame around the opening in the north wall - jambs only, the hole stays clear
            K.DoorFrame("DoorFrame_ToParking", g, new Vector3(0f, 0f, HZ), ENTRY_CLEAR_W, ENTRY_CLEAR_H, 0.22f, 0.5f, "Brass");
            // glass leaves hung open against the jambs, never in the way
            K.DoorLeaf("Door_L", g, new Vector3(-ENTRY_CLEAR_W * 0.5f, 0f, HZ - 0.1f), ENTRY_CLEAR_W * 0.5f, ENTRY_CLEAR_H - 0.1f, 0.08f, "Glass", 180f, 100f);
            K.DoorLeaf("Door_R", g, new Vector3(ENTRY_CLEAR_W * 0.5f, 0f, HZ - 0.1f), ENTRY_CLEAR_W * 0.5f, ENTRY_CLEAR_H - 0.1f, 0.08f, "Glass", 0f, -100f);
            K.NeonStrip("Door_Neon", _light, new Vector3(0f, ENTRY_CLEAR_H + 0.5f, HZ - 0.3f), new Vector3(ENTRY_CLEAR_W + 0.8f, 0.09f, 0.09f), "NeonMagenta", new Color(1f, 0.08f, 0.6f), 3.4f, 10f);
            K.Marker("DOOR_ToParkingScene", g, new Vector3(0f, 0f, HZ - 0.4f));

            // beaded curtains tied back either side, the middle stays clear for sight lines
            var bd = K.Group("Beads", _dress);
            for (int s = 0; s < 2; s++)
                for (int i = 0; i < 5; i++)
                {
                    float x = (s == 0 ? -3.0f : 2.1f) + i * 0.22f;
                    K.Box("Bead_" + s + "_" + i, bd, new Vector3(x, 2.0f, HZ - 4.5f), new Vector3(0.025f, 3.0f, 0.025f), "BrassDark", default(Vector3), false);
                }
            K.Box("Pelmet", bd, new Vector3(0f, 3.6f, HZ - 4.5f), new Vector3(lobbyHalf * 2f, 0.4f, 0.16f), "Velvet", default(Vector3), false);

            // coat check, set back so it never narrows the lobby mouth
            var cc = K.Group("CoatCheck", _dress);
            cc.localPosition = new Vector3(-6.2f, 0f, HZ - 1.6f);
            K.Box("Counter", cc, new Vector3(0f, 0.55f, 0f), new Vector3(2.4f, 1.1f, 0.7f), "WallPanel");
            K.Box("Top", cc, new Vector3(0f, 1.13f, 0f), new Vector3(2.5f, 0.07f, 0.8f), "Marble", default(Vector3), false);
            K.Box("Led", cc, new Vector3(0f, 0.3f, -0.37f), new Vector3(2.3f, 0.06f, 0.03f), "NeonCyan", default(Vector3), false);
            K.Sign("Sign", cc, new Vector3(0f, 1.7f, -0.1f), new Vector2(2.0f, 0.28f), "COAT CHECK", new Color(0.95f, 0.8f, 0.95f), 0f);
            K.Marker("NPC_CoatCheck", cc, new Vector3(0f, 0f, 0.6f));

            Stanchion(_dress, "Host_Rope_A", new Vector3(-2.9f, 0f, HZ - 5.6f));
            Stanchion(_dress, "Host_Rope_B", new Vector3(2.9f, 0f, HZ - 5.6f));
            K.AddLight(_light, "LobbyLight", new Vector3(0f, 3.2f, HZ - 2.2f), new Vector3(90f, 0f, 0f), LightType.Spot, new Color(1f, 0.35f, 0.7f), 4f, 9f, 90f);

            K.Marker("NAV_Entrance", _play, new Vector3(0f, 0f, HZ - 6.0f));
        }

        static void BackOfHouse()
        {
            var g = K.Group("BackOfHouse", _circ);

            // rest rooms, north-west corner, with a walkable corridor mouth
            float corrX = -9.6f;
            K.Box("Corridor_Wall", g, new Vector3(corrX, 2.1f, HZ - 2.6f), new Vector3(0.3f, 4.2f, 5.2f), "WallPanel");
            K.DoorLeaf("WC_Door_M", g, new Vector3(corrX - 0.2f, 0f, HZ - 4.4f), 0.9f, 2.1f, 0.08f, "Wood", -90f, 80f);
            K.DoorLeaf("WC_Door_F", g, new Vector3(corrX - 0.2f, 0f, HZ - 1.6f), 0.9f, 2.1f, 0.08f, "Wood", -90f, 80f);
            K.Sign("WC_Sign", _dress, new Vector3(corrX + 0.2f, 2.6f, HZ - 3.0f), new Vector2(1.4f, 0.24f), "REST ROOMS", new Color(0.9f, 0.8f, 0.95f), 90f);

            // staff door - the route deeper into the building for later chapters.
            // It opens into a real recess so the player can walk up to it and stand in it;
            // the locked steel leaf at the back of the recess is what ends the route.
            var sd = K.Group("StaffExit", g);
            float rz = STAFF_DOOR_Z, rd = 2.6f;               // recess depth beyond the wall line
            float rx = -HX - 0.4f - rd * 0.5f;
            float rw = K.Nav.DoorClear + 1.2f;
            K.Box("Recess_Floor", sd, new Vector3(rx, -0.2f, rz), new Vector3(rd + 0.4f, 0.4f, rw), "Floor");
            K.Box("Recess_Ceiling", sd, new Vector3(rx, K.Nav.DoorHeight + 0.5f, rz), new Vector3(rd + 0.4f, 0.4f, rw + 0.6f), "Ceiling");
            K.Box("Recess_N", sd, new Vector3(rx, 1.5f, rz + rw * 0.5f), new Vector3(rd + 0.4f, 3.0f, 0.3f), "Wall");
            K.Box("Recess_S", sd, new Vector3(rx, 1.5f, rz - rw * 0.5f), new Vector3(rd + 0.4f, 3.0f, 0.3f), "Wall");
            K.Box("Recess_Back", sd, new Vector3(rx - rd * 0.5f - 0.15f, 1.5f, rz), new Vector3(0.3f, 3.0f, rw), "Wall");

            K.DoorFrame("Frame", sd, new Vector3(-HX - 0.42f, 0f, rz), K.Nav.DoorClear, K.Nav.DoorHeight, 0.18f, 0.3f, "SteelDark", 90f);
            K.Box("Locked_Leaf", sd, new Vector3(rx - rd * 0.5f + 0.06f, K.Nav.DoorHeight * 0.5f, rz),
                  new Vector3(0.1f, K.Nav.DoorHeight, K.Nav.DoorClear), "SteelDark", default(Vector3), false);
            K.Box("Push_Bar", sd, new Vector3(rx - rd * 0.5f + 0.16f, 1.05f, rz), new Vector3(0.06f, 0.08f, 1.3f), "Brass", default(Vector3), false);
            K.Sign("StaffSign", sd, new Vector3(-HX + 0.4f, 2.6f, rz), new Vector2(1.6f, 0.24f), "STAFF ONLY", new Color(0.95f, 0.3f, 0.35f), 90f);
            K.Box("Exit_Light", sd, new Vector3(-HX + 0.3f, 2.85f, rz), new Vector3(0.06f, 0.24f, 0.66f), "ExitGreen", default(Vector3), false);
            K.AddLight(_light, "StaffExit_Light", new Vector3(rx, 2.3f, rz), Vector3.zero, LightType.Point, new Color(0.3f, 1f, 0.45f), 1.4f, 5f);
            K.Marker("DOOR_StaffOnly", sd, new Vector3(rx, 0f, rz));

            // cocktail tables, kept off every route between two zones
            float[,] tp = { { -8.4f, 9.5f }, { 8.4f, 9.5f }, { -8.6f, -1.5f }, { 8.6f, -1.5f },
                            { -8.0f, -7.5f }, { 8.0f, -7.5f }, { 0f, 11.0f } };
            for (int i = 0; i < tp.GetLength(0); i++)
                CocktailTable(_dress, "Table_" + i, new Vector3(tp[i, 0], 0f, tp[i, 1]));
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
            var g = K.Group("Rigging", _dress);

            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -DF_R - 0.8f : DF_R + 0.8f;
                Truss(g, "Truss_X" + i, new Vector3(x + DF.x, 6.6f, DF.z), 15f, 0f);
            }
            Truss(g, "Truss_Z0", new Vector3(DF.x, 6.9f, DF.z - DF_R - 0.8f), 15f, 90f);
            Truss(g, "Truss_Z1", new Vector3(DF.x, 6.9f, DF.z + DF_R + 0.8f), 15f, 90f);

            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Vector3 p = new Vector3(DF.x + Mathf.Cos(a) * (DF_R + 0.8f), 6.3f, DF.z + Mathf.Sin(a) * (DF_R + 0.8f));
                MovingHead(_light, "Head_" + i, p, (i % 2 == 0) ? "NeonMagenta" : "NeonCyan", i % 3 == 0);
            }

            for (int i = 0; i < 7; i++)
                K.Box("CeilBar_" + i, g, new Vector3(-13.5f + i * 4.5f, CH - 0.15f, 0f), new Vector3(0.12f, 0.1f, HZ * 1.5f),
                      (i % 2 == 0) ? "NeonPurple" : "NeonBlue", default(Vector3), false);

            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var sp = K.Group("FlySpeaker_" + i, g);
                sp.localPosition = new Vector3(DF.x + Mathf.Cos(a) * 7.4f, 5.6f, DF.z + Mathf.Sin(a) * 7.4f);
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
            K.AddLight(g, "Beam", new Vector3(0f, -0.2f, 0f), new Vector3(70f, 0f, 0f), LightType.Spot, c, 5f, 13f, 26f);
        }

        // ───────────────────────── crowd ─────────────────────────
        static void Crowd()
        {
            var g = K.Group("Crowd", _actors);
            // dancers stay inside the platform rim so nobody stands half on the step
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f + 0.3f;
                float r = 1.7f + (i % 3) * 1.2f;
                var npc = K.PlaceHuman(P_NPC, g, new Vector3(DF.x + Mathf.Cos(a) * r, 0.15f, DF.z + Mathf.Sin(a) * r), -a * Mathf.Rad2Deg);
                if (npc != null) npc.name = "NPC_Dancer_" + i.ToString("00");
            }
            // drinkers stand behind the stool ring, not in the walking lane
            for (int i = 0; i < 6; i++)
            {
                float a = (-46f + 92f * i / 5f) * Mathf.Deg2Rad + Mathf.PI * 0.5f;
                Vector3 p = BAR_C + new Vector3(Mathf.Cos(a) * 9.0f, 0f, Mathf.Sin(a) * 9.0f);
                var npc = K.PlaceHuman(P_NPC, g, p, -a * Mathf.Rad2Deg + 180f);
                if (npc != null) npc.name = "NPC_BarPatron_" + i.ToString("00");
            }
            float[,] sp = { { -8.4f, 8.2f }, { 8.4f, 8.2f }, { -8.6f, -3.0f }, { 8.6f, -3.0f }, { 1.2f, 11.0f } };
            for (int i = 0; i < sp.GetLength(0); i++)
            {
                var npc = K.PlaceHuman(P_NPC, g, new Vector3(sp[i, 0], 0f, sp[i, 1]), i * 47f);
                if (npc != null) npc.name = "NPC_Guest_" + i.ToString("00");
            }
            // bartenders stand in the service well, between counter and back bar
            for (int i = 0; i < 2; i++)
            {
                var npc = K.PlaceHuman(P_NPC, g, new Vector3(-2.2f + i * 4.4f, 0f, BAR_C.z + 6.2f), 0f);
                if (npc != null) npc.name = "NPC_Bartender_" + i;
            }
        }

        // ───────────────────────── gameplay ─────────────────────────
        static void Gameplay()
        {
            var g = _play;

            K.Marker("PlayerSpawn_Entrance", g, new Vector3(0f, 0.2f, HZ - 2.5f));
            K.PlacePlayer(P_PLAYER, g, new Vector3(0f, 0.02f, HZ - 2.5f), 180f);
            K.Marker("PlayerSpawn_DanceFloor", g, new Vector3(0f, 0.2f, DF.z + DF_R + 2f));

            // security: three guards patrolling the VIP edge, one on the door
            var gd = K.Group("GUARDS", _actors);
            Guard(gd, "Guard_01_VipNorth", new Vector3(VIP_EDGE + 1.0f, 0f, 7.0f), 90f,
                  new Vector3[] { new Vector3(VIP_EDGE + 1.0f, 0f, 7.0f), new Vector3(VIP_EDGE + 3.5f, 0f, 4.0f) });
            Guard(gd, "Guard_02_VipMid", new Vector3(VIP_EDGE + 1.0f, 0f, 0f), 90f,
                  new Vector3[] { new Vector3(VIP_EDGE + 1.0f, 0f, 0f), new Vector3(VIP_EDGE + 3.5f, 0f, -2.5f) });
            Guard(gd, "Guard_03_VipSouth", new Vector3(VIP_EDGE + 1.0f, 0f, -7.0f), 110f,
                  new Vector3[] { new Vector3(VIP_EDGE + 1.0f, 0f, -7.0f), new Vector3(VIP_EDGE + 3.5f, 0f, -9.0f) });
            Guard(gd, "Guard_04_Door", new Vector3(3.0f, 0f, HZ - 6.0f), 180f,
                  new Vector3[] { new Vector3(3.0f, 0f, HZ - 6.0f) });

            // the two men Kem overhears talking about the SD card
            var tgt = K.Group("TARGETS", _actors);
            var a = K.PlaceHuman(P_NPC, tgt, new Vector3(VIP_CX + 0.4f, VIP_Y, -5.6f), 200f);
            if (a != null) a.name = "NPC_TARGET_Suit_A";
            var b = K.PlaceHuman(P_NPC, tgt, new Vector3(VIP_CX - 0.7f, VIP_Y, -7.0f), 20f);
            if (b != null) b.name = "NPC_TARGET_Suit_B";
            K.Marker("DIALOGUE_SDCardTalk", tgt, new Vector3(VIP_CX, VIP_Y + 1.6f, -6.3f));

            var hide = K.Group("CoverPoints", g);
            K.Marker("Hide_Column_W0", hide, new Vector3(BAL_W_X + 1.0f, 0f, 2.7f));
            K.Marker("Hide_Column_W1", hide, new Vector3(BAL_W_X + 1.0f, 0f, -3.1f));
            K.Marker("Hide_Curtain", hide, new Vector3(VIP_EDGE + 1.5f, 0f, -8.6f));
            K.Marker("Hide_BehindBar", hide, new Vector3(0f, 0f, BAR_C.z + 6.2f));
            K.Marker("Hide_Crowd", hide, new Vector3(DF.x, 0.15f, DF.z - 2f));
            K.Marker("Hide_Speakers", hide, new Vector3(STAGE_EDGE + 1.2f, 0f, -4.1f));
            K.Marker("Hide_CoatCheck", hide, new Vector3(-6.2f, 0f, HZ - 3.0f));

            var obj = K.Group("Objectives", g);
            K.Marker("OBJ_01_EnterClub", obj, new Vector3(0f, 0f, HZ - 6.0f));
            K.Marker("OBJ_02_CrossDanceFloor", obj, DF);
            K.Marker("OBJ_03_ReachVipEdge", obj, new Vector3(VIP_EDGE + 1.2f, 0f, -7.4f));
            K.Marker("OBJ_04_OverhearSuits", obj, new Vector3(VIP_EDGE + 0.6f, 0f, -6.4f));
            K.Marker("OBJ_05_LeaveViaStaffDoor", obj, new Vector3(-HX - 1.7f, 0f, STAFF_DOOR_Z));

            var cam = K.Group("CutsceneCameras", g);
            CamAnchor(cam, "CUT_01_EnterHall", new Vector3(0f, 1.75f, HZ - 5.5f), new Vector3(2f, 180f, 0f));
            CamAnchor(cam, "CUT_02_DanceFloorReveal", new Vector3(7.5f, 2.6f, 10.5f), new Vector3(10f, -145f, 0f));
            CamAnchor(cam, "CUT_03_VipApproach", new Vector3(-7.0f, 1.7f, -4.5f), new Vector3(2f, -70f, 0f));
            CamAnchor(cam, "CUT_04_Eavesdrop", new Vector3(VIP_EDGE + 1.4f, 1.6f, -8.0f), new Vector3(2f, -32f, 0f));
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
