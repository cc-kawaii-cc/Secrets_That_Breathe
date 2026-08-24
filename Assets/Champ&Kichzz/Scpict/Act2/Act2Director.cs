using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// สมองของ ACT 2 "ล้วงคองูเห่า" — เก็บว่าเล่นถึงบีทไหน ได้หลักฐานอะไรมาแล้ว
    /// และจุดเช็คพอยต์ล่าสุด อยู่ข้าม scene (DontDestroyOnLoad)
    ///
    /// ลำดับเรื่อง: อู่ซ่อมรถ (พิสูจน์หลักฐาน) → ลานจอด B1 (ลอบเข้า/ถ่ายรูป) → คลับ (แอบฟัง)
    ///
    /// ทุกอย่างในซีนคุยกับตัวนี้ผ่าน id ของ objective ไม่ต้องรู้จักกันเอง
    /// เช่น กล้องถ่ายรูปเสร็จ → Act2Director.Instance.Complete("OBJ_PhotoBumper")
    /// </summary>
    public class Act2Director : MonoBehaviour
    {
        public static Act2Director Instance { get; private set; }

        // ── events ──
        public static event Action<Act2Objective> OnObjectiveChanged;
        public static event Action<string> OnObjectiveCompleted;
        public static event Action<string> OnEvidenceGained;
        public static event Action<string> OnNotice;          // ข้อความสั้นๆ มุมจอ
        public static event Action<string> OnSceneComplete;   // ชื่อซีนที่เพิ่งเคลียร์ครบ

        [Header("Debug")]
        [Tooltip("ข้ามไปเริ่มที่ objective ลำดับนี้เลย (สำหรับเทสเฉพาะจุด)")]
        public int startAtIndex = 0;
        public bool logBeats = true;

        [Header("การข้ามซีน")]
        [Tooltip("เคลียร์เควสในซีนครบแล้วพาไปซีนถัดไปเอง")]
        public bool autoAdvanceScene = true;
        [Tooltip("หน่วงก่อนเปลี่ยนซีน ให้ผู้เล่นได้อ่านบทสรุปก่อน (แนะนำ 3-5 วินาที)")]
        [Range(1f, 10f)] public float advanceDelay = 4f;

        [Header("Checkpoint")]
        [Tooltip("จุดที่ผู้เล่นจะถูกส่งกลับเมื่อโดนยามจับ")]
        public Vector3 checkpointPosition;
        public Quaternion checkpointRotation = Quaternion.identity;
        public string checkpointScene;

        readonly HashSet<string> _done = new HashSet<string>();
        readonly HashSet<string> _evidence = new HashSet<string>();
        int _index;
        bool _advancing;

        public IReadOnlyCollection<string> Evidence { get { return _evidence; } }
        public Act2Objective Current
        {
            get { return _index >= 0 && _index < Act2Script.Objectives.Length ? Act2Script.Objectives[_index] : null; }
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _index = Mathf.Clamp(startAtIndex, 0, Act2Script.Objectives.Length - 1);
        }

        void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        void Start()
        {
            SyncToScene(SceneManager.GetActiveScene().name);
            EnsureCheckpoint();
            Announce();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != this) return;
            SyncToScene(scene.name);
            checkpointScene = null;      // ของซีนเก่าใช้ไม่ได้แล้ว
            EnsureCheckpoint();
            Announce();
        }

        /// <summary>
        /// เล่นซีนไหนอยู่ ให้เลื่อน objective ไปที่ตัวแรกของซีนนั้น
        /// ทำให้เปิดซีนไหนกด Play ก็เทสได้เลย ไม่ต้องไล่มาจากต้น act
        /// </summary>
        void SyncToScene(string sceneName)
        {
            var list = Act2Script.Objectives;
            if (_index < list.Length && list[_index].scene == sceneName) return;

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].scene != sceneName) continue;
                if (_done.Contains(list[i].id)) continue;
                _index = i;
                if (logBeats) Debug.Log("[Act2] เข้าซีน " + sceneName + " → objective: " + list[i].id);
                return;
            }
        }

        // ───────────────────────── objectives ─────────────────────────

        public bool IsDone(string id) { return _done.Contains(id); }

        /// <summary>ทำ objective สำเร็จ ถ้าตรงกับตัวปัจจุบันจะเลื่อนไปตัวถัดไปให้</summary>
        public void Complete(string id)
        {
            if (string.IsNullOrEmpty(id) || !_done.Add(id)) return;
            if (logBeats) Debug.Log("[Act2] สำเร็จ: " + id);
            if (OnObjectiveCompleted != null) OnObjectiveCompleted(id);

            var list = Act2Script.Objectives;
            // เลื่อนข้ามทุกตัวที่ทำไปแล้ว (เผื่อผู้เล่นทำข้ามลำดับ)
            while (_index < list.Length && _done.Contains(list[_index].id)) _index++;
            Announce();

            if (_index >= list.Length && logBeats) Debug.Log("[Act2] เคลียร์ objective ครบทุกบทแล้ว");
            // ปล่อยให้ CheckSceneCleared ประกาศจบซีน/จบบทที่เดียว แม้เป็นตัวสุดท้ายของทั้งบท
            CheckSceneCleared();
        }

        /// <summary>
        /// เคลียร์เควสของซีนนี้ครบหรือยัง ถ้าครบก็พาไปซีนถัดไปเอง
        ///
        /// ให้ที่นี่เป็นเจ้าของการเปลี่ยนซีนคนเดียว เขต trigger ที่ประตูจึงไม่ต้องรู้เรื่อง
        /// ผู้เล่นที่เก็บเควสครบแล้วแต่เดินไม่โดนประตูพอดีจะไม่ติดค้างอยู่ในด่าน
        /// </summary>
        void CheckSceneCleared()
        {
            if (_advancing) return;

            string scene = SceneManager.GetActiveScene().name;
            var list = Act2Script.Objectives;
            for (int i = 0; i < list.Length; i++)
                if (list[i].scene == scene && !_done.Contains(list[i].id)) return;   // ยังเหลือ

            string next = Act2Script.NextScene(scene);
            if (logBeats) Debug.Log("[Act2] เคลียร์ซีน " + scene + " ครบแล้ว → " + (next ?? "จบบท"));
            if (OnSceneComplete != null) OnSceneComplete(scene);

            _advancing = true;
            StartCoroutine(AdvanceRoutine(Act2Script.SceneCompleteLine(scene),
                                          autoAdvanceScene ? next : null));
        }

        /// <summary>
        /// ประกาศจบซีน ค้างไว้ให้ผู้เล่นอ่าน แล้วค่อยพาไปซีนถัดไป
        /// ประกาศเสมอแม้ไม่มีซีนต่อ เพราะซีนปิดบทต้องขึ้น "จบ Act 3" ให้เห็นด้วย
        /// </summary>
        IEnumerator AdvanceRoutine(string line, string nextScene)
        {
            Notice(line);
            if (SubtitleManager.Instance != null) SubtitleManager.Instance.Show(line, advanceDelay);
            yield return new WaitForSeconds(advanceDelay);

            if (!string.IsNullOrEmpty(nextScene))
            {
                if (GameManager.Instance != null) GameManager.Instance.LoadScene(nextScene);
                else SceneManager.LoadScene(nextScene);
            }
            _advancing = false;
        }

        void Announce()
        {
            var cur = Current;
            if (cur == null) return;
            if (OnObjectiveChanged != null) OnObjectiveChanged(cur);
        }

        /// <summary>ถ้า objective ปัจจุบันไปอยู่คนละซีน ให้โหลดซีนนั้น</summary>
        public void GoToObjectiveScene()
        {
            var cur = Current;
            if (cur == null || GameManager.Instance == null) return;
            if (cur.scene == SceneManager.GetActiveScene().name) return;
            GameManager.Instance.LoadScene(cur.scene);
        }

        /// <summary>
        /// ทำให้ซีนนี้มีเช็คพอยต์ที่ใช้ได้เสมอ
        ///
        /// ถ้ากด Play ที่ซีนตรงๆ event sceneLoaded อาจไม่ยิง เช็คพอยต์จะเป็น (0,0,0)
        /// แล้วผู้เล่นที่โดนจับจะถูกส่งไปโผล่กลางอากาศที่จุดกำเนิดโลก
        /// </summary>
        public void EnsureCheckpoint()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (checkpointScene == scene) return;

            var pm = PlayerManager.Instance;
            if (pm != null && pm.playerRoot != null)
            {
                SetCheckpoint(pm.playerRoot.transform.position, pm.playerRoot.transform.rotation);
                return;
            }
            // ยังไม่มี player ก็หาหมุดจุดเกิดของซีนแทน
            Vector3 spawn;
            if (FindSpawnMarker(out spawn)) SetCheckpoint(spawn, Quaternion.identity);
        }

        static bool FindSpawnMarker(out Vector3 position)
        {
            position = Vector3.zero;
            var all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (!all[i].name.StartsWith("PlayerSpawn")) continue;
                position = all[i].position;
                return true;
            }
            return false;
        }

        // ───────────────────────── evidence ─────────────────────────

        public bool HasEvidence(string id) { return _evidence.Contains(id); }

        public void GainEvidence(string id, string noticeText = null)
        {
            if (string.IsNullOrEmpty(id) || !_evidence.Add(id)) return;
            if (logBeats) Debug.Log("[Act2] ได้หลักฐาน: " + id);
            if (OnEvidenceGained != null) OnEvidenceGained(id);
            if (!string.IsNullOrEmpty(noticeText)) Notice(noticeText);
        }

        public void Notice(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (OnNotice != null) OnNotice(text);
        }

        // ───────────────────────── checkpoint ─────────────────────────

        public void SetCheckpoint(Vector3 pos, Quaternion rot)
        {
            checkpointScene = SceneManager.GetActiveScene().name;
            checkpointPosition = pos;
            checkpointRotation = rot;
        }

        /// <summary>
        /// ส่งผู้เล่นกลับเช็คพอยต์ล่าสุด (ใช้ตอนโดนยามไล่ออก)
        ///
        /// จงใจไม่โหลดซีนใหม่ไม่ว่ากรณีใด — การโดนจับควรรีเซ็ตอยู่ในฉากนั้น
        /// ถ้าโหลดซีน โค้ดที่เรียกจะถูกทำลายกลางคัน แล้วเกมค้างอยู่ที่ Cutscene
        /// เช็คพอยต์ที่เป็นของซีนอื่นถือว่าใช้ไม่ได้ ให้หาจุดเกิดของซีนนี้แทน
        /// </summary>
        public bool RespawnAtCheckpoint()
        {
            var pm = PlayerManager.Instance;
            if (pm == null || pm.playerRoot == null) return false;

            string scene = SceneManager.GetActiveScene().name;
            Vector3 target = checkpointPosition;
            Quaternion rot = checkpointRotation;

            if (checkpointScene != scene)
            {
                Vector3 spawn;
                if (!FindSpawnMarker(out spawn)) return false;
                target = spawn;
                rot = Quaternion.identity;
                SetCheckpoint(target, rot);
            }

            pm.TeleportTo(target, rot);
            var movement = pm.playerRoot.GetComponentInChildren<PlayerMovement>();
            if (movement != null) movement.ResetMotion();
            return true;
        }
    }

    /// <summary>หนึ่งเป้าหมายในบท พร้อมชื่อ marker ที่ใช้ชี้ทางบนจอ</summary>
    [Serializable]
    public class Act2Objective
    {
        public string id;
        public string scene;
        public string text;
        /// <summary>ชื่อ GameObject ในซีนที่จะใช้เป็นหมุดนำทาง (เว้นว่าง = ไม่ชี้)</summary>
        public string waypoint;

        public Act2Objective(string id, string scene, string text, string waypoint)
        {
            this.id = id; this.scene = scene; this.text = text; this.waypoint = waypoint;
        }
    }

    /// <summary>
    /// บทของ ACT 2 เขียนไว้ที่เดียว เรียงตามลำดับการเล่น
    /// id ตรงกับชื่อ marker ที่ตัว builder สร้างไว้ในซีน
    /// </summary>
    public static class Act2Script
    {
        public const string SceneGarage = "Main2_Garage";
        public const string SceneParking = "Main2_ParkingB1";
        public const string SceneClub = "Main2_Club";
        public const string ScenePenthouse = "Penthouse";

        // หลักฐาน
        public const string EV_PaintChip = "EV_PaintChip";
        public const string EV_PaintCode = "EV_PaintCode";
        public const string EV_OwnerName = "EV_OwnerName";
        public const string EV_BumperPhoto = "EV_BumperPhoto";
        public const string EV_DashcamMissing = "EV_DashcamMissing";
        public const string EV_SDCardLocation = "EV_SDCardLocation";
        public const string EV_ConfessionCall = "EV_ConfessionCall";

        public static readonly Act2Objective[] Objectives =
        {
            // ── 2.1 การพิสูจน์หลักฐาน (อู่ซ่อมรถ) ──
            new Act2Objective("OBJ_02_TalkToFriend",        SceneGarage,  "คุยกับเพื่อนที่อู่",                      "OBJ_02_TalkToFriend"),
            new Act2Objective("OBJ_03_PlaceEvidenceOnBench",SceneGarage,  "วางเศษสีบนโต๊ะตรวจ",                     "OBJ_03_PlaceEvidenceOnBench"),
            new Act2Objective("OBJ_04_ExamineUnderMagnifier",SceneGarage, "ส่องเศษสีใต้แว่นขยาย",                   "OBJ_04_ExamineUnderMagnifier"),
            new Act2Objective("OBJ_05_MatchPaintCode",      SceneGarage,  "เทียบรหัสสีกับชาร์ต",                    "OBJ_05_MatchPaintCode"),
            new Act2Objective("OBJ_06_SearchPartsDatabase", SceneGarage,  "ค้นฐานข้อมูลอะไหล่",                     "OBJ_06_SearchPartsDatabase"),
            new Act2Objective("OBJ_07_CheckOrderRecords",   SceneGarage,  "ขึ้นไปดูบันทึกการสั่งอะไหล่ที่ออฟฟิศ",   "OBJ_07_CheckOrderRecords"),
            new Act2Objective("OBJ_08_PinResultOnBoard",    SceneGarage,  "ปักผลลัพธ์บนบอร์ดสืบสวน",               "OBJ_08_PinResultOnBoard"),
            new Act2Objective("OBJ_09_LeaveGarage",         SceneGarage,  "ออกจากอู่ ไปที่คลับ",                    "OBJ_09_LeaveGarage"),

            // ── ลานจอดรถ VIP (Stealth ขั้นต้น) ──
            new Act2Objective("OBJ_02_LearnCrouch",         SceneParking, "หมอบ (กด Ctrl) แล้วลอดผ่านแนวรถ",        "OBJ_02_LearnCrouch"),
            new Act2Objective("OBJ_03_ExamineTargetCar",    SceneParking, "หารถสปอร์ตสีแดง เลี่ยงสายตายาม",         "OBJ_03_ExamineTargetCar"),
            new Act2Objective("OBJ_PhotoBumper",            SceneParking, "ถ่ายรูปกันชนหน้าที่พังเป็นหลักฐาน",      "INTERACT_ExamineCar"),
            new Act2Objective("OBJ_CheckDashcam",           SceneParking, "ส่องดูในห้องโดยสาร",                     "INTERACT_ExamineCar"),
            new Act2Objective("OBJ_05_EnterClub",           SceneParking, "เข้าไปในคลับ",                           "OBJ_05_EnterClub"),

            // ── ในคลับ ──
            new Act2Objective("OBJ_02_CrossDanceFloor",     SceneClub,    "ข้ามฟลอร์เต้นไปฝั่ง VIP",                "OBJ_02_CrossDanceFloor"),
            new Act2Objective("OBJ_03_ReachVipEdge",        SceneClub,    "เข้าใกล้ขอบโซน VIP โดยไม่ให้ยามเห็น",    "OBJ_03_ReachVipEdge"),
            new Act2Objective("OBJ_04_OverhearSuits",       SceneClub,    "แอบฟังสองคนนั้นคุยกัน",                  "OBJ_04_OverhearSuits"),
            new Act2Objective("OBJ_05_LeaveViaStaffDoor",   SceneClub,    "ออกทางประตูพนักงาน",                     "OBJ_05_LeaveViaStaffDoor"),

            // ── ACT 3: เพนต์เฮาส์ของแชมป์ ──
            // ใช้ตัวจัดการชุดเดียวกับ ACT 2 (objective / เช็คพอยต์ / HUD / ระบบยาม)
            // เป็นบทต่อเนื่องกัน แยกคลาสใหม่จะได้โค้ดซ้ำทั้งชุดโดยไม่ได้อะไรเพิ่ม
            new Act2Objective("OBJ_A3_01_EnterHouse",    ScenePenthouse, "เปิดประตูลิฟต์ เข้าไปในเพนต์เฮาส์",     "Door_L"),
            new Act2Objective("OBJ_A3_02_ReachUpstairs", ScenePenthouse, "ขึ้นชั้นสองโดยไม่ให้บอดี้การ์ดเห็น",    "NAV_StairTop"),
            new Act2Objective("OBJ_A3_03_Hide",          ScenePenthouse, "ซ่อนตัวที่ตู้เสื้อผ้าในห้องนอน",        "Dresser"),
            new Act2Objective("OBJ_A3_04_Overhear",      ScenePenthouse, "แอบฟังแชมป์คุยโทรศัพท์ — ห้ามส่งเสียง", "NAV_SuiteBedroom"),
            new Act2Objective("OBJ_A3_05_Escape",        ScenePenthouse, "หนีออกจากเพนต์เฮาส์",                   "LIFT_ToClub"),
        };

        /// <summary>ข้อความตอนเคลียร์ซีนนั้นครบ — ซีนปิดบทจะได้ประกาศจบบทให้ผู้เล่นอ่าน</summary>
        public static string SceneCompleteLine(string scene)
        {
            switch (scene)
            {
                case SceneClub:      return "จบ Act 2 — SD Card อยู่ที่เซฟในเพนต์เฮาส์ของแชมป์";
                case ScenePenthouse: return "จบ Act 3";
                default:             return "เคลียร์ครบแล้ว — กำลังไปต่อ...";
            }
        }

        public static Act2Objective Find(string id)
        {
            for (int i = 0; i < Objectives.Length; i++)
                if (Objectives[i].id == id) return Objectives[i];
            return null;
        }

        /// <summary>ซีนถัดไปตามลำดับบท (null = ซีนสุดท้ายของ act)</summary>
        public static string NextScene(string scene)
        {
            bool seen = false;
            for (int i = 0; i < Objectives.Length; i++)
            {
                if (Objectives[i].scene == scene) { seen = true; continue; }
                if (seen) return Objectives[i].scene;
            }
            return null;
        }
    }
}
