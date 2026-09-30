using SecretsThatBreathe.Act2;
using TMPro;
using UnityEngine;

namespace SecretsThatBreathe.Act4
{
    /// <summary>
    /// ทำให้ซีนของ ACT 4 กด Play ได้ทันที เหมือน Act2Bootstrap แต่ไม่ลากระบบลอบเร้นมาด้วย
    ///
    /// - เสก manager แกนกลาง + Act2Director (objective ของ ACT 4 อยู่ใน Act2Script) + Act2HUD
    /// - สร้าง Act4HUD (จอดำ, แถบซีเนมา, กล่องบทพูด, ซักค้าน, QTE) ของซีนนี้
    /// - หยุดนาฬิกาเส้นตายของ ACT 2 ที่อาจติดมาข้ามซีน — SD Card ได้มาแล้ว เส้นตายหมดความหมาย
    /// - ใส่หลักฐานในแฟ้มคดีให้ครบ กดเล่นซีนศาลตรง ๆ ก็ซักค้านได้
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class Act4Bootstrap : MonoBehaviour
    {
        [Header("ไฟฉาย")]
        public bool startWithFlashlightOff = true;

        [Header("แฟ้มคดี")]
        [Tooltip("ใส่หลักฐานที่เรื่องรับประกันว่ามีแล้วให้ครบ (ใช้ตอนกด Play ที่ซีนนี้ตรงๆ)")]
        public bool ensureCaseFile = true;

        void Awake()
        {
            EnsureManagers();
            EnsurePlayer();
            if (FindAnyObjectByType<Act4HUD>() == null) new GameObject("~Act4 HUD").AddComponent<Act4HUD>();
        }

        void Start()
        {
            StopDeadlineClock();
            if (!ensureCaseFile || Act2Director.Instance == null) return;
            // หลักฐานตามแฟ้มคดีใน data asset ของห้องพิจารณาคดี (ซีนอื่นใช้ค่าเริ่มต้น)
            var court = FindAnyObjectByType<CourtroomSequence>();
            var evidence = court != null ? court.Data.caseFile.evidence : Act4Script.DefaultCaseFile();
            if (evidence != null)
                for (int i = 0; i < evidence.Length; i++)
                    Act2Director.Instance.GainEvidence(evidence[i].id);
        }

        void EnsureManagers()
        {
            GameObject systems = null;
            if (GameManager.Instance == null || SubtitleManager.Instance == null
                || DialogueManager.Instance == null || Act2Director.Instance == null
                || FindAnyObjectByType<Act2HUD>() == null)
            {
                systems = GameObject.Find("~Act2 Systems");
                if (systems == null) systems = new GameObject("~Act2 Systems");
            }

            // ลำดับสำคัญ: Director กับ HUD ต้องมีก่อน GameManager เรียก SetState (เหมือน Act2Bootstrap)
            if (Act2Director.Instance == null) systems.AddComponent<Act2Director>();
            if (SubtitleManager.Instance == null) systems.AddComponent<SubtitleManager>();
            if (DialogueManager.Instance == null) systems.AddComponent<DialogueManager>();
            if (FindAnyObjectByType<Act2HUD>() == null) systems.AddComponent<Act2HUD>();
            if (GameManager.Instance == null) systems.AddComponent<GameManager>();
        }

        void EnsurePlayer()
        {
            var movement = FindAnyObjectByType<PlayerMovement>();
            if (movement == null)
            {
                Debug.LogWarning("[Act4Bootstrap] ไม่พบ player ในซีน — คุมตัวละครไม่ได้");
                return;
            }
            var player = movement.gameObject;

            // เหมือน Act2Bootstrap: สร้างบน object ที่ปิดอยู่ก่อน ตั้งค่าเสร็จค่อยเปิด
            if (PlayerManager.Instance == null)
            {
                var host = new GameObject("~Act4 PlayerManager");
                host.SetActive(false);
                var pm = host.AddComponent<PlayerManager>();
                pm.persistAcrossScenes = false;
                pm.playerRoot = player;
                host.SetActive(true);
            }

            var torch = player.GetComponentInChildren<FlashlightController>(true);
            if (torch == null) return;
            if (torch.flashlightSource == null)
            {
                var lights = player.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type != LightType.Spot) continue;
                    torch.flashlightSource = lights[i];
                    break;
                }
            }
            if (startWithFlashlightOff) torch.isFlashlightOn = false;
            if (torch.flashlightSource != null) torch.flashlightSource.enabled = torch.isFlashlightOn;
        }

        /// <summary>
        /// นาฬิกาของ ACT 2 อยู่ข้ามซีน (DontDestroyOnLoad) ถ้าเล่นต่อมาจากเพนต์เฮาส์มันยังเดินอยู่
        /// หยุดมันแล้วล้างตัวเลขบน HUD ทิ้ง ไม่งั้นนับถอยหลังค้างกลางห้องพิจารณาคดี
        /// </summary>
        static void StopDeadlineClock()
        {
            if (DeadlineClock.Instance != null) DeadlineClock.Instance.StopClock();
            var hud = FindAnyObjectByType<Act2HUD>();
            if (hud == null) return;
            foreach (var t in hud.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == "Clock") t.text = "";
        }
    }
}
