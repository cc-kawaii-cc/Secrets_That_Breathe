using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class PhoneHandsetSetup
    {
        static PhoneHandsetSetup()
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
                foreach (var call in root.GetComponentsInChildren<PhoneCallInteraction>(true))
                {
                    var pickup = call.GetComponent<PhoneHandsetPickup>();
                    bool changed = false;
                    if (pickup != null && pickup.handset != null)
                    {
                        foreach (var item in pickup.handset.GetComponentsInChildren<UnityEngine.Transform>(true))
                        {
                            if (GameObjectUtility.GetStaticEditorFlags(item.gameObject) == 0) continue;
                            Undo.RecordObject(item.gameObject, "Make phone handset movable");
                            GameObjectUtility.SetStaticEditorFlags(item.gameObject, 0);
                            EditorUtility.SetDirty(item.gameObject);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(item.gameObject);
                            changed = true;
                        }
                    }
                    if (pickup != null && pickup.handset != null && call.handsetPickup == pickup)
                    {
                        if (changed) EditorSceneManager.MarkSceneDirty(scene);
                        continue;
                    }
                    if (pickup == null) pickup = Undo.AddComponent<PhoneHandsetPickup>(call.gameObject);
                    Undo.RecordObject(pickup, "Assign phone handset Cube.001");
                    foreach (var child in call.GetComponentsInChildren<UnityEngine.Transform>(true))
                        if (child.name == "Cube.001" && pickup.handset == null) pickup.handset = child;
                    if (pickup.handset != null)
                        foreach (var item in pickup.handset.GetComponentsInChildren<UnityEngine.Transform>(true))
                        {
                            Undo.RecordObject(item.gameObject, "Make phone handset movable");
                            GameObjectUtility.SetStaticEditorFlags(item.gameObject, 0);
                            EditorUtility.SetDirty(item.gameObject);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(item.gameObject);
                        }
                    Undo.RecordObject(call, "Connect phone handset animation");
                    call.handsetPickup = pickup;
                    EditorUtility.SetDirty(pickup);
                    EditorUtility.SetDirty(call);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pickup);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(call);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
    }
}
