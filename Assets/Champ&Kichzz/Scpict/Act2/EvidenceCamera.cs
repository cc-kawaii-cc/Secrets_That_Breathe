using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// โหมดกล้องถ่ายหลักฐาน
    ///
    /// ไม่ใช่แค่กดปุ่มแล้วผ่าน — ต้องเล็งให้เป้าหมายเต็มกรอบและอยู่กลางจอจริงๆ
    /// ทำให้ผู้เล่นต้องเดินเข้าไปใกล้ ซึ่งคือจุดที่เสี่ยงโดนยามเห็นที่สุด
    /// การถ่ายรูปเลยกลายเป็นการตัดสินใจ ไม่ใช่พิธีกรรม
    /// </summary>
    public class EvidenceCamera : MonoBehaviour
    {
        public static EvidenceCamera Instance { get; private set; }

        public static event Action<bool> OnModeChanged;
        /// <summary>คุณภาพการจัดเฟรม 0..1 กับข้อความบอกใบ้</summary>
        public static event Action<float, string> OnFramingChanged;
        public static event Action<PhotoSubject> OnCaptured;

        [Header("Input")]
        [Tooltip("ปุ่มเปิด/ปิดโหมดกล้อง (เว้นว่างจะใช้ Q)")]
        public InputActionReference toggleAction;
        [Tooltip("ปุ่มกดชัตเตอร์ (เว้นว่างจะใช้คลิกซ้าย)")]
        public InputActionReference shutterAction;

        [Header("การจัดเฟรม")]
        [Tooltip("คุณภาพขั้นต่ำที่กดชัตเตอร์ได้")]
        [Range(0.2f, 1f)] public float requiredQuality = 0.62f;
        [Tooltip("ซูมตอนเปิดโหมดกล้อง (ยิ่งน้อยยิ่งซูมเข้า)")]
        public float zoomFov = 38f;
        public float zoomSpeed = 8f;
        [Tooltip("เดินช้าลงตอนยกกล้อง")]
        [Range(0.1f, 1f)] public float moveMultiplier = 0.5f;

        public bool IsAiming { get; private set; }
        public float Quality { get; private set; }
        public PhotoSubject Target { get; private set; }

        Camera _cam;
        PlayerMovement _movement;
        CrouchAbility _crouch;
        InputAction _toggleFallback, _shutterFallback;
        float _defaultFov;

        void Awake()
        {
            Instance = this;
            _movement = GetComponent<PlayerMovement>();
            _crouch = GetComponent<CrouchAbility>();
            if (toggleAction == null)
            {
                _toggleFallback = new InputAction("PhotoMode", InputActionType.Button, "<Keyboard>/q");
                _toggleFallback.AddBinding("<Gamepad>/buttonNorth");
            }
            if (shutterAction == null)
            {
                _shutterFallback = new InputAction("Shutter", InputActionType.Button, "<Mouse>/leftButton");
                _shutterFallback.AddBinding("<Gamepad>/rightTrigger");
            }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnEnable()
        {
            if (toggleAction != null) toggleAction.action.Enable();
            if (shutterAction != null) shutterAction.action.Enable();
            if (_toggleFallback != null) _toggleFallback.Enable();
            if (_shutterFallback != null) _shutterFallback.Enable();
        }

        void OnDisable()
        {
            if (toggleAction != null) toggleAction.action.Disable();
            if (shutterAction != null) shutterAction.action.Disable();
            if (_toggleFallback != null) _toggleFallback.Disable();
            if (_shutterFallback != null) _shutterFallback.Disable();
            SetAiming(false);
        }

        bool Pressed(InputActionReference reference, InputAction fallback)
        {
            if (reference != null) return reference.action.WasPressedThisFrame();
            return fallback != null && fallback.WasPressedThisFrame();
        }

        void Update()
        {
            if (_cam == null)
            {
                _cam = PlayerManager.Instance != null ? PlayerManager.Instance.playerCamera : Camera.main;
                if (_cam == null) return;
                _defaultFov = _cam.fieldOfView;
            }

            bool canPlay = GameManager.Instance == null || GameManager.Instance.IsState(GameState.Exploration);
            if (!canPlay) { SetAiming(false); return; }

            if (Pressed(toggleAction, _toggleFallback)) SetAiming(!IsAiming);

            float wantFov = IsAiming ? zoomFov : _defaultFov;
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, wantFov, zoomSpeed * Time.deltaTime);

            if (!IsAiming) return;

            // ยกกล้องแล้วเดินช้าลง แต่ยังหมอบได้ (หมอบคูณทับอีกที)
            if (_movement != null)
            {
                float crouchMul = _crouch != null ? Mathf.Lerp(1f, _crouch.crouchSpeedMultiplier, _crouch.CrouchAmount) : 1f;
                _movement.speedMultiplier = crouchMul * moveMultiplier;
            }

            string hint;
            float quality;
            Target = FindBestSubject(out quality, out hint);
            Quality = quality;
            if (OnFramingChanged != null) OnFramingChanged(Quality, hint);

            if (Pressed(shutterAction, _shutterFallback)) TryCapture();
        }

        void SetAiming(bool value)
        {
            if (IsAiming == value) return;
            IsAiming = value;
            if (!value)
            {
                Quality = 0f;
                Target = null;
                if (_movement != null && _crouch != null)
                    _movement.speedMultiplier = Mathf.Lerp(1f, _crouch.crouchSpeedMultiplier, _crouch.CrouchAmount);
                else if (_movement != null) _movement.speedMultiplier = 1f;
            }
            if (OnModeChanged != null) OnModeChanged(value);
        }

        void TryCapture()
        {
            if (Target == null)
            {
                if (Act2Director.Instance != null) Act2Director.Instance.Notice("ไม่มีอะไรน่าถ่ายตรงนี้");
                return;
            }
            if (Quality < requiredQuality)
            {
                if (Act2Director.Instance != null) Act2Director.Instance.Notice("จัดเฟรมยังไม่ดีพอ — " + Target.hint);
                return;
            }

            Target.MarkCaptured();
            if (OnCaptured != null) OnCaptured(Target);
            var director = Act2Director.Instance;
            if (director != null)
            {
                director.GainEvidence(Target.evidenceId, Target.successLine);
                if (!string.IsNullOrEmpty(Target.objectiveId)) director.Complete(Target.objectiveId);
            }
            if (SubtitleManager.Instance != null && !string.IsNullOrEmpty(Target.successLine))
                SubtitleManager.Instance.Show(Target.successLine);
            SetAiming(false);
        }

        /// <summary>
        /// หา subject ที่จัดเฟรมได้ดีที่สุดในจอตอนนี้ พร้อมบอกว่าติดตรงไหน
        /// คุณภาพ = ใกล้พอ x อยู่กลางจอ x ไม่มีอะไรบัง
        /// </summary>
        PhotoSubject FindBestSubject(out float bestQuality, out string hint)
        {
            bestQuality = 0f;
            hint = "หาเป้าหมาย...";
            PhotoSubject best = null;

            var subjects = FindObjectsByType<PhotoSubject>(FindObjectsSortMode.None);
            for (int i = 0; i < subjects.Length; i++)
            {
                var s = subjects[i];
                if (s == null || s.Captured) continue;

                Bounds b;
                if (!s.TryGetBounds(out b)) continue;

                float dist = Vector3.Distance(_cam.transform.position, b.center);
                if (dist > s.maxDistance) continue;

                Rect rect;
                if (!ScreenRect(b, out rect)) continue;

                float coverage = (rect.width * rect.height) / (Screen.width * (float)Screen.height);
                float coverageScore = Mathf.Clamp01(coverage / Mathf.Max(0.001f, s.minScreenCoverage));

                Vector2 screenCentre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                float offset = Vector2.Distance(rect.center, screenCentre);
                float maxOffset = screenCentre.magnitude;
                float centreScore = Mathf.Clamp01(1f - offset / (maxOffset * 0.55f));

                // กันชนที่พังอยู่ "ในกล่อง collider ของตัวรถ" การยิงเส้นไปหามันจึงชนตัวรถทุกครั้ง
                // ถ้านับว่าถูกบัง คุณภาพจะเป็นศูนย์ตลอดและกดชัตเตอร์ไม่ผ่านไม่ว่ายืนตรงไหน
                bool blocked = Occluded(s, b);

                float q = blocked ? 0f : coverageScore * centreScore;
                if (q <= bestQuality) continue;

                bestQuality = q;
                best = s;
                hint = blocked ? "มีอะไรบังอยู่"
                     : coverageScore < 0.75f ? "เข้าไปใกล้อีก"
                     : centreScore < 0.75f ? "เล็งให้อยู่กลางกรอบ"
                     : "ได้แล้ว — กดชัตเตอร์";
            }
            return best;
        }

        /// <summary>
        /// มีอะไรบังเป้าจริงไหม
        ///
        /// ตัวเป้าเองและของที่มันติดอยู่ (ตัวรถ) ไม่นับ และจุดชนที่ตกอยู่ในกล่องของเป้า
        /// ก็ไม่นับ เพราะนั่นคือผิวของสิ่งที่กำลังจะถ่ายอยู่แล้ว
        /// </summary>
        bool Occluded(PhotoSubject s, Bounds b)
        {
            Vector3 from = _cam.transform.position;
            Vector3 dir = b.center - from;
            float len = dir.magnitude;
            if (len < 0.01f) return false;

            Bounds slack = b;
            slack.Expand(0.35f);

            var hits = Physics.RaycastAll(from, dir / len, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Transform t = hits[i].collider.transform;
                if (t.IsChildOf(s.transform)) continue;
                if (s.partOf != null && t.IsChildOf(s.partOf)) continue;
                if (slack.Contains(hits[i].point)) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// กล่อง 3D → กรอบสี่เหลี่ยมบนจอ
        ///
        /// มุมที่อยู่หลังกล้องถูกข้ามไป ไม่ใช่ล้มทั้งการคำนวณ — ตอนเข้าไปใกล้พอที่จะกินพื้นที่จอ
        /// ตามเกณฑ์ มักจะมีสักมุมเลยระนาบกล้องไปแล้วเสมอ
        /// </summary>
        bool ScreenRect(Bounds b, out Rect rect)
        {
            rect = new Rect();
            if (_cam.WorldToScreenPoint(b.center).z <= 0f) return false;   // เป้าอยู่หลังกล้อง

            Vector3 min = b.min, max = b.max;
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            int valid = 0;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                Vector3 sp = _cam.WorldToScreenPoint(corner);
                if (sp.z <= 0f) continue;
                valid++;
                if (sp.x < xMin) xMin = sp.x;
                if (sp.x > xMax) xMax = sp.x;
                if (sp.y < yMin) yMin = sp.y;
                if (sp.y > yMax) yMax = sp.y;
            }
            if (valid < 2) { rect = new Rect(0f, 0f, Screen.width, Screen.height); return true; }

            rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            return true;
        }
    }
}
