using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerSeatController))]
    public sealed class SeatInteractor : MonoBehaviour
    {
        [Header("Canvas UI")]
        [Tooltip("Canvas prompt. Edit its Text (TMP) content to change the sentence.")]
        public SeatPromptUI promptCanvas;
        [Tooltip("Optional custom prompt object. Used when Prompt Canvas is not assigned.")]
        public GameObject promptUI;
        private PlayerSeatController player;
        private ChairInteractable focused;
        private ChairInteractable[] chairs;
        private InputAction interact;

        private void Awake()
        {
            player = GetComponent<PlayerSeatController>();
            interact = new InputAction("Sit", InputActionType.Button, "<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonSouth");
        }

        private void Start()
        {
            chairs = FindObjectsByType<ChairInteractable>(FindObjectsSortMode.None);
            if (promptCanvas == null && promptUI == null)
                promptCanvas = SeatPromptUI.FindOrCreate(gameObject.scene);
            SetPrompt(false);
        }

        private void OnEnable() { interact?.Enable(); }
        private void OnDisable() { interact?.Disable(); focused = null; SetPrompt(false); }
        private void OnDestroy() { interact?.Dispose(); }

        private void Update()
        {
            focused = null;
            if (player.CanSit && player.playerBody != null && Time.timeScale > 0f)
            {
                float nearestDistance = float.PositiveInfinity;
                foreach (ChairInteractable candidate in chairs)
                {
                    if (candidate == null || !candidate.CanSit || candidate.interactionZone == null ||
                        candidate.gameObject.scene != gameObject.scene || !candidate.IsPlayerInZone(player.playerBody) ||
                        !candidate.IsLookingAtChair(player.playerCamera)) continue;
                    float distance = (candidate.seatPoint.position - player.playerBody.position).sqrMagnitude;
                    if (distance < nearestDistance) { focused = candidate; nearestDistance = distance; }
                }
                if (focused != null && interact.WasPressedThisFrame() && focused.TryInteract(player)) focused = null;
            }
            SetPrompt(focused != null);
        }

        private void SetPrompt(bool visible)
        {
            if (promptCanvas != null) promptCanvas.SetVisible(visible);
            else if (promptUI != null && promptUI.activeSelf != visible) promptUI.SetActive(visible);
        }
    }
}
