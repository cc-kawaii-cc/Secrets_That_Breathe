using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class PhoneCallAudioSetup
    {
        private static bool queued;
        static PhoneCallAudioSetup()
        {
            QueueInstall();
            EditorSceneManager.sceneOpened += (scene, mode) => QueueInstall();
            EditorApplication.hierarchyChanged += QueueInstall;
            EditorApplication.projectChanged += QueueInstall;
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
            foreach (var root in scene.GetRootGameObjects())
                foreach (var call in root.GetComponentsInChildren<PhoneCallInteraction>(true))
                {
                    AssignButtonClips(call);
                    if (call.ringSource != null && call.effectSource != null) continue;
                    Undo.RecordObject(call, "Assign persistent phone audio sources");
                    if (call.ringSource == null) call.ringSource = Source(call, "PhoneRingAudio", true);
                    if (call.effectSource == null) call.effectSource = Source(call, "PhoneButtonAudio", false);
                    EditorUtility.SetDirty(call);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(call);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
        private static void AssignButtonClips(PhoneCallInteraction call)
        {
            var answer = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SecretsThatBreathe/Audio/Phone/Phone_PickUp.wav");
            var hangUp = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SecretsThatBreathe/Audio/Phone/Phone_HangUp_Reversed.wav");
            bool assignAnswer = call.answerSound == null && answer != null;
            bool assignHangUp = call.hangUpSound == null && hangUp != null;
            if (!assignAnswer && !assignHangUp) return;
            Undo.RecordObject(call, "Assign phone pickup and reversed hangup sounds");
            if (assignAnswer) call.answerSound = answer;
            if (assignHangUp) call.hangUpSound = hangUp;
            EditorUtility.SetDirty(call);
            PrefabUtility.RecordPrefabInstancePropertyModifications(call);
            EditorSceneManager.MarkSceneDirty(call.gameObject.scene);
        }
        private static AudioSource Source(PhoneCallInteraction call, string name, bool ringing)
        {
            var existing = call.transform.Find(name);
            if (existing != null && existing.TryGetComponent<AudioSource>(out var previous)) return previous;
            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create persistent " + name);
                go.transform.SetParent(call.transform, false);
            }
            var source = Undo.AddComponent<AudioSource>(go);
            source.playOnAwake = false;
            source.loop = ringing;
            source.spatialBlend = ringing ? 1f : 0f;
            source.volume = call.volume;
            source.dopplerLevel = 0f;
            if (ringing)
            {
                source.clip = call.ringSound;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1f;
                source.maxDistance = Mathf.Max(1f, call.ringAudibleDistance);
            }
            EditorUtility.SetDirty(source);
            return source;
        }
    }
}
