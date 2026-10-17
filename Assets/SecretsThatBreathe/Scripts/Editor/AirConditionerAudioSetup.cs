using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class AirConditionerAudioSetup
    {
        private static bool queued;
        static AirConditionerAudioSetup()
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
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!item.name.Equals("Air conditioner (1)", System.StringComparison.OrdinalIgnoreCase)
                        && !item.name.Equals("Air conditioner (2)", System.StringComparison.OrdinalIgnoreCase)) continue;
                    var child = item.Find("AirConditionerAudio");
                    if (child != null && child.GetComponent<AudioSource>() != null) continue;
                    if (child == null)
                    {
                        var go = new GameObject("AirConditionerAudio");
                        Undo.RegisterCreatedObjectUndo(go, "Create air conditioner ambience");
                        go.transform.SetParent(item, false);
                        child = go.transform;
                        Bounds bounds = default;
                        bool found = false;
                        foreach (var renderer in item.GetComponentsInChildren<Renderer>(true))
                            if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
                        if (found) child.position = bounds.center;
                    }
                    var source = Undo.AddComponent<AudioSource>(child.gameObject);
                    source.playOnAwake = true;
                    source.loop = true;
                    source.spatialBlend = 1f;
                    source.volume = .3f;
                    source.dopplerLevel = 0f;
                    source.rolloffMode = AudioRolloffMode.Linear;
                    source.minDistance = 1f;
                    source.maxDistance = 12f;
                    source.priority = 180;
                    EditorUtility.SetDirty(source);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
    }
}
