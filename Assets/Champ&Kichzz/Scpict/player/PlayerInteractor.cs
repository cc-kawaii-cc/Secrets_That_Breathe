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

        bool hovering = false;
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            var interactable = hit.collider.GetComponent<StoryInteractable>();
            if (interactable != null && !interactable.hasInteracted)
            {
                hovering = true;
                if (interactAction != null && interactAction.action.WasPressedThisFrame())
                {
                    interactable.DoInteract();
                    if (!string.IsNullOrEmpty(interactable.inspectText))
                        SubtitleManager.Instance.Show(interactable.inspectText);
                }
            }
        }
        if (promptUI) promptUI.SetActive(hovering);
    }

    private void OnEnable()  => interactAction?.action.Enable();
    private void OnDisable() => interactAction?.action.Disable();
}