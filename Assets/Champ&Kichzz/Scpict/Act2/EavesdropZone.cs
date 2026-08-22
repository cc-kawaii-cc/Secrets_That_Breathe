using System;
using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// โซนแอบฟัง — หัวใจของฉากในคลับ
    ///
    /// ยิ่งเข้าใกล้ยิ่งได้ยินชัด แต่ยามก็เห็นง่ายขึ้นตามกัน
    /// ผู้เล่นต้องเลือกเองว่าจะเสี่ยงแค่ไหน และเลือกใหม่ได้ตลอดเวลา
    /// นั่นทำให้ฉากนี้เป็นเกมเพลย์ ไม่ใช่คัตซีนที่ต้องยืนรอ
    ///
    /// เพลงคลับจะถูกหรี่ลงตามความชัด ให้ผู้เล่นรู้สึกว่า "ตั้งใจฟัง" จริงๆ
    /// </summary>
    public class EavesdropZone : MonoBehaviour
    {
        public static EavesdropZone Active { get; private set; }

        /// <summary>ความชัด 0..1 กับความคืบหน้า 0..1</summary>
        public static event Action<float, float> OnListening;
        public static event Action OnEnded;

        [Header("เป้าหมาย")]
        [Tooltip("จุดที่คนคุยกันยืนอยู่ (เว้นว่างจะใช้ตัวเอง)")]
        public Transform speakers;

        [Header("ระยะ")]
        [Tooltip("เข้าใกล้กว่านี้ = ได้ยินชัดเต็ม 100%")]
        public float clearDistance = 3.5f;
        [Tooltip("ไกลกว่านี้ = ไม่ได้ยินอะไรเลย")]
        public float maxDistance = 9f;
        [Tooltip("ต้องชัดอย่างน้อยเท่านี้ถึงจะเก็บความคืบหน้าได้")]
        [Range(0.1f, 1f)] public float minClarityToProgress = 0.35f;

        [Header("ความเสี่ยง")]
        [Tooltip("ตัวคูณการมองเห็นตอนอยู่ระยะชัดสุด (>1 = ยามเห็นง่ายขึ้น)")]
        public float maxExposureMultiplier = 1.9f;

        [Header("บทสนทนา")]
        [Tooltip("วินาทีที่ต้องฟังรวมทั้งหมด")]
        public float listenSeconds = 14f;
        [Tooltip("ฟังไม่ครบแล้วเดินหนี ความคืบหน้าลดต่อวินาที")]
        public float decayPerSecond = 0.12f;
        [TextArea] public string[] lines =
        {
            "ชายชุดสูท A: รถมันอยู่ข้างล่างนี่แหละ พรุ่งนี้เช้าค่อยเอาไปเก็บ",
            "ชายชุดสูท B: แล้วกล้องหน้ารถล่ะ ถอดออกหรือยัง",
            "ชายชุดสูท A: ถอดแล้ว การ์ดอยู่ในเซฟที่เพนต์เฮาส์",
            "ชายชุดสูท B: เช้าพรุ่งนี้ทำลายทิ้งให้หมด อย่าให้เหลือ",
        };

        [Header("เสียงรอบข้าง")]
        [Tooltip("เพลงคลับที่จะถูกหรี่ลงตอนตั้งใจฟัง")]
        public AudioSource ambientMusic;
        [Range(0f, 1f)] public float duckedVolume = 0.18f;
        public float duckSpeed = 3f;

        [Header("ผลลัพธ์")]
        public string evidenceId = "EV_SDCardLocation";
        public string objectiveId = "OBJ_04_OverhearSuits";
        [TextArea] public string completeNotice = "รู้แล้ว — SD Card อยู่ในเซฟที่เพนต์เฮาส์ของแชมป์ ต้องชิงมาก่อนเช้า";

        public float Clarity { get; private set; }
        public float Progress { get; private set; }
        public bool Completed { get; private set; }

        StealthTarget _target;
        float _musicBaseVolume = -1f;
        int _lineShown = -1;
        bool _wasListening;

        void Awake()
        {
            if (speakers == null) speakers = transform;
        }

        void OnDisable()
        {
            if (Active == this) { Active = null; if (OnEnded != null) OnEnded(); }
            RestoreMusic(true);
        }

        void Update()
        {
            if (Completed) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsState(GameState.Exploration)) return;

            if (_target == null)
            {
                _target = StealthTarget.Instance;
                if (_target == null) return;
            }

            float dist = Vector3.Distance(_target.transform.position, speakers.position);
            Clarity = dist <= clearDistance ? 1f
                    : dist >= maxDistance ? 0f
                    : 1f - Mathf.InverseLerp(clearDistance, maxDistance, dist);

            bool listening = Clarity >= minClarityToProgress;

            if (listening)
            {
                Active = this;

                // เข้ามาใกล้ = โผล่ออกจากที่กำบัง ยามเห็นง่ายขึ้นตามระยะ
                _target.PushVisibilityMultiplier(Mathf.Lerp(1f, maxExposureMultiplier, Clarity));

                // ชัดแค่ไหน เก็บความคืบหน้าเร็วเท่านั้น
                Progress += (Clarity / Mathf.Max(0.5f, listenSeconds)) * Time.deltaTime;
                ShowLineFor(Progress);
            }
            else
            {
                if (Active == this) Active = null;
                Progress -= decayPerSecond * Time.deltaTime;
            }

            Progress = Mathf.Clamp01(Progress);
            // ยิง event เฉพาะตอนกำลังฟังจริง ไม่งั้นหลอดบนจอไม่ยอมหายไปเลย
            if (listening)
            {
                if (OnListening != null) OnListening(Clarity, Progress);
            }
            else if (_wasListening)
            {
                if (OnEnded != null) OnEnded();
            }
            _wasListening = listening;
            UpdateMusic(listening ? Clarity : 0f);

            if (Progress >= 1f) Complete();
        }

        void ShowLineFor(float progress)
        {
            if (lines == null || lines.Length == 0) return;
            int index = Mathf.Clamp(Mathf.FloorToInt(progress * lines.Length), 0, lines.Length - 1);
            if (index == _lineShown) return;
            _lineShown = index;
            if (SubtitleManager.Instance != null) SubtitleManager.Instance.Show(lines[index]);
        }

        void UpdateMusic(float duckAmount)
        {
            if (ambientMusic == null) return;
            if (_musicBaseVolume < 0f) _musicBaseVolume = ambientMusic.volume;
            float target = Mathf.Lerp(_musicBaseVolume, duckedVolume, duckAmount);
            ambientMusic.volume = Mathf.MoveTowards(ambientMusic.volume, target, duckSpeed * Time.deltaTime);
        }

        void RestoreMusic(bool instant)
        {
            if (ambientMusic == null || _musicBaseVolume < 0f) return;
            ambientMusic.volume = instant
                ? _musicBaseVolume
                : Mathf.MoveTowards(ambientMusic.volume, _musicBaseVolume, duckSpeed * Time.deltaTime);
        }

        void Complete()
        {
            Completed = true;
            Active = null;
            if (OnEnded != null) OnEnded();
            RestoreMusic(false);

            var director = Act2Director.Instance;
            if (director != null)
            {
                director.GainEvidence(evidenceId, completeNotice);
                if (!string.IsNullOrEmpty(objectiveId)) director.Complete(objectiveId);
            }
            if (SubtitleManager.Instance != null) SubtitleManager.Instance.Show(completeNotice);
        }

        void OnDrawGizmosSelected()
        {
            Vector3 c = speakers != null ? speakers.position : transform.position;
            Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.9f);
            Gizmos.DrawWireSphere(c, clearDistance);
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(c, maxDistance);
        }
    }
}
