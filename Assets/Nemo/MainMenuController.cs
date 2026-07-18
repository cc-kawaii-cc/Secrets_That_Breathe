using UnityEngine;
using System.Collections; // ต้องมีสำหรับใช้งาน Coroutine

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("ลาก Panel เลือกด่านมาใส่ที่นี่")]
    // เปลี่ยนมาใช้ RectTransform เพื่อให้จัดการตำแหน่งหน้าจอได้ง่ายขึ้น
    public RectTransform levelSelectPanel;

    [Header("ตั้งค่าการสไลด์ (Animation Settings)")]
    public float slideDuration = 0.5f; // เวลาที่ใช้สไลด์ (วินาที)

    [Tooltip("กราฟปรับความเด้ง ให้จุดเริ่มต้นอยู่ที่ 0 และจุดจบอยู่ที่ 1")]
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector2 offScreenPosition; // ตำแหน่งนอกจอ (ซ้ายสุด)
    private Vector2 onScreenPosition;  // ตำแหน่งกลางจอ (เป้าหมาย)

    void Start()
    {
        if (levelSelectPanel != null)
        {
            // จำตำแหน่งเป้าหมายตรงกลางจอเอาไว้ก่อน
            onScreenPosition = levelSelectPanel.anchoredPosition;

            // คำนวณตำแหน่งนอกจอฝั่งซ้าย (เอาความกว้างจอไปคูณให้มันหลุดขอบ)
            offScreenPosition = new Vector2(-Screen.width * 2f, onScreenPosition.y);

            // จับ Panel ไปซ่อนไว้ฝั่งซ้ายก่อนเกมเริ่ม
            levelSelectPanel.anchoredPosition = offScreenPosition;
            levelSelectPanel.gameObject.SetActive(false);
        }
    }

    // ฟังก์ชันเปิดหน้าเลือกด่าน (ผูกกับปุ่ม Start)
    public void OpenLevelSelect()
    {
        if (levelSelectPanel != null)
        {
            levelSelectPanel.gameObject.SetActive(true);
            StopAllCoroutines(); // หยุดอนิเมชันเก่าเผื่อผู้เล่นกดย้ำๆ
            StartCoroutine(AnimatePanel(offScreenPosition, onScreenPosition, false));
        }
    }

    // ฟังก์ชันปิดหน้าเลือกด่าน (ผูกกับปุ่ม Back)
    public void CloseLevelSelect()
    {
        if (levelSelectPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimatePanel(levelSelectPanel.anchoredPosition, offScreenPosition, true));
        }
    }

    // ระบบรันอนิเมชันสไลด์
    private IEnumerator AnimatePanel(Vector2 start, Vector2 end, bool isClosing)
    {
        float elapsedTime = 0f;

        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.deltaTime;
            float timeRatio = elapsedTime / slideDuration;

            // ดึงค่าจากเส้นกราฟที่คุณตั้งไว้ เพื่อให้เกิดเอฟเฟกต์ไดนามิก
            float curveValue = slideCurve.Evaluate(timeRatio);

            // LerpUnclamped ทำให้กราฟพุ่งทะลุ 100% แล้วเด้งกลับมาได้ (เพิ่มความดึ๋ง)
            levelSelectPanel.anchoredPosition = Vector2.LerpUnclamped(start, end, curveValue);

            yield return null; // รอเฟรมถัดไป
        }

        // ทำให้แน่ใจว่าตอนจบมันอยู่ตรงตำแหน่งเป๊ะๆ
        levelSelectPanel.anchoredPosition = end;

        // ถ้าเป็นการสไลด์ปิด ให้ซ่อน GameObject ไปด้วยเพื่อประหยัดทรัพยากร
        if (isClosing)
        {
            levelSelectPanel.gameObject.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Debug.Log("ออกจากเกมแล้ว!");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}