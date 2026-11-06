using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GUYMeshCheck
{
    [Serializable] public class MeshInfo { public string name; public long id; public int vertices, submeshes; public long indices; }
    [Serializable] public class ModelInfo { public string path; public MeshInfo[] meshes; public string[] transforms; public int[] materialSlots; }
    [Serializable] public class Report { public bool success; public ModelInfo[] before, after; public string error; }
    public static void Run()
    {
        var report = new Report();
        try
        {
            report.before = Enumerable.Range(0, 6).Select(Snapshot).ToArray();
            var work = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../GUYOptimization/Round2"));
            // This class runs only in the isolated validation project.
            for (int i = 0; i < 6; i++)
            {
                File.Copy(Path.Combine(work, "Model" + i, "optimized-preserved.fbx"), "Assets/Models/Model" + i + ".fbx", true);
                AssetDatabase.ImportAsset("Assets/Models/Model" + i + ".fbx", ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
            report.after = Enumerable.Range(0, 6).Select(Snapshot).ToArray();
            for (int i = 0; i < 6; i++)
            {
                var a = report.before[i]; var b = report.after[i];
                if (!a.transforms.SequenceEqual(b.transforms) || !a.materialSlots.SequenceEqual(b.materialSlots)) throw new Exception("Hierarchy/transform/material slots changed: " + i);
                if (!a.meshes.Select(m => m.name + ":" + m.id).SequenceEqual(b.meshes.Select(m => m.name + ":" + m.id))) throw new Exception("Mesh file IDs changed: " + i);
                if (b.meshes.Sum(m => m.indices) >= a.meshes.Sum(m => m.indices)) throw new Exception("No triangle reduction: " + i);
            }
            report.success = true;
        }
        catch (Exception e) { report.error = e.ToString(); Debug.LogException(e); }
        File.WriteAllText("mesh-validation.json", JsonUtility.ToJson(report, true));
        EditorApplication.Exit(report.success ? 0 : 1);
    }
    private static ModelInfo Snapshot(int i)
    {
        string path = "Assets/Models/Model" + i + ".fbx";
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null) throw new Exception("Missing model: " + path);
        var meshes = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Select(m =>
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m, out string guid, out long id);
            long indices = 0; for (int s = 0; s < m.subMeshCount; s++) indices += (long)m.GetIndexCount(s);
            return new MeshInfo { name = m.name, id = id, vertices = m.vertexCount, submeshes = m.subMeshCount, indices = indices };
        }).OrderBy(m => m.id).ToArray();
        var transforms = root.GetComponentsInChildren<Transform>(true).Select(t => AnimationUtility.CalculateTransformPath(t, root.transform) + ":" + t.localPosition.ToString("F6") + t.localRotation.ToString("F6") + t.localScale.ToString("F6")).ToArray();
        return new ModelInfo { path = path, meshes = meshes, transforms = transforms, materialSlots = root.GetComponentsInChildren<Renderer>(true).Select(r => r.sharedMaterials.Length).ToArray() };
    }
}
