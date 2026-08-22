using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>ของในฉากที่หยิบเป็นกระสุนได้ — วางไว้ที่บาร์ ที่ถังขยะ</summary>
    public class ThrowablePickup : MonoBehaviour
    {
        public int amount = 1;
        public string noticeText = "เก็บของขว้างได้";

        void OnTriggerEnter(Collider other)
        {
            var throwAbility = other.GetComponentInParent<ThrowAbility>();
            if (throwAbility == null) return;
            throwAbility.AddAmmo(amount);
            if (Act2Director.Instance != null) Act2Director.Instance.Notice(noticeText);
            Destroy(gameObject);
        }
    }
}
