using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class CanvasTextStackSetup
    {
        static CanvasTextStackSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                var canvas = root.GetComponentInChildren<SeatPromptUI>(true);
                if (canvas == null) continue;
                if (canvas.GetComponent<CanvasTextStackLayout>() == null)
                {
                    var layout = Undo.AddComponent<CanvasTextStackLayout>(canvas.gameObject);
                    EditorUtility.SetDirty(layout);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                NormalizeTextPositions(canvas);
            }
        }
        private static void NormalizeTextPositions(SeatPromptUI canvas)
        {
            if (canvas.commonTextLayoutRevision >= 1) return;
            foreach (var label in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.GetComponentInParent<Button>(true) != null) continue;
                bool skip = false;
                for (var ancestor = label.transform; ancestor != null && ancestor != canvas.transform; ancestor = ancestor.parent)
                    if (ancestor.name.StartsWith("TestSkip") || ancestor.name.StartsWith("CallRow")
                        || ancestor.name == "Conversation" || ancestor.name == "CallBackLines" || ancestor.name == "SamudNoteCanvas" || ancestor.name == "RotateHint"
                        || (ancestor.name == "Lines" && ancestor.parent != null && ancestor.parent.name == "PhoneCallUI"))
                    { skip = true; break; }
                if (skip) continue;
                var panel = label.transform.parent as RectTransform;
                if (panel == null || panel == canvas.transform) continue;
                Undo.RecordObject(panel, "Align general text at the same Canvas position");
                SeatPromptUI.PositionCommonTextPanel(panel);
                EditorUtility.SetDirty(panel);
                PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
            }
            Undo.RecordObject(canvas, "Remember general text layout update");
            canvas.commonTextLayoutRevision = 1;
            EditorUtility.SetDirty(canvas);
            PrefabUtility.RecordPrefabInstancePropertyModifications(canvas);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        }
    }
}
