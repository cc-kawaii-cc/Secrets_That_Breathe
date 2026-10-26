using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class ExitObjectiveSetup
    {
        static ExitObjectiveSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Install;
                return;
            }
            var scene = SceneManager.GetSceneByPath("Assets/Tonpalm/Lawyer Room.unity");
            if (!scene.IsValid() || !scene.isLoaded) return;
            var roots = scene.GetRootGameObjects();
            var story = roots.SelectMany(g => g.GetComponentsInChildren<DrawerSearchScareSequence>(true)).FirstOrDefault();
            var search = roots.SelectMany(g => g.GetComponentsInChildren<DocumentSearchSequence>(true)).FirstOrDefault();
            var canvas = roots.SelectMany(g => g.GetComponentsInChildren<SeatPromptUI>(true)).FirstOrDefault();
            if (story == null || search == null || canvas == null || story.exitObjectiveText != null || search.returnToDeskMessage == null) return;
            EditorSceneManager.SaveScene(scene, "Tools/DialogueAudit/LawyerRoom-before-exit-objective.unity", true);
            var parent = SeatingCanvasGroups.Group(canvas, SeatingCanvasGroups.Gameplay);
            var existing = parent.Find("ExitObjective");
            var panel = existing != null ? existing.gameObject : Object.Instantiate(search.returnToDeskMessage.transform.parent.gameObject, parent);
            panel.name = "ExitObjective";
            var label = panel.GetComponentInChildren<TMP_Text>(true);
            if (existing == null) label.text = "\"ไปที่ประตูทางออก\"";
            panel.SetActive(false);
            story.exitObjectiveText = label;
            story.exitObjectiveDelay = 3f;
            EditorUtility.SetDirty(story);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("EXIT_OBJECTIVE_OK: saved yellow Canvas reminder, 3 seconds after standing up and restoring control.");
        }
    }
}
