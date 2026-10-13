using UnityEngine;

namespace SecretsThatBreathe
{
    // Apply after normal camera updates. The controller also suspends CinemachineBrain.
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class SeatCameraAnimation : MonoBehaviour
    {
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
