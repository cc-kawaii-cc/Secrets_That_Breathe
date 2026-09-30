using System.Collections;
using System.Collections.Generic;
using SecretsThatBreathe.Act2;
using UnityEngine;
using S = SecretsThatBreathe.Act4.Act4Script;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// ACT 4.1 การต่อสู้ในศาล + 4.2 จุดหักมุม — ซีน Main4_Courtroom
    ///
    ///   1) เข้าห้อง ทบทวนแฟ้มคดี [Tab] เดินไปโต๊ะทนายโจทก์          (OBJ_A4_01_TakeSeat)
    ///   2) การพิจารณาคดีเต็มรูปแบบ (CourtroomTrial.cs)                (OBJ_A4_02)
    ///   3) เดินไปจอ กด E เปิดคลิป SD Card -> The Climax -> The Twist -> The Ruling -> The Aftermath (OBJ_A4_03)
    ///   4) ห้องว่างเปล่า เดินออกทางประตู   (OBJ_A4_04 -> Act2Director พาไปซีนหน้าศาล)
    ///
    /// บทพูด คำถาม พยาน ตัวเลข อยู่ใน <see cref="data"/> (Act4_CourtroomData.asset) แก้ใน Inspector ได้ทั้งหมด
    /// ส่วน component นี้เก็บค่าจังหวะ/ความเร็วของนักแสดงและกล้อง
    /// </summary>
    public partial class CourtroomSequence : Act4Sequence
    {
        [Header("ข้อมูลบท (แก้บท/คำถาม/เวลาคัดค้าน ที่ asset นี้)")]
        public Act4CourtroomData data;

        [Header("ฉาก")]
        public DashcamClip clip;

        [Header("ความเร็วนักแสดง (m/s)")]
        [Range(0.3f, 4f)] public float witnessWalkSpeed = 1.5f;
        [Range(0.3f, 4f)] public float judgeWalkSpeed = 1.1f;
        [Range(0.3f, 4f)] public float champWalkSpeed = 1.4f;
        [Range(0.3f, 4f)] public float defenseWalkSpeed = 1.4f;

        [Header("ความสูงตัวแทนนักแสดง (เมตร)")]
        [Tooltip("ตอนนั่ง")]
        [Range(0.8f, 1.8f)] public float seatedHeight = 1.32f;
        [Tooltip("ตอนยืน")]
        [Range(1.2f, 2.2f)] public float standingHeight = 1.8f;

        [Header("เสียง (เว้นว่างได้ — โปรเจกต์ยังไม่มีไฟล์เสียง)")]
        public AudioClip gavelSfx;
        public AudioClip impactSfx;

        [Header("เทส")]
        [Tooltip("ข้ามการ์ดบทและช็อตเปิดห้อง")]
        public bool skipIntro = false;

        Transform _defense, _aunt, _champ;
        float _cred;
        bool _clipStarted, _leaving;

        /// <summary>ข้อมูลที่ใช้เล่นจริง — ถ้าไม่ได้ใส่ asset จะใช้ค่าเริ่มต้นจาก TrialScript</summary>
        public Act4CourtroomData Data
        {
            get
            {
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<Act4CourtroomData>();
                    data.ResetToDefaults();
                    Debug.LogWarning("[Act4] CourtroomSequence ไม่ได้ใส่ data asset — ใช้ค่าเริ่มต้นแทน (แก้ใน Inspector ไม่ได้)");
                }
                return data;
            }
        }

        protected override Act4Cast Cast { get { return Data.cast; } }
        protected override string WitnessName { get { return _standSpeaker; } }

        void OnEnable() { Act2Director.OnObjectiveCompleted += OnObjective; }
        void OnDisable() { Act2Director.OnObjectiveCompleted -= OnObjective; }

        static bool Done(string id) { return Director != null && Director.IsDone(id); }
        static void Complete(string id) { if (Director != null) Director.Complete(id); }

        IEnumerator Start()
        {
            yield return WaitForSystems();
            _defense = Find("Defense_Lawyer");
            _aunt = Find("Aunt_Samorn");
            _champ = Find("Champ");
            // องค์คณะยังไม่ขึ้นบัลลังก์ — จะเดินออกมาจากประตูผู้พิพากษาตอนเริ่มพิจารณา
            HideJudgesUntilSession();
            SetupCaseFile();

            if (!skipIntro) yield return Intro();
            else if (Hud != null) Hud.SetFade(0f);
            Hud.CaseFileAllowed = true;

            // ── 1) เดินไปโต๊ะทนายโจทก์ ──
            var seat = Find(S.OBJ_TakeSeat);
            while (!Done(S.OBJ_TakeSeat))
            {
                if (seat != null && PlanarDistanceToPlayer(seat.position) < 0.9f && !Hud.CaseFileOpen) Complete(S.OBJ_TakeSeat);
                if (Director == null) break;
                yield return null;
            }
            if (Hud.CaseFileOpen) Hud.SetCaseFileOpen(false);

            // ── 2) การพิจารณาคดี ──
            yield return Trial(seat);
        }

        void OnObjective(string id)
        {
            if (id == S.OBJ_LeaveCourt && !_leaving) StartCoroutine(Leave());
        }

        // ───────────────────────── intro ─────────────────────────

        IEnumerator Intro()
        {
            var i = Data.intro;
            BeginCutscene();
            Cut("CAM_Establishing", false);
            StartCoroutine(Hud.Fade(0f, 2.5f));
            yield return Hud.Card(i.cardSmall, i.cardBig, i.cardSub, i.cardHold);
            yield return SayAll(i.roomShot);
            Cut("CAM_ChampDock", false);
            yield return SayAll(i.champShot);
            EndCutscene();
            yield return SayAll(i.exploring);
        }

        List<Act4Evidence> CaseFileList()
        {
            var list = new List<Act4Evidence>();
            var ev = Data.caseFile.evidence;
            if (ev == null) return list;
            for (int i = 0; i < ev.Length; i++)
                if (Director == null || Director.HasEvidence(ev[i].id)) list.Add(ev[i]);
            return list;
        }

        /// <summary>ตัดไปมุมของคนที่กำลังพูด (คำบรรยาย/ความคิดในใจ = อยู่มุมเดิม)</summary>
        void ShotFor(Line line)
        {
            if (line == null) return;
            switch (line.who)
            {
                case Who.Judge: Cut("CAM_JudgesLowAngle", false); break;
                case Who.Defense: Cut("CAM_DefenseLawyer", false); break;
                case Who.Witness: Cut("CAM_Witness", false); break;
                case Who.Aunt: Cut("CAM_AuntSamorn", false); break;
                case Who.Champ: Cut("CAM_ChampDock", false); break;
                case Who.Khem: KhemCloseUp(); break;
            }
        }

        /// <summary>ช็อตหน้าเข้ม: ยืนเยื้องหน้าโต๊ะมองกลับมาที่ตัวแทนเข้ม</summary>
        void KhemCloseUp()
        {
            Vector3 feet = PlayerFeet();
            CutTo(feet + new Vector3(1.5f, 1.5f, -0.6f), feet + new Vector3(0f, 1.55f, 0f), true);
        }

        // ───────────────────────── 3) เปิดคลิป -> หักมุม -> พิพากษา ─────────────────────────

        /// <summary>เรียกจาก Act2Interactable บนจอทีวี (builder ผูก persistent listener ไว้)</summary>
        public void PlayClip()
        {
            if (_clipStarted || !Done(S.OBJ_CrossExamine)) return;
            _clipStarted = true;
            Hud.CaseFileAllowed = false;
            StartCoroutine(Verdict());
        }

        IEnumerator Verdict()
        {
            var v = Data.verdict;
            var c = Data.credibility;
            BeginCutscene();
            PlacePlayerAt(Find("POS_PlayClip"));

            // ── The Climax ──
            Cut("CAM_TVScreen", true);
            yield return Say(v.clipIntro);
            if (clip != null)
            {
                StartCoroutine(clip.Play());
                bool impact = false, voice = false;
                while (!clip.Finished)
                {
                    if (!impact && clip.ImpactHappened)
                    {
                        impact = true;
                        PlaySfx(impactSfx, clip.transform.position);
                        StartCoroutine(Shake(0.35f, 0.02f));
                    }
                    if (!voice && clip.VoiceDue)
                    {
                        voice = true;
                        StartCoroutine(Say(v.clipVoice));
                    }
                    yield return null;
                }
                yield return new WaitForSeconds(0.6f);
            }

            Cut("CAM_AuntSamorn", false);
            StartCoroutine(Stance(_aunt, standingHeight - 0.15f, 0.9f));
            yield return SayAll(v.auntJoy);
            Cut("CAM_ChampDock", false);
            yield return SayAll(v.champPale);
            KhemCloseUp();
            SetCred(c.afterClip);
            yield return SayAll(v.khemVictory);

            // ── The Twist ──
            Cut("CAM_DefenseLawyer", false);
            var stand = Find("MARK_DefenseStand");
            if (_defense != null)
            {
                StartCoroutine(Stance(_defense, standingHeight, 0.8f));
                if (stand != null) StartCoroutine(WalkTo(_defense, stand.position, 0.9f));
            }
            yield return SayAll(v.defenseRises);
            if (_defense != null && stand != null) _defense.rotation = stand.rotation;
            yield return SayAll(v.defenseQuestion);
            KhemCloseUp();
            yield return SayAll(v.khemShock);
            Cut("CAM_DefenseLawyer", false);
            yield return SayAll(v.defenseReveal);
            SetCred(c.afterTwist);

            // ── The Ruling ──
            Cut("CAM_JudgesLowAngle", false);
            yield return Say(v.inadmissible);
            if (clip != null) clip.NoSignal();
            yield return Say(v.excluded);
            Cut("CAM_Gavel", false);
            yield return GavelStrike();
            yield return Say(v.judgment);
            yield return GavelStrike();
            SetCred(c.afterVerdict);

            Cut("CAM_AuntSamorn", false);
            StartCoroutine(Stance(_aunt, seatedHeight, 1.4f));
            yield return SayAll(v.auntCollapse);

            // ── The Aftermath ──
            yield return ChampSneers(v);

            // ห้องว่าง: ตัดจอดำ ทุกคนออกไปหมด เหลือเข้มคนเดียวที่โต๊ะ
            yield return Hud.Fade(1f, 1.2f);
            EmptyCourtroom();
            Hud.ShowCredibility(false);
            PlacePlayerAt(Find(S.OBJ_TakeSeat));
            EndCutscene();
            StartCoroutine(Hud.Fade(0f, 1.6f));
            yield return SayAll(v.emptyRoom);
            Complete(S.OBJ_PlayClip);
            Hud.CaseFileAllowed = true;
            yield return SayAll(v.khemAfter);
        }

        void SetCred(float value)
        {
            _cred = value;
            Hud.SetCredibility(_cred);
        }

        IEnumerator ChampSneers(VerdictText v)
        {
            if (_champ == null) yield break;
            var sneer = Find("MARK_ChampSneer");
            yield return Stance(_champ, standingHeight, 0.5f);

            // มุมมองเข้ม: แชมป์เดินตรงเข้ามาหาถึงโต๊ะ
            PlacePlayerAt(Find(S.OBJ_TakeSeat));
            Cut("CAM_ChampSneer", false);
            bool arrived = false;
            StartCoroutine(TrackUntil(_champ, () => arrived));
            var pts = PathPoints("PATH_ChampToKhem");
            for (int i = 1; i < pts.Length; i++) yield return WalkTo(_champ, pts[i], champWalkSpeed);
            if (sneer != null) yield return Face(_champ, PlayerFeet(), 0.4f);

            yield return Say(v.champSneer1);
            // โน้มตัวเข้ามาใกล้อีกนิด
            Vector3 lean = Vector3.Lerp(_champ.position - Vector3.up * FeetOffset(_champ), PlayerFeet(), 0.3f);
            yield return WalkTo(_champ, lean, 0.8f);
            yield return Say(v.champSneer2);
            arrived = true;

            // เดินออกประตู บอดี้การ์ดลุกตาม
            StartCoroutine(ExitAlong(_champ, "PATH_ChampExit", champWalkSpeed + 0.1f, 0));
            StartCoroutine(Follow(Find("Gallery_Bodyguard_A"), 0.6f));
            StartCoroutine(Follow(Find("Gallery_Bodyguard_B"), 0.9f));
            yield return Track(_champ, HeadAbovePivot, null, 3f).WithTimeout(this, 3.2f);
        }

        /// <summary>จุดเล็งหัวนักแสดง วัดจาก pivot (กลางแคปซูล ~0.9 m เหนือพื้น) ไม่ใช่จากเท้า</summary>
        const float HeadAbovePivot = 0.6f;

        IEnumerator TrackUntil(Transform t, System.Func<bool> stop) { yield return Track(t, HeadAbovePivot, stop, 4f); }

        IEnumerator Follow(Transform guard, float delay)
        {
            if (guard == null) yield break;
            yield return new WaitForSeconds(delay);
            yield return Stance(guard, standingHeight, 0.4f);
            yield return WalkTo(guard, new Vector3(0f, 0f, guard.position.z), champWalkSpeed + 0.1f);
            yield return ExitAlong(guard, "PATH_ChampExit", champWalkSpeed + 0.1f, 2);
        }

        IEnumerator ExitAlong(Transform actor, string path, float speed, int startIndex)
        {
            yield return WalkPath(actor, path, speed, startIndex);
            Hide(actor);
        }

        /// <summary>ยกค้อนขึ้นแล้วเคาะลง หมุนรอบปลายด้าม</summary>
        IEnumerator GavelStrike()
        {
            var gavel = Find("Gavel");
            if (gavel == null) yield break;
            var parts = new List<Transform>();
            foreach (var n in new[] { "Head", "Band", "Handle" })
            {
                var p = gavel.Find(n);
                if (p != null) parts.Add(p);
            }
            var pos0 = new Vector3[parts.Count];
            var rot0 = new Quaternion[parts.Count];
            for (int i = 0; i < parts.Count; i++) { pos0[i] = parts[i].localPosition; rot0[i] = parts[i].localRotation; }
            Vector3 pivot = new Vector3(0.40f, 0.06f, 0f);   // ปลายด้าม ตามที่ builder วางไว้

            yield return Swing(parts, pos0, rot0, pivot, 0f, -48f, 0.35f);
            yield return Swing(parts, pos0, rot0, pivot, -48f, 0f, 0.06f);
            PlaySfx(gavelSfx, gavel.position);
            yield return Shake(0.25f, 0.03f);
        }

        static IEnumerator Swing(List<Transform> parts, Vector3[] pos0, Quaternion[] rot0, Vector3 pivot, float from, float to, float seconds)
        {
            float e = 0f;
            while (true)
            {
                e += Time.deltaTime;
                float k = Mathf.Clamp01(e / seconds);
                var q = Quaternion.Euler(0f, 0f, Mathf.Lerp(from, to, k * k));
                for (int i = 0; i < parts.Count; i++)
                {
                    parts[i].localPosition = pivot + q * (pos0[i] - pivot);
                    parts[i].localRotation = q * rot0[i];
                }
                if (k >= 1f) yield break;
                yield return null;
            }
        }

        /// <summary>หลังพิพากษา ทุกคนออกจากห้อง — เหลือตำรวจศาลที่ประตูคนเดียว</summary>
        void EmptyCourtroom()
        {
            var actors = Find("50_ACTORS");
            if (actors == null) return;
            foreach (Transform a in actors)
                if (a.name != "CourtPolice_Door" && a.gameObject != khemStandIn) a.gameObject.SetActive(false);
        }

        IEnumerator Leave()
        {
            _leaving = true;
            Hud.CaseFileAllowed = false;
            BeginCutscene(false);
            // Act2Director จะโหลดซีนหน้าศาลเองหลังประกาศจบซีน — ค่อย ๆ มืดรอไว้
            yield return Hud.Fade(1f, 2.5f);
        }
    }

    static class CoroutineTimeout
    {
        /// <summary>เล่น routine ไม่เกิน seconds วินาที (ใช้กับ Track ที่ไม่มีเงื่อนไขจบเอง)</summary>
        public static IEnumerator WithTimeout(this IEnumerator routine, MonoBehaviour host, float seconds)
        {
            var c = host.StartCoroutine(routine);
            yield return new WaitForSeconds(seconds);
            host.StopCoroutine(c);
        }
    }
}
