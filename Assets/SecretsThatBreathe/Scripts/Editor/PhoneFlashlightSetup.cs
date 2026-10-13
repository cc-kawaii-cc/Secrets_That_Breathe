using StarterAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class PhoneFlashlightSetup
    {
        static PhoneFlashlightSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var flashlight in root.GetComponentsInChildren<FirstPersonFlashlight>(true))
                {
                    var view = flashlight.GetComponent<PhoneFlashlightView>();
                    if (view != null && view.phonePrefab != null) continue;
                    if (view == null) view = Undo.AddComponent<PhoneFlashlightView>(flashlight.gameObject);
                    view.phonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SecretsThatBreathe/Props/Phone/Prefabs/Phone_Blank.prefab");
                    foreach (var sceneRoot in scene.GetRootGameObjects())
                        foreach (var camera in sceneRoot.GetComponentsInChildren<Camera>(true)) if (camera.CompareTag("MainCamera")) view.viewCamera = camera;
                    view.onMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SecretsThatBreathe/Props/Phone/Materials/Screen_Flashlight_On.mat");
                    view.offMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SecretsThatBreathe/Props/Phone/Materials/Screen_Flashlight_Off.mat");
                    Undo.RecordObject(flashlight, "Link phone flashlight presentation");
                    flashlight.ConfigurePhone(view);
                    EditorUtility.SetDirty(view); EditorUtility.SetDirty(flashlight);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(view);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(flashlight);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
    }
}
