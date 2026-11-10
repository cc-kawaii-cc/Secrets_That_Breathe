using StarterAssets;
using UnityEngine;

namespace SecretsThatBreathe
{
    // Range sensor. The saved EveningPoliceEncounter consumes IsPlayerInRange.
    [DisallowMultipleComponent]
    public sealed class PoliceJumpscareRange : MonoBehaviour
    {
        [Header("Adjust the range in world metres")]
        [Min(.1f)] public float triggerRadius = 3f;
        [Tooltip("Total width of the detection sector in degrees.")]
        [Range(1f, 360f)] public float triggerAngle = 360f;
        [Tooltip("Rotate the sector horizontally relative to the police object's forward direction.")]
        [Range(-180f, 180f)] public float directionOffsetDegrees;
        [Tooltip("Centre relative to the police object's feet.")]
        public Vector3 localCenter;
        [Tooltip("Ignore a player on a different floor or far above/below the police.")]
        [Min(.1f)] public float maximumHeightDifference = 1.5f;
        [Header("Optional references — found in this scene if left empty")]
        public FirstPersonController player;
        public PorschePassengerRide arrivalRide;
        [SerializeField, Tooltip("Range status during Play; consumed by the encounter controller on SeatingCanvas.")]
        private bool playerInRange;
        public bool IsPlayerInRange => playerInRange;
        public Vector3 WorldCenter => transform.TransformPoint(localCenter);

        private void Start()
        {
            if (player == null)
                foreach (var candidate in FindObjectsByType<FirstPersonController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (candidate.gameObject.scene == gameObject.scene) { player = candidate; break; }
            if (arrivalRide == null)
                foreach (var candidate in FindObjectsByType<PorschePassengerRide>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (candidate.gameObject.scene == gameObject.scene && candidate.gameObject.name == "FD")
                    { arrivalRide = candidate; break; }
        }

        private void LateUpdate()
        {
            playerInRange = false;
            if (Time.timeScale <= 0f || player == null || !player.isActiveAndEnabled || player.MovementLocked) return;
            if (arrivalRide != null && !arrivalRide.HasExited) return;
            Vector3 delta = player.transform.position - WorldCenter;
            if (Mathf.Abs(delta.y) > maximumHeightDifference) return;
            delta.y = 0f;
            Vector3 forward = Quaternion.AngleAxis(directionOffsetDegrees, Vector3.up) * Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            playerInRange = delta.sqrMagnitude <= triggerRadius * triggerRadius &&
                (delta.sqrMagnitude < .000001f || triggerAngle >= 360f || Vector3.Angle(forward, delta) <= triggerAngle * .5f);
        }

        private void OnDisable() => playerInRange = false;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = playerInRange ? Color.green : new Color(1f, .65f, .1f, 1f);
            Vector3 center = WorldCenter + Vector3.up * .04f;
            float radius = Mathf.Max(.1f, triggerRadius);
            Vector3 forward = Quaternion.AngleAxis(directionOffsetDegrees, Vector3.up) * Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            float angle = Mathf.Clamp(triggerAngle, 1f, 360f);
            Vector3 first = center + Quaternion.AngleAxis(-angle * .5f, Vector3.up) * forward * radius;
            Vector3 previous = first;
            for (int i = 1; i <= 64; i++)
            {
                Vector3 point = center + Quaternion.AngleAxis(Mathf.Lerp(-angle * .5f, angle * .5f, i / 64f), Vector3.up) * forward * radius;
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
            if (angle < 360f) { Gizmos.DrawLine(center, first); Gizmos.DrawLine(center, previous); }
            Gizmos.DrawLine(center, center + forward * radius);
            Gizmos.DrawLine(center - Vector3.right * .15f, center + Vector3.right * .15f);
            Gizmos.DrawLine(center - Vector3.forward * .15f, center + Vector3.forward * .15f);
        }
    }
}
