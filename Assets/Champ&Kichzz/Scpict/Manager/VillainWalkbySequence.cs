using System.Collections;
using UnityEngine;

/// <summary>
/// คัตซีน "ผู้ร้ายเดินผ่าน" — ใช้ตอนผู้เล่นวาร์ปมาถึงซีน Main3_2
/// ลำดับเหตุการณ์:
///   1) เริ่มคัตซีน (เล่นทันทีตอนมาถึง ถ้า playOnStart, หรือรอผู้เล่นเดินชนทริกเกอร์)
///   2) ล็อกการควบคุม (Cutscene) แล้วปล่อยผู้ร้ายเดินจาก startPoint → endPoint
///      ผ่านหน้าผู้เล่น
///   3) กล้องผู้เล่นหันมองตามผู้ร้ายไปเรื่อย ๆ จนผู้ร้ายเดินพ้นไป
///   4) คืนการควบคุม (Exploration) ให้เล่นต่อได้ตามปกติ
/// </summary>
public class VillainWalkbySequence : MonoBehaviour
{
    [Header("เงื่อนไขเริ่มคัตซีน")]
    [Tooltip("เล่นทันทีตอนมาถึงซีน (ตรงตามดีไซน์ 'วาร์ปมา Wrap แล้วเล่นเลย')\nถ้าปิด จะรอผู้เล่นเดินชน Collider (ต้องติ๊ก Is Trigger)")]
    public bool playOnStart = true;
    [Tooltip("หน่วงเวลาเล็กน้อยหลังมาถึงก่อนเริ่ม (กันผู้เล่น/กล้องยัง bind ไม่เสร็จ)")]
    public float startDelay = 0.4f;

    [Header("Villain (ผู้ร้าย — ลากตัวโมเดลที่สร้างใหม่ใส่ ปล่อย inactive ไว้ก็ได้)")]
    public GameObject villain;
    [Tooltip("ตัวขับ Animator เดิน (optional) — จะสั่ง bool ตอนเริ่ม/หยุดเดิน")]
    public Animator villainAnimator;
    public string walkAnimBool = "IsWalking";

    [Header("เส้นทางเดิน")]
    public Transform startPoint;   // จุดที่ผู้ร้ายโผล่มา
    public Transform endPoint;     // จุดที่ผู้ร้ายเดินหายไป
    public float walkSpeed = 2.2f;
    [Tooltip("ระยะถึง endPoint ที่ถือว่าเดินพ้นแล้ว")]
    public float arriveThreshold = 0.15f;

    [Header("กล้องมองตาม")]
    [Tooltip("เปิด = กล้องผู้เล่นหันมองตามผู้ร้าย / ปิด = กล้องอยู่เฉย ๆ ผู้ร้ายยังเดินปกติ")]
    public bool cameraFollowsVillain = false;
    [Tooltip("จุดเล็งบนตัวผู้ร้าย (ยกขึ้นระดับหน้าอก/หัว)")]
    public Vector3 lookOffset = new Vector3(0f, 1.4f, 0f);
    public float cameraTurnSpeed = 6f;

    [Header("บทบรรยาย (optional)")]
    [TextArea(2, 3)] public string[] subtitleLines;
    public float subtitleDuration = 3f;

    private bool _triggered;

    private void Start()
    {
        if (villain) villain.SetActive(false);
        if (playOnStart) StartCoroutine(AutoStart());
    }

    private IEnumerator AutoStart()
    {
        // รอผู้เล่น/กล้องถูก bind (PlayerManager rebind ใน sceneLoaded) แล้วค่อยเริ่ม
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
        while (PlayerManager.Instance == null || PlayerManager.Instance.playerBody == null)
            yield return null;
        if (_triggered) yield break;
        _triggered = true;
        yield return Sequence();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playOnStart || _triggered || !other.CompareTag("Player")) return;
        _triggered = true;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // ล็อกการควบคุมเฉพาะตอนที่กล้องต้องมองตาม — ถ้าปล่อยกล้องอิสระ ไม่ล็อก
        // (ผู้ร้ายจะเดินเป็นฉากหลัง ผู้เล่นยังเดิน/มองเองได้ตามปกติ)
        if (cameraFollowsVillain)
        {
            GameManager.Instance.SetState(GameState.Cutscene);
            GameEvents.CutsceneStart();
        }

        if (subtitleLines != null && subtitleLines.Length > 0)
            SubtitleManager.Instance.Show(subtitleLines, subtitleDuration);

        // ตั้งผู้ร้ายที่จุดเริ่มแล้วเปิดตัว
        if (villain && startPoint)
            villain.transform.SetPositionAndRotation(startPoint.position, startPoint.rotation);
        if (villain) villain.SetActive(true);
        if (villainAnimator && !string.IsNullOrEmpty(walkAnimBool))
            villainAnimator.SetBool(walkAnimBool, true);

        // กล้องหันมองตามผู้ร้ายไปเรื่อย ๆ จนกว่าจะเดินพ้น (เปิด/ปิดได้)
        bool walking = true;
        if (villain && cameraFollowsVillain)
            StartCoroutine(CinematicCamera.Instance.FollowTarget(
                villain.transform, () => !walking, lookOffset, cameraTurnSpeed));

        // เดินผู้ร้าย start → end
        if (villain && endPoint)
        {
            while (Vector3.Distance(villain.transform.position, endPoint.position) > arriveThreshold)
            {
                Vector3 flatDir = endPoint.position - villain.transform.position;
                flatDir.y = 0f;
                if (flatDir.sqrMagnitude > 0.0001f)
                    villain.transform.rotation = Quaternion.Slerp(
                        villain.transform.rotation,
                        Quaternion.LookRotation(flatDir),
                        8f * Time.deltaTime);

                villain.transform.position = Vector3.MoveTowards(
                    villain.transform.position, endPoint.position, walkSpeed * Time.deltaTime);
                yield return null;
            }
        }

        // ผู้ร้ายเดินพ้นแล้ว — หยุดตามกล้อง แล้วคืนการควบคุม
        walking = false;
        if (villainAnimator && !string.IsNullOrEmpty(walkAnimBool))
            villainAnimator.SetBool(walkAnimBool, false);
        if (villain) villain.SetActive(false);

        if (cameraFollowsVillain)
        {
            CinematicCamera.Instance.EndOverride();
            GameManager.Instance.SetState(GameState.Exploration);
            GameEvents.CutsceneEnd();
        }
    }
}
