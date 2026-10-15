using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class ThaiTextDisplaySetup
    {
        private static bool queued;
        static ThaiTextDisplaySetup()
        {
            QueueInstall();
            EditorSceneManager.sceneOpened += (scene, mode) => QueueInstall();
            EditorApplication.hierarchyChanged += QueueInstall;
        }
        private static void QueueInstall()
        {
            if (queued || EditorApplication.isPlayingOrWillChangePlaymode) return;
            queued = true;
            EditorApplication.delayCall += () => { queued = false; Install(); };
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                var canvas = root.GetComponentInChildren<SeatPromptUI>(true);
                if (canvas == null) continue;
                foreach (var label in canvas.GetComponentsInChildren<TMP_Text>(true))
                {
                    var display = label.GetComponent<ThaiTextDisplay>();
                    if (display != null) continue;
                    display = Undo.AddComponent<ThaiTextDisplay>(label.gameObject);
                    display.Bind();
                    EditorUtility.SetDirty(display);
                    EditorUtility.SetDirty(label);
                    changed = true;
                }
            }
            if (changed) EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
