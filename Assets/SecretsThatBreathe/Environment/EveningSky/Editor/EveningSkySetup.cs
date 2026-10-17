using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    public static class EveningSkySetup
    {
        public const string MaterialPath = "Assets/SecretsThatBreathe/Environment/EveningSky/EveningSky.mat";

        [MenuItem("Tools/Secrets That Breathe/Sky/Apply Evening Sky To Active Scene")]
        public static void Apply()
        {
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (sky == null || sky.shader == null || ShaderUtil.ShaderHasError(sky.shader))
            {
                Debug.LogError("Evening Sky material is missing or its shader has errors.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Evening Sky");
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null || component.GetType().FullName != "Tenkoku.Core.TenkokuModule")
                        continue;

                    // Keep the existing Light transforms, colors and intensities.
                    // Only stop the dynamic controller and its sky/weather visuals.
                    foreach (MonoBehaviour script in component.GetComponentsInChildren<MonoBehaviour>(true))
                        if (script != null) Disable(script);
                    foreach (Camera camera in component.GetComponentsInChildren<Camera>(true))
                        Disable(camera);
                    foreach (Renderer renderer in component.GetComponentsInChildren<Renderer>(true))
                    {
                        Undo.RecordObject(renderer, "Hide Tenkoku sky visual");
                        renderer.enabled = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    }
                }
            }

            Undo.RecordObjects(Resources.FindObjectsOfTypeAll<RenderSettings>(), "Set Evening Sky");
            RenderSettings.skybox = sky;
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeObject = sky;
            SceneView.RepaintAll();
            Debug.Log("Evening Sky applied. Existing light and fog settings are preserved. Save the scene to keep this change.");
        }

        private static void Disable(Behaviour component)
        {
            Undo.RecordObject(component, "Stop Tenkoku sky controller");
            component.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }
}
