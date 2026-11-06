#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // One-shot read-only audit. Never saves or edits the scene or its objects.
    [InitializeOnLoad]
    public static class LawyerRoomPerformanceAudit
    {
        const string ScenePath = "Assets/MainScenes/From_PM/Lawyer Room.unity";
        const string Folder = "Tools/LawyerRoomPerformance";
        [Serializable] class Request { public string id; }
        [Serializable] public class MeshRow
        {
            public string objectPath, meshName, meshAsset, meshGuid, prefabAsset, prefabRoot;
            public long meshFileId, triangles; public int vertices, submeshes, materialSlots;
            public bool active, rendererEnabled, castsShadows, skinned;
            public string[] materials, shaders;
        }
        [Serializable] public class LightRow
        { public string objectPath, type, bakeType, shadows; public bool active; public float range, intensity; }
        [Serializable] public class ColliderRow
        { public string objectPath, meshAsset; public bool active, convex, trigger; public long triangles; }
        [Serializable] public class Result
        {
            public string requestId, scenePath, error, pipeline;
            public bool success, usedLoadedScene, sceneDirtyBefore, sceneDirtyAfter;
            public MeshRow[] meshes; public LightRow[] lights; public ColliderRow[] meshColliders;
            public int activeCameras, activeParticleSystems;
        }
        static LawyerRoomPerformanceAudit() => EditorApplication.update += Poll;
        static string Hierarchy(Transform t)
        { var p = new List<string>(); for (; t != null; t = t.parent) p.Add(t.name); p.Reverse(); return string.Join("/", p); }
        static long Triangles(Mesh mesh)
        {
            long count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                if (mesh.GetTopology(i) == MeshTopology.Triangles) count += (long)mesh.GetIndexCount(i) / 3;
            return count;
        }
        static void Poll()
        {
            string request = Folder + "/audit.request.json";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var input = JsonUtility.FromJson<Request>(File.ReadAllText(request));
            if (input == null || string.IsNullOrEmpty(input.id) || input.id.Any(c => !char.IsLetterOrDigit(c) && c != '-')) return;
            File.Move(request, Folder + "/" + input.id + ".consumed.json");
            var result = Run(); result.requestId = input.id;
            File.WriteAllText(Folder + "/" + input.id + ".result.json", JsonUtility.ToJson(result, true));
        }
        public static void Batch()
        {
            var result = Run(); Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder + "/batch.result.json", JsonUtility.ToJson(result, true));
            EditorApplication.Exit(result.success ? 0 : 1);
        }
        static Result Run()
        {
            var result = new Result { scenePath = ScenePath };
            Scene preview = default;
            try
            {
                var scene = SceneManager.GetSceneByPath(ScenePath);
                result.usedLoadedScene = scene.IsValid() && scene.isLoaded;
                if (!result.usedLoadedScene) { preview = EditorSceneManager.OpenPreviewScene(ScenePath); scene = preview; }
                result.sceneDirtyBefore = scene.isDirty;
                var roots = scene.GetRootGameObjects();
                var rows = new List<MeshRow>();
                foreach (var renderer in roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)))
                {
                    var skin = renderer as SkinnedMeshRenderer;
                    var filter = renderer.GetComponent<MeshFilter>();
                    var mesh = skin != null ? skin.sharedMesh : filter != null ? filter.sharedMesh : null;
                    if (mesh == null) continue;
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long id);
                    var instance = PrefabUtility.GetNearestPrefabInstanceRoot(renderer.gameObject);
                    var mats = renderer.sharedMaterials;
                    rows.Add(new MeshRow { objectPath = Hierarchy(renderer.transform), meshName = mesh.name,
                        meshAsset = AssetDatabase.GetAssetPath(mesh), meshGuid = guid, meshFileId = id,
                        prefabAsset = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(renderer.gameObject),
                        prefabRoot = instance != null ? Hierarchy(instance.transform) : "",
                        triangles = Triangles(mesh), vertices = mesh.vertexCount, submeshes = mesh.subMeshCount,
                        active = renderer.gameObject.activeInHierarchy, rendererEnabled = renderer.enabled,
                        castsShadows = renderer.shadowCastingMode != ShadowCastingMode.Off, skinned = skin != null,
                        materialSlots = mats.Length, materials = mats.Select(m => m != null ? AssetDatabase.GetAssetPath(m) : "").ToArray(),
                        shaders = mats.Select(m => m != null && m.shader != null ? m.shader.name : "missing").ToArray() });
                }
                result.meshes = rows.ToArray();
                result.lights = roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).Select(l => new LightRow {
                    objectPath = Hierarchy(l.transform), type = l.type.ToString(), bakeType = l.lightmapBakeType.ToString(),
                    shadows = l.shadows.ToString(), active = l.isActiveAndEnabled, range = l.range, intensity = l.intensity }).ToArray();
                result.meshColliders = roots.SelectMany(r => r.GetComponentsInChildren<MeshCollider>(true)).Select(c => new ColliderRow {
                    objectPath = Hierarchy(c.transform), meshAsset = c.sharedMesh != null ? AssetDatabase.GetAssetPath(c.sharedMesh) : "",
                    triangles = c.sharedMesh != null ? Triangles(c.sharedMesh) : 0, active = c.enabled && c.gameObject.activeInHierarchy,
                    convex = c.convex, trigger = c.isTrigger }).ToArray();
                result.activeCameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Count(c => c.isActiveAndEnabled);
                result.activeParticleSystems = roots.SelectMany(r => r.GetComponentsInChildren<ParticleSystem>(true)).Count(p => p.gameObject.activeInHierarchy);
                result.pipeline = AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline);
                result.sceneDirtyAfter = scene.isDirty; result.success = true;
            }
            catch (Exception e) { result.error = e.ToString(); }
            finally { if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview); }
            return result;
        }
    }
}
#endif
