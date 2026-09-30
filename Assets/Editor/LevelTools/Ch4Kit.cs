using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using K = SecretsThatBreathe.LevelTools.LevelKit;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// ชิ้นส่วนที่ใช้ร่วมกันระหว่างสองซีนของ Chapter 4 (ห้องพิจารณาคดี + หน้าศาล)
    ///
    /// ลวดลายงานไม้แบบศาลไทยตามภาพอ้างอิง: ลูกฟักนูน, แถบเซาะร่องแนวตั้ง, ราวเจาะลาย "|O|O|"
    /// ป้ายภาษาไทย ตราชู และตัวแทนนักแสดง (แคปซูล) ที่นั่ง/ยืนตามตำแหน่งจริงในศาล
    ///
    /// ทุกฟังก์ชันใช้วัสดุจาก library ที่ <see cref="Materials"/> สร้างไว้ ต้องเรียกก่อนเสมอ
    /// </summary>
    public static class Ch4Kit
    {
        public const string DataFolder = "Assets/MainScenes/Main4_Court";
        public const string MatFolder = DataFolder + "/Materials";
        /// <summary>data asset ของบท ACT 4 (แก้ใน Inspector) — builder สร้างให้ครั้งแรก ไม่เขียนทับ</summary>
        public const string ScriptFolder = DataFolder + "/Data";

        public const string SceneCourtroom = "Main4_Courtroom";
        public const string SceneCourtSteps = "Main4_CourtSteps";
        public const string CourtroomPath = DataFolder + "/" + SceneCourtroom + ".unity";
        public const string CourtStepsPath = DataFolder + "/" + SceneCourtSteps + ".unity";

        // player ตัวเดียวกับบทก่อน — PlacePlayer จะย่อสเกลให้เท่า Nav.HumanHeight
        public const string P_PLAYER = "Assets/Champ&Kichzz/Prefab/Player/player.prefab";
        public const string P_NPC = "Assets/Champ&Kichzz/Prefab/Npc/NPC.prefab";

        // ฟอนต์เดียวกับซับไตเติลในเกม — ฟอนต์ default ของ TMP ไม่มีสระ/วรรณยุกต์ไทย ป้ายจะเป็นสี่เหลี่ยม
        const string THAI_FONT = "Assets/Champ&Kichzz/Fonts/PKFbasicDemo SDF.asset";

        public static readonly Color GoldInk = new Color(0.93f, 0.78f, 0.42f);
        public static readonly Color DarkInk = new Color(0.10f, 0.10f, 0.12f);
        public static readonly Color RedInk = new Color(0.70f, 0.08f, 0.08f);

        // ───────────────────────── materials ─────────────────────────

        /// <summary>วัสดุทั้งบท ใช้ชุดเดียวกันทั้งในห้องพิจารณาคดีและหน้าศาล</summary>
        public static void Materials()
        {
            K.UseLibrary(MatFolder, "M_C_");

            // ── งานไม้ศาล: มะฮอกกานีแดงเข้มแบบภาพอ้างอิง ──
            K.Mat("Wood", new Color(0.34f, 0.16f, 0.075f), 0f, 0.55f);
            K.Mat("WoodDark", new Color(0.19f, 0.085f, 0.04f), 0f, 0.5f);
            K.Mat("WoodLight", new Color(0.45f, 0.23f, 0.11f), 0f, 0.52f);

            // ── ผิวห้อง ──
            K.Mat("Plaster", new Color(0.87f, 0.86f, 0.83f), 0f, 0.2f);
            K.Mat("Ceiling", new Color(0.93f, 0.93f, 0.92f), 0f, 0.15f);
            K.Mat("Vinyl", new Color(0.47f, 0.64f, 0.66f), 0f, 0.6f);          // พื้นยางสีฟ้าอมเขียวแบบภาพอ้างอิง
            K.Mat("VinylDark", new Color(0.33f, 0.47f, 0.49f), 0f, 0.6f);
            K.Mat("Carpet", new Color(0.34f, 0.06f, 0.07f), 0f, 0.08f);        // พรมแดงเลือดหมูบนบัลลังก์

            // ── ของตกแต่ง ──
            K.Mat("Leather", new Color(0.05f, 0.05f, 0.055f), 0.1f, 0.55f);
            K.Mat("Gold", new Color(0.85f, 0.66f, 0.26f), 0.95f, 0.72f);
            K.Mat("Crimson", new Color(0.45f, 0.05f, 0.06f), 0f, 0.35f);
            K.Mat("Chrome", new Color(0.75f, 0.76f, 0.78f), 0.95f, 0.8f);
            K.Mat("Alu", new Color(0.70f, 0.715f, 0.735f), 0.88f, 0.72f);      // LevelKit.DoorLeaf ใช้ทำมือจับ
            K.Mat("Black", new Color(0.04f, 0.04f, 0.045f), 0.2f, 0.5f);
            K.Mat("PlasticWhite", new Color(0.92f, 0.92f, 0.90f), 0f, 0.45f);
            K.Mat("Paper", new Color(0.94f, 0.93f, 0.89f), 0f, 0.1f);
            K.Mat("FolderGreen", new Color(0.10f, 0.30f, 0.20f), 0f, 0.3f);    // แฟ้มคดีสีเขียวแบบราชการไทย
            K.Mat("FolderBlue", new Color(0.12f, 0.20f, 0.42f), 0f, 0.3f);
            K.Mat("RedPaint", new Color(0.62f, 0.04f, 0.05f), 0.4f, 0.8f);     // เศษกันชนสีแดง (ของกลาง)

            // ── เสื้อผ้าตัวแทนนักแสดง ──
            K.Mat("Robe", new Color(0.05f, 0.05f, 0.06f), 0f, 0.3f);           // ครุยผู้พิพากษา/ทนาย
            K.Mat("PoliceKhaki", new Color(0.44f, 0.31f, 0.18f), 0f, 0.3f);    // ตำรวจไทยชุดสีกากี
            K.Mat("OfficerWhite", new Color(0.86f, 0.86f, 0.82f), 0f, 0.3f);   // เจ้าหน้าที่หน้าบัลลังก์
            K.Mat("AuntCloth", new Color(0.36f, 0.30f, 0.44f), 0f, 0.2f);      // ป้าสมร
            K.Mat("ChampSuit", new Color(0.12f, 0.16f, 0.30f), 0.1f, 0.5f);    // แชมป์
            K.Mat("SuitBlack", new Color(0.08f, 0.08f, 0.09f), 0.1f, 0.4f);    // บอดี้การ์ด
            K.Mat("Civilian", new Color(0.46f, 0.40f, 0.33f), 0f, 0.25f);
            K.Mat("CivilianB", new Color(0.30f, 0.38f, 0.42f), 0f, 0.25f);
            K.Mat("Witness", new Color(0.52f, 0.47f, 0.30f), 0f, 0.25f);       // พยานเท็จ
            K.Mat("Skin", new Color(0.72f, 0.53f, 0.40f), 0f, 0.35f);

            // ── ภายนอก (หน้าศาลตอนฝนตก) ──
            K.Mat("Stone", new Color(0.62f, 0.61f, 0.58f), 0.05f, 0.78f);      // หินแกรนิตเปียก
            K.Mat("StoneDark", new Color(0.30f, 0.30f, 0.30f), 0.05f, 0.80f);
            K.Mat("Facade", new Color(0.84f, 0.82f, 0.76f), 0f, 0.45f);        // ผนังตึกศาลสีครีม
            K.Mat("Column", new Color(0.90f, 0.89f, 0.85f), 0f, 0.5f);
            K.Mat("RoofTile", new Color(0.52f, 0.20f, 0.10f), 0f, 0.55f);      // กระเบื้องหลังคาทรงไทยสีส้มอิฐ
            K.Mat("Asphalt", new Color(0.09f, 0.09f, 0.10f), 0f, 0.86f);       // ถนนเปียกสะท้อนไฟ
            K.Mat("Concrete", new Color(0.46f, 0.45f, 0.44f), 0f, 0.55f);
            K.Mat("CurbRed", new Color(0.66f, 0.07f, 0.06f), 0f, 0.6f);        // ขอบฟุตบาทขาวแดง
            K.Mat("CurbWhite", new Color(0.86f, 0.86f, 0.84f), 0f, 0.6f);
            K.Mat("LineYellow", new Color(0.85f, 0.66f, 0.10f), 0f, 0.6f);     // เส้นกลางถนนสีเหลืองแบบไทย
            K.Mat("LineWhite", new Color(0.88f, 0.88f, 0.86f), 0f, 0.6f);
            K.Mat("Iron", new Color(0.10f, 0.10f, 0.10f), 0.8f, 0.45f);        // ตะแกรงท่อระบายน้ำ
            K.Mat("Greenery", new Color(0.10f, 0.22f, 0.09f), 0f, 0.4f);
            K.Mat("Soil", new Color(0.12f, 0.09f, 0.07f), 0f, 0.3f);
            K.Mat("Bark", new Color(0.20f, 0.15f, 0.11f), 0f, 0.3f);
            K.Mat("CarBlack", new Color(0.015f, 0.015f, 0.018f), 0.85f, 0.92f); // รถหรูสีดำเงาวับ
            K.Mat("ShopFront", new Color(0.55f, 0.50f, 0.42f), 0f, 0.4f);      // ตึกแถวฝั่งตรงข้าม
            K.Mat("ShopFrontB", new Color(0.42f, 0.46f, 0.44f), 0f, 0.4f);
            K.Mat("Shutter", new Color(0.36f, 0.37f, 0.38f), 0.6f, 0.5f);      // ประตูม้วนเหล็ก
            K.Mat("Porcelain", new Color(0.92f, 0.91f, 0.87f), 0f, 0.85f);     // ถ้วยชาหมออรินทร์
            K.Mat("FlagRed", new Color(0.65f, 0.08f, 0.10f), 0f, 0.3f);
            K.Mat("FlagWhite", new Color(0.92f, 0.92f, 0.92f), 0f, 0.3f);
            K.Mat("FlagBlue", new Color(0.08f, 0.12f, 0.36f), 0f, 0.3f);
            K.Mat("Tarp", new Color(0.12f, 0.30f, 0.52f), 0f, 0.5f);           // ผ้าใบรถเข็นขายของ
            K.Mat("Blood", new Color(0.30f, 0.01f, 0.02f), 0f, 0.85f);         // เลือดจากมือเข้มตอนบีบเข็มกลัด
            K.Mat("Tea", new Color(0.36f, 0.18f, 0.06f), 0f, 0.92f);           // น้ำชาของหมออรินทร์

            K.MatTransparent("Glass", new Color(0.70f, 0.78f, 0.82f, 0.25f), 0.95f);
            K.MatTransparent("GlassTint", new Color(0.03f, 0.03f, 0.04f, 0.78f), 0.97f); // กระจกรถฟิล์มดำ
            K.MatTransparent("EvidenceBag", new Color(0.85f, 0.88f, 0.90f, 0.30f), 0.9f);
            K.MatTransparent("Puddle", new Color(0.05f, 0.06f, 0.08f, 0.55f), 1f);
            K.MatTransparent("GutterWater", new Color(0.30f, 0.34f, 0.38f, 0.45f), 1f);

            K.MatEmissive("LightPanel", new Color(0.95f, 0.96f, 1f), new Color(1f, 0.98f, 0.94f) * 2.2f);
            K.MatEmissive("WindowMorning", new Color(0.95f, 0.92f, 0.84f), new Color(1f, 0.93f, 0.78f) * 2.0f);
            K.MatEmissive("ScreenIdle", new Color(0.05f, 0.08f, 0.14f), new Color(0.10f, 0.22f, 0.45f) * 0.9f);
            K.MatEmissive("LedWarm", new Color(0.95f, 0.80f, 0.58f), new Color(1f, 0.82f, 0.55f) * 1.8f);
            K.MatEmissive("LedRed", new Color(0.9f, 0.1f, 0.08f), new Color(1f, 0.1f, 0.05f) * 2.5f);
            K.MatEmissive("SodiumLamp", new Color(1f, 0.72f, 0.40f), new Color(1f, 0.62f, 0.28f) * 3.2f);
            K.MatEmissive("ShopWindow", new Color(0.80f, 0.70f, 0.52f), new Color(1f, 0.80f, 0.50f) * 1.3f);
            K.MatEmissive("TailLight", new Color(0.7f, 0.05f, 0.05f), new Color(1f, 0.05f, 0.03f) * 1.6f);
        }

        /// <summary>
        /// วัสดุ URP/Unlit เป็น asset — ระบบรันไทม์ (คลิปกล้องหน้ารถ) ใช้เป็นต้นแบบ new Material()
        /// ต้องมี asset อ้างถึงเชเดอร์ไว้ ไม่งั้น Shader.Find ใน build จะหาไม่เจอ
        /// </summary>
        public static Material UnlitMat(string key, Color c)
        {
            string path = MatFolder + "/M_C_" + key + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) return null;
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ───────────────────────── geometry helpers ─────────────────────────

        /// <summary>แผ่นพื้น/เพดานแบบระบุขอบเขต topY = ผิวบน</summary>
        public static GameObject Slab(Transform p, string name, float x0, float x1, float z0, float z1,
                                      float topY, float thick, string mat, bool collider = true)
        {
            return K.Box(name, p, new Vector3((x0 + x1) * 0.5f, topY - thick * 0.5f, (z0 + z1) * 0.5f),
                         new Vector3(x1 - x0, thick, z1 - z0), mat, default(Vector3), collider);
        }

        /// <summary>ผนัง/แผงตั้งจากพื้น floorY สูง h ระบุด้วยขอบเขตในผัง</summary>
        public static GameObject Panel(Transform p, string name, float x0, float x1, float z0, float z1,
                                       float floorY, float h, string mat, bool collider = true)
        {
            return K.Box(name, p, new Vector3((x0 + x1) * 0.5f, floorY + h * 0.5f, (z0 + z1) * 0.5f),
                         new Vector3(Mathf.Max(0.02f, x1 - x0), h, Mathf.Max(0.02f, z1 - z0)),
                         mat, default(Vector3), collider);
        }

        /// <summary>collider ล่องหน ใช้กันเดินทะลุของที่ทำจากชิ้นเล็ก ๆ ไม่มี collider เอง</summary>
        public static GameObject Blocker(Transform parent, string name, Vector3 centre, Vector3 size, float yaw = 0f)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = centre;
            g.transform.localEulerAngles = new Vector3(0f, yaw, 0f);
            g.AddComponent<BoxCollider>().size = size;
            g.isStatic = true;
            return g;
        }

        /// <summary>แท่งกลมลากระหว่างสองจุด (พิกัด local ของ parent) — โซ่ตราชู ราวจับ สายไฟ</summary>
        public static GameObject Rod(Transform parent, string name, Vector3 a, Vector3 b, float diameter, string mat)
        {
            Vector3 d = b - a;
            var g = K.Cyl(name, parent, (a + b) * 0.5f, diameter, Mathf.Max(0.001f, d.magnitude), mat);
            if (d.sqrMagnitude > 1e-8f) g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            return g;
        }

        /// <summary>
        /// ปริซึมสามเหลี่ยม (หน้าจั่ว) ฐานกว้าง width สูง height หนา depth ตาม local Z
        /// ฐานอยู่ที่ y = 0 ของ pivot — primitive ของ Unity ไม่มีสามเหลี่ยม จึงสร้าง mesh เอง
        /// mesh ถูกเก็บลงในไฟล์ซีนไปพร้อมกัน ไม่ต้องทำเป็น asset แยก
        /// </summary>
        public static GameObject Gable(string name, Transform parent, Vector3 basePos, float width, float height,
                                       float depth, string mat, float yaw = 0f)
        {
            float w = width * 0.5f, d = depth * 0.5f;
            var v = new List<Vector3>();
            var t = new List<int>();

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 e)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(e);
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
                t.Add(i); t.Add(i + 2); t.Add(i + 3);
            }
            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c);
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
            }

            Vector3 lf = new Vector3(-w, 0f, -d), rf = new Vector3(w, 0f, -d), tf = new Vector3(0f, height, -d);
            Vector3 lb = new Vector3(-w, 0f, d), rb = new Vector3(w, 0f, d), tb = new Vector3(0f, height, d);
            Tri(lf, tf, rf);                 // หน้า (-Z)
            Tri(rb, tb, lb);                 // หลัง (+Z)
            Quad(lf, lb, tb, tf);            // ลาดซ้าย
            Quad(rf, tf, tb, rb);            // ลาดขวา
            Quad(lf, rf, rb, lb);            // ท้อง

            var mesh = new Mesh { name = name + "_Mesh" };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = basePos;
            g.transform.localEulerAngles = new Vector3(0f, yaw, 0f);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = g.AddComponent<MeshRenderer>();
            var m = K.M(mat);
            if (m != null) mr.sharedMaterial = m;
            g.isStatic = true;
            return g;
        }

        // ───────────────────────── งานไม้แบบศาลไทย ─────────────────────────

        /// <summary>
        /// แผงไม้ลูกฟัก: กรอบสีเข้ม + ลูกฟักนูนสีอ่อนกว่า
        /// centre = จุดบนผิวแผงที่จะแปะ, yaw = ทิศที่หน้าแผงหันออกไป (0 = หัน +Z)
        /// </summary>
        public static Transform RaisedPanel(Transform parent, string name, Vector3 centre, float w, float h, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = centre;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Frame", g, new Vector3(0f, 0f, 0.01f), new Vector3(w, h, 0.02f), "WoodDark", default(Vector3), false);
            K.Box("Field", g, new Vector3(0f, 0f, 0.025f), new Vector3(w - 0.12f, h - 0.12f, 0.02f), "WoodLight", default(Vector3), false);
            return g;
        }

        /// <summary>แถบเซาะร่องแนวตั้งแบบหน้าบัลลังก์ในภาพอ้างอิง ความยาววิ่งตาม local X</summary>
        public static Transform Flutes(Transform parent, string name, Vector3 centre, float length, float h,
                                       float yaw, float pitch = 0.07f)
        {
            var g = K.Group(name, parent);
            g.localPosition = centre;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Back", g, new Vector3(0f, 0f, 0.01f), new Vector3(length, h, 0.02f), "WoodDark", default(Vector3), false);
            int n = Mathf.Max(1, Mathf.FloorToInt(length / pitch));
            float start = -(n - 1) * pitch * 0.5f;
            for (int i = 0; i < n; i++)
                K.Box("Reed_" + i, g, new Vector3(start + i * pitch, 0f, 0.025f),
                      new Vector3(pitch * 0.55f, h - 0.03f, 0.02f), "Wood", default(Vector3), false);
            return g;
        }

        /// <summary>
        /// ราวไม้เจาะลาย "|O|O|" แบบศาลไทย (บนบัลลังก์ โต๊ะหน้าบัลลังก์ ราวกั้น)
        /// centre = กึ่งกลางขอบล่าง ความยาววิ่งตาม local X ไม่มี collider — ของทึบข้างล่างกันเดินอยู่แล้ว
        /// </summary>
        public static Transform PiercedRail(Transform parent, string name, Vector3 centre, float length, float h,
                                            float yaw, float depth = 0.06f)
        {
            var g = K.Group(name, parent);
            g.localPosition = centre;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Sill", g, new Vector3(0f, 0.025f, 0f), new Vector3(length, 0.05f, depth + 0.02f), "WoodDark", default(Vector3), false);
            K.Box("Cap", g, new Vector3(0f, h - 0.03f, 0f), new Vector3(length + 0.04f, 0.06f, depth + 0.06f), "Wood", default(Vector3), false);

            float band = h - 0.11f;
            float y = 0.05f + band * 0.5f;
            const float pitch = 0.12f;
            int n = Mathf.Max(2, Mathf.FloorToInt(length / pitch));
            float start = -(n - 1) * pitch * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float x = start + i * pitch;
                if (i % 2 == 0)
                    K.Box("Bar_" + i, g, new Vector3(x, y, 0f), new Vector3(0.028f, band, depth), "Wood", default(Vector3), false);
                else
                    K.Cyl("Ring_" + i, g, new Vector3(x, y, 0f), Mathf.Min(0.085f, band * 0.8f), depth, "Wood",
                          new Vector3(90f, 0f, 0f));
            }
            return g;
        }

        /// <summary>
        /// ตราชู — สัญลักษณ์ความยุติธรรม หน้าตราหัน local +Z
        /// tilt = องศาที่คานเอียง (0 = เที่ยงตรง) เผื่อใช้ทำฉาก "ตราชูที่ขึ้นสนิม" ตอนท้ายบท
        /// </summary>
        public static Transform Scales(Transform parent, string name, Vector3 centre, float s, float yaw, float tilt = 0f)
        {
            var g = K.Group(name, parent);
            g.localPosition = centre;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);

            K.Cyl("Post", g, new Vector3(0f, 0f, 0f), 0.05f * s, 1.0f * s, "Gold");
            K.Box("Base", g, new Vector3(0f, -0.49f * s, 0f), new Vector3(0.42f * s, 0.05f * s, 0.07f * s), "Gold", default(Vector3), false);
            K.Box("Foot", g, new Vector3(0f, -0.54f * s, 0f), new Vector3(0.62f * s, 0.05f * s, 0.09f * s), "Gold", default(Vector3), false);
            K.Sphere("Finial", g, new Vector3(0f, 0.53f * s, 0f), 0.10f * s, "Gold");

            var beam = K.Group("Beam", g);
            beam.localPosition = new Vector3(0f, 0.40f * s, 0f);
            beam.localEulerAngles = new Vector3(0f, 0f, tilt);
            K.Box("Bar", beam, Vector3.zero, new Vector3(0.92f * s, 0.035f * s, 0.035f * s), "Gold", default(Vector3), false);

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.44f * s;
                Vector3 top = new Vector3(x, 0f, 0f);
                Vector3 pan = new Vector3(x, -0.38f * s, 0f);
                Rod(beam, "Chain_" + side + "_A", top, pan + new Vector3(-0.11f * s, 0f, 0f), 0.012f * s, "Gold");
                Rod(beam, "Chain_" + side + "_B", top, pan + new Vector3(0.11f * s, 0f, 0f), 0.012f * s, "Gold");
                K.Cyl("Pan_" + side, beam, pan, 0.26f * s, 0.03f * s, "Gold");
            }
            return g;
        }

        // ───────────────────────── ป้าย ─────────────────────────

        /// <summary>
        /// ป้ายข้อความภาษาไทย — ใช้ K.Sign แล้วเปลี่ยนเป็นฟอนต์ที่มีสระไทย
        /// yaw = ทิศที่คนอ่านยืนอยู่ (0 = อ่านจากฝั่ง +Z, 180 = อ่านจากฝั่ง -Z)
        /// </summary>
        public static GameObject ThaiSign(string name, Transform parent, Vector3 pos, Vector2 box, string text,
                                          Color colour, float yaw, bool bold = true)
        {
            var g = K.Sign(name, parent, pos, box, text, colour, yaw, bold);
            var tmp = g.GetComponent<TMPro.TextMeshPro>();
            var f = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(THAI_FONT);
            if (tmp != null && f != null)
            {
                tmp.font = f;
                // ระยะห่างตัวอักษรของ K.Sign ดันสระบน/ล่างให้หลุดจากพยัญชนะ
                tmp.characterSpacing = 0f;
            }
            return g;
        }

        /// <summary>ป้ายชื่อตั้งโต๊ะ ไม้สีเข้มตัวหนังสือทอง หันหน้าไปทาง yaw</summary>
        public static void Nameplate(Transform parent, string name, Vector3 pos, float yaw, string text)
        {
            var g = K.Group(name, parent);
            g.localPosition = pos;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Block", g, new Vector3(0f, 0.05f, 0f), new Vector3(0.46f, 0.10f, 0.05f), "WoodDark", default(Vector3), false);
            ThaiSign("Text", g, new Vector3(0f, 0.05f, 0.028f), new Vector2(0.42f, 0.08f), text, GoldInk, 0f);
        }

        /// <summary>แผ่นป้ายติดผนัง พื้นขาวตัวแดง (ป้ายประกาศราชการ)</summary>
        public static void NoticePlate(Transform parent, string name, Vector3 pos, float yaw, Vector2 size, string text)
        {
            var g = K.Group(name, parent);
            g.localPosition = pos;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Plate", g, new Vector3(0f, 0f, 0.008f), new Vector3(size.x, size.y, 0.016f), "PlasticWhite", default(Vector3), false);
            ThaiSign("Text", g, new Vector3(0f, 0f, 0.018f), size * 0.86f, text, RedInk, 0f);
        }

        // ───────────────────────── เฟอร์นิเจอร์ ─────────────────────────

        /// <summary>เก้าอี้สำนักงานหนังดำ (ทนาย/เจ้าหน้าที่) คนนั่งหันไปทาง local +Z</summary>
        public static void OfficeChair(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Cyl("Base", g, new Vector3(0f, 0.04f, 0f), 0.62f, 0.06f, "Black");
            K.Cyl("Column", g, new Vector3(0f, 0.25f, 0f), 0.06f, 0.36f, "Chrome", default(Vector3), true);
            K.Box("Seat", g, new Vector3(0f, 0.48f, 0f), new Vector3(0.52f, 0.10f, 0.50f), "Leather", default(Vector3), false);
            K.Box("Back", g, new Vector3(0f, 0.86f, -0.24f), new Vector3(0.50f, 0.64f, 0.08f), "Leather", new Vector3(-6f, 0f, 0f), false);
            K.Box("Arm_L", g, new Vector3(-0.28f, 0.64f, 0f), new Vector3(0.05f, 0.05f, 0.40f), "Black", default(Vector3), false);
            K.Box("Arm_R", g, new Vector3(0.28f, 0.64f, 0f), new Vector3(0.05f, 0.05f, 0.40f), "Black", default(Vector3), false);
        }

        /// <summary>
        /// เก้าอี้ผู้พิพากษา: พนักพิงหนังดำสูง หัวพนักเป็นหน้าจั่วไม้ มีตราทองกลางจั่ว (ตามภาพอ้างอิง)
        /// </summary>
        public static void JudgeChair(Transform parent, string name, Vector3 p, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = p;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Plinth", g, new Vector3(0f, 0.20f, 0f), new Vector3(0.62f, 0.40f, 0.56f), "WoodDark");
            K.Box("Seat", g, new Vector3(0f, 0.46f, 0.02f), new Vector3(0.64f, 0.12f, 0.60f), "Leather", default(Vector3), false);
            K.Box("Back", g, new Vector3(0f, 1.02f, -0.28f), new Vector3(0.64f, 1.02f, 0.12f), "Leather", default(Vector3), false);
            K.Box("Arm_L", g, new Vector3(-0.35f, 0.70f, -0.02f), new Vector3(0.08f, 0.10f, 0.56f), "Wood", default(Vector3), false);
            K.Box("Arm_R", g, new Vector3(0.35f, 0.70f, -0.02f), new Vector3(0.08f, 0.10f, 0.56f), "Wood", default(Vector3), false);
            // หัวพนักทรงจั่ว
            K.Box("Crest", g, new Vector3(0f, 1.62f, -0.28f), new Vector3(0.70f, 0.22f, 0.10f), "Wood", default(Vector3), false);
            Gable("Crest_Peak", g, new Vector3(0f, 1.73f, -0.28f), 0.74f, 0.22f, 0.10f, "Wood");
            K.Cyl("Medallion", g, new Vector3(0f, 1.66f, -0.225f), 0.13f, 0.02f, "Gold", new Vector3(90f, 0f, 0f));
        }

        /// <summary>ม้านั่งไม้มีพนัก (ที่นั่งประชาชน/โจทก์/จำเลย) คนนั่งหันไปทาง local +Z</summary>
        public static void PublicBench(Transform parent, string name, Vector3 centre, float length, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = centre;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Seat", g, new Vector3(0f, 0.44f, 0f), new Vector3(length, 0.06f, 0.44f), "Wood", default(Vector3), false);
            K.Box("Apron", g, new Vector3(0f, 0.34f, 0.19f), new Vector3(length - 0.1f, 0.14f, 0.03f), "WoodDark", default(Vector3), false);
            K.Box("Back", g, new Vector3(0f, 0.78f, -0.23f), new Vector3(length, 0.46f, 0.05f), "Wood", new Vector3(-8f, 0f, 0f), false);
            K.Box("BackRail", g, new Vector3(0f, 1.02f, -0.27f), new Vector3(length + 0.04f, 0.05f, 0.08f), "WoodDark", default(Vector3), false);
            K.Box("End_L", g, new Vector3(-length * 0.5f, 0.50f, -0.03f), new Vector3(0.07f, 1.0f, 0.54f), "WoodDark", default(Vector3), false);
            K.Box("End_R", g, new Vector3(length * 0.5f, 0.50f, -0.03f), new Vector3(0.07f, 1.0f, 0.54f), "WoodDark", default(Vector3), false);
            // collider ก้อนเดียวคลุมทั้งม้านั่ง กันผู้เล่นมุดใต้ที่นั่งหรือติดขอบพนัก
            Blocker(g, "Collider", new Vector3(0f, 0.5f, -0.03f), new Vector3(length + 0.07f, 1.0f, 0.56f));
        }

        /// <summary>ไมโครโฟนคอห่านตั้งโต๊ะ ปลายไมค์ชี้ไปทาง local +Z</summary>
        public static void Mic(Transform parent, string name, Vector3 pos, float yaw)
        {
            var g = K.Group(name, parent);
            g.localPosition = pos;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Cyl("Base", g, new Vector3(0f, 0.012f, 0f), 0.11f, 0.024f, "Black");
            Rod(g, "Neck", new Vector3(0f, 0.02f, 0f), new Vector3(0f, 0.34f, 0.10f), 0.012f, "Black");
            Rod(g, "Head", new Vector3(0f, 0.34f, 0.10f), new Vector3(0f, 0.36f, 0.19f), 0.03f, "Black");
            K.Sphere("Led", g, new Vector3(0f, 0.03f, 0.05f), 0.014f, "LedRed");
        }

        /// <summary>จอภาพ กรอบดำ หน้าจอหัน local +Z — pos = กึ่งกลางจอ</summary>
        public static Transform Monitor(Transform parent, string name, Vector3 pos, float yaw, float w, float h,
                                        string screenMat = "ScreenIdle")
        {
            var g = K.Group(name, parent);
            g.localPosition = pos;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            K.Box("Bezel", g, Vector3.zero, new Vector3(w + 0.04f, h + 0.04f, 0.05f), "Black", default(Vector3), false);
            K.Box("Screen", g, new Vector3(0f, 0f, 0.027f), new Vector3(w, h, 0.005f), screenMat, default(Vector3), false);
            return g;
        }

        /// <summary>กองแฟ้มคดี + กระดาษบนโต๊ะ</summary>
        public static void FileStack(Transform parent, string name, Vector3 pos, float yaw, int count, string folderMat)
        {
            var g = K.Group(name, parent);
            g.localPosition = pos;
            g.localEulerAngles = new Vector3(0f, yaw, 0f);
            for (int i = 0; i < count; i++)
            {
                float jitter = (i % 2 == 0) ? 0.012f : -0.01f;
                K.Box("Folder_" + i, g, new Vector3(jitter, 0.012f + i * 0.026f, -jitter), new Vector3(0.24f, 0.024f, 0.33f),
                      i % 3 == 2 ? "Paper" : folderMat, new Vector3(0f, i * 3f, 0f), false);
            }
        }

        // ───────────────────────── นักแสดง ─────────────────────────

        /// <summary>
        /// ตัวแทนนักแสดง (แคปซูลจาก NPC.prefab) พร้อมแผ่นหน้าบอกทิศที่หัน
        ///
        /// seated = ย่อสูงเหลือราว 1.3 m ให้อ่านออกว่านั่ง และถอด collider ออก
        /// (คนนั่งทับเก้าอี้ ถ้าเหลือ collider จะไปขวางทางเดินและซ้อนกับเก้าอี้)
        /// ยืน = สูงเท่า Nav.HumanHeight เท่าผู้เล่น
        /// </summary>
        /// <param name="seatedHeight">ความสูงแคปซูลตอนนั่ง วัดจาก pos (ในรถเพดานต่ำ ต้องวางบนเบาะแล้วใช้ค่าน้อยลง)</param>
        public static GameObject Person(Transform parent, string name, Vector3 pos, float yaw, string mat, bool seated,
                                        float seatedHeight = 1.32f)
        {
            GameObject go = seated
                ? K.Place(P_NPC, parent, pos, yaw, 0f, 0f, new Vector3(0.5f, seatedHeight * 0.5f, 0.5f))
                : K.PlaceHuman(P_NPC, parent, pos, yaw);
            if (go == null) return null;
            go.name = name;

            var m = K.M(mat);
            if (m != null)
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = m;

            if (seated)
            {
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                // คนนั่งต้องซ้อนกับเบาะอยู่แล้ว ตัดลิงก์ prefab ออก LevelAudit จะได้ไม่นับเป็นของวางผิดที่
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            // หัวสีผิว + แผ่นหน้า: อ่านออกว่าเป็นคนและหันไปทางไหนตอนจัดมุมกล้อง
            // (ผู้พิพากษาใส่ครุยดำนั่งเก้าอี้หนังดำ ไม่มีหัวจะกลืนหายไปกับเก้าอี้)
            // หัวดันไปข้างหน้า 10 cm ให้โผล่พ้นโดมแคปซูล (อยู่ตรงกลางจะจมหายเหลือแค่กระหม่อม)
            // ตัวเลขต้องตรงกับ Act4Sequence.SetHeight ที่ปรับหัวตอนลุก/นั่งระหว่างคัตซีน
            Bounds b;
            if (K.TryBounds(go, out b))
            {
                Vector3 s = go.transform.lossyScale;
                Vector3 f = go.transform.forward;
                f.y = 0f;
                f.Normalize();
                Vector3 headPos = new Vector3(b.center.x, b.max.y - 0.14f, b.center.z) + f * 0.10f;
                var head = K.Sphere("Head", go.transform, Vector3.zero, 1f, "Skin", false);
                head.transform.position = headPos;
                head.transform.localScale = new Vector3(0.24f / s.x, 0.26f / s.y, 0.24f / s.z);

                var visor = K.Box("FacingVisor", go.transform, Vector3.zero, Vector3.one, "Black", default(Vector3), false, false);
                visor.transform.position = headPos + f * 0.115f + Vector3.up * 0.03f;
                visor.transform.localScale = new Vector3(0.16f / s.x, 0.045f / s.y, 0.04f / s.z);
            }
            return go;
        }

        // ───────────────────────── markers ─────────────────────────

        /// <summary>หมุดกล้องสำหรับ cutscene: ตั้งตำแหน่งแล้วหันมองจุดเป้าหมาย</summary>
        public static Transform CamMarker(Transform parent, string name, Vector3 pos, Vector3 lookAt)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            g.localPosition = pos;
            Vector3 d = lookAt - pos;
            if (d.sqrMagnitude > 1e-6f) g.localRotation = Quaternion.LookRotation(d.normalized);
            return g;
        }

        /// <summary>หมุดยืน/เดินของนักแสดง หันไปทาง yaw</summary>
        public static Transform Mark(Transform parent, string name, Vector3 pos, float yaw = 0f)
        {
            return K.Marker(name, parent, pos, yaw).transform;
        }

        /// <summary>เส้นทางเดินเป็นหมุด Point_00, Point_01 ... แบบเดียวกับ ChampWalkPath ของ ACT 3</summary>
        public static Transform Path(Transform parent, string name, params Vector3[] points)
        {
            var g = K.Group(name, parent);
            for (int i = 0; i < points.Length; i++) K.Marker("Point_" + i.ToString("00"), g, points[i]);
            return g;
        }

        // ───────────────────────── audit ─────────────────────────

        /// <summary>
        /// LevelAudit โดยปิด collider ทุกตัวบนตัวผู้เล่นระหว่างตรวจ
        ///
        /// ไม่งั้นตัวผู้เล่นเองขวางทางเดินตรงจุดเกิด — ทางเดินหลังห้องพิจารณาคดีลึกแค่ 2.5 m
        /// ผู้เล่นยืนกลางทางก็ตัดฝั่งตะวันออกขาดจากจุดเกิด ทั้งที่เดินได้จริง
        /// (player.prefab มี collider สองตัว: CharacterController + แคปซูลอีกอัน ต้องปิดทั้งคู่)
        /// </summary>
        public static string Audit(Transform root, string label, float ceiling)
        {
            var own = new List<Collider>();
            var rigs = root.GetComponentsInChildren<CharacterController>(true);
            for (int i = 0; i < rigs.Length; i++)
                foreach (var c in rigs[i].GetComponentsInChildren<Collider>(true))
                    if (c.enabled) own.Add(c);
            for (int i = 0; i < own.Count; i++) own[i].enabled = false;
            try
            {
                return LevelAudit.Format(label, LevelAudit.Run(root, 0.4f, ceiling));
            }
            finally
            {
                for (int i = 0; i < own.Count; i++) own[i].enabled = true;
            }
        }

        // ───────────────────────── data asset ─────────────────────────

        /// <summary>
        /// โหลด data asset ของบท ถ้ายังไม่มีสร้างใหม่ด้วยค่าเริ่มต้น — ไม่เขียนทับของที่แก้ไว้ใน Inspector
        /// (สร้างฉากใหม่กี่ครั้งบทที่แก้ไว้ก็ยังอยู่)
        /// </summary>
        public static T DataAsset<T>(string fileName, System.Action<T> init = null) where T : ScriptableObject
        {
            string path = ScriptFolder + "/" + fileName + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            if (!AssetDatabase.IsValidFolder(ScriptFolder))
                AssetDatabase.CreateFolder(DataFolder, "Data");
            asset = ScriptableObject.CreateInstance<T>();
            if (init != null) init(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[Ch4] สร้าง data asset: " + path);
            return asset;
        }

        // ───────────────────────── build settings ─────────────────────────

        /// <summary>ใส่ซีนลง Build Settings ถ้ายังไม่มี (SceneExitDoor โหลดซีนที่ไม่อยู่ในลิสต์ไม่ได้)</summary>
        public static void EnsureInBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < list.Count; i++)
                if (list[i].path == path) return;
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[Ch4] เพิ่มซีนลง Build Settings: " + path);
        }
    }
}
