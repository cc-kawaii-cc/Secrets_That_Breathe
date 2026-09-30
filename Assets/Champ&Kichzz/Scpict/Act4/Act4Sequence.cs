using System.Collections;
using System.Collections.Generic;
using SecretsThatBreathe.Act2;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// เครื่องมือคัตซีนที่ทั้งสองซีนของ ACT 4 ใช้ร่วมกัน
    ///
    /// กล้อง: ยืมกล้องของผู้เล่นมาตัดไปตามหมุด CAM_* ที่ builder วางไว้ แบบเดียวกับ ChampArrivalSequence
    ///        (ถอดออกจากตัวผู้เล่นก่อน แล้วคืนตำแหน่งเดิมตอนจบ)
    /// บทพูด: ขึ้นในกล่องของ Act4HUD ไม่ใช้ DialogueManager เพราะตัวนั้นคืนสถานะเป็น Exploration
    ///        ทุกครั้งที่คุยจบ — กลางคัตซีนผู้เล่นจะได้การควบคุมคืนมาหนึ่งเฟรมแล้วกล้องกระตุก
    /// นักแสดง: เดินตามหมุด PATH_*/Point_xx ลุก/นั่ง/ทรุด ด้วยการยืดหดแคปซูล (ตัวแทนก่อนมีโมเดลจริง)
    /// ตัวเข้ม: ผู้เล่นไม่มีตัว (มุมมองบุคคลที่หนึ่ง) ช็อตมุมที่สามจึงเปิดตัวแทน Khem_StandIn ไว้ยืนตรงตำแหน่งผู้เล่น
    /// </summary>
    public abstract class Act4Sequence : MonoBehaviour
    {
        [Header("ตัวแทนเข้มในช็อตมุมที่สาม")]
        public GameObject khemStandIn;

        [Header("เวลาอ่านบท")]
        [Tooltip("วินาทีพื้นฐานต่อบรรทัด")]
        public float lineBaseSeconds = 1.8f;
        [Tooltip("วินาทีเพิ่มต่อตัวอักษร")]
        public float lineSecondsPerChar = 0.05f;

        protected static Act4HUD Hud { get { return Act4HUD.Instance; } }
        protected static Act2Director Director { get { return Act2Director.Instance; } }

        Camera _cam;
        Transform _camParent;
        Vector3 _camLocalPos;
        Quaternion _camLocalRot;
        float _camNear;
        bool _camTaken;
        Vector3 _shakeOffset;
        readonly Dictionary<string, Transform> _cache = new Dictionary<string, Transform>();

        protected Camera Cam
        {
            get
            {
                if (_cam == null && PlayerManager.Instance != null) _cam = PlayerManager.Instance.playerCamera;
                if (_cam == null) _cam = Camera.main;
                return _cam;
            }
        }

        protected Transform Player
        {
            get { return PlayerManager.Instance != null && PlayerManager.Instance.playerRoot != null ? PlayerManager.Instance.playerRoot.transform : null; }
        }

        protected bool InCutscene { get; private set; }

        // ───────────────────────── สถานะ ─────────────────────────

        protected void BeginCutscene(bool letterbox = true)
        {
            InCutscene = true;
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Cutscene);
            if (Hud != null) Hud.SetLetterbox(letterbox);
            SetGameHudVisible(false);
            TakeCamera();
        }

        protected void EndCutscene()
        {
            ReleaseCamera();
            ShowKhem(false);
            if (Hud != null) { Hud.SetLetterbox(false); Hud.HideLine(); }
            SetGameHudVisible(true);
            InCutscene = false;
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Exploration);
        }

        /// <summary>ซ่อน HUD เป้าหมาย/หมุดนำทางของ ACT 2 ระหว่างคัตซีน ให้ภาพสะอาด</summary>
        protected static void SetGameHudVisible(bool visible)
        {
            var hud = FindAnyObjectByType<Act2HUD>();
            if (hud == null) return;
            foreach (var c in hud.GetComponentsInChildren<Canvas>(true)) c.enabled = visible;
        }

        /// <summary>รอให้ manager ตั้งตัวเสร็จ (GameManager.Start สั่ง Exploration ในเฟรมแรก จะทับคัตซีนที่เริ่มเร็วกว่า)</summary>
        protected IEnumerator WaitForSystems()
        {
            yield return null;
            yield return null;
            float t = 0f;
            while ((GameManager.Instance == null || Hud == null || PlayerManager.Instance == null) && t < 3f)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        // ───────────────────────── กล้อง ─────────────────────────

        protected void TakeCamera()
        {
            if (_camTaken || Cam == null) return;
            _camParent = _cam.transform.parent;
            _camLocalPos = _cam.transform.localPosition;
            _camLocalRot = _cam.transform.localRotation;
            _camNear = _cam.nearClipPlane;
            // ถอดออกจากตัวผู้เล่นก่อน ไม่งั้นย้ายตัวผู้เล่น (วาร์ปเข้าที่นั่ง) แล้วกล้องจะถูกลากตามไปด้วย
            _cam.transform.SetParent(null, true);
            // ช็อตใกล้ (ค้อน มือ ถ้วยชา) อยู่ใกล้กว่า near 0.3 ของกล้องผู้เล่น
            _cam.nearClipPlane = 0.03f;
            _camTaken = true;
        }

        protected void ReleaseCamera()
        {
            if (!_camTaken) return;
            _camTaken = false;
            if (_cam == null) return;
            _cam.transform.SetParent(_camParent, false);
            _cam.transform.localPosition = _camLocalPos;
            _cam.transform.localRotation = _camLocalRot;
            _cam.nearClipPlane = _camNear;
            _shakeOffset = Vector3.zero;
        }

        /// <summary>ตัดไปยังหมุดกล้อง showKhem = เปิดตัวแทนเข้ม (ช็อตมุมที่สามที่เห็นตัวเข้ม)</summary>
        protected void Cut(string marker, bool showKhem = true)
        {
            var t = Find(marker);
            if (t == null || Cam == null) { Debug.LogWarning("[Act4] ไม่พบหมุดกล้อง " + marker); return; }
            TakeCamera();
            _cam.transform.SetPositionAndRotation(t.position, t.rotation);
            ShowKhem(showKhem);
        }

        protected void CutTo(Vector3 pos, Vector3 lookAt, bool showKhem = true)
        {
            if (Cam == null) return;
            TakeCamera();
            Vector3 d = lookAt - pos;
            _cam.transform.SetPositionAndRotation(pos, d.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(d) : _cam.transform.rotation);
            ShowKhem(showKhem);
        }

        /// <summary>เลื่อนกล้องจากตำแหน่งปัจจุบันไปยังหมุดแบบนุ่ม ๆ</summary>
        protected IEnumerator Blend(string marker, float seconds)
        {
            var t = Find(marker);
            if (t == null || Cam == null) yield break;
            yield return BlendTo(t.position, t.rotation, seconds);
        }

        protected IEnumerator BlendTo(Vector3 pos, Quaternion rot, float seconds)
        {
            TakeCamera();
            Vector3 p0 = _cam.transform.position;
            Quaternion r0 = _cam.transform.rotation;
            float e = 0f;
            while (e < seconds)
            {
                e += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, e / seconds);
                _cam.transform.SetPositionAndRotation(Vector3.Lerp(p0, pos, k), Quaternion.Slerp(r0, rot, k));
                yield return null;
            }
            _cam.transform.SetPositionAndRotation(pos, rot);
        }

        /// <summary>ดันกล้องเข้าหาจุดที่มองอยู่ช้า ๆ (push-in) ระหว่างรอบทพูด</summary>
        protected IEnumerator PushIn(float metres, float seconds)
        {
            if (Cam == null) yield break;
            Vector3 dir = _cam.transform.forward;
            float e = 0f, moved = 0f;
            while (e < seconds)
            {
                e += Time.deltaTime;
                float target = metres * Mathf.SmoothStep(0f, 1f, e / seconds);
                _cam.transform.position += dir * (target - moved);
                moved = target;
                yield return null;
            }
        }

        /// <summary>กล้องมองตามเป้าเคลื่อนที่ไปเรื่อย ๆ ขณะ routine อื่นกำลังเล่น</summary>
        protected IEnumerator Track(Transform target, float height, System.Func<bool> until, float turnSpeed = 5f)
        {
            while (target != null && Cam != null && (until == null || !until()))
            {
                Vector3 d = target.position + Vector3.up * height - _cam.transform.position;
                if (d.sqrMagnitude > 1e-4f)
                    _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, Quaternion.LookRotation(d),
                                                               1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
                yield return null;
            }
        }

        protected IEnumerator Shake(float seconds, float amplitude)
        {
            if (Cam == null) yield break;
            float e = 0f;
            while (e < seconds)
            {
                e += Time.deltaTime;
                float a = amplitude * (1f - e / seconds);
                Vector3 next = new Vector3(Random.Range(-a, a), Random.Range(-a, a), 0f);
                _cam.transform.position += _cam.transform.rotation * (next - _shakeOffset);
                _shakeOffset = next;
                yield return null;
            }
            _cam.transform.position -= _cam.transform.rotation * _shakeOffset;
            _shakeOffset = Vector3.zero;
        }

        // ───────────────────────── บทพูด ─────────────────────────

        protected static bool AdvancePressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                return true;
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        /// <summary>
        /// ขึ้นบทหนึ่งบรรทัด แล้วรอจนครบเวลาอ่าน (คำนวณจากความยาว) หรือกดข้าม
        /// กดข้ามได้เฉพาะตอนอยู่ในคัตซีน — ตอนเดินเล่น Space คือปุ่มกระโดด
        /// </summary>
        protected IEnumerator Say(string speaker, string text, float minSeconds = 0f)
        {
            if (Hud == null) yield break;
            bool skippable = InCutscene;
            Hud.ShowLine(speaker, text, skippable);
            float seconds = Mathf.Max(minSeconds, lineBaseSeconds + text.Length * lineSecondsPerChar);
            float e = 0f;
            while (e < seconds)
            {
                e += Time.deltaTime;
                // ครึ่งวินาทีแรกไม่รับปุ่ม กันกดรัวแล้วข้ามทีละหลายบรรทัด
                if (skippable && e > 0.45f && AdvancePressed()) break;
                yield return null;
            }
            Hud.HideLine();
        }

        /// <summary>ชื่อบนจอของแต่ละบทบาท (มาจาก data asset ของซีน)</summary>
        protected abstract Act4Cast Cast { get; }

        /// <summary>ชื่อพยานที่อยู่ในคอกพยานตอนนี้ (ใช้กับ Who.Witness)</summary>
        protected virtual string WitnessName { get { return null; } }

        protected string SpeakerName(Line line)
        {
            var cast = Cast ?? new Act4Cast();
            return cast.NameOf(line, WitnessName);
        }

        /// <summary>ขึ้นบทหนึ่งบรรทัดจาก data (ค้างอย่างน้อย hold ของบรรทัดนั้น)</summary>
        protected IEnumerator Say(Line line, float minSeconds = 0f)
        {
            if (line == null || string.IsNullOrEmpty(line.text)) return Nothing();
            return Say(SpeakerName(line), line.text, Mathf.Max(minSeconds, line.hold));
        }

        protected IEnumerator SayAll(Line[] lines)
        {
            if (lines == null) yield break;
            for (int i = 0; i < lines.Length; i++) yield return Say(lines[i]);
        }

        static IEnumerator Nothing() { yield break; }

        // ───────────────────────── ผู้เล่น ─────────────────────────

        /// <summary>วาร์ปผู้เล่นไปยืนที่หมุด (หมุดอยู่ระดับพื้น) หันตามหมุด</summary>
        protected void PlacePlayerAt(Transform marker)
        {
            var pm = PlayerManager.Instance;
            if (marker == null || pm == null || pm.playerRoot == null) return;
            float lift = 0.9f;
            if (pm.controller != null)
                lift = (pm.controller.height * 0.5f - pm.controller.center.y) * Mathf.Abs(pm.playerRoot.transform.lossyScale.y) + 0.02f;
            pm.TeleportTo(marker.position + Vector3.up * lift, Quaternion.Euler(0f, marker.eulerAngles.y, 0f));
            if (pm.movement != null) pm.movement.ResetMotion();
        }

        /// <summary>ตำแหน่งเท้าของผู้เล่นตอนนี้</summary>
        protected Vector3 PlayerFeet()
        {
            var p = Player;
            if (p == null) return Vector3.zero;
            var pm = PlayerManager.Instance;
            float lift = 0.9f;
            if (pm != null && pm.controller != null)
                lift = (pm.controller.height * 0.5f - pm.controller.center.y) * Mathf.Abs(p.lossyScale.y);
            return p.position - Vector3.up * lift;
        }

        protected float PlanarDistanceToPlayer(Vector3 point)
        {
            var p = Player;
            if (p == null) return float.MaxValue;
            Vector3 d = p.position - point;
            d.y = 0f;
            return d.magnitude;
        }

        /// <summary>เปิด/ปิดตัวแทนเข้ม วางตรงเท้าผู้เล่น หันทางเดียวกัน</summary>
        protected void ShowKhem(bool on)
        {
            if (khemStandIn == null) return;
            if (on && Player != null)
            {
                var feet = PlayerFeet();
                khemStandIn.SetActive(true);
                PlaceActorFeet(khemStandIn.transform, feet);
                khemStandIn.transform.rotation = Quaternion.Euler(0f, Player.eulerAngles.y, 0f);
            }
            else khemStandIn.SetActive(false);
        }

        // ───────────────────────── หา object ในซีน ─────────────────────────

        protected Transform Find(string name)
        {
            Transform t;
            if (_cache.TryGetValue(name, out t) && t != null) return t;
            var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name.Trim() != name) continue;
                if (!all[i].gameObject.scene.IsValid()) continue;   // ข้าม prefab asset
                _cache[name] = all[i];
                return all[i];
            }
            return null;
        }

        /// <summary>หมุดของเส้นทาง PATH_xxx เรียงตาม Point_00, Point_01 ...</summary>
        protected Vector3[] PathPoints(string pathName)
        {
            var root = Find(pathName);
            if (root == null) return new Vector3[0];
            var list = new List<Transform>();
            foreach (Transform c in root) if (c.name.StartsWith("Point_")) list.Add(c);
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var pts = new Vector3[list.Count];
            for (int i = 0; i < pts.Length; i++) pts[i] = list[i].position;
            return pts;
        }

        // ───────────────────────── นักแสดง ─────────────────────────

        /// <summary>ความสูงจากเท้าถึง pivot ของตัวละคร (แคปซูล pivot อยู่กลางตัว)</summary>
        protected static float FeetOffset(Transform actor)
        {
            var r = actor.GetComponent<Renderer>();
            if (r == null) r = actor.GetComponentInChildren<Renderer>();
            return r != null ? actor.position.y - r.bounds.min.y : 0f;
        }

        protected static void PlaceActorFeet(Transform actor, Vector3 feet)
        {
            actor.position = feet + Vector3.up * FeetOffset(actor);
        }

        /// <summary>เดินไปยังตำแหน่งเท้าเป้าหมาย หันหน้าตามทิศเดิน</summary>
        protected static IEnumerator WalkTo(Transform actor, Vector3 feetTarget, float speed, float turnSpeed = 8f)
        {
            if (actor == null) yield break;
            float off = FeetOffset(actor);
            Vector3 target = feetTarget + Vector3.up * off;
            while (actor != null)
            {
                Vector3 flat = target - actor.position;
                flat.y = 0f;
                if (flat.magnitude <= 0.05f && Mathf.Abs(target.y - actor.position.y) < 0.05f) { actor.position = target; yield break; }
                if (flat.sqrMagnitude > 1e-4f)
                    actor.rotation = Quaternion.Slerp(actor.rotation, Quaternion.LookRotation(flat.normalized),
                                                      1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
                actor.position = Vector3.MoveTowards(actor.position, target, speed * Time.deltaTime);
                yield return null;
            }
        }

        protected IEnumerator WalkPath(Transform actor, string pathName, float speed, int startIndex = 0)
        {
            var pts = PathPoints(pathName);
            for (int i = startIndex; i < pts.Length; i++) yield return WalkTo(actor, pts[i], speed);
        }

        protected static IEnumerator Face(Transform actor, Vector3 point, float seconds)
        {
            if (actor == null) yield break;
            Vector3 d = point - actor.position;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-4f) yield break;
            Quaternion r0 = actor.rotation, r1 = Quaternion.LookRotation(d.normalized);
            float e = 0f;
            while (e < seconds)
            {
                e += Time.deltaTime;
                actor.rotation = Quaternion.Slerp(r0, r1, Mathf.SmoothStep(0f, 1f, e / seconds));
                yield return null;
            }
            actor.rotation = r1;
        }

        /// <summary>
        /// ลุก/นั่ง/ทรุด: ยืดหดแคปซูลให้สูง height เมตร โดยเท้ายังแตะพื้นเดิม
        /// หัวกับแผ่นหน้าที่ builder ติดไว้จะถูกปรับสเกลกลับ ไม่ให้หัวยืดไปพร้อมตัว
        /// </summary>
        protected static IEnumerator Stance(Transform actor, float height, float seconds)
        {
            if (actor == null) yield break;
            float feet = actor.position.y - FeetOffset(actor);
            Vector3 s0 = actor.localScale;
            float h0 = s0.y * 2f;
            float e = 0f;
            while (e < seconds)
            {
                e += Time.deltaTime;
                SetHeight(actor, Mathf.Lerp(h0, height, Mathf.SmoothStep(0f, 1f, e / Mathf.Max(0.0001f, seconds))), feet);
                yield return null;
            }
            SetHeight(actor, height, feet);
        }

        protected static void SetHeight(Transform actor, float height, float feetY)
        {
            var s = actor.localScale;
            s.y = height * 0.5f;             // แคปซูล default สูง 2 หน่วย
            actor.localScale = s;
            Vector3 p = actor.position;
            p.y = feetY + height * 0.5f;
            actor.position = p;

            // ตัวเลขตรงกับ Ch4Kit.Person: หัวต่ำจากยอด 14 cm ดันไปหน้า 10 cm, แผ่นหน้าอยู่หน้าหัวอีก 11.5 cm
            Vector3 ls = actor.lossyScale;
            var head = actor.Find("Head");
            if (head != null)
            {
                head.localScale = new Vector3(0.24f / ls.x, 0.26f / ls.y, 0.24f / ls.z);
                head.localPosition = new Vector3(0f, 1f - 0.14f / ls.y, 0.10f / ls.z);
            }
            var visor = actor.Find("FacingVisor");
            if (visor != null)
            {
                visor.localScale = new Vector3(0.16f / ls.x, 0.045f / ls.y, 0.04f / ls.z);
                visor.localPosition = new Vector3(0f, 1f - 0.11f / ls.y, 0.215f / ls.z);
            }
        }

        protected static void Hide(Transform actor) { if (actor != null) actor.gameObject.SetActive(false); }

        /// <summary>เล่น coroutine สองตัวพร้อมกัน แล้วรอให้จบทั้งคู่</summary>
        protected IEnumerator Both(IEnumerator a, IEnumerator b)
        {
            bool doneA = false, doneB = false;
            StartCoroutine(Run(a, () => doneA = true));
            StartCoroutine(Run(b, () => doneB = true));
            while (!doneA || !doneB) yield return null;
        }

        static IEnumerator Run(IEnumerator routine, System.Action done)
        {
            yield return routine;
            done();
        }

        protected static void PlaySfx(AudioClip clip, Vector3 at, float volume = 1f)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, at, volume);
        }
    }
}
