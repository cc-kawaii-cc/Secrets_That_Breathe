using UnityEngine;
using SecretsThatBreathe.Act2;
using K = SecretsThatBreathe.LevelTools.LevelKit;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// ต่อระบบเกมเพลย์ของ ACT 2 เข้ากับด่านที่เพิ่ง generate เสร็จ
    ///
    /// ทุกอย่างอ้างอิงจาก marker ที่ตัว builder วางไว้อยู่แล้ว (OBJ_*, DOOR_*, Guard_*, Cover_*)
    /// ไม่มีพิกัดลอยๆ ในไฟล์นี้ ด่านขยับตรงไหนเกมเพลย์ก็ตามไปเอง
    /// </summary>
    public static class Ch2Act2Wiring
    {
        // ───────────────────────── helpers ─────────────────────────

        static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = Find(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static Transform Systems(Transform root)
        {
            var play = K.Category(root, K.Cat.Gameplay);
            var t = play.Find("ACT2_Systems");
            if (t != null) return t;
            return K.Group("ACT2_Systems", play);
        }

        static void Bootstrap(Transform root, bool stealthDebug = false)
        {
            var host = Systems(root).gameObject;
            if (host.GetComponent<Act2Bootstrap>() != null) return;
            var boot = host.AddComponent<Act2Bootstrap>();
            boot.showStealthDebug = stealthDebug;
        }

        /// <summary>กล่อง trigger ที่กด E ได้ วางทับ marker ของ objective นั้น</summary>
        static Act2Interactable Interact(Transform parent, string name, Vector3 worldPos, Vector3 size,
                                         string objectiveId, string inspectText,
                                         string requiresObjective = null, string evidenceId = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;      // raycast ของ PlayerInteractor ยังชนได้ แต่ไม่ขวางทางเดิน

            var it = go.AddComponent<Act2Interactable>();
            it.objectName = name;
            it.objectiveId = objectiveId;
            it.evidenceId = evidenceId;
            it.requiresObjective = requiresObjective;
            it.inspectText = inspectText;
            it.blockedLine = "ยังไม่ถึงขั้นนั้น";
            return it;
        }

        static Act2ReachZone Reach(Transform parent, string name, Vector3 worldPos, Vector3 size,
                                   string objectiveId, string notice,
                                   bool requireCrouching = false, string loadScene = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;

            var z = go.AddComponent<Act2ReachZone>();
            z.objectiveId = objectiveId;
            z.noticeText = notice;
            z.requireCrouching = requireCrouching;
            z.loadSceneOnComplete = loadScene;
            return z;
        }

        static void Shadow(Transform parent, string name, Vector3 worldPos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos + Vector3.up * size.y * 0.5f;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
            go.AddComponent<ShadowVolume>();
        }

        /// <summary>ทำให้ยามที่ builder วางไว้มีสายตาและเดินลาดตระเวนจริง</summary>
        static void ArmGuards(Transform root, float viewDistance, float viewAngle, float secondsToCatch)
        {
            var actors = K.Category(root, K.Cat.Actors);
            var guards = actors.Find("GUARDS");
            if (guards == null) return;

            foreach (Transform guard in guards)
            {
                if (!guard.name.StartsWith("Guard_")) continue;
                var vision = guard.gameObject.GetComponent<GuardVision>();
                if (vision == null) vision = guard.gameObject.AddComponent<GuardVision>();
                vision.viewDistance = viewDistance;
                vision.viewAngle = viewAngle;
                vision.secondsToCatch = secondsToCatch;
                vision.eyeOffset = new Vector3(0f, 1.55f, 0f);

                if (guard.gameObject.GetComponent<GuardPatrol>() == null)
                    guard.gameObject.AddComponent<GuardPatrol>();
            }
        }

        /// <summary>วางเขตกำบังทับจุดหลบที่ level design ทำ marker ไว้แล้ว</summary>
        static void ShadowsFromMarkers(Transform root, string groupName, string prefix, Vector3 size)
        {
            var play = K.Category(root, K.Cat.Gameplay);
            var group = Find(play, groupName);
            if (group == null) return;

            var host = K.Group("ACT2_CoverVolumes", Systems(root));
            foreach (Transform marker in group)
            {
                if (!marker.name.StartsWith(prefix)) continue;
                Shadow(host, "Shadow_" + marker.name, marker.position, size);
            }
        }

        // ───────────────────────── 2.1 อู่ซ่อมรถ ─────────────────────────

        public static void WireGarage(Transform root)
        {
            Bootstrap(root);
            var host = K.Group("ACT2_Beats", Systems(root));

            Vector3 Pos(string marker, Vector3 fallback)
            {
                var t = Find(root, marker);
                return t != null ? t.position : fallback;
            }

            // เล็งไปที่ตัว prop จริงถ้ามี — marker ของ objective ลอยสูงกว่าของจริงอยู่ราวครึ่งเมตร
            // ผู้เล่นเล็งไปที่แว่นขยายแล้วไม่เจออะไร เพราะกล่องอยู่เหนือหัวแว่นขึ้นไป
            Vector3 OnProp(string propName, string marker, Vector3 fallback)
            {
                var prop = Find(root, propName);
                if (prop == null) return Pos(marker, fallback);
                Bounds b;
                if (!K.TryBounds(prop.gameObject, out b)) return prop.position;
                return b.center;
            }

            var talk = Interact(host, "ACT_TalkToFriend", Pos("OBJ_02_TalkToFriend", Vector3.zero),
                new Vector3(2f, 2.2f, 2f), "OBJ_02_TalkToFriend",
                null, null, Act2Script.EV_PaintChip);
            talk.speakerName = "เพื่อนช่าง";
            talk.dialogueLines = new[]
            {
                "เข้ม: พี่ ช่วยดูอะไรให้หน่อย เศษสีจากที่เกิดเหตุ",
                "เพื่อนช่าง: เอามาสิ... เออ นี่มันสีพ่นโรงงาน ไม่ใช่สีซ่อม",
                "เพื่อนช่าง: วางบนโต๊ะตรวจก่อน เดี๋ยวส่องให้",
            };

            // กล่องเล็กและไม่ทับกัน — จุดพวกนี้อยู่ห่างกันไม่ถึงเมตรบนโต๊ะตัวเดียวกัน
            // กล่องใหญ่จะซ้อนกันจนเล็งแยกไม่ออกว่ากำลังจะกดอันไหน
            Interact(host, "ACT_PlaceEvidence", OnProp("EVIDENCE_Layout", "OBJ_03_PlaceEvidenceOnBench", Vector3.zero),
                new Vector3(0.7f, 0.7f, 0.7f), "OBJ_03_PlaceEvidenceOnBench",
                "วางเศษสีลงบนโต๊ะสแตนเลสแล้ว", "OBJ_02_TalkToFriend");

            Interact(host, "ACT_Magnifier", OnProp("MagnifierLamp", "OBJ_04_ExamineUnderMagnifier", Vector3.zero),
                new Vector3(0.6f, 0.7f, 0.6f), "OBJ_04_ExamineUnderMagnifier",
                "ใต้แว่นขยายเห็นชัด — สีสองชั้น ชั้นล่างเป็นสีรองพื้นของโรงงาน",
                "OBJ_03_PlaceEvidenceOnBench");

            Interact(host, "ACT_PaintChart", OnProp("PaintChipBoard", "OBJ_05_MatchPaintCode", Vector3.zero),
                new Vector3(1.4f, 1.4f, 0.6f), "OBJ_05_MatchPaintCode",
                "รหัสสีตรงกับรุ่นรถสปอร์ตนำเข้า ล็อตปีล่าสุด",
                "OBJ_04_ExamineUnderMagnifier", Act2Script.EV_PaintCode);

            Interact(host, "ACT_PartsDatabase", OnProp("DatabaseCart", "OBJ_06_SearchPartsDatabase", Vector3.zero),
                new Vector3(0.8f, 0.9f, 0.8f), "OBJ_06_SearchPartsDatabase",
                "ในประเทศมีรถรุ่นนี้สีนี้ไม่ถึงสิบคัน",
                "OBJ_05_MatchPaintCode");

            Interact(host, "ACT_OrderRecords", OnProp("RecordsStation", "OBJ_07_CheckOrderRecords", Vector3.zero),
                new Vector3(1.6f, 1.8f, 1.6f), "OBJ_07_CheckOrderRecords",
                "มีคนสั่งกันชนหน้าใหม่เมื่อวาน ชื่อผู้สั่ง: แชมป์ — ลูก ส.ส.",
                "OBJ_06_SearchPartsDatabase", Act2Script.EV_OwnerName);

            var pin = Interact(host, "ACT_PinBoard", OnProp("InvestigationBoard", "OBJ_08_PinResultOnBoard", Vector3.zero),
                new Vector3(2f, 1.6f, 1f), "OBJ_08_PinResultOnBoard",
                "ปักชื่อไว้กลางบอร์ด — รถอยู่ที่คลับของมันคืนนี้",
                "OBJ_07_CheckOrderRecords");
            pin.setCheckpointHere = true;

            // ออกจากอู่ = ไปลานจอด B1 (Act2Director เป็นคนสั่งโหลดซีนเอง)
            Reach(host, "ACT_LeaveGarage", Pos("OBJ_09_LeaveGarage", new Vector3(0f, 0f, -24f)),
                new Vector3(14f, 4f, 5f), "OBJ_09_LeaveGarage", "ไปที่คลับ");
        }

        // ───────────────────────── ลานจอดรถ VIP ─────────────────────────

        public static void WireParking(Transform root)
        {
            Bootstrap(root);
            var host = K.Group("ACT2_Beats", Systems(root));
            var struc = K.Category(root, K.Cat.Structure);

            // ── บทเรียนหมอบ: ท่อร้อยสายพาดต่ำกลางเลน ──
            // ลอดใต้ท่อ (เร็ว) หรือเดินอ้อมทางช่องจอด (ช้ากว่า เจอยามมากกว่า)
            var duct = K.Group("LowDuct_CrouchGate", struc);
            const float ductZ = -7f, ductBottom = 1.0f;
            K.Box("Duct", duct, new Vector3(0f, ductBottom + 0.55f, ductZ), new Vector3(11.6f, 1.1f, 0.8f), "Alu");
            K.Box("Hanger_L", duct, new Vector3(-5.6f, ductBottom + 1.4f, ductZ), new Vector3(0.1f, 0.6f, 0.1f), "SteelDark", default(Vector3), false);
            K.Box("Hanger_R", duct, new Vector3(5.6f, ductBottom + 1.4f, ductZ), new Vector3(0.1f, 0.6f, 0.1f), "SteelDark", default(Vector3), false);
            K.Box("Hazard", duct, new Vector3(0f, ductBottom + 0.06f, ductZ - 0.42f), new Vector3(11.6f, 0.12f, 0.02f), "LineYellow", default(Vector3), false);
            K.Sign("Duct_Sign", duct, new Vector3(0f, ductBottom + 1.35f, ductZ - 0.45f), new Vector2(4.2f, 0.3f),
                   "LOW CLEARANCE  1.0 m", new Color(0.9f, 0.75f, 0.1f), 180f);

            Reach(host, "ACT_LearnCrouch", new Vector3(0f, 0f, ductZ + 1.6f), new Vector3(11f, 1.6f, 2.4f),
                "OBJ_02_LearnCrouch", "หมอบลอดผ่านได้แล้ว — ยามมองเห็นยากขึ้นตอนหมอบ", true);

            Vector3 Pos(string marker, Vector3 fallback)
            {
                var t = Find(root, marker);
                return t != null ? t.position : fallback;
            }

            // ── ถึงตัวรถ ──
            Reach(host, "ACT_ReachTargetCar", Pos("OBJ_03_ExamineTargetCar", new Vector3(8.6f, 0f, 10.4f)),
                new Vector3(4f, 2.4f, 4f), "OBJ_03_ExamineTargetCar", "รถคันนั้นแหละ — เข้าไปดูใกล้ๆ");

            // ── กันชนหน้าที่พังยับ: เป้าหมายของการถ่ายรูป ──
            var car = Find(root, "Car_TARGET_RedSports");
            Vector3 bumperPos = car != null ? car.position : new Vector3(8.6f, 0f, 12.6f);
            Bounds carBounds;
            if (car != null && K.TryBounds(car.gameObject, out carBounds))
                bumperPos = new Vector3(carBounds.min.x + 0.15f, carBounds.min.y + 0.45f, carBounds.center.z);

            var dress = K.Category(root, K.Cat.Dressing);
            var damage = K.Group("Bumper_Damaged", dress);
            K.Box("Crush_A", damage, bumperPos + new Vector3(0f, 0f, -0.45f), new Vector3(0.35f, 0.5f, 0.7f), "HazardBlack", new Vector3(0f, 0f, 14f), false);
            K.Box("Crush_B", damage, bumperPos + new Vector3(-0.06f, -0.08f, 0.25f), new Vector3(0.3f, 0.42f, 0.8f), "HazardBlack", new Vector3(6f, 0f, -9f), false);
            K.Box("Paint_Scrape", damage, bumperPos + new Vector3(-0.14f, 0.12f, -0.1f), new Vector3(0.05f, 0.22f, 1.1f), "LineWhite", default(Vector3), false);

            var photo = damage.gameObject.AddComponent<PhotoSubject>();
            photo.evidenceId = Act2Script.EV_BumperPhoto;
            photo.objectiveId = "OBJ_PhotoBumper";
            photo.hint = "เล็งให้กันชนที่ยุบเต็มกรอบ";
            photo.successLine = "ถ่ายไว้แล้ว — กันชนหน้ายุบ รอยสีตรงกับเศษที่เก็บมาจากที่เกิดเหตุ";
            photo.minScreenCoverage = 0.10f;
            photo.maxDistance = 9f;
            photo.partOf = car;      // collider ของตัวรถต้องไม่ถูกนับว่าบังกันชนของตัวเอง

            // ── ส่องในห้องโดยสาร: กล้องหน้ารถหายไปแล้ว ──
            // ต้องอยู่ "ข้าง" รถ ไม่ใช่กลางตัวรถ — ตัวรถมี box collider ทึบ
            // กล่องที่ฝังอยู่ข้างในจะโดนบังจน raycast ของผู้เล่นไปไม่ถึงเลย
            Vector3 cabinPos = car != null && K.TryBounds(car.gameObject, out carBounds)
                ? new Vector3(carBounds.center.x - 0.6f, carBounds.min.y + 1.05f, carBounds.min.z - 0.55f)
                : new Vector3(8.2f, 1.05f, 11.1f);
            var cabin = Interact(host, "ACT_CheckDashcam", cabinPos, new Vector3(1.6f, 1.2f, 0.8f),
                "OBJ_CheckDashcam",
                "ที่ยึดกล้องหน้ารถยังอยู่... แต่ตัวกล้องถูกถอดออกไปแล้ว",
                "OBJ_PhotoBumper", Act2Script.EV_DashcamMissing);
            cabin.setCheckpointHere = true;

            // ── เข้าคลับ ──
            Reach(host, "ACT_EnterClub", Pos("DOOR_ToClubScene", new Vector3(0f, 0f, 18.3f)),
                new Vector3(5f, 3f, 4f), "OBJ_05_EnterClub", "เข้าไปในคลับ");

            // ── ยาม เขตกำบัง ของขว้าง ──
            ArmGuards(root, 8.5f, 70f, 3.5f);
            ShadowsFromMarkers(root, "CoverPoints", "Cover_", new Vector3(2.6f, 2.4f, 2.6f));

            var pickups = K.Group("ACT2_Throwables", Systems(root));
            Vector3[] cans = { new Vector3(-6.9f, 0.2f, -14.4f), new Vector3(6.9f, 0.2f, 3f), new Vector3(-14f, 0.2f, 9f) };
            for (int i = 0; i < cans.Length; i++) Throwable(pickups, "Can_" + i, cans[i]);
        }

        static void Throwable(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(1.2f, 1.6f, 1.2f);
            box.isTrigger = true;
            var pick = go.AddComponent<ThrowablePickup>();
            pick.amount = 2;
            pick.noticeText = "เก็บกระป๋องได้ 2 ใบ  [G] ขว้างล่อยาม";
        }

        // ───────────────────────── ในคลับ ─────────────────────────

        public static void WireClub(Transform root)
        {
            Bootstrap(root);
            var host = K.Group("ACT2_Beats", Systems(root));

            Vector3 Pos(string marker, Vector3 fallback)
            {
                var t = Find(root, marker);
                return t != null ? t.position : fallback;
            }

            Reach(host, "ACT_CrossDanceFloor", Pos("OBJ_02_CrossDanceFloor", new Vector3(0f, 0f, 2f)),
                new Vector3(6f, 3f, 6f), "OBJ_02_CrossDanceFloor", "ข้ามฟลอร์มาแล้ว — ฝั่ง VIP อยู่ทางซ้าย");

            Reach(host, "ACT_ReachVipEdge", Pos("OBJ_03_ReachVipEdge", new Vector3(-10f, 0f, -7.4f)),
                new Vector3(3.5f, 3f, 5f), "OBJ_03_ReachVipEdge", "ใกล้พอแล้ว — หาที่กำบังแล้วฟังให้ครบ");

            // ── โซนแอบฟัง: ยิ่งใกล้ยิ่งชัด ยิ่งใกล้ยิ่งเสี่ยง ──
            var suits = Find(root, "DIALOGUE_SDCardTalk");
            var zoneGo = new GameObject("ACT_EavesdropSuits");
            zoneGo.transform.SetParent(host, false);
            zoneGo.transform.position = suits != null ? suits.position : new Vector3(-14.6f, 0.45f, -6.3f);

            var zone = zoneGo.AddComponent<EavesdropZone>();
            zone.objectiveId = "OBJ_04_OverhearSuits";
            zone.evidenceId = Act2Script.EV_SDCardLocation;
            zone.clearDistance = 3.2f;
            zone.maxDistance = 9f;
            zone.listenSeconds = 15f;
            zone.maxExposureMultiplier = 2.0f;

            // เพลงคลับที่จะถูกหรี่ตอนตั้งใจฟัง — ใส่คลิปเองได้ทีหลัง
            var musicGo = new GameObject("ClubMusic");
            musicGo.transform.SetParent(K.Category(root, K.Cat.Env), false);
            musicGo.transform.position = Pos("NAV_DanceFloor", new Vector3(0f, 1.5f, 2f));
            var music = musicGo.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = true;
            music.spatialBlend = 0.35f;
            music.volume = 0.65f;
            zone.ambientMusic = music;

            // ── ออกทางประตูพนักงาน = จบ ACT 2 ──
            Reach(host, "ACT_LeaveViaStaffDoor", Pos("DOOR_StaffOnly", new Vector3(-20.7f, 0f, -13f)),
                new Vector3(2.4f, 3f, 2.4f), "OBJ_05_LeaveViaStaffDoor",
                "ออกมาได้แล้ว — เป้าหมายต่อไปคือเซฟในเพนต์เฮาส์");

            // ยามในคลับมองใกล้กว่าอีก คนแน่นและไฟกระพริบ
            ArmGuards(root, 7.5f, 65f, 3.8f);
            ShadowsFromMarkers(root, "CoverPoints", "Hide_", new Vector3(2.8f, 2.4f, 2.8f));
        }
    }
}
