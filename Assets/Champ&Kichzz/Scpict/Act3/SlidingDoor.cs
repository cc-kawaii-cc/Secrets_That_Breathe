using SecretsThatBreathe.Act2;
using UnityEngine;

namespace SecretsThatBreathe.Act3
{
    /// <summary>
    /// ประตูบานเลื่อนคู่ (Door_L / Door_R) — เล็งแล้วกด E เพื่อเปิด/ปิด
    ///
    /// สืบทอดจาก <see cref="Act2Interactable"/> เพื่อให้ <see cref="PlayerInteractor"/> เดิมหาเจอ
    /// และได้เงื่อนไข/การปิด objective มาใช้ฟรี แต่ปลดให้กดซ้ำได้ —
    /// ประตูต้องเปิดปิดได้เรื่อย ๆ ไม่ใช่กดครั้งเดียวจบเหมือนวัตถุสำรวจ
    ///
    /// เปิดประตูมีเสียง ยามที่อยู่ในรัศมีจะหันมาดู เป็นราคาที่ต้องจ่ายของการเข้าบ้าน
    /// </summary>
    public class SlidingDoor : Act2Interactable
    {
        [Header("บานประตู")]
        [Tooltip("บานที่จะเลื่อน — บานคู่ให้ใส่สองตัว จะเลื่อนแยกออกจากกันคนละทาง")]
        public Transform[] leaves;
        [Tooltip("ระยะที่แต่ละบานเลื่อนออกจากจุดปิด (เมตร)")]
        public float slideDistance = 1.0f;
        [Tooltip("แกนที่บานเลื่อนไป (world space) — ประตูลิฟต์ในซีนนี้เลื่อนตามแกน Z")]
        public Vector3 slideAxis = Vector3.forward;
        public float slideSpeed = 2.2f;

        [Header("เสียงตอนเปิด")]
        [Tooltip("ความดัง 0..1 ที่ยามจะได้ยิน (0 = เงียบสนิท)")]
        [Range(0f, 1f)] public float openNoise = 0.45f;
        [Tooltip("รัศมีที่ยามได้ยินเสียงประตู (เมตร)")]
        public float openNoiseRadius = 9f;

        public bool IsOpen { get; private set; }

        Vector3[] _closed;

        void Awake()
        {
            if (leaves == null) return;
            _closed = new Vector3[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
                if (leaves[i] != null) _closed[i] = leaves[i].position;
        }

        bool _usedOnce;

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
            if (open && openNoise > 0f)
                StealthEvents.Noise(transform.position, openNoiseRadius, openNoise);
        }

        void Update()
        {
            if (leaves == null || _closed == null) return;
            Vector3 axis = slideAxis.sqrMagnitude < 0.0001f ? Vector3.forward : slideAxis.normalized;

            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i] == null) continue;
                // บานคู่แยกออกคนละทาง: ตัวคู่ไปทางลบ ตัวคี่ไปทางบวก
                float sign = (i % 2 == 0) ? -1f : 1f;
                Vector3 target = IsOpen ? _closed[i] + axis * slideDistance * sign : _closed[i];
                leaves[i].position = Vector3.MoveTowards(leaves[i].position, target, slideSpeed * Time.deltaTime);
            }
        }
    }
}
