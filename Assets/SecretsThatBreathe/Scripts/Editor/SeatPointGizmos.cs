using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe.Editor
{
    public static class SeatPointGizmos
    {
        // Draw on the selected anchor itself, without adding components to scene objects.
        [DrawGizmo(GizmoType.Selected)]
        private static void DrawSelectedPoint(Transform point, GizmoType gizmoType)
        {
            var chair = point.GetComponentInParent<ChairInteractable>();
            if (chair == null) return;
            bool isSeat = chair.seatPoint == point;
            bool isCamera = chair.seatedCameraPoint == point;
            if (!isSeat && !isCamera) return;

            Color previousColor = Gizmos.color;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = isSeat ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(point.position, isSeat ? 0.15f : 0.08f);
            Gizmos.DrawRay(point.position, point.forward * (isSeat ? 0.5f : 0.7f));
            Handles.Label(point.position + Vector3.up * 0.2f, isSeat ? "SeatPoint" : "SeatedCameraPoint");
            Gizmos.color = previousColor;
            Gizmos.matrix = previousMatrix;
        }
    }
}
