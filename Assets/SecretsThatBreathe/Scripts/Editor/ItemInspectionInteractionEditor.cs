using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe.Editor
{
    [CustomEditor(typeof(ItemInspectionInteraction))]
    [CanEditMultipleObjects]
    public sealed class ItemInspectionInteractionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            bool children = true;
            while (property.NextVisible(children))
            {
                children = false;
                if (property.name == "heldPosition") DrawPosition(property);
                else
                    using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
                        EditorGUILayout.PropertyField(property, true);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPosition(SerializedProperty position)
        {
            EditorGUILayout.LabelField("Held Position", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(position.FindPropertyRelative("x"), new GUIContent("X"));
            EditorGUILayout.PropertyField(position.FindPropertyRelative("y"), new GUIContent("Y"));
            var midpoint = serializedObject.FindProperty("startZoomAtMidpoint");
            if (!midpoint.hasMultipleDifferentValues && !midpoint.boolValue)
                EditorGUILayout.PropertyField(position.FindPropertyRelative("z"), new GUIContent("Z (Start Distance)"));
            else
                using (new EditorGUI.DisabledScope(true))
                {
                    var min = serializedObject.FindProperty("zoomMinDistance");
                    var max = serializedObject.FindProperty("zoomMaxDistance");
                    float near = Mathf.Max(.1f, min.floatValue);
                    float far = Mathf.Max(near + .01f, max.floatValue);
                    EditorGUI.showMixedValue = midpoint.hasMultipleDifferentValues || min.hasMultipleDifferentValues || max.hasMultipleDifferentValues;
                    EditorGUILayout.FloatField("Z (Automatic Midpoint)", (near + far) * .5f);
                    EditorGUI.showMixedValue = false;
                }
            EditorGUI.indentLevel--;
        }
    }
}
