using System;
using System.Collections;
using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// ตัวตัดสินตอนโดนจับ — ยามไล่ออก แล้วส่งกลับเช็คพอยต์
    ///
    /// เลือกโทษแบบนี้เพราะ ACT 2 คือด่านสอน stealth ด่านแรก
    /// Game Over แล้วโหลดซีนใหม่จะกินเวลาเทสระบบมากเกินไป
    /// ผู้เล่นเสียเวลาเดินกลับ แต่ไม่เสียความคืบหน้า
    /// </summary>
    public class AlertDirector : MonoBehaviour
    {
        public static AlertDirector Instance { get; private set; }

        public static event Action<int> OnCaught;        // ส่งจำนวนครั้งที่โดนจับ
        public static event Action<float> OnAlertLevel;  // ความสงสัยสูงสุดในซีน 0..1

        [Header("บทตอนโดนจับ")]
        [TextArea] public string caughtLine = "ยาม: เฮ้ย! ตรงนี้เขาห้ามเข้านะครับ ออกไปเลย";
        public float holdSeconds = 2.2f;
        [Tooltip("ยามมองไม่เห็นผู้เล่นกี่วินาทีหลังโผล่กลับมา กันโดนจับซ้ำทันที")]
        public float graceSeconds = 2.5f;

        [Header("Debug")]
        public bool logCatches = true;

        public bool IsResolving { get; private set; }
        /// <summary>ช่วงคุ้มกันหลังเพิ่งโผล่กลับมา ยามจะยังไม่เริ่มนับความสงสัย</summary>
        public bool InGracePeriod { get { return Time.time < _graceUntil; } }
        public int CaughtCount { get; private set; }
        /// <summary>ความสงสัยที่สูงที่สุดของยามทุกตัวในซีน — เอาไปโชว์เป็นหลอดบนจอ</summary>
        public float HighestSuspicion { get; private set; }

        GuardVision[] _guards;
        float _rescanTimer;
        float _graceUntil;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Rescan();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Rescan()
        {
            _guards = FindObjectsByType<GuardVision>(FindObjectsSortMode.None);
        }

        void Update()
        {
            // ยามอาจถูกสร้างทีหลัง (เช่นซีนโหลดเป็นขั้น) — สแกนซ้ำเป็นระยะ
            _rescanTimer -= Time.deltaTime;
            if (_rescanTimer <= 0f) { _rescanTimer = 2f; Rescan(); }

            if (_guards == null) return;
            float highest = 0f;
            for (int i = 0; i < _guards.Length; i++)
            {
                if (_guards[i] == null) continue;
                if (_guards[i].Suspicion > highest) highest = _guards[i].Suspicion;
            }
            HighestSuspicion = highest;
            if (OnAlertLevel != null) OnAlertLevel(highest);
        }

        public void Caught(GuardVision by)
        {
            if (IsResolving) return;
            StartCoroutine(CaughtRoutine(by));
        }

        IEnumerator CaughtRoutine(GuardVision by)
        {
            IsResolving = true;
            // try/finally: ต่อให้มีอะไรพังกลางทาง เกมต้องคืนการควบคุมให้ผู้เล่นเสมอ
            // ค้างอยู่ที่ Cutscene = เดินไม่ได้ กดอะไรไม่ได้ ต้องปิดเกมทิ้งอย่างเดียว
            try
            {
                CaughtCount++;
                if (logCatches) Debug.Log("[Act2] โดนจับครั้งที่ " + CaughtCount + " โดย " + (by != null ? by.name : "?"));
                if (OnCaught != null) OnCaught(CaughtCount);

                if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Cutscene);
                if (SubtitleManager.Instance != null) SubtitleManager.Instance.Show(caughtLine);
                if (Act2Director.Instance != null) Act2Director.Instance.Notice("โดนจับได้ — กลับไปเริ่มใหม่");

                // ใช้เวลาจริง ไม่ผูกกับ timeScale เผื่อมีอะไรไปหยุดเวลาไว้
                float until = Time.unscaledTime + holdSeconds;
                while (Time.unscaledTime < until) yield return null;

                bool moved = Act2Director.Instance != null && Act2Director.Instance.RespawnAtCheckpoint();
                if (!moved) Debug.LogWarning("[Act2] ส่งกลับเช็คพอยต์ไม่สำเร็จ — ไม่พบ player หรือจุดเกิดในซีนนี้");

                // ยามกลับไปประจำที่ ความสงสัยเคลียร์ ไม่งั้นโดนจับซ้ำทันทีที่โผล่
                ResetAllGuards();
                _graceUntil = Time.time + graceSeconds;
                yield return null;
            }
            finally
            {
                if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Exploration);
                IsResolving = false;
            }
        }

        /// <summary>ยามทุกคนกลับไปยืนที่เดิม ความสงสัยเป็นศูนย์</summary>
        public void ResetAllGuards()
        {
            Rescan();
            if (_guards == null) return;
            for (int i = 0; i < _guards.Length; i++)
            {
                if (_guards[i] == null) continue;
                var patrol = _guards[i].GetComponent<GuardPatrol>();
                if (patrol != null) patrol.ResetToPost();
                _guards[i].ResetSuspicion();
            }
        }
    }
}
