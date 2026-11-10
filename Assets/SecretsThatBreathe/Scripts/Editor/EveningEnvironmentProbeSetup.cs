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
        private const string SessionKey = "EveningEnvironmentProbeCoverage20261110";
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
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            LightProbeGroup group = null;
            var existing = new List<Vector3>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<LightProbeGroup>(true))
            {
                if (candidate.name == GroupName) group = candidate;
                if (!candidate.isActiveAndEnabled) continue;
                foreach (var point in candidate.probePositions)
                    existing.Add(candidate.transform.TransformPoint(point));
            }

            var cells = new Dictionary<Vector2Int, float>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.gameObject.activeInHierarchy || renderer is ParticleSystemRenderer) continue;
                bool vegetation = false;
                for (Transform t = renderer.transform; t != null; t = t.parent)
                {
                    string name = t.name.ToLowerInvariant();
                    if (name.Contains("tree") || name.Contains("bush") || name.Contains("banana") ||
                        name.Contains("vegetation") || name.Contains("rice") || name.Contains("paddy")) { vegetation = true; break; }
                }
                if (!vegetation) continue;
                Bounds bounds = renderer.bounds;
                if (bounds.size.x > 35f || bounds.size.z > 35f) continue;
                var cell = new Vector2Int(Mathf.RoundToInt(bounds.center.x / GridSize), Mathf.RoundToInt(bounds.center.z / GridSize));
                if (!cells.ContainsKey(cell)) cells.Add(cell, bounds.min.y);
            }
            foreach (var root in scene.GetRootGameObjects())
            foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
            {
                var data = terrain.terrainData;
                if (data == null) continue;
                foreach (var tree in data.treeInstances)
                {
                    if (tree.prototypeIndex < 0 || tree.prototypeIndex >= data.treePrototypes.Length) continue;
                    var prefab = data.treePrototypes[tree.prototypeIndex].prefab;
                    if (prefab == null) continue;
                    string name = prefab.name.ToLowerInvariant();
                    if (!(name.Contains("tree") || name.Contains("rice") || name.Contains("paddy") || name.Contains("banana") || name.Contains("grass"))) continue;
                    Vector3 world = terrain.transform.TransformPoint(Vector3.Scale(tree.position, data.size));
                    var cell = new Vector2Int(Mathf.RoundToInt(world.x / GridSize), Mathf.RoundToInt(world.z / GridSize));
                    if (!cells.ContainsKey(cell)) cells.Add(cell, world.y);
                }
            }
            if (cells.Count == 0) return;

            if (group == null)
            {
                var go = new GameObject(GroupName);
                Undo.RegisterCreatedObjectUndo(go, "Add environment light probes");
                SceneManager.MoveGameObjectToScene(go, scene);
                group = go.AddComponent<LightProbeGroup>();
            }
            Undo.RecordObject(group, "Extend environment light probes");
            var positions = new List<Vector3>(group.probePositions);
            int added = 0;
            foreach (var cell in cells)
            {
                float x = cell.Key.x * GridSize, z = cell.Key.y * GridSize, y = cell.Value;
                foreach (float height in new[] { 1f, 5f, 10f })
                {
                    Vector3 world = new Vector3(x, y + height, z);
                    bool covered = false;
                    foreach (var point in existing) if ((point - world).sqrMagnitude < 64f) { covered = true; break; }
                    if (covered) continue;
                    positions.Add(group.transform.InverseTransformPoint(world));
                    existing.Add(world);
                    added++;
                }
            }
            group.probePositions = positions.ToArray();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Evening] Added {added} editable environment probes. Save the scene and bake lighting to populate them.", group);
        }
    }
}
