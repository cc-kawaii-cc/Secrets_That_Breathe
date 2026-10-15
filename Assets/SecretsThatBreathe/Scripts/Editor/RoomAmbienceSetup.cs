using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class RoomAmbienceSetup
    {
        static RoomAmbienceSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
            EditorApplication.projectChanged += () => EditorApplication.delayCall += Install;
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                    if (item.name == "RoomAmbience" && item.TryGetComponent<AudioSource>(out var existing))
                    {
                        if (AssetDatabase.GetAssetPath(existing.clip) == "Assets/xomxomski-ambient-empty-room-noise-sound-effect-429845.mp3")
                        {
                            var loop = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SecretsThatBreathe/Audio/Ambience/Room_Empty_SeamlessLoop.wav");
                            if (loop != null)
                            {
                                Undo.RecordObject(existing, "Use seamless room ambience");
                                existing.clip = loop;
                                existing.loop = true;
                                EditorUtility.SetDirty(existing);
                                PrefabUtility.RecordPrefabInstancePropertyModifications(existing);
                                EditorSceneManager.MarkSceneDirty(scene);
                            }
                        }
                        return;
                    }
            var ambience = new GameObject("RoomAmbience");
            SceneManager.MoveGameObjectToScene(ambience, scene);
            Undo.RegisterCreatedObjectUndo(ambience, "Create room ambience audio");
            var source = Undo.AddComponent<AudioSource>(ambience);
            source.playOnAwake = true;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = .25f;
            source.dopplerLevel = 0f;
            source.priority = 200;
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SecretsThatBreathe/Audio/Ambience/Room_Empty_SeamlessLoop.wav");
            EditorUtility.SetDirty(source);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
