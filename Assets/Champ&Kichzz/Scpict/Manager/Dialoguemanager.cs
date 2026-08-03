using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    public GameObject dialogPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogText;

    [Header("Input (New Input System)")]
    [Tooltip("ปุ่มสำหรับไปบรรทัดถัดไป (เช่น คลิกซ้าย / ปุ่ม Submit)")]
    public InputActionReference advanceAction;

    public bool IsTalking { get; private set; }

    private string[] _names;
    private string[] _lines;
    private int _index;
    private int _startFrame;
    private Action _onComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (dialogPanel) dialogPanel.SetActive(false);
    }

    private void OnEnable()
    {
        advanceAction?.action.Enable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        advanceAction?.action.Disable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ตอนข้าม scene: panel/text ของ scene เก่าถูกทำลาย — หาชุดใหม่ใต้ player ใหม่
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;
        if (dialogPanel != null) return;
        BindUI();
    }

    /// <summary>
    /// ผูก UI บทสนทนาเข้ากับ player ปัจจุบัน
    /// เรียกได้ทั้งตอนโหลด scene และตอนจะใช้งานจริง (lazy) — กันกรณี sceneLoaded ไม่ยิง
    /// (เช่นกด Play ที่ scene เริ่มต้นโดยตรง event นี้จะไม่ทำงาน)
    /// ค้นหาแบบ recursive จึงไม่ติดปัญหาถ้า path ใน hierarchy ต่างจากเดิม
    /// </summary>
    private void BindUI()
    {
        var newPlayer = FindFirstObjectByType<PlayerMovement>();
        if (newPlayer == null) return;

        Transform panel = FindDeep(newPlayer.transform, "PoliceDialogPanel");
        if (panel == null) return;

        dialogPanel = panel.gameObject;
        Transform n = FindDeep(panel, "NameText");
        Transform d = FindDeep(panel, "DialogText");
        nameText = n ? n.GetComponent<TextMeshProUGUI>() : null;
        dialogText = d ? d.GetComponent<TextMeshProUGUI>() : null;
        IsTalking = false;
        dialogPanel.SetActive(false); // ซ่อนเหมือนตอน Awake ปกติ
    }

    // ค้นหาลูก (รวมตัวที่ inactive) ตามชื่อแบบลึกทุกชั้น
    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    public void StartDialogue(string[] speakerNames, string[] dialogLines, Action onComplete = null)
    {
        if (IsTalking || dialogLines == null || dialogLines.Length == 0) return;
        BindUI(); // ผูกกับ panel ของ player ที่ active อยู่เสมอ — กัน panel เก่าถูกทำลายข้าม scene
        if (dialogPanel == null)
        {
            Debug.LogWarning("[DialogueManager] หา PoliceDialogPanel ใต้ player ไม่เจอ — บทสนทนาจะไม่ขึ้น");
            return;
        }

        _names = speakerNames;
        _lines = dialogLines;
        _onComplete = onComplete;
        _index = 0;
        _startFrame = Time.frameCount;
        IsTalking = true;

        if (GameManager.Instance) GameManager.Instance.SetState(GameState.Dialogue);
        if (dialogPanel) dialogPanel.SetActive(true);
        ShowLine();
    }

    private void Update()
    {
        if (!IsTalking) return;
        // กันเพิ่งเริ่มคุยเฟรมเดียวกับที่กด E — รอเฟรมถัดไปค่อยรับ input เลื่อนบรรทัด
        if (Time.frameCount <= _startFrame) return;
        if (AdvancePressed())
        {
            _index++;
            if (_index < _lines.Length) ShowLine();
            else EndDialogue();
        }
    }

    // เลื่อนบรรทัดได้หลายทาง — กันค้างถ้า advanceAction ไม่ถูกตั้ง/ไม่ถูกกด
    private bool AdvancePressed()
    {
        if (advanceAction != null && advanceAction.action != null
            && advanceAction.action.WasPressedThisFrame()) return true;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame
                           || kb.enterKey.wasPressedThisFrame
                           || kb.eKey.wasPressedThisFrame)) return true;

        return false;
    }

    private void ShowLine()
    {
        if (nameText)
            nameText.text = (_names != null && _index < _names.Length) ? _names[_index] : "";
        if (dialogText)
            dialogText.text = _lines[_index];
    }

    private void EndDialogue()
    {
        IsTalking = false;
        if (dialogPanel) dialogPanel.SetActive(false);
        if (GameManager.Instance) GameManager.Instance.SetState(GameState.Exploration);

        Action cb = _onComplete;
        _onComplete = null;
        cb?.Invoke();
    }
}