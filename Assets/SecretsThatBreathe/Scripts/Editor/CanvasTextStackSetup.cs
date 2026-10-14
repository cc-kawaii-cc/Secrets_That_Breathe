using UnityEditor;
using UnityEditor.SceneManagement;
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
                if (canvas == null || canvas.GetComponent<CanvasTextStackLayout>() != null) continue;
                var layout = Undo.AddComponent<CanvasTextStackLayout>(canvas.gameObject);
                EditorUtility.SetDirty(layout);
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
