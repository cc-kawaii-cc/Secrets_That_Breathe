using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SecretsThatBreathe.Editor
{
    [CustomEditor(typeof(WaterVendingMachineInteraction))]
    public sealed class WaterVendingMachineInteractionEditor : UnityEditor.Editor
    {
        readonly BoxBoundsHandle zoneHandle = new BoxBoundsHandle();
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var machine = (WaterVendingMachineInteraction)target;
            EditorGUILayout.HelpBox("Select the machine and drag the green box handles in Scene view. You can also select InteractionZone and use Box Collider > Edit Collider.", MessageType.Info);
            if (machine.messagePanel && GUILayout.Button("Select Message Text in Canvas"))
            {
                var text=machine.messagePanel.GetComponentInChildren<TMPro.TMP_Text>(true);
                Selection.activeGameObject=text ? text.gameObject : machine.messagePanel;
            }
        }
        void OnSceneGUI()
        {
            var machine=(WaterVendingMachineInteraction)target;
            var zone=machine.interactionZone;
            if (!zone) return;
            zoneHandle.handleColor = new Color(.25f,1,.4f);
            zoneHandle.wireframeColor = new Color(.25f,1,.4f);
            using (new Handles.DrawingScope(new Color(.25f,1,.4f),zone.transform.localToWorldMatrix))
            {
                zoneHandle.center=zone.center; zoneHandle.size=zone.size;
                EditorGUI.BeginChangeCheck();
                zoneHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(zone,"Resize water machine interaction area");
                    zone.center=zoneHandle.center; zone.size=zoneHandle.size;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
                    EditorUtility.SetDirty(zone);
                }
            }
            Handles.Label(zone.transform.TransformPoint(zone.center + Vector3.up * zone.size.y * .5f),"Water machine interaction area");
        }
    }
}
