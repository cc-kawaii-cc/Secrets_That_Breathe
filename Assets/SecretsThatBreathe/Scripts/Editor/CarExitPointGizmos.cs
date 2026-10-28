using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class CarExitPointGizmos
    {
        static CarExitPointGizmos()
        {
            SceneView.duringSceneGui += Draw;
            Selection.selectionChanged += SceneView.RepaintAll;
        }

        private static void Draw(SceneView view)
        {
            if (!view.drawGizmos || Event.current.type != EventType.Repaint) return;
            foreach (var ride in Object.FindObjectsByType<PorschePassengerRide>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Transform point = ride.exitPoint;
                if (point == null || !point.gameObject.scene.IsValid() || !point.gameObject.scene.isLoaded) continue;
                bool selected = Selection.Contains(point.gameObject) || Selection.Contains(ride.gameObject);
                var oldDepth = Handles.zTest;
                try
                {
                    Handles.zTest = CompareFunction.Always;
                    Vector3 foot = point.position;
                    Vector3 eye = foot + Vector3.up * 1.7f;
                    float length = selected ? 1.4f : .9f;
                    Quaternion look = Quaternion.Euler(Mathf.Clamp(Mathf.DeltaAngle(0f, point.eulerAngles.x), -90f, 90f), point.eulerAngles.y, 0f);
                    using (new Handles.DrawingScope(new Color(.25f, 1f, .35f, selected ? 1f : .65f)))
                    {
                        Handles.DrawWireDisc(foot, Vector3.up, .28f);
                        Handles.DrawLine(foot - Vector3.right * .35f, foot + Vector3.right * .35f);
                        Handles.DrawLine(foot - Vector3.forward * .35f, foot + Vector3.forward * .35f);
                        Handles.DrawDottedLine(foot, eye, 4f);
                    }
                    using (new Handles.DrawingScope(Color.cyan))
                    {
                        Handles.SphereHandleCap(0, eye, Quaternion.identity, .12f, EventType.Repaint);
                        Handles.ArrowHandleCap(0, eye, look, length, EventType.Repaint);
                    }
                    Handles.Label(foot + Vector3.up * .12f, "ExitPoint / FEET", EditorStyles.whiteBoldLabel);
                    if (selected)
                        Handles.Label(eye + Vector3.up * .2f, "LOOK DIRECTION — rotate Y / X", EditorStyles.whiteBoldLabel);
                }
                finally { Handles.zTest = oldDepth; }
            }
        }
    }
}
