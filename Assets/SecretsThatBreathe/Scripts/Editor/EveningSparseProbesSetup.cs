using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class EveningSparseProbesSetup
    {
        private static bool installing;
        private static string Request => Path.GetFullPath(Path.Combine(Application.dataPath, "../Tools/EveningLighting/sparse-probes-sync.request"));
        static EveningSparseProbesSetup()
        {
            EditorApplication.delayCall += Install;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install;
            };
        }
        private static void Install()
        {
            if (installing || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += Install; return; }
            const string path = "Assets/MainScenes/From_PM/MainScene_2_Evening.unity";
            var scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded) return;
            installing = true;
            Scene preview = default;
            try
            {
                preview = EditorSceneManager.OpenPreviewScene(path);
                const string name = "FD_Route_Sparse_LightProbes";
                var saved = preview.GetRootGameObjects().First(go => go.name == name);
                var current = scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
                if (current == null)
                {
                    current = Object.Instantiate(saved);
                    current.name = name;
                    SceneManager.MoveGameObjectToScene(current, scene);
                    Undo.RegisterCreatedObjectUndo(current, "Add sparse FD route probes");
                }
                else
                {
                    var probes = current.GetComponent<LightProbeGroup>();
                    Undo.RecordObjects(new Object[] { current.transform, probes }, "Update sparse FD route probes");
                    current.transform.SetPositionAndRotation(saved.transform.position, saved.transform.rotation);
                    current.transform.localScale = saved.transform.localScale;
                    probes.probePositions = saved.GetComponent<LightProbeGroup>().probePositions;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                File.WriteAllText(Path.ChangeExtension(Request, ".result.txt"),
                    "Open scene synchronized: " + current.GetComponent<LightProbeGroup>().probePositions.Length + " sparse route probes. Bake required.");
                File.Delete(Request);
            }
            finally
            {
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                installing = false;
            }
        }
    }
}
