using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class WaterVendingMachineInteraction : MonoBehaviour
    {
        [Header("Scene Canvas (edit text and fonts on its TMP components)")]
        public GameObject hintPanel;
        public GameObject messagePanel;
        [Header("Interaction area (drag its green handles in Scene view)")]
        public BoxCollider interactionZone;
        [Header("Crosshair target (resize this separate Box Collider)")]
        public BoxCollider lookTarget;
        public Transform playerBody;
        public Camera playerCamera;
        [Min(.1f)] public float messageDuration = 6f;

        public bool HasInteracted { get; private set; }
        public bool InteractionLocked { get; private set; }
        [Header("Play Mode interaction status")]
        [SerializeField] private string interactionStatus;
        float messageTime;

        void Awake()
        {
            if (hintPanel) hintPanel.SetActive(false);
            if (messagePanel) messagePanel.SetActive(false);
        }
        void Update()
        {
            if (InteractionLocked) return;
            if (messageTime > 0)
            {
                messageTime -= Time.deltaTime;
                if (messageTime <= 0 && messagePanel) messagePanel.SetActive(false);
            }
            bool available = CanInteract();
            if (hintPanel) hintPanel.SetActive(available);
            if (available && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                TryInteract();
        }
        public bool CanInteract()
        {
            if (DocumentSearchSequence.BlocksRoomInteractions(gameObject.scene)) { interactionStatus = "Document search in progress"; return false; }
            if (PhoneCallInteraction.IsBusy(gameObject.scene)) { interactionStatus = "Player controls locked"; return false; }
            if (InteractionLocked) { interactionStatus = "Interaction locked after aunt dialogue"; return false; }
            if (HasInteracted) { interactionStatus = "Already interacted"; return false; }
            if (!isActiveAndEnabled || Time.timeScale <= 0) { interactionStatus = "Disabled or paused"; return false; }
            if (!playerCamera) playerCamera = Camera.main;
            if (!playerCamera || playerCamera.gameObject.scene != gameObject.scene) { interactionStatus = "No player camera"; return false; }
            if (!playerBody)
            {
                var controller = playerCamera.transform.root.GetComponentInChildren<CharacterController>();
                playerBody = controller ? controller.transform : playerCamera.transform;
            }
            if (!IsPlayerInZone()) { interactionStatus = "Player outside InteractionZone"; return false; }
            if (!IsCrosshairOnTarget()) { interactionStatus = "Crosshair outside LookTarget"; return false; }
            interactionStatus = "Ready: press E";
            return true;
        }
        private bool IsCrosshairOnTarget()
        {
            if (!lookTarget || !lookTarget.enabled || !lookTarget.gameObject.activeInHierarchy) return false;
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            var localRay = new Ray(lookTarget.transform.InverseTransformPoint(ray.origin),
                lookTarget.transform.InverseTransformVector(ray.direction));
            var box = new Bounds(lookTarget.center, lookTarget.size);
            return box.Contains(localRay.origin) || box.IntersectRay(localRay, out _);
        }
        public bool IsPlayerInZone()
        {
            if (!playerBody || !interactionZone || !interactionZone.enabled || !interactionZone.gameObject.activeInHierarchy) return false;
            Vector3 offset = interactionZone.transform.InverseTransformPoint(playerBody.position) - interactionZone.center;
            Vector3 half = interactionZone.size * .5f;
            return Mathf.Abs(offset.x) <= half.x && Mathf.Abs(offset.y) <= half.y && Mathf.Abs(offset.z) <= half.z;
        }
        public bool TryInteract()
        {
            if (!CanInteract()) return false;
            if (!messagePanel) return false;
            HasInteracted = true;
            if (hintPanel) hintPanel.SetActive(false);
            messagePanel.SetActive(true);
            messageTime = messageDuration;
            return true;
        }
        public void LockInteraction()
        {
            InteractionLocked = true;
            if (hintPanel) hintPanel.SetActive(false);
            if (messagePanel) messagePanel.SetActive(false);
            messageTime = 0;
        }
        void OnDisable()
        {
            if (hintPanel) hintPanel.SetActive(false);
            if (messagePanel) messagePanel.SetActive(false);
            messageTime = 0;
        }
    }
}
