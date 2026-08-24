using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using K = SecretsThatBreathe.LevelTools.LevelKit;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// CHAPTER 3 - PENTHOUSE. เข้ามาจากคลับ (Main2_Club) ด้วยลิฟต์ส่วนตัว
    ///
    /// ผังสองชั้น มองจากด้านบน (x: ตะวันตก -> ตะวันออก, z: ใต้ -> เหนือ)
    ///
    ///   L2  y 3.60   +--------+---------------+----------------------+
    ///   ชั้นบน       |  POOL  |   TERRACE     |     UPPER SUITE      |
    ///                | (ยื่น)  |  (โล่ง/มีชายคา) |   (กระจก + หลังคา)    |
    ///                +--------+------[บันได]--+----------------------+
    ///
    ///   L1  y 0.00            +---------------+----------------------+
    ///   ชั้นล่าง               |    LIVING     |    DINING / KITCHEN  |   z  2..12
    ///                         +---------------+----------------------+
    ///                         |            CORRIDOR                  |   z -2.. 2
    ///                         +-------+---------------+--------------+
    ///                         | STUDY |   BEDROOM     |  LIFT LOBBY  |   z-12..-2
    ///                         +-------+---------------+--------------+
    ///
    /// ชั้นสองยื่นเลยชั้นล่างไปทางตะวันตก 4.5 m สระว่ายน้ำจึงลอยอยู่นอกตัวอาคาร (infinity pool)
    /// เหมือนภาพอ้างอิง และไม่ไปกินเพดานห้องข้างล่าง
    ///
    /// ทุกทางเดินกว้างอย่างน้อย <see cref="LevelKit.Nav.PathClear"/> บันไดลูกตั้ง 0.18 m
    /// และขั้นบันไดสระ 0.24 m ทั้งคู่ต่ำกว่า stepOffset ของ CharacterController ผู้เล่นจึงไม่ติด
    ///
    /// Menu: Tools > Secrets That Breathe > Build Chapter 3 Penthouse
    /// </summary>
    public static class Ch3PenthouseBuilder
    {
        // ── master dimensions (metres) ──
        public const float HX = 16f;              // ครึ่งความกว้างชั้นสอง -> 32 m
        public const float HZ = 12f;              // ครึ่งความลึก        -> 24 m

        /// <summary>ขอบตะวันตกของชั้นล่าง — ชั้นสองยื่นเลยไปถึง -HX</summary>
        public const float L1_X0 = -11.5f;

        public const float L2_Y = 3.60f;          // ระดับพื้นชั้นสอง = ความสูงชั้นล่าง
        public const float L2_H = 3.40f;          // ความสูงชั้นสอง
        public const float ROOF_Y = L2_Y + L2_H;  // 7.00 — ท้องหลังคา

        // สระว่ายน้ำ ริมขอบตะวันตกของชั้นสอง
        public const float POOL_X0 = -HX, POOL_X1 = L1_X0;      // -16 .. -11.5
        public const float POOL_Z0 = -7f, POOL_Z1 = 7f;
        public const float POOL_DEPTH = 1.20f;

        // ช่องบันได เจาะพื้นชั้นสอง — เปิดคลุมทั้งช่วงบันไดเผื่อหัวชน
        public const float WELL_X0 = -8.6f, WELL_X1 = -5.5f;
        public const float WELL_Z0 = -0.5f, WELL_Z1 = 6.0f;

        // บันไดขึ้นชั้นสอง: 20 ลูก ลูกตั้ง 0.18 ลูกนอน 0.30 -> ยาว 6 m พอดีช่อง
        public const float STAIR_X = -7.5f;
        public const float STAIR_Z0 = 0f;
        const int STAIR_STEPS = 20;
        const float STAIR_TREAD = 0.30f;

        /// <summary>เส้นแบ่งชั้นสอง: ระเบียงโล่ง x &lt; SUITE_X, ห้องกระจก x &gt; SUITE_X</summary>
        public const float SUITE_X = -2f;

        // หลังคาแผ่นใหญ่ ยื่นคลุม suite กับระเบียงส่วนใน ปล่อยสระเปิดรับฟ้า
        public const float ROOF_X0 = -8f, ROOF_X1 = 17.5f;

        public const string ScenePath = "Assets/MainScenes/Main3_Penthouse/Penthouse.unity";
        public const string DataFolder = "Assets/MainScenes/Main3_Penthouse";
        public const string MatFolder = DataFolder + "/Materials";

        // player ตัวเดียวกับที่ซีนคลับใช้ (Main2_Club) — PlacePlayer จะย่อสเกลให้เท่า Nav.HumanHeight
        const string P_PLAYER = "Assets/Champ&Kichzz/Prefab/Player/player.prefab";
        const string PREV_SCENE = "Main2_Club";

        static Transform _root;
        static Transform _env, _struct, _circ, _dress, _light, _play;

        [MenuItem("Tools/Secrets That Breathe/Build Chapter 3 Penthouse", false, 13)]
        public static void BuildScene() { BuildScene(true); }

        public static void BuildScene(bool askToSave)
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Ch3Penthouse] leave play mode first."); return; }
            if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            K.EnsureFolder(MatFolder);
            K.ResetPlaced();
            Materials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _root = new GameObject("=== CH3 PENTHOUSE ===").transform;
            K.BuildCategories(_root);
            _env = K.Category(_root, K.Cat.Env);
            _struct = K.Category(_root, K.Cat.Structure);
            _circ = K.Category(_root, K.Cat.Circulation);
            _dress = K.Category(_root, K.Cat.Dressing);
            _light = K.Category(_root, K.Cat.Lighting);
            K.Category(_root, K.Cat.Actors);   // ว่างไว้ก่อน รอ NPC ของบทนี้
            _play = K.Category(_root, K.Cat.Gameplay);

            Atmosphere();
            CityBackdrop();
            ShellL1();
            ShellL2();
            Stair();
            InteriorL1();
            TerraceL2();
            SuiteL2();
            Lighting();
            Gameplay();

            GroundProps();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Ch3Penthouse] built -> " + ScenePath);
            Debug.Log(LevelAudit.Format("CH3 PENTHOUSE", LevelAudit.Run(_root, 0.4f, ROOF_Y)));
        }

        /// <summary>Sits every placed prefab exactly on whatever it is standing on.</summary>
        static void GroundProps()
        {
            Physics.SyncTransforms();
            var placed = K.Placed;
            for (int i = 0; i < placed.Count; i++) K.SnapDown(placed[i]);
        }

        // ───────────────────────── helpers ─────────────────────────

        /// <summary>แผ่นพื้น/หลังคาแบบระบุขอบเขต topY = ผิวบน</summary>
        static GameObject Slab(Transform p, string name, float x0, float x1, float z0, float z1,
                               float topY, float thick, string mat, bool collider = true)
        {
            return K.Box(name, p, new Vector3((x0 + x1) * 0.5f, topY - thick * 0.5f, (z0 + z1) * 0.5f),
                         new Vector3(x1 - x0, thick, z1 - z0), mat, default(Vector3), collider);
        }

        /// <summary>ผนัง/กระจกตั้งจากพื้น floorY สูง h ระบุด้วยขอบเขตในผัง</summary>
        static GameObject Panel(Transform p, string name, float x0, float x1, float z0, float z1,
                                float floorY, float h, string mat, bool collider = true)
        {
            return K.Box(name, p, new Vector3((x0 + x1) * 0.5f, floorY + h * 0.5f, (z0 + z1) * 0.5f),
                         new Vector3(Mathf.Max(0.02f, x1 - x0), h, Mathf.Max(0.02f, z1 - z0)),
                         mat, default(Vector3), collider);
        }

        /// <summary>ราวกระจกกันตก: กระจกใส + ราวจับบนสุด</summary>
        static void GlassRail(Transform p, string name, float x0, float x1, float z0, float z1, float floorY)
        {
            var g = K.Group(name, p);
            Panel(g, "Glass", x0, x1, z0, z1, floorY + 0.05f, 1.00f, "Glass");
            Panel(g, "Cap", x0 - 0.03f, x1 + 0.03f, z0 - 0.03f, z1 + 0.03f, floorY + 1.05f, 0.06f, "Alu", false);
            Panel(g, "Shoe", x0 - 0.04f, x1 + 0.04f, z0 - 0.04f, z1 + 0.04f, floorY, 0.08f, "Alu", false);
        }

        static Material LoadOrCreateMat(string file, string shader)
        {
            string path = MatFolder + "/" + file + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var sh = Shader.Find(shader);
                if (sh == null) return null;
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        // ───────────────────────── materials ─────────────────────────
        static void Materials()
        {
            K.UseLibrary(MatFolder, "M_P_");
            K.Mat("Stone", new Color(0.72f, 0.70f, 0.67f), 0.05f, 0.62f);       // พื้นหินขัดในบ้าน
            K.Mat("StoneDark", new Color(0.30f, 0.29f, 0.28f), 0.1f, 0.5f);
            K.Mat("Wood", new Color(0.42f, 0.26f, 0.15f), 0f, 0.42f);           // พื้นไม้ระเบียง
            K.Mat("WoodPale", new Color(0.62f, 0.45f, 0.29f), 0f, 0.40f);
            K.Mat("Marble", new Color(0.86f, 0.85f, 0.83f), 0.1f, 0.86f);
            K.Mat("White", new Color(0.90f, 0.90f, 0.89f), 0f, 0.30f);          // ฝ้า/หลังคา
            K.Mat("Plaster", new Color(0.80f, 0.78f, 0.75f), 0f, 0.25f);
            K.Mat("Concrete", new Color(0.46f, 0.45f, 0.44f), 0f, 0.28f);
            K.Mat("Alu", new Color(0.70f, 0.715f, 0.735f), 0.88f, 0.72f);
            K.Mat("Steel", new Color(0.42f, 0.43f, 0.46f), 0.9f, 0.5f);
            K.Mat("Black", new Color(0.045f, 0.045f, 0.05f), 0.2f, 0.45f);      // กรอบกระจก/ครัว
            K.Mat("Brass", new Color(0.66f, 0.50f, 0.22f), 0.92f, 0.70f);
            K.Mat("Fabric", new Color(0.68f, 0.66f, 0.62f), 0f, 0.22f);         // โซฟา
            K.Mat("FabricWarm", new Color(0.55f, 0.42f, 0.34f), 0f, 0.24f);
            K.Mat("Leather", new Color(0.24f, 0.16f, 0.12f), 0f, 0.36f);
            K.Mat("Greenery", new Color(0.13f, 0.28f, 0.11f), 0f, 0.24f);       // ต้นไม้ในกระบะ
            K.Mat("Soil", new Color(0.14f, 0.11f, 0.09f), 0f, 0.18f);
            K.Mat("PoolTile", new Color(0.16f, 0.52f, 0.55f), 0.05f, 0.88f);
            K.Mat("CityDark", new Color(0.055f, 0.06f, 0.085f), 0.1f, 0.35f);   // ตึกรอบข้าง

            K.MatTransparent("Glass", new Color(0.68f, 0.76f, 0.82f, 0.16f), 0.96f);
            K.MatTransparent("Water", new Color(0.16f, 0.72f, 0.78f, 0.62f), 0.97f);

            K.MatEmissive("PoolGlow", new Color(0.30f, 0.85f, 0.90f), new Color(0.25f, 0.95f, 1f) * 2.4f);
            K.MatEmissive("LedWarm", new Color(0.95f, 0.80f, 0.58f), new Color(1f, 0.78f, 0.48f) * 2.6f);
            K.MatEmissive("CityWindow", new Color(0.85f, 0.76f, 0.55f), new Color(1f, 0.82f, 0.52f) * 1.7f);
            K.MatEmissive("CityWindowCool", new Color(0.60f, 0.72f, 0.88f), new Color(0.62f, 0.78f, 1f) * 1.4f);
        }

        // ───────────────────────── atmosphere ─────────────────────────
        static void Atmosphere()
        {
            var g = K.Group("Atmosphere", _env);

            // ท้องฟ้าพลบค่ำแบบภาพอ้างอิง — ฟ้าอมม่วงข้างบน ส้มตรงขอบฟ้า
            var sky = LoadOrCreateMat("M_P_Skybox_Dusk", "Skybox/Procedural");
            if (sky != null)
            {
                sky.SetFloat("_SunSize", 0.05f);
                sky.SetFloat("_SunSizeConvergence", 4f);
                sky.SetFloat("_AtmosphereThickness", 1.55f);
                sky.SetColor("_SkyTint", new Color(0.40f, 0.38f, 0.62f));
                sky.SetColor("_GroundColor", new Color(0.11f, 0.10f, 0.15f));
                sky.SetFloat("_Exposure", 1.15f);
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.30f, 0.31f, 0.44f);
            RenderSettings.ambientEquatorColor = new Color(0.31f, 0.24f, 0.26f);
            RenderSettings.ambientGroundColor = new Color(0.09f, 0.08f, 0.11f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.36f, 0.31f, 0.40f);
            // เมืองอยู่ไกลเป็นร้อยเมตร ความหนาแน่นต่ำ ๆ พอให้ตึกไกลจางลงตามระยะ
            RenderSettings.fogDensity = 0.0022f;
            RenderSettings.reflectionIntensity = 0.7f;

            var probe = new GameObject("Reflection Probe");
            probe.transform.SetParent(g, false);
            probe.transform.localPosition = new Vector3(0f, L2_Y + 1.5f, 0f);
            var rp = probe.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Realtime;
            rp.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            rp.size = new Vector3(HX * 2f + 8f, ROOF_Y + 4f, HZ * 2f + 8f);
            rp.boxProjection = true;
            rp.resolution = 128;
            rp.intensity = 0.85f;
        }

        /// <summary>
        /// เมืองรอบตัว: เพนต์เฮาส์อยู่ยอดตึก ตึกอื่นจึงอยู่ "ต่ำกว่า" ระดับพื้นเป็นร้อยเมตร
        /// ทั้งหมดไม่มี collider และไม่อยู่ในเส้นทางเดิน เป็นฉากหลังล้วน ๆ
        /// </summary>
        static void CityBackdrop()
        {
            // เมืองต้องอยู่นอก root ของด่าน ไม่งั้น LevelAudit จะกางกริดคลุมทั้งเมือง
            // (หลายร้อยเมตรต่อด้าน = กริดหลักล้านช่อง) แล้วค้างไปเลย
            var g = new GameObject("=== CITY BACKDROP ===").transform;
            const float GROUND_Y = -140f;

            Slab(g, "City_Ground", -900f, 900f, -900f, 900f, GROUND_Y, 4f, "CityDark", false);

            // seed คงที่ เพื่อให้ build ซ้ำแล้วเมืองหน้าตาเหมือนเดิมทุกครั้ง
            var rng = new System.Random(30303);
            float Rand(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

            var towers = K.Group("Towers", g);
            for (int i = 0; i < 80; i++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = Rand(52f, 340f);
                float x = Mathf.Cos(ang) * dist;
                float z = Mathf.Sin(ang) * dist;

                // ตึกใกล้เตี้ยกว่า เพื่อไม่บังวิวจากระเบียง
                float near = Mathf.InverseLerp(52f, 340f, dist);
                float top = Rand(-95f, Mathf.Lerp(-40f, 25f, near));
                float h = top - GROUND_Y;
                float w = Rand(9f, 26f);
                float d = Rand(9f, 26f);

                var t = K.Box("Tower_" + i, towers, new Vector3(x, GROUND_Y + h * 0.5f, z),
                              new Vector3(w, h, d), "CityDark", new Vector3(0f, Rand(0f, 90f), 0f), false);

                // แถบหน้าต่างเรืองแสง ไล่ขึ้นไปตามความสูง
                int floors = Mathf.Clamp(Mathf.FloorToInt(h / 8f), 2, 10);
                for (int f = 0; f < floors; f++)
                {
                    float y = GROUND_Y + 6f + f * (h - 8f) / floors;
                    string mat = (rng.Next(0, 4) == 0) ? "CityWindowCool" : "CityWindow";
                    K.Box("Win_" + i + "_" + f, t.transform,
                          new Vector3(0f, (y - (GROUND_Y + h * 0.5f)) / h, 0f),
                          new Vector3(1.005f, 2.6f / h, 1.005f), mat, default(Vector3), false);
                }
            }

            // ถนนเรืองแสงตัดผ่านเมือง เห็นเป็นเส้นไฟจากมุมสูงเหมือนภาพอ้างอิง
            var roads = K.Group("Roads", g);
            for (int i = 0; i < 7; i++)
            {
                float o = -300f + i * 100f;
                K.Box("Road_NS_" + i, roads, new Vector3(o, GROUND_Y + 2.2f, 0f),
                      new Vector3(7f, 0.4f, 1400f), "LedWarm", default(Vector3), false);
                K.Box("Road_EW_" + i, roads, new Vector3(0f, GROUND_Y + 2.2f, o),
                      new Vector3(1400f, 0.4f, 7f), "LedWarm", default(Vector3), false);
            }
        }

        // ───────────────────────── ชั้นล่าง: เปลือกอาคาร ─────────────────────────
        static void ShellL1()
        {
            var g = K.Group("Shell_L1", _struct);

            Slab(g, "Floor_L1", L1_X0, HX, -HZ, HZ, 0f, 0.40f, "Stone");

            // ผนังกระจกรอบตัวชั้นล่าง — เต็มความสูง กันตกด้วย collider ของกระจกเอง
            var mull = K.Group("Mullions_L1", _dress);
            CurtainWall(g, mull, "Glass_L1_N", L1_X0, HX, HZ - 0.08f, HZ, 0f, L2_Y);
            CurtainWall(g, mull, "Glass_L1_S", L1_X0, HX, -HZ, -HZ + 0.08f, 0f, L2_Y);
            CurtainWall(g, mull, "Glass_L1_E", HX - 0.08f, HX, -HZ, HZ, 0f, L2_Y);
            CurtainWall(g, mull, "Glass_L1_W", L1_X0, L1_X0 + 0.08f, -HZ, HZ, 0f, L2_Y);
        }

        /// <summary>ผนังกระจกเต็มความสูงพร้อมเสาเอ็นอลูมิเนียมทุก ๆ 3 m</summary>
        static void CurtainWall(Transform glassParent, Transform mullionParent, string name,
                                float x0, float x1, float z0, float z1, float floorY, float h)
        {
            Panel(glassParent, name, x0, x1, z0, z1, floorY, h, "Glass");

            bool alongX = (x1 - x0) > (z1 - z0);
            float len = alongX ? (x1 - x0) : (z1 - z0);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 3f));
            var g = K.Group(name + "_Frame", mullionParent);

            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                if (alongX)
                {
                    float x = Mathf.Lerp(x0, x1, t);
                    Panel(g, "Mullion_" + i, x - 0.05f, x + 0.05f, z0 - 0.02f, z1 + 0.02f, floorY, h, "Black", false);
                }
                else
                {
                    float z = Mathf.Lerp(z0, z1, t);
                    Panel(g, "Mullion_" + i, x0 - 0.02f, x1 + 0.02f, z - 0.05f, z + 0.05f, floorY, h, "Black", false);
                }
            }
            // รางบน-ล่าง
            Panel(g, "Head", x0 - 0.03f, x1 + 0.03f, z0 - 0.03f, z1 + 0.03f, floorY + h - 0.12f, 0.12f, "Black", false);
            Panel(g, "Sill", x0 - 0.03f, x1 + 0.03f, z0 - 0.03f, z1 + 0.03f, floorY, 0.10f, "Alu", false);
        }

        // ───────────────────────── ชั้นสอง: พื้น + หลังคา ─────────────────────────
        static void ShellL2()
        {
            var g = K.Group("Shell_L2", _struct);

            // พื้นชั้นสองแบ่งเป็นแถบตาม x เพื่อเว้นช่องสระกับช่องบันได
            // แถบสระ: เว้น z ช่วงสระไว้
            Slab(g, "Slab_Pool_S", POOL_X0, POOL_X1, -HZ, POOL_Z0, L2_Y, 0.30f, "Wood");
            Slab(g, "Slab_Pool_N", POOL_X0, POOL_X1, POOL_Z1, HZ, L2_Y, 0.30f, "Wood");
            // แถบทางเดินริมสระ (กว้าง 2 m > PathClear) เดินอ้อมสระได้ตลอดแนว
            Slab(g, "Slab_Walk", POOL_X1, WELL_X0, -HZ, HZ, L2_Y, 0.30f, "Wood");
            // แถบบันได: เว้นช่องบันไดไว้
            Slab(g, "Slab_Stair_S", WELL_X0, WELL_X1, -HZ, WELL_Z0, L2_Y, 0.30f, "Wood");
            Slab(g, "Slab_Stair_N", WELL_X0, WELL_X1, WELL_Z1, HZ, L2_Y, 0.30f, "Wood");
            // แถบตะวันออก: เต็มผืน (ระเบียงส่วนใน + suite)
            Slab(g, "Slab_East", WELL_X1, HX, -HZ, HZ, L2_Y, 0.30f, "Wood");

            // เปลี่ยนผิวเป็นหินขัดเฉพาะในห้อง suite ให้อ่านออกว่าเข้าในบ้านแล้ว
            Slab(_dress, "SuiteFloor_Finish", SUITE_X, HX - 0.08f, -HZ + 0.08f, HZ - 0.08f,
                 L2_Y + 0.012f, 0.024f, "Marble", false);

            // หลังคาแผ่นใหญ่ ยื่นคลุม suite และระเบียงส่วนใน ปล่อยสระเปิดรับฟ้า
            Slab(g, "Roof", ROOF_X0, ROOF_X1, -HZ - 1f, HZ + 1f, ROOF_Y + 0.50f, 0.50f, "White");
            // ครีบขอบหลังคาบางลง ให้เงาคมแบบภาพอ้างอิง
            Slab(_dress, "Roof_Fascia_W", ROOF_X0 - 0.6f, ROOF_X0, -HZ - 1f, HZ + 1f, ROOF_Y + 0.34f, 0.34f, "White", false);
            Slab(_dress, "Roof_Fascia_N", ROOF_X0 - 0.6f, ROOF_X1, HZ + 1f, HZ + 1.6f, ROOF_Y + 0.34f, 0.34f, "White", false);
            Slab(_dress, "Roof_Fascia_S", ROOF_X0 - 0.6f, ROOF_X1, -HZ - 1.6f, -HZ - 1f, ROOF_Y + 0.34f, 0.34f, "White", false);

            // เสารับหลังคาฝั่งระเบียง — เลี่ยงช่องบันได (z -0.5..6.0) ไม่งั้นเสาจะลอยกลางช่อง
            // และไปยืนขวางหัวบันไดพอดี
            float[] colZ = { -9f, -3f, 9f };
            for (int i = 0; i < colZ.Length; i++)
                K.Box("Roof_Col_" + i, g, new Vector3(ROOF_X0 + 0.35f, L2_Y + L2_H * 0.5f, colZ[i]),
                      new Vector3(0.30f, L2_H, 0.30f), "White");
        }

        // ───────────────────────── บันไดขึ้นชั้นสอง ─────────────────────────
        static void Stair()
        {
            var g = K.Group("Stair_L1_to_L2", _circ);
            float rise = L2_Y / STAIR_STEPS;                 // 0.18 m
            float w = K.Nav.DoorClear;                       // 1.80 m

            for (int i = 0; i < STAIR_STEPS; i++)
            {
                float z0 = STAIR_Z0 + i * STAIR_TREAD;
                // ลูกตั้งซ้อนกันเป็นก้อนตัน กันผู้เล่นตกร่องระหว่างขั้น
                Slab(g, "Step_" + i, STAIR_X - w * 0.5f, STAIR_X + w * 0.5f, z0, z0 + STAIR_TREAD,
                     (i + 1) * rise, (i + 1) * rise, "Marble");
            }

            // ราวกันตกสองข้างบันได + ราวรอบช่องบันไดชั้นบน
            float run = STAIR_STEPS * STAIR_TREAD;
            StairRail(g, "Rail_W", STAIR_X - w * 0.5f, run);
            StairRail(g, "Rail_E", STAIR_X + w * 0.5f, run);

            var edge = K.Group("Well_Edge", _circ);
            GlassRail(edge, "Well_W", WELL_X0, WELL_X0 + 0.05f, WELL_Z0, WELL_Z1, L2_Y);
            GlassRail(edge, "Well_E", WELL_X1 - 0.05f, WELL_X1, WELL_Z0, WELL_Z1, L2_Y);
            GlassRail(edge, "Well_S", WELL_X0, WELL_X1, WELL_Z0, WELL_Z0 + 0.05f, L2_Y);

            K.NeonStrip("Stair_Glow", _light,
                        new Vector3(STAIR_X, L2_Y * 0.5f, STAIR_Z0 + run * 0.5f),
                        new Vector3(w - 0.1f, 0.05f, 0.05f), "LedWarm", new Color(1f, 0.8f, 0.5f), 1.6f, 7f);

            K.Marker("NAV_StairFoot", _play, new Vector3(STAIR_X, 0f, STAIR_Z0 - 1.4f));
            K.Marker("NAV_StairTop", _play, new Vector3(STAIR_X, L2_Y, WELL_Z1 + 1.4f));
        }

        static void StairRail(Transform parent, string name, float x, float run)
        {
            var g = K.Group(name, parent);
            int posts = 7;
            for (int i = 0; i <= posts; i++)
            {
                float t = i / (float)posts;
                float z = STAIR_Z0 + run * t;
                float y = L2_Y * t;
                K.Box("Post_" + i, g, new Vector3(x, y + 0.5f, z), new Vector3(0.05f, 1.0f, 0.05f), "Alu", default(Vector3), false);
            }
            float angle = Mathf.Atan2(L2_Y, run) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt(run * run + L2_Y * L2_Y);
            K.Box("Handrail", g, new Vector3(x, L2_Y * 0.5f + 1.0f, STAIR_Z0 + run * 0.5f),
                  new Vector3(0.06f, 0.06f, len), "Alu", new Vector3(-angle, 0f, 0f), false);
            K.Box("Infill", g, new Vector3(x, L2_Y * 0.5f + 0.52f, STAIR_Z0 + run * 0.5f),
                  new Vector3(0.02f, 0.88f, len), "Glass", new Vector3(-angle, 0f, 0f), false);
        }

        // ───────────────────────── ชั้นล่าง: ห้องต่าง ๆ ─────────────────────────
        static void InteriorL1()
        {
            var g = K.Group("Rooms_L1", _struct);
            const float WT = 0.25f;                 // ความหนาผนัง
            const float CORR_Z = -2f;               // เส้นแบ่งโถงทางเดินกับห้องนอนฝั่งใต้
            const float BED_X = -2f, LOBBY_X = 8f;  // เส้นแบ่งห้องฝั่งใต้

            // ฝ้าชั้นล่าง (ใต้พื้นชั้นสอง) — ทาขาวให้ห้องสว่าง
            // เว้นช่องบันไดไว้เหมือนแผ่นพื้น ไม่งั้นจะมีฝ้าขาวพาดขวางช่องโล่งเวลามองขึ้นไป
            var ceil = K.Group("Ceiling_L1", _dress);
            float cy = L2_Y - 0.31f;
            Slab(ceil, "Ceil_W", L1_X0, WELL_X0, -HZ, HZ, cy, 0.04f, "White", false);
            Slab(ceil, "Ceil_Well_S", WELL_X0, WELL_X1, -HZ, WELL_Z0, cy, 0.04f, "White", false);
            Slab(ceil, "Ceil_Well_N", WELL_X0, WELL_X1, WELL_Z1, HZ, cy, 0.04f, "White", false);
            Slab(ceil, "Ceil_E", WELL_X1, HX, -HZ, HZ, cy, 0.04f, "White", false);

            // ผนังกั้นห้องฝั่งใต้ เจาะประตูให้เดินเข้าได้ทุกห้อง
            K.WallWithOpening("Wall_Study", g, new Vector3((L1_X0 + BED_X) * 0.5f, 0f, CORR_Z),
                              BED_X - L1_X0, L2_Y, WT, "Plaster", 0f, K.Nav.DoorClear, K.Nav.DoorHeight);
            K.WallWithOpening("Wall_Bedroom", g, new Vector3((BED_X + LOBBY_X) * 0.5f, 0f, CORR_Z),
                              LOBBY_X - BED_X, L2_Y, WT, "Plaster", 0f, K.Nav.DoorClear, K.Nav.DoorHeight);
            K.WallWithOpening("Wall_Lobby", g, new Vector3((LOBBY_X + HX) * 0.5f, 0f, CORR_Z),
                              HX - LOBBY_X, L2_Y, WT, "Plaster", 0f, K.Nav.DoorClear, K.Nav.DoorHeight);

            Panel(g, "Wall_Study_Bed", BED_X - WT * 0.5f, BED_X + WT * 0.5f, -HZ, CORR_Z, 0f, L2_Y, "Plaster");
            Panel(g, "Wall_Bed_Lobby", LOBBY_X - WT * 0.5f, LOBBY_X + WT * 0.5f, -HZ, CORR_Z, 0f, L2_Y, "Plaster");

            // ── LIVING (โซนเหนือ ฝั่งกลาง) ──
            // วางเลี่ยงแนวบันได x -8.4..-6.6 ไว้ทั้งหมด ฝั่งตะวันตกของบันไดปล่อยโล่งเป็นทางเดิน
            var lv = K.Group("ZONE_Living", _dress);
            Slab(lv, "Rug", -5f, 2.5f, 3.5f, 10f, 0.02f, 0.04f, "FabricWarm", false);
            Sofa(lv, "Sofa_Main", new Vector3(-1.5f, 0f, 4.6f), 0f, 3.4f);
            Sofa(lv, "Sofa_Side", new Vector3(-4.6f, 0f, 7.5f), 90f, 2.6f);
            K.Box("CoffeeTable", lv, new Vector3(-1.5f, 0.18f, 7.0f), new Vector3(1.9f, 0.36f, 0.9f), "WoodPale");
            K.Box("CoffeeTable_Top", lv, new Vector3(-1.5f, 0.38f, 7.0f), new Vector3(2.0f, 0.05f, 1.0f), "Marble", default(Vector3), false);
            K.Box("Sideboard", lv, new Vector3(2.4f, 0.32f, 10.0f), new Vector3(0.5f, 0.64f, 3.0f), "Black");
            Planter(lv, "Plant_Living", new Vector3(-4.8f, 0f, 11.0f), 1.0f);
            K.Marker("NAV_Living", _play, new Vector3(-1.5f, 0f, 7.0f));

            // ── DINING / KITCHEN (ตะวันออกเฉียงเหนือ) ──
            var dn = K.Group("ZONE_Dining", _dress);
            K.Box("DiningTable", dn, new Vector3(6.5f, 0.36f, 6.5f), new Vector3(2.6f, 0.72f, 1.2f), "WoodPale");
            K.Box("DiningTable_Top", dn, new Vector3(6.5f, 0.75f, 6.5f), new Vector3(2.8f, 0.06f, 1.4f), "Marble", default(Vector3), false);
            for (int i = 0; i < 3; i++)
            {
                Chair(dn, "Chair_N_" + i, new Vector3(5.4f + i * 1.1f, 0f, 7.5f), 180f);
                Chair(dn, "Chair_S_" + i, new Vector3(5.4f + i * 1.1f, 0f, 5.5f), 0f);
            }
            // ครัวชิดผนังตะวันออก + เกาะกลาง เว้นทางเดินระหว่างกันเกิน PathClear
            K.Box("Kitchen_Run", dn, new Vector3(14.6f, 0.45f, 6f), new Vector3(0.7f, 0.90f, 7f), "Black");
            K.Box("Kitchen_Top", dn, new Vector3(14.6f, 0.92f, 6f), new Vector3(0.8f, 0.05f, 7.1f), "Marble", default(Vector3), false);
            K.Box("Kitchen_Upper", dn, new Vector3(14.8f, 1.95f, 6f), new Vector3(0.45f, 0.80f, 6f), "Black", default(Vector3), false);
            K.Box("Island", dn, new Vector3(11.6f, 0.45f, 6f), new Vector3(1.1f, 0.90f, 4.2f), "Black");
            K.Box("Island_Top", dn, new Vector3(11.6f, 0.92f, 6f), new Vector3(1.3f, 0.05f, 4.4f), "Marble", default(Vector3), false);
            for (int i = 0; i < 3; i++)
                K.Cyl("Stool_" + i, dn, new Vector3(10.6f, 0.33f, 4.4f + i * 1.6f), 0.38f, 0.66f, "Leather", default(Vector3), true);
            K.Marker("NAV_Dining", _play, new Vector3(9.3f, 0f, 6.5f));

            // ── STUDY (ใต้ ฝั่งตะวันตก) — ห้องทำงาน จุดวางหลักฐานของบท ──
            var st = K.Group("ZONE_Study", _dress);
            K.Box("Desk", st, new Vector3(-8.5f, 0.37f, -9f), new Vector3(2.4f, 0.74f, 1.0f), "WoodPale");
            K.Box("Desk_Top", st, new Vector3(-8.5f, 0.76f, -9f), new Vector3(2.6f, 0.05f, 1.2f), "Black", default(Vector3), false);
            Chair(st, "Desk_Chair", new Vector3(-8.5f, 0f, -7.6f), 180f);
            K.Box("Bookshelf", st, new Vector3(-11.1f, 1.1f, -6f), new Vector3(0.45f, 2.2f, 4.4f), "WoodPale");
            for (int i = 0; i < 4; i++)
                K.Box("Shelf_Led_" + i, st, new Vector3(-10.85f, 0.55f + i * 0.52f, -6f), new Vector3(0.06f, 0.03f, 4.1f), "LedWarm", default(Vector3), false);
            K.Box("Safe", st, new Vector3(-3.1f, 0.4f, -10.6f), new Vector3(0.8f, 0.8f, 0.7f), "Steel");
            K.Marker("NAV_Study", _play, new Vector3(-8.5f, 0f, -7.2f));
            K.Marker("INTERACT_Desk", _play, new Vector3(-8.5f, 0.8f, -8.2f));
            K.Marker("INTERACT_Safe", _play, new Vector3(-3.1f, 0.5f, -9.9f));

            // ── MASTER BEDROOM (ใต้ กลาง) ──
            var bd = K.Group("ZONE_Bedroom", _dress);
            K.Box("Bed", bd, new Vector3(3f, 0.28f, -9.4f), new Vector3(2.1f, 0.56f, 2.2f), "WoodPale");
            K.Box("Mattress", bd, new Vector3(3f, 0.68f, -9.4f), new Vector3(2.0f, 0.26f, 2.1f), "Fabric", default(Vector3), false);
            K.Box("Headboard", bd, new Vector3(3f, 0.95f, -10.6f), new Vector3(2.4f, 1.3f, 0.16f), "FabricWarm", default(Vector3), false);
            for (int i = 0; i < 2; i++)
                K.Box("Nightstand_" + i, bd, new Vector3(1.5f + i * 3f, 0.24f, -10.3f), new Vector3(0.5f, 0.48f, 0.45f), "Black");
            K.Box("Wardrobe", bd, new Vector3(7.4f, 1.2f, -7.6f), new Vector3(0.6f, 2.4f, 3.0f), "WoodPale");
            K.Marker("NAV_Bedroom", _play, new Vector3(4.8f, 0f, -7.6f));

            // ── LIFT LOBBY (ใต้ ฝั่งตะวันออก) — ทางเข้าจากคลับ ──
            var lb = K.Group("ZONE_LiftLobby", _dress);
            Slab(lb, "Lobby_Floor_Finish", LOBBY_X + 0.3f, HX - 0.2f, -HZ + 0.3f, CORR_Z - 0.3f, 0.015f, 0.03f, "StoneDark", false);
            K.Box("Console", lb, new Vector3(9.4f, 0.42f, -6.5f), new Vector3(0.45f, 0.84f, 1.8f), "Black");
            Planter(lb, "Plant_Lobby", new Vector3(9.6f, 0f, -10.4f), 0.9f);
            K.Marker("NAV_LiftLobby", _play, new Vector3(12f, 0f, -7f));
        }

        static void Sofa(Transform parent, string name, Vector3 p, float yaw, float len)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Base", g, new Vector3(0f, 0.19f, 0f), new Vector3(len, 0.38f, 0.95f), "Fabric");
            K.Box("Seat", g, new Vector3(0f, 0.43f, 0.05f), new Vector3(len - 0.1f, 0.14f, 0.85f), "Fabric", default(Vector3), false);
            K.Box("Back", g, new Vector3(0f, 0.62f, -0.40f), new Vector3(len, 0.60f, 0.20f), "Fabric", default(Vector3), false);
            K.Box("Arm_L", g, new Vector3(-len * 0.5f + 0.09f, 0.52f, 0f), new Vector3(0.18f, 0.30f, 0.95f), "Fabric", default(Vector3), false);
            K.Box("Arm_R", g, new Vector3(len * 0.5f - 0.09f, 0.52f, 0f), new Vector3(0.18f, 0.30f, 0.95f), "Fabric", default(Vector3), false);
        }

        static void Chair(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Seat", g, new Vector3(0f, 0.44f, 0f), new Vector3(0.46f, 0.07f, 0.46f), "WoodPale");
            K.Box("Back", g, new Vector3(0f, 0.72f, -0.21f), new Vector3(0.46f, 0.50f, 0.06f), "WoodPale", default(Vector3), false);
            for (int i = 0; i < 4; i++)
            {
                float sx = (i % 2 == 0) ? -0.19f : 0.19f;
                float sz = (i < 2) ? -0.19f : 0.19f;
                K.Box("Leg_" + i, g, new Vector3(sx, 0.22f, sz), new Vector3(0.05f, 0.44f, 0.05f), "WoodPale", default(Vector3), false);
            }
        }

        /// <summary>กระบะต้นไม้ — ใช้ทั้งในบ้านและบนระเบียง</summary>
        static void Planter(Transform parent, string name, Vector3 p, float size)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Box("Box", g, new Vector3(0f, 0.28f, 0f), new Vector3(size, 0.56f, size), "Concrete");
            K.Box("Soil", g, new Vector3(0f, 0.56f, 0f), new Vector3(size - 0.12f, 0.06f, size - 0.12f), "Soil", default(Vector3), false);
            K.Cyl("Trunk", g, new Vector3(0f, 0.95f, 0f), 0.10f, 0.8f, "Wood");
            K.Sphere("Crown", g, new Vector3(0f, 1.7f, 0f), size * 1.15f, "Greenery");
            K.Sphere("Crown_B", g, new Vector3(size * 0.22f, 1.35f, -size * 0.18f), size * 0.8f, "Greenery");
        }

        /// <summary>แนวพุ่มไม้เตี้ยตามขอบระเบียง เหมือนแถบเขียวในภาพอ้างอิง</summary>
        static void Hedge(Transform parent, string name, float x0, float x1, float z0, float z1, float floorY)
        {
            var g = K.Group(name, parent);
            Panel(g, "Trough", x0, x1, z0, z1, floorY, 0.45f, "Concrete");
            Panel(g, "Foliage", x0 + 0.05f, x1 - 0.05f, z0 + 0.05f, z1 - 0.05f, floorY + 0.42f, 0.55f, "Greenery", false);
        }

        // ───────────────────────── ชั้นสอง: ระเบียง + สระ ─────────────────────────
        static void TerraceL2()
        {
            var g = K.Group("ZONE_Terrace", _dress);

            Pool();

            // ราวกระจกรอบระเบียงที่เปิดโล่ง — กันผู้เล่นตกตึก
            var rail = K.Group("Terrace_Rails", _circ);
            GlassRail(rail, "Rail_N", POOL_X0, SUITE_X, HZ - 0.06f, HZ, L2_Y);
            GlassRail(rail, "Rail_S", POOL_X0, SUITE_X, -HZ, -HZ + 0.06f, L2_Y);
            GlassRail(rail, "Rail_W_S", POOL_X0, POOL_X0 + 0.06f, -HZ, POOL_Z0, L2_Y);
            GlassRail(rail, "Rail_W_N", POOL_X0, POOL_X0 + 0.06f, POOL_Z1, HZ, L2_Y);
            // ขอบ infinity ของสระ กระจกเตี้ยกว่าเพื่อไม่บังวิว
            Panel(rail, "Rail_Pool_Edge", POOL_X0 - 0.06f, POOL_X0, POOL_Z0, POOL_Z1, L2_Y + 0.05f, 0.65f, "Glass");

            // เตียงอาบแดดหันออกวิว — ชิดขอบสระ เหลือเลนเดินด้านหลังกว้าง ~2.1 m
            // (ถ้าวางกลางแถบ เตียงจะกินความกว้างจนผู้เล่นเดินผ่านไม่ได้)
            for (int i = 0; i < 3; i++)
                Lounger(g, "Lounger_" + i, new Vector3(-11.05f, L2_Y, -3.4f + i * 3.4f), -90f);

            // แถบเขียวคั่นระหว่างระเบียงกับ suite เหมือนภาพอ้างอิง
            Hedge(g, "Hedge_N", -9.2f, SUITE_X - 0.4f, HZ - 1.5f, HZ - 0.55f, L2_Y);
            Hedge(g, "Hedge_S", -9.2f, SUITE_X - 0.4f, -HZ + 0.55f, -HZ + 1.5f, L2_Y);
            Planter(g, "Planter_A", new Vector3(-4.6f, L2_Y, 9.2f), 1.0f);
            Planter(g, "Planter_B", new Vector3(-4.6f, L2_Y, -9.2f), 1.0f);

            // ชุดโซฟา outdoor ใต้ชายคา — วางฝั่งใต้ ไม่ขวางบานเลื่อนที่ z 1.4..4.6
            // และอยู่ทางตะวันออกของช่องบันได (x > -5.5) จึงไม่ลอยเหนือช่องว่าง
            Sofa(g, "Terrace_Sofa", new Vector3(-4.6f, L2_Y, -3.0f), 90f, 2.6f);
            K.Box("Terrace_Table", g, new Vector3(-3.4f, L2_Y + 0.16f, -3.0f), new Vector3(0.8f, 0.32f, 1.4f), "WoodPale");

            // วางเหนือช่องบันได (z > 6) ไม่งั้น marker จะไปตกอยู่กลางช่องว่าง
            K.Marker("NAV_Terrace", _play, new Vector3(-6.8f, L2_Y, 8.5f));
            K.Marker("NAV_PoolDeck", _play, new Vector3(-10.4f, L2_Y, 0f));
        }

        static void Pool()
        {
            var g = K.Group("Pool", _struct);
            float basinTop = L2_Y;
            float basinFloor = L2_Y - POOL_DEPTH;          // 2.40
            const float WT = 0.25f;

            // ก้นสระ + ผนังสระสี่ด้าน (ก้นหนาหน่อย เป็นท้องแผ่นที่มองเห็นจากนอกอาคาร)
            Slab(g, "Basin_Floor", POOL_X0, POOL_X1, POOL_Z0, POOL_Z1, basinFloor, 0.35f, "PoolTile");
            Panel(g, "Basin_W", POOL_X0, POOL_X0 + WT, POOL_Z0, POOL_Z1, basinFloor, POOL_DEPTH, "PoolTile");
            Panel(g, "Basin_E", POOL_X1 - WT, POOL_X1, POOL_Z0, POOL_Z1, basinFloor, POOL_DEPTH, "PoolTile");
            Panel(g, "Basin_S", POOL_X0, POOL_X1, POOL_Z0, POOL_Z0 + WT, basinFloor, POOL_DEPTH, "PoolTile");
            Panel(g, "Basin_N", POOL_X0, POOL_X1, POOL_Z1 - WT, POOL_Z1, basinFloor, POOL_DEPTH, "PoolTile");

            // บันไดลงสระมุมตะวันออกเฉียงใต้ ลูกตั้ง 0.24 m (ต่ำกว่า stepOffset) ขึ้นลงได้เอง
            const int steps = 5;
            float rise = POOL_DEPTH / steps;
            for (int i = 0; i < steps; i++)
            {
                float x0 = POOL_X1 - WT - (i + 1) * 0.34f;
                Slab(g, "PoolStep_" + i, x0, POOL_X1 - WT, POOL_Z0 + WT, POOL_Z0 + WT + 1.8f,
                     basinFloor + (steps - i) * rise, (steps - i) * rise, "PoolTile");
            }

            // ผิวน้ำ ต่ำกว่าขอบสระเล็กน้อย ไม่มี collider เดินลุยลงไปได้
            // ความหนาไล่ลงถึงก้นสระพอดี ไม่ให้เห็นช่องว่างระหว่างน้ำกับพื้นสระ
            Slab(_dress, "Water", POOL_X0 + WT, POOL_X1 - WT, POOL_Z0 + WT, POOL_Z1 - WT,
                 basinTop - 0.10f, POOL_DEPTH - 0.10f, "Water", false);

            // ไฟใต้น้ำ ทำให้สระเรืองฟ้าแบบภาพอ้างอิง
            for (int i = 0; i < 5; i++)
            {
                float z = POOL_Z0 + 1.6f + i * 2.7f;
                K.Box("PoolLight_" + i, _dress, new Vector3(POOL_X1 - WT - 0.05f, basinFloor + 0.55f, z),
                      new Vector3(0.08f, 0.22f, 0.5f), "PoolGlow", default(Vector3), false);
                K.AddLight(_light, "PoolLamp_" + i, new Vector3(POOL_X1 - 1.2f, basinFloor + 0.7f, z),
                           Vector3.zero, LightType.Point, new Color(0.3f, 0.9f, 1f), 2.2f, 6f);
            }

            K.Marker("NAV_PoolSteps", _play, new Vector3(POOL_X1 - 1.2f, L2_Y, POOL_Z0 + 1.2f));
        }

        static void Lounger(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Frame", g, new Vector3(0f, 0.19f, 0f), new Vector3(1.95f, 0.38f, 0.72f), "WoodPale");
            K.Box("Pad", g, new Vector3(0f, 0.43f, 0f), new Vector3(1.85f, 0.12f, 0.66f), "Fabric", default(Vector3), false);
            K.Box("Backrest", g, new Vector3(-0.72f, 0.63f, 0f), new Vector3(0.62f, 0.12f, 0.66f),
                  "Fabric", new Vector3(0f, 0f, 38f), false);
            K.Box("Towel", g, new Vector3(0.35f, 0.51f, 0f), new Vector3(0.5f, 0.05f, 0.6f), "White", default(Vector3), false);
        }

        // ───────────────────────── ชั้นสอง: ห้องกระจก ─────────────────────────
        static void SuiteL2()
        {
            var g = K.Group("Shell_Suite_L2", _struct);
            var mull = K.Group("Mullions_L2", _dress);

            // ผนังกระจกด้านที่ติดระเบียง เจาะบานเลื่อนกว้าง ๆ ให้เดินออกไปสระได้
            // yaw 90 ทำให้ local +X ทาบกับ world -Z ตำแหน่งช่องเปิดจึงกลับเครื่องหมาย
            const float DOOR_Z = 3f;
            const float DOOR_W = 3.2f;
            K.WallWithOpening("Glass_Suite_W", g, new Vector3(SUITE_X, L2_Y, 0f), HZ * 2f, L2_H, 0.10f,
                              "Glass", -DOOR_Z, DOOR_W, K.Nav.DoorHeight + 0.4f, 90f);
            K.Marker("DOOR_TerraceToSuite", _circ, new Vector3(SUITE_X, L2_Y, DOOR_Z));

            // อีกสามด้านเป็นผนังกระจกเต็มความสูง วิวเมืองรอบทิศ
            CurtainWall(g, mull, "Glass_Suite_N", SUITE_X, HX, HZ - 0.08f, HZ, L2_Y, L2_H);
            CurtainWall(g, mull, "Glass_Suite_S", SUITE_X, HX, -HZ, -HZ + 0.08f, L2_Y, L2_H);
            CurtainWall(g, mull, "Glass_Suite_E", HX - 0.08f, HX, -HZ, HZ, L2_Y, L2_H);

            // ── ห้องนั่งเล่นชั้นบน ──
            var lv = K.Group("ZONE_SuiteLounge", _dress);
            Slab(lv, "Rug", 0.5f, 7.5f, 1f, 8f, L2_Y + 0.03f, 0.04f, "FabricWarm", false);
            Sofa(lv, "Suite_Sofa", new Vector3(4f, L2_Y, 2.2f), 0f, 3.0f);
            Sofa(lv, "Suite_Sofa_B", new Vector3(4f, L2_Y, 6.8f), 180f, 3.0f);
            K.Box("Suite_Table", lv, new Vector3(4f, L2_Y + 0.18f, 4.5f), new Vector3(1.6f, 0.36f, 0.9f), "WoodPale");
            K.Box("Suite_Table_Top", lv, new Vector3(4f, L2_Y + 0.38f, 4.5f), new Vector3(1.7f, 0.05f, 1.0f), "Marble", default(Vector3), false);
            Planter(lv, "Suite_Plant", new Vector3(0.6f, L2_Y, 10.2f), 1.0f);

            // บาร์เล็ก ๆ ชิดผนังตะวันออก
            K.Box("Bar", lv, new Vector3(14.4f, L2_Y + 0.5f, 2f), new Vector3(0.8f, 1.0f, 4.0f), "Black");
            K.Box("Bar_Top", lv, new Vector3(14.4f, L2_Y + 1.02f, 2f), new Vector3(0.95f, 0.05f, 4.2f), "Marble", default(Vector3), false);
            K.Box("Bar_Led", lv, new Vector3(13.98f, L2_Y + 0.28f, 2f), new Vector3(0.04f, 0.05f, 3.8f), "LedWarm", default(Vector3), false);
            for (int i = 0; i < 3; i++)
                K.Cyl("Bar_Stool_" + i, lv, new Vector3(13.2f, L2_Y + 0.33f, 0.6f + i * 1.4f), 0.38f, 0.66f, "Leather", default(Vector3), true);

            // ── ห้องนอนชั้นบน (ฝั่งใต้) ──
            var bd = K.Group("ZONE_SuiteBedroom", _dress);
            // ผนังกั้นห้องนอน เจาะประตูไว้ที่ x = 3 (openingCentreX วัดจากจุดกึ่งกลางผนัง)
            K.WallWithOpening("Wall_SuiteBed", g, new Vector3((SUITE_X + HX) * 0.5f, L2_Y, -3.775f),
                              HX - SUITE_X, L2_H, 0.25f, "Plaster", -4f, K.Nav.DoorClear, K.Nav.DoorHeight);

            K.Box("Bed", bd, new Vector3(6f, L2_Y + 0.28f, -8.5f), new Vector3(2.2f, 0.56f, 2.3f), "WoodPale");
            K.Box("Mattress", bd, new Vector3(6f, L2_Y + 0.68f, -8.5f), new Vector3(2.1f, 0.26f, 2.2f), "Fabric", default(Vector3), false);
            K.Box("Headboard", bd, new Vector3(6f, L2_Y + 0.98f, -9.75f), new Vector3(2.5f, 1.35f, 0.16f), "FabricWarm", default(Vector3), false);
            K.Box("Bench", bd, new Vector3(6f, L2_Y + 0.22f, -7.1f), new Vector3(2.0f, 0.44f, 0.5f), "Leather");
            for (int i = 0; i < 2; i++)
                K.Box("Nightstand_" + i, bd, new Vector3(4.4f + i * 3.2f, L2_Y + 0.24f, -9.4f), new Vector3(0.5f, 0.48f, 0.45f), "Black");
            K.Box("Dresser", bd, new Vector3(13.4f, L2_Y + 0.35f, -7.5f), new Vector3(0.55f, 0.70f, 3.0f), "WoodPale");

            K.Marker("NAV_SuiteLounge", _play, new Vector3(6f, L2_Y, 4.5f));
            K.Marker("NAV_SuiteBedroom", _play, new Vector3(8.5f, L2_Y, -7f));
        }

        // ───────────────────────── lighting ─────────────────────────
        static void Lighting()
        {
            var g = K.Group("KeyLights", _light);

            // ดวงอาทิตย์ตกต่ำใกล้ขอบฟ้า มาจากทางตะวันตก (ฝั่งสระ) ให้เงายาวพาดเข้าห้อง
            var sun = K.AddLight(g, "Sun_Dusk", new Vector3(0f, 30f, 0f), new Vector3(7f, 118f, 0f),
                                 LightType.Directional, new Color(1f, 0.68f, 0.44f), 1.5f, 0f, 60f, true);
            sun.shadowStrength = 0.75f;
            RenderSettings.sun = sun;

            // ไฟส่องลงจากใต้หลังคาเหนือระเบียง
            for (int i = 0; i < 4; i++)
                K.AddLight(g, "Canopy_" + i, new Vector3(-5f, ROOF_Y - 0.2f, -7.5f + i * 5f), new Vector3(90f, 0f, 0f),
                           LightType.Spot, new Color(1f, 0.84f, 0.62f), 3.2f, 8f, 95f);

            // ไฟในห้อง suite
            for (int i = 0; i < 6; i++)
                K.AddLight(g, "Suite_" + i, new Vector3(2f + (i % 3) * 6f, ROOF_Y - 0.3f, (i < 3) ? 5f : -7f),
                           new Vector3(90f, 0f, 0f), LightType.Spot, new Color(1f, 0.88f, 0.72f), 3.0f, 9f, 100f);

            // ไฟชั้นล่าง กระจายให้ทั่วทุกโซน
            float[,] l1 = { { -6f, 6.5f }, { 4.5f, 6.5f }, { 12f, 6f }, { -8.5f, -8f }, { 4f, -8.5f }, { 12f, -7f }, { 0f, 0f } };
            for (int i = 0; i < l1.GetLength(0); i++)
                K.AddLight(g, "L1_" + i, new Vector3(l1[i, 0], L2_Y - 0.4f, l1[i, 1]), new Vector3(90f, 0f, 0f),
                           LightType.Spot, new Color(1f, 0.87f, 0.70f), 3.2f, 8.5f, 105f);

            // ไฟเส้นซ่อนใต้ฝ้า เดินตามขอบห้องชั้นล่าง
            K.NeonStrip("Cove_L1_N", _light, new Vector3((L1_X0 + HX) * 0.5f, L2_Y - 0.45f, HZ - 0.5f),
                        new Vector3(HX - L1_X0 - 1f, 0.06f, 0.06f), "LedWarm", new Color(1f, 0.8f, 0.55f), 1.4f, 9f);
            K.NeonStrip("Cove_L1_S", _light, new Vector3((L1_X0 + HX) * 0.5f, L2_Y - 0.45f, -HZ + 0.5f),
                        new Vector3(HX - L1_X0 - 1f, 0.06f, 0.06f), "LedWarm", new Color(1f, 0.8f, 0.55f), 1.4f, 9f);
        }

        // ───────────────────────── gameplay ─────────────────────────
        static void Gameplay()
        {
            var g = _play;

            // ผู้เล่นโผล่ที่โถงลิฟต์ หันหน้าเข้าบ้าน (ทิศเหนือ)
            var spawn = new Vector3(12.5f, 0f, -8.5f);
            K.Marker("PlayerSpawn_FromClub", g, spawn + Vector3.up * 0.2f);
            K.PlacePlayer(P_PLAYER, g, new Vector3(spawn.x, 0.02f, spawn.z), 0f);
            K.Marker("PlayerSpawn_Terrace", g, new Vector3(-6.5f, L2_Y + 0.2f, 0f));

            // ลิฟต์กลับลงไปคลับ — เล็งแล้วกด E เหมือนประตูข้ามซีนของบทก่อน
            LiftDoor();

            var boot = new GameObject("~Bootstrap");
            boot.transform.SetParent(g, false);
            boot.AddComponent<SceneBootstrap>();
        }

        /// <summary>ประตูลิฟต์ฝั่งตะวันออกของโถง กด E เพื่อกลับซีนคลับ</summary>
        static void LiftDoor()
        {
            var g = K.Group("LIFT_ToClub", _circ);
            float x = HX - 0.35f, z = -8.5f;

            // ผิวนอกของกรอบหยุดก่อนถึงผนังกระจก 0.02 m กันสองผิวทับกันแล้ว z-fight
            K.Box("Surround", g, new Vector3(x + 0.11f, 1.5f, z), new Vector3(0.28f, 3.0f, 3.2f), "StoneDark");
            K.Box("Door_L", g, new Vector3(x, 1.15f, z - 0.55f), new Vector3(0.12f, 2.3f, 1.05f), "Brass", default(Vector3), false);
            K.Box("Door_R", g, new Vector3(x, 1.15f, z + 0.55f), new Vector3(0.12f, 2.3f, 1.05f), "Brass", default(Vector3), false);
            K.Box("Jamb", g, new Vector3(x - 0.02f, 1.2f, z), new Vector3(0.06f, 2.5f, 0.06f), "Black", default(Vector3), false);
            K.Box("CallPanel", g, new Vector3(x - 0.06f, 1.15f, z - 1.05f), new Vector3(0.05f, 0.28f, 0.14f), "LedWarm", default(Vector3), false);
            K.Sign("Sign", g, new Vector3(x - 0.1f, 2.6f, z), new Vector2(1.6f, 0.26f), "LOBBY", new Color(0.92f, 0.86f, 0.72f), -90f);

            // กล่องเล็ง: trigger จึงไม่ขวางทางเดิน แต่ raycast ของ PlayerInteractor ยังชนได้
            var hit = new GameObject("ExitToClub");
            hit.transform.SetParent(g, false);
            hit.transform.localPosition = new Vector3(x - 0.35f, 1.2f, z);
            var box = hit.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.6f, 2.4f, 2.4f);

            var door = hit.AddComponent<SceneExitDoor>();
            door.nextSceneName = PREV_SCENE;

            var story = hit.AddComponent<StoryInteractable>();
            story.objectName = "ลิฟต์ลงไปชั้นคลับ";
            UnityEventTools.AddVoidPersistentListener(story.onInteract, door.GoToNextScene);

            K.Marker("DOOR_ToClubScene", g, new Vector3(x - 1.2f, 0f, z));
        }
    }
}
