using System.Collections;
using UnityEngine;

/// <summary>
/// ลำดับ "ขึ้นรถขับออกจากพื้นที่" — คู่กับ CarIntroSequence (ขาเข้า)
/// วางไว้บนตัวรถ แล้วใส่ StoryInteractable ที่รถ ผูก onInteract → TryDepart()
/// การปลดล็อกมี 2 ทาง:
///   1) เรียก Unlock() จาก event อื่น (เช่น คุยกับตำรวจจบ)
///   2) ใส่ unlockClueId — ปลดล็อกอัตโนมัติเมื่อ GameEvents.ClueFound ส่ง id ตรงกัน
/// </summary>
public class CarDepartSequence : MonoBehaviour
{
    [Header("◾ Player / Car")]
    [Tooltip("จุดนั่งในรถ (ใช้ตัวเดียวกับ CarIntroSequence ได้)")]
    public Transform carSeat;
    [Tooltip("เส้นทางขับออก — ลากจุดเรียงตามลำดับ")]
    public Transform[] waypoints;
    public float carSpeed = 7f;

    [Header("◾ หันมองก่อนออกรถ (เว้นว่าง = ไม่หัน)")]
    [Tooltip("เช่น จุดเกิดเหตุ — เข้มจะหันมองก่อนขับออก")]
    public Transform lookBackTarget;
    public float lookBackDuration = 2.5f;
    public Vector3 lookBackOffset = new Vector3(0f, 1f, 0f);

    [Header("◾ การปลดล็อก")]
    [Tooltip("ติ๊กไว้ = ขึ้นรถได้เลย / ไม่ติ๊ก = ต้องรอ Unlock ก่อน")]
    public bool unlocked = false;
    [Tooltip("ปลดล็อกอัตโนมัติเมื่อเก็บหลักฐาน id นี้ (เว้นว่าง = ไม่ใช้)")]
    public string unlockClueId = "";
    [TextArea] public string lockedHint = "ยังไปไม่ได้... มีเรื่องต้องจัดการก่อน";

    [Header("◾ จบฉาก")]
    [Tooltip("ชื่อ scene ถัดไป (ต้องอยู่ใน Build Settings) — เว้นว่าง = โชว์ข้อความจบบทแทน")]
    public string nextSceneName = "";
    [TextArea] public string endText = "";
    public float fadeTime = 1.2f;

    [Header("◾ อ้างอิง (เว้นว่างให้หาเองตอนใช้งาน)")]
    [Tooltip("StoryInteractable บนตัวรถ — ไว้รีเซ็ตให้กดซ้ำได้ตอนยังล็อกอยู่")]
    public StoryInteractable carInteractable;
    [Tooltip("FadeImage ใน Canvas ของ player")]
    public UnityEngine.UI.Image fadeImage;

    private bool _departing;

    private void OnEnable()  => GameEvents.OnClueFound += OnClueFound;
    private void OnDisable() => GameEvents.OnClueFound -= OnClueFound;

    private void OnClueFound(string clueId)
    {
        if (!string.IsNullOrEmpty(unlockClueId) && clueId == unlockClueId)
            unlocked = true;
    }

    public void Unlock() => unlocked = true;

    /// <summary>เรียกจาก StoryInteractable.onInteract บนตัวรถ</summary>
    public void TryDepart()
    {
        if (_departing) return;
        if (!unlocked)
        {
            if (SubtitleManager.Instance) SubtitleManager.Instance.Show(lockedHint, 2.5f);
            if (carInteractable) carInteractable.hasInteracted = false; // ให้กลับมากดใหม่ได้
            return;
        }
        StartCoroutine(Depart());
    }

    private IEnumerator Depart()
    {
        _departing = true;
        var pm = PlayerManager.Instance;
        GameManager.Instance.SetState(GameState.Cutscene);

        // จับผู้เล่นขึ้นนั่งรถ
        if (pm != null && carSeat != null)
        {
            if (pm.controller) pm.controller.enabled = false;
            pm.playerRoot.transform.SetPositionAndRotation(carSeat.position, carSeat.rotation);
            pm.playerRoot.transform.SetParent(carSeat);

            // ล้างมุมก้ม/เงยที่ค้างจากตอนเล็งกดขึ้นรถ — ให้มองตรงตามหน้ารถ
            if (pm.playerCamera)
                pm.playerCamera.transform.localRotation = Quaternion.identity;
        }

        // หันมองจุดเกิดเหตุก่อนขับออก
        if (lookBackTarget != null && CinematicCamera.Instance != null)
        {
            yield return CinematicCamera.Instance.LookAt(lookBackTarget, 0.8f, lookBackOffset);
            yield return new WaitForSeconds(lookBackDuration);
            CinematicCamera.Instance.EndOverride();
        }

        // ขับตามเส้นทาง (ล็อกแกน Y ของรถไว้ ระดับพื้นไม่เด้ง)
        foreach (Transform wp in waypoints)
        {
            if (wp == null) continue;
            while (true)
            {
                Vector3 target = new Vector3(wp.position.x, transform.position.y, wp.position.z);
                if (Vector3.Distance(transform.position, target) <= 0.15f) break;

                transform.position = Vector3.MoveTowards(transform.position, target, carSpeed * Time.deltaTime);
                Vector3 dir = target - transform.position; dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 3f);
                yield return null;
            }
        }

        // Fade มืด
        if (fadeImage == null && pm != null)
        {
            Transform f = pm.playerRoot.transform.Find("Canvas/FadeImage");
            if (f) fadeImage = f.GetComponent<UnityEngine.UI.Image>();
        }
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float t = 0f;
            Color c = fadeImage.color;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.05f, fadeTime);
                c.a = Mathf.Clamp01(t);
                fadeImage.color = c;
                yield return null;
            }
        }

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (pm != null) pm.playerRoot.transform.SetParent(null); // ปล่อยจากรถก่อนเปลี่ยน scene
            GameManager.Instance.LoadScene(nextSceneName);
        }
        else if (!string.IsNullOrEmpty(endText) && SubtitleManager.Instance)
        {
            SubtitleManager.Instance.Show(endText, 6f);
        }
    }
}
