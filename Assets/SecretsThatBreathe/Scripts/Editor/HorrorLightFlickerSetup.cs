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
    }
}
