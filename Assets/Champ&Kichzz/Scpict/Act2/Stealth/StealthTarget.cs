using System;
using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>เสียงที่ดังขึ้นในโลก ยามที่อยู่ในรัศมีจะหันไปดู</summary>
    public static class StealthEvents
    {
        /// <summary>ตำแหน่ง, รัศมีที่ได้ยิน, ความแรง 0..1</summary>
        public static event Action<Vector3, float, float> OnNoise;

        public static void Noise(Vector3 position, float radius, float strength)
        {
            if (OnNoise != null) OnNoise(position, radius, strength);
        }
    }

    /// <summary>
    /// ติดบนตัวผู้เล่น — สรุปว่า "ตอนนี้เห็นง่ายแค่ไหน" และ "เสียงดังแค่ไหน"
    /// ยามทุกตัวอ่านค่าจากตรงนี้ที่เดียว จะได้ปรับสมดุลจากจุดเดียว
    /// </summary>
    public class StealthTarget : MonoBehaviour
    {
        public static StealthTarget Instance { get; private set; }

        [Header("ตัวคูณการมองเห็น")]
        [Tooltip("ยืนนิ่ง")] public float visibilityIdle = 0.75f;
        [Tooltip("เดิน")] public float visibilityWalk = 1.0f;
        [Tooltip("วิ่ง")] public float visibilitySprint = 1.35f;
        [Tooltip("หมอบแล้วคูณเพิ่ม (ยิ่งน้อยยิ่งเห็นยาก)")] public float crouchMultiplier = 0.45f;
        [Tooltip("อยู่ในเงา/หลังกำบัง คูณเพิ่ม")] public float shadowMultiplier = 0.5f;
        [Tooltip("เปิดไฟฉายแล้วคูณเพิ่ม — ส่องทางได้ แต่ยามเห็นแต่ไกล")]
        public float flashlightMultiplier = 2.2f;

        [Header("เสียง")]
        public float noiseIdle = 0f;
        public float noiseWalk = 0.35f;
        public float noiseSprint = 1f;
        public float noiseCrouchMultiplier = 0.25f;
        [Tooltip("รัศมีที่ยามได้ยินตอนเสียงดังสุด (เมตร)")]
        public float maxHearRadius = 14f;
        [Tooltip("ยิงเสียงฝีเท้าออกไปทุกๆ กี่วินาที")]
        public float footstepInterval = 0.45f;

        /// <summary>
        /// ตัวคูณจากสถานการณ์ เช่น โผล่หน้าไปแอบฟังใกล้ๆ = เห็นง่ายขึ้น
        /// ระบบอื่นเซ็ตค่านี้ทุกเฟรมที่ต้องการ ไม่งั้นมันจะไหลกลับเป็น 1 เอง
        /// </summary>
        public float ExternalVisibilityMultiplier { get; private set; }

        /// <summary>0 = แทบมองไม่เห็น, 1 = เห็นตามปกติ, มากกว่า 1 = เตะตา</summary>
        public float Visibility { get; private set; }
        public float Noise { get; private set; }
        public bool InShadow { get { return _shadowCount > 0; } }

        PlayerMovement _movement;
        CrouchAbility _crouch;
        FlashlightController _flashlight;
        int _shadowCount;
        float _stepTimer;
        float _externalThisFrame = 1f;

        void Awake()
        {
            Instance = this;
            ExternalVisibilityMultiplier = 1f;
            _movement = GetComponent<PlayerMovement>();
            _crouch = GetComponent<CrouchAbility>();
            _flashlight = GetComponentInChildren<FlashlightController>(true);
        }

        /// <summary>ไฟฉายเปิดอยู่ไหม — ใช้ทั้งคำนวณการมองเห็นและโชว์บน HUD</summary>
        public bool FlashlightOn
        {
            get { return _flashlight != null && _flashlight.isFlashlightOn; }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>
        /// ให้ระบบอื่นดันค่าการมองเห็นชั่วคราว (เรียกทุกเฟรมที่ยังต้องการ)
        /// เก็บค่าที่สูงสุดของเฟรมนั้น เผื่อมีหลายอย่างดันพร้อมกัน
        /// </summary>
        public void PushVisibilityMultiplier(float multiplier)
        {
            if (multiplier > _externalThisFrame) _externalThisFrame = multiplier;
        }

        /// <summary>เรียกจาก <see cref="ShadowVolume"/> ตอนเข้า/ออกเขตกำบัง</summary>
        public void SetInShadow(bool inside)
        {
            _shadowCount = Mathf.Max(0, _shadowCount + (inside ? 1 : -1));
        }

        void Update()
        {
            bool moving = _movement != null && _movement.IsMoving;
            bool sprinting = _movement != null && _movement.IsSprinting;
            float crouchAmt = _crouch != null ? _crouch.CrouchAmount : 0f;

            ExternalVisibilityMultiplier = _externalThisFrame;
            _externalThisFrame = 1f;      // ใครอยากดันต่อ ต้องเรียกซ้ำเฟรมถัดไป

            float baseVis = !moving ? visibilityIdle : (sprinting ? visibilitySprint : visibilityWalk);
            Visibility = baseVis
                       * Mathf.Lerp(1f, crouchMultiplier, crouchAmt)
                       * (InShadow ? shadowMultiplier : 1f)
                       * (FlashlightOn ? flashlightMultiplier : 1f)
                       * ExternalVisibilityMultiplier;

            float baseNoise = !moving ? noiseIdle : (sprinting ? noiseSprint : noiseWalk);
            Noise = baseNoise * Mathf.Lerp(1f, noiseCrouchMultiplier, crouchAmt);

            // ฝีเท้าเป็นจังหวะ ไม่ใช่เสียงต่อเนื่อง — ยามจะได้มีจังหวะหันมา
            if (Noise <= 0.01f) { _stepTimer = 0f; return; }
            _stepTimer -= Time.deltaTime;
            if (_stepTimer > 0f) return;
            _stepTimer = footstepInterval / Mathf.Max(0.2f, Noise);
            StealthEvents.Noise(transform.position, maxHearRadius * Noise, Noise);
        }
    }
}
