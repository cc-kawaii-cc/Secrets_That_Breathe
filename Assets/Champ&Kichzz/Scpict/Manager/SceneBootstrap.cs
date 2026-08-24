using UnityEngine;

/// <summary>
/// ทำให้ซีนที่ generate จากสคริปต์ (ไม่มี Managers วางไว้ในซีน) กด Play ได้ทันที
///
/// เสก manager แกนกลางที่ขาดให้ตอนรันไทม์ แล้วผูก PlayerManager เข้ากับ player ของซีนนี้
/// ถ้ามี manager อยู่แล้ว (เดินมาจากซีนก่อนหน้า หรือซีนมี Managers ของตัวเอง) จะไม่สร้างซ้ำ
///
/// ต่างจาก Act2Bootstrap ตรงที่ตัวนี้ลงแค่แกนกลาง ไม่ลากระบบลอบเร้น/นาฬิกาของ ACT 2 มาด้วย
/// </summary>
[DefaultExecutionOrder(-500)]
public class SceneBootstrap : MonoBehaviour
{
    [Header("เปิด/ปิดระบบ")]
    public bool spawnManagers = true;
    public bool bindPlayer = true;

    [Header("ไฟฉาย")]
    [Tooltip("ผูก Light ของไฟฉายให้ FlashlightController — prefab ไม่ได้ผูกมาให้ ปุ่มสลับจึงไม่มีผล")]
    public bool wireFlashlight = true;
    public bool startWithFlashlightOff = true;

    private void Awake()
    {
        if (spawnManagers) EnsureManagers();
        if (bindPlayer) EnsurePlayer();
    }

    private void EnsureManagers()
    {
        if (GameManager.Instance != null && SubtitleManager.Instance != null
            && DialogueManager.Instance != null && CinematicCamera.Instance != null) return;

        var systems = GameObject.Find("~Systems");
        if (systems == null) systems = new GameObject("~Systems");

        if (SubtitleManager.Instance == null) systems.AddComponent<SubtitleManager>();
        if (DialogueManager.Instance == null) systems.AddComponent<DialogueManager>();
        if (CinematicCamera.Instance == null) systems.AddComponent<CinematicCamera>();

        // GameManager ต้องมาหลังสุด เพราะ Start ของมันสั่ง SetState ทันที
        if (GameManager.Instance == null) systems.AddComponent<GameManager>();
    }

    private void EnsurePlayer()
    {
        var movement = FindAnyObjectByType<PlayerMovement>();
        if (movement == null)
        {
            Debug.LogWarning("[SceneBootstrap] ไม่พบ player ในซีน — คุมตัวละครไม่ได้");
            return;
        }
        var player = movement.gameObject;

        // PlayerManager ผูกกับ player ของซีนนี้ ไม่ข้ามซีน (level builder วาง player ไว้ให้แต่ละซีนแล้ว)
        //
        // ต้องสร้างบน GameObject ที่ปิดอยู่ก่อนแล้วค่อยเปิด: Awake ของมันรันทันทีที่ AddComponent
        // ซึ่งเร็วเกินกว่าจะตั้งค่าทัน และค่า default persistAcrossScenes = true
        // จะไปเรียก DontDestroyOnLoad กับ object ที่ไม่ใช่ root
        if (PlayerManager.Instance == null)
        {
            var host = new GameObject("~PlayerManager");
            host.SetActive(false);
            var pm = host.AddComponent<PlayerManager>();
            pm.persistAcrossScenes = false;
            pm.playerRoot = player;
            host.SetActive(true);
        }

        if (!wireFlashlight) return;
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
        if (torch.flashlightSource != null)
            torch.flashlightSource.enabled = torch.isFlashlightOn;
    }
}
