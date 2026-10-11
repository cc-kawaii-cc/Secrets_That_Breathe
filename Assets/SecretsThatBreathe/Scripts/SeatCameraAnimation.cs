using UnityEngine;

namespace SecretsThatBreathe
{
    // Apply after normal camera updates. The controller also suspends CinemachineBrain.
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class SeatCameraAnimation : MonoBehaviour
    {
        [Min(0.05f)] public float duration = 1f;
        public AnimationCurve movement = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Range(0f, 20f)] public float dipDegrees = 7f;
        [Range(0f, 0.15f)] public float dipDistance = 0.035f;

        private Transform view;
        private Vector3 position;
        private Quaternion rotation;
        private bool ownsView;

        public void Begin(Transform cameraTransform, Vector3 startPosition, Quaternion startRotation)
        {
            view = cameraTransform;
            ownsView = true;
            SetPose(startPosition, startRotation);
        }

        public void Sample(Vector3 start, Quaternion startRotation, Transform destination, float progress)
        {
            float t = Mathf.Clamp01(progress);
            float eased = movement != null ? Mathf.Clamp01(movement.Evaluate(t)) : Mathf.SmoothStep(0f, 1f, t);
            float dip = Mathf.Sin(t * Mathf.PI);
            SetPose(Vector3.Lerp(start, destination.position, eased) - Vector3.up * (dipDistance * dip),
                Quaternion.Slerp(startRotation, destination.rotation, eased) * Quaternion.Euler(dipDegrees * dip, 0f, 0f));
        }

        public void SetPose(Vector3 nextPosition, Quaternion nextRotation)
        {
            position = nextPosition;
            rotation = nextRotation;
            Apply();
        }

        public void Release() { ownsView = false; view = null; }

        private void LateUpdate() { Apply(); }

        private void Apply()
        {
            if (ownsView && view != null) view.SetPositionAndRotation(position, rotation);
        }
    }
}
