using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class CodexGarageGuyFbxRelinker
{
    private const string ScenePath = "Assets/Tonpalm/Garage.unity";
    private const string MenuPath = "Tools/Codex/Safely Replace Garage GUY FBX With Prefabs";
    private const string RequestFileName = "codex-run-garage-guy-fbx-relink.request";

    private sealed class Replacement
    {
        public readonly string Label;
        public readonly string ModelPath;
        public readonly string PrefabPath;
        public readonly int ExpectedCount;

        public Replacement(string label, string modelPath, string prefabPath, int expectedCount)
        {
            Label = label;
            ModelPath = modelPath;
            PrefabPath = prefabPath;
            ExpectedCount = expectedCount;
        }
    }

    private static readonly Replacement[] Replacements =
    {
        new("Alloy wheel", "Assets/GUY/Model/Alloy wheel/Model/Alloy wheel.fbx", "Assets/GUY/Model/Alloy wheel/Perfab/Alloy wheel.prefab", 19),
        new("Bookshelf", "Assets/GUY/Model/Bookshelf/Model/Bookshelf.fbx", "Assets/GUY/Model/Bookshelf/Perfab/Bookshelf.prefab", 1),
        new("Fridge", "Assets/GUY/Model/Fridge/Model/Fridge.fbx", "Assets/GUY/Model/Fridge/Perfab/Fridge.prefab", 1),
        new("Notebook", "Assets/GUY/Model/Notebook/Model/Notebook.fbx", "Assets/GUY/Model/Notebook/Perfab/Notebook.prefab", 1),
        new("Paint bucket", "Assets/GUY/Model/Paint bucket/Model/Paint bucket.fbx", "Assets/GUY/Model/Paint bucket/Perfab/Paint bucket.prefab", 11),
        new("Phone", "Assets/GUY/Model/Phone/Model/Phone.fbx", "Assets/GUY/Model/Phone/Perfab/Phone.prefab", 2),
        new("printer", "Assets/GUY/Model/printer/Model/printer.fbx", "Assets/GUY/Model/printer/Perfab/printer.prefab", 2),
        new("Tire", "Assets/GUY/Model/Tire/Model/Tire.fbx", "Assets/GUY/Model/Tire/Perfab/Tire.prefab", 35),
        new("wrench", "Assets/GUY/Model/wrench/Model/wrench.fbx", "Assets/GUY/Model/wrench/Perfab/wrench.prefab", 1),
    };

    [InitializeOnLoadMethod]
    private static void RunRequestedOperationAfterCompile()
    {
        string requestPath = GetProjectTempPath(RequestFileName);
        if (!File.Exists(requestPath))
            return;

        // Claim the one-shot request before scheduling, so a second domain reload cannot repeat it.
        File.Delete(requestPath);
        EditorApplication.delayCall += Run;
    }

    [MenuItem(MenuPath)]
    private static void Run()
    {
        Scene scene = default;
        bool openedByTool = false;
        string backupPath = null;

        try
        {
            scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedByTool = true;
            }

            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException($"Could not load {ScenePath}.");

            // Preserve any unsaved user work in this scene before creating the backup.
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save the Garage scene before replacement.");

            backupPath = CreateBackup();

            var settings = new PrefabReplacingSettings
            {
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                prefabOverridesOptions = PrefabOverridesOptions.KeepAllPossibleOverrides,
                changeRootNameToAssetName = false,
                logInfo = false,
            };

            var report = new List<string>
            {
                $"Scene: {ScenePath}",
                $"Backup: {backupPath}",
                $"Started: {DateTime.Now:O}",
            };

            foreach (Replacement replacement in Replacements)
            {
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(replacement.PrefabPath);
                if (prefabAsset == null)
                    throw new InvalidOperationException($"Missing replacement prefab: {replacement.PrefabPath}");

                string[] dependencies = AssetDatabase.GetDependencies(replacement.PrefabPath, true);
                if (!dependencies.Contains(replacement.ModelPath, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"{replacement.PrefabPath} does not wrap the expected model. " +
                        $"Expected dependency {replacement.ModelPath}.");
                }

                GameObject[] instances = FindInstanceRoots(scene, replacement.ModelPath);
                if (instances.Length != replacement.ExpectedCount)
                {
                    throw new InvalidOperationException(
                        $"Unexpected {replacement.Label} count. Expected {replacement.ExpectedCount}, found {instances.Length}.");
                }

                string[] before = instances.Select(TransformSignature).OrderBy(value => value, StringComparer.Ordinal).ToArray();

                PrefabUtility.ReplacePrefabAssetOfPrefabInstances(
                    instances,
                    prefabAsset,
                    settings,
                    InteractionMode.AutomatedAction);

                GameObject[] oldInstances = FindInstanceRoots(scene, replacement.ModelPath);
                GameObject[] newInstances = FindInstanceRoots(scene, replacement.PrefabPath);
                string[] after = newInstances.Select(TransformSignature).OrderBy(value => value, StringComparer.Ordinal).ToArray();

                if (oldInstances.Length != 0)
                    throw new InvalidOperationException($"{replacement.Label} still has {oldInstances.Length} FBX instances after replacement.");
                if (newInstances.Length != replacement.ExpectedCount)
                    throw new InvalidOperationException(
                        $"{replacement.Label} replacement count mismatch. Expected {replacement.ExpectedCount}, found {newInstances.Length}.");
                if (!before.SequenceEqual(after, StringComparer.Ordinal))
                    throw new InvalidOperationException($"{replacement.Label} transform or scene placement changed during replacement.");

                report.Add($"{replacement.Label}: {newInstances.Length}");
            }

            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save the Garage scene after replacement.");

            report.Add($"Completed: {DateTime.Now:O}");
            report.Add("Status: SUCCESS");
            WriteResult(report);
            Debug.Log($"[Codex] Replaced 73 GUY FBX instances with regular prefabs in {ScenePath}. Backup: {backupPath}");
        }
        catch (Exception exception)
        {
            WriteResult(new[]
            {
                $"Scene: {ScenePath}",
                $"Backup: {backupPath ?? "<not created>"}",
                $"Failed: {DateTime.Now:O}",
                "Status: FAILED",
                exception.ToString(),
            });

            Debug.LogException(exception);

            if (scene.IsValid() && scene.isLoaded)
            {
                // Discard partial in-memory replacements. The last saved scene and backup remain intact.
                EditorSceneManager.CloseScene(scene, true);
                if (!openedByTool)
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            throw;
        }
        finally
        {
            if (openedByTool && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static GameObject[] FindInstanceRoots(Scene scene, string assetPath)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .Where(gameObject => PrefabUtility.GetNearestPrefabInstanceRoot(gameObject) == gameObject)
            .Where(gameObject => string.Equals(
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject),
                assetPath,
                StringComparison.Ordinal))
            .ToArray();
    }

    private static string TransformSignature(GameObject gameObject)
    {
        Transform transform = gameObject.transform;
        Transform parent = transform.parent;
        return string.Join("|",
            HierarchyPath(parent),
            transform.GetSiblingIndex().ToString(CultureInfo.InvariantCulture),
            gameObject.name,
            Vector(transform.localPosition),
            QuaternionValue(transform.localRotation),
            Vector(transform.localScale),
            gameObject.activeSelf ? "active" : "inactive",
            gameObject.layer.ToString(CultureInfo.InvariantCulture),
            gameObject.tag,
            GameObjectUtility.GetStaticEditorFlags(gameObject).ToString());
    }

    private static string HierarchyPath(Transform transform)
    {
        if (transform == null)
            return "<root>";

        var parts = new Stack<string>();
        while (transform != null)
        {
            parts.Push($"{transform.GetSiblingIndex()}:{transform.name}");
            transform = transform.parent;
        }

        return string.Join("/", parts);
    }

    private static string Vector(Vector3 value) => string.Format(
        CultureInfo.InvariantCulture,
        "{0:R},{1:R},{2:R}",
        value.x,
        value.y,
        value.z);

    private static string QuaternionValue(Quaternion value) => string.Format(
        CultureInfo.InvariantCulture,
        "{0:R},{1:R},{2:R},{3:R}",
        value.x,
        value.y,
        value.z,
        value.w);

    private static string CreateBackup()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("Could not resolve project root.");
        string backupDirectory = Path.Combine(projectRoot, "tmp", "codex-backups");
        Directory.CreateDirectory(backupDirectory);
        string backupPath = Path.Combine(
            backupDirectory,
            $"Garage.before-guy-fbx-to-prefab.{DateTime.Now:yyyyMMdd-HHmmss}.unity");
        File.Copy(Path.Combine(projectRoot, ScenePath), backupPath, false);
        return backupPath;
    }

    private static void WriteResult(IEnumerable<string> lines)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("Could not resolve project root.");
        string resultDirectory = Path.Combine(projectRoot, "tmp");
        Directory.CreateDirectory(resultDirectory);
        File.WriteAllLines(Path.Combine(resultDirectory, "codex-garage-guy-fbx-relink-result.txt"), lines);
    }

    private static string GetProjectTempPath(string fileName)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("Could not resolve project root.");
        string tempDirectory = Path.Combine(projectRoot, "tmp");
        Directory.CreateDirectory(tempDirectory);
        return Path.Combine(tempDirectory, fileName);
    }
}
