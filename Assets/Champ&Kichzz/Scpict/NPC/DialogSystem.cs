using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class DialogSystem : MonoBehaviour
{
    public static DialogSystem instance;

    [Header("Input Actions")]
    public InputActionReference advanceDialogAction; // ปุ่มสำหรับคลิกเพื่อไปประโยคถัดไป

    [Header("Player Control")]
    public PlayerMovement playerMovement;
    public MouseLook mouseLook;

    [Header("UI References")]
    public GameObject dialogPanel; 
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogText;

    private string[] currentNames;
    private string[] currentLines;
    private int currentLineIndex = 0;
    
    private bool isTalking = false;
    private NPCWalker currentNPC;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void StartDialog(NPCWalker npc, string[] names, string[] lines)
    {
        if (isTalking) return;
        
        isTalking = true;
        currentNPC = npc;
        currentNames = names;
        currentLines = lines;
        currentLineIndex = 0;

        if (playerMovement) playerMovement.enabled = false;
        if (mouseLook) mouseLook.enabled = false;

        dialogPanel.SetActive(true);
        ShowNextLine();
    }

    void Update()
    {
        // ใช้ Action แทนเมาส์คลิกซ้าย
        if (isTalking && advanceDialogAction.action.WasPressedThisFrame())
        {
            currentLineIndex++;
            if (currentLineIndex < currentLines.Length)
            {
                ShowNextLine();
            }
            else
            {
                EndDialog();
            }
        }
    }

    void ShowNextLine()
    {
        nameText.text = currentNames[currentLineIndex];
        dialogText.text = currentLines[currentLineIndex];
    }

    void EndDialog()
    {
        isTalking = false;
        dialogPanel.SetActive(false);
        
        if (playerMovement) playerMovement.enabled = true;
        if (mouseLook) mouseLook.enabled = true;
        if (currentNPC) currentNPC.EndTalking();
    }

    private void OnEnable() => advanceDialogAction?.action.Enable();
    private void OnDisable() => advanceDialogAction?.action.Disable();
}