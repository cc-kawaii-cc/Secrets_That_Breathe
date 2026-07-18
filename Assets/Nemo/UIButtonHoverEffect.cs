using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; // เพิ่มเข้ามาเพื่อรองรับ TextMeshPro

public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("ตั้งค่าการขยายขนาด")]
    public float scaleMultiplier = 1.15f;
    public float animationSpeed = 15f;

    [Header("ตั้งค่าความสว่าง (สี)")]
    public Color hoverColor = Color.white;

    [Header("เป้าหมายที่จะให้สว่าง (ถ้าปล่อยว่าง โค้ดจะหาให้อัตโนมัติ)")]
    public Graphic targetText;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private Color originalColor;
    private Color targetColor;

    void Start()
    {
        // เก็บค่าเริ่มต้นของขนาด
        originalScale = transform.localScale;
        targetScale = originalScale;

        // ถ้าไม่ได้ลาก Text มาใส่ใน Inspector โค้ดจะค้นหาจากตัวมันเองและลูกๆ ของมัน
        if (targetText == null)
        {
            // หา TextMeshPro ก่อน (นิยมใช้สุด)
            targetText = GetComponentInChildren<TextMeshProUGUI>();

            // ถ้าไม่มี TextMeshPro ให้ลองหา Text ธรรมดา
            if (targetText == null)
            {
                targetText = GetComponentInChildren<Text>();
            }
        }

        // ถ้าเจอ Text แล้ว ให้เก็บค่าสีเดิมไว้
        if (targetText != null)
        {
            originalColor = targetText.color;
            targetColor = originalColor;
        }
        else
        {
            Debug.LogWarning("หา Text ไม่เจอ! กรุณาลาก Text มาใส่ในช่อง Target Text");
        }
    }

    void Update()
    {
        // 1. เปลี่ยนขนาด (Scale ทั้งปุ่ม)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);

        // 2. เปลี่ยนสี (Color เฉพาะตัวหนังสือ)
        if (targetText != null)
        {
            targetText.color = Color.Lerp(targetText.color, targetColor, Time.deltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * scaleMultiplier;
        targetColor = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        targetColor = originalColor; // กลับไปใช้สีเดิม
    }
}