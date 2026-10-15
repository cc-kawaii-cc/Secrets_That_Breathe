using System.Collections;
using UnityEngine;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(16000)]
    public sealed class PhoneHandsetPickup : MonoBehaviour
    {
        public Transform handset;
        public Camera playerCamera;
        [Header("Animation")]
        [Tooltip("Disable to move the handset instantly when answering and hanging up.")]
        public bool enableAnimation = true;
        [Header("Handset centre relative to camera, in metres")]
        public Vector3 earPosition = new Vector3(.25f, -.07f, .22f);
        public Vector3 earRotation = new Vector3(0f, 0f, 90f);
        [Min(.01f)] public float pickupDuration = .5f;
        [Min(.01f)] public float putDownDuration = .45f;
        private Vector3 originalPosition, originalScale, localCentre;
        private Quaternion originalRotation;
        private Transform originalParent;
        private bool captured;
        private float progress;
        private Collider[] colliders;
        private bool[] colliderStates;

        public IEnumerator PickUp()
        {
            if (handset == null)
                foreach (var child in GetComponentsInChildren<Transform>(true))
                    if (child.name == "Cube.001") { handset = child; break; }
            if (handset == null)
            {
                Debug.LogWarning("[Phone] Assign Cube.001 to Phone Handset Pickup > Handset.", this);
                yield break;
            }
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogWarning("[Phone] No player camera for handset pickup.", this);
                yield break;
            }
            originalParent = handset.parent;
            originalPosition = handset.localPosition;
            originalRotation = handset.localRotation;
            originalScale = handset.localScale;
            var renderers = handset.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds(handset.position, Vector3.zero);
            bool found = false;
            foreach (var renderer in renderers)
            {
                if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
            }
            localCentre = handset.InverseTransformPoint(bounds.center);
            colliders = handset.GetComponentsInChildren<Collider>(true);
            colliderStates = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++) { colliderStates[i] = colliders[i].enabled; colliders[i].enabled = false; }
            captured = true;
            progress = 0f;
            if (enableAnimation) yield return Animate(1f, pickupDuration);
            else { progress = 1f; ApplyPose(); }
        }
        public IEnumerator PutDown()
        {
            if (!captured) yield break;
            if (enableAnimation) yield return Animate(0f, putDownDuration);
            Restore();
        }
        private IEnumerator Animate(float target, float duration)
        {
            float from = progress;
            float elapsed = 0f;
            duration = Mathf.Max(.01f, duration);
            while (elapsed < duration)
            {
                progress = Mathf.Lerp(from, target, elapsed / duration);
                ApplyPose();
                yield return null;
                elapsed += Time.deltaTime;
            }
            progress = target;
            ApplyPose();
        }
        private void LateUpdate() { if (captured) ApplyPose(); }
        private void ApplyPose()
        {
            if (handset == null || playerCamera == null) return;
            Vector3 basePosition = originalParent != null ? originalParent.TransformPoint(originalPosition) : originalPosition;
            Quaternion baseRotation = originalParent != null ? originalParent.rotation * originalRotation : originalRotation;
            Quaternion targetRotation = playerCamera.transform.rotation * Quaternion.Euler(earRotation);
            // Compensate for imported mesh pivots: the editable position controls the visible handset centre.
            Vector3 targetPosition = playerCamera.transform.TransformPoint(earPosition)
                - targetRotation * Vector3.Scale(localCentre, handset.lossyScale);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            handset.SetPositionAndRotation(Vector3.Lerp(basePosition, targetPosition, eased), Quaternion.Slerp(baseRotation, targetRotation, eased));
        }
        public void Restore()
        {
            if (!captured) return;
            if (handset != null)
            {
                handset.localPosition = originalPosition;
                handset.localRotation = originalRotation;
                handset.localScale = originalScale;
            }
            for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
            progress = 0f;
            captured = false;
        }
        private void OnDisable() => Restore();
    }
}
