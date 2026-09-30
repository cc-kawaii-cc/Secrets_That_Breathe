using System.Collections;
using SecretsThatBreathe.Act2;
using UnityEngine;
using S = SecretsThatBreathe.Act4.Act4Script;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// ACT 4.3 - 4.5 + Post-Credit — ซีน Main4_CourtSteps (หน้าศาล ฝนตกหนัก)
    ///
    ///   4.4 ความเงียบที่ดังที่สุด: ป้าสมรเดินลงบันไดไปทรุดร้องไห้ไร้เสียง เข้มเดินเข้าไปหา (OBJ_A4_05)
    ///       ป้าลุกขึ้นเดินหนีไปโดยไม่พูดสักคำ
    ///   4.5 พิธีกรรม: เดินไปริมถนน (OBJ_A4_06) -> QTE บีบเข็มกลัด -> ขว้างลงท่อ -> monologue
    ///   Post-Credit: มุมหลังไหล่หมออรินทร์ในรถสีดำฝั่งตรงข้าม รินชา เสียงบรรยาย
    ///   จบด้วยโลโก้ EPISODE 1: THE SHATTERED SCALES
    ///
    /// บทพูด การ์ด ความเร็ว ระยะ โลโก้ อยู่ใน <see cref="data"/> (Act4_CourtStepsData.asset) แก้ใน Inspector ได้
    /// </summary>
    public class CourtStepsSequence : Act4Sequence
    {
        [Header("ข้อมูลบท (แก้บท/ความเร็ว/ระยะ ที่ asset นี้)")]
        public Act4CourtStepsData data;

        [Header("ระบบ")]
        public PinCrushQTE qte;
        [Tooltip("วัสดุน้ำชา (builder ใส่ให้)")]
        public Material teaMaterial;

        [Header("เสียง (เว้นว่างได้)")]
        [Tooltip("เสียงฝน — ถ้าใส่ จะถูกหรี่ลงตอนป้าสมรเดินหนี (ความเงียบที่ดังที่สุด)")]
        public AudioSource rainAudio;
        public AudioClip splashSfx;

        Transform _aunt;
        bool _auntDown;

        /// <summary>ข้อมูลที่ใช้เล่นจริง — ถ้าไม่ได้ใส่ asset จะใช้ค่าเริ่มต้น</summary>
        public Act4CourtStepsData Data
        {
            get
            {
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<Act4CourtStepsData>();
                    Debug.LogWarning("[Act4] CourtStepsSequence ไม่ได้ใส่ data asset — ใช้ค่าเริ่มต้นแทน (แก้ใน Inspector ไม่ได้)");
                }
                return data;
            }
        }

        protected override Act4Cast Cast { get { return Data.cast; } }

        static void Complete(string id) { if (Director != null) Director.Complete(id); }

        IEnumerator Start()
        {
            yield return WaitForSystems();
            var d = Data;
            _aunt = Find("Aunt_Samorn");

            // ── เปิดฉาก: ออกจากประตูศาล ฝนตกหนัก ──
            BeginCutscene();
            Cut("CAM_StairsWide", true);
            StartCoroutine(Hud.Fade(0f, 2.5f));
            yield return Hud.Card(d.cardSmall, d.cardBig, d.cardSub, d.cardHold);
            EndCutscene();

            StartCoroutine(AuntDescends());
            yield return SayAll(d.khemSeesAunt);

            // ── 4.4 เข้าไปหาป้า (ต้องรอป้าทรุดลงก่อน) ──
            while (!(_auntDown && _aunt != null && PlanarDistanceToPlayer(_aunt.position) < d.reachDistance)) yield return null;
            yield return AuntWalksAway();
            Complete(S.OBJ_ReachAunt);

            // ── 4.5 เดินไปริมถนน ──
            var spot = Find(S.OBJ_Ritual);
            while (spot != null && PlanarDistanceToPlayer(spot.position) > d.ritualDistance) yield return null;
            yield return Ritual(spot);
            yield return PostCredit();
            yield return EndCard();
        }

        // ───────────────────────── 4.3 / 4.4 ─────────────────────────

        IEnumerator AuntDescends()
        {
            if (_aunt == null) yield break;
            yield return new WaitForSeconds(0.5f);
            yield return WalkPath(_aunt, "PATH_AuntDescend", Data.auntDescendSpeed, 1);
            var mark = Find("MARK_AuntCollapse");
            if (mark != null) yield return Face(_aunt, _aunt.position + mark.forward, 0.6f);
            yield return Stance(_aunt, Data.auntCollapsedHeight, 1.3f);
            _auntDown = true;
            if (!InCutscene) yield return SayAll(Data.auntCollapsed);
        }

        IEnumerator AuntWalksAway()
        {
            var d = Data;
            BeginCutscene();
            // มุมตาเข้ม หันไปหาป้า
            var cam = Cam.transform;
            Vector3 head = _aunt.position + Vector3.up * 0.4f;
            yield return BlendTo(cam.position, Quaternion.LookRotation(head - cam.position), 0.7f);
            yield return SayAll(d.khemReaches);

            // ป้าลุกขึ้น หันหลังให้ แล้วเดินจากไป
            yield return Stance(_aunt, 1.7f, 1.1f);
            var pts = PathPoints("PATH_AuntWalkAway");
            if (pts.Length > 1) yield return Face(_aunt, pts[1], 0.7f);
            PlacePlayerAt(Find("MARK_KhemComfort"));
            StartCoroutine(AuntLeaves());
            Cut("CAM_AuntWalkAway_Back", true);
            float rain0 = rainAudio != null ? rainAudio.volume : 1f;
            StartCoroutine(DuckRain(rain0 * d.rainDuckVolume, 2f));
            yield return SayAll(d.auntLeaves);
            yield return SayAll(d.khemGuilt);
            StartCoroutine(DuckRain(rain0, 3f));
            EndCutscene();
        }

        IEnumerator AuntLeaves()
        {
            yield return WalkPath(_aunt, "PATH_AuntWalkAway", Data.auntLeaveSpeed, 1);
            Hide(_aunt);
        }

        IEnumerator DuckRain(float to, float seconds)
        {
            if (rainAudio == null) yield break;
            float from = rainAudio.volume, e = 0f;
            while (e < seconds)
            {
                e += Time.deltaTime;
                rainAudio.volume = Mathf.Lerp(from, to, e / seconds);
                yield return null;
            }
        }

        // ───────────────────────── 4.5 พิธีกรรม ─────────────────────────

        IEnumerator Ritual(Transform spot)
        {
            BeginCutscene();
            PlacePlayerAt(spot);

            // มุมตาเข้ม ก้มมองมือที่กำเข็มกลัด
            Vector3 eye = PlayerFeet() + Vector3.up * 1.6f;
            Vector3 fwd = spot != null ? spot.forward : Vector3.back;
            CutTo(eye, eye + fwd * 1.0f + Vector3.down * 0.65f, false);
            yield return SayAll(Data.ritualIntro);

            if (qte != null)
            {
                yield return qte.Run(Cam.transform, PlayerFeet().y);
                // กระชากเข็มกลัดขว้างลงท่อ
                var drain = Find("DRAIN_Target");
                Vector3 target = drain != null ? drain.position + Vector3.up * 0.02f : eye + fwd * 1.5f + Vector3.down * 1.7f;
                StartCoroutine(qte.Throw(target, 0.8f));
                yield return new WaitForSeconds(0.4f);
                Cut("CAM_Drain_Top", false);
                while (!qte.PinLanded) yield return null;
                if (splashSfx != null) AudioSource.PlayClipAtPoint(splashSfx, target);
                yield return new WaitForSeconds(0.9f);
                qte.HideHand();
            }

            // monologue: มุมต่ำมองขึ้น ค่อย ๆ ดันกล้องเข้าไป
            Cut("CAM_QTE_LowHero", true);
            StartCoroutine(PushIn(0.7f, 7f));
            yield return SayAll(Data.monologue);
        }

        // ───────────────────────── Post-Credit ─────────────────────────

        IEnumerator PostCredit()
        {
            yield return Hud.Fade(1f, 1.5f);
            yield return new WaitForSeconds(0.8f);

            Cut("CAM_PostCredit_BehindArin", true);   // เห็นเข้มยืนตากฝนอยู่ริมฟุตบาทผ่านกระจกรถ
            StartCoroutine(Hud.Fade(0f, 2f));
            yield return SayAll(Data.carIntro);

            Cut("CAM_PostCredit_TeaPour", false);
            StartCoroutine(PourTea());
            yield return SayAll(Data.teaPour);

            Cut("CAM_PostCredit_BehindArin", true);
            StartCoroutine(PushIn(0.25f, 6f));
            yield return SayAll(Data.finalWords);
            yield return Hud.Fade(1f, 1.4f);
        }

        /// <summary>รินชา: เอียงกาน้ำชา สายน้ำชาไหลลงถ้วย น้ำในถ้วยค่อย ๆ เต็ม</summary>
        IEnumerator PourTea()
        {
            var set = Find("TeaSet");
            if (set == null) yield break;
            var pot = set.Find("Teapot");
            var spout = set.Find("Teapot_Spout");
            var cup = set.Find("Cup");
            if (pot == null || cup == null) yield break;

            Vector3 pivot = pot.localPosition;
            Vector3 spoutP0 = spout != null ? spout.localPosition : Vector3.zero;
            Quaternion spoutR0 = spout != null ? spout.localRotation : Quaternion.identity;
            Quaternion potR0 = pot.localRotation;

            GameObject stream = null, tea = null;
            float e = 0f;
            while (e < 4.2f)
            {
                e += Time.deltaTime;
                // เอียง 0 -> 38 องศาใน 1 วินาที ค้างไว้ แล้วตั้งกลับตอนท้าย
                float tilt = e < 1f ? Mathf.SmoothStep(0f, 38f, e) : (e > 3.3f ? Mathf.SmoothStep(38f, 0f, (e - 3.3f) / 0.9f) : 38f);
                var q = Quaternion.Euler(0f, 0f, -tilt);
                pot.localRotation = q * potR0;
                if (spout != null)
                {
                    spout.localPosition = pivot + q * (spoutP0 - pivot);
                    spout.localRotation = q * spoutR0;
                }

                bool pouring = e > 1f && e < 3.3f;
                if (pouring && stream == null && teaMaterial != null)
                {
                    stream = MakeTea("TeaStream", set);
                    tea = MakeTea("TeaInCup", set);
                    tea.transform.localPosition = cup.localPosition + Vector3.up * 0.012f;
                    tea.transform.localScale = new Vector3(0.062f, 0.001f, 0.062f);
                }
                if (stream != null)
                {
                    stream.SetActive(pouring);
                    Vector3 tip = spout != null ? spout.localPosition + q * new Vector3(0.04f, 0.02f, 0f) : pivot;
                    Vector3 bottom = cup.localPosition + Vector3.up * 0.02f;
                    stream.transform.localPosition = (tip + bottom) * 0.5f;
                    stream.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (tip - bottom).normalized);
                    stream.transform.localScale = new Vector3(0.006f, (tip - bottom).magnitude * 0.5f, 0.006f);
                }
                if (tea != null && pouring)
                {
                    float fill = Mathf.InverseLerp(1f, 3.3f, e);
                    tea.transform.localPosition = cup.localPosition + Vector3.up * (0.005f + fill * 0.042f);
                    tea.transform.localScale = new Vector3(0.062f, 0.002f, 0.062f);
                }
                yield return null;
            }
        }

        GameObject MakeTea(string name, Transform parent)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = name;
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.GetComponent<Renderer>().sharedMaterial = teaMaterial;
            return g;
        }

        // ───────────────────────── โลโก้ตอนจบ ─────────────────────────

        IEnumerator EndCard()
        {
            var d = Data;
            Hud.ShowTitle(d.titleSmall, d.titleBig, d.titleSub);
            yield return Hud.TitleAlpha(1f, 2f);
            yield return new WaitForSeconds(d.titleHold);
            // ปิด objective สุดท้าย -> Act2Director ประกาศ "จบ EPISODE 1"
            Complete(S.OBJ_Ritual);

            string menu = d.menuScene;
            if (string.IsNullOrEmpty(menu) || !Application.CanStreamedLevelBeLoaded(menu)) yield break;
            yield return new WaitForSeconds(4.5f);
            Hud.ShowLine("", d.menuPrompt, false);
            while (!AdvancePressed()) yield return null;
            if (GameManager.Instance != null) GameManager.Instance.LoadScene(menu);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(menu);
        }
    }
}
