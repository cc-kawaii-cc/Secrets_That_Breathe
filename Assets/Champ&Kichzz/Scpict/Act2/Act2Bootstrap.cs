using UnityEngine;

namespace SecretsThatBreathe.Act2
{
    /// <summary>
    /// ทำให้ซีนไหนของ ACT 2 ก็กด Play ได้ทันที
    ///
    /// ซีนทั้งสามถูก generate จากสคริปต์ จึงไม่มี manager วางไว้ในซีนเลย
    /// ตัวนี้เสก manager ที่ขาดให้ตอนรันไทม์ และติดความสามารถที่ ACT 2 ต้องใช้
    /// ให้ player ตัวที่อยู่ในซีนนั้น
    ///
    /// ถ้าซีนไหนมี manager อยู่แล้ว (เช่นเปิดมาจาก Bootstrap scene) จะไม่สร้างซ้ำ
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class Act2Bootstrap : MonoBehaviour
    {
        [Header("เปิด/ปิดระบบ")]
        public bool spawnManagers = true;
        public bool grantPlayerAbilities = true;
        public bool spawnHUD = true;
        public bool spawnAlertDirector = true;

        [Header("ไฟฉาย")]
        [Tooltip("เริ่มด่านโดยปิดไฟฉาย — ด่านลอบเร้นไม่ควรเริ่มด้วยการถือไฟส่องหน้ายาม")]
        public bool startWithFlashlightOff = true;

        [Header("ตัวเลือกเทส")]
        [Tooltip("โชว์ตัวเลขค่าซ่อนตัวบนจอ สำหรับจูนสมดุล")]
        public bool showStealthDebug = false;
        [Tooltip("เริ่ม ACT 2 ที่ objective ลำดับนี้ (0 = ตั้งแต่ต้น)")]
        public int startAtObjective = 0;

        void Awake()
        {
            if (spawnManagers) EnsureManagers();
            if (spawnAlertDirector && AlertDirector.Instance == null)
                gameObject.AddComponent<AlertDirector>();
            if (grantPlayerAbilities) EnsurePlayerAbilities();
        }

        void EnsureManagers()
        {
            // manager ที่ต้องอยู่ข้าม scene รวมไว้ก้อนเดียว
            GameObject systems = null;
            if (GameManager.Instance == null || SubtitleManager.Instance == null
                || DialogueManager.Instance == null || Act2Director.Instance == null
                || DeadlineClock.Instance == null)
            {
                systems = GameObject.Find("~Act2 Systems");
                if (systems == null) systems = new GameObject("~Act2 Systems");
            }

            // ลำดับสำคัญ: Act2Director กับ HUD ต้องมีก่อน GameManager เรียก SetState
            if (Act2Director.Instance == null)
            {
                var director = systems.AddComponent<Act2Director>();
                director.startAtIndex = startAtObjective;
            }
            if (DeadlineClock.Instance == null) systems.AddComponent<DeadlineClock>();
            if (SubtitleManager.Instance == null) systems.AddComponent<SubtitleManager>();
            if (DialogueManager.Instance == null) systems.AddComponent<DialogueManager>();

            if (spawnHUD && FindAnyObjectByType<Act2HUD>() == null)
            {
                var hudHost = systems != null ? systems : new GameObject("~Act2 Systems");
                var hud = hudHost.AddComponent<Act2HUD>();
                hud.showDebugStealthValues = showStealthDebug;
            }

            // GameManager ต้องมาหลังสุด เพราะ Start ของมันจะสั่ง SetState ทันที
            if (GameManager.Instance == null) systems.AddComponent<GameManager>();
        }

        void EnsurePlayerAbilities()
        {
            var movement = FindAnyObjectByType<PlayerMovement>();
            if (movement == null)
            {
                Debug.LogWarning("[Act2Bootstrap] ไม่พบ player ในซีน — ความสามารถของ ACT 2 จะไม่ถูกติดตั้ง");
                return;
            }
            var player = movement.gameObject;

            // PlayerManager ผูกกับ player ของซีนนี้ ไม่ข้ามซีน
            // (แต่ละซีนมี player ของตัวเองที่ level builder วางไว้)
            //
            // ต้องสร้างบน GameObject ที่ปิดอยู่ก่อน แล้วค่อยเปิด: Awake ของมันจะรัน
            // ทันทีที่ AddComponent ซึ่งเร็วเกินกว่าจะตั้งค่าทัน และค่า default
            // persistAcrossScenes = true จะไปเรียก DontDestroyOnLoad กับ object ที่ไม่ใช่ root
            if (PlayerManager.Instance == null)
            {
                var host = new GameObject("~Act2 PlayerManager");
                host.SetActive(false);
                var pm = host.AddComponent<PlayerManager>();
                pm.persistAcrossScenes = false;
                pm.playerRoot = player;
                host.SetActive(true);
            }

            // FlashlightController.flashlightSource ไม่ได้ถูกผูกไว้บน prefab
            // ตัว Light จึงติดค้างตลอดเวลาและปุ่มสลับไม่มีผล ผูกให้ตรงนี้
            var torch = player.GetComponentInChildren<FlashlightController>(true);
            if (torch != null)
            {
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
                if (torch.flashlightSource != null)
                    torch.flashlightSource.enabled = torch.isFlashlightOn;
            }

            if (player.GetComponent<CrouchAbility>() == null) player.AddComponent<CrouchAbility>();
            if (player.GetComponent<StealthTarget>() == null) player.AddComponent<StealthTarget>();
            if (player.GetComponent<EvidenceCamera>() == null) player.AddComponent<EvidenceCamera>();
            if (player.GetComponent<ThrowAbility>() == null) player.AddComponent<ThrowAbility>();
        }
    }
}
