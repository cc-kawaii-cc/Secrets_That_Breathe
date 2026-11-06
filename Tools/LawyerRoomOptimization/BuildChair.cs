using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildOptimizedLawyerChair
{
    const string Source = "Assets/SecretsThatBreathe/Props/SmoothedChairs/Chair_5e546eeaa124d9c4fac7f82be9fd2043_4579454407841743219_Smooth60.asset";
    [Serializable] class Input { public string name; public Vector3[] positions, normals; public Vector2[] uv, uv2; public int[] indices; }
    [Serializable] class Report
    {
        public bool success; public string error, guidBefore, guidAfter;
        public long idBefore, idAfter, trianglesBefore, trianglesAfter;
        public int verticesBefore, verticesAfter, submeshesBefore, submeshesAfter;
        public Bounds boundsBefore, boundsAfter;
    }
    public static void Run()
    {
        var work = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../LawyerRoomOptimization"));
        var result = new Report();
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<Mesh>(Source);
            if (source == null || source.subMeshCount != 1 || source.blendShapeCount != 0) throw new Exception("Unexpected source mesh");
            result.verticesBefore = source.vertexCount; result.trianglesBefore = source.GetIndexCount(0) / 3;
            if (result.trianglesBefore != 77696) throw new Exception("Unexpected chair baseline");
            result.submeshesBefore = source.subMeshCount; result.boundsBefore = source.bounds;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out result.guidBefore, out result.idBefore);
            var input = JsonUtility.FromJson<Input>(File.ReadAllText(Path.Combine(work, "optimized-mesh.json")));
            if (input.name != source.name || input.positions.Length != input.normals.Length || input.positions.Length != input.uv.Length || input.positions.Length != input.uv2.Length)
                throw new Exception("Vertex attributes mismatch");
            if (input.positions.Any(v => !float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z)) ||
                input.normals.Any(v => !float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z)) ||
                input.indices.Any(i => i < 0 || i >= input.positions.Length)) throw new Exception("Invalid mesh data");
            var mesh = new Mesh { name = source.name, indexFormat = input.positions.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = input.positions; mesh.normals = input.normals; mesh.uv = input.uv; mesh.uv2 = input.uv2;
            mesh.SetTriangles(input.indices, 0); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            if ((mesh.bounds.min - source.bounds.min).magnitude > source.bounds.size.magnitude * .0025f ||
                (mesh.bounds.max - source.bounds.max).magnitude > source.bounds.size.magnitude * .0025f) throw new Exception("Bounds changed too much");
            EditorUtility.CopySerialized(mesh, source); AssetDatabase.SaveAssetIfDirty(source);
            UnityEngine.Object.DestroyImmediate(mesh);
            AssetDatabase.ImportAsset(Source, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            source = AssetDatabase.LoadAssetAtPath<Mesh>(Source);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out result.guidAfter, out result.idAfter);
            result.verticesAfter = source.vertexCount; result.trianglesAfter = source.GetIndexCount(0) / 3;
            result.submeshesAfter = source.subMeshCount; result.boundsAfter = source.bounds;
            if (result.guidBefore != result.guidAfter || result.idBefore != result.idAfter || result.submeshesBefore != result.submeshesAfter || result.trianglesAfter >= result.trianglesBefore)
                throw new Exception("Stable reference/reduction validation failed");
            File.Copy(Source, Path.Combine(work, "optimized-chair.asset"), true);
            result.success = true;
        }
        catch (Exception e) { result.error = e.ToString(); Debug.LogException(e); }
        File.WriteAllText(Path.Combine(work, "unity-build.json"), JsonUtility.ToJson(result, true));
        if (!result.success) { EditorApplication.Exit(1); return; }
        SecretsThatBreathe.Editor.LawyerRoomPerformanceAudit.Batch();
    }
}
