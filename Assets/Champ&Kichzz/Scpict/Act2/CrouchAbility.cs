using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// ระบบก้มหมอบ — บทเรียนแรกของ stealth ในลานจอด B1
    ///
    /// ย่อ CharacterController กับกล้องลง เดินช้าลง และทำให้ยามเห็นยากขึ้น
    /// (ตัวคูณการมองเห็นอยู่ที่ <see cref="StealthTarget"/>)
    ///
    /// ลุกขึ้นไม่ได้ถ้ามีอะไรอยู่เหนือหัว — กัน player ทะลุพื้น/เพดาน
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CrouchAbility : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("ปุ่มหมอบ (เว้นว่างจะใช้ Left Ctrl)")]
        public InputActionReference crouchAction;
        [Tooltip("กดค้าง = หมอบ / ปิด = กดสลับ")]
        public bool holdToCrouch = true;

        [Header("Settings")]
        [Range(0.35f, 0.9f)] public float crouchHeightRatio = 0.55f;
        [Range(0.1f, 1f)] public float crouchSpeedMultiplier = 0.45f;
        public float transitionSpeed = 9f;

        public bool IsCrouching { get; private set; }
        /// <summary>0 = ยืนเต็ม, 1 = หมอบสุด (ใช้ไล่ค่าระหว่างเปลี่ยนท่า)</summary>
        public float CrouchAmount { get; private set; }

        CharacterController _cc;
        PlayerMovement _movement;
        Transform _camera;

        float _standHeight, _standCenterY, _standCamY;
        bool _toggleState;
        InputAction _fallback;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _movement = GetComponent<PlayerMovement>();
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) _camera = cam.transform;

            _standHeight = _cc.height;
            _standCenterY = _cc.center.y;
            _standCamY = _camera != null ? _camera.localPosition.y : 0f;

            if (crouchAction == null)
            {
                // ยังไม่ได้ทำ action asset ก็เล่นได้เลย
                _fallback = new InputAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
                _fallback.AddBinding("<Gamepad>/leftStickPress");
            }
        }

        void OnEnable()
        {
            if (crouchAction != null) crouchAction.action.Enable();
            if (_fallback != null) _fallback.Enable();
        }

        void OnDisable()
        {
            if (crouchAction != null) crouchAction.action.Disable();
            if (_fallback != null) _fallback.Disable();
            // ปิดสคริปต์ระหว่างหมอบแล้วค้างเตี้ยจะงง — คืนความสูงให้ก่อน
            ApplyHeight(0f);
        }

        bool ReadInput(out bool pressedThisFrame)
        {
            if (crouchAction != null)
            {
                pressedThisFrame = crouchAction.action.WasPressedThisFrame();
                return crouchAction.action.IsPressed();
            }
            pressedThisFrame = _fallback != null && _fallback.WasPressedThisFrame();
            return _fallback != null && _fallback.IsPressed();
        }

        void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsState(GameState.Exploration)) return;

            bool pressed, held = ReadInput(out pressed);
            if (!holdToCrouch && pressed) _toggleState = !_toggleState;
            bool want = holdToCrouch ? held : _toggleState;

            // อยากลุกแต่มีเพดาน/รถอยู่เหนือหัว → หมอบต่อ
            if (!want && IsCrouching && BlockedAbove())
            {
                want = true;
                if (!holdToCrouch) _toggleState = true;
            }

            IsCrouching = want;
            float target = want ? 1f : 0f;
            CrouchAmount = Mathf.MoveTowards(CrouchAmount, target, transitionSpeed * Time.deltaTime);
            ApplyHeight(CrouchAmount);

            if (_movement != null)
                _movement.speedMultiplier = Mathf.Lerp(1f, crouchSpeedMultiplier, CrouchAmount);
        }

        void ApplyHeight(float amount)
        {
            if (_cc == null) return;
            float h = Mathf.Lerp(_standHeight, _standHeight * crouchHeightRatio, amount);
            // ยึดเท้าไว้กับที่ ย่อจากหัวลงมา ไม่ใช่ย่อจากกลางตัว
            float centerY = _standCenterY - (_standHeight - h) * 0.5f;
            _cc.height = h;
            _cc.center = new Vector3(_cc.center.x, centerY, _cc.center.z);

            if (_camera != null)
            {
                var lp = _camera.localPosition;
                lp.y = _standCamY - (_standHeight - h) * 0.9f;
                _camera.localPosition = lp;
            }
        }

        bool BlockedAbove()
        {
            float scale = Mathf.Abs(transform.lossyScale.y);
            float r = _cc.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
            float feet = transform.position.y + (_cc.center.y - _cc.height * 0.5f) * scale;
            float standTop = feet + _standHeight * scale;

            Vector3 top = new Vector3(transform.position.x, standTop - r, transform.position.z);
            Vector3 bottom = new Vector3(transform.position.x, feet + r + 0.05f, transform.position.z);
            var hits = Physics.OverlapCapsule(bottom, top, r * 0.95f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
                if (!hits[i].transform.IsChildOf(transform)) return true;
            return false;
        }
    }
}
