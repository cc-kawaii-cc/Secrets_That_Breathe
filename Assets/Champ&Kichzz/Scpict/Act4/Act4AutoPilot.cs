using SecretsThatBreathe.Act2;
using UnityEngine;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// ผู้เล่นอัตโนมัติสำหรับ QA ซีนห้องพิจารณาคดี — ไม่ได้วางไว้ในซีน ใส่เองตอนเทส
    /// (เพิ่ม component นี้ให้ GameObject ไหนก็ได้ระหว่าง Play)
    ///
    /// สุ่มเลือกทุกอย่าง: ตัวเลือก, คัดค้าน/ไม่คัดค้าน, ซักไซ้/แสดงหลักฐาน ทั้งถูกและผิด
    /// ทำให้วิ่งผ่านทุกกิ่งของบทได้โดยไม่ต้องกดเอง ความน่าเชื่อถือหมดหลอดจะถูกรีเซ็ตให้เอง
    /// จึงเล่นจนจบฉากได้เสมอ และตัวเดินไปที่นั่ง/จอ/ประตูให้เองตามเป้าหมายปัจจุบัน
    /// </summary>
    public class Act4AutoPilot : MonoBehaviour
    {
        [Tooltip("วินาทีระหว่างการตัดสินใจแต่ละครั้ง")]
        public float thinkSeconds = 0.35f;
        public int seed = 7;
        [Range(0f, 1f)] public float objectChance = 0.5f;
        [Tooltip("โอกาสเลือกหลักฐานที่ถูกเมื่อรู้คำตอบ (0 = สุ่มล้วน, 1 = ตอบถูกทุกครั้ง) — ช่วยให้เทสจบเร็ว")]
        [Range(0f, 1f)] public float accuracy = 0.5f;
        [Tooltip("เร่งบทพูดให้เร็วขึ้นระหว่างเทส")]
        public bool fastLines = true;

        System.Random _rng;
        float _t;

        void Start()
        {
            _rng = new System.Random(seed);
            if (!fastLines) return;
            foreach (var seq in FindObjectsByType<Act4Sequence>(FindObjectsSortMode.None))
            {
                seq.lineBaseSeconds = 0.5f;
                seq.lineSecondsPerChar = 0.01f;
            }
        }

        void Update()
        {
            _t -= Time.unscaledDeltaTime;
            if (_t > 0f) return;
            _t = thinkSeconds;

            var hud = Act4HUD.Instance;
            if (hud == null) return;
            if (hud.CaseFileOpen) { hud.SetCaseFileOpen(false); return; }

            if (hud.ChoiceOpen) { hud.DebugChoose(_rng.Next(hud.ChoiceCount)); return; }
            if (hud.CrossExamOpen)
            {
                string known = CorrectEvidenceFor(hud.CrossExamPrompt);
                if (known != null && _rng.NextDouble() < accuracy) { hud.DebugPresent(known); return; }
                var all = Evidence();
                if (all.Length > 0) hud.DebugPresent(all[_rng.Next(all.Length)].id);
                return;
            }
            if (hud.TestimonyOpen)
            {
                if (CorrectEvidenceFor(hud.TestimonyText) != null && _rng.NextDouble() < accuracy)
                {
                    hud.DebugTestimony(Act4HUD.TestimonyInput.Present);
                    return;
                }
                int r = _rng.Next(10);
                hud.DebugTestimony(r < 3 ? Act4HUD.TestimonyInput.Next
                                 : r < 4 ? Act4HUD.TestimonyInput.Prev
                                 : r < 7 ? Act4HUD.TestimonyInput.Press
                                 : Act4HUD.TestimonyInput.Present);
                return;
            }
            if (hud.ObjectionPromptVisible)
            {
                if (_rng.NextDouble() < objectChance) hud.DebugObjection();
                _t = 1.2f;   // ตัดสินใจครั้งเดียวต่อคำถาม
                return;
            }

            DriveExploration();
        }

        /// <summary>หลักฐานที่ถูกสำหรับข้อความนี้ (ขั้นให้พยานดูวัตถุพยาน / ข้อคำเบิกความที่ขัดหลักฐาน / ข้อต่อสู้สุดท้าย)</summary>
        static string CorrectEvidenceFor(string text)
        {
            var d = CourtData();
            if (string.IsNullOrEmpty(text) || d == null) return null;
            if (d.plaintiffWitnesses != null)
                foreach (var p in d.plaintiffWitnesses)
                    if (p.exhibit != null && p.exhibit.prompt == text) return p.exhibit.evidenceId;
            if (d.finalClaim != null && d.finalClaim.prompt == text) return d.finalClaim.evidenceId;
            if (d.defenseWitnesses != null)
                foreach (var w in d.defenseWitnesses)
                    if (w.testimony != null && w.testimony.statements != null)
                        foreach (var st in w.testimony.statements)
                            if (st.text == text && !string.IsNullOrEmpty(st.contradiction)) return st.contradiction;
            return null;
        }

        static Act4CourtroomData CourtData()
        {
            var seq = FindAnyObjectByType<CourtroomSequence>();
            return seq != null ? seq.Data : null;
        }

        static Act4Evidence[] Evidence()
        {
            var d = CourtData();
            return d != null && d.caseFile.evidence != null ? d.caseFile.evidence : Act4Script.DefaultCaseFile();
        }

        /// <summary>ตอนเดินเล่น: พาไปทำเป้าหมายปัจจุบัน (นั่งประจำโต๊ะ / กดจอ / ออกประตู)</summary>
        void DriveExploration()
        {
            var gm = GameManager.Instance;
            var director = Act2Director.Instance;
            if (gm == null || director == null || !gm.IsState(GameState.Exploration) || director.Current == null) return;

            switch (director.Current.id)
            {
                case Act4Script.OBJ_TakeSeat:
                    var seat = GameObject.Find(Act4Script.OBJ_TakeSeat);
                    var pm = PlayerManager.Instance;
                    if (seat == null || pm == null || pm.controller == null) return;
                    float lift = (pm.controller.height * 0.5f - pm.controller.center.y) *
                                 Mathf.Abs(pm.playerRoot.transform.lossyScale.y) + 0.02f;
                    pm.TeleportTo(seat.transform.position + Vector3.up * lift, seat.transform.rotation);
                    break;
                case Act4Script.OBJ_PlayClip:
                    Interact("SCREEN_EvidenceTV");
                    break;
                case Act4Script.OBJ_LeaveCourt:
                    Interact("EXIT_ToCourtSteps");
                    break;
            }
        }

        static void Interact(string name)
        {
            var go = GameObject.Find(name);
            var act = go != null ? go.GetComponent<StoryInteractable>() : null;
            if (act != null && !act.hasInteracted) act.DoInteract();
        }
    }
}
