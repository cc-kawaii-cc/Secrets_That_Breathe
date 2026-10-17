using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class LookTextInteraction : MonoBehaviour
    {
        public PlayerSeatController player;
        public Camera playerCamera;
        public BoxCollider interactionZone;
        public BoxCollider lookTarget;
        [Header("Edit sentences on SeatingCanvas")]
        public TMP_Text promptText;
        public TMP_Text messageText;
        [Min(.1f)] public float messageDuration = 3f;
        public bool interactOnce = false;
        [HideInInspector] public int repeatSettingsRevision;
        public bool HasInteracted { get; private set; }
        private float remaining;

        private static void Panel(TMP_Text label, bool show)
        {
            if (label != null) label.transform.parent.gameObject.SetActive(show);
        }
        public bool CanInteract()
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0f || remaining > 0f || (interactOnce && HasInteracted)
                || player == null || !player.CanExplore || messageText == null) return false;
            if (playerCamera == null) playerCamera = player.playerCamera != null ? player.playerCamera : Camera.main;
            var controller = player.GetComponent<CharacterController>();
            Vector3 position = controller != null ? controller.transform.TransformPoint(controller.center) : player.transform.position;
            return InteractionLook.IsPointInBox(position, interactionZone) && InteractionLook.IsLookingAtBox(playerCamera, lookTarget);
        }
        private void Awake() => Hide();
        private void Update()
        {
            if (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                if (remaining <= 0f) Panel(messageText, false);
            }
            bool available = CanInteract();
            Panel(promptText, available);
            if (available && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryInteract();
        }
        public bool TryInteract()
        {
            if (!CanInteract()) return false;
            HasInteracted = true;
            Panel(promptText, false);
            Panel(messageText, true);
            remaining = Mathf.Max(.1f, messageDuration);
            return true;
        }
        private void Hide()
        {
            Panel(promptText, false);
            Panel(messageText, false);
            remaining = 0f;
        }
        private void OnDisable() => Hide();
    }
}
