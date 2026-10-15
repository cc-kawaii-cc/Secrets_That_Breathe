using UnityEngine;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class ChairInteractable : MonoBehaviour
    {
        [Tooltip("Player capsule position at floor level, facing the desk.")]
        public Transform seatPoint;
        [Tooltip("Final camera position and direction at seated eye level.")]
        public Transform seatedCameraPoint;
        [Header("Prompt area")]
        [Tooltip("Player body must be inside this box to show the prompt and allow sitting. Adjust it with Edit Collider.")]
        public BoxCollider interactionZone;
        [Tooltip("When assigned, this trigger's message must finish displaying before sitting or inspecting nearby chairs.")]
        public MiddleRoomThoughtTrigger unlockTrigger;
        public bool IsUnlocked => unlockTrigger == null || unlockTrigger.HasFinishedShowing;
        [Header("Look at chair to sit")]
        public BoxCollider lookTarget;
        [HideInInspector] public int lookTargetFitVersion;
        [Tooltip("Initial eye height used only when creating missing seat points. Adjust SeatedCameraPoint to change an existing seat.")]
        [Min(0.3f)] public float defaultEyeHeight = 1.1f;

        public bool CanSit => isActiveAndEnabled && IsUnlocked && seatPoint != null && seatedCameraPoint != null;

        public bool TryInteract(PlayerSeatController player)
        {
            return CanSit && player != null && IsPlayerInZone(player.playerBody) && IsLookingAtChair(player.playerCamera) && player.TrySit(this);
        }

        public bool IsLookingAtChair(Camera view)
        {
            return InteractionLook.IsLookingAtBox(view, lookTarget);
        }


        public bool IsPlayerInZone(Transform playerBody)
        {
            if (playerBody == null) return false;
            if (interactionZone == null) return false;
            if (!interactionZone.enabled || !interactionZone.gameObject.activeInHierarchy) return false;
            Vector3 offset = interactionZone.transform.InverseTransformPoint(playerBody.position) - interactionZone.center;
            Vector3 half = interactionZone.size * 0.5f;
            return Mathf.Abs(offset.x) <= half.x && Mathf.Abs(offset.y) <= half.y && Mathf.Abs(offset.z) <= half.z;
        }

        public void CreateDefaultInteractionZone()
        {
            if (interactionZone != null || seatPoint == null) return;
            Transform existing = transform.Find("InteractionZone");
            if (existing != null)
            {
                interactionZone = existing.GetComponent<BoxCollider>();
                if (interactionZone != null) return;
            }
            Transform zone = existing != null ? existing : new GameObject("InteractionZone").transform;
            zone.SetParent(transform, false);
            zone.SetPositionAndRotation(seatPoint.position + Vector3.up, seatPoint.rotation);
            Vector3 parentScale = transform.lossyScale;
            zone.localScale = new Vector3(1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.z)));
            interactionZone = zone.gameObject.AddComponent<BoxCollider>();
            interactionZone.isTrigger = true;
            interactionZone.size = new Vector3(2f, 2.2f, 2f);
        }

        // Fallback for the existing Lawyer Room scene; editor-created anchors take priority.
        public void CreateDefaultPoints(Transform deskTarget)
        {
            if (seatPoint != null && seatedCameraPoint != null) return;
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Debug.LogWarning("Chair has no renderer; assign seat points manually.", this); return; }
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Vector3 targetPosition = bounds.center + transform.forward;
            if (deskTarget != null)
            {
                Renderer targetRenderer = deskTarget.GetComponentInChildren<Renderer>();
                targetPosition = targetRenderer != null ? targetRenderer.bounds.center : deskTarget.position;
            }
            Vector3 forward = targetPosition - bounds.center;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Quaternion facing = Quaternion.LookRotation(forward.normalized);
            float floorY = bounds.min.y;
            Physics.SyncTransforms();
            var hits = Physics.RaycastAll(new Vector3(bounds.center.x, bounds.min.y + 0.05f, bounds.center.z),
                Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform) || hit.normal.y < 0.7f || hit.collider is CharacterController) continue;
                floorY = hit.point.y;
                break;
            }
            Vector3 position = new Vector3(bounds.center.x, floorY, bounds.center.z) + forward.normalized * 0.08f;
            if (seatPoint == null) seatPoint = MakePoint("SeatPoint", position, facing);
            if (seatedCameraPoint == null) seatedCameraPoint = MakePoint("SeatedCameraPoint", position + Vector3.up * defaultEyeHeight, facing);
        }

        private Transform MakePoint(string pointName, Vector3 position, Quaternion rotation)
        {
            Transform point = transform.Find(pointName);
            if (point != null) return point;
            point = new GameObject(pointName).transform;
            point.SetParent(transform, false);
            point.SetPositionAndRotation(position, rotation);
            return point;
        }

        private void OnDrawGizmosSelected()
        {
            if (interactionZone != null)
            {
                Matrix4x4 previousMatrix = Gizmos.matrix;
                Gizmos.matrix = interactionZone.transform.localToWorldMatrix;
                Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.15f);
                Gizmos.DrawCube(interactionZone.center, interactionZone.size);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(interactionZone.center, interactionZone.size);
                Gizmos.matrix = previousMatrix;
            }
            if (seatPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(seatPoint.position, 0.15f);
                Gizmos.DrawRay(seatPoint.position, seatPoint.forward * 0.5f);
            }
            if (seatedCameraPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(seatedCameraPoint.position, 0.08f);
                Gizmos.DrawRay(seatedCameraPoint.position, seatedCameraPoint.forward * 0.7f);
            }
        }
    }
}
