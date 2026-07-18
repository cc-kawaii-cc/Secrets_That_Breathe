using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// สถานะหลักของเกม — ใช้เป็น "ตัวกลาง" คุมว่าผู้เล่นทำอะไรได้บ้าง
/// แทนการ enable/disable controller กระจัดกระจายในแต่ละสคริปต์
/// </summary>
public enum GameState
{
    Boot,         // กำลังบูตระบบ
    Exploration,  // เดินสำรวจปกติ — คุมตัวละคร + มองได้
    Cutscene,     // ฉาก scripted — ล็อกการคุมทั้งหมด
    Dialogue,     // กำลังคุยกับ NPC — ล็อกการเดิน/มอง
    Inspecting,   // กำลังหมุนดูวัตถุ 3D
    Paused        // หยุดเกม
}

/// <summary>
/// แกนกลางของเกม (Persistent / DontDestroyOnLoad)
/// - เป็น "จุดเดียว" ที่เปลี่ยน GameState ได้ → กันสถานะตีกัน (race condition)
/// - เป็นตัวสั่งเปิด/ปิดการคุมผู้เล่นผ่าน PlayerManager แทนที่จะให้ทุก sequence สั่งเอง
/// - ดูแล scene flow (โหลดด่านถัดไป)
/// วาง prefab นี้ไว้ใน Bootstrap scene เพียงตัวเดียว
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState _state = GameState.Boot;
    public GameState State => _state;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // เริ่มเกมที่โหมดสำรวจ (PlayerManager.Awake รันไปก่อนแล้วใน Awake phase)
        SetState(GameState.Exploration);
    }

    /// <summary>
    /// จุดเดียวที่อนุญาตให้เปลี่ยนสถานะเกม
    /// ทุก sequence ควรเรียกอันนี้ ไม่ใช่ไปแตะ movement.enabled เอง
    /// </summary>
    public void SetState(GameState next)
    {
        if (_state == next) return;
        GameState prev = _state;
        _state = next;

        // กฎกลาง: คุมตัวละครได้เฉพาะตอน Exploration เท่านั้น
        bool canControl = next == GameState.Exploration;
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.SetControl(canControl);

        // คุมเวลาเกมตอน pause
        Time.timeScale = (next == GameState.Paused) ? 0f : 1f;

        if (logStateChanges) Debug.Log($"[GameManager] State: {prev} → {next}");
        GameEvents.GameStateChanged(next);
    }

    public bool IsState(GameState s) => _state == s;

    public void TogglePause()
    {
        if (_state == GameState.Paused) SetState(GameState.Exploration);
        else if (_state == GameState.Exploration) SetState(GameState.Paused);
    }

    // ---------- Scene Flow ----------
    /// <summary>
    /// โหลด scene ถัดไป (เช่นจบ Office → ไป CrimeScene)
    /// หมายเหตุ: ไม่เรียก GameEvents.ClearAll() อัตโนมัติตรงนี้ เพราะจะไปลบ subscription
    ///          ของ object ที่ persistent ด้วย — ให้ใช้วินัย OnEnable/OnDisable แทน
    ///          (object ใน scene เก่าจะได้ OnDisable ตอนถูกทำลายระหว่างโหลดอยู่แล้ว)
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SetState(GameState.Cutscene); // ระหว่างโหลดไม่ให้ขยับ
        SceneManager.LoadScene(sceneName);
    }
}