using SecretsThatBreathe.Act2;
using SecretsThatBreathe.Act3;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using K = SecretsThatBreathe.LevelTools.LevelKit;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// ต่อเกมเพลย์ ACT 3 เข้ากับซีน Penthouse ที่สร้าง/แก้ไว้แล้ว
    ///
    /// ตั้งใจให้ "เติม" ของลงซีนเดิม ไม่ใช่ generate ซีนใหม่ทับ — ในซีนมีของที่วางเองไว้แล้ว
    /// (Bodyguard1-4, Champ Spawn, ตำแหน่งประตูที่ขยับเอง) รันซ้ำได้เรื่อย ๆ ไม่สร้างซ้ำ
    ///
    /// Menu: Tools > Secrets That Breathe > Wire Chapter 3 Penthouse
    /// </summary>
    public static class Ch3PenthouseWiring
    {
        public const string ScenePath = Ch3PenthouseBuilder.ScenePath;

        const string OBJ_ENTER = "OBJ_A3_01_EnterHouse";
        const string OBJ_UPSTAIRS = "OBJ_A3_02_ReachUpstairs";
        const string OBJ_HIDE = "OBJ_A3_03_Hide";
        const string OBJ_OVERHEAR = "OBJ_A3_04_Overhear";
        const string OBJ_ESCAPE = "OBJ_A3_05_Escape";

        [MenuItem("Tools/Secrets That Breathe/Wire Chapter 3 Penthouse", false, 14)]
        public static void Wire()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Ch3Wire] leave play mode first."); return; }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var host = Root("=== ACT3 SYSTEMS ===");
            Bootstrap(host);
            Bodyguards();
            var eaves = Eavesdrop(host);
            var arrival = ChampArrival(host, eaves);
            HideAtDresser(arrival, eaves);
            Doors();
            ReachUpstairs(host);
            EscapeExit();
            OpenLiftShaft();   // ต้องอยู่หลัง Doors/EscapeExit เพราะย้ายของที่สองตัวนั้นสร้างไว้

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Ch3Wire] ต่อระบบ ACT 3 เข้าซีนเรียบร้อย -> " + ScenePath);
        }

        // ───────────────────────── helpers ─────────────────────────

        /// <summary>หา GameObject ในซีนตามชื่อ (ตัดช่องว่างหัวท้าย เพราะบางตัวตั้งชื่อมีเว้นวรรคนำ)</summary>
        static Transform Find(string name)
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name.Trim() == name) return all[i];
            return null;
        }

        static Transform Root(string name)
        {
            var t = Find(name);
            if (t != null) return t;
            return new GameObject(name).transform;
        }

        static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static T Ensure<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        /// <summary>
        /// หมุดที่ "ตั้งตำแหน่งให้เฉพาะตอนสร้างใหม่"
        /// รันซ้ำจะไม่ไปทับตำแหน่งที่ลากจัดเองไว้แล้ว
        /// </summary>
        static Transform Point(Transform parent, string name, Vector3 worldPos, Quaternion rot)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing;

            var t = Child(parent, name);
            t.SetPositionAndRotation(worldPos, rot);
            return t;
        }

        // ───────────────────────── ระบบแกนกลาง ─────────────────────────

        /// <summary>
        /// ACT 3 ใช้ความสามารถชุดเดียวกับ ACT 2 (หมอบ, ค่าซ่อนตัว, HUD, ระบบยาม)
        /// จึงเปลี่ยนจาก SceneBootstrap มาใช้ Act2Bootstrap ที่ลงของครบกว่า
        /// </summary>
        static void Bootstrap(Transform host)
        {
            var old = Find("~Bootstrap");
            if (old != null)
            {
                var plain = old.GetComponent<SceneBootstrap>();
                if (plain != null) Object.DestroyImmediate(plain);
            }

            // Act2Bootstrap เสก AlertDirector ให้เองตอนรัน จึงไม่ต้องใส่ซ้ำตรงนี้
            var boot = Child(host, "Bootstrap").gameObject;
            Ensure<Act2Bootstrap>(boot);
            Ensure<MicNoiseDetector>(boot);
            Ensure<PenthouseIntro>(boot);
        }

        // ───────────────────────── ยาม ─────────────────────────

        /// <summary>
        /// ติดสายตา/การเดินตรวจให้บอดี้การ์ดทั้งสี่
        ///
        /// เส้นทางสร้างอิงตำแหน่งที่แต่ละตัวยืนอยู่ตอนนี้ (ไป-กลับ 6 m) ไม่ใช่พิกัดตายตัว
        /// เพราะตำแหน่งยามเป็นของที่วางเอง ไว้ค่อยลาก Point_00/Point_01 จัดเส้นทางจริงทีหลัง
        /// </summary>
        // โพสต์ประจำของยามทั้งสี่ (y = ระดับพื้นที่ต้องยืน) สองคนชั้นล่าง สองคนชั้นบน
        // ตอนวางเองไว้นอกตัวอาคารและต่ำกว่าพื้น 1.2 m ตรงนี้จับมาวางให้ยืนบนพื้นจริง
        static readonly Vector3[] PostA =
        {
            new Vector3( 12.0f, 0f,   -1.0f),   // 1 โถงทางเดินชั้นล่าง ฝั่งตะวันออก
            new Vector3( -4.5f, 0f,    2.8f),   // 2 โซนนั่งเล่น/ครัว ชั้นล่าง
            new Vector3( -9.5f, 3.6f, -9.5f),   // 3 ระเบียงริมสระ ชั้นบน
            new Vector3(  0.5f, 3.6f,  1.0f),   // 4 ห้องนั่งเล่นชั้นบน
        };
        static readonly Vector3[] PostB =
        {
            new Vector3(-10.0f, 0f,   -1.0f),
            new Vector3( 12.5f, 0f,    2.8f),
            new Vector3( -9.5f, 3.6f,  9.5f),
            new Vector3( 12.0f, 3.6f,  1.0f),
        };

        static void Bodyguards()
        {
            int found = 0;
            for (int n = 1; n <= 4; n++)
            {
                var t = Find("Bodyguard" + n);
                if (t == null) continue;
                int i = n - 1;
                found++;

                var go = t.gameObject;
                var vision = Ensure<GuardVision>(go);
                vision.viewDistance = 12f;
                vision.viewAngle = 95f;
                vision.proximityRadius = 1.6f;
                vision.secondsToCatch = 3.0f;
                vision.canHear = true;

                var patrol = Ensure<GuardPatrol>(go);
                patrol.speed = 1.4f;
                patrol.waitAtPoint = 2.5f;
                patrol.pingPong = true;

                // สร้างเส้นทางครั้งเดียว รันซ้ำจะไม่ไปทับหมุดที่ลากจัดไว้แล้ว
                if (t.Find("PatrolRoute") != null) continue;

                Ground(t, PostA[i], PostB[i], vision);
                var route = Child(t, "PatrolRoute");
                Point(route, "Point_00", PostA[i], Quaternion.identity);
                Point(route, "Point_01", PostB[i], Quaternion.identity);
            }
            if (found == 0) Debug.LogWarning("[Ch3Wire] ไม่พบ Bodyguard1-4 ในซีน");
        }

        /// <summary>
        /// วางยามให้ "เท้า" แตะพื้น แล้วตั้งจุดตาให้สูงจากพื้นราว 1.6 m
        ///
        /// จุดหมุนของแคปซูลอยู่กึ่งกลางตัว ไม่ใช่ที่เท้า วางตามพิกัดพื้นตรง ๆ ครึ่งตัวล่างจะจมพื้น
        /// วัดระยะจาก renderer จริงแทนการฝังค่าคงที่ พอเปลี่ยนไปใช้โมเดลคนจริงก็ยังวางถูก
        /// </summary>
        static void Ground(Transform t, Vector3 post, Vector3 lookTowards, GuardVision vision)
        {
            float pivotAboveFeet = 0f;
            var r = t.GetComponentInChildren<Renderer>();
            if (r != null) pivotAboveFeet = t.position.y - r.bounds.min.y;

            t.position = new Vector3(post.x, post.y + pivotAboveFeet, post.z);

            Vector3 flat = lookTowards - t.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f) t.rotation = Quaternion.LookRotation(flat.normalized);

            vision.eyeOffset = new Vector3(0f, Mathf.Max(0.2f, 1.6f - pivotAboveFeet), 0f);
        }

        // ───────────────────────── ฉากแอบฟัง ─────────────────────────

        static SilentEavesdropZone Eavesdrop(Transform host)
        {
            var go = Child(host, "Eavesdrop_PhoneCall").gameObject;
            var z = Ensure<SilentEavesdropZone>(go);
            z.objectiveId = OBJ_OVERHEAR;
            z.evidenceId = Act2Script.EV_ConfessionCall;
            return z;
        }

        static ChampArrivalSequence ChampArrival(Transform host, SilentEavesdropZone eaves)
        {
            var go = Child(host, "ChampArrival").gameObject;
            var seq = Ensure<ChampArrivalSequence>(go);
            seq.eavesdrop = eaves;

            var spawn = Find("Champ Spawn");
            if (spawn == null)
            {
                Debug.LogWarning("[Ch3Wire] ไม่พบหมุด 'Champ Spawn' — ข้ามการสร้างรถ");
                return seq;
            }
            seq.carSpawn = spawn;

            var stage = Child(host, "ChampStage");
            Vector3 s = spawn.position;

            // ลานจอด: แผ่นพื้นหน้าตึก ให้รถมีที่ให้จอดจริง ๆ ไม่ใช่ลอยกลางอากาศ
            if (stage.Find("CarPark") == null) BuildCarPark(stage, s);
            seq.carParkSpot = Point(stage, "CarParkSpot", s + new Vector3(-9f, 0f, 6f),
                                    Quaternion.Euler(0f, 300f, 0f));

            if (seq.car == null) seq.car = BuildCar(stage, s);
            if (seq.champ == null) seq.champ = BuildChamp(stage, seq.car.transform);

            seq.champExitSpot = Point(stage, "ChampExitSpot", seq.carParkSpot.position + new Vector3(-2.2f, 0f, 0f),
                                      Quaternion.identity);

            // เส้นทางเดินขึ้นบ้าน — หมุดคร่าว ๆ ให้ลากปรับตามผังจริงทีหลัง
            var path = Child(stage, "ChampWalkPath");
            var lobby = Find("NAV_LiftLobby");
            var stairFoot = Find("NAV_StairFoot");
            var stairTop = Find("NAV_StairTop");
            var bedroom = Find("NAV_SuiteBedroom");

            if (path.childCount == 0)
            {
                int i = 0;
                Point(path, "Point_" + (i++).ToString("00"), s + new Vector3(-14f, 0f, 10f), Quaternion.identity);
                if (lobby != null) Point(path, "Point_" + (i++).ToString("00"), lobby.position, Quaternion.identity);
                if (stairFoot != null) Point(path, "Point_" + (i++).ToString("00"), stairFoot.position, Quaternion.identity);
                if (stairTop != null) Point(path, "Point_" + (i++).ToString("00"), stairTop.position, Quaternion.identity);
            }

            var pts = new System.Collections.Generic.List<Transform>();
            foreach (Transform c in path) if (c.name.StartsWith("Point_")) pts.Add(c);
            pts.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            seq.walkPath = pts.ToArray();

            Vector3 phonePos = bedroom != null ? bedroom.position : new Vector3(8.5f, 3.6f, -7f);
            seq.phoneSpot = Point(stage, "ChampPhoneSpot", phonePos, Quaternion.Euler(0f, 180f, 0f));

            eaves.sequence = seq;
            return seq;
        }

        static void BuildCarPark(Transform parent, Vector3 near)
        {
            K.UseLibrary(Ch3PenthouseBuilder.MatFolder, "M_P_");
            var g = Child(parent, "CarPark");
            g.position = near;

            K.Box("Pad", g, new Vector3(-9f, -0.06f, 4f), new Vector3(22f, 0.12f, 14f), "Concrete");
            for (int i = 0; i < 5; i++)
                K.Box("Bay_" + i, g, new Vector3(-16f + i * 3.4f, 0.005f, 4f), new Vector3(0.12f, 0.02f, 5.4f),
                      "White", default(Vector3), false);
            K.Box("Kerb", g, new Vector3(-9f, 0.08f, 10.9f), new Vector3(22f, 0.16f, 0.3f), "White");
        }

        static GameObject BuildCar(Transform parent, Vector3 at)
        {
            K.UseLibrary(Ch3PenthouseBuilder.MatFolder, "M_P_");
            var g = Child(parent, "ChampCar");
            g.position = at;

            K.Box("Body", g, new Vector3(0f, 0.62f, 0f), new Vector3(1.9f, 0.75f, 4.5f), "Black");
            K.Box("Cabin", g, new Vector3(0f, 1.18f, -0.25f), new Vector3(1.7f, 0.62f, 2.3f), "Glass", default(Vector3), false);
            K.Box("Head_L", g, new Vector3(-0.62f, 0.72f, 2.22f), new Vector3(0.45f, 0.16f, 0.1f), "LedWarm", default(Vector3), false);
            K.Box("Head_R", g, new Vector3(0.62f, 0.72f, 2.22f), new Vector3(0.45f, 0.16f, 0.1f), "LedWarm", default(Vector3), false);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -0.92f : 0.92f;
                float z = (i < 2) ? 1.45f : -1.45f;
                K.Cyl("Wheel_" + i, g, new Vector3(x, 0.34f, z), 0.68f, 0.24f, "Black", new Vector3(0f, 0f, 90f));
            }
            K.AddLight(g, "Headlights", new Vector3(0f, 0.75f, 2.4f), Vector3.zero,
                       LightType.Spot, new Color(1f, 0.93f, 0.8f), 4f, 22f, 55f);
            return g.gameObject;
        }

        static GameObject BuildChamp(Transform parent, Transform car)
        {
            K.UseLibrary(Ch3PenthouseBuilder.MatFolder, "M_P_");
            // ต้องหาทั้งซีน ไม่ใช่แค่ใต้ stage — รันรอบก่อนย้ายตัวแชมป์ไปเป็นลูกของรถไปแล้ว
            var t = Find("Champ");
            if (t == null)
            {
                var go = new GameObject("Champ");
                go.transform.SetParent(parent, false);
                t = go.transform;
                // แคปซูลแทนตัวไปก่อน สูงเท่าคนจริงตาม Nav.HumanHeight
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body_PLACEHOLDER";
                body.transform.SetParent(t, false);
                body.transform.localPosition = new Vector3(0f, K.Nav.HumanHeight * 0.5f, 0f);
                body.transform.localScale = new Vector3(0.5f, K.Nav.HumanHeight * 0.5f, 0.5f);
                K.Paint(body, "FabricWarm");
                Object.DestroyImmediate(body.GetComponent<Collider>());
            }
            t.SetParent(car, false);
            t.localPosition = new Vector3(-0.4f, 0.2f, -0.2f);
            t.gameObject.SetActive(false);
            return t.gameObject;
        }

        // ───────────────────────── ที่ซ่อน ─────────────────────────

        static void HideAtDresser(ChampArrivalSequence seq, SilentEavesdropZone eaves)
        {
            var dresser = Find("Dresser");
            if (dresser == null) { Debug.LogWarning("[Ch3Wire] ไม่พบ Dresser"); return; }

            var hide = Ensure<HideSpot>(dresser.gameObject);
            hide.objectName = "ตู้เสื้อผ้า";
            hide.inspectText = "ซ่อนตัวรอฟังว่าแชมป์จะพูดอะไร";
            hide.objectiveId = OBJ_HIDE;
            hide.requiresObjective = OBJ_UPSTAIRS;
            hide.blockedLine = "ยังไม่มีเหตุผลให้ต้องซ่อนตอนนี้";
            hide.sequence = seq;

            // จุดยืนตอนซ่อน: มุดชิดตู้ หันหน้าออกมาทางห้อง (ตั้งครั้งแรกครั้งเดียว)
            hide.hideViewpoint = Point(dresser, "HideViewpoint",
                dresser.position + new Vector3(-0.9f, -0.35f, 0f), Quaternion.Euler(0f, -90f, 0f));
            hide.exitViewpoint = Point(dresser, "ExitViewpoint",
                dresser.position + new Vector3(-1.8f, -0.35f, 0f), Quaternion.Euler(0f, -90f, 0f));

            eaves.hideSpot = hide;
        }

        // ───────────────────────── ประตูลิฟต์ ─────────────────────────

        static void Doors()
        {
            var l = Find("Door_L");
            var r = Find("Door_R");
            if (l == null || r == null) { Debug.LogWarning("[Ch3Wire] ไม่พบ Door_L / Door_R"); return; }

            // บานประตูสร้างมาโดยไม่มี collider เลย — ไม่มีอะไรให้ raycast ชนเพื่อกด E
            // และไม่มีอะไรกันคนเดินทะลุตอนปิดด้วย จึงแยก collider ออกเป็นสองอันคนละหน้าที่:
            //   DoorInteract = trigger บาง ๆ สำหรับกด E อยู่ฝั่งนอก (ใกล้ทางที่ผู้เล่นเดินเข้ามา)
            //   DoorBlocker  = ทึบ กันเดินทะลุตอนปิด อยู่ฝั่งใน — SlidingDoor จะปิดใช้งานเองตอนเปิดประตู
            // ถ้าวางซ้อนตำแหน่งเดียวกัน raycast จะเจอแค่อันที่ใกล้กว่าเสมอ อีกอันโดนบังจนใช้ไม่ได้
            // เลยต้องแยกออกจากกันเล็กน้อยตามแนวลึกของประตู โดยให้ตัวกดอยู่ใกล้ผู้เล่นกว่าเสมอ
            Transform host = l.parent != null ? l.parent : l;
            float doorX = (l.position.x + r.position.x) * 0.5f;
            float doorY = (l.position.y + r.position.y) * 0.5f;
            float zc = (l.position.z + r.position.z) * 0.5f;
            float gapZ = Mathf.Abs(l.position.z - r.position.z);
            float clearH = Mathf.Max(Mathf.Abs(l.localScale.y), Mathf.Abs(r.localScale.y));

            var interact = Child(host, "DoorInteract");
            interact.position = new Vector3(doorX - 0.15f, doorY, zc);
            interact.rotation = Quaternion.identity;
            var iCol = Ensure<BoxCollider>(interact.gameObject);
            iCol.isTrigger = true;
            iCol.center = Vector3.zero;
            iCol.size = new Vector3(0.2f, clearH + 0.3f, gapZ + 1.2f);

            var blockerT = Child(host, "DoorBlocker");
            blockerT.position = new Vector3(doorX + 0.1f, doorY, zc);
            blockerT.rotation = Quaternion.identity;
            var bCol = Ensure<BoxCollider>(blockerT.gameObject);
            bCol.isTrigger = false;
            bCol.center = Vector3.zero;
            bCol.size = new Vector3(0.2f, clearH, gapZ + 0.3f);

            var door = Ensure<SlidingDoor>(interact.gameObject);
            door.objectName = "ประตูลิฟต์";
            door.objectiveId = OBJ_ENTER;
            door.leaves = new[] { l, r };
            door.blocker = bCol;
            // ใส่ค่าเริ่มต้นให้เฉพาะตอนยังไม่เคยตั้ง — ปรับระยะ/ทิศเองใน Inspector แล้วจะไม่โดนทับ
            if (door.openOffsets == null || door.openOffsets.Length != door.leaves.Length)
                door.openOffsets = new[] { new Vector3(0f, 0f, -10f), new Vector3(0f, 0f, -6f) };
        }

        // ───────────────────────── ขึ้นชั้นสอง / หนีออก ─────────────────────────

        static void ReachUpstairs(Transform host)
        {
            var top = Find("NAV_StairTop");
            if (top == null) { Debug.LogWarning("[Ch3Wire] ไม่พบ NAV_StairTop"); return; }

            var go = Child(host, "Reach_Upstairs").gameObject;
            go.transform.position = top.position;
            var box = Ensure<BoxCollider>(go);
            box.size = new Vector3(4f, 3f, 4f);
            box.isTrigger = true;

            var z = Ensure<Act2ReachZone>(go);
            z.objectiveId = OBJ_UPSTAIRS;
            z.noticeText = "ขึ้นมาถึงชั้นสองแล้ว — ห้องนอนอยู่ทางปีกใต้";
            z.setCheckpointHere = true;
        }

        /// <summary>
        /// ลิฟต์ตัวเดิมเคยกดแล้วโหลดกลับซีนคลับ — ตอนนี้เป็นทางหนีปิดบทแทน
        /// กดได้ต่อเมื่อฟังบทสนทนาจบแล้ว และกดแล้วจะไปจบที่ "จบ Act 3"
        /// </summary>
        static void EscapeExit()
        {
            var exit = Find("ExitToClub");
            if (exit == null) { Debug.LogWarning("[Ch3Wire] ไม่พบ ExitToClub"); return; }

            var oldDoor = exit.GetComponent<SceneExitDoor>();
            if (oldDoor != null) Object.DestroyImmediate(oldDoor);
            var oldStory = exit.GetComponent<StoryInteractable>();
            if (oldStory != null && !(oldStory is Act2Interactable)) Object.DestroyImmediate(oldStory);

            exit.name = "ExitToLift";
            var act = Ensure<Act2Interactable>(exit.gameObject);
            act.objectName = "ปุ่มเรียกลิฟต์";
            act.objectiveId = OBJ_ESCAPE;
            act.requiresObjective = OBJ_OVERHEAR;
            act.blockedLine = "ยังไม่ได้อะไรกลับไปเลย — ขึ้นไปดูข้างบนก่อน";
            act.inspectText = "ลงลิฟต์ไป ก่อนใครจะรู้ว่ามาที่นี่";
        }

        /// <summary>
        /// เจาะช่องลิฟต์ให้เดินเข้าไปได้จริง
        ///
        /// เดิมลิฟต์เป็นแค่แผ่นตกแต่งแปะหน้าผนัง: แผ่น Surround ทึบตันปิดเต็มช่องประตู
        /// และผนังกระจกด้านตะวันออกก็ยาวรวดไม่มีรู เปิดประตูแล้วก็ยังชนกำแพงอยู่ดี
        ///
        /// อีกปัญหาที่แก้พร้อมกันคือปุ่มเรียกลิฟต์เดิมอยู่ "หน้า" กล่องกดประตู
        /// เรย์ของ PlayerInteractor เลยไปโดนปุ่มลิฟต์ (ที่ยังถูกล็อกอยู่) ก่อนทุกครั้ง
        /// จนกดเปิดประตูไม่ได้เลย — ย้ายปุ่มเข้าไปไว้ผนังในห้องโดยสารแทน
        /// </summary>
        static void OpenLiftShaft()
        {
            var l = Find("Door_L");
            var r = Find("Door_R");
            if (l == null || r == null) return;

            K.UseLibrary(Ch3PenthouseBuilder.MatFolder, "M_P_");
            Transform host = l.parent != null ? l.parent : l;

            float doorX = l.position.x;
            float zc = (l.position.z + r.position.z) * 0.5f;
            float clearW = Mathf.Abs(l.position.z - r.position.z) + Mathf.Abs(l.localScale.z);
            float clearH = Mathf.Abs(l.localScale.y);
            float z0 = zc - clearW * 0.5f, z1 = zc + clearW * 0.5f;

            // ── วงกบแทนแผ่นทึบ ──
            var solid = host.Find("Surround");
            if (solid != null) Object.DestroyImmediate(solid.gameObject);

            if (host.Find("LiftFrame") == null)
            {
                var frame = Child(host, "LiftFrame");
                const float jamb = 0.45f, depth = 0.5f;
                K.Box("Jamb_S", frame, new Vector3(doorX, clearH * 0.5f, z0 - jamb * 0.5f),
                      new Vector3(depth, clearH, jamb), "StoneDark");
                K.Box("Jamb_N", frame, new Vector3(doorX, clearH * 0.5f, z1 + jamb * 0.5f),
                      new Vector3(depth, clearH, jamb), "StoneDark");
                K.Box("Header", frame, new Vector3(doorX, clearH + 0.35f, zc),
                      new Vector3(depth, 0.7f, clearW + jamb * 2f), "StoneDark");
            }

            // ── ห้องโดยสาร ──
            const float cabDepth = 2.2f;
            float cx0 = doorX + 0.25f, cx1 = cx0 + cabDepth;
            float cz0 = z0 - 0.3f, cz1 = z1 + 0.3f;
            float cabH = clearH + 0.4f;
            float czc = (cz0 + cz1) * 0.5f, czw = cz1 - cz0;

            if (host.Find("LiftCabin") == null)
            {
                var cab = Child(host, "LiftCabin");
                // พื้นยื่นเข้ามาใต้ธรณีประตู เชื่อมกับพื้นโถง ไม่งั้นมีร่องให้ตกระหว่างก้าวเข้า
                float fx0 = doorX - 0.5f;
                K.Box("Floor", cab, new Vector3((fx0 + cx1) * 0.5f, -0.1f, czc),
                      new Vector3(cx1 - fx0, 0.2f, czw), "StoneDark");
                K.Box("Ceiling", cab, new Vector3((cx0 + cx1) * 0.5f, cabH + 0.1f, czc),
                      new Vector3(cabDepth, 0.2f, czw), "StoneDark");
                K.Box("Back", cab, new Vector3(cx1 + 0.1f, cabH * 0.5f, czc),
                      new Vector3(0.2f, cabH, czw), "Brass");
                K.Box("Side_S", cab, new Vector3((cx0 + cx1) * 0.5f, cabH * 0.5f, cz0 - 0.1f),
                      new Vector3(cabDepth, cabH, 0.2f), "StoneDark");
                K.Box("Side_N", cab, new Vector3((cx0 + cx1) * 0.5f, cabH * 0.5f, cz1 + 0.1f),
                      new Vector3(cabDepth, cabH, 0.2f), "StoneDark");
                K.Box("Panel", cab, new Vector3(cx1 - 0.06f, 1.15f, cz0 + 0.5f),
                      new Vector3(0.06f, 0.34f, 0.2f), "LedWarm", default(Vector3), false);
                K.AddLight(cab, "CabinLight", new Vector3((cx0 + cx1) * 0.5f, cabH - 0.15f, czc),
                           new Vector3(90f, 0f, 0f), LightType.Point, new Color(1f, 0.9f, 0.72f), 2.2f, 5f);
            }

            // ── เจาะรูบนผนังกระจกให้ตรงกับช่องลิฟต์ ──
            var glass = Find("Glass_L1_E");
            if (glass != null && Find("Glass_L1_E_South") == null)
            {
                Vector3 gp = glass.position;
                Vector3 gs = glass.localScale;
                float gz0 = gp.z - gs.z * 0.5f, gz1 = gp.z + gs.z * 0.5f;

                var south = Object.Instantiate(glass.gameObject, glass.parent);
                south.name = "Glass_L1_E_South";
                south.transform.position = new Vector3(gp.x, gp.y, (gz0 + cz0) * 0.5f);
                south.transform.localScale = new Vector3(gs.x, gs.y, Mathf.Max(0.02f, cz0 - gz0));

                glass.position = new Vector3(gp.x, gp.y, (cz1 + gz1) * 0.5f);
                glass.localScale = new Vector3(gs.x, gs.y, Mathf.Max(0.02f, gz1 - cz1));
            }

            // ── ปุ่มเรียกลิฟต์ไปอยู่ผนังในห้องโดยสาร พ้นแนวกล่องกดประตู ──
            var exit = Find("ExitToLift");
            if (exit != null) exit.position = new Vector3(cx1 - 0.45f, 1.2f, czc);
        }
    }
}
