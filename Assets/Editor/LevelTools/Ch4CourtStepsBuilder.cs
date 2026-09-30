using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SecretsThatBreathe.Act4;
using K = SecretsThatBreathe.LevelTools.LevelKit;
using C = SecretsThatBreathe.LevelTools.Ch4Kit;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// CHAPTER 4 - หน้าศาลตอนฝนตกหนัก (ACT 4.3 กำเนิดปีศาจ / 4.4 ความเงียบที่ดังที่สุด /
    /// 4.5 พิธีกรรมทำลายศรัทธา / Post-Credit หมออรินทร์)
    ///
    /// ผังมองจากด้านบน (x: ตะวันตก -> ตะวันออก, z: ใต้ -> เหนือ)
    ///
    ///   z 32 +-----------------------------------------------------------+
    ///        |             อาคารศาล (หลังคาจั่วทรงไทย)                    |
    ///   z 16 +--------------+ [ ประตูใหญ่ ] +----------------------------+
    ///        |  ลานบนฐานสูง  |   มุขหน้า     |  ลานบนฐานสูง (ยก 2.4 m)      |
    ///  z 7.5 +--ราวกันตก----+ (เสา 6 ต้น)  +---ราวกันตก------------------+
    ///        |    ไม้พุ่ม     |  บันได 15 ขั้น |    ไม้พุ่ม                   |
    ///  z 0.75|               +---------------+                            |
    ///        |  เสาธง   ต้นไม้         ลานหน้าศาล           ต้นไม้            |
    ///  z -8  |- - - - - - - - - - ฟุตบาท - - - - - [ท่อระบายน้ำ] - - ป้ายรถเมล์|
    ///  z -11 |==ขอบฟุตบาทขาวแดง=========================================|
    ///        |                    ถนน (เส้นกลางสีเหลือง)                  |
    ///  z -23 |==========  [รถหรูสีดำของหมออรินทร์] =======================|
    ///        |  ฟุตบาทฝั่งตรงข้าม + เสาไฟฟ้าสายระโยงระยาง                  |
    ///  z -26 +  ตึกแถว: ถ่ายเอกสาร / สำนักงานกฎหมาย / รับประกันตัว ...      +
    ///
    /// ฝนตกเฉพาะส่วนที่ไม่มีหลังคา (ใต้มุขหน้าแห้ง) ผู้เล่นเริ่มที่หน้าประตูใหญ่ มองลงบันไดไปเห็นถนน
    /// และรถสีดำจอดนิ่งอยู่ฝั่งตรงข้ามตั้งแต่แรก — ผู้เล่นที่สังเกตจะเห็นก่อน post-credit
    ///
    /// Menu: Tools > Secrets That Breathe > Build Chapter 4 Court Steps (Rain)
    /// </summary>
    public static class Ch4CourtStepsBuilder
    {
        // ── ระดับพื้น ──
        const float ROAD_Y = -0.15f;                // ถนนต่ำกว่าฟุตบาท 15 cm (ต่ำกว่า stepOffset เดินลงถนนได้)
        public const float PODIUM_Y = 2.40f;        // ฐานอาคาร = บันได 15 ลูก x 0.16 m
        const int STEPS = 15;
        const float TREAD = 0.45f;

        // ── ผังแนว z ──
        const float FACADE_Z = 16f;                 // หน้าอาคาร
        const float PODIUM_Z0 = 7.5f;               // ขอบหน้าฐานอาคาร = หัวบันได
        const float STAIR_Z0 = PODIUM_Z0 - STEPS * TREAD;   // 0.75 ตีนบันได
        const float SIDEWALK_Z0 = -11f, SIDEWALK_Z1 = -8f;
        const float ROAD_Z0 = -23f, ROAD_Z1 = -11f;
        const float FAR_WALK_Z0 = -26f;
        const float HX = 32f;                       // ความยาวถนนครึ่งหนึ่ง

        // ── มุขหน้า ──
        const float STAIR_HX = 8f;                  // บันไดกว้าง 16 m
        const float PORTICO_HX = 9.8f;
        const float COL_Z = 8.3f;
        const float COL_TOP = PODIUM_Y + 7.0f;      // 9.40
        const float ENTAB_TOP = COL_TOP + 0.6f;     // 10.00
        const float ROOF_PITCH = 40f;               // หลังคาทรงไทยชันกว่าหลังคาทั่วไป
        const float BLD_HX = 22f, BLD_Z1 = 32f, BLD_TOP = 13f;

        // ── บีทสำคัญ ──
        static readonly Vector3 DRAIN_POS = new Vector3(-2.5f, ROAD_Y, SIDEWALK_Z0 - 0.35f);
        static readonly Vector3 QTE_SPOT = new Vector3(-2.5f, 0f, SIDEWALK_Z0 + 0.8f);
        static readonly Vector3 AUNT_COLLAPSE = new Vector3(1.2f, 0f, -2.6f);
        static readonly Vector3 CAR_POS = new Vector3(5f, ROAD_Y, ROAD_Z0 + 1.15f);

        public const string ScenePath = C.CourtStepsPath;

        static Transform _root;
        static Transform _env, _struct, _circ, _dress, _light, _actors, _play;

        [MenuItem("Tools/Secrets That Breathe/Build Chapter 4 Court Steps (Rain)", false, 16)]
        public static void BuildScene() { BuildScene(true); }

        public static void BuildScene(bool askToSave)
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Ch4CourtSteps] leave play mode first."); return; }

            if (askToSave && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog(
                    "สร้างซีนหน้าศาลใหม่ทับ?",
                    C.SceneCourtSteps + ".unity มีอยู่แล้ว การ build จะสร้างซีนใหม่ทับทั้งหมด\n" +
                    "ของที่วางหรือขยับเองในซีนจะหายทั้งหมด",
                    "สร้างใหม่ทับ", "ยกเลิก")) return;
            if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            K.EnsureFolder(C.MatFolder);
            K.ResetPlaced();
            C.Materials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _root = new GameObject("=== CH4 COURT STEPS ===").transform;
            K.BuildCategories(_root);
            _env = K.Category(_root, K.Cat.Env);
            _struct = K.Category(_root, K.Cat.Structure);
            _circ = K.Category(_root, K.Cat.Circulation);
            _dress = K.Category(_root, K.Cat.Dressing);
            _light = K.Category(_root, K.Cat.Lighting);
            _actors = K.Category(_root, K.Cat.Actors);
            _play = K.Category(_root, K.Cat.Gameplay);

            Atmosphere();
            Ground();
            Street();
            Podium();
            Stairs();
            Portico();
            ThaiRoof();
            MainBuilding();
            Plaza();
            Shophouses();
            PowerLines();
            ArinCar();
            Lighting();
            Actors();
            Gameplay();
            Rain();

            GroundProps();

            C.EnsureInBuildSettings(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Ch4CourtSteps] built -> " + ScenePath);
            // สูงสุดที่ยืนได้คือลานบนฐานอาคาร ของที่สูงกว่านั้นเป็นหลังคา/กันสาด ไม่ใช่ทางเดิน
            Debug.Log(C.Audit(_root, "CH4 COURT STEPS", PODIUM_Y + 0.05f));
        }

        static void GroundProps()
        {
            Physics.SyncTransforms();
            var placed = K.Placed;
            for (int i = 0; i < placed.Count; i++) K.SnapDown(placed[i]);
        }

        // ───────────────────────── helpers ─────────────────────────

        /// <summary>
        /// ก้อนทรงกล่องที่ผิวบนลาดเอียงตามแนว z (สูง top0 ที่ z0 ไปถึง top1 ที่ z1)
        /// ใช้ทำผนังข้างบันได — primitive ของ Unity ลาดแบบนี้ไม่ได้ จึงสร้าง mesh เอง
        /// </summary>
        static GameObject Ramp(string name, Transform parent, float x0, float x1, float z0, float z1,
                               float y0, float top0, float top1, string mat)
        {
            var v = new[]
            {
                new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), new Vector3(x0, y0, z1),
                new Vector3(x0, top0, z0), new Vector3(x1, top0, z0), new Vector3(x1, top1, z1), new Vector3(x0, top1, z1),
            };
            int[][] faces =
            {
                new[] { 0, 4, 5, 1 },   // ใต้ (-Z)
                new[] { 2, 6, 7, 3 },   // เหนือ (+Z)
                new[] { 3, 7, 4, 0 },   // ตะวันตก
                new[] { 1, 5, 6, 2 },   // ตะวันออก
                new[] { 4, 7, 6, 5 },   // บน
                new[] { 0, 1, 2, 3 },   // ล่าง
            };
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int f = 0; f < faces.Length; f++)
            {
                int b = verts.Count;
                for (int k = 0; k < 4; k++) verts.Add(v[faces[f][k]]);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
            }
            var mesh = new Mesh { name = name + "_Mesh" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = g.AddComponent<MeshRenderer>();
            var m = K.M(mat);
            if (m != null) mr.sharedMaterial = m;
            g.isStatic = true;
            return g;
        }

        static Material LoadOrCreateMat(string file, string shader)
        {
            string path = C.MatFolder + "/" + file + ".mat";
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

        // ───────────────────────── atmosphere ─────────────────────────
        static void Atmosphere()
        {
            var g = K.Group("Atmosphere", _env);

            // ฟ้าครึ้มพายุ: เทาเรียบทั้งผืนแบบเมฆฝนปิดฟ้า
            // Skybox/Procedural ออกโทนน้ำเงินเสมอ จึงใช้ 6 Sided แบบไม่มี texture (ได้สีเทาเรียบ x tint)
            var sky = LoadOrCreateMat("M_C_Skybox_Overcast", "Skybox/6 Sided");
            if (sky != null)
            {
                sky.SetColor("_Tint", new Color(0.44f, 0.47f, 0.52f));
                sky.SetFloat("_Exposure", 1.0f);
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.26f, 0.29f, 0.34f);
            RenderSettings.ambientEquatorColor = new Color(0.17f, 0.18f, 0.20f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.07f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.29f, 0.32f, 0.36f);
            // ม่านฝน: ตึกแถวฝั่งตรงข้าม (~15 m) ยังเห็นชัด ปลายถนนจางหาย
            RenderSettings.fogDensity = 0.03f;
            RenderSettings.reflectionIntensity = 0.9f;

            var probe = new GameObject("Reflection Probe");
            probe.transform.SetParent(g, false);
            probe.transform.localPosition = new Vector3(0f, 3f, -10f);
            var rp = probe.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Realtime;
            rp.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            rp.size = new Vector3(HX * 2f, 24f, 60f);
            rp.boxProjection = false;
            rp.resolution = 128;
            rp.intensity = 1f;
        }

        // ───────────────────────── พื้น ─────────────────────────
        static void Ground()
        {
            // พื้นไกล ๆ รอบฉาก (ไม่มี collider) ให้ขอบฉากจมหายไปในหมอกแทนที่จะเห็นเป็นขอบโลก
            var far = new GameObject("~Backdrop Ground").transform;
            C.Slab(far, "FarGround", -200f, 200f, -200f, 200f, ROAD_Y - 0.02f, 0.2f, "Asphalt", false);
        }

        // ───────────────────────── ถนน + ฟุตบาท ─────────────────────────
        static void Street()
        {
            var g = K.Group("Street", _struct);
            var d = K.Group("Street_Markings", _dress);

            C.Slab(g, "Road", -HX, HX, ROAD_Z0, ROAD_Z1, ROAD_Y, 0.3f, "Asphalt");
            C.Slab(g, "Sidewalk_Court", -HX, HX, SIDEWALK_Z0, SIDEWALK_Z1, 0f, 0.3f, "Concrete");
            C.Slab(g, "Sidewalk_Far", -HX, HX, FAR_WALK_Z0, ROAD_Z0, 0f, 0.3f, "Concrete");

            // เส้นกลางถนนคู่สีเหลือง + เส้นแบ่งช่องจราจรสีขาวประ แบบถนนไทย
            float mid = (ROAD_Z0 + ROAD_Z1) * 0.5f;
            C.Slab(d, "CentreLine_A", -HX, HX, mid - 0.2f, mid - 0.08f, ROAD_Y + 0.006f, 0.012f, "LineYellow", false);
            C.Slab(d, "CentreLine_B", -HX, HX, mid + 0.08f, mid + 0.2f, ROAD_Y + 0.006f, 0.012f, "LineYellow", false);
            for (float x = -HX + 1f; x < HX; x += 6f)
            {
                C.Slab(d, "Lane_S_" + x, x, x + 3f, mid - 3.06f, mid - 2.94f, ROAD_Y + 0.006f, 0.012f, "LineWhite", false);
                C.Slab(d, "Lane_N_" + x, x, x + 3f, mid + 2.94f, mid + 3.06f, ROAD_Y + 0.006f, 0.012f, "LineWhite", false);
            }
            // ทางม้าลายหน้าศาล
            for (float z = ROAD_Z0 + 0.6f; z < ROAD_Z1 - 0.5f; z += 1.0f)
                C.Slab(d, "Zebra_" + z.ToString("0.0"), 11f, 14.5f, z, z + 0.5f, ROAD_Y + 0.006f, 0.012f, "LineWhite", false);

            // ขอบฟุตบาทขาวแดง (ห้ามจอด) สองฝั่งถนน
            Curb(d, "Curb_Court", SIDEWALK_Z0, -1f);
            Curb(d, "Curb_Far", ROAD_Z0, 1f);

            // น้ำไหลเลาะขอบฟุตบาทลงท่อ
            C.Slab(d, "GutterWater_Court", -HX, HX, SIDEWALK_Z0 - 0.45f, SIDEWALK_Z0, ROAD_Y + 0.012f, 0.012f, "GutterWater", false);
            C.Slab(d, "GutterWater_Far", -HX, HX, ROAD_Z0, ROAD_Z0 + 0.4f, ROAD_Y + 0.012f, 0.012f, "GutterWater", false);

            // ท่อระบายน้ำริมฟุตบาท — ตัวกลางคือจุดที่เข้มขว้างเข็มกลัดทิ้ง
            float[] dx = { -20f, -8f, DRAIN_POS.x, 10f, 22f };
            for (int i = 0; i < dx.Length; i++)
                Drain(d, (dx[i] == DRAIN_POS.x) ? "DRAIN_Target" : "Drain_" + i, new Vector3(dx[i], ROAD_Y, DRAIN_POS.z));

            // แอ่งน้ำขังสะท้อนไฟ (ถนน + ลาน)
            var rng = new System.Random(4404);
            var pud = K.Group("Puddles", _dress);
            for (int i = 0; i < 26; i++)
            {
                float x = -HX + 2f + (float)rng.NextDouble() * (HX * 2f - 4f);
                bool road = i % 2 == 0;
                float z = road ? ROAD_Z0 + 0.8f + (float)rng.NextDouble() * 10.4f
                               : SIDEWALK_Z0 + 0.3f + (float)rng.NextDouble() * 10.5f;
                float y = road ? ROAD_Y : 0f;
                float w = 0.8f + (float)rng.NextDouble() * 2.2f;
                float l = 0.6f + (float)rng.NextDouble() * 1.6f;
                K.Box("Puddle_" + i, pud, new Vector3(x, y + 0.004f, z), new Vector3(w, 0.006f, l), "Puddle",
                      new Vector3(0f, (float)rng.NextDouble() * 180f, 0f), false);
            }
        }

        /// <summary>ขอบฟุตบาททาสีสลับขาวแดงทุก 1 m — side = ทิศที่หน้าขอบหันไป (-1 = ใต้)</summary>
        static void Curb(Transform parent, string name, float z, float side)
        {
            var g = K.Group(name, parent);
            for (int i = 0; i < (int)(HX * 2f); i++)
            {
                float x = -HX + i + 0.5f;
                string mat = (i % 2 == 0) ? "CurbRed" : "CurbWhite";
                K.Box("Seg_" + i, g, new Vector3(x, (ROAD_Y + 0.01f) * 0.5f, z + side * 0.06f),
                      new Vector3(1f, -ROAD_Y + 0.01f, 0.12f), mat, default(Vector3), false);
                K.Box("Top_" + i, g, new Vector3(x, 0.004f, z - side * 0.1f), new Vector3(1f, 0.008f, 0.2f), mat, default(Vector3), false);
            }
        }

        static void Drain(Transform parent, string name, Vector3 pos)
        {
            var g = K.Group(name, parent);
            g.localPosition = pos;
            K.Box("Pit", g, new Vector3(0f, 0.004f, 0f), new Vector3(0.9f, 0.008f, 0.5f), "Black", default(Vector3), false);
            for (int i = 0; i < 7; i++)
                K.Box("Bar_" + i, g, new Vector3(-0.39f + i * 0.13f, 0.012f, 0f), new Vector3(0.04f, 0.016f, 0.5f), "Iron", default(Vector3), false);
            K.Box("Frame", g, new Vector3(0f, 0.01f, -0.27f), new Vector3(1.0f, 0.02f, 0.04f), "Iron", default(Vector3), false);
            // ช่องรับน้ำที่หน้าขอบฟุตบาท
            K.Box("CurbInlet", g, new Vector3(0f, 0.07f, 0.23f), new Vector3(0.8f, 0.08f, 0.03f), "Black", default(Vector3), false);
            K.Marker("MARK_DrainGrate", g, new Vector3(0f, 0.02f, 0f));
        }

        // ───────────────────────── ฐานอาคาร ─────────────────────────
        static void Podium()
        {
            var g = K.Group("Podium", _struct);

            // ฐานยกสูงทั้งแนวหน้าอาคาร หัวบันไดต่อเป็นลานเดียวกับมุขหน้า
            C.Slab(g, "Podium", -BLD_HX, BLD_HX, PODIUM_Z0, FACADE_Z, PODIUM_Y, PODIUM_Y, "Stone");
            C.Panel(_dress, "Podium_Band", -BLD_HX - 0.05f, BLD_HX + 0.05f, PODIUM_Z0 - 0.08f, PODIUM_Z0, PODIUM_Y - 0.3f, 0.3f, "Column", false);

            // ราวกันตกหน้าฐาน นอกช่วงบันได (สูงจากลานข้างล่าง 2.4 m)
            for (int s = -1; s <= 1; s += 2)
            {
                float x0 = s < 0 ? -BLD_HX : PORTICO_HX;
                float x1 = s < 0 ? -PORTICO_HX : BLD_HX;
                Balustrade(g, "Balustrade_" + (s < 0 ? "W" : "E"), x0, x1, PODIUM_Z0 + 0.15f, PODIUM_Y);

                // แนวไม้พุ่มชิดฐาน
                // กระบะสูง 0.45 m เกิน stepOffset 0.3 ก้าวขึ้นไม่ได้อยู่แล้ว ใช้ collider สูงชนผนังฐาน
                // ไม่งั้นหลังกระบะกลายเป็นพื้นลอยที่ไปไม่ถึง
                C.Panel(g, "Hedge_Trough_" + s, x0 + 0.3f, x1 - 0.3f, PODIUM_Z0 - 1.1f, PODIUM_Z0 - 0.1f, 0f, 0.45f, "Concrete", false);
                C.Blocker(g, "Hedge_Collider_" + s, new Vector3((x0 + x1) * 0.5f, 1.5f, PODIUM_Z0 - 0.55f),
                          new Vector3(x1 - x0 - 0.6f, 3.0f, 1.1f));
                C.Panel(_dress, "Hedge_" + s, x0 + 0.35f, x1 - 0.35f, PODIUM_Z0 - 1.05f, PODIUM_Z0 - 0.15f, 0.42f, 0.7f, "Greenery", false);
            }
            // ข้างฐานสองด้าน ปิดขอบให้ลานบนฐานเป็นทางตัน ไม่มีช่องตก
            Balustrade(g, "Balustrade_SideW", -BLD_HX, -BLD_HX + 0.2f, PODIUM_Z0, PODIUM_Y, FACADE_Z);
            Balustrade(g, "Balustrade_SideE", BLD_HX - 0.2f, BLD_HX, PODIUM_Z0, PODIUM_Y, FACADE_Z);
        }

        /// <summary>
        /// ราวลูกกรงปูนสีขาว — ภาพเป็นลูกกรงเตี้ย 1 m แต่ collider ล่องหนสูงเลยเพดานตรวจ
        /// กันผู้เล่นกระโดด (jumpHeight 2 m) ข้ามราวลงไปข้างล่าง
        /// </summary>
        static void Balustrade(Transform parent, string name, float x0, float x1, float z, float floorY, float z1 = float.NaN)
        {
            var g = K.Group(name, parent);
            bool alongZ = !float.IsNaN(z1);
            float za = z, zb = alongZ ? z1 : z + 0.2f;
            C.Panel(g, "Base", x0, x1, za, zb, floorY, 0.15f, "Column", false);
            C.Panel(g, "Rail", x0 - 0.03f, x1 + 0.03f, za - 0.03f, zb + 0.03f, floorY + 0.9f, 0.12f, "Column", false);
            float len = alongZ ? (zb - za) : (x1 - x0);
            int n = Mathf.Max(2, Mathf.FloorToInt(len / 0.3f));
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                Vector3 p = alongZ ? new Vector3((x0 + x1) * 0.5f, floorY + 0.525f, Mathf.Lerp(za + 0.1f, zb - 0.1f, t))
                                   : new Vector3(Mathf.Lerp(x0 + 0.1f, x1 - 0.1f, t), floorY + 0.525f, (za + zb) * 0.5f);
                K.Cyl("Baluster_" + i, g, p, 0.12f, 0.75f, "Column");
            }
            C.Blocker(g, "Collider", new Vector3((x0 + x1) * 0.5f, floorY + 1.6f, (za + zb) * 0.5f),
                      new Vector3(x1 - x0, 3.2f, zb - za));
        }

        // ───────────────────────── บันไดศาล ─────────────────────────
        static void Stairs()
        {
            var g = K.Group("CourtStairs", _circ);
            float rise = PODIUM_Y / STEPS;    // 0.16 m
            for (int i = 0; i < STEPS; i++)
            {
                float z0 = STAIR_Z0 + i * TREAD;
                // ลูกตั้งซ้อนเป็นก้อนตัน ไม่มีร่องให้ตก
                C.Slab(g, "Step_" + i.ToString("00"), -STAIR_HX, STAIR_HX, z0, z0 + TREAD, (i + 1) * rise, (i + 1) * rise, "Stone");
                // จมูกบันไดสีเข้ม เห็นขั้นชัดตอนเปียก
                C.Slab(_dress, "Nosing_" + i.ToString("00"), -STAIR_HX, STAIR_HX, z0, z0 + 0.06f, (i + 1) * rise + 0.003f, 0.006f, "StoneDark", false);
            }

            // ผนังข้างบันไดลาดตามขั้น + ป้อมหัวเสาที่ตีนบันได
            for (int s = -1; s <= 1; s += 2)
            {
                float xa = s < 0 ? -PORTICO_HX : STAIR_HX;
                float xb = s < 0 ? -STAIR_HX : PORTICO_HX;
                Ramp("Cheek_" + (s < 0 ? "W" : "E"), g, xa, xb, STAIR_Z0, PODIUM_Z0, 0f, 0.95f, PODIUM_Y + 0.95f, "Column");
                C.Blocker(g, "Cheek_Collider_" + s, new Vector3((xa + xb) * 0.5f, 1.8f, (STAIR_Z0 + PODIUM_Z0) * 0.5f),
                          new Vector3(xb - xa, 3.6f, PODIUM_Z0 - STAIR_Z0));
                // ป้อมที่ตีนบันได มีโคมไฟบนยอด
                float px = (xa + xb) * 0.5f;
                C.Panel(g, "Pier_" + s, px - 0.75f, px + 0.75f, STAIR_Z0 - 1.2f, STAIR_Z0 + 0.3f, 0f, 1.5f, "Column", false);
                C.Blocker(g, "Pier_Collider_" + s, new Vector3(px, 1.5f, STAIR_Z0 - 0.45f), new Vector3(1.5f, 3.0f, 1.5f));
                C.Panel(_dress, "Pier_Cap_" + s, px - 0.85f, px + 0.85f, STAIR_Z0 - 1.3f, STAIR_Z0 + 0.4f, 1.5f, 0.12f, "Stone", false);
                K.Cyl("Pier_Lamp_Post_" + s, _dress, new Vector3(px, 1.95f, STAIR_Z0 - 0.45f), 0.12f, 0.7f, "Iron");
                K.Box("Pier_Lamp_" + s, _dress, new Vector3(px, 2.5f, STAIR_Z0 - 0.45f), new Vector3(0.36f, 0.5f, 0.36f), "SodiumLamp", default(Vector3), false);
            }
            // ราวจับโลหะกลางบันได
            C.Rod(g, "Handrail_Mid", new Vector3(0f, 0.9f, STAIR_Z0 + 0.2f), new Vector3(0f, PODIUM_Y + 0.9f, PODIUM_Z0 - 0.2f), 0.05f, "Chrome");
            for (int i = 0; i <= 4; i++)
            {
                float t = i / 4f;
                float z = Mathf.Lerp(STAIR_Z0 + 0.2f, PODIUM_Z0 - 0.2f, t);
                float y = Mathf.Lerp(0f, PODIUM_Y, t) + 0.16f;
                C.Rod(g, "Handrail_Post_" + i, new Vector3(0f, y, z), new Vector3(0f, y + 0.74f, z), 0.04f, "Chrome");
            }
        }

        // ───────────────────────── มุขหน้า ─────────────────────────
        static void Portico()
        {
            var g = K.Group("Portico", _struct);

            // พื้นมุขหน้าหินขัด ต่างจากลานหินข้างล่างให้รู้ว่าอยู่ใต้หลังคาแล้ว
            C.Slab(_dress, "Portico_Floor", -PORTICO_HX, PORTICO_HX, PODIUM_Z0 + 0.3f, FACADE_Z, PODIUM_Y + 0.01f, 0.02f, "StoneDark", false);

            // เสาสี่เหลี่ยม 6 ต้น หัวเสาบัว ช่องระหว่างเสา 2.2 m
            for (int i = 0; i < 6; i++)
            {
                float x = -7.5f + i * 3f;
                var c = K.Group("Column_" + i, g);
                c.localPosition = new Vector3(x, PODIUM_Y, COL_Z);
                K.Box("Plinth", c, new Vector3(0f, 0.15f, 0f), new Vector3(1.05f, 0.3f, 1.05f), "Stone");
                K.Box("Shaft", c, new Vector3(0f, 3.5f, 0f), new Vector3(0.8f, 7.0f, 0.8f), "Column");
                K.Box("Capital_Lotus", c, new Vector3(0f, 6.65f, 0f), new Vector3(1.0f, 0.35f, 1.0f), "Column", default(Vector3), false);
                K.Box("Capital_Band", c, new Vector3(0f, 6.4f, 0f), new Vector3(0.86f, 0.1f, 0.86f), "Gold", default(Vector3), false);
            }

            // ทับหลัง (entablature) + ฝ้าใต้หลังคามุข
            C.Slab(g, "Entablature", -PORTICO_HX - 0.2f, PORTICO_HX + 0.2f, COL_Z - 0.7f, FACADE_Z, ENTAB_TOP, ENTAB_TOP - COL_TOP, "Facade", false);
            C.Slab(_dress, "Entablature_Band", -PORTICO_HX - 0.25f, PORTICO_HX + 0.25f, COL_Z - 0.76f, COL_Z - 0.68f, COL_TOP + 0.45f, 0.12f, "Gold", false);
            C.ThaiSign("Name_Sign", _dress, new Vector3(0f, COL_TOP + 0.24f, COL_Z - 0.72f), new Vector2(10f, 0.6f),
                       "ศาลสถิตยุติธรรม", C.GoldInk, 180f);

            // ประตูใหญ่เข้าอาคาร (ปิด — ห้องพิจารณาคดีจบไปแล้ว)
            var dr = K.Group("MainDoors", _circ);
            float dz = FACADE_Z - 0.05f;
            C.Panel(dr, "Frame", -2.4f, 2.4f, dz - 0.1f, FACADE_Z, PODIUM_Y, 4.6f, "WoodDark", false);
            C.Panel(dr, "Leaf_L", -2.1f, -0.02f, dz - 0.16f, dz - 0.1f, PODIUM_Y, 4.2f, "Wood", false);
            C.Panel(dr, "Leaf_R", 0.02f, 2.1f, dz - 0.16f, dz - 0.1f, PODIUM_Y, 4.2f, "Wood", false);
            C.Panel(dr, "Glass_L", -1.8f, -0.3f, dz - 0.17f, dz - 0.16f, PODIUM_Y + 1.2f, 2.5f, "Glass", false);
            C.Panel(dr, "Glass_R", 0.3f, 1.8f, dz - 0.17f, dz - 0.16f, PODIUM_Y + 1.2f, 2.5f, "Glass", false);
            K.Marker("SHUT_CourtDoors", dr, new Vector3(0f, PODIUM_Y, FACADE_Z - 1f));

            // ม้านั่งรอหน้าห้องแบบศาลไทย ชิดผนังใต้มุข
            for (int s = -1; s <= 1; s += 2)
                C.PublicBench(_dress, "WaitingBench_" + s, new Vector3(s * 5.5f, PODIUM_Y, FACADE_Z - 0.7f), 3.2f, 180f);
        }

        // ───────────────────────── หลังคาทรงไทย ─────────────────────────
        static void ThaiRoof()
        {
            var g = K.Group("ThaiRoof", _dress);
            float half = PORTICO_HX;
            float rise = half * Mathf.Tan(ROOF_PITCH * Mathf.Deg2Rad);   // ~6.9 m
            float ridgeY = ENTAB_TOP + rise;
            float z0 = COL_Z - 0.7f, z1 = FACADE_Z;

            // หน้าจั่ว (หน้าบัน) พื้นแดง กรอบทองด้านใน แบบหน้าบันอาคารราชการไทย
            C.Gable("Gable", g, new Vector3(0f, ENTAB_TOP, (z0 + z1) * 0.5f), half * 2f, rise, z1 - z0, "Crimson");
            Vector3 fl = new Vector3(-half + 1.2f, ENTAB_TOP + 0.35f, z0 - 0.05f);
            Vector3 fr = new Vector3(half - 1.2f, ENTAB_TOP + 0.35f, z0 - 0.05f);
            Vector3 ft = new Vector3(0f, ENTAB_TOP + rise - 1.25f, z0 - 0.05f);
            C.Rod(g, "Frame_Base", fl, fr, 0.14f, "Gold");
            C.Rod(g, "Frame_L", fl, ft, 0.14f, "Gold");
            C.Rod(g, "Frame_R", fr, ft, 0.14f, "Gold");

            // ผืนหลังคาสองฝั่ง ยื่นชายคาเลยหน้าจั่วออกไป
            float slopeLen = Mathf.Sqrt(half * half + rise * rise);
            Vector2 dir = new Vector2(half, rise) / slopeLen;      // จากชายคาขึ้นไปหาสัน
            Vector2 nrm = new Vector2(-dir.y, dir.x);              // ตั้งฉากชี้ออกนอกหลังคา (ฝั่งตะวันตก)
            const float eave = 0.9f, thick = 0.3f;
            float len = slopeLen + eave;
            for (int s = -1; s <= 1; s += 2)
            {
                // จุดกึ่งกลางผืนหลังคาฝั่ง s (คำนวณฝั่งตะวันตกแล้วสะท้อนแกน x)
                Vector2 eaveW = new Vector2(-half, ENTAB_TOP) - dir * eave;
                Vector2 ridge = new Vector2(0f, ridgeY);
                Vector2 c = (eaveW + ridge) * 0.5f + nrm * (thick * 0.5f);
                K.Box("Roof_" + (s < 0 ? "W" : "E"), g, new Vector3(s < 0 ? c.x : -c.x, c.y, (z0 + z1) * 0.5f - 0.3f),
                      new Vector3(len, thick, z1 - z0 + 0.8f), "RoofTile", new Vector3(0f, 0f, -s * ROOF_PITCH), false);

                // ป้านลมทอง + ใบระกาหยักตามแนวหลังคาด้านหน้า
                Vector3 a = new Vector3(s * (half + dir.x * eave), ENTAB_TOP - dir.y * eave + thick, z0 - 0.75f);
                Vector3 b = new Vector3(0f, ridgeY + thick, z0 - 0.75f);
                C.Rod(g, "Bargeboard_" + s, a, b, 0.22f, "Gold");
                int teeth = 16;
                for (int i = 1; i < teeth; i++)
                {
                    Vector3 p = Vector3.Lerp(a, b, i / (float)teeth);
                    K.Box("BaiRaka_" + s + "_" + i, g, p + new Vector3(0f, 0.2f, 0f), new Vector3(0.1f, 0.28f, 0.08f), "Gold",
                          new Vector3(0f, 0f, s * 20f), false);
                }
                // หางหงส์ที่ปลายชายคา
                C.Rod(g, "HangHong_" + s, a, a + new Vector3(s * 0.35f, 0.9f, 0f), 0.14f, "Gold");
            }
            // ช่อฟ้าบนยอดจั่ว
            Vector3 apex = new Vector3(0f, ridgeY + 0.3f, z0 - 0.75f);
            C.Rod(g, "ChoFa_Stem", apex, apex + new Vector3(0f, 1.9f, 0.45f), 0.22f, "Gold");
            C.Rod(g, "ChoFa_Hook", apex + new Vector3(0f, 1.9f, 0.45f), apex + new Vector3(0f, 2.2f, -0.3f), 0.16f, "Gold");
            K.Sphere("ChoFa_Head", g, apex + new Vector3(0f, 2.2f, -0.3f), 0.28f, "Gold");

            // ตราชูกลางหน้าบัน — ตราเดียวกับเหนือบัลลังก์ในห้องพิจารณาคดี
            var em = K.Group("Pediment_Emblem", g);
            em.localPosition = new Vector3(0f, ENTAB_TOP + rise * 0.38f, z0 - 0.02f);
            K.Cyl("Ring", em, Vector3.zero, 2.3f, 0.06f, "Gold", new Vector3(90f, 0f, 0f));
            K.Cyl("Field", em, new Vector3(0f, 0f, -0.03f), 2.1f, 0.06f, "WoodDark", new Vector3(90f, 0f, 0f));
            C.Scales(em, "Scales", new Vector3(0f, 0.02f, -0.12f), 1.6f, 180f);
        }

        // ───────────────────────── ตัวอาคารหลัก ─────────────────────────
        static void MainBuilding()
        {
            var g = K.Group("MainBuilding", _struct);
            C.Panel(g, "Block", -BLD_HX, BLD_HX, FACADE_Z, BLD_Z1, 0f, BLD_TOP, "Facade");
            C.Panel(_dress, "Cornice", -BLD_HX - 0.3f, BLD_HX + 0.3f, FACADE_Z - 0.3f, BLD_Z1 + 0.3f, BLD_TOP - 0.4f, 0.4f, "Column", false);

            // หลังคาจั่วใหญ่ตามแนวยาวอาคาร
            float depth = BLD_Z1 - FACADE_Z;
            C.Gable("MainRoof", g, new Vector3(0f, BLD_TOP, (FACADE_Z + BLD_Z1) * 0.5f), depth + 1.6f, 6.2f, BLD_HX * 2f + 1.2f, "RoofTile", 90f);

            // หน้าต่างสองชั้นสองฝั่งมุขหน้า บางบานเปิดไฟ (เจ้าหน้าที่ยังทำงานอยู่)
            var w = K.Group("Windows", _dress);
            var rng = new System.Random(4405);
            for (int s = -1; s <= 1; s += 2)
                for (float x = PORTICO_HX + 1.8f; x < BLD_HX - 1f; x += 2.6f)
                    for (int f = 0; f < 2; f++)
                    {
                        float y = PODIUM_Y + 1.2f + f * 4.2f;
                        string mat = rng.Next(0, 3) == 0 ? "ShopWindow" : "GlassTint";
                        C.Panel(w, "Win_" + s + "_" + x.ToString("0.0") + "_" + f, s * x - 0.7f, s * x + 0.7f,
                                FACADE_Z - 0.04f, FACADE_Z, y, 2.4f, mat, false);
                        C.Panel(w, "WinFrame_" + s + "_" + x.ToString("0.0") + "_" + f, s * x - 0.8f, s * x + 0.8f,
                                FACADE_Z - 0.08f, FACADE_Z, y - 0.1f, 0.1f, "Column", false);
                    }
        }

        // ───────────────────────── ลานหน้าศาล ─────────────────────────
        static void Plaza()
        {
            var g = K.Group("Plaza", _struct);
            C.Slab(g, "Plaza", -HX, HX, SIDEWALK_Z1, PODIUM_Z0 + 0.4f, 0f, 0.3f, "Stone");
            // แนวรอยต่อหิน
            for (float z = SIDEWALK_Z1 + 1.5f; z < STAIR_Z0 - 0.5f; z += 1.5f)
                C.Slab(_dress, "Joint_" + z.ToString("0.0"), -HX, HX, z - 0.02f, z + 0.02f, 0.004f, 0.008f, "StoneDark", false);

            // เสาธงชาติ — ธงเปียกห้อยตกลงมาแนบเสา
            var fp = K.Group("Flagpole", _dress);
            fp.localPosition = new Vector3(-13f, 0f, -3.5f);
            K.Cyl("Base", fp, new Vector3(0f, 0.2f, 0f), 1.2f, 0.4f, "Stone", default(Vector3), true);
            K.Cyl("Pole", fp, new Vector3(0f, 6.2f, 0f), 0.14f, 11.6f, "Chrome", default(Vector3), true);
            K.Sphere("Finial", fp, new Vector3(0f, 12.05f, 0f), 0.22f, "Gold");
            var flag = K.Group("ThaiFlag", fp);
            flag.localPosition = new Vector3(0.07f, 11.8f, 0f);
            flag.localEulerAngles = new Vector3(0f, 0f, -74f);
            string[] stripes = { "FlagRed", "FlagWhite", "FlagBlue", "FlagWhite", "FlagRed" };
            float[] hs = { 0.2f, 0.2f, 0.4f, 0.2f, 0.2f };
            float yTop = 0f;
            for (int i = 0; i < stripes.Length; i++)
            {
                K.Box("Stripe_" + i, flag, new Vector3(0.9f, yTop - hs[i] * 0.5f, 0f), new Vector3(1.8f, hs[i], 0.02f), stripes[i], default(Vector3), false);
                yTop -= hs[i];
            }

            // ต้นไม้ใหญ่สี่ต้นในกระบะเตี้ย (สูง 0.45 = ก้าวขึ้นได้ ไม่เป็นเกาะที่ไปไม่ถึง)
            float[] tx = { -19f, -8.5f, 8.5f, 19f };
            for (int i = 0; i < tx.Length; i++) Tree(_dress, "Tree_" + i, new Vector3(tx[i], 0f, -4.8f));

            // เสาหลักกันรถริมฟุตบาท
            for (float x = -HX + 2f; x < HX; x += 3f)
            {
                if (Mathf.Abs(x - DRAIN_POS.x) < 1.2f) continue;
                K.Cyl("Bollard_" + x, _dress, new Vector3(x, 0.4f, SIDEWALK_Z0 + 0.45f), 0.18f, 0.8f, "Iron", default(Vector3), true);
            }

            // โคมไฟถนนฝั่งศาล
            float[] lx = { -18f, -6f, 6f, 18f };
            for (int i = 0; i < lx.Length; i++) StreetLamp(_dress, "StreetLamp_" + i, new Vector3(lx[i], 0f, SIDEWALK_Z0 + 0.5f));

            BusShelter(_dress, new Vector3(16f, 0f, SIDEWALK_Z0 + 1.6f));
        }

        static void Tree(Transform parent, string name, Vector3 p)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Box("Planter", g, new Vector3(0f, 0.225f, 0f), new Vector3(2.0f, 0.45f, 2.0f), "Stone");
            K.Box("Soil", g, new Vector3(0f, 0.455f, 0f), new Vector3(1.8f, 0.02f, 1.8f), "Soil", default(Vector3), false);
            K.Cyl("Trunk", g, new Vector3(0f, 2.4f, 0f), 0.38f, 4.0f, "Bark", default(Vector3), true);
            K.Sphere("Crown_A", g, new Vector3(0f, 5.4f, 0f), 4.6f, "Greenery");
            K.Sphere("Crown_B", g, new Vector3(1.6f, 4.8f, 0.8f), 3.2f, "Greenery");
            K.Sphere("Crown_C", g, new Vector3(-1.4f, 4.9f, -0.9f), 3.4f, "Greenery");
        }

        static void StreetLamp(Transform parent, string name, Vector3 p)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            K.Cyl("Pole", g, new Vector3(0f, 3.5f, 0f), 0.16f, 7f, "Iron", default(Vector3), true);
            C.Rod(g, "Arm", new Vector3(0f, 6.8f, 0f), new Vector3(0f, 7.1f, -1.6f), 0.08f, "Iron");
            K.Box("Head", g, new Vector3(0f, 7.05f, -1.7f), new Vector3(0.36f, 0.14f, 0.7f), "Iron", default(Vector3), false);
            K.Box("Lens", g, new Vector3(0f, 6.97f, -1.7f), new Vector3(0.3f, 0.03f, 0.6f), "SodiumLamp", default(Vector3), false);
        }

        static void BusShelter(Transform parent, Vector3 p)
        {
            var g = K.Group("BusShelter", parent);
            g.localPosition = p;
            K.Box("Roof", g, new Vector3(0f, 2.6f, 0f), new Vector3(4.2f, 0.1f, 1.6f), "Tarp", new Vector3(-4f, 0f, 0f), false);
            for (int i = 0; i < 2; i++)
                K.Box("Post_" + i, g, new Vector3(i == 0 ? -1.9f : 1.9f, 1.3f, 0.6f), new Vector3(0.1f, 2.6f, 0.1f), "Chrome");
            K.Box("BackPanel", g, new Vector3(0f, 1.4f, 0.72f), new Vector3(3.8f, 1.8f, 0.04f), "Glass");
            K.Box("Bench", g, new Vector3(0f, 0.43f, 0.35f), new Vector3(3.0f, 0.06f, 0.4f), "Chrome");
            C.Blocker(g, "BenchCollider", new Vector3(0f, 0.22f, 0.35f), new Vector3(3.0f, 0.45f, 0.4f));
            C.ThaiSign("Sign", g, new Vector3(0f, 2.3f, -0.8f), new Vector2(2.6f, 0.28f), "ป้ายรถประจำทาง", Color.white, 180f);
        }

        // ───────────────────────── ตึกแถวฝั่งตรงข้าม ─────────────────────────
        static void Shophouses()
        {
            var g = K.Group("Shophouses", _struct);
            const float UNIT = 4f, FLOORS = 3, FLOOR_H = 3.6f;
            float zf = FAR_WALK_Z0;
            float h = FLOORS * FLOOR_H + 0.8f;
            C.Panel(g, "Mass", -HX, HX, zf - 12f, zf, 0f, h, "ShopFront");

            // ร้านรอบศาลไทย: ถ่ายเอกสาร สำนักงานกฎหมาย รับประกันตัว — ตัดกับเรื่องที่เพิ่งเกิดในศาล
            string[] signs = { "ถ่ายเอกสาร", "", "สำนักงานกฎหมาย", "ข้าวแกง", "", "รับประกันตัว 24 ชม.",
                               "เครื่องเขียน", "", "กาแฟโบราณ", "ถ่ายเอกสาร", "", "ทนายความ", "", "ร้านยา", "" };
            var d = K.Group("Shopfronts", _dress);
            var rng = new System.Random(4406);
            int n = Mathf.RoundToInt(HX * 2f / UNIT);
            for (int i = 0; i < n; i++)
            {
                float x0 = -HX + i * UNIT, xc = x0 + UNIT * 0.5f;
                var u = K.Group("Unit_" + i.ToString("00"), d);
                string face = (i % 3 == 1) ? "ShopFrontB" : "ShopFront";
                C.Panel(u, "Face", x0 + 0.02f, x0 + UNIT - 0.02f, zf + 0.0f, zf + 0.06f, 0f, h, face, false);
                C.Panel(u, "Pilaster", x0 - 0.12f, x0 + 0.12f, zf, zf + 0.18f, 0f, h, "Concrete", false);

                // ชั้นล่าง: ประตูม้วนเหล็ก (ปิดเพราะฝน) บางห้องเปิดไฟ
                bool open = rng.Next(0, 4) == 0;
                C.Panel(u, open ? "Shopfront_Lit" : "Shutter", x0 + 0.3f, x0 + UNIT - 0.3f, zf + 0.06f, zf + 0.1f, 0f, 3.0f,
                        open ? "ShopWindow" : "Shutter", false);
                if (!open)
                    for (int k = 0; k < 12; k++)
                        C.Panel(u, "Rib_" + k, x0 + 0.3f, x0 + UNIT - 0.3f, zf + 0.1f, zf + 0.12f, 0.2f + k * 0.24f, 0.03f, "Iron", false);

                // กันสาด
                K.Box("Awning", u, new Vector3(xc, 3.35f, zf + 0.75f), new Vector3(UNIT - 0.1f, 0.06f, 1.5f),
                      (i % 2 == 0) ? "Tarp" : "Shutter", new Vector3(12f, 0f, 0f), false);

                // ป้ายร้าน
                if (!string.IsNullOrEmpty(signs[i % signs.Length]))
                {
                    K.Box("SignBoard", u, new Vector3(xc, 4.1f, zf + 0.15f), new Vector3(UNIT - 0.4f, 0.7f, 0.08f), "PlasticWhite", default(Vector3), false);
                    C.ThaiSign("SignText", u, new Vector3(xc, 4.1f, zf + 0.2f), new Vector2(UNIT - 0.6f, 0.55f),
                               signs[i % signs.Length], C.RedInk, 0f);
                }

                // หน้าต่างชั้นบน
                for (int f = 1; f < FLOORS; f++)
                    for (int w = 0; w < 2; w++)
                    {
                        float wx = x0 + 1.0f + w * 2.0f;
                        string mat = rng.Next(0, 3) == 0 ? "ShopWindow" : "GlassTint";
                        C.Panel(u, "Win_" + f + "_" + w, wx - 0.6f, wx + 0.6f, zf + 0.06f, zf + 0.1f, f * FLOOR_H + 0.9f, 1.6f, mat, false);
                        K.Box("AC_" + f + "_" + w, u, new Vector3(wx + 0.25f, f * FLOOR_H + 0.55f, zf + 0.3f), new Vector3(0.7f, 0.5f, 0.3f),
                              "PlasticWhite", default(Vector3), false);
                    }
            }

            // รถเข็นขายของคลุมผ้าใบหลบฝน
            var cart = K.Group("FoodCart_Covered", _dress);
            cart.localPosition = new Vector3(-9f, 0f, FAR_WALK_Z0 + 1.2f);
            K.Box("Body", cart, new Vector3(0f, 0.5f, 0f), new Vector3(1.8f, 0.9f, 0.8f), "Chrome");
            K.Box("Tarp", cart, new Vector3(0f, 1.05f, 0f), new Vector3(1.95f, 0.25f, 0.95f), "Tarp", new Vector3(0f, 0f, 3f), false);
            K.Cyl("Wheel_A", cart, new Vector3(-0.6f, 0.15f, 0.42f), 0.3f, 0.06f, "Black", new Vector3(90f, 0f, 0f));
            K.Cyl("Wheel_B", cart, new Vector3(0.6f, 0.15f, 0.42f), 0.3f, 0.06f, "Black", new Vector3(90f, 0f, 0f));
        }

        // ───────────────────────── เสาไฟฟ้าสายระโยงระยาง ─────────────────────────
        static void PowerLines()
        {
            var g = K.Group("PowerLines", _dress);
            float z = ROAD_Z0 - 0.6f;
            float[] px = { -30f, -18f, -6f, 6f, 18f, 30f };
            float[] wireY = { 8.3f, 8.0f, 7.7f, 7.0f, 6.6f, 6.2f };
            for (int i = 0; i < px.Length; i++)
            {
                var p = K.Group("Pole_" + i, g);
                p.localPosition = new Vector3(px[i], 0f, z);
                K.Box("Pole", p, new Vector3(0f, 4.4f, 0f), new Vector3(0.3f, 8.8f, 0.3f), "Concrete");
                K.Box("CrossArm", p, new Vector3(0f, 8.2f, 0f), new Vector3(1.8f, 0.1f, 0.1f), "Iron", default(Vector3), false);
                K.Box("CrossArm_Low", p, new Vector3(0f, 6.9f, 0f), new Vector3(1.2f, 0.1f, 0.1f), "Iron", default(Vector3), false);
                // โคมไฟถนนแขวนเสาไฟฟ้าแบบไทย ยื่นออกถนน
                C.Rod(p, "LampArm", new Vector3(0f, 5.8f, 0f), new Vector3(0f, 6.1f, 1.8f), 0.07f, "Iron");
                K.Box("LampHead", p, new Vector3(0f, 6.05f, 1.9f), new Vector3(0.3f, 0.12f, 0.6f), "Iron", default(Vector3), false);
                K.Box("LampLens", p, new Vector3(0f, 5.98f, 1.9f), new Vector3(0.26f, 0.03f, 0.5f), "SodiumLamp", default(Vector3), false);
                if (i == 2)
                {
                    K.Cyl("Transformer", p, new Vector3(0.5f, 7.4f, 0f), 0.6f, 1.0f, "Concrete");
                    K.Box("CableBox", p, new Vector3(0f, 3.2f, 0.25f), new Vector3(0.4f, 0.6f, 0.2f), "Concrete", default(Vector3), false);
                }
                // กองสายที่ม้วนเก็บไว้บนเสา (ลักษณะเด่นของเสาไฟฟ้าไทย)
                K.Cyl("CableCoil", p, new Vector3(0f, 5.2f, 0.2f), 0.7f, 0.18f, "Black", new Vector3(90f, 0f, 0f));

                if (i == px.Length - 1) continue;
                // สายหย่อนกลางช่วง — สองท่อนต่อช่วงก็พอให้เห็นว่าห้อย
                for (int w = 0; w < wireY.Length; w++)
                {
                    float xOff = (w % 3 - 1) * 0.7f;
                    Vector3 a = new Vector3(px[i] + xOff * 0.2f, wireY[w], z + xOff * 0.1f);
                    Vector3 b = new Vector3(px[i + 1] + xOff * 0.2f, wireY[w] + (w % 2 == 0 ? 0.1f : -0.15f), z + xOff * 0.1f);
                    Vector3 m = (a + b) * 0.5f + Vector3.down * (0.45f + w * 0.12f);
                    C.Rod(g, "Wire_" + i + "_" + w + "_A", a, m, 0.025f, "Black");
                    C.Rod(g, "Wire_" + i + "_" + w + "_B", m, b, 0.025f, "Black");
                }
            }
        }

        // ───────────────────────── รถหรูสีดำ (Post-Credit) ─────────────────────────
        /// <summary>
        /// รถของหมออรินทร์จอดติดเครื่องฝั่งตรงข้ามศาล หันหัวไปทางตะวันตก
        /// ด้านขวาของรถ (local +X) จึงหันเข้าหาศาล — หมออรินทร์นั่งเบาะหลังขวา มองผ่านกระจกฟิล์มไปที่เข้ม
        /// รถพวงมาลัยขวาแบบไทย คนขับนั่งหน้าขวา
        /// </summary>
        static void ArinCar()
        {
            var g = K.Group("ArinCar", _dress);
            g.localPosition = CAR_POS;
            g.localEulerAngles = new Vector3(0f, -90f, 0f);

            // ตัวถัง (ภาพ) — collider ก้อนเดียวคลุมทั้งคัน สูงเลยเพดานตรวจ กันขึ้นไปยืนบนหลังคารถ
            K.Box("Body", g, new Vector3(0f, 0.6f, 0f), new Vector3(1.9f, 0.7f, 5.1f), "CarBlack", default(Vector3), false);
            K.Box("Hood", g, new Vector3(0f, 0.97f, 1.75f), new Vector3(1.84f, 0.06f, 1.5f), "CarBlack", new Vector3(3f, 0f, 0f), false);
            K.Box("Trunk", g, new Vector3(0f, 0.97f, -2.05f), new Vector3(1.84f, 0.06f, 0.95f), "CarBlack", default(Vector3), false);
            K.Box("Cabin_Glass", g, new Vector3(0f, 1.2f, -0.2f), new Vector3(1.72f, 0.5f, 2.7f), "GlassTint", default(Vector3), false);
            K.Box("Roof", g, new Vector3(0f, 1.47f, -0.25f), new Vector3(1.68f, 0.05f, 2.3f), "CarBlack", default(Vector3), false);
            for (int s = -1; s <= 1; s += 2)
            {
                K.Box("Pillar_A_" + s, g, new Vector3(s * 0.84f, 1.2f, 1.08f), new Vector3(0.06f, 0.5f, 0.08f), "CarBlack", default(Vector3), false);
                K.Box("Pillar_B_" + s, g, new Vector3(s * 0.87f, 1.2f, -0.1f), new Vector3(0.05f, 0.5f, 0.1f), "CarBlack", default(Vector3), false);
                K.Box("Pillar_C_" + s, g, new Vector3(s * 0.84f, 1.2f, -1.48f), new Vector3(0.06f, 0.5f, 0.1f), "CarBlack", default(Vector3), false);
                K.Box("Mirror_" + s, g, new Vector3(s * 1.02f, 1.02f, 0.95f), new Vector3(0.16f, 0.1f, 0.06f), "CarBlack", default(Vector3), false);
                K.Box("Headlight_" + s, g, new Vector3(s * 0.62f, 0.78f, 2.56f), new Vector3(0.42f, 0.12f, 0.03f), "LedWarm", default(Vector3), false);
                K.Box("Taillight_" + s, g, new Vector3(s * 0.66f, 0.82f, -2.56f), new Vector3(0.38f, 0.1f, 0.03f), "TailLight", default(Vector3), false);
            }
            K.Box("Grille", g, new Vector3(0f, 0.68f, 2.56f), new Vector3(0.6f, 0.26f, 0.03f), "Chrome", default(Vector3), false);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -0.9f : 0.9f;
                float zz = (i < 2) ? 1.6f : -1.55f;
                K.Cyl("Wheel_" + i, g, new Vector3(x, 0.36f, zz), 0.72f, 0.26f, "Black", new Vector3(0f, 0f, 90f));
                K.Cyl("Rim_" + i, g, new Vector3(x * 1.08f, 0.36f, zz), 0.42f, 0.04f, "Chrome", new Vector3(0f, 0f, 90f));
            }
            C.Blocker(g, "Collider", new Vector3(0f, 1.4f, 0f), new Vector3(1.95f, 2.8f, 5.15f));

            // ── ภายในรถ ──
            var cab = K.Group("Interior", g);
            K.Box("RearSeat", cab, new Vector3(0f, 0.38f, -0.9f), new Vector3(1.6f, 0.25f, 0.55f), "Leather", default(Vector3), false);
            K.Box("RearSeat_Back", cab, new Vector3(0f, 0.8f, -1.22f), new Vector3(1.6f, 0.7f, 0.12f), "Leather", new Vector3(-8f, 0f, 0f), false);
            K.Box("FrontSeat_R", cab, new Vector3(0.42f, 0.75f, 0.35f), new Vector3(0.55f, 0.8f, 0.12f), "Leather", new Vector3(-8f, 0f, 0f), false);
            K.Box("FrontSeat_L", cab, new Vector3(-0.42f, 0.75f, 0.35f), new Vector3(0.55f, 0.8f, 0.12f), "Leather", new Vector3(-8f, 0f, 0f), false);
            K.Box("Steering", cab, new Vector3(0.42f, 0.95f, 0.95f), new Vector3(0.38f, 0.38f, 0.04f), "Black", new Vector3(-60f, 0f, 0f), false);

            // โต๊ะพับหลังเบาะหน้า + ชุดน้ำชาของหมออรินทร์
            var tea = K.Group("TeaSet", cab);
            tea.localPosition = new Vector3(0.42f, 0.98f, 0.12f);
            K.Box("Tray", tea, Vector3.zero, new Vector3(0.42f, 0.02f, 0.28f), "Wood", default(Vector3), false);
            K.Sphere("Teapot", tea, new Vector3(-0.1f, 0.07f, 0f), 0.13f, "Porcelain");
            C.Rod(tea, "Teapot_Spout", new Vector3(-0.04f, 0.07f, 0f), new Vector3(0.04f, 0.12f, 0f), 0.02f, "Porcelain");
            K.Cyl("Cup", tea, new Vector3(0.1f, 0.04f, 0.02f), 0.075f, 0.06f, "Porcelain");
            K.Marker("MARK_TeaCup", tea, new Vector3(0.1f, 0.08f, 0.02f));

            // หมออรินทร์ (เบาะหลังขวา ฝั่งศาล) + คนขับ
            C.Person(_actors, "Dr_Arin", g.TransformPoint(new Vector3(0.42f, 0.5f, -0.85f)), -90f, "SuitBlack", true, 0.92f);
            C.Person(_actors, "Arin_Driver", g.TransformPoint(new Vector3(0.42f, 0.5f, 0.55f)), -90f, "SuitBlack", true, 0.92f);

            K.AddLight(g, "Interior_Glow", new Vector3(0f, 1.38f, -0.5f), Vector3.zero, LightType.Point,
                       new Color(1f, 0.82f, 0.6f), 0.5f, 1.8f);
            // ไฟท้ายสะท้อนพื้นเปียก — ระยะสั้นพอไม่ให้แสงแดงลามเข้าไปในห้องโดยสาร
            K.AddLight(g, "TailGlow", new Vector3(0f, 0.5f, -3.2f), Vector3.zero, LightType.Point,
                       new Color(1f, 0.08f, 0.05f), 1.2f, 2.4f);
        }

        // ───────────────────────── lighting ─────────────────────────
        static void Lighting()
        {
            var g = K.Group("KeyLights", _light);

            // แสงฟ้าครึ้มจากด้านบน อ่อนและเย็น เงานุ่ม
            var sun = K.AddLight(g, "Overcast", new Vector3(0f, 30f, 0f), new Vector3(62f, -28f, 0f),
                                 LightType.Directional, new Color(0.62f, 0.68f, 0.78f), 0.38f, 0f, 60f, true);
            sun.shadowStrength = 0.35f;
            RenderSettings.sun = sun;

            // ไฟถนนโซเดียมสีส้ม — ตัวที่ใกล้ท่อระบายน้ำคือไฟหลักของฉากพิธีกรรม
            float[] lx = { -18f, -6f, 6f, 18f };
            for (int i = 0; i < lx.Length; i++)
                K.AddLight(g, "StreetLamp_" + i, new Vector3(lx[i], 6.9f, SIDEWALK_Z0 - 1.2f), new Vector3(90f, 0f, 0f),
                           LightType.Spot, new Color(1f, 0.64f, 0.32f), 14f, 13f, 110f, i == 1);
            float[] fx = { -18f, -6f, 6f, 18f };
            for (int i = 0; i < fx.Length; i++)
                K.AddLight(g, "FarLamp_" + i, new Vector3(fx[i], 5.9f, ROAD_Z0 + 1.3f), new Vector3(90f, 0f, 0f),
                           LightType.Spot, new Color(1f, 0.64f, 0.32f), 9f, 10f, 110f);

            // ไฟใต้มุขหน้า อุ่น ๆ ตัดกับฝนสีเทาข้างนอก
            for (int i = 0; i < 3; i++)
                K.AddLight(g, "Portico_" + i, new Vector3(-5f + i * 5f, COL_TOP - 0.3f, 12f), new Vector3(90f, 0f, 0f),
                           LightType.Spot, new Color(1f, 0.85f, 0.62f), 7f, 10f, 120f);
            // ไฟโคมหัวเสาตีนบันได
            for (int s = -1; s <= 1; s += 2)
                K.AddLight(g, "PierLamp_" + s, new Vector3(s * (STAIR_HX + PORTICO_HX) * 0.5f, 2.6f, STAIR_Z0 - 0.45f), Vector3.zero,
                           LightType.Point, new Color(1f, 0.7f, 0.4f), 2.5f, 6f);
            // ไฟส่องหน้าบันจากลานด้านล่าง
            K.AddLight(g, "Pediment_Wash", new Vector3(0f, 1f, -3f), new Vector3(-48f, 0f, 0f), LightType.Spot,
                       new Color(1f, 0.86f, 0.66f), 14f, 30f, 50f);
        }

        // ───────────────────────── นักแสดง ─────────────────────────
        static void Actors()
        {
            // ป้าสมรออกจากประตูศาลพร้อมเข้ม แล้วเดินลงบันไดไปทรุดที่ลาน (ดูเส้นทางใน BEATS)
            C.Person(_actors, "Aunt_Samorn", new Vector3(1.8f, PODIUM_Y, FACADE_Z - 2.2f), 180f, "AuntCloth", false);
            // รปภ. ศาลยืนหลบฝนใต้มุข
            C.Person(_actors, "CourtGuard", new Vector3(-7.5f, PODIUM_Y, FACADE_Z - 1.6f), 150f, "PoliceKhaki", false);
        }

        // ───────────────────────── gameplay ─────────────────────────
        static void Gameplay()
        {
            var g = _play;

            // เข้มเดินออกจากประตูใหญ่ หันหน้าลงบันได เห็นถนนกับรถสีดำฝั่งตรงข้ามตั้งแต่เฟรมแรก
            var spawn = new Vector3(0f, PODIUM_Y, FACADE_Z - 2.5f);
            K.Marker("PlayerSpawn_CourtDoors", g, spawn + Vector3.up * 0.2f, 180f);
            K.PlacePlayer(C.P_PLAYER, g, new Vector3(spawn.x, PODIUM_Y + 0.02f, spawn.z), 180f);

            K.Marker("NAV_Portico", g, new Vector3(-4f, PODIUM_Y, 12f));
            K.Marker("NAV_Terrace_W", g, new Vector3(-16f, PODIUM_Y, 12f));
            K.Marker("NAV_Terrace_E", g, new Vector3(16f, PODIUM_Y, 12f));
            K.Marker("NAV_StairTop", g, new Vector3(0f, PODIUM_Y, PODIUM_Z0 + 0.8f));
            K.Marker("NAV_StairFoot", g, new Vector3(0f, 0f, STAIR_Z0 - 1.2f));
            K.Marker("NAV_Plaza", g, new Vector3(0f, 0f, -4f));
            K.Marker("NAV_Sidewalk", g, new Vector3(0f, 0f, SIDEWALK_Z0 + 1.6f));
            K.Marker("NAV_FarSidewalk", g, new Vector3(0f, 0f, FAR_WALK_Z0 + 1.5f));

            // ── บีท 4.3 - 4.5 ──
            var beats = K.Group("BEATS", g);
            C.Path(beats, "PATH_AuntDescend",
                   new Vector3(1.8f, PODIUM_Y, FACADE_Z - 2.2f), new Vector3(1.8f, PODIUM_Y, PODIUM_Z0 + 0.6f),
                   new Vector3(1.4f, 0f, STAIR_Z0 - 0.6f), AUNT_COLLAPSE);
            C.Mark(beats, "MARK_AuntCollapse", AUNT_COLLAPSE, 200f);                          // ทรุดร้องไห้ไร้เสียง
            C.Mark(beats, "MARK_KhemComfort", AUNT_COLLAPSE + new Vector3(-1.3f, 0f, 0.9f), 125f); // เข้มเข้าไปจะปลอบ
            C.Path(beats, "PATH_AuntWalkAway",
                   AUNT_COLLAPSE, new Vector3(6f, 0f, -6.5f), new Vector3(12f, 0f, SIDEWALK_Z0 + 1.4f),
                   new Vector3(HX - 2f, 0f, SIDEWALK_Z0 + 1.4f));
            // พิธีกรรม: ยืนริมฟุตบาทเหนือท่อระบายน้ำ หันหน้าไปทางถนน (และรถสีดำฝั่งตรงข้าม)
            C.Mark(beats, Act4Script.OBJ_Ritual, QTE_SPOT, 180f);
            C.Mark(beats, "MARK_PinLands", DRAIN_POS + new Vector3(0f, 0.02f, 0f));
            C.Mark(beats, "MARK_BloodDrip", QTE_SPOT + new Vector3(0.25f, 0.005f, -0.3f));

            // ── หมุดกล้อง ──
            var cams = K.Group("CAMERAS", g);
            C.CamMarker(cams, "CAM_StairsWide", new Vector3(7f, 1.3f, -7f), new Vector3(1.5f, 1.8f, 5f));
            C.CamMarker(cams, "CAM_AuntCollapse_Low", new Vector3(2.8f, 0.45f, -4.4f), AUNT_COLLAPSE + new Vector3(0f, 0.6f, 0f));
            // หลังไหล่ซ้ายเข้ม ห่างออกมา 2.5 m ตัวเข้มอยู่มุมล่างขวา ไม่บังป้าที่เดินจากไป
            C.CamMarker(cams, "CAM_AuntWalkAway_Back", AUNT_COLLAPSE + new Vector3(-2.75f, 2.1f, 3.0f), new Vector3(6f, 1.0f, -6.5f));
            C.CamMarker(cams, "CAM_QTE_LowHero", new Vector3(-1.7f, 0.25f, SIDEWALK_Z0 - 0.9f), QTE_SPOT + new Vector3(0f, 1.3f, 0f));
            C.CamMarker(cams, "CAM_Drain_Top", DRAIN_POS + new Vector3(0.4f, 0.9f, 0.6f), DRAIN_POS);
            C.CamMarker(cams, "CAM_KhemFromCar_Wide", new Vector3(CAR_POS.x - 1f, 1.6f, CAR_POS.z + 3f), QTE_SPOT + new Vector3(0f, 1.4f, 0f));
            C.CamMarker(cams, "CAM_EpisodeLogo_Crane", new Vector3(0f, 9f, -19f), new Vector3(0f, 7f, 10f));

            // Post-credit: หลังไหล่หมออรินทร์ในรถ มองผ่านกระจกฟิล์มดำไปที่เข้มริมฟุตบาท
            var car = GameObject.Find("ArinCar");
            if (car != null)
            {
                // หลังเบาะ เยื้องมาทางกลางรถ: หัวหมออรินทร์เป็นเงาอยู่ซ้ายเฟรม เข้มอยู่ในกรอบกระจกข้างหลังขวา
                // (ถ้าตั้งตรงหลังเขาพอดี หัวเขาจะบังเข้มมิด)
                Vector3 camPos = car.transform.TransformPoint(new Vector3(0.2f, 1.3f, -1.55f));
                C.CamMarker(cams, "CAM_PostCredit_BehindArin", camPos, QTE_SPOT + new Vector3(0f, 1.3f, 0f));
                C.CamMarker(cams, "CAM_PostCredit_TeaPour", car.transform.TransformPoint(new Vector3(0.05f, 1.2f, -0.45f)),
                            car.transform.TransformPoint(new Vector3(0.52f, 1.02f, 0.14f)));
            }

            // ── ระบบ ACT 4 ──
            var boot = new GameObject("~Bootstrap");
            boot.transform.SetParent(g, false);
            boot.AddComponent<Act4Bootstrap>();

            // ตัวแทนเข้ม (ชุดสูทดำตากฝน) — ปิดไว้ คัตซีนเปิดเอง
            var khem = C.Person(_actors, "Khem_StandIn", spawn, 180f, "SuitBlack", false);
            if (khem != null)
            {
                foreach (var c in khem.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                khem.SetActive(false);
            }

            var host = new GameObject("~Act4 CourtSteps");
            host.transform.SetParent(g, false);
            var qte = host.AddComponent<PinCrushQTE>();
            qte.skin = K.M("Skin");
            qte.gold = K.M("Gold");
            qte.blood = K.M("Blood");
            var seq = host.AddComponent<CourtStepsSequence>();
            seq.khemStandIn = khem;
            seq.data = C.DataAsset<Act4CourtStepsData>("Act4_CourtStepsData");
            seq.qte = qte;
            seq.teaMaterial = K.M("Tea");
        }

        // ───────────────────────── ฝน ─────────────────────────
        /// <summary>
        /// ฝนตกหนัก: เส้นฝนยืดตามความเร็ว + ละอองกระเด็นบนพื้น
        /// ตกเฉพาะส่วนหน้าหัวบันได ใต้มุขหน้าแห้ง แยกไว้นอก root ของด่าน
        /// ไม่งั้น bounds ของ particle ไปขยายพื้นที่ที่ LevelAudit ต้องกวาด
        /// </summary>
        static void Rain()
        {
            var weather = new GameObject("~Weather").transform;

            var rainMat = LoadOrCreateMat("M_C_Rain", "Universal Render Pipeline/Particles/Unlit");
            if (rainMat != null)
            {
                rainMat.SetFloat("_Surface", 1f);
                rainMat.SetFloat("_Blend", 0f);
                rainMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                rainMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                rainMat.SetFloat("_ZWrite", 0f);
                rainMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                rainMat.SetColor("_BaseColor", new Color(0.78f, 0.83f, 0.9f, 0.42f));
                rainMat.renderQueue = (int)RenderQueue.Transparent;
                EditorUtility.SetDirty(rainMat);
            }

            float zc = (FAR_WALK_Z0 - 8f + PODIUM_Z0) * 0.5f;
            float zs = PODIUM_Z0 - (FAR_WALK_Z0 - 8f);

            // ── เส้นฝน ──
            var rain = new GameObject("Rain");
            rain.transform.SetParent(weather, false);
            rain.transform.localPosition = new Vector3(0f, 18f, zc);
            var ps = rain.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 5f;
            main.startLifetime = 1.05f;
            main.startSpeed = 0f;
            main.startSize = 0.022f;
            main.startColor = new Color(0.8f, 0.85f, 0.92f, 0.5f);
            main.maxParticles = 20000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            var em = ps.emission;
            em.rateOverTime = 16000f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(HX * 2f, 0.5f, zs);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(1.4f);
            vel.y = new ParticleSystem.MinMaxCurve(-18f);
            vel.z = new ParticleSystem.MinMaxCurve(0.4f);
            var pr = rain.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.velocityScale = 0.055f;
            pr.lengthScale = 1f;
            pr.shadowCastingMode = ShadowCastingMode.Off;
            pr.receiveShadows = false;
            if (rainMat != null) pr.sharedMaterial = rainMat;

            // ── ละอองกระเด็นบนพื้น ──
            var splash = new GameObject("RainSplash");
            splash.transform.SetParent(weather, false);
            // กึ่งกลางระหว่างฟุตบาท (0) กับถนน (-0.15) ละอองจะโผล่ขึ้นจากผิวทั้งสองระดับ
            splash.transform.localPosition = new Vector3(0f, -0.08f, (FAR_WALK_Z0 + STAIR_Z0) * 0.5f);
            var sp = splash.AddComponent<ParticleSystem>();
            sp.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var sm = sp.main;
            sm.loop = true;
            sm.prewarm = true;
            sm.startLifetime = 0.22f;
            sm.startSpeed = 0f;
            sm.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            sm.startColor = new Color(0.85f, 0.88f, 0.92f, 0.35f);
            sm.maxParticles = 4000;
            sm.simulationSpace = ParticleSystemSimulationSpace.World;
            var se = sp.emission;
            se.rateOverTime = 5000f;
            var ss = sp.shape;
            ss.shapeType = ParticleSystemShapeType.Box;
            ss.scale = new Vector3(HX * 2f, 0.01f, STAIR_Z0 - FAR_WALK_Z0);
            var sv = sp.velocityOverLifetime;
            sv.enabled = true;
            sv.space = ParticleSystemSimulationSpace.World;
            sv.x = new ParticleSystem.MinMaxCurve(0f);
            sv.y = new ParticleSystem.MinMaxCurve(0.9f);
            sv.z = new ParticleSystem.MinMaxCurve(0f);
            var sz = sp.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.2f));
            var spr = splash.GetComponent<ParticleSystemRenderer>();
            spr.renderMode = ParticleSystemRenderMode.Billboard;
            spr.shadowCastingMode = ShadowCastingMode.Off;
            spr.receiveShadows = false;
            if (rainMat != null) spr.sharedMaterial = rainMat;

            ps.Play();
            sp.Play();
        }
    }
}
