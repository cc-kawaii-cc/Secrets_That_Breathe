using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// คุยกับตำรวจในซีน Main3_2
/// - ผูกกับ StoryInteractable ของตัวตำรวจ: เดินเข้าใกล้ + เล็งแล้วกด E → เรียก Talk()
/// - เปิดบทสนทนาผ่าน DialogueManager (แก้/เพิ่มบรรทัดได้จาก Inspector)
/// - พอคุยจบจะ "ปลดล็อก" ทางออก (wrap) ให้กด E กลับ Main3 ได้
///   ก่อนคุยจบ ทางออกถูกล็อกไว้ (StoryInteractable.hasInteracted = true) จึงกดกลับไม่ได้
/// </summary>
public class PoliceDialogue : MonoBehaviour
{
    [Header("บทสนทนา (แก้/เพิ่มบรรทัดได้ — ชื่อผู้พูดกับประโยคจับคู่กันตาม index)")]
    [Tooltip("ชื่อผู้พูดของแต่ละบรรทัด เช่น เข้ม / ตำรวจ")]
    public string[] speakerNames;
    [TextArea(2, 4)]
    public string[] dialogLines;

    [Header("ปลดล็อกทางออกเมื่อคุยจบ")]
    [Tooltip("ลาก StoryInteractable ของ wrap (ทางกลับ Main3) มาใส่ — เริ่มเกมให้ตั้ง hasInteracted = true ไว้")]
    public StoryInteractable exitToUnlock;

    [Header("อีเวนต์ตอนคุยจบ (optional)")]
    public UnityEvent onDialogueEnd;

    public bool HasTalked { get; private set; }

    /// <summary>เรียกจาก StoryInteractable.onInteract ของตัวตำรวจ (ตอนกด E)</summary>
    public void Talk()
    {
        if (HasTalked) return;
        if (DialogueManager.Instance == null || DialogueManager.Instance.IsTalking) return;
        DialogueManager.Instance.StartDialogue(speakerNames, dialogLines, OnEnd);
    }

    private void OnEnd()
    {
        HasTalked = true;
        // ปลดล็อก wrap: กลับมา "กด E ได้" อีกครั้ง
        if (exitToUnlock != null) exitToUnlock.hasInteracted = false;
        onDialogueEnd?.Invoke();
    }
}
