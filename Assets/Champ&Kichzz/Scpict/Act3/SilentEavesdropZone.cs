using System;
using System.Collections;
using SecretsThatBreathe.Act2;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe.Act3
{
    /// <summary>
    /// ฉากแอบฟังแชมป์คุยโทรศัพท์กับพ่อ — กติกาเดียวคือ "ห้ามส่งเสียง"
    ///
    /// ผู้เล่นมุดอยู่ในตู้ ขยับไม่ได้ ทำได้อย่างเดียวคือเงียบ
    /// ความดังอ่านจากไมค์จริงผ่าน <see cref="MicNoiseDetector"/> ต้องดังต่อเนื่องเกิน
    /// <see cref="graceSeconds"/> ถึงจะโดนจับ — ไอจามทีเดียวยังรอด แต่ตกใจแล้วร้องคือจบ
    ///
    /// เครื่องที่ไม่มีไมค์ยังเล่นจบได้ (ดูทางเล่นสำรองที่ <see cref="fallbackToKeyPress"/>)
    /// ไม่งั้นคนที่ไม่มีไมค์จะติดอยู่ตรงนี้ตลอดไป
    /// </summary>
    public class SilentEavesdropZone : MonoBehaviour
    {
        /// <summary>ความดัง 0..1 และความใกล้โดนจับ 0..1 — HUD ใช้วาดหลอดวัดเสียง</summary>
        public static event Action<float, float> OnNoiseLevel;
        /// <summary>ฉากจบแล้ว (ผ่านหรือโดนจับ) — HUD ใช้ซ่อนหลอด</summary>
        public static event Action OnNoiseMeterHide;

        [Header("บทสนทนา (แก้ได้ตามใจ — ชื่อผู้พูดรวมอยู่ในบรรทัดแล้ว)")]
        [TextArea(2, 6)]
        public string[] lines =
        {
            "(เสียงโทรศัพท์ดังขึ้น แชมป์กดรับสายแล้วตะคอกใส่ทันที)",
            "แชมป์: ฮัลโหลพ่อ! ... โธ่เว้ย! พ่อจะโทรมาด่าผมทำไมอีกวะ! ผมบอกแล้วไงว่านังนั่นมันวิ่งตัดหน้าผมเอง! มันอยากตายเองนะเว้ย!",
            "(แชมป์ทุบโต๊ะดัง ปัง!)",
            "เสียงพ่อ: แกหุบปากเดี๋ยวนี้ไอ้ลูกเวร! แกคิดว่าตอนนี้ฉันว่างมากเหรอไงฮะ? ใกล้เลือกตั้งแล้วแกยังจะหาเรื่องใส่ตัวอีก! ถ้าสื่อรู้เรื่องนี้ แกคิดว่าฉันจะเอาหน้าไปไว้ที่ไหน!",
            "แชมป์: ก็แล้วพ่อจะให้ผมทำไง! ให้ผมเดินไปมอบตัวเหรอ? ... พ่อเป็นถึง ส.ส. นะเว้ย! พ่อมีอำนาจ พ่อก็จัดการดิ! หรือพ่ออยากเห็นลูกตัวเองติดคุก? พ่ออยากให้ตระกูลเราพังเพราะเรื่องแค่นี้เหรอ?",
            "(ความเงียบชั่วอึดใจ... ได้ยินเสียงพ่อถอนหายใจยาวผ่านโทรศัพท์)",
            "เสียงพ่อ: ...เออ กูจัดการให้แล้ว ผู้กำกับสั่งลูกน้องให้ลบไฟล์กล้องวงจรปิดไปแล้ว ส่วนทางอัยการ... กูต้องเอาที่ดินแปลงสวยไปแลกกว่ามันจะยอมสั่งไม่ฟ้อง",
            "แชมป์: จริงเหรอพ่อ... เฮ้อ... ผมก็นึกว่าพ่อจะทิ้งผมซะแล้ว",
            "เสียงพ่อ: จำใส่หัวแกไว้แชมป์... นี่เป็นครั้งสุดท้าย ชีวิตคนมันไม่ได้ถูกๆ เหมือนผักปลาที่แกจะขับรถเหยียบเล่นแล้วให้ฉันตามจ่ายตลอดไป... เข้าใจไหม!",
            "แชมป์: เข้าใจครับพ่อ... ผมสัญญาจะไม่ซิ่งแถวนั้นอีก... พ่อแม่งเจ๋งว่ะ ผมรู้อยู่แล้วว่าพ่อเคลียร์ได้... รักพ่อนะครับท่าน ส.ส.",
            "(แชมป์วางสาย แล้วหัวเราะหึๆ ในลำคอ)",
        };

        [Tooltip("วินาทีต่อบรรทัด (ใช้เมื่อไม่ได้ระบุรายบรรทัดด้านล่าง)")]
        public float secondsPerLine = 5.5f;
        [Tooltip("ระบุเวลาเป็นรายบรรทัด — เว้นว่างไว้จะใช้ค่าด้านบนทุกบรรทัด")]
        public float[] perLineSeconds;

        [Header("ห้ามส่งเสียง")]
        public bool useMicrophone = true;
        [Tooltip("ดังเกินค่านี้ = กำลังส่งเสียง (เทียบกับ MicNoiseDetector.Loudness)")]
        [Range(0f, 1f)] public float micThreshold = 0.12f;
        [Tooltip("ต้องดังต่อเนื่องกี่วินาทีถึงโดนจับ — กันเสียงแวบเดียวทำเกมจบ")]
        public float graceSeconds = 0.8f;
        [Tooltip("ไม่มีไมค์ให้ใช้แทน: ขยับตัวในตู้ (กดปุ่มเดิน/กระโดด) = เสียงกรุกกรัก")]
        public bool fallbackToKeyPress = true;

        [Header("ตอนโดนจับ")]
        [TextArea] public string caughtLine = "แชมป์: ...เดี๋ยวนะพ่อ เมื่อกี้มีเสียงอะไรวะ";
        public float caughtHoldSeconds = 2.4f;
        [TextArea] public string retryNotice = "โดนจับได้ — ย้อนกลับไปซ่อนใหม่อีกครั้ง";

        [Header("ของที่ต้องรีเซ็ตตอนเริ่มใหม่")]
        public HideSpot hideSpot;
        public ChampArrivalSequence sequence;

        [Header("ผลลัพธ์")]
        public string evidenceId = "EV_ConfessionCall";
        public string objectiveId = "OBJ_A3_04_Overhear";
        [TextArea] public string completeNotice = "ได้ยินหมดแล้ว — พ่อของแชมป์สั่งลบไฟล์กล้องเอง รีบออกไปจากที่นี่";

        /// <summary>ความดังปัจจุบัน 0..1 เอาไปทำหลอดบนจอได้</summary>
        public float CurrentLoudness { get; private set; }
        /// <summary>ใกล้โดนจับแค่ไหน 0..1 (ดังต่อเนื่องมาแล้วกี่ % ของ graceSeconds)</summary>
        public float DangerRatio { get; private set; }
        public bool Completed { get; private set; }
        public bool Running { get; private set; }

        Coroutine _routine;
        float _loudFor;

        /// <summary>เรียกจาก <see cref="ChampArrivalSequence"/> ตอนแชมป์ถึงห้องนอนแล้ว</summary>
        public void Begin()
        {
            if (Running || Completed) return;
            _routine = StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            Running = true;
            _loudFor = 0f;

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;

                float dur = (perLineSeconds != null && i < perLineSeconds.Length && perLineSeconds[i] > 0f)
                          ? perLineSeconds[i] : secondsPerLine;
                if (SubtitleManager.Instance != null) SubtitleManager.Instance.Show(lines[i], dur);

                float t = 0f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    CurrentLoudness = ReadLoudness();

                    if (IsMakingNoise())
                    {
                        _loudFor += Time.deltaTime;
                        DangerRatio = graceSeconds > 0f ? Mathf.Clamp01(_loudFor / graceSeconds) : 1f;
                        if (OnNoiseLevel != null) OnNoiseLevel(CurrentLoudness, DangerRatio);
                        if (_loudFor >= graceSeconds) { yield return CaughtRoutine(); yield break; }
                    }
                    else
                    {
                        // เงียบแล้วผ่อนคืนเร็วกว่าตอนสะสม ผู้เล่นจึงแก้ตัวได้ทัน
                        _loudFor = Mathf.Max(0f, _loudFor - Time.deltaTime * 2f);
                        DangerRatio = graceSeconds > 0f ? Mathf.Clamp01(_loudFor / graceSeconds) : 0f;
                        if (OnNoiseLevel != null) OnNoiseLevel(CurrentLoudness, DangerRatio);
                    }
                    yield return null;
                }
            }

            Complete();
        }

        float ReadLoudness()
        {
            var mic = MicNoiseDetector.Instance;
            return (useMicrophone && mic != null && mic.Available) ? mic.Loudness : 0f;
        }

        bool IsMakingNoise()
        {
            var mic = MicNoiseDetector.Instance;
            if (useMicrophone && mic != null && mic.Available)
                return mic.Loudness >= micThreshold;

            // ไม่มีไมค์ — ขยับตัวในตู้ถือว่าเสียงดัง ยังพอมีความกดดันให้ฉากนี้อยู่
            if (!fallbackToKeyPress) return false;
            var kb = Keyboard.current;
            if (kb == null) return false;
            return kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed
                   || kb.spaceKey.isPressed || kb.leftShiftKey.isPressed;
        }

        IEnumerator CaughtRoutine()
        {
            Running = false;
            HideMeter();
            if (SubtitleManager.Instance != null) SubtitleManager.Instance.Show(caughtLine, caughtHoldSeconds);
            if (Act2Director.Instance != null) Act2Director.Instance.Notice(retryNotice);

            // ใช้เวลาจริง เผื่อมีอะไรไปหยุด timeScale ไว้
            float until = Time.unscaledTime + caughtHoldSeconds;
            while (Time.unscaledTime < until) yield return null;

            // ออกจากตู้ก่อน แล้วค่อยส่งกลับเช็คพอยต์ที่ตั้งไว้ตอนเข้าไปซ่อน
            if (HideSpot.Current != null) HideSpot.Current.Exit();
            if (Act2Director.Instance != null) Act2Director.Instance.RespawnAtCheckpoint();
            if (AlertDirector.Instance != null) AlertDirector.Instance.ResetAllGuards();

            // ตั้งฉากใหม่ทั้งชุด ผู้เล่นเดินกลับไปกดซ่อนแล้วดูรอบใหม่ได้เลย
            if (sequence != null) sequence.Reset();
            if (hideSpot != null) hideSpot.Rearm();
        }

        void Complete()
        {
            Completed = true;
            Running = false;
            HideMeter();

            if (HideSpot.Current != null) HideSpot.Current.Exit();

            var director = Act2Director.Instance;
            if (director != null)
            {
                if (!string.IsNullOrEmpty(evidenceId)) director.GainEvidence(evidenceId, completeNotice);
                if (!string.IsNullOrEmpty(objectiveId)) director.Complete(objectiveId);
            }
            if (SubtitleManager.Instance != null) SubtitleManager.Instance.Show(completeNotice, 5f);
        }

        void HideMeter()
        {
            _routine = null;
            CurrentLoudness = 0f;
            DangerRatio = 0f;
            if (OnNoiseMeterHide != null) OnNoiseMeterHide();
        }
    }
}
