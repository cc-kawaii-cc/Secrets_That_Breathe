using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // Remove the previously generated dot from an already-open scene after recompilation.
    [InitializeOnLoad]
    public static class SeatCrosshairCleanup
    {
        static SeatCrosshairCleanup()
        {
            EditorApplication.delayCall += RemoveOldDot;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += RemoveOldDot;
        }

        private static void RemoveOldDot()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var ui in root.GetComponentsInChildren<SeatPromptUI>(true))
                {
                    var dot = ui.transform.Find("CrosshairDot");
                    if (dot == null) continue;
                    Undo.DestroyObjectImmediate(dot.gameObject);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
    }
}
