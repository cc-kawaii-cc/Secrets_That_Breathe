using StarterAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class FlashlightAudioSetup
    {
        private static bool queued;
        static FlashlightAudioSetup()
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
                foreach (var flashlight in root.GetComponentsInChildren<FirstPersonFlashlight>(true))
                {
                    var serialized = new SerializedObject(flashlight);
                    var revision = serialized.FindProperty("sharedSwitchAudioRevision");
                    var reference = serialized.FindProperty("audioSource");
                    if (revision.intValue >= 1 && reference.objectReferenceValue != null) continue;
                    var previousOn = serialized.FindProperty("turnOnAudioSource").objectReferenceValue as AudioSource;
                    var previousOff = serialized.FindProperty("turnOffAudioSource").objectReferenceValue as AudioSource;
                    var previousShared = reference.objectReferenceValue as AudioSource;
                    var preferred = previousOn != null && previousOn.clip != null ? previousOn
                        : previousOff != null && previousOff.clip != null ? previousOff : previousShared;
                    var child = flashlight.transform.Find("FlashlightAudio");
                    if (child == null)
                    {
                        var go = new GameObject("FlashlightAudio");
                        Undo.RegisterCreatedObjectUndo(go, "Create shared flashlight switch audio");
                        go.transform.SetParent(flashlight.transform, false);
                        child = go.transform;
                    }
                    var source = child.GetComponent<AudioSource>();
                    if (source == null) source = Undo.AddComponent<AudioSource>(child.gameObject);
                    Undo.RecordObject(source, "Merge flashlight switch settings");
                    if (revision.intValue < 1)
                    {
                        CopySettings(preferred, source);
                        if (source.clip == null)
                        {
                            var clip = serialized.FindProperty("turnOnSound").objectReferenceValue as AudioClip;
                            if (clip == null) clip = serialized.FindProperty("turnOffSound").objectReferenceValue as AudioClip;
                            source.clip = clip;
                            source.volume *= Mathf.Clamp01(serialized.FindProperty("soundVolume").floatValue);
                        }
                    }
                    source.playOnAwake = false;
                    source.loop = false;
                    EditorUtility.SetDirty(source);
                    reference.objectReferenceValue = source;
                    serialized.FindProperty("turnOnAudioSource").objectReferenceValue = null;
                    serialized.FindProperty("turnOffAudioSource").objectReferenceValue = null;
                    revision.intValue = 1;
                    serialized.ApplyModifiedProperties();
                    RemoveOldSource(flashlight, "FlashlightOnAudio", source);
                    RemoveOldSource(flashlight, "FlashlightOffAudio", source);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(flashlight);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
        private static void CopySettings(AudioSource previous, AudioSource source)
        {
            if (previous == source) return;
            if (previous == null) { source.spatialBlend = 0f; source.dopplerLevel = 0f; return; }
            source.clip = previous.clip;
            source.volume = previous.volume;
            source.pitch = previous.pitch;
            source.panStereo = previous.panStereo;
            source.spatialBlend = previous.spatialBlend;
            source.dopplerLevel = previous.dopplerLevel;
            source.spread = previous.spread;
            source.minDistance = previous.minDistance;
            source.maxDistance = previous.maxDistance;
            source.reverbZoneMix = previous.reverbZoneMix;
            source.priority = previous.priority;
            source.mute = previous.mute;
            source.bypassEffects = previous.bypassEffects;
            source.bypassListenerEffects = previous.bypassListenerEffects;
            source.bypassReverbZones = previous.bypassReverbZones;
            source.outputAudioMixerGroup = previous.outputAudioMixerGroup;
            foreach (var curve in new[] { AudioSourceCurveType.CustomRolloff, AudioSourceCurveType.SpatialBlend, AudioSourceCurveType.ReverbZoneMix, AudioSourceCurveType.Spread })
            {
                var previousCurve = previous.GetCustomCurve(curve);
                if (previousCurve != null) source.SetCustomCurve(curve, previousCurve);
            }
            source.rolloffMode = previous.rolloffMode;
        }
        private static void RemoveOldSource(FirstPersonFlashlight flashlight, string name, AudioSource current)
        {
            var child = flashlight.transform.Find(name);
            if (child == null || child.gameObject == current.gameObject) return;
            // Remove only the simple audio objects created by the previous setup.
            if (child.childCount == 0 && child.GetComponent<AudioSource>() != null && child.GetComponents<Component>().Length == 2)
                Undo.DestroyObjectImmediate(child.gameObject);
        }
    }
}
