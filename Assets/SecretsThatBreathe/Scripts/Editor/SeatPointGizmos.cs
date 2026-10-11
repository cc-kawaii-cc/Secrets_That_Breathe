using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class SeatPointGizmos
    {
        static SeatPointGizmos()
        {
            SceneView.duringSceneGui += DrawSelectedPoints;
            Selection.selectionChanged += SceneView.RepaintAll;
        }

        // Draw directly in Scene view instead of relying on a Transform gizmo callback.
        private static void DrawSelectedPoints(SceneView view)
        {
            if (!view.drawGizmos || Event.current.type != EventType.Repaint) return;
            foreach (Transform point in Selection.transforms)
            {
                var chairs = point.GetComponentsInParent<ChairInteractable>(true);
                foreach (var chair in chairs)
                {
                    bool isSeat = chair.seatPoint == point;
                    bool isCamera = chair.seatedCameraPoint == point;
                    if (!isSeat && !isCamera) continue;
                    float radius = isSeat ? 0.15f : 0.08f;
                    var previousDepthTest = Handles.zTest;
                    try
                    {
                        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                        using (new Handles.DrawingScope(isSeat ? Color.green : Color.cyan, Matrix4x4.identity))
                        {
                            Handles.DrawWireDisc(point.position, Vector3.up, radius);
                            Handles.DrawWireDisc(point.position, Vector3.right, radius);
                            Handles.DrawWireDisc(point.position, Vector3.forward, radius);
                            Handles.ArrowHandleCap(0, point.position, point.rotation, isSeat ? 0.5f : 0.7f, EventType.Repaint);
                            Handles.Label(point.position + Vector3.up * 0.2f, isSeat ? "SeatPoint" : "SeatedCameraPoint");
                        }
                    }
                    finally { Handles.zTest = previousDepthTest; }
                    break;
                }
            }
        }
    }
}
