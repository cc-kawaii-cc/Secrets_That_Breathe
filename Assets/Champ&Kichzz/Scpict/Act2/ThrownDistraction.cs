using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>ของที่ถูกขว้าง — ตกกระทบครั้งแรกแล้วส่งเสียงเรียกยาม แล้วค่อยๆ หายไป</summary>
    public class ThrownDistraction : MonoBehaviour
    {
        public float noiseRadius = 13f;
        public float noiseStrength = 0.9f;
        public float lifetime = 12f;

        bool _landed;

        void Start() { Destroy(gameObject, lifetime); }

        void OnCollisionEnter(Collision collision)
        {
            if (_landed) return;
            _landed = true;
            StealthEvents.Noise(transform.position, noiseRadius, noiseStrength);
        }
    }
}
