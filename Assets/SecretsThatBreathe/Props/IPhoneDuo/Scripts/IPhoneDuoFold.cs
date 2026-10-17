using UnityEngine;

namespace SecretsThatBreathe.IPhoneDuo
{
    /// <summary>Folds the right leaf while retaining the FBX hinge's imported rest rotation.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class IPhoneDuoFold : MonoBehaviour
    {
        [SerializeField] private Transform hinge;
        [SerializeField] private Vector3 localFoldAxis = Vector3.up;
        [SerializeField] private Quaternion openLocalRotation = Quaternion.identity;
        [SerializeField, Range(0f, 180f)] private float foldAngle;

        public float FoldAngle => foldAngle;
        public Transform Hinge => hinge;
        public Vector3 LocalFoldAxis => localFoldAxis;

        public void Configure(Transform hingeTransform, Vector3 hingeLocalAxis)
        {
            hinge = hingeTransform;
            localFoldAxis = hingeLocalAxis.sqrMagnitude > .0001f ? hingeLocalAxis.normalized : Vector3.up;
            openLocalRotation = hinge ? hinge.localRotation : Quaternion.identity;
            foldAngle = 0f;
            ApplyFold();
        }

        public void SetFoldAngle(float angle)
        {
            foldAngle = Mathf.Clamp(angle, 0f, 180f);
            ApplyFold();
        }

        [ContextMenu("Fold/Open flat")]
        public void Open() => SetFoldAngle(0f);

        [ContextMenu("Fold/Half open")]
        public void HalfOpen() => SetFoldAngle(90f);

        [ContextMenu("Fold/Close")]
        public void Close() => SetFoldAngle(180f);

        private void OnEnable() => ApplyFold();
        private void OnValidate() => ApplyFold();

        private void ApplyFold()
        {
            foldAngle = Mathf.Clamp(foldAngle, 0f, 180f);
            if (!hinge) return;
            Vector3 axis = localFoldAxis.sqrMagnitude > .0001f ? localFoldAxis.normalized : Vector3.up;
            hinge.localRotation = openLocalRotation * Quaternion.AngleAxis(-foldAngle, axis);
        }
    }
}
