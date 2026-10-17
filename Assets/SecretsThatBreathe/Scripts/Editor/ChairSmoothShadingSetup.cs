using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // The chair is a variant of a larger office FBX. Leave that shared FBX untouched.
    [InitializeOnLoad]
    public static class ChairSmoothShadingSetup
    {
        private const string ChairGuid = "6d1635443d98355448462426c3207648";
        private const string Folder = "Assets/SecretsThatBreathe/Props/SmoothedChairs";
        private const float SmoothAngle = 60f;
        private static bool queued;

        static ChairSmoothShadingSetup()
        {
            Queue();
            EditorSceneManager.sceneOpened += (scene, mode) => Queue();
            EditorApplication.hierarchyChanged += Queue;
        }
        private static void Queue()
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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(ChairGuid));
            if (prefab == null) return;
            var sources = new HashSet<Mesh>();
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null) sources.Add(filter.sharedMesh);
            var replacements = new Dictionary<Mesh, Mesh>();
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var source = filter.sharedMesh;
                    if (source == null || !sources.Contains(source)) continue;
                    if (!replacements.TryGetValue(source, out var smooth))
                    {
                        smooth = LoadOrCreate(source);
                        replacements.Add(source, smooth);
                    }
                    Undo.RecordObject(filter, "Smooth chair shading");
                    filter.sharedMesh = smooth;
                    EditorUtility.SetDirty(filter);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                    changed = true;
                }
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                SceneView.RepaintAll();
            }
        }
        private static Mesh LoadOrCreate(Mesh source)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long id);
            string path = Folder + "/Chair_" + guid + "_" + id + "_Smooth60.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            EnsureFolder(Folder);
            // Editor mesh data access also works when the FBX has Read/Write disabled.
            using (var read = MeshUtility.AcquireReadOnlyMeshData(source))
            {
                var write = Mesh.AllocateWritableMeshData(1);
                var input = read[0];
                var output = write[0];
                output.SetVertexBufferParams(source.vertexCount, source.GetVertexAttributes());
                for (int stream = 0; stream < source.vertexBufferCount; stream++)
                    input.GetVertexData<byte>(stream).CopyTo(output.GetVertexData<byte>(stream));
                int indexCount = source.indexFormat == IndexFormat.UInt32
                    ? input.GetIndexData<uint>().Length : input.GetIndexData<ushort>().Length;
                output.SetIndexBufferParams(indexCount, source.indexFormat);
                if (source.indexFormat == IndexFormat.UInt32)
                    input.GetIndexData<uint>().CopyTo(output.GetIndexData<uint>());
                else input.GetIndexData<ushort>().CopyTo(output.GetIndexData<ushort>());
                output.subMeshCount = source.subMeshCount;
                for (int i = 0; i < source.subMeshCount; i++)
                    output.SetSubMesh(i, source.GetSubMesh(i), MeshUpdateFlags.DontRecalculateBounds);
                var mesh = new Mesh { name = source.name + "_Smooth60" };
                Mesh.ApplyAndDisposeWritableMeshData(write, mesh);
                mesh.bounds = source.bounds;
                SmoothNormals(mesh, SmoothAngle);
                AssetDatabase.CreateAsset(mesh, path);
                AssetDatabase.SaveAssetIfDirty(mesh);
                return mesh;
            }
        }
        internal static void SmoothNormals(Mesh mesh, float angle)
        {
            mesh.RecalculateNormals();
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var result = new Vector3[normals.Length];
            var groups = new Dictionary<Vector3, List<int>>();
            for (int i = 0; i < vertices.Length; i++)
            {
                // FBX UV seams use duplicate vertices at exactly the same position.
                if (!groups.TryGetValue(vertices[i], out var group))
                    groups.Add(vertices[i], group = new List<int>());
                group.Add(i);
            }
            float threshold = Mathf.Cos(angle * Mathf.Deg2Rad);
            foreach (var group in groups.Values)
                foreach (int i in group)
                {
                    Vector3 sum = Vector3.zero;
                    foreach (int j in group)
                        if (Vector3.Dot(normals[i], normals[j]) >= threshold) sum += normals[j];
                    result[i] = sum.sqrMagnitude > 1e-12f ? sum.normalized : normals[i];
                }
            mesh.normals = result;
            if (mesh.HasVertexAttribute(VertexAttribute.TexCoord0)) mesh.RecalculateTangents();
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }
    }
}
