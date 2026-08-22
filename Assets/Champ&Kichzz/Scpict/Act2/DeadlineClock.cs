using System;
using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// นาฬิกานับถอยหลังถึงเช้า — เส้นตายของ ACT 2
    ///
    /// เริ่มเดินตอนเข้มรู้ชื่อเจ้าของรถ (ปักผลบนบอร์ดที่อู่) แล้วเดินยาวข้ามซีน
    /// ไม่ใช่กลไกแพ้/ชนะ แต่เป็นแรงกดดัน — ผู้เล่นจะรู้สึกว่าการยืนรอ patrol
    /// นานเกินไปมีราคาที่ต้องจ่าย
    ///
    /// ระหว่างเทสระบบให้ปิด <see cref="failOnExpire"/> ไว้ เวลาหมดแล้วยังเล่นต่อได้
    /// </summary>
    public class DeadlineClock : MonoBehaviour
    {
        public static DeadlineClock Instance { get; private set; }

        /// <summary>เวลาที่เหลือ (วินาทีจริง) กับสัดส่วน 0..1</summary>
        public static event Action<float, float> OnTick;
        public static event Action<int> OnWarning;     // ส่งนาทีที่เหลือตอนเตือน
        public static event Action OnExpired;

        [Header("เวลา")]
        [Tooltip("นาทีจริงที่ให้ผู้เล่นทำภารกิจให้จบ")]
        public float totalMinutes = 22f;
        [Tooltip("เวลาในเกมตอนเริ่มนับ (ชั่วโมงแบบ 24 ชม.)")]
        public float storyStartHour = 23.5f;
        [Tooltip("เวลาในเกมตอนหมดเวลา")]
        public float storyEndHour = 6f;

        [Header("การเตือน")]
        [Tooltip("เตือนเมื่อเหลือกี่นาที (เรียงจากมากไปน้อย)")]
        public int[] warnAtMinutes = { 10, 5, 1 };

        [Header("ตอนหมดเวลา")]
        [Tooltip("เปิด = หมดเวลาแล้วถือว่าล้มเหลว ปิด = แค่เตือน (แนะนำตอนเทสระบบ)")]
        public bool failOnExpire = false;
        [TextArea] public string expiredLine = "ฟ้าสางแล้ว... สายไปแล้ว";

        public bool Running { get; private set; }
        public bool Expired { get; private set; }
        public float SecondsLeft { get; private set; }
        public float Normalised { get { return Mathf.Clamp01(SecondsLeft / Mathf.Max(1f, totalMinutes * 60f)); } }

        int _nextWarnIndex;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SecondsLeft = totalMinutes * 60f;
        }

        void OnEnable() { Act2Director.OnObjectiveCompleted += OnObjectiveCompleted; }
        void OnDisable() { Act2Director.OnObjectiveCompleted -= OnObjectiveCompleted; }

        void OnObjectiveCompleted(string id)
        {
            // นาฬิกาเริ่มเดินตอนรู้ว่าเป็นรถของใคร — ก่อนหน้านั้นยังไม่มีเส้นตาย
            if (id == "OBJ_08_PinResultOnBoard") StartClock();
        }

        public void StartClock()
        {
            if (Running || Expired) return;
            Running = true;
            SecondsLeft = totalMinutes * 60f;
            _nextWarnIndex = 0;
            if (Act2Director.Instance != null)
                Act2Director.Instance.Notice("SD Card จะถูกทำลายตอนเช้า — เหลือเวลาไม่มาก");
        }

        public void StopClock() { Running = false; }

        void Update()
        {
            if (!Running || Expired) return;

            SecondsLeft -= Time.deltaTime;
            if (OnTick != null) OnTick(SecondsLeft, Normalised);

            if (warnAtMinutes != null && _nextWarnIndex < warnAtMinutes.Length)
            {
                int mark = warnAtMinutes[_nextWarnIndex];
                if (SecondsLeft <= mark * 60f)
                {
                    _nextWarnIndex++;
                    if (OnWarning != null) OnWarning(mark);
                    if (Act2Director.Instance != null)
                        Act2Director.Instance.Notice("เหลือเวลาอีก " + mark + " นาที");
                }
            }

            if (SecondsLeft > 0f) return;

            SecondsLeft = 0f;
            Expired = true;
            Running = false;
            if (OnExpired != null) OnExpired();
            if (SubtitleManager.Instance != null) SubtitleManager.Instance.Show(expiredLine);
            if (!failOnExpire && Act2Director.Instance != null)
                Act2Director.Instance.Notice("หมดเวลาแล้ว (โหมดเทส: เล่นต่อได้)");
        }

        /// <summary>เวลาในเกม เช่น "02:41" — ไล่จาก storyStartHour ไป storyEndHour ตามเวลาที่ใช้ไป</summary>
        public string StoryTimeText()
        {
            float span = storyEndHour - storyStartHour;
            if (span < 0f) span += 24f;                       // ข้ามเที่ยงคืน
            float t = 1f - Normalised;
            float hour = Mathf.Repeat(storyStartHour + span * t, 24f);
            int h = Mathf.FloorToInt(hour);
            int m = Mathf.FloorToInt((hour - h) * 60f);
            return h.ToString("00") + ":" + m.ToString("00");
        }

        public string CountdownText()
        {
            int total = Mathf.CeilToInt(SecondsLeft);
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
    }
}
