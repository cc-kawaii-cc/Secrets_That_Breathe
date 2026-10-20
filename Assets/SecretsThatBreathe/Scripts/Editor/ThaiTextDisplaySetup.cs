using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class ThaiTextDisplaySetup
    {
        private static bool queued;
        static ThaiTextDisplaySetup()
        {
            QueueInstall();
            EditorSceneManager.sceneOpened += (scene, mode) => QueueInstall();
            EditorApplication.hierarchyChanged += QueueInstall;
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
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Nemo_Upgrade/Font/Kanit/Kanit-Medium SDF.asset");
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (font != null && label.font != font)
                    {
                        Undo.RecordObject(label, "Restore Kanit Medium UI font");
                        label.font = font;
                        label.fontSharedMaterial = font.material;
                        EditorUtility.SetDirty(label);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                        changed = true;
                    }
                    var color = label.color;
                    if (color.r == 1f && color.g == 1f && color.b == 1f)
                    {
                        Undo.RecordObject(label, "Change white UI text to DCDCDC");
                        color.r = color.g = color.b = 220f / 255f;
                        label.color = color;
                        EditorUtility.SetDirty(label);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                        changed = true;
                    }
                    var display = label.GetComponent<ThaiTextDisplay>();
                    if (display != null) continue;
                    display = Undo.AddComponent<ThaiTextDisplay>(label.gameObject);
                    display.Bind();
                    EditorUtility.SetDirty(display);
                    EditorUtility.SetDirty(label);
                    changed = true;
                }
            }
            if (changed) EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
