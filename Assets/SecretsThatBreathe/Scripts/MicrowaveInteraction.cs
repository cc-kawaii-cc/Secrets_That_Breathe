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
        [Range(1f, 80f)] public float lookAngle = 35f;
        [Min(0.1f)] public float messageDuration = 4f;
        public bool interactOnce = true;
        public bool HasInteracted { get; private set; }
        private Renderer[] meshes;
        private float remaining;

        private void Awake()
        {
            meshes = GetComponentsInChildren<Renderer>(true);
            Hide();
        }
        private void Update()
        {
            if (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                if (remaining <= 0f && messagePanel != null) messagePanel.SetActive(false);
            }
            if (playerCamera == null) playerCamera = Camera.main;
            bool available = Time.timeScale > 0f && remaining <= 0f && (!interactOnce || !HasInteracted)
                && (player == null || player.CanExplore)
                && IsPlayerInZone()
                && InteractionLook.IsLookingAt(playerCamera, meshes, lookAngle, float.MaxValue);
            if (available)
            {
                // Prevent inspection through solid walls, while ignoring the player's own colliders.
                var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                Bounds bounds = default;
                bool found = false;
                foreach (var mesh in meshes)
                    if (mesh != null && mesh.enabled && mesh.gameObject.activeInHierarchy)
                    {
                        if (!found) { bounds = mesh.bounds; found = true; }
                        else bounds.Encapsulate(mesh.bounds);
                    }
                float distance = Vector3.Distance(ray.origin, bounds.center);
                if (bounds.IntersectRay(ray, out float entry)) distance = entry;
                foreach (var hit in Physics.RaycastAll(ray, distance, ~0, QueryTriggerInteraction.Ignore))
                    if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(playerCamera.transform.root)
                        && !bounds.Contains(hit.point)) { available = false; break; }
            }
            if (hintPanel != null) hintPanel.SetActive(available);
            if (available && messagePanel != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                HasInteracted = true;
                if (hintPanel != null) hintPanel.SetActive(false);
                messagePanel.SetActive(true);
                remaining = messageDuration;
            }
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
