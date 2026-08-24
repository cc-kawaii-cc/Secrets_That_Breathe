using SecretsThatBreathe.Act2;
using UnityEngine;

namespace SecretsThatBreathe.Act3
{
    /// <summary>
    /// ประตูบานเลื่อน (Door_L / Door_R) — เล็งแล้วกด E เพื่อเปิด/ปิด
    ///
    /// แต่ละบานเลื่อนไปตาม <see cref="openOffsets"/> ของตัวเอง (บวกกับตำแหน่งปิดที่จับไว้
    /// ตอนเริ่มซีน) ปิดแล้วกลับตำแหน่งเดิมเป๊ะเสมอ ปรับระยะ/ทิศทางแยกทีละบานได้อิสระ
    /// ใน Inspector — ไม่ผูกกับแกนหรือทิศทางตายตัวเหมือนเดิม
    ///
    /// การกันเดินทะลุแยกออกจากตัวบานเอง: ใช้ <see cref="blocker"/> เป็น collider ทึบต่างหาก
    /// เปิด/ปิดตาม IsOpen เพราะถ้าติด collider ไว้ที่ตัวบานตรงๆ มันจะไปแย่งชนกับกล่องเล็ง
    /// (raycast เจอ collider ไหนใกล้กว่าก่อน อีกอันที่อยู่ลึกกว่าจะโดนบังจนกดไม่ติด)
    /// </summary>
    public class SlidingDoor : Act2Interactable
    {
        [Header("บานประตู")]
        [Tooltip("บานที่จะเลื่อน — บานคู่ให้ใส่สองตัว")]
        public Transform[] leaves;
        [Tooltip("ระยะ+ทิศที่แต่ละบานเลื่อนไปตอนเปิด (world space, บวกกับตำแหน่งปิดที่จับไว้ตอนเริ่มซีน)\nต้องมีจำนวนเท่ากับ leaves — ปรับเลขตรงนี้ได้เลยถ้าอยากให้เลื่อนมาก/น้อย/คนละทิศ")]
        public Vector3[] openOffsets = { new Vector3(0f, 0f, -10f), new Vector3(0f, 0f, -6f) };
        public float slideSpeed = 4f;

        [Header("กันเดินทะลุ")]
        [Tooltip("collider ทึบที่ปิดกั้นช่องประตูตอนปิดอยู่ — ปิดใช้งานอัตโนมัติตอนประตูเปิด (เว้นว่างได้ถ้าไม่ต้องกันคนเดิน)")]
        public Collider blocker;

        [Header("เสียงตอนเปิด")]
        [Tooltip("ความดัง 0..1 ที่ยามจะได้ยิน (0 = เงียบสนิท)")]
        [Range(0f, 1f)] public float openNoise = 0.45f;
        [Tooltip("รัศมีที่ยามได้ยินเสียงประตู (เมตร)")]
        public float openNoiseRadius = 9f;

        public bool IsOpen { get; private set; }

        Vector3[] _closed;
        bool _usedOnce;

        void Awake()
        {
            if (leaves == null || leaves.Length == 0) return;

            _closed = new Vector3[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
                if (leaves[i] != null) _closed[i] = leaves[i].position;

            if (openOffsets == null || openOffsets.Length != leaves.Length)
            {
                Debug.LogWarning("[SlidingDoor] openOffsets ไม่ครบตามจำนวน leaves บนออบเจ็กต์ " + name +
                                  " — บานที่ขาดจะไม่ขยับตอนเปิด");
                var filled = new Vector3[leaves.Length];
                for (int i = 0; i < leaves.Length; i++)
                    filled[i] = (openOffsets != null && i < openOffsets.Length) ? openOffsets[i] : Vector3.zero;
                openOffsets = filled;
            }

            if (blocker != null) blocker.enabled = true;   // เริ่มเกมประตูปิดอยู่ — บล็อกไว้ก่อน
        }

        public override void DoInteract()
        {
            // ครั้งแรกให้ base จัดการ objective/เงื่อนไขก่อน
            // ถ้ายังถูกล็อกอยู่ base จะไม่ตั้ง hasInteracted — ประตูก็ไม่ต้องเปิด
            if (!_usedOnce)
            {
                base.DoInteract();
                if (!hasInteracted) return;
                _usedOnce = true;
            }

            // PlayerInteractor ข้ามวัตถุที่ hasInteracted ไปแล้ว ต้องปลดคืนทุกครั้ง
            // ไม่งั้นประตูจะเปิดได้ครั้งเดียวแล้วปิดไม่ได้อีกเลย
            hasInteracted = false;
            Toggle();
        }

        public void Toggle() { SetOpen(!IsOpen); }

        public void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;
            if (blocker != null) blocker.enabled = !open;
            if (open && openNoise > 0f)
                StealthEvents.Noise(transform.position, openNoiseRadius, openNoise);
        }

        void Update()
        {
            if (leaves == null || _closed == null) return;
            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i] == null) continue;
                Vector3 offset = (openOffsets != null && i < openOffsets.Length) ? openOffsets[i] : Vector3.zero;
                Vector3 target = IsOpen ? _closed[i] + offset : _closed[i];
                leaves[i].position = Vector3.MoveTowards(leaves[i].position, target, slideSpeed * Time.deltaTime);
            }
        }
    }
}
