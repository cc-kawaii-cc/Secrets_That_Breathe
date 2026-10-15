using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class AuntTurnAudioSetup
    {
        private static bool queued;
        static AuntTurnAudioSetup()
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
                foreach (var story in root.GetComponentsInChildren<DrawerSearchScareSequence>(true))
                {
                    if (story.scareAudioSource != null && story.auntTurnAudioRevision >= 1) continue;
                    Undo.RecordObject(story, "Assign editable aunt turn audio");
                    if (story.scareAudioSource == null)
                    {
                        var child = story.transform.Find("AuntTurnAudio");
                        if (child == null)
                        {
                            var go = new GameObject("AuntTurnAudio");
                            Undo.RegisterCreatedObjectUndo(go, "Create aunt turn audio");
                            go.transform.SetParent(story.transform, false);
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
                        }
                        story.scareAudioSource = source;
                    }
                    if (story.auntTurnAudioRevision < 1)
                    {
                        var source = story.scareAudioSource;
                        Undo.RecordObject(source, "Move aunt sound and volume into Audio Source");
                        if (source.clip == null) source.clip = story.scareSound;
                        source.volume *= Mathf.Clamp01(story.scareVolume);
                        EditorUtility.SetDirty(source);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(source);
                        story.auntTurnAudioRevision = 1;
                    }
                    EditorUtility.SetDirty(story);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(story);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
    }
}
