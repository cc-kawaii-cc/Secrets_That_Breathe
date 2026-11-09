using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class EveningEnvironmentProbeSetup
    {
        private const string ScenePath = "Assets/MainScenes/From_PM/MainScene_2_Evening.unity";
        private const string GroupName = "AddedEnvironment_LightProbes";
        private const float GridSize = 20f;

        static EveningEnvironmentProbeSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += Install;
        }

        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == GroupName) return;

            var cells = new Dictionary<Vector2Int, float>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.gameObject.activeInHierarchy || renderer is ParticleSystemRenderer) continue;
                string name = renderer.gameObject.name.ToLowerInvariant();
                string parent = renderer.transform.parent != null ? renderer.transform.parent.name.ToLowerInvariant() : "";
                if (!(name.Contains("tree") || name.Contains("bush") || name.Contains("banana") ||
                      name.Contains("vegetation") || parent.Contains("tree") || parent.Contains("bush") ||
                      parent.Contains("banana") || parent.Contains("vegetation"))) continue;
                Bounds bounds = renderer.bounds;
                if (bounds.size.x > 35f || bounds.size.z > 35f) continue;
                var cell = new Vector2Int(Mathf.RoundToInt(bounds.center.x / GridSize), Mathf.RoundToInt(bounds.center.z / GridSize));
                if (!cells.ContainsKey(cell)) cells.Add(cell, bounds.min.y);
            }
            if (cells.Count == 0) return;

            var go = new GameObject(GroupName);
            Undo.RegisterCreatedObjectUndo(go, "Add environment light probes");
            SceneManager.MoveGameObjectToScene(go, scene);
            var group = go.AddComponent<LightProbeGroup>();
            var positions = new List<Vector3>();
            foreach (var cell in cells)
            {
                float x = cell.Key.x * GridSize, z = cell.Key.y * GridSize, y = cell.Value;
                positions.Add(new Vector3(x, y + 1f, z));
                positions.Add(new Vector3(x, y + 5f, z));
                positions.Add(new Vector3(x, y + 10f, z));
            }
            group.probePositions = positions.ToArray();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Evening] Added {positions.Count} editable environment probes. Save the scene and bake lighting to populate them.", go);
        }
    }
}
