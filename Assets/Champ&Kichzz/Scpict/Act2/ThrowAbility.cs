using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// ขว้างของล่อยาม — ทางเลือกที่สามนอกจาก "รอ" กับ "วิ่งหนี"
    ///
    /// ทำให้ผู้เล่นเป็นฝ่ายกำหนดจังหวะเองแทนที่จะยืนรอ patrol
    /// ซึ่งเป็นสิ่งที่ทำให้ stealth สนุกแทนที่จะน่าเบื่อ
    /// </summary>
    public class ThrowAbility : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("ปุ่มขว้าง (เว้นว่างจะใช้ G)")]
        public InputActionReference throwAction;

        [Header("กระสุน")]
        [Tooltip("prefab ของที่ขว้าง เว้นว่างจะสร้างกระป๋องให้เอง")]
        public GameObject projectilePrefab;
        public int startingCount = 3;
        public float throwForce = 9f;
        public float cooldown = 0.6f;

        [Header("เสียงตอนตก")]
        public float noiseRadius = 13f;
        [Range(0f, 1f)] public float noiseStrength = 0.9f;

        public int Remaining { get; private set; }

        Camera _cam;
        InputAction _fallback;
        float _cooldownTimer;

        void Awake()
        {
            Remaining = startingCount;
            if (throwAction == null)
            {
                _fallback = new InputAction("Throw", InputActionType.Button, "<Keyboard>/g");
                _fallback.AddBinding("<Gamepad>/buttonWest");
            }
        }

        void OnEnable()
        {
            if (throwAction != null) throwAction.action.Enable();
            if (_fallback != null) _fallback.Enable();
        }

        void OnDisable()
        {
            if (throwAction != null) throwAction.action.Disable();
            if (_fallback != null) _fallback.Disable();
        }

        /// <summary>เก็บของขว้างเพิ่ม (ขวดที่บาร์ กระป๋องในลานจอด)</summary>
        public void AddAmmo(int amount)
        {
            Remaining += Mathf.Max(0, amount);
        }

        void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
            if (GameManager.Instance != null && !GameManager.Instance.IsState(GameState.Exploration)) return;

            bool pressed = throwAction != null
                ? throwAction.action.WasPressedThisFrame()
                : (_fallback != null && _fallback.WasPressedThisFrame());
            if (!pressed || Remaining <= 0 || _cooldownTimer > 0f) return;

            Throw();
        }

        void Throw()
        {
            if (_cam == null)
            {
                _cam = PlayerManager.Instance != null ? PlayerManager.Instance.playerCamera : Camera.main;
                if (_cam == null) return;
            }

            Remaining--;
            _cooldownTimer = cooldown;

            GameObject go = projectilePrefab != null
                ? Instantiate(projectilePrefab)
                : BuildCan();

            go.transform.position = _cam.transform.position + _cam.transform.forward * 0.6f;
            go.transform.rotation = Random.rotation;

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.25f;
            rb.linearVelocity = _cam.transform.forward * throwForce + Vector3.up * 1.2f;
            rb.angularVelocity = Random.insideUnitSphere * 8f;

            var noise = go.AddComponent<ThrownDistraction>();
            noise.noiseRadius = noiseRadius;
            noise.noiseStrength = noiseStrength;

            if (Act2Director.Instance != null)
                Act2Director.Instance.Notice("ขว้างแล้ว (เหลือ " + Remaining + ")");
        }

        /// <summary>กระป๋องสังกะสีแบบง่าย เผื่อยังไม่ได้ทำ prefab</summary>
        static GameObject BuildCan()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "ThrownCan";
            go.transform.localScale = new Vector3(0.07f, 0.06f, 0.07f);
            var r = go.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                var m = new Material(r.sharedMaterial);
                m.color = new Color(0.62f, 0.64f, 0.66f);
                r.material = m;
            }
            return go;
        }
    }
}
