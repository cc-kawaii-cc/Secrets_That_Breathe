using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// สิ่งที่ถ่ายรูปเป็นหลักฐานได้ — ติดไว้ที่กันชนหน้าที่พังของรถสปอร์ต
    /// ใช้ bounds ของ renderer ที่ระบุเป็นกรอบเป้าหมาย
    /// </summary>
    public class PhotoSubject : MonoBehaviour
    {
        [Tooltip("id หลักฐานที่จะได้เมื่อถ่ายสำเร็จ")]
        public string evidenceId = "EV_BumperPhoto";
        [Tooltip("id objective ที่จะถือว่าสำเร็จ")]
        public string objectiveId;
        [TextArea] public string hint = "เล็งให้กันชนที่พังเต็มกรอบ";
        [TextArea] public string successLine = "ถ่ายไว้แล้ว — กันชนหน้ายุบ เศษสีตรงกับที่เก็บมา";

        [Tooltip("ต้องกินพื้นที่จออย่างน้อยกี่ % ถึงจะนับว่าใกล้พอ")]
        [Range(0.02f, 0.9f)] public float minScreenCoverage = 0.16f;
        [Tooltip("ระยะไกลสุดที่ยังถ่ายได้")]
        public float maxDistance = 12f;

        [Tooltip("ของที่เป้านี้ติดอยู่ (เช่นตัวรถ) — collider ของมันจะไม่นับว่าบัง")]
        public Transform partOf;

        public bool Captured { get; private set; }
        public void MarkCaptured() { Captured = true; }

        /// <summary>กล่องครอบของจริง เอาไว้คำนวณว่าเข้ากรอบแค่ไหน</summary>
        public bool TryGetBounds(out Bounds bounds)
        {
            bounds = new Bounds(transform.position, Vector3.one * 0.5f);
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return false;
            bounds = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) bounds.Encapsulate(rs[i].bounds);
            return true;
        }
    }
}
