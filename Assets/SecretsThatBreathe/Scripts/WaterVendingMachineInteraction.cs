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
        public Transform playerBody;
        public Camera playerCamera;
        [Min(.1f)] public float messageDuration = 6f;

        public bool HasInteracted { get; private set; }
        Renderer[] meshes;
        float messageTime;

        void Awake()
        {
            meshes = GetComponentsInChildren<Renderer>(true);
            if (hintPanel) hintPanel.SetActive(false);
            if (messagePanel) messagePanel.SetActive(false);
        }
        void Update()
        {
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
            if (HasInteracted || !isActiveAndEnabled || Time.timeScale <= 0) return false;
            if (!playerCamera) playerCamera = Camera.main;
            if (!playerCamera || playerCamera.gameObject.scene != gameObject.scene) return false;
            if (!playerBody)
            {
                var controller = playerCamera.transform.root.GetComponentInChildren<CharacterController>();
                playerBody = controller ? controller.transform : playerCamera.transform;
            }
            if (!IsPlayerInZone()) return false;
            Bounds bounds = default;
            bool found = false;
            foreach (var mesh in meshes)
            {
                if (!mesh || !mesh.enabled || !mesh.gameObject.activeInHierarchy) continue;
                if (!found) { bounds = mesh.bounds; found = true; }
                else bounds.Encapsulate(mesh.bounds);
            }
            if (!found) return false;
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!bounds.IntersectRay(ray, out float distance)) return false;
            // The imported machine has no collider. Use its visible bounds as the target,
            // but reject solid objects in front so E cannot inspect through a wall.
            foreach (var hit in Physics.RaycastAll(ray, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(playerCamera.transform.root)) continue;
                if (bounds.Contains(hit.point + ray.direction * .02f)) continue;
                return false;
            }
            return true;
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
        void OnDisable()
        {
            if (hintPanel) hintPanel.SetActive(false);
            if (messagePanel) messagePanel.SetActive(false);
            messageTime = 0;
        }
    }
}
