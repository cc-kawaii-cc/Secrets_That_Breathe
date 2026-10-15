using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class NoodleEatingAudioSetup
    {
        private static bool queued;
        static NoodleEatingAudioSetup()
        {
            Queue();
            EditorApplication.hierarchyChanged += Queue;
            EditorSceneManager.sceneOpened += (scene, mode) => Queue();
        }
        private static void Queue()
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
            foreach (var root in scene.GetRootGameObjects())
                foreach (var eating in root.GetComponentsInChildren<NoodleEatingController>(true))
                {
                    if (eating.audioSource != null) continue;
                    var child = eating.transform.Find("NoodleEatingAudio");
                    if (child == null)
                    {
                        var go = new GameObject("NoodleEatingAudio");
                        Undo.RegisterCreatedObjectUndo(go, "Create noodle eating audio");
                        go.transform.SetParent(eating.transform, false);
                        child = go.transform;
                    }
                    var source = child.GetComponent<AudioSource>();
                    if (source == null)
                    {
                        source = Undo.AddComponent<AudioSource>(child.gameObject);
                        source.playOnAwake = false;
                        source.loop = false;
                        source.spatialBlend = 0f;
                        source.dopplerLevel = 0f;
                        source.volume = 1f;
                        EditorUtility.SetDirty(source);
                    }
                    Undo.RecordObject(eating, "Assign noodle eating audio source");
                    eating.audioSource = source;
                    EditorUtility.SetDirty(eating);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(eating);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
    }
}
