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
                    if (view != null && view.phonePrefab != null)
                    {
                        EnsureScenePhone(view, flashlight);
                        continue;
                    }
                    if (view == null) view = Undo.AddComponent<PhoneFlashlightView>(flashlight.gameObject);
                    view.phonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SecretsThatBreathe/Props/Phone/Prefabs/Phone_Blank.prefab");
                    foreach (var sceneRoot in scene.GetRootGameObjects())
                        foreach (var camera in sceneRoot.GetComponentsInChildren<Camera>(true)) if (camera.CompareTag("MainCamera")) view.viewCamera = camera;
                    view.onMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SecretsThatBreathe/Props/Phone/Materials/Screen_Flashlight_On.mat");
                    view.offMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SecretsThatBreathe/Props/Phone/Materials/Screen_Flashlight_Off.mat");
                    Undo.RecordObject(flashlight, "Link phone flashlight presentation");
                    flashlight.ConfigurePhone(view);
                    EnsureScenePhone(view, flashlight);
                    EditorUtility.SetDirty(view); EditorUtility.SetDirty(flashlight);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(view);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(flashlight);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
        private static void EnsureScenePhone(PhoneFlashlightView view, FirstPersonFlashlight flashlight)
        {
            if (view.heldPhone != null) return;
            if (view.viewCamera == null)
                foreach (var root in view.gameObject.scene.GetRootGameObjects())
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                        if (camera.CompareTag("MainCamera")) view.viewCamera = camera;
            if (view.phonePrefab == null || view.viewCamera == null) return;
            var existing = view.viewCamera.transform.Find("HeldPhone");
            Undo.RecordObject(view, "Assign editable scene phone");
            if (existing != null) view.heldPhone = existing.gameObject;
            else
            {
                view.heldPhone = PrefabUtility.InstantiatePrefab(view.phonePrefab, view.viewCamera.transform) as GameObject;
                if (view.heldPhone == null) return;
                Undo.RegisterCreatedObjectUndo(view.heldPhone, "Create editable HeldPhone");
                view.heldPhone.name = "HeldPhone";
                float distance = Mathf.Max(view.cameraDistance, view.viewCamera.nearClipPlane + .025f);
                view.heldPhone.transform.SetPositionAndRotation(view.viewCamera.ViewportToWorldPoint(new Vector3(view.screenPosition.x, view.screenPosition.y, distance)),
                    view.viewCamera.transform.rotation * Quaternion.Euler(view.localRotation));
                view.heldPhone.transform.localScale = Vector3.one * view.phoneScale;
                foreach (var script in view.heldPhone.GetComponentsInChildren<Phone.PhoneFlashlightScreen>(true))
                {
                    script.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(script);
                }
                foreach (var collider in view.heldPhone.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                }
                foreach (var renderer in view.heldPhone.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    if (renderer.name == "Screen" && view.offMaterial != null) renderer.sharedMaterial = view.offMaterial;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                foreach (var light in view.heldPhone.GetComponentsInChildren<Light>(true))
                {
                    light.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(light);
                }
                view.heldPhone.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view.heldPhone.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view.heldPhone);
            }
            foreach (var anchor in view.heldPhone.GetComponentsInChildren<Transform>(true))
                if (anchor.name == "FlashlightAnchor")
                {
                    var light = anchor.GetComponent<Light>();
                    if (light == null) light = anchor.GetComponentInChildren<Light>(true);
                    if (light != null)
                    {
                        var previous = new SerializedObject(flashlight).FindProperty("flashlightSource").objectReferenceValue as Light;
                        if (previous != null && previous != light)
                        {
                            Undo.RecordObject(previous, "Disable the former camera flashlight");
                            previous.enabled = false;
                            EditorUtility.SetDirty(previous);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(previous);
                        }
                        Undo.RecordObject(flashlight, "Use editable HeldPhone flashlight");
                        flashlight.ConfigureLightSource(light);
                        EditorUtility.SetDirty(flashlight);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(flashlight);
                    }
                    break;
                }
            EditorUtility.SetDirty(view);
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }
    }
}
