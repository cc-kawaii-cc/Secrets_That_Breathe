using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SecretsThatBreathe.Act2;
using SecretsThatBreathe.Act4;
using K = SecretsThatBreathe.LevelTools.LevelKit;
using C = SecretsThatBreathe.LevelTools.Ch4Kit;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// CHAPTER 4 - ห้องพิจารณาคดี (ACT 4.1 การต่อสู้ในศาล / 4.2 จุดหักมุม) เช้าวันพิพากษา
    ///
    /// ผังห้องพิจารณาคดีแบบศาลไทย มองจากด้านบน (x: ตะวันตก -> ตะวันออก, z: ใต้ -> เหนือ)
    ///
    ///   z 10 +---------------------------------------------------------------+
    ///        |            [ ตราชู ]         บัลลังก์ (ยกพื้น 0.90 m)  [ประตูผู้พิพากษา]|
    ///        |  ปีกซ้าย   [=========== โต๊ะบัลลังก์ + ราวเจาะลาย ===========]  บันได |
    ///  z 3.4 |  (ทีวี)        [===== โต๊ะหน้าบัลลังก์ (เจ้าหน้าที่) =====]           |
    ///        | ทนายโจทก์                  [ คอกพยาน ]               ทนายจำเลย  |
    ///        |  (เข้ม)       ของกลาง                                (ฝั่งแชมป์)  |
    ///  z-1.5 |======= ราวกั้น =======     ประตูคอก      ======= ราวกั้น =======|
    ///        |   ที่นั่งโจทก์ (ป้าสมร)                  ที่นั่งจำเลย (แชมป์)       |
    ///        |   ม้านั่งประชาชน 4 แถว    | ทางเดินกลาง |   ม้านั่งประชาชน 4 แถว    |
    ///  z-10  +-------------------------[ ประตูเข้าห้อง ]------------------------+
    ///       x -7.5                                                         x 7.5
    ///
    /// โจทก์ในคดีนี้คือป้าสมร (ผู้เสียหายฟ้องเอง) เพราะอัยการถูกเคลียร์ให้สั่งไม่ฟ้องไปแล้วใน ACT 3
    /// เข้มจึงนั่งโต๊ะ "ทนายโจทก์" ฝั่งตะวันตก ส่วนทนายของแชมป์นั่งโต๊ะทนายจำเลยฝั่งตะวันออก
    ///
    /// เหนือบัลลังก์ใช้ "ตราชู" แทนพระบรมฉายาลักษณ์ที่มีในห้องพิจารณาคดีจริง เพราะเรื่องนี้ผู้พิพากษา
    /// รับสินบน — ไม่ควรวางฉากผู้พิพากษาทุจริตไว้ใต้พระบรมฉายาลักษณ์ ตราชูยังผูกกับชื่อตอน
    /// "The Shattered Scales" และภาพปิดท้ายของหมออรินทร์ด้วย
    ///
    /// Menu: Tools > Secrets That Breathe > Build Chapter 4 Courtroom
    /// </summary>
    public static class Ch4CourtroomBuilder
    {
        // ── master dimensions (metres) ──
        public const float HX = 7.5f;               // ครึ่งความกว้างห้อง -> 15 m
        public const float Z0 = -10f, Z1 = 10f;     // ยาว 20 m
        public const float H = 4.6f;                // เพดานรอบนอก
        public const float WT = 0.30f;              // ความหนาผนัง

        // ฝ้าหลุม (coffer) กลางห้อง ยกสูงขึ้นและมีไฟซ่อนรอบขอบ แบบภาพอ้างอิงห้องจริง
        const float CX = 4.8f, CZ0 = -7f, CZ1 = 7f, COFFER_Y = 5.2f;

        // ── ฝั่งประชาชน ──
        public const float BAR_Z = -1.5f;           // ราวกั้นระหว่างที่นั่งประชาชนกับพื้นที่ว่าความ
        const float AISLE_HALF = 1.1f;              // ทางเดินกลางกว้าง 2.2 m
        const float BANK_X = 5.8f;                  // ปลายแถวม้านั่ง เหลือทางเดินข้าง 1.7 m
        static readonly float[] ROW_Z = { -3.6f, -4.8f, -6.0f, -7.2f };
        const float PARTY_Z = -2.35f;               // ที่นั่งโจทก์/จำเลย ชิดหลังราวกั้น

        // ── พื้นที่ว่าความ ──
        const float TABLE_X = 4.6f;                 // โต๊ะทนายสองฝั่ง (|x|)
        const float TABLE_Z = 1.6f;
        const float WIT_Z = 1.2f;                   // คอกพยาน
        const float CLERK_Z0 = 3.05f, CLERK_Z1 = 3.75f, CLERK_HX = 3.0f;

        // ── บัลลังก์ ──
        public const float BENCH_Z0 = 5.2f, BENCH_Z1 = 5.9f;
        const float BENCH_HX = 5.2f;
        public const float PLAT_Y = 0.90f;          // ยกพื้น 5 ลูก x 0.18 m (ต่ำกว่า stepOffset)
        const float BENCH_TOP = 1.68f;
        const float JUDGE_Z = 6.85f;
        static readonly float[] JUDGE_X = { -1.8f, 0f, 1.8f };

        // บันไดขึ้นบัลลังก์ชิดผนังตะวันออก กว้างเท่า DoorClear
        const float STAIR_X0 = 5.6f, STAIR_X1 = 7.4f, STAIR_Z0 = 4.4f;
        const int STAIR_STEPS = 5;
        const float STAIR_TREAD = 0.30f;
        const float JUDGE_DOOR_X = 6.4f;

        public const string ScenePath = C.CourtroomPath;

        // ตำแหน่งสำคัญที่ใช้ทั้งฉากและกล้อง
        static readonly Vector3 KHEM_SPOT = new Vector3(-TABLE_X - 0.75f, 0f, TABLE_Z);
        static readonly Vector3 TV_POS = new Vector3(-3.9f, 0f, 4.25f);
        static readonly Vector3 GAVEL_POS = new Vector3(0.55f, BENCH_TOP + 0.05f, 5.95f);

        static Transform _root;
        static Transform _env, _struct, _circ, _dress, _light, _actors, _play;

        [MenuItem("Tools/Secrets That Breathe/Build Chapter 4 Courtroom", false, 15)]
        public static void BuildScene() { BuildScene(true); }

        /// <summary>สร้างทั้งสองซีนของ ACT 4 ต่อกัน (ห้องพิจารณาคดี -> หน้าศาลตอนฝนตก)</summary>
        [MenuItem("Tools/Secrets That Breathe/Build ALL Chapter 4 Levels", false, 17)]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Ch4] leave play mode first."); return; }
            bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(C.CourtroomPath) != null ||
                          AssetDatabase.LoadAssetAtPath<SceneAsset>(C.CourtStepsPath) != null;
            if (exists && !EditorUtility.DisplayDialog("สร้างซีน ACT 4 ใหม่ทับทั้งสองซีน?",
                    "ของที่วางหรือขยับเองในซีนห้องพิจารณาคดีและหน้าศาลจะหายทั้งหมด", "สร้างใหม่ทับ", "ยกเลิก")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            BuildScene(false);
            Ch4CourtStepsBuilder.BuildScene(false);
        }

        public static void BuildScene(bool askToSave)
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Ch4Courtroom] leave play mode first."); return; }

            if (askToSave && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog(
                    "สร้างซีนห้องพิจารณาคดีใหม่ทับ?",
                    C.SceneCourtroom + ".unity มีอยู่แล้ว การ build จะสร้างซีนใหม่ทับทั้งหมด\n" +
                    "ของที่วางหรือขยับเองในซีนจะหายทั้งหมด",
                    "สร้างใหม่ทับ", "ยกเลิก")) return;
            if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            K.EnsureFolder(C.MatFolder);
            K.ResetPlaced();
            C.Materials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _root = new GameObject("=== CH4 COURTROOM ===").transform;
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
            Walls();
            Ceiling();
            Platform();
            Bench();
            BackWall();
            ClerkDesk();
            WitnessStand();
            CounselTables();
            BarRail();
            Seating();
            EvidenceProps();
            RoomDressing();
            Lighting();
            Actors();
            Gameplay();

            GroundProps();

            C.EnsureInBuildSettings(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Ch4Courtroom] built -> " + ScenePath);
            // ผิวที่สูงเกินยกพื้นบัลลังก์คือหลังม้านั่ง/โต๊ะ ไม่ใช่ทางเดิน ตัดออกจากการตรวจ
            Debug.Log(C.Audit(_root, "CH4 COURTROOM", PLAT_Y + 0.05f));
        }

        static void GroundProps()
        {
            Physics.SyncTransforms();
            var placed = K.Placed;
            for (int i = 0; i < placed.Count; i++) K.SnapDown(placed[i]);
        }

        // ───────────────────────── atmosphere ─────────────────────────
        static void Atmosphere()
        {
            var g = K.Group("Atmosphere", _env);

            // ห้องปิดทึบ แสงมาจากไฟเพดานกับหน้าต่างฝ้าเป็นหลัก — ambient นวล ๆ ไม่ให้มุมห้องดำ
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.56f, 0.57f);
            RenderSettings.ambientEquatorColor = new Color(0.44f, 0.41f, 0.37f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.17f, 0.15f);
            RenderSettings.fog = false;
            RenderSettings.reflectionIntensity = 0.6f;

            var probe = new GameObject("Reflection Probe");
            probe.transform.SetParent(g, false);
            probe.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            var rp = probe.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Realtime;
            rp.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            rp.size = new Vector3(HX * 2f + 0.5f, COFFER_Y + 0.5f, Z1 - Z0 + 0.5f);
            rp.boxProjection = true;
            rp.resolution = 128;
            rp.intensity = 0.8f;
        }

        // ───────────────────────── เปลือกห้อง ─────────────────────────
        static void Shell()
        {
            var g = K.Group("Shell", _struct);
            C.Slab(g, "Floor", -HX - WT, HX + WT, Z0 - WT, Z1 + WT, 0f, 0.30f, "Vinyl");

            // ผนังสี่ด้านอยู่นอกขอบเขตภายในห้อง สูงเลยฝ้าหลุมขึ้นไป กันเห็นรอยต่อ
            float wallH = COFFER_Y + 0.4f;
            C.Panel(g, "Wall_W", -HX - WT, -HX, Z0 - WT, Z1 + WT, 0f, wallH, "Plaster");
            C.Panel(g, "Wall_E", HX, HX + WT, Z0 - WT, Z1 + WT, 0f, wallH, "Plaster");
            C.Panel(g, "Wall_S", -HX, HX, Z0 - WT, Z0, 0f, wallH, "Plaster");
            C.Panel(g, "Wall_N", -HX, HX, Z1, Z1 + WT, 0f, wallH, "Plaster");

            // แถบพื้นสีเข้มบอกเขตพื้นที่ว่าความ (หน้าราวกั้นถึงบัลลังก์)
            C.Slab(_dress, "Floor_WellBorder", -HX + 0.3f, HX - 0.3f, BAR_Z + 0.25f, BAR_Z + 0.45f, 0.006f, 0.012f, "VinylDark", false);
            C.Slab(_dress, "Floor_AisleRunner", -0.8f, 0.8f, Z0 + 0.2f, BAR_Z - 0.3f, 0.006f, 0.012f, "VinylDark", false);
        }

        // ───────────────────────── ผนัง + ไม้บุผนัง ─────────────────────────
        static void Walls()
        {
            var g = K.Group("Wainscot", _dress);

            // ไม้บุผนังสูง 1.2 m รอบห้อง ทำตามภาพอ้างอิงห้องจริง
            C.Panel(g, "Wainscot_W", -HX, -HX + 0.04f, Z0, BENCH_Z1, 0f, 1.2f, "Wood", false);
            C.Panel(g, "Wainscot_E", HX - 0.04f, HX, Z0, STAIR_Z0, 0f, 1.2f, "Wood", false);
            C.Panel(g, "Wainscot_S", -HX, HX, Z0, Z0 + 0.04f, 0f, 1.2f, "Wood", false);
            C.Panel(g, "Cap_W", -HX, -HX + 0.07f, Z0, BENCH_Z1, 1.2f, 0.06f, "WoodDark", false);
            C.Panel(g, "Cap_E", HX - 0.07f, HX, Z0, STAIR_Z0, 1.2f, 0.06f, "WoodDark", false);
            C.Panel(g, "Cap_S", -HX, HX, Z0, Z0 + 0.07f, 1.2f, 0.06f, "WoodDark", false);
            C.Panel(g, "Skirting_W", -HX, -HX + 0.06f, Z0, BENCH_Z1, 0f, 0.12f, "WoodDark", false);
            C.Panel(g, "Skirting_E", HX - 0.06f, HX, Z0, STAIR_Z0, 0f, 0.12f, "WoodDark", false);
            C.Panel(g, "Skirting_S", -HX, HX, Z0, Z0 + 0.06f, 0f, 0.12f, "WoodDark", false);

            // ลูกฟักบนไม้บุผนัง เว้นช่วงประตู
            for (float z = Z0 + 0.8f; z < BENCH_Z0 - 0.4f; z += 1.2f)
            {
                if (Mathf.Abs(z - 4.2f) < 0.9f) continue;                    // ประตูห้องเจ้าหน้าที่
                C.RaisedPanel(g, "Panel_W_" + z.ToString("0.0"), new Vector3(-HX + 0.04f, 0.66f, z), 1.0f, 0.8f, 90f);
                if (z < STAIR_Z0 - 0.5f)
                    C.RaisedPanel(g, "Panel_E_" + z.ToString("0.0"), new Vector3(HX - 0.04f, 0.66f, z), 1.0f, 0.8f, -90f);
            }
            for (float x = -HX + 0.8f; x < HX - 0.4f; x += 1.2f)
            {
                if (Mathf.Abs(x) < 1.5f) continue;                            // ประตูเข้าห้อง
                C.RaisedPanel(g, "Panel_S_" + x.ToString("0.0"), new Vector3(x, 0.66f, Z0 + 0.04f), 1.0f, 0.8f, 0f);
            }

            // ไม้บุผนังส่วนบนบัลลังก์ (ข้างผู้พิพากษา)
            C.Panel(g, "Wainscot_W_Plat", -HX, -HX + 0.04f, BENCH_Z1, Z1, PLAT_Y, 1.3f, "Wood", false);
            C.Panel(g, "Wainscot_E_Plat", HX - 0.04f, HX, STAIR_Z0, Z1, 0f, PLAT_Y + 1.3f, "Wood", false);

            // หน้าต่างฝ้าสูงฝั่งตะวันออก แดดเช้าส่องเข้ามา
            var win = K.Group("Windows_E", _dress);
            float[] wz = { -7.5f, -4.5f, -1.5f, 1.5f };
            for (int i = 0; i < wz.Length; i++)
            {
                float z = wz[i];
                C.Panel(win, "Glass_" + i, HX - 0.03f, HX - 0.01f, z - 0.7f, z + 0.7f, 2.7f, 1.3f, "WindowMorning", false);
                C.Panel(win, "Frame_Head_" + i, HX - 0.06f, HX, z - 0.75f, z + 0.75f, 3.98f, 0.06f, "Alu", false);
                C.Panel(win, "Frame_Sill_" + i, HX - 0.10f, HX, z - 0.75f, z + 0.75f, 2.64f, 0.06f, "Alu", false);
                C.Panel(win, "Frame_Mid_" + i, HX - 0.06f, HX, z - 0.03f, z + 0.03f, 2.7f, 1.3f, "Alu", false);
                C.Panel(win, "Frame_L_" + i, HX - 0.06f, HX, z - 0.75f, z - 0.69f, 2.7f, 1.3f, "Alu", false);
                C.Panel(win, "Frame_R_" + i, HX - 0.06f, HX, z + 0.69f, z + 0.75f, 2.7f, 1.3f, "Alu", false);
            }
        }

        // ───────────────────────── ฝ้าเพดาน ─────────────────────────
        static void Ceiling()
        {
            var g = K.Group("Ceiling", _struct);
            const float T = 0.2f;
            C.Slab(g, "Ceil_W", -HX, -CX, Z0, Z1, H + T, T, "Ceiling", false);
            C.Slab(g, "Ceil_E", CX, HX, Z0, Z1, H + T, T, "Ceiling", false);
            C.Slab(g, "Ceil_S", -CX, CX, Z0, CZ0, H + T, T, "Ceiling", false);
            C.Slab(g, "Ceil_N", -CX, CX, CZ1, Z1, H + T, T, "Ceiling", false);
            C.Slab(g, "Coffer_Top", -CX, CX, CZ0, CZ1, COFFER_Y + T, T, "Ceiling", false);

            // ขอบหลุมฝ้า
            float dh = COFFER_Y - H;
            C.Panel(g, "Coffer_Side_W", -CX - 0.05f, -CX, CZ0, CZ1, H, dh, "Ceiling", false);
            C.Panel(g, "Coffer_Side_E", CX, CX + 0.05f, CZ0, CZ1, H, dh, "Ceiling", false);
            C.Panel(g, "Coffer_Side_S", -CX, CX, CZ0 - 0.05f, CZ0, H, dh, "Ceiling", false);
            C.Panel(g, "Coffer_Side_N", -CX, CX, CZ1, CZ1 + 0.05f, H, dh, "Ceiling", false);

            // คิ้วไม้รอบหลุมฝ้า
            C.Panel(_dress, "Coffer_Trim_W", -CX - 0.12f, -CX + 0.02f, CZ0 - 0.12f, CZ1 + 0.12f, H - 0.08f, 0.08f, "WoodDark", false);
            C.Panel(_dress, "Coffer_Trim_E", CX - 0.02f, CX + 0.12f, CZ0 - 0.12f, CZ1 + 0.12f, H - 0.08f, 0.08f, "WoodDark", false);
            C.Panel(_dress, "Coffer_Trim_S", -CX - 0.12f, CX + 0.12f, CZ0 - 0.12f, CZ0 + 0.02f, H - 0.08f, 0.08f, "WoodDark", false);
            C.Panel(_dress, "Coffer_Trim_N", -CX - 0.12f, CX + 0.12f, CZ1 - 0.02f, CZ1 + 0.12f, H - 0.08f, 0.08f, "WoodDark", false);
        }

        // ───────────────────────── บัลลังก์: ยกพื้น + บันได ─────────────────────────
        static void Platform()
        {
            var g = K.Group("Platform", _struct);

            // ยกพื้นเต็มความกว้างห้อง ฝั่งตะวันตกหลังปีกซ้ายจึงไม่เป็นซอกที่ไปไม่ถึง
            C.Slab(g, "Platform", -HX, HX, BENCH_Z1, Z1, PLAT_Y, PLAT_Y, "Wood");
            C.Slab(_dress, "Platform_Carpet", -HX + 0.05f, HX - 0.05f, BENCH_Z1 + 0.02f, Z1 - 0.08f,
                   PLAT_Y + 0.012f, 0.024f, "Carpet", false);

            // บันไดขึ้นบัลลังก์ ลูกตั้งซ้อนเป็นก้อนตัน กันตกร่อง
            var st = K.Group("Stair_Bench", _circ);
            float rise = PLAT_Y / STAIR_STEPS;
            for (int i = 0; i < STAIR_STEPS; i++)
            {
                float z0 = STAIR_Z0 + i * STAIR_TREAD;
                C.Slab(st, "Step_" + i, STAIR_X0, STAIR_X1, z0, z0 + STAIR_TREAD, (i + 1) * rise, (i + 1) * rise, "WoodDark");
                C.Slab(_dress, "Step_Carpet_" + i, STAIR_X0 + 0.1f, STAIR_X1 - 0.1f, z0 + 0.02f, z0 + STAIR_TREAD,
                       (i + 1) * rise + 0.01f, 0.02f, "Carpet", false);
            }
            // ราวจับชิดผนัง
            float run = STAIR_STEPS * STAIR_TREAD;
            C.Rod(st, "Handrail", new Vector3(HX - 0.08f, 0.95f, STAIR_Z0), new Vector3(HX - 0.08f, PLAT_Y + 0.95f, STAIR_Z0 + run), 0.05f, "Gold");
            C.Rod(st, "Handrail_Post_Foot", new Vector3(HX - 0.08f, 0f, STAIR_Z0), new Vector3(HX - 0.08f, 0.95f, STAIR_Z0), 0.04f, "Gold");
            C.Rod(st, "Handrail_Post_Head", new Vector3(HX - 0.08f, PLAT_Y, STAIR_Z0 + run), new Vector3(HX - 0.08f, PLAT_Y + 0.95f, STAIR_Z0 + run), 0.04f, "Gold");

            // แผงกั้นข้างบันได (ปลายโต๊ะบัลลังก์ฝั่งตะวันออก) พร้อมราวเจาะลาย
            C.Panel(g, "StairScreen", BENCH_HX, STAIR_X0, STAIR_Z0, BENCH_Z1, 0f, 1.45f, "Wood");
            C.RaisedPanel(_dress, "StairScreen_Panel", new Vector3(BENCH_HX, 0.7f, (STAIR_Z0 + BENCH_Z0) * 0.5f), 0.6f, 0.8f, -90f);
            C.PiercedRail(_dress, "StairScreen_Rail", new Vector3((BENCH_HX + STAIR_X0) * 0.5f, 1.45f, (STAIR_Z0 + BENCH_Z1) * 0.5f),
                          BENCH_Z1 - STAIR_Z0, 0.26f, 90f, 0.3f);

            // ปีกซ้าย: ตู้ไม้เตี้ยกว่าบัลลังก์หนึ่งระดับ แบบภาพอ้างอิง
            C.Panel(g, "Wing_W", -HX, -BENCH_HX, BENCH_Z0 + 0.1f, BENCH_Z1, 0f, 1.45f, "Wood");
            C.Panel(_dress, "Wing_W_Plinth", -HX, -BENCH_HX, BENCH_Z0 + 0.04f, BENCH_Z0 + 0.1f, 0f, 0.16f, "WoodDark", false);
            C.RaisedPanel(_dress, "Wing_W_Panel_A", new Vector3(-6.95f, 0.72f, BENCH_Z0 + 0.1f), 0.8f, 0.9f, 180f);
            C.RaisedPanel(_dress, "Wing_W_Panel_B", new Vector3(-5.85f, 0.72f, BENCH_Z0 + 0.1f), 0.8f, 0.9f, 180f);
            C.Flutes(_dress, "Wing_W_Flutes", new Vector3(-(HX + BENCH_HX) * 0.5f, 1.3f, BENCH_Z0 + 0.1f), HX - BENCH_HX - 0.1f, 0.22f, 180f);
            C.PiercedRail(_dress, "Wing_W_Rail", new Vector3(-(HX + BENCH_HX) * 0.5f, 1.45f, BENCH_Z0 + 0.14f),
                          HX - BENCH_HX, 0.26f, 0f);

            // ประตูผู้พิพากษา (ปิดตาย เข้าได้จากหลังฉากเท่านั้น)
            var dr = K.Group("JudgesDoor", _circ);
            float dz = Z1 - 0.06f;
            C.Panel(dr, "Leaf", JUDGE_DOOR_X - 0.5f, JUDGE_DOOR_X + 0.5f, dz - 0.05f, dz, PLAT_Y, 2.25f, "WoodLight", false);
            C.Panel(dr, "Jamb_L", JUDGE_DOOR_X - 0.62f, JUDGE_DOOR_X - 0.5f, dz - 0.09f, dz, PLAT_Y, 2.35f, "WoodDark", false);
            C.Panel(dr, "Jamb_R", JUDGE_DOOR_X + 0.5f, JUDGE_DOOR_X + 0.62f, dz - 0.09f, dz, PLAT_Y, 2.35f, "WoodDark", false);
            C.Panel(dr, "Head", JUDGE_DOOR_X - 0.62f, JUDGE_DOOR_X + 0.62f, dz - 0.09f, dz, PLAT_Y + 2.25f, 0.12f, "WoodDark", false);
            C.RaisedPanel(dr, "Leaf_Panel_Top", new Vector3(JUDGE_DOOR_X, PLAT_Y + 1.6f, dz - 0.05f), 0.8f, 0.9f, 180f);
            C.RaisedPanel(dr, "Leaf_Panel_Bot", new Vector3(JUDGE_DOOR_X, PLAT_Y + 0.55f, dz - 0.05f), 0.8f, 0.8f, 180f);
            K.Box("Handle", dr, new Vector3(JUDGE_DOOR_X - 0.38f, PLAT_Y + 1.05f, dz - 0.1f), new Vector3(0.03f, 0.18f, 0.04f), "Gold", default(Vector3), false);
            K.Marker("SHUT_JudgesDoor", dr, new Vector3(JUDGE_DOOR_X, PLAT_Y, dz - 0.6f));
        }

        // ───────────────────────── โต๊ะบัลลังก์ ─────────────────────────
        static void Bench()
        {
            var g = K.Group("Bench", _struct);
            var d = K.Group("Bench_Carving", _dress);

            // ตัวโต๊ะบัลลังก์ตันทั้งก้อน หน้าโต๊ะหันลงห้อง (ทิศใต้)
            C.Panel(g, "Bench_Body", -BENCH_HX, BENCH_HX, BENCH_Z0, BENCH_Z1, 0f, BENCH_TOP, "Wood");
            C.Panel(d, "Plinth", -BENCH_HX - 0.04f, BENCH_HX + 0.04f, BENCH_Z0 - 0.06f, BENCH_Z0, 0f, 0.16f, "WoodDark", false);

            // สามช่วงคั่นด้วยเสาอิง แต่ละช่วงมีลูกฟักสองบาน + แถบเซาะร่องด้านบน (ตามภาพอ้างอิง)
            float[] px = { -BENCH_HX + 0.18f, -1.73f, 1.73f, BENCH_HX - 0.18f };
            for (int i = 0; i < px.Length; i++)
            {
                C.Panel(d, "Pilaster_" + i, px[i] - 0.18f, px[i] + 0.18f, BENCH_Z0 - 0.07f, BENCH_Z0, 0.16f, 1.29f, "WoodLight", false);
                C.Panel(d, "Pilaster_Cap_" + i, px[i] - 0.22f, px[i] + 0.22f, BENCH_Z0 - 0.10f, BENCH_Z0, 1.35f, 0.08f, "WoodDark", false);
            }
            for (int b = 0; b < 3; b++)
            {
                float x0 = px[b] + 0.18f, x1 = px[b + 1] - 0.18f;
                float bw = x1 - x0, bc = (x0 + x1) * 0.5f;
                C.RaisedPanel(d, "Bay" + b + "_Panel_L", new Vector3(bc - bw * 0.25f, 0.68f, BENCH_Z0), bw * 0.5f - 0.16f, 0.78f, 180f);
                C.RaisedPanel(d, "Bay" + b + "_Panel_R", new Vector3(bc + bw * 0.25f, 0.68f, BENCH_Z0), bw * 0.5f - 0.16f, 0.78f, 180f);
                C.Flutes(d, "Bay" + b + "_Flutes", new Vector3(bc, 1.2f, BENCH_Z0), bw - 0.06f, 0.26f, 180f);
            }
            C.Panel(d, "Cornice", -BENCH_HX - 0.08f, BENCH_HX + 0.08f, BENCH_Z0 - 0.12f, BENCH_Z0, 1.43f, BENCH_TOP - 1.43f, "Wood", false);
            C.Panel(d, "Desk_Top", -BENCH_HX - 0.1f, BENCH_HX + 0.1f, BENCH_Z0 - 0.15f, BENCH_Z1 + 0.28f, BENCH_TOP, 0.05f, "WoodDark", false);
            C.PiercedRail(d, "Rail", new Vector3(0f, BENCH_TOP + 0.05f, BENCH_Z0 - 0.05f), BENCH_HX * 2f, 0.30f, 0f);

            // เก้าอี้องค์คณะสามท่าน
            for (int i = 0; i < JUDGE_X.Length; i++)
                C.JudgeChair(g, "JudgeChair_" + i, new Vector3(JUDGE_X[i], PLAT_Y, JUDGE_Z), 180f);

            // ของบนโต๊ะ: แฟ้มสำนวน ไมค์ประจำที่นั่ง ค้อน
            var desk = K.Group("Bench_Desk", _dress);
            float deskY = BENCH_TOP + 0.05f;
            for (int i = 0; i < JUDGE_X.Length; i++)
            {
                C.FileStack(desk, "Files_" + i, new Vector3(JUDGE_X[i] - 0.42f, deskY, 5.98f), 8f, 3 + i, "FolderGreen");
                C.Mic(desk, "Mic_Judge_" + i, new Vector3(JUDGE_X[i] + 0.3f, deskY, 5.72f), 180f);
            }
            Gavel(desk, GAVEL_POS);
        }

        /// <summary>
        /// ค้อนตามบท "เคาะค้อน ยกฟ้อง!"
        /// ศาลไทยจริงไม่ใช้ค้อน — วางไว้ให้ตามสคริปต์ ถ้าอยากให้สมจริงลบทิ้งแล้วให้ผู้พิพากษาอ่านคำพิพากษาแทน
        /// </summary>
        static void Gavel(Transform parent, Vector3 pos)
        {
            var g = K.Group("Gavel", parent);
            g.localPosition = pos;
            g.localEulerAngles = new Vector3(0f, -25f, 0f);
            K.Cyl("Block", g, new Vector3(0f, 0.02f, 0f), 0.16f, 0.04f, "WoodDark");
            K.Cyl("Head", g, new Vector3(0.14f, 0.075f, 0f), 0.07f, 0.17f, "WoodLight", new Vector3(90f, 0f, 0f));
            K.Cyl("Band", g, new Vector3(0.14f, 0.075f, 0f), 0.074f, 0.03f, "Gold", new Vector3(90f, 0f, 0f));
            C.Rod(g, "Handle", new Vector3(0.14f, 0.075f, 0f), new Vector3(0.40f, 0.06f, 0f), 0.024f, "WoodLight");
            K.Marker("MARK_Gavel", g, new Vector3(0.14f, 0.075f, 0f));
        }

        // ───────────────────────── ผนังหลังบัลลังก์ ─────────────────────────
        static void BackWall()
        {
            var g = K.Group("BackWall", _dress);
            float zf = Z1 - 0.05f;
            C.Panel(g, "Wood", -HX, HX, zf, Z1, PLAT_Y, H - PLAT_Y, "Wood", false);
            C.Panel(g, "Cornice", -HX, HX, Z1 - 0.2f, Z1, H - 0.25f, 0.25f, "WoodDark", false);
            C.Panel(g, "Base", -HX, HX, Z1 - 0.1f, Z1, PLAT_Y, 0.15f, "WoodDark", false);

            // เสาอิงแบ่งผนังเป็นช่อง ช่องกลางกว้างที่สุดสำหรับตราชู
            float[] px = { -5.3f, -2.7f, 2.7f, 5.3f };
            for (int i = 0; i < px.Length; i++)
            {
                C.Panel(g, "Pilaster_" + i, px[i] - 0.2f, px[i] + 0.2f, zf - 0.09f, zf, PLAT_Y + 0.15f, H - PLAT_Y - 0.4f, "WoodLight", false);
                C.Panel(g, "Pilaster_Cap_" + i, px[i] - 0.26f, px[i] + 0.26f, zf - 0.13f, zf, H - 0.42f, 0.14f, "WoodDark", false);
                C.Flutes(g, "Pilaster_Flutes_" + i, new Vector3(px[i], PLAT_Y + 1.9f, zf - 0.09f), 0.3f, 2.4f, 180f, 0.06f);
            }
            // ช่องข้าง: ลูกฟักสูงสองชั้น (ช่องขวาสุดเป็นประตูผู้พิพากษา)
            float[,] bays = { { -HX + 0.1f, -5.5f }, { -5.1f, -2.9f }, { 2.9f, 5.1f } };
            for (int b = 0; b < bays.GetLength(0); b++)
            {
                float c = (bays[b, 0] + bays[b, 1]) * 0.5f, w = bays[b, 1] - bays[b, 0] - 0.3f;
                C.RaisedPanel(g, "Bay" + b + "_Low", new Vector3(c, PLAT_Y + 1.0f, zf), w, 1.3f, 180f);
                C.RaisedPanel(g, "Bay" + b + "_High", new Vector3(c, PLAT_Y + 2.6f, zf), w, 1.5f, 180f);
            }

            // ตราชูทองบนพื้นวงกลมแดง — จุดเด่นของห้อง (แทนพระบรมฉายาลักษณ์ ดูหมายเหตุหัวไฟล์)
            var em = K.Group("Emblem_Scales", g);
            em.localPosition = new Vector3(0f, 3.45f, zf);
            K.Cyl("Ring", em, new Vector3(0f, 0f, -0.02f), 1.56f, 0.04f, "Gold", new Vector3(90f, 0f, 0f));
            K.Cyl("Field", em, new Vector3(0f, 0f, -0.05f), 1.42f, 0.04f, "Crimson", new Vector3(90f, 0f, 0f));
            C.Scales(em, "Scales", new Vector3(0f, 0.02f, -0.14f), 1.05f, 180f);
            // แพรประดับสองข้างตรา
            C.Panel(g, "Drape_L", -1.55f, -1.05f, zf - 0.06f, zf - 0.02f, 2.0f, 2.4f, "Crimson", false);
            C.Panel(g, "Drape_R", 1.05f, 1.55f, zf - 0.06f, zf - 0.02f, 2.0f, 2.4f, "Crimson", false);
            C.Panel(g, "Drape_Pelmet", -1.7f, 1.7f, zf - 0.1f, zf - 0.02f, 4.25f, 0.18f, "Gold", false);
        }

        // ───────────────────────── โต๊ะหน้าบัลลังก์ ─────────────────────────
        static void ClerkDesk()
        {
            var g = K.Group("ClerkDesk", _struct);
            var d = K.Group("ClerkDesk_Carving", _dress);

            // หน้าโต๊ะสูง 1.05 m บังเจ้าหน้าที่ที่นั่งอยู่หลังโต๊ะ ส่วนพื้นทำงานข้างในต่ำลงมาเท่าโต๊ะปกติ
            C.Panel(g, "Front", -CLERK_HX, CLERK_HX, CLERK_Z0, CLERK_Z0 + 0.12f, 0f, 1.05f, "Wood");
            C.Panel(g, "Side_W", -CLERK_HX, -CLERK_HX + 0.08f, CLERK_Z0, CLERK_Z1, 0f, 1.05f, "Wood");
            C.Panel(g, "Side_E", CLERK_HX - 0.08f, CLERK_HX, CLERK_Z0, CLERK_Z1, 0f, 1.05f, "Wood");
            // พื้นทำงานไม่มี collider — หน้าโต๊ะกับข้างโต๊ะกันเดินอยู่แล้ว ถ้ามีจะกลายเป็นชั้นให้ผู้เล่นกระโดดขึ้นไปยืน
            C.Panel(g, "Worktop", -CLERK_HX + 0.08f, CLERK_HX - 0.08f, CLERK_Z0 + 0.12f, CLERK_Z1, 0.72f, 0.04f, "WoodDark", false);

            C.Panel(d, "Plinth", -CLERK_HX - 0.03f, CLERK_HX + 0.03f, CLERK_Z0 - 0.05f, CLERK_Z0, 0f, 0.14f, "WoodDark", false);
            for (int i = 0; i < 4; i++)
            {
                float x = -CLERK_HX + 0.75f + i * 1.5f;
                C.RaisedPanel(d, "Panel_" + i, new Vector3(x, 0.5f, CLERK_Z0), 1.2f, 0.62f, 180f);
            }
            C.Flutes(d, "Flutes", new Vector3(0f, 0.93f, CLERK_Z0), CLERK_HX * 2f - 0.1f, 0.18f, 180f);
            C.Panel(d, "Top", -CLERK_HX - 0.05f, CLERK_HX + 0.05f, CLERK_Z0 - 0.06f, CLERK_Z0 + 0.2f, 1.05f, 0.04f, "WoodDark", false);
            C.PiercedRail(d, "Rail", new Vector3(0f, 1.09f, CLERK_Z0 + 0.07f), CLERK_HX * 2f, 0.24f, 0f);

            // ระบบบันทึกคำเบิกความ: จอหันหาเจ้าหน้าที่ (ทิศเหนือ)
            var items = K.Group("ClerkDesk_Items", _dress);
            float wy = 0.74f;
            K.Box("MonitorStand", items, new Vector3(0.9f, wy + 0.1f, 3.35f), new Vector3(0.06f, 0.2f, 0.06f), "Black", default(Vector3), false);
            C.Monitor(items, "RecordingMonitor", new Vector3(0.9f, wy + 0.36f, 3.36f), 0f, 0.52f, 0.30f);
            K.Box("Keyboard", items, new Vector3(0.9f, wy + 0.012f, 3.62f), new Vector3(0.44f, 0.02f, 0.15f), "Black", default(Vector3), false);
            C.FileStack(items, "Files_Clerk", new Vector3(-0.8f, wy, 3.5f), -12f, 5, "FolderGreen");
            C.FileStack(items, "Files_Clerk_B", new Vector3(-1.9f, wy, 3.45f), 6f, 3, "FolderBlue");
            C.Mic(items, "Mic_Clerk", new Vector3(0.2f, wy, 3.45f), 0f);
            K.Box("RecordingUnit", items, new Vector3(2.0f, wy + 0.06f, 3.45f), new Vector3(0.44f, 0.12f, 0.3f), "Black", default(Vector3), false);
            K.Sphere("RecordingUnit_Led", items, new Vector3(1.85f, wy + 0.08f, 3.29f), 0.02f, "LedRed");

            C.OfficeChair(g, "Chair_Clerk", new Vector3(0.9f, 0f, 4.3f), 180f);
        }

        // ───────────────────────── คอกพยาน ─────────────────────────
        static void WitnessStand()
        {
            var g = K.Group("WitnessStand", _struct);
            g.localPosition = new Vector3(0f, 0f, WIT_Z);
            const float RISER = 0.15f;

            // พื้นยกของคอก — ทางขึ้นเปิดด้านหลัง (ทิศใต้) พยานยืนหันหน้าหาองค์คณะ
            C.Slab(g, "Riser", -0.7f, 0.7f, -0.6f, 0.6f, RISER, RISER, "WoodDark");

            // ด้านหน้าหักมุมแปดเหลี่ยม ราวลูกกรงไม้แนวตั้งแบบภาพอ้างอิง
            Vector3[] a = { new Vector3(-0.7f, 0f, -0.6f), new Vector3(-0.7f, 0f, 0.35f), new Vector3(-0.45f, 0f, 0.6f),
                            new Vector3(0.45f, 0f, 0.6f), new Vector3(0.7f, 0f, 0.35f), new Vector3(0.7f, 0f, -0.6f) };
            for (int s = 0; s < a.Length - 1; s++)
                BalusterRun(g, "Side_" + s, a[s], a[s + 1], RISER);

            C.Nameplate(g, "Plate", new Vector3(0f, RISER + 0.3f, 0.68f), 0f, "พยาน");
            C.Mic(g, "Mic_Witness", new Vector3(0.25f, RISER + 1.07f, 0.6f), 180f);
            // แผ่นคำสาบานตนก่อนเบิกความ
            K.Box("OathCard", g, new Vector3(-0.3f, RISER + 1.08f, 0.6f), new Vector3(0.26f, 0.01f, 0.18f), "Paper",
                  new Vector3(-15f, 0f, 0f), false);
            K.Marker("MARK_WitnessStand", g, new Vector3(0f, RISER, -0.05f), 0f);
        }

        /// <summary>ราวคอกพยานหนึ่งด้าน: ฐาน + ลูกกรง + ราวจับหนา มี collider บาง ๆ กันเดินทะลุ</summary>
        static void BalusterRun(Transform parent, string name, Vector3 a, Vector3 b, float floorY)
        {
            var g = K.Group(name, parent);
            Vector3 mid = (a + b) * 0.5f;
            Vector3 dir = b - a;
            float len = dir.magnitude;
            g.localPosition = new Vector3(mid.x, floorY, mid.z);
            g.localRotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, dir.normalized));

            K.Box("Base", g, new Vector3(0f, 0.06f, 0f), new Vector3(len + 0.04f, 0.12f, 0.08f), "WoodDark", default(Vector3), false);
            K.Box("Handrail", g, new Vector3(0f, 1.02f, 0f), new Vector3(len + 0.1f, 0.09f, 0.12f), "Wood", default(Vector3), false);
            int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.11f));
            for (int i = 0; i <= n; i++)
            {
                float x = -len * 0.5f + len * i / n;
                K.Box("Baluster_" + i, g, new Vector3(x, 0.56f, 0f), new Vector3(0.04f, 0.88f, 0.04f), "Wood", default(Vector3), false);
            }
            C.Blocker(g, "Collider", new Vector3(0f, 0.55f, 0f), new Vector3(len, 1.1f, 0.1f));
        }

        // ───────────────────────── โต๊ะทนายสองฝั่ง ─────────────────────────
        static void CounselTables()
        {
            // ฝั่งตะวันตก = ทนายโจทก์ (เข้ม) หันหน้าเข้ากลางห้อง
            CounselTable("Table_Plaintiff", -TABLE_X, 90f, "ทนายโจทก์", true);
            // ฝั่งตะวันออก = ทนายจำเลย (ทนายของแชมป์)
            CounselTable("Table_Defense", TABLE_X, -90f, "ทนายจำเลย", false);
        }

        /// <summary>
        /// โต๊ะทนาย วางยาวตามแนวผนังข้าง แผงหน้าโต๊ะหันเข้ากลางห้อง เก้าอี้สองตัวด้านนอก
        /// เว้นช่องระหว่างเก้าอี้ 1.7 m ให้ผู้เล่นยืนว่าความตรงกลางโต๊ะได้
        /// </summary>
        static void CounselTable(string name, float x, float yaw, string plate, bool isKhem)
        {
            var g = K.Group(name, _struct);
            g.localPosition = new Vector3(x, 0f, TABLE_Z);
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            const float L = 3.0f, W = 0.85f;

            K.Box("Top", g, new Vector3(0f, 0.745f, 0f), new Vector3(L, 0.05f, W), "WoodDark", default(Vector3), false);
            K.Box("Modesty", g, new Vector3(0f, 0.37f, W * 0.5f - 0.04f), new Vector3(L, 0.72f, 0.06f), "Wood");
            K.Box("End_A", g, new Vector3(-L * 0.5f + 0.03f, 0.37f, 0f), new Vector3(0.06f, 0.72f, W - 0.05f), "Wood");
            K.Box("End_B", g, new Vector3(L * 0.5f - 0.03f, 0.37f, 0f), new Vector3(0.06f, 0.72f, W - 0.05f), "Wood");
            for (int i = 0; i < 3; i++)
                C.RaisedPanel(g, "Panel_" + i, new Vector3(-1.0f + i * 1.0f, 0.4f, W * 0.5f - 0.01f), 0.85f, 0.5f, 0f);

            float ty = 0.77f;
            C.FileStack(g, "Files_A", new Vector3(-1.05f, ty, -0.05f), 5f, 4, "FolderGreen");
            C.FileStack(g, "Files_B", new Vector3(0.95f, ty, -0.1f), -8f, 2, "FolderBlue");
            C.Mic(g, "Mic", new Vector3(0.35f, ty, 0.12f), 180f);
            C.Nameplate(g, "Nameplate", new Vector3(-0.2f, ty, W * 0.5f - 0.12f), 0f, plate);
            K.Cyl("WaterGlass", g, new Vector3(1.25f, ty + 0.06f, 0.1f), 0.07f, 0.12f, "Glass");

            C.OfficeChair(g, "Chair_A", new Vector3(-1.15f, 0f, -0.85f), 0f);
            C.OfficeChair(g, "Chair_B", new Vector3(1.15f, 0f, -0.85f), 0f);

            if (isKhem)
            {
                // แฟ้มหลักฐานของเข้ม — จุดเลือกหลักฐานตอนซักค้าน (รูปถ่ายเศษสีรถที่คลับ VIP)
                var folder = K.Group("INTERACT_EvidenceFolder", g);
                folder.localPosition = new Vector3(0.05f, ty, -0.18f);
                folder.localEulerAngles = new Vector3(0f, 12f, 0f);
                K.Box("Binder", folder, new Vector3(0f, 0.025f, 0f), new Vector3(0.30f, 0.05f, 0.36f), "FolderBlue", default(Vector3), false);
                for (int i = 0; i < 4; i++)
                    K.Box("Photo_" + i, folder, new Vector3(-0.3f + i * 0.02f, 0.004f + i * 0.002f, 0.06f - i * 0.05f),
                          new Vector3(0.15f, 0.004f, 0.1f), "Paper", new Vector3(0f, -20f + i * 11f, 0f), false);
                var col = folder.gameObject.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.center = new Vector3(-0.1f, 0.05f, 0f);
                col.size = new Vector3(0.6f, 0.2f, 0.5f);
            }
        }

        // ───────────────────────── ราวกั้น + ประตูคอก ─────────────────────────
        static void BarRail()
        {
            var g = K.Group("BarRail", _struct);
            var d = K.Group("BarRail_Carving", _dress);
            float half = K.Nav.DoorClear * 0.5f;

            for (int side = -1; side <= 1; side += 2)
            {
                float x0 = side < 0 ? -HX : half + 0.14f;
                float x1 = side < 0 ? -half - 0.14f : HX;
                string tag = side < 0 ? "W" : "E";
                float cx = (x0 + x1) * 0.5f, len = x1 - x0;

                C.Panel(g, "Bar_" + tag, x0, x1, BAR_Z - 0.04f, BAR_Z + 0.04f, 0f, 0.75f, "Wood");
                C.PiercedRail(d, "Rail_" + tag, new Vector3(cx, 0.75f, BAR_Z), len, 0.28f, 0f);
                for (float px = x0 + 0.75f; px < x1 - 0.4f; px += 1.35f)
                {
                    C.RaisedPanel(d, "Panel_N_" + tag + px.ToString("0.0"), new Vector3(px, 0.4f, BAR_Z + 0.04f), 1.05f, 0.5f, 0f);
                    C.RaisedPanel(d, "Panel_S_" + tag + px.ToString("0.0"), new Vector3(px, 0.4f, BAR_Z - 0.04f), 1.05f, 0.5f, 180f);
                }
                // เสาหัวเสาข้างประตูคอก
                float nx = side * (half + 0.07f);
                K.Box("Newel_" + tag, g, new Vector3(nx, 0.55f, BAR_Z), new Vector3(0.14f, 1.1f, 0.14f), "WoodDark");
                K.Sphere("Newel_Knob_" + tag, d, new Vector3(nx, 1.14f, BAR_Z), 0.12f, "WoodLight");
            }

            // ประตูคอกสองบาน เปิดค้างเข้าด้านใน (collider ถูกถอดโดย DoorLeaf อยู่แล้ว)
            K.DoorLeaf("Gate_W", g, new Vector3(-half, 0f, BAR_Z), half - 0.02f, 0.82f, 0.05f, "Wood", 0f, -90f);
            K.DoorLeaf("Gate_E", g, new Vector3(half, 0f, BAR_Z), half - 0.02f, 0.82f, 0.05f, "Wood", 180f, 90f);
            K.Marker("DOOR_BarGate", _circ, new Vector3(0f, 0f, BAR_Z));

            // ป้ายฝั่งคู่ความ หันเข้าหาองค์คณะ
            C.Nameplate(d, "Plate_Plaintiff", new Vector3(-3.55f, 0.78f, BAR_Z + 0.06f), 0f, "โจทก์");
            C.Nameplate(d, "Plate_Defendant", new Vector3(3.55f, 0.78f, BAR_Z + 0.06f), 0f, "จำเลย");
        }

        // ───────────────────────── ที่นั่ง ─────────────────────────
        static void Seating()
        {
            var g = K.Group("Seating", _struct);

            // ที่นั่งคู่ความ ชิดหลังราวกั้น
            float pl = BANK_X - 1.3f;
            C.PublicBench(g, "Bench_Plaintiff", new Vector3(-(1.3f + BANK_X) * 0.5f, 0f, PARTY_Z), pl, 0f);
            C.PublicBench(g, "Bench_Defendant", new Vector3((1.3f + BANK_X) * 0.5f, 0f, PARTY_Z), pl, 0f);

            // ที่นั่งประชาชนสองฝั่งทางเดินกลาง
            float bl = BANK_X - AISLE_HALF;
            float bc = (BANK_X + AISLE_HALF) * 0.5f;
            for (int r = 0; r < ROW_Z.Length; r++)
            {
                C.PublicBench(g, "Gallery_W_" + r, new Vector3(-bc, 0f, ROW_Z[r]), bl, 0f);
                C.PublicBench(g, "Gallery_E_" + r, new Vector3(bc, 0f, ROW_Z[r]), bl, 0f);
            }
        }

        // ───────────────────────── ของกลาง + จอหลักฐาน ─────────────────────────
        static void EvidenceProps()
        {
            // จอทีวีบนขาตั้งล้อเลื่อน หันหาโต๊ะทนายโจทก์ — ใช้เปิดคลิปจาก SD Card ใน The Climax
            var tv = K.Group("SCREEN_EvidenceTV", _dress);
            tv.localPosition = TV_POS;
            Vector3 face = KHEM_SPOT - TV_POS; face.y = 0f;
            tv.localRotation = Quaternion.LookRotation(face.normalized);
            K.Box("Base", tv, new Vector3(0f, 0.08f, 0f), new Vector3(0.8f, 0.06f, 0.6f), "Black");
            for (int i = 0; i < 4; i++)
                K.Sphere("Wheel_" + i, tv, new Vector3((i % 2 == 0) ? -0.34f : 0.34f, 0.04f, (i < 2) ? -0.24f : 0.24f), 0.08f, "Black");
            K.Box("Pole", tv, new Vector3(0f, 0.85f, -0.06f), new Vector3(0.09f, 1.5f, 0.07f), "Black", default(Vector3), false);
            K.Box("Shelf", tv, new Vector3(0f, 0.78f, 0.04f), new Vector3(0.46f, 0.03f, 0.32f), "Black", default(Vector3), false);
            K.Box("Laptop", tv, new Vector3(0f, 0.805f, 0.04f), new Vector3(0.34f, 0.02f, 0.24f), "Chrome", default(Vector3), false);
            K.Box("SDCardReader", tv, new Vector3(0.14f, 0.82f, 0.13f), new Vector3(0.05f, 0.012f, 0.03f), "Black", default(Vector3), false);
            C.Monitor(tv, "Screen", new Vector3(0f, 1.55f, 0f), 0f, 1.22f, 0.69f);
            K.Marker("MARK_ScreenCentre", tv, new Vector3(0f, 1.55f, 0.05f));

            // โต๊ะวางของกลาง: เศษกันชนสีแดงในถุงหลักฐาน + ภาพถ่าย
            var ex = K.Group("ExhibitTable", _dress);
            ex.localPosition = new Vector3(2.3f, 0f, 2.3f);
            ex.localEulerAngles = new Vector3(0f, -20f, 0f);
            K.Box("Top", ex, new Vector3(0f, 0.74f, 0f), new Vector3(1.1f, 0.04f, 0.6f), "WoodDark");
            for (int i = 0; i < 4; i++)
                K.Box("Leg_" + i, ex, new Vector3((i % 2 == 0) ? -0.5f : 0.5f, 0.36f, (i < 2) ? -0.25f : 0.25f),
                      new Vector3(0.05f, 0.72f, 0.05f), "WoodDark", default(Vector3), false);
            K.Box("Bag", ex, new Vector3(-0.2f, 0.8f, 0f), new Vector3(0.36f, 0.08f, 0.28f), "EvidenceBag", default(Vector3), false);
            K.Box("BumperShard", ex, new Vector3(-0.2f, 0.79f, 0f), new Vector3(0.22f, 0.04f, 0.13f), "RedPaint", new Vector3(0f, 25f, 6f), false);
            K.Box("Tag", ex, new Vector3(0.02f, 0.765f, 0.1f), new Vector3(0.1f, 0.005f, 0.06f), "Paper", default(Vector3), false);
            C.ThaiSign("Tag_Text", ex, new Vector3(0.1f, 0.845f, -0.305f), new Vector2(0.5f, 0.07f), "ของกลาง", C.RedInk, 180f);
            for (int i = 0; i < 3; i++)
                K.Box("Photo_" + i, ex, new Vector3(0.2f + i * 0.12f, 0.765f, -0.05f + i * 0.04f), new Vector3(0.15f, 0.004f, 0.1f),
                      "Paper", new Vector3(0f, i * 9f, 0f), false);
        }

        // ───────────────────────── ของตกแต่งห้อง ─────────────────────────
        static void RoomDressing()
        {
            var g = K.Group("Room", _dress);

            // ประตูเข้าห้อง (บานคู่ ปิดอยู่ — ออกด้วยการกด E ที่ประตู ดู Gameplay)
            var door = K.Group("MainDoor", _circ);
            float dz = Z0 + 0.05f;
            C.Panel(door, "Leaf_L", -0.95f, -0.01f, dz, dz + 0.05f, 0f, 2.4f, "WoodLight", false);
            C.Panel(door, "Leaf_R", 0.01f, 0.95f, dz, dz + 0.05f, 0f, 2.4f, "WoodLight", false);
            C.Panel(door, "Jamb_L", -1.1f, -0.95f, Z0, dz + 0.1f, 0f, 2.55f, "WoodDark", false);
            C.Panel(door, "Jamb_R", 0.95f, 1.1f, Z0, dz + 0.1f, 0f, 2.55f, "WoodDark", false);
            C.Panel(door, "Head", -1.1f, 1.1f, Z0, dz + 0.1f, 2.4f, 0.18f, "WoodDark", false);
            for (int s = -1; s <= 1; s += 2)
            {
                C.Panel(door, "Vision_" + s, s * 0.48f - 0.12f, s * 0.48f + 0.12f, dz + 0.05f, dz + 0.06f, 1.35f, 0.6f, "Glass", false);
                C.RaisedPanel(door, "LeafPanel_" + s, new Vector3(s * 0.48f, 0.6f, dz + 0.05f), 0.7f, 0.8f, 0f);
                K.Box("PushBar_" + s, door, new Vector3(s * 0.1f, 1.05f, dz + 0.1f), new Vector3(0.03f, 0.4f, 0.03f), "Chrome", default(Vector3), false);
            }

            // นาฬิกาเหนือประตู — 09:00 เวลาเริ่มพิจารณาคดีของศาลไทย
            WallClock(g, new Vector3(0f, 3.3f, Z0 + 0.02f), 0f);

            // ป้ายประกาศข้างประตู
            C.NoticePlate(g, "Notice_Phones", new Vector3(-2.6f, 1.85f, Z0 + 0.02f), 0f, new Vector2(0.9f, 0.3f), "โปรดปิดโทรศัพท์มือถือ");
            C.NoticePlate(g, "Notice_NoRecording", new Vector3(2.6f, 1.85f, Z0 + 0.02f), 0f, new Vector2(1.1f, 0.34f), "ห้ามถ่ายภาพ บันทึกภาพ หรือบันทึกเสียง");

            // ประตูห้องเจ้าหน้าที่ (ผนังตะวันตก ปิดตาย) — หมายเลข 7 ในภาพอ้างอิง
            var sd = K.Group("StaffDoor", _circ);
            float sx = -HX + 0.05f;
            C.Panel(sd, "Leaf", sx, sx + 0.05f, 3.7f, 4.7f, 0f, 2.25f, "WoodLight", false);
            C.Panel(sd, "Frame_A", -HX, sx + 0.09f, 3.58f, 3.7f, 0f, 2.35f, "WoodDark", false);
            C.Panel(sd, "Frame_B", -HX, sx + 0.09f, 4.7f, 4.82f, 0f, 2.35f, "WoodDark", false);
            C.Panel(sd, "Frame_Head", -HX, sx + 0.09f, 3.58f, 4.82f, 2.25f, 0.12f, "WoodDark", false);
            C.Panel(sd, "Window", sx + 0.05f, sx + 0.06f, 4.0f, 4.4f, 1.3f, 0.55f, "Glass", false);
            K.Box("Handle", sd, new Vector3(sx + 0.09f, 1.05f, 4.55f), new Vector3(0.04f, 0.03f, 0.14f), "Chrome", default(Vector3), false);
            C.NoticePlate(sd, "Sign", new Vector3(sx + 0.06f, 2.55f, 4.2f), 90f, new Vector2(0.8f, 0.2f), "เจ้าหน้าที่");
            K.Marker("SHUT_StaffDoor", sd, new Vector3(-HX + 0.7f, 0f, 4.2f));

            // แอร์ติดผนังแบบอาคารราชการไทย
            ACUnit(g, "AC_E_0", new Vector3(HX - 0.12f, 3.55f, -6.0f), -90f);
            ACUnit(g, "AC_E_1", new Vector3(HX - 0.12f, 3.55f, 0.0f), -90f);
            ACUnit(g, "AC_W_0", new Vector3(-HX + 0.12f, 3.55f, -6.0f), 90f);
            ACUnit(g, "AC_W_1", new Vector3(-HX + 0.12f, 3.55f, -1.0f), 90f);

            // จอติดผนังสองฝั่ง ให้คนในห้อง (ป้าสมร) เห็นคลิปพร้อมกัน
            C.Monitor(g, "WallScreen_W", new Vector3(-HX + 0.06f, 3.0f, 3.3f), 90f, 1.1f, 0.62f);
            C.Monitor(g, "WallScreen_E", new Vector3(HX - 0.06f, 3.0f, 3.3f), -90f, 1.1f, 0.62f);

            // กล้องวงจรปิด — ห้องนี้มีกล้องที่ "ใช้งานได้" ตัดกับกล้องที่ "เสียพอดี" ใน ACT 1
            CCTV(g, "CCTV_SW", new Vector3(-HX + 0.25f, H - 0.12f, Z0 + 0.25f));
            CCTV(g, "CCTV_NE", new Vector3(HX - 0.25f, H - 0.12f, Z1 - 0.25f));

            // ลำโพงเพดาน
            float[] sz = { -8f, -3f, 2f };
            for (int i = 0; i < sz.Length; i++)
            {
                K.Cyl("Speaker_W_" + i, g, new Vector3(-6.2f, H - 0.01f, sz[i]), 0.26f, 0.02f, "PlasticWhite");
                K.Cyl("Speaker_E_" + i, g, new Vector3(6.2f, H - 0.01f, sz[i]), 0.26f, 0.02f, "PlasticWhite");
            }
        }

        static void WallClock(Transform parent, Vector3 pos, float yaw)
        {
            var g = K.Group("WallClock_0900", parent);
            g.localPosition = pos;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Cyl("Rim", g, new Vector3(0f, 0f, 0.02f), 0.44f, 0.04f, "Black", new Vector3(90f, 0f, 0f));
            K.Cyl("Face", g, new Vector3(0f, 0f, 0.042f), 0.38f, 0.01f, "Paper", new Vector3(90f, 0f, 0f));
            // คนมองจาก +Z เห็นแกน +X อยู่ทางซ้ายมือ = เลข 9
            K.Box("Hand_Hour", g, new Vector3(0.055f, 0f, 0.05f), new Vector3(0.11f, 0.02f, 0.005f), "Black", default(Vector3), false);
            K.Box("Hand_Minute", g, new Vector3(0f, 0.08f, 0.052f), new Vector3(0.012f, 0.16f, 0.005f), "Black", default(Vector3), false);
        }

        static void ACUnit(Transform parent, string name, Vector3 pos, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = pos;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Body", g, new Vector3(0f, 0f, 0.11f), new Vector3(0.95f, 0.30f, 0.22f), "PlasticWhite", default(Vector3), false);
            K.Box("Vent", g, new Vector3(0f, -0.11f, 0.215f), new Vector3(0.8f, 0.05f, 0.02f), "Black", default(Vector3), false);
            K.Sphere("Led", g, new Vector3(0.36f, -0.05f, 0.225f), 0.015f, "LedWarm");
        }

        static void CCTV(Transform parent, string name, Vector3 pos)
        {
            var g = K.Group(name, parent);
            g.localPosition = pos;
            K.Cyl("Mount", g, new Vector3(0f, 0.08f, 0f), 0.16f, 0.04f, "PlasticWhite");
            K.Sphere("Dome", g, Vector3.zero, 0.16f, "Black");
            K.Sphere("Led", g, new Vector3(0f, -0.02f, 0.07f), 0.015f, "LedRed");
        }

        // ───────────────────────── lighting ─────────────────────────
        static void Lighting()
        {
            var g = K.Group("KeyLights", _light);
            var fx = K.Group("Fixtures", _light);

            // โคมฟลูออเรสเซนต์ฝังฝ้าแบบห้องราชการ
            float[] tx = { -3.0f, 0f, 3.0f };
            float[] tz = { -6f, -3f, 0f, 3f, 6f };
            for (int i = 0; i < tx.Length; i++)
                for (int k = 0; k < tz.Length; k++)
                    K.Box("Troffer_" + i + "_" + k, fx, new Vector3(tx[i], COFFER_Y - 0.015f, tz[k]),
                          new Vector3(0.6f, 0.03f, 1.2f), "LightPanel", default(Vector3), false);
            for (int k = 0; k < 5; k++)
            {
                float z = -8f + k * 4f;
                K.Box("Troffer_W_" + k, fx, new Vector3(-6.1f, H - 0.015f, z), new Vector3(0.6f, 0.03f, 1.2f), "LightPanel", default(Vector3), false);
                K.Box("Troffer_E_" + k, fx, new Vector3(6.1f, H - 0.015f, z), new Vector3(0.6f, 0.03f, 1.2f), "LightPanel", default(Vector3), false);
            }
            // ไฟซ่อนรอบหลุมฝ้า
            K.NeonStrip("Cove_W", fx, new Vector3(-CX + 0.1f, H + 0.05f, 0f), new Vector3(0.05f, 0.04f, CZ1 - CZ0 - 0.2f), "LedWarm", Color.white);
            K.NeonStrip("Cove_E", fx, new Vector3(CX - 0.1f, H + 0.05f, 0f), new Vector3(0.05f, 0.04f, CZ1 - CZ0 - 0.2f), "LedWarm", Color.white);

            // ไฟจริง: ขาวอมเหลืองนวล ๆ กระจายทั่วห้อง ไม่ทำเงา (ประหยัด)
            Color work = new Color(1f, 0.96f, 0.88f);
            float[,] spots = { { -2.5f, -6f }, { 2.5f, -6f }, { -2.5f, -1f }, { 2.5f, -1f }, { -2.5f, 3.5f }, { 2.5f, 3.5f } };
            for (int i = 0; i < spots.GetLength(0); i++)
                K.AddLight(g, "Ceiling_" + i, new Vector3(spots[i, 0], COFFER_Y - 0.1f, spots[i, 1]), new Vector3(90f, 0f, 0f),
                           LightType.Spot, work, 5.5f, 10f, 118f);
            K.AddLight(g, "Ceiling_W_Aisle", new Vector3(-6.1f, H - 0.1f, -3f), new Vector3(90f, 0f, 0f), LightType.Spot, work, 3.5f, 7f, 110f);
            K.AddLight(g, "Ceiling_E_Aisle", new Vector3(6.1f, H - 0.1f, -3f), new Vector3(90f, 0f, 0f), LightType.Spot, work, 3.5f, 7f, 110f);

            // ไฟหลักส่ององค์คณะจากด้านบนค่อนหน้า ทำเงาชัด ให้บัลลังก์ดูน่าเกรงขาม
            var key = K.AddLight(g, "Key_Bench", new Vector3(0f, H - 0.2f, 3.6f), new Vector3(58f, 0f, 0f),
                                 LightType.Spot, new Color(1f, 0.93f, 0.82f), 9f, 12f, 70f, true);
            key.shadowStrength = 0.7f;
            // ไฟส่องตราชู
            K.AddLight(g, "Accent_Emblem", new Vector3(0f, H - 0.15f, 7.8f), new Vector3(30f, 0f, 0f),
                       LightType.Spot, new Color(1f, 0.85f, 0.6f), 6f, 6f, 45f);

            // แดดเช้าลอดหน้าต่างฝั่งตะวันออก พาดเป็นวงบนพื้นห้อง
            float[] wz = { -7.5f, -4.5f, -1.5f, 1.5f };
            for (int i = 0; i < wz.Length; i++)
                K.AddLight(g, "MorningSun_" + i, new Vector3(HX - 0.4f, 3.9f, wz[i]), new Vector3(48f, -100f, 0f),
                           LightType.Spot, new Color(1f, 0.88f, 0.66f), 7f, 11f, 55f);
        }

        // ───────────────────────── นักแสดง ─────────────────────────
        static void Actors()
        {
            var a = _actors;

            // องค์คณะผู้พิพากษา — ท่านกลางคือเจ้าของสำนวน (รับงานมาแล้ว)
            // เก้าอี้บัลลังก์ยกสูง ผู้พิพากษานั่งแล้วต้องโผล่พ้นราวบัลลังก์ (2.03 m) ตั้งแต่อกขึ้นไป
            const float JUDGE_SEATED = 1.6f;
            C.Person(a, "Judge_Left", new Vector3(JUDGE_X[0], PLAT_Y, JUDGE_Z - 0.05f), 180f, "Robe", true, JUDGE_SEATED);
            C.Person(a, "Judge_Presiding", new Vector3(JUDGE_X[1], PLAT_Y, JUDGE_Z - 0.05f), 180f, "Robe", true, JUDGE_SEATED);
            C.Person(a, "Judge_Right", new Vector3(JUDGE_X[2], PLAT_Y, JUDGE_Z - 0.05f), 180f, "Robe", true, JUDGE_SEATED);

            C.Person(a, "Clerk", new Vector3(0.9f, 0f, 4.3f), 180f, "OfficerWhite", true);

            // พยานนั่งรอในห้อง แล้วเดินเข้าคอกพยานเมื่อศาลเรียก (CourtroomTrial.CallWitness)
            C.Person(a, "Witness_Garage", new Vector3(-1.7f, 0f, ROW_Z[1] + 0.03f), 0f, "Civilian", true);    // นายเอกชัย (พยานโจทก์)
            C.Person(a, "Witness_False", new Vector3(1.7f, 0f, ROW_Z[1] + 0.03f), 0f, "Witness", true);      // นายวิชัย (พยานจำเลย)
            C.Person(a, "Witness_Mechanic", new Vector3(1.6f, 0f, ROW_Z[2] + 0.03f), 0f, "CivilianB", true); // นายสมบัติ (พยานจำเลย)

            // ฝั่งจำเลย: ทนายนั่งเก้าอี้ใกล้บัลลังก์ ผู้ช่วยนั่งอีกตัว
            C.Person(a, "Defense_Lawyer", new Vector3(TABLE_X + 0.85f, 0f, TABLE_Z + 1.15f), -90f, "Robe", true);
            C.Person(a, "Defense_Assistant", new Vector3(TABLE_X + 0.85f, 0f, TABLE_Z - 1.15f), -90f, "SuitBlack", true);

            // คู่ความ
            C.Person(a, "Aunt_Samorn", new Vector3(-2.3f, 0f, PARTY_Z + 0.03f), 0f, "AuntCloth", true);
            C.Person(a, "Champ", new Vector3(2.4f, 0f, PARTY_Z + 0.03f), 0f, "ChampSuit", true);

            // ตำรวจศาล
            C.Person(a, "CourtPolice_Door", new Vector3(-2.0f, 0f, Z0 + 0.8f), 0f, "PoliceKhaki", false);
            C.Person(a, "CourtPolice_Dock", new Vector3(6.65f, 0f, PARTY_Z), -90f, "PoliceKhaki", false);

            // ผู้ชม: ฝั่งตะวันตกชาวบ้านมาให้กำลังใจป้าสมร ฝั่งตะวันออกบอดี้การ์ดกับนักข่าวของฝั่งแชมป์
            C.Person(a, "Gallery_Neighbour_A", new Vector3(-4.6f, 0f, ROW_Z[0] + 0.03f), 0f, "Civilian", true);
            C.Person(a, "Gallery_Neighbour_B", new Vector3(-2.0f, 0f, ROW_Z[0] + 0.03f), 0f, "CivilianB", true);
            C.Person(a, "Gallery_Neighbour_C", new Vector3(-3.4f, 0f, ROW_Z[1] + 0.03f), 0f, "Civilian", true);
            C.Person(a, "Gallery_Relative", new Vector3(-5.0f, 0f, ROW_Z[2] + 0.03f), 0f, "CivilianB", true);
            C.Person(a, "Gallery_Bodyguard_A", new Vector3(2.0f, 0f, ROW_Z[0] + 0.03f), 0f, "SuitBlack", true);
            C.Person(a, "Gallery_Bodyguard_B", new Vector3(3.0f, 0f, ROW_Z[0] + 0.03f), 0f, "SuitBlack", true);
            C.Person(a, "Gallery_Reporter", new Vector3(4.9f, 0f, ROW_Z[1] + 0.03f), 0f, "Civilian", true);
            C.Person(a, "Gallery_Spectator", new Vector3(3.8f, 0f, ROW_Z[2] + 0.03f), 0f, "CivilianB", true);
            C.Person(a, "Gallery_Spectator_B", new Vector3(4.2f, 0f, ROW_Z[3] + 0.03f), 0f, "Civilian", true);
        }

        // ───────────────────────── gameplay ─────────────────────────
        static void Gameplay()
        {
            var g = _play;

            // ผู้เล่น (เข้ม) เดินเข้าห้องจากประตูหลัง ให้เห็นทั้งห้องก่อนเริ่มพิจารณาคดี
            var spawn = new Vector3(0f, 0f, Z0 + 1.3f);
            K.Marker("PlayerSpawn_Entrance", g, spawn + Vector3.up * 0.2f);
            K.PlacePlayer(C.P_PLAYER, g, new Vector3(spawn.x, 0.02f, spawn.z), 0f);

            // หมุดนำทาง (LevelAudit ใช้เป็นจุดเริ่ม flood fill ด้วย)
            K.Marker("NAV_Aisle", g, new Vector3(0f, 0f, -5f));
            K.Marker("NAV_Well", g, new Vector3(0f, 0f, -0.4f));
            K.Marker("NAV_StairFoot_Bench", g, new Vector3((STAIR_X0 + STAIR_X1) * 0.5f, 0f, STAIR_Z0 - 0.6f));
            K.Marker("NAV_Platform", g, new Vector3(3.5f, PLAT_Y, 8.4f));

            // ── บีทของ ACT 4.1 - 4.2 ──
            var beats = K.Group("BEATS", g);
            C.Mark(beats, "OBJ_A4_01_TakeSeat", KHEM_SPOT, 90f);                             // เดินไปประจำโต๊ะทนายโจทก์
            C.Mark(beats, "POS_CrossExamine", new Vector3(-2.2f, 0f, 0.3f), 70f);              // จุดยืนซักค้านพยาน
            // ยืนข้างจอตอนเปิดคลิป — เยื้องออกจากแนวกล้อง CAM_TVScreen ไม่งั้นตัวเข้มบังจอ
            C.Mark(beats, "POS_PlayClip", new Vector3(-5.0f, 0f, 4.3f), 70f);
            C.Mark(beats, "MARK_DefenseStand", new Vector3(TABLE_X + 0.75f, 0f, TABLE_Z), -90f); // ทนายจำเลยลุกขึ้นยิ้ม
            C.Mark(beats, "MARK_AuntStand", new Vector3(-2.3f, 0f, PARTY_Z + 0.45f), 0f);      // ป้าสมรลุกขึ้นร้องไห้ดีใจ
            C.Mark(beats, "MARK_ChampSneer", new Vector3(-4.9f, 0f, -0.5f), -15f);             // แชมป์มายืนเยาะเย้ยเข้ม
            C.Path(beats, "PATH_ChampToKhem",
                   new Vector3(2.4f, 0f, PARTY_Z + 0.5f), new Vector3(0.3f, 0f, -1.1f), new Vector3(-3.2f, 0f, -0.9f),
                   new Vector3(-4.9f, 0f, -0.5f));
            C.Path(beats, "PATH_ChampExit",
                   new Vector3(-3.2f, 0f, -0.9f), new Vector3(0f, 0f, -0.9f), new Vector3(0f, 0f, -5f), new Vector3(0f, 0f, Z0 + 0.6f));
            C.Path(beats, "PATH_AuntExit",
                   new Vector3(-2.3f, 0f, PARTY_Z + 0.45f), new Vector3(-0.6f, 0f, -3.0f), new Vector3(-0.6f, 0f, Z0 + 0.6f));

            // ── หมุดกล้อง cutscene ──
            var cams = K.Group("CAMERAS", g);
            C.CamMarker(cams, "CAM_Establishing", new Vector3(0f, 3.4f, Z0 + 0.6f), new Vector3(0f, 1.6f, 4.5f));
            // ต้องสูงพอข้ามราวโต๊ะหน้าบัลลังก์ (1.33 m) และหน้าบัลลังก์ (2.03 m) — ต่ำกว่านี้ราวบังองค์คณะหมด
            C.CamMarker(cams, "CAM_JudgesLowAngle", new Vector3(0.6f, 1.75f, 2.0f), new Vector3(0f, 2.3f, JUDGE_Z));
            C.CamMarker(cams, "CAM_Witness", new Vector3(-0.55f, 1.7f, 2.55f), new Vector3(0f, 1.75f, WIT_Z));
            C.CamMarker(cams, "CAM_KhemOverShoulder", new Vector3(KHEM_SPOT.x - 0.5f, 1.75f, TABLE_Z - 0.4f), new Vector3(0f, 1.6f, WIT_Z));
            C.CamMarker(cams, "CAM_TVScreen", new Vector3(-5.1f, 1.6f, 2.4f), TV_POS + new Vector3(0f, 1.55f, 0f));
            C.CamMarker(cams, "CAM_DefenseLawyer", new Vector3(2.6f, 1.55f, TABLE_Z), new Vector3(TABLE_X + 0.75f, 1.6f, TABLE_Z));
            C.CamMarker(cams, "CAM_ChampDock", new Vector3(1.2f, 1.35f, -0.6f), new Vector3(2.4f, 1.1f, PARTY_Z));
            C.CamMarker(cams, "CAM_AuntSamorn", new Vector3(-1.0f, 1.35f, -0.6f), new Vector3(-2.3f, 1.1f, PARTY_Z));
            // อยู่หลังราวเจาะลายบนโต๊ะบัลลังก์ ไม่งั้นลูกกรงบังค้อน
            C.CamMarker(cams, "CAM_Gavel", new Vector3(0.95f, 2.3f, 5.4f), GAVEL_POS + new Vector3(0.14f, 0.07f, 0f));
            C.CamMarker(cams, "CAM_ChampSneer", new Vector3(KHEM_SPOT.x - 0.2f, 1.7f, TABLE_Z + 0.4f), new Vector3(-4.9f, 1.65f, -0.5f));

            // ── ระบบ ACT 4 ──
            var boot = new GameObject("~Bootstrap");
            boot.transform.SetParent(g, false);
            boot.AddComponent<Act4Bootstrap>();

            // ตัวแทนเข้มในช็อตมุมที่สาม (ผู้เล่นไม่มีตัว) — ปิดไว้ คัตซีนเปิดเอง
            var khem = C.Person(_actors, "Khem_StandIn", KHEM_SPOT, 90f, "Robe", false);
            if (khem != null)
            {
                foreach (var c in khem.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                khem.SetActive(false);
            }

            var host = new GameObject("~Act4 Courtroom");
            host.transform.SetParent(g, false);
            var seq = host.AddComponent<CourtroomSequence>();
            seq.khemStandIn = khem;
            seq.data = C.DataAsset<Act4CourtroomData>("Act4_CourtroomData", d => d.ResetToDefaults());
            seq.clip = WireScreen(seq);

            // ประตูออก: เปิดได้หลังพิพากษาเท่านั้น กดแล้ว Act2Director พาไปซีนหน้าศาลเอง
            var exit = new GameObject("EXIT_ToCourtSteps");
            exit.transform.SetParent(g, false);
            exit.transform.localPosition = new Vector3(0f, 1.2f, Z0 + 0.35f);
            var box = exit.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.0f, 2.4f, 0.5f);
            var door = exit.AddComponent<Act2Interactable>();
            door.objectName = "ประตูห้องพิจารณาคดี";
            door.objectiveId = Act4Script.OBJ_LeaveCourt;
            door.requiresObjective = Act4Script.OBJ_PlayClip;
            door.blockedLine = "การพิจารณาคดียังไม่จบ";
        }

        /// <summary>
        /// จอทีวี: กล่องกด E (trigger) + คลิปกล้องหน้ารถ
        /// กดได้หลังซักค้านจบ แล้วเรียก CourtroomSequence.PlayClip ผ่าน persistent listener
        /// </summary>
        static DashcamClip WireScreen(CourtroomSequence seq)
        {
            var tv = _dress.Find("SCREEN_EvidenceTV");
            if (tv == null) { Debug.LogWarning("[Ch4Courtroom] ไม่พบ SCREEN_EvidenceTV"); return null; }

            // trigger คลุมตัวจอ อยู่หน้าฐานล้อ เรย์ของ PlayerInteractor จะชนตัวนี้ก่อนของทึบ
            var col = tv.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 1.5f, 0.05f);
            col.size = new Vector3(1.4f, 1.0f, 0.3f);

            var act = tv.gameObject.AddComponent<Act2Interactable>();
            act.objectName = "จอทีวี — เปิดคลิปจาก SD Card";
            act.requiresObjective = Act4Script.OBJ_CrossExamine;
            act.blockedLine = "ยังไม่ถึงเวลา — ต้องหักคำพยานให้ได้ก่อน";
            // UnityEvent ยังเป็น null จนกว่า Unity จะ serialize component ครั้งแรก
            if (act.onInteract == null) act.onInteract = new UnityEngine.Events.UnityEvent();
            UnityEventTools.AddVoidPersistentListener(act.onInteract, seq.PlayClip);

            var clip = tv.gameObject.AddComponent<DashcamClip>();
            var monitor = tv.Find("Screen");
            clip.overlayRoot = monitor;
            var surface = monitor != null ? monitor.Find("Screen") : null;
            clip.screen = surface != null ? surface.GetComponent<Renderer>() : null;
            clip.unlitBase = C.UnlitMat("UnlitBase", Color.white);
            return clip;
        }
    }
}
