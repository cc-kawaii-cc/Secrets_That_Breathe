using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class EmptyNoodleCupSetup
    {
        static EmptyNoodleCupSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            GameObject empty = null;
            NoodleEatingController eating = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (eating == null) eating = root.GetComponentInChildren<NoodleEatingController>(true);
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                    if (item.name == "MEMEEE_Empty") empty = item.gameObject;
            }
            if (empty == null || eating == null || eating.emptyCupModel == empty) return;
            Undo.RecordObject(eating, "Use MEMEEE_Empty after eating");
            var oldEmpty = eating.emptyCupModel;
            eating.emptyCupModel = empty;
            Undo.RecordObject(empty, "Hide empty cup until eating finishes");
            empty.SetActive(false);
            if (oldEmpty != null && oldEmpty != empty)
            {
                Undo.RecordObject(oldEmpty, "Hide previous empty cup model");
                oldEmpty.SetActive(false);
                EditorUtility.SetDirty(oldEmpty);
                PrefabUtility.RecordPrefabInstancePropertyModifications(oldEmpty);
            }
            EditorUtility.SetDirty(empty);
            EditorUtility.SetDirty(eating);
            PrefabUtility.RecordPrefabInstancePropertyModifications(empty);
            PrefabUtility.RecordPrefabInstancePropertyModifications(eating);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
