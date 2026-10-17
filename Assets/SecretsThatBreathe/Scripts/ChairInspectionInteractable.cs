using UnityEngine;

namespace SecretsThatBreathe
{
    public sealed class ChairInspectionInteractable : MonoBehaviour
    {
        public ChairInteractable referenceChair;
        public BoxCollider interactionZone;
        public BoxCollider lookTarget;
        [HideInInspector] public int lookTargetFitVersion;
        [Tooltip("Drag this chair's TMP message from SeatingCanvas here. Edit the sentence on that Canvas text object.")]
        public TMPro.TMP_Text messageText;
        [SerializeField, HideInInspector] private int messageAssignmentVersion;
        public bool NeedsMessageAssignment => messageAssignmentVersion < 1 || messageText == null;

        public void AssignInitialMessage(TMPro.TMP_Text text)
        {
            if (!NeedsMessageAssignment) return;
            messageText = text;
            messageAssignmentVersion = 1;
        }
        public bool HasInteracted { get; private set; }
        public void ResetForTesting() => HasInteracted = false;

        private void Awake()
        {
            if (messageText != null) messageText.transform.parent.gameObject.SetActive(false);
        }

        public bool CanInspect(PlayerSeatController player)
        {
            if (HasInteracted || messageText == null || !isActiveAndEnabled || player == null || !player.CanSit || !player.playerBody || Time.timeScale <= 0f ||
                referenceChair == null || !referenceChair.IsUnlocked || interactionZone == null ||
                !interactionZone.enabled || !interactionZone.gameObject.activeInHierarchy) return false;
            Vector3 p = interactionZone.transform.InverseTransformPoint(player.playerBody.position) - interactionZone.center;
            Vector3 half = interactionZone.size * 0.5f;
            if (Mathf.Abs(p.x) > half.x || Mathf.Abs(p.y) > half.y || Mathf.Abs(p.z) > half.z) return false;
            return InteractionLook.IsLookingAtBox(player.playerCamera, lookTarget);
        }

        public bool TryInspect(PlayerSeatController player, ChairInspectionUI ui)
        {
            if (!CanInspect(player) || ui == null || !ui.ShowMessage(messageText)) return false;
            HasInteracted = true;
            DocumentSearchSequence.NotifyChairInspection(gameObject.scene, messageText);
            return true;
        }

        public float DistanceSquared(Vector3 point)
        {
            return ((lookTarget != null ? lookTarget.transform.TransformPoint(lookTarget.center) : transform.position) - point).sqrMagnitude;
        }

        public void CreateZoneFromReference()
        {
            if (interactionZone != null || referenceChair == null || referenceChair.interactionZone == null) return;
            var source = referenceChair.interactionZone;
            var sourceMeshes = referenceChair.GetComponentsInChildren<Renderer>(true);
            var targetMeshes = GetComponentsInChildren<Renderer>(true);
            if (sourceMeshes.Length == 0 || targetMeshes.Length == 0) return;
            Bounds sourceBounds = sourceMeshes[0].bounds, targetBounds = targetMeshes[0].bounds;
            foreach (var mesh in sourceMeshes) sourceBounds.Encapsulate(mesh.bounds);
            foreach (var mesh in targetMeshes) targetBounds.Encapsulate(mesh.bounds);
            Quaternion delta = transform.rotation * Quaternion.Inverse(referenceChair.transform.rotation);
            var zone = new GameObject("InteractionZone").transform;
            zone.SetParent(transform, false);
            zone.SetPositionAndRotation(targetBounds.center + delta * (source.transform.TransformPoint(source.center) - sourceBounds.center),
                delta * source.transform.rotation);
            Vector3 scale = transform.lossyScale;
            zone.localScale = new Vector3(1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)), 1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)), 1f / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
            interactionZone = zone.gameObject.AddComponent<BoxCollider>();
            interactionZone.isTrigger = true;
            Vector3 sourceScale = source.transform.lossyScale;
            interactionZone.size = Vector3.Scale(source.size, new Vector3(Mathf.Abs(sourceScale.x), Mathf.Abs(sourceScale.y), Mathf.Abs(sourceScale.z)));
        }
    }
}
