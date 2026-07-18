using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectInspector : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference lookAction; // ดึงค่าเมาส์ (ดึงตัวเดียวกับของ MouseLook ได้เลย)
    public InputActionReference cancelAction; // ปุ่มสำหรับออกจากการ Inspect (เช่น Esc หรือ คลิกขวา)

    [Header("References")]
    public Transform inspectPoint;
    public MonoBehaviour playerMoveScript;
    public MonoBehaviour mouseLookScript;

    private GameObject currentModel;
    private bool isInspecting = false;
    public float rotateSpeed = 50f; // *หมายเหตุ: New Input System ค่า Delta จะไวกว่าแบบเดิม อาจจะต้องลด rotateSpeed ลงมา

    void Update()
    {
        if (isInspecting && currentModel != null)
        {
            // ดึงค่าการขยับของเมาส์
            Vector2 mouseDelta = lookAction.action.ReadValue<Vector2>();
            float rotX = mouseDelta.x * rotateSpeed * Time.deltaTime;
            float rotY = mouseDelta.y * rotateSpeed * Time.deltaTime;

            currentModel.transform.Rotate(Vector3.up, -rotX, Space.World);
            currentModel.transform.Rotate(Vector3.right, rotY, Space.World);
            
            // ตรวจสอบปุ่มกดยกเลิก
            if (cancelAction.action.WasPressedThisFrame())
            {
                CloseInspection();
            }
        }
    }

    public void ShowItem(GameObject itemPrefab)
    {
        if (isInspecting) return;
        isInspecting = true;
        if(playerMoveScript != null) playerMoveScript.enabled = false;
        if(mouseLookScript != null) mouseLookScript.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        currentModel = Instantiate(itemPrefab, inspectPoint.position, inspectPoint.rotation);
        currentModel.transform.parent = inspectPoint;
    }

    public void CloseInspection()
    {
        isInspecting = false;
        if (currentModel != null) Destroy(currentModel);
        if(playerMoveScript != null) playerMoveScript.enabled = true;
        if(mouseLookScript != null) mouseLookScript.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable() 
    {
        lookAction?.action.Enable();
        cancelAction?.action.Enable();
    }
    private void OnDisable() 
    {
        lookAction?.action.Disable();
        cancelAction?.action.Disable();
    }
}