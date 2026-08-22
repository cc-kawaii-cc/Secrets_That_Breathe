using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    public float interactRange = 3f;
    public LayerMask interactableLayer;

    [Header("Input (New Input System)")]
    public InputActionReference interactAction;

    [Header("UI")]
    [Tooltip("ป้าย prompt เช่น '[E] สำรวจ' (optional)")]
    public GameObject promptUI;

    private Camera _cam;
    // เผื่อ scene ที่ generate มาแล้วยังไม่ได้ผูก action asset — ยังกด E โต้ตอบได้
    private UnityEngine.InputSystem.InputAction _fallbackInteract;

    private bool InteractPressed()
    {
        if (interactAction != null && interactAction.action != null)
            return interactAction.action.WasPressedThisFrame();
        return _fallbackInteract != null && _fallbackInteract.WasPressedThisFrame();
    }

    private void Awake()
    {
        if (interactAction == null)
        {
            _fallbackInteract = new UnityEngine.InputSystem.InputAction(
                "Interact", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/e");
            _fallbackInteract.AddBinding("<Gamepad>/buttonSouth");
        }
    }

    private void Start()
    {
        _cam = ResolveCamera();
        if (promptUI) promptUI.SetActive(false);
    }

    private Camera ResolveCamera()
    {
        if (PlayerManager.Instance != null && PlayerManager.Instance.playerCamera != null)
            return PlayerManager.Instance.playerCamera;
        return Camera.main;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsState(GameState.Exploration))
        {
            if (promptUI) promptUI.SetActive(false);
            return;
        }

        if (_cam == null) { _cam = ResolveCamera(); if (_cam == null) return; }

        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red);

        // ยิงทะลุแล้วค่อยคัด: จุดโต้ตอบที่กดไปแล้วเป็น trigger ที่ยังลอยอยู่
        // ถ้าใช้ Raycast ธรรมดา มันจะบังจุดถัดไปที่อยู่หลังมันถาวร
        // (แว่นขยายในอู่หาไม่เจอเพราะเหตุนี้ — โต๊ะตรวจหลักฐานบังอยู่)
        var hits = Physics.RaycastAll(ray, interactRange, interactableLayer, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        StoryInteractable found = null;
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i].collider;
            if (col.transform.IsChildOf(transform)) continue;      // ตัวเราเอง

            var candidate = col.GetComponent<StoryInteractable>();
            if (candidate != null)
            {
                if (!candidate.hasInteracted) { found = candidate; break; }
                continue;                                          // กดไปแล้ว มองผ่านไปหาอันหลัง
            }
            if (!col.isTrigger) break;                             // ของทึบ = มองไม่ทะลุ
        }

        bool hovering = found != null;
        if (hovering && InteractPressed())
        {
            found.DoInteract();
            if (!string.IsNullOrEmpty(found.inspectText) && SubtitleManager.Instance != null)
                SubtitleManager.Instance.Show(found.inspectText);
        }
        if (promptUI) promptUI.SetActive(hovering);
    }

    private void OnEnable()
    {
        interactAction?.action.Enable();
        _fallbackInteract?.Enable();
    }

    private void OnDisable()
    {
        interactAction?.action.Disable();
        _fallbackInteract?.Disable();
    }
}