using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class HorrorLightFlickerSetup
    {
        static HorrorLightFlickerSetup()
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
                foreach (var target in root.GetComponentsInChildren<Transform>(true))
                {
                    if (target.name != "LouveredLight_T8_2x36W (1)") continue;
                    var flicker = target.GetComponent<HorrorLightFlicker>();
                    bool created = flicker == null;
                    if (created) flicker = Undo.AddComponent<HorrorLightFlicker>(target.gameObject);
                    EnsureAudioSource(flicker);
                    if (!created && flicker.offMaterial != null) continue;
                    Undo.RecordObject(flicker, "Set dark tube material");
                    flicker.offMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SecretsThatBreathe/Props/LouveredLight/Materials/Tube_Off.mat");
                    flicker.dimBrightness = 0f;
                    if (created) flicker.targetLights = target.GetComponentsInChildren<Light>(true);
                    if (created) foreach (var light in flicker.targetLights)
                    {
                        Undo.RecordObject(light, "Use realtime flickering light");
                        light.lightmapBakeType = LightmapBakeType.Realtime;
                        EditorUtility.SetDirty(light);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(light);
                    }
                    EditorUtility.SetDirty(flicker);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(flicker);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
        private static void EnsureAudioSource(HorrorLightFlicker flicker)
        {
            if (flicker.flickerAudioSource != null) return;
            var child = flicker.transform.Find("FlickerAudio");
            if (child == null)
            {
                var go = new GameObject("FlickerAudio");
                Undo.RegisterCreatedObjectUndo(go, "Create flickering light audio");
                go.transform.SetParent(flicker.transform, false);
                child = go.transform;
            }
            var source = child.GetComponent<AudioSource>();
            if (source == null)
            {
                source = Undo.AddComponent<AudioSource>(child.gameObject);
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 1f;
                source.volume = .5f;
                source.dopplerLevel = 0f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1f;
                source.maxDistance = 12f;
                EditorUtility.SetDirty(source);
            }
            Undo.RecordObject(flicker, "Assign flickering light audio");
            flicker.flickerAudioSource = source;
            EditorUtility.SetDirty(flicker);
            PrefabUtility.RecordPrefabInstancePropertyModifications(flicker);
            EditorSceneManager.MarkSceneDirty(flicker.gameObject.scene);
        }
    }
}
