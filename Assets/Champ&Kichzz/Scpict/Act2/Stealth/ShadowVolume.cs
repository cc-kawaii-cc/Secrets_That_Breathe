using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// กล่อง trigger ที่ทำให้ผู้เล่นเห็นยากขึ้นเมื่ออยู่ข้างใน
    /// วางไว้หลังเสา หลังรถ ใต้ระเบียง — ที่ที่ level design ตั้งใจให้เป็นที่หลบ
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ShadowVolume : MonoBehaviour
    {
        void Reset()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var t = other.GetComponentInParent<StealthTarget>();
            if (t != null) t.SetInShadow(true);
        }

        void OnTriggerExit(Collider other)
        {
            var t = other.GetComponentInParent<StealthTarget>();
            if (t != null) t.SetInShadow(false);
        }
    }
}
