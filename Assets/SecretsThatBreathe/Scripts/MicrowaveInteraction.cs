using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class MicrowaveInteraction : MonoBehaviour
    {
        [Header("Text is edited on SeatingCanvas")]
        public GameObject hintPanel;
        public GameObject messagePanel;
        public Camera playerCamera;
        public PlayerSeatController player;
        [Header("Interaction area (resize the Box Collider in Scene view)")]
        public BoxCollider interactionZone;
        [Header("Crosshair target (resize this separate Box Collider)")]
        public BoxCollider lookTarget;
        [Min(0.1f)] public float messageDuration = 4f;
        public bool interactOnce = true;
        [HideInInspector] public int restoredInteractionSettingsVersion;
        [Header("Play Mode interaction status")]
        [SerializeField] private string interactionStatus;
        public bool HasInteracted { get; private set; }
        private float remaining;

        private void Awake()
        {
            Hide();
        }
        private void Update()
        {
            if (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                if (remaining <= 0f && messagePanel != null) messagePanel.SetActive(false);
            }
            if (playerCamera == null) playerCamera = player != null && player.playerCamera != null ? player.playerCamera : Camera.main;
            bool inZone = IsPlayerInZone();
            bool onTarget = IsCrosshairOnTarget();
            bool available = Time.timeScale > 0f && remaining <= 0f && (!interactOnce || !HasInteracted)
                && !DocumentSearchSequence.BlocksRoomInteractions(gameObject.scene)
                && (player == null || player.CanExplore)
                && inZone && onTarget;
            interactionStatus = playerCamera == null ? "No player camera"
                : interactOnce && HasInteracted ? "Already interacted"
                : remaining > 0f ? "Showing message"
                : player != null && !player.CanExplore ? "Player controls locked"
                : !inZone ? "Player outside InteractionZone"
                : !onTarget ? "Crosshair outside LookTarget"
                : "Ready: press E";
            if (hintPanel != null) hintPanel.SetActive(available);
            if (available && messagePanel != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                HasInteracted = true;
                if (hintPanel != null) hintPanel.SetActive(false);
                messagePanel.SetActive(true);
                remaining = messageDuration;
            }
        }
        private bool IsCrosshairOnTarget()
        {
            if (playerCamera == null || lookTarget == null || !lookTarget.enabled
                || !lookTarget.gameObject.activeInHierarchy) return false;
            // The centre of the camera viewport matches the centred crosshair. No angle/distance gate:
            // InteractionZone alone controls proximity, LookTarget alone controls aiming.
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            // Test only the configured box in its own coordinate space. Unrelated oversized
            // room/counter colliders cannot invalidate the target from particular view angles.
            var localRay = new Ray(lookTarget.transform.InverseTransformPoint(ray.origin),
                lookTarget.transform.InverseTransformVector(ray.direction));
            var box = new Bounds(lookTarget.center, lookTarget.size);
            return box.Contains(localRay.origin) || box.IntersectRay(localRay, out _);
        }
        private bool IsPlayerInZone()
        {
            if (playerCamera == null || interactionZone == null || !interactionZone.enabled
                || !interactionZone.gameObject.activeInHierarchy) return false;
            var body = player != null ? player.GetComponent<CharacterController>()
                : playerCamera.transform.root.GetComponentInChildren<CharacterController>();
            Vector3 position = body != null ? body.transform.TransformPoint(body.center) : playerCamera.transform.position;
            Vector3 offset = interactionZone.transform.InverseTransformPoint(position) - interactionZone.center;
            Vector3 half = interactionZone.size * 0.5f;
            return Mathf.Abs(offset.x) <= half.x && Mathf.Abs(offset.y) <= half.y && Mathf.Abs(offset.z) <= half.z;
        }
        private void Hide()
        {
            if (hintPanel != null) hintPanel.SetActive(false);
            if (messagePanel != null) messagePanel.SetActive(false);
            remaining = 0f;
        }
        private void OnDisable() => Hide();
    }
}
