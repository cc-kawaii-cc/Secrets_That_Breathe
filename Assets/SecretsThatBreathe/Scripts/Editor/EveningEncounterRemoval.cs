using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // One-time cleanup for an already-open scene; never creates encounter objects.
    [InitializeOnLoad]
    public static class EveningEncounterRemoval
    {
        private static string Request => Path.GetFullPath(Path.Combine(Application.dataPath, "../Tools/EveningPoliceRemoval/cleanup-open-scene.request"));
        static EveningEncounterRemoval()
        {
            EditorApplication.delayCall += Clean;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Clean;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Clean;
            };
        }
        private static void Clean()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += Clean; return; }
            var scene = SceneManager.GetSceneByPath("Assets/MainScenes/From_PM/MainScene_2_Evening.unity");
            if (!scene.IsValid() || !scene.isLoaded) return;
            var canvas = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "SeatingCanvas");
            var obsolete = canvas != null ? canvas.transform.Find("PoliceEncounter") : null;
            if (obsolete != null && obsolete.GetComponents<MonoBehaviour>().Any(c => c == null || c.GetType().Name == "EveningPoliceDialogue"))
            {
                Undo.DestroyObjectImmediate(obsolete.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            File.WriteAllText(Path.ChangeExtension(Request, ".result.txt"), "Removed old encounter UI from the open scene; proximity range is retained.");
            File.Delete(Request);
        }
    }
}
