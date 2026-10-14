using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class MiddleRoomThoughtSetup
    {
        static MiddleRoomThoughtSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        }

        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var trigger in root.GetComponentsInChildren<MiddleRoomThoughtTrigger>(true))
                    if (trigger.name == "MiddleRoomThoughtTrigger" || trigger.name == "FindDeskThoughtTrigger")
                    {
                        Undo.DestroyObjectImmediate(trigger.gameObject);
                        changed = true;
                    }
            var canvas = SeatPromptUI.FindOrCreate(scene);
            foreach (var name in new[] { "MiddleRoomThought", "FindDeskThought" })
            {
                var oldPanel = SeatingCanvasGroups.FindPanel(canvas, name);
                if (oldPanel == null) continue;
                Undo.DestroyObjectImmediate(oldPanel.gameObject);
                changed = true;
            }
            var existing = canvas.GetComponentInChildren<OfficeIntroDialogue>(true);
            if (existing == null || existing.lines == null || existing.lines.Length != 5 || existing.testSkipHint == null)
            {
                var intro = OfficeIntroDialogue.Install(canvas);
                EditorUtility.SetDirty(intro);
                changed = true;
            }
            var introDialogue = canvas.GetComponentInChildren<OfficeIntroDialogue>(true);
            bool hasIntroTrigger = false;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<OfficeIntroTrigger>(true) != null) hasIntroTrigger = true;
            if (!hasIntroTrigger)
            {
                var trigger = OfficeIntroTrigger.Install(introDialogue);
                Undo.RegisterCreatedObjectUndo(trigger.gameObject, "Create intro dialogue trigger");
                EditorUtility.SetDirty(trigger);
                changed = true;
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var chair in root.GetComponentsInChildren<ChairInteractable>(true))
                    if (chair.unlockTrigger != null)
                    {
                        Undo.RecordObject(chair, "Replace chair trigger gate with intro dialogue");
                        chair.unlockTrigger = null;
                        EditorUtility.SetDirty(chair);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(chair);
                        changed = true;
                    }
            if (changed) EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
