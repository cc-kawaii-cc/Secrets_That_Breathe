using System;
using UnityEngine;
using UnityEngine.InputSystem;
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
    private Action _onComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (dialogPanel) dialogPanel.SetActive(false);
    }

    private void OnEnable()  => advanceAction?.action.Enable();
    private void OnDisable() => advanceAction?.action.Disable();
    
    public void StartDialogue(string[] speakerNames, string[] dialogLines, Action onComplete = null)
    {
        if (IsTalking || dialogLines == null || dialogLines.Length == 0) return;

        _names = speakerNames;
        _lines = dialogLines;
        _onComplete = onComplete;
        _index = 0;
        IsTalking = true;

        if (GameManager.Instance) GameManager.Instance.SetState(GameState.Dialogue);
        if (dialogPanel) dialogPanel.SetActive(true);
        ShowLine();
    }

    private void Update()
    {
        if (!IsTalking) return;
        if (advanceAction != null && advanceAction.action.WasPressedThisFrame())
        {
            _index++;
            if (_index < _lines.Length) ShowLine();
            else EndDialogue();
        }
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