using UnityEngine;

namespace SecretsThatBreathe.Act3
{
    /// <summary>
    /// อ่านความดังจากไมค์ของผู้เล่นจริง ๆ — ใช้ตอนฉากแอบฟังที่ "ห้ามส่งเสียง"
    ///
    /// เก็บเป็นค่า RMS ต่อเนื่อง 0..1 ไม่ใช่สวิตช์ ผู้เล่นจึงเห็นหลอดขยับตามลมหายใจตัวเอง
    /// และรู้ตัวก่อนว่ากำลังจะดังเกิน
    ///
    /// ถ้าเครื่องไม่มีไมค์หรือไม่ได้รับสิทธิ์ <see cref="Available"/> จะเป็น false
    /// ฉากที่เรียกใช้ต้องมีทางเล่นสำรองเสมอ ไม่งั้นคนไม่มีไมค์จะเล่นต่อไม่ได้
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MicNoiseDetector : MonoBehaviour
    {
        public static MicNoiseDetector Instance { get; private set; }

        [Header("ไมค์")]
        public bool useMicrophone = true;
        [Tooltip("เว้นว่าง = ใช้ไมค์ตัวหลักของเครื่อง")]
        public string deviceName = "";
        [Tooltip("จำนวนตัวอย่างที่ใช้เฉลี่ยความดัง — ยิ่งมากยิ่งนิ่ง แต่ตอบสนองช้าลง")]
        public int sampleWindow = 256;

        [Header("ความไว")]
        [Tooltip("ดังเกินค่านี้ = ถือว่าส่งเสียง (0..1) ปรับให้เข้ากับไมค์แต่ละตัว")]
        [Range(0f, 1f)] public float loudThreshold = 0.10f;
        [Tooltip("ตัวคูณขยายสัญญาณก่อนเทียบ threshold — ไมค์เบาให้เพิ่มค่านี้")]
        public float gain = 6f;
        [Tooltip("ความเร็วที่หลอดไล่ตามค่าจริง ยิ่งน้อยยิ่งหน่วง")]
        public float smoothing = 12f;

        [Header("Debug")]
        public bool logStatus = true;

        /// <summary>ความดังปัจจุบัน 0..1 (ผ่านการขยายและกรองแล้ว)</summary>
        public float Loudness { get; private set; }
        /// <summary>ไมค์ใช้งานได้จริงไหม — ถ้า false ฉากต้องใช้ทางเล่นสำรอง</summary>
        public bool Available { get; private set; }
        public bool IsLoud { get { return Available && Loudness >= loudThreshold; } }

        AudioClip _clip;
        string _device;
        float[] _samples;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _samples = new float[Mathf.Max(64, sampleWindow)];
        }

        void OnEnable() { StartMic(); }
        void OnDisable() { StopMic(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void StartMic()
        {
            if (!useMicrophone) { Available = false; return; }
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                Available = false;
                if (logStatus) Debug.LogWarning("[Mic] ไม่พบไมค์ในเครื่อง — ฉากแอบฟังจะใช้เสียงในเกมแทน");
                return;
            }

            _device = string.IsNullOrEmpty(deviceName) ? Microphone.devices[0] : deviceName;
            // วนบันทึกลง buffer 1 วินาที อ่านค่าล่าสุดออกมาเรื่อย ๆ ไม่ต้องเล่นเสียงออกลำโพง
            _clip = Microphone.Start(_device, true, 1, 44100);
            Available = _clip != null;
            if (logStatus)
            {
                if (Available) Debug.Log("[Mic] เปิดไมค์: " + _device);
                else Debug.LogWarning("[Mic] เปิดไมค์ไม่สำเร็จ — ฉากแอบฟังจะใช้เสียงในเกมแทน");
            }
        }

        void StopMic()
        {
            if (_clip != null && !string.IsNullOrEmpty(_device) && Microphone.IsRecording(_device))
                Microphone.End(_device);
            _clip = null;
            Available = false;
            Loudness = 0f;
        }

        void Update()
        {
            if (!Available || _clip == null) return;

            int pos = Microphone.GetPosition(_device) - _samples.Length;
            if (pos < 0) return;                       // ยังบันทึกไม่พอหนึ่งหน้าต่าง
            if (!_clip.GetData(_samples, pos)) return;

            // RMS อ่านความดังที่หูรับรู้ได้ตรงกว่า peak ซึ่งกระโดดตามเสียงป๊อกแป๊กเดียว
            float sum = 0f;
            for (int i = 0; i < _samples.Length; i++) sum += _samples[i] * _samples[i];
            float rms = Mathf.Sqrt(sum / _samples.Length);

            float target = Mathf.Clamp01(rms * gain);
            Loudness = Mathf.MoveTowards(Loudness, target, smoothing * Time.deltaTime);
        }
    }
}
