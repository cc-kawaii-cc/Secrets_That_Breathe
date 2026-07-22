using UnityEngine;
using UnityEngine.SceneManagement;
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Player References (ลากใส่ครั้งเดียว / เว้นว่างให้ AutoBind หาให้)")]
    public GameObject playerRoot;
    public Transform playerBody;
    public Camera playerCamera;
    public PlayerMovement movement;
    public MouseLook mouseLook;
    public CharacterController controller;
    public FlashlightController flashlight;

    [Header("Persistence")]
    [Tooltip("ให้ Player ตัวนี้อยู่ข้าม scene ไหม (เปิดถ้าใช้ Player ตัวเดียวทั้งเกม)")]
    public bool persistAcrossScenes = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

        AutoBindIfNeeded();
    }

    private void Start()
    {
        GameEvents.PlayerReady(playerRoot);
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    /// <summary>
    /// ตอนข้าม scene: player ของ scene เก่าถูกทำลายไปพร้อม scene
    /// ต้องหา player ตัวใหม่ของ scene นี้มา bind แทน ไม่งั้นคุมตัวละครไม่ได้
    /// (ถ้าเล่น scene เดี่ยวๆ playerRoot ยังไม่ตาย — ข้ามทันที ไม่กระทบของเดิม)
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return; // กันตัว duplicate ที่กำลังจะถูกทำลาย
        if (!persistAcrossScenes) return;
        if (playerRoot != null) return;

        var newMovement = FindFirstObjectByType<PlayerMovement>();
        if (newMovement == null)
        {
            Debug.LogWarning("[PlayerManager] ไม่พบ player ใน scene ใหม่ — คุมตัวละครไม่ได้");
            return;
        }

        playerRoot = newMovement.gameObject;
        playerBody = null; playerCamera = null; movement = null;
        mouseLook = null; controller = null; flashlight = null;
        AutoBindIfNeeded();

        // คืนสถานะการคุมตามกฎกลางของ GameManager
        SetControl(GameManager.Instance != null && GameManager.Instance.IsState(GameState.Exploration));
        GameEvents.PlayerReady(playerRoot);
    }
    
    private void AutoBindIfNeeded()
    {
        if (playerRoot == null)   playerRoot = gameObject;
        if (movement == null)     movement = playerRoot.GetComponentInChildren<PlayerMovement>();
        if (controller == null)   controller = playerRoot.GetComponentInChildren<CharacterController>();
        if (mouseLook == null)    mouseLook = playerRoot.GetComponentInChildren<MouseLook>();
        if (playerCamera == null) playerCamera = playerRoot.GetComponentInChildren<Camera>();
        if (flashlight == null)   flashlight = playerRoot.GetComponentInChildren<FlashlightController>();
        if (playerBody == null && movement != null) playerBody = movement.transform;
    }
    
    public void SetControl(bool enabled)
    {
        if (movement) movement.enabled = enabled;
        if (mouseLook) mouseLook.enabled = enabled;
    }
    
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (controller != null) controller.enabled = false;
        playerRoot.transform.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();
        if (controller != null) controller.enabled = true;
    }
}