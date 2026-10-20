using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

// Deliberately limited to explicit, one-use requests for the Porsche headlight task.
// No scene, prefab, importer, mesh, or material is modified by the inspect operation.
[InitializeOnLoad]
public static class PorscheHeadlightTools
{
    internal const string AssetRoot = "Assets/Model Palm/Porsche V.2";
    internal const string ModelPath = AssetRoot + "/PorscheV.2.fbx";
    internal const string PrefabPath = AssetRoot + "/PorscheV.2.prefab";
    private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static readonly string WorkRoot = Path.Combine(ProjectRoot, "Tools", "PorscheHeadlights");
    private static readonly string RequestPath = Path.Combine(WorkRoot, "request.json");
    private static readonly string ResultPath = Path.Combine(WorkRoot, "result.json");
    private static bool busy;
    private static double nextCheck;

    [Serializable]
    public sealed class Request
    {
        public string id;
        public string operation;
    }

    [Serializable]
    public sealed class Result
    {
        public string id;
        public string operation;
        public bool success;
        public string error;
        public string consumedRequest;
        public string utc;
        public string unityVersion;
        public string projectRoot;
        public SceneInfo[] scenes;
        public string activeScenePath;
        public string prefabStageAssetPath;
        public bool prefabStageDirty;
        public AssetInfo[] assets;
        public MeshInfo[] sourceMeshes;
        public NodeInfo[] sourceNodes;
        public NodeInfo[] prefabNodes;
        public NodeInfo[] stageNodes;
        public BuildInfo build;
        public RepairInfo repair;
    }

    [Serializable]
    public sealed class SceneInfo
    {
        public string name;
        public string path;
        public bool isLoaded;
        public bool isDirty;
        public int handle;
    }

    [Serializable]
    public sealed class AssetInfo
    {
        public string path;
        public string guid;
        public long length;
        public string sha256;
        public string metaSha256;
    }

    [Serializable]
    public sealed class MeshInfo
    {
        public string name;
        public string guid;
        public long localId;
        public bool isReadable;
        public int vertices;
        public long triangles;
        public int subMeshes;
        public string indexFormat;
        public Vector3 boundsCenter;
        public Vector3 boundsSize;
        public string[] attributes;
        public string[] vertexBufferSha256;
        public string indexBufferSha256;
        public SubMeshInfo[] parts;
        public string readError;
    }

    [Serializable]
    public sealed class SubMeshInfo
    {
        public int index;
        public string topology;
        public int indexStart;
        public int indexCount;
        public int baseVertex;
        public int firstVertex;
        public int vertexCount;
    }

    [Serializable]
    public sealed class NodeInfo
    {
        public string path;
        public string globalId;
        public string assetGuid;
        public long localId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public bool active;
        public string meshName;
        public string meshGuid;
        public long meshLocalId;
        public int meshSubMeshes;
        public string[] materialNames;
        public string[] materialReferences;
        public string[] baseMapReferences;
        public string[] componentReferences;
    }

    [Serializable]
    public sealed class LensSelection { public LensGroup[] groups; }

    [Serializable]
    public sealed class LensGroup
    {
        public string side;
        public int expectedTriangles;
        public Corner[] corners;
        public int[] blenderPolygonIndices;
    }

    [Serializable]
    public sealed class Corner { public float x, y, z, u, v; }

    [Serializable]
    public sealed class BuildInfo
    {
        public string backupDirectory;
        public string selectionSha256;
        public int originalTriangles;
        public int generatedTriangles;
        public int originalCarTriangles;
        public int finalCarTriangles;
        public int leftLensTriangles;
        public int rightLensTriangles;
        public int leftBulbTriangles;
        public int rightBulbTriangles;
        public bool rawAttributesPreserved;
        public bool sourceAssetsUnchanged;
        public bool existingObjectAndComponentIdsPreserved;
        public bool sceneDirtyStatesPreserved;
        public MeshInfo[] generatedMeshes;
        public string[] generatedAssets;
    }

    [Serializable]
    public sealed class RepairInfo
    {
        public string backupDirectory;
        public bool existingObjectAndComponentIdsPreserved;
        public bool sceneDirtyStatesPreserved;
    }

    private sealed class RawMesh
    {
        public Mesh original;
        public VertexAttributeDescriptor[] layout;
        public int[] strides;
        public byte[][] streams;
        public Vector3[] positions;
        public Vector2[] uv;
        public List<int[]> parts;
        public int TriangleCount { get { return parts.Sum(part => part.Length / 3); } }
    }

    private sealed class Triangle
    {
        public RawMesh source;
        public int a, b, c;
    }

    static PorscheHeadlightTools()
    {
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < nextCheck)
            return;
        nextCheck = EditorApplication.timeSinceStartup + 0.5;
        if (!File.Exists(RequestPath)) return;

        busy = true;
        Request request = null;
        var result = new Result
        {
            utc = DateTime.UtcNow.ToString("o"),
            unityVersion = Application.unityVersion,
            projectRoot = ProjectRoot
        };
        try
        {
            var json = File.ReadAllText(RequestPath);
            request = JsonUtility.FromJson<Request>(json);
            result.id = request == null ? null : request.id;
            result.operation = request == null ? null : request.operation;
            var validId = request != null && !string.IsNullOrEmpty(request.id) &&
                          Regex.IsMatch(request.id, "^[A-Za-z0-9_-]{1,100}$");
            var consumedDirectory = Path.Combine(WorkRoot, "consumed");
            Directory.CreateDirectory(consumedDirectory);
            var claim = Path.Combine(consumedDirectory,
                validId ? request.id + ".json" : "invalid-" + Guid.NewGuid().ToString("N") + ".json");
            if (File.Exists(claim))
            {
                var duplicate = Path.Combine(consumedDirectory,
                    "duplicate-" + Guid.NewGuid().ToString("N") + ".json");
                File.Move(RequestPath, duplicate);
                result.consumedRequest = duplicate;
                throw new InvalidOperationException("This request ID has already been consumed.");
            }
            File.Move(RequestPath, claim);
            result.consumedRequest = claim;
            if (!validId) throw new InvalidOperationException("A unique request id is required (letters, numbers, _ or -).");
            if (request.operation == "inspect") Inspect(result);
            else if (request.operation == "build") Build(result, request.id);
            else if (request.operation == "fix_headlight_materials") FixHeadlightMaterials(result, request.id);
            else throw new InvalidOperationException("Only explicit inspect, build and fix_headlight_materials operations are supported.");
            result.success = true;
        }
        catch (Exception exception)
        {
            result.error = exception.ToString();
            // Malformed input is also consumed so it cannot replay after a reload.
            if (File.Exists(RequestPath))
            {
                try
                {
                    var directory = Path.Combine(WorkRoot, "consumed");
                    Directory.CreateDirectory(directory);
                    var invalid = Path.Combine(directory, "invalid-" + Guid.NewGuid().ToString("N") + ".json");
                    File.Move(RequestPath, invalid);
                    result.consumedRequest = invalid;
                }
                catch (Exception consumeException) { result.error += "\nConsume failed: " + consumeException.Message; }
            }
        }
        finally
        {
            try
            {
                Directory.CreateDirectory(WorkRoot);
                File.WriteAllText(ResultPath, JsonUtility.ToJson(result, true));
            }
            catch (Exception exception) { Debug.LogError("Porsche headlight result could not be written: " + exception); }
            busy = false;
        }
    }

    private static void Inspect(Result result)
    {
        var scenes = new List<SceneInfo>();
        for (var index = 0; index < SceneManager.sceneCount; index++)
        {
            var scene = SceneManager.GetSceneAt(index);
            scenes.Add(new SceneInfo
            {
                name = scene.name, path = scene.path, isLoaded = scene.isLoaded,
                isDirty = scene.isDirty, handle = scene.handle
            });
        }
        result.scenes = scenes.ToArray();
        result.activeScenePath = SceneManager.GetActiveScene().path;
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        result.prefabStageAssetPath = stage == null ? "" : stage.assetPath;
        result.prefabStageDirty = stage != null && stage.scene.isDirty;
        result.stageNodes = stage == null ? new NodeInfo[0] : DescribeNodes(stage.prefabContentsRoot);

        var assetPaths = new List<string> { ModelPath, PrefabPath };
        assetPaths.AddRange(Directory.GetFiles(Path.Combine(ProjectRoot, AssetRoot, "Material"), "*.mat")
            .Select(ToAssetPath));
        assetPaths.AddRange(Directory.GetFiles(Path.Combine(ProjectRoot, AssetRoot, "Texture"))
            .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)).Select(ToAssetPath));
        result.assets = assetPaths.Select(DescribeAsset).ToArray();
        result.sourceMeshes = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Mesh>().Select(DescribeMesh).ToArray();
        result.sourceNodes = DescribeNodes(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        result.prefabNodes = DescribeNodes(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
    }

    private static void FixHeadlightMaterials(Result result, string requestId)
    {
        Inspect(result);
        var beforeNodes = result.prefabNodes;
        var beforeScenes = result.scenes;
        var beforeAssets = result.assets.Where(asset => asset.path != PrefabPath).ToArray();
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == PrefabPath && stage.scene.isDirty)
        {
            // The earlier owned build can leave the stage dirty after its explicit asset save.
            // Accept that state only while its inspected hierarchy still matches the saved prefab.
            var matchesSavedPrefab = result.stageNodes.Length == beforeNodes.Length && beforeNodes.All(savedNode =>
            {
                var stagedNode = result.stageNodes.SingleOrDefault(node => node.path == savedNode.path);
                return stagedNode != null && stagedNode.active == savedNode.active &&
                    stagedNode.position == savedNode.position && stagedNode.rotation == savedNode.rotation && stagedNode.scale == savedNode.scale &&
                    stagedNode.meshGuid == savedNode.meshGuid && stagedNode.meshLocalId == savedNode.meshLocalId &&
                    (stagedNode.materialReferences ?? new string[0]).SequenceEqual(savedNode.materialReferences ?? new string[0]) &&
                    (stagedNode.componentReferences ?? new string[0]).Length == (savedNode.componentReferences ?? new string[0]).Length;
            });
            if (!matchesSavedPrefab)
                throw new InvalidOperationException("The dirty prefab stage differs from the saved owned build; material repair was not started.");
        }
        var material = AssetDatabase.LoadAssetAtPath<Material>(AssetRoot + "/Material/Headlights.mat");
        if (!material) throw new InvalidOperationException("The generated Headlights material is missing.");

        var backupDirectory = Path.Combine(WorkRoot, "Backup", requestId);
        Directory.CreateDirectory(backupDirectory);
        File.WriteAllText(Path.Combine(backupDirectory, "before.json"), JsonUtility.ToJson(result, true));
        File.Copy(Path.Combine(ProjectRoot, PrefabPath), Path.Combine(backupDirectory, "PorscheV.2.prefab"));
        File.Copy(Path.Combine(ProjectRoot, PrefabPath + ".meta"), Path.Combine(backupDirectory, "PorscheV.2.prefab.meta"));

        var isolated = stage == null || stage.assetPath != PrefabPath;
        var root = isolated ? PrefabUtility.LoadPrefabContents(PrefabPath) : stage.prefabContentsRoot;
        var renderers = new List<MeshRenderer>();
        var previousMaterials = new List<Material[]>();
        var saved = false;
        try
        {
            foreach (var side in new[] { "Left", "Right" })
            {
                var child = root.transform.Find("Headlight_" + side);
                if (!child) throw new InvalidOperationException("The owned Headlight_" + side + " object is missing.");
                var filter = child.GetComponent<MeshFilter>();
                var renderer = child.GetComponent<MeshRenderer>();
                if (!filter || !filter.sharedMesh || filter.sharedMesh.subMeshCount != 1 || !renderer ||
                    AssetDatabase.GetAssetPath(filter.sharedMesh) != AssetRoot + "/Meshes/Headlight_" + side + ".asset")
                    throw new InvalidOperationException("Unexpected headlight mesh or renderer; material repair was refused.");
                renderers.Add(renderer);
                previousMaterials.Add(renderer.sharedMaterials);
            }
            foreach (var renderer in renderers)
            {
                Undo.RecordObject(renderer, "Repair Porsche headlight material slots");
                renderer.sharedMaterials = new[] { material };
            }
            ValidateMaterialSlots(root);
            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out success);
            if (!success) throw new InvalidOperationException("Saving the material repair failed.");
            saved = true;
            Inspect(result);
            var idsPreserved = beforeNodes.All(before =>
            {
                var after = result.prefabNodes.SingleOrDefault(node => node.path == before.path);
                return after != null && before.localId == after.localId && before.assetGuid == after.assetGuid &&
                    before.position == after.position && before.rotation == after.rotation && before.scale == after.scale &&
                    before.meshGuid == after.meshGuid && before.meshLocalId == after.meshLocalId &&
                    (before.componentReferences ?? new string[0]).SequenceEqual(after.componentReferences ?? new string[0]);
            });
            var sceneStatesPreserved = beforeScenes.All(before =>
            {
                var scene = FindLoadedScene(before.handle);
                return scene.IsValid() && scene.path == before.path && scene.isDirty == before.isDirty;
            });
            var assetsPreserved = beforeAssets.All(before => FileHash(Path.Combine(ProjectRoot, before.path)) == before.sha256 &&
                FileHash(Path.Combine(ProjectRoot, before.path) + ".meta") == before.metaSha256);
            var headlightsValid = result.prefabNodes.Where(node => node.path == root.name + "/Headlight_Left" ||
                node.path == root.name + "/Headlight_Right").All(node => node.meshSubMeshes == 1 && node.materialReferences.Length == 1 &&
                    node.materialReferences[0] == ObjectReference(material));
            if (!idsPreserved || !sceneStatesPreserved || !assetsPreserved || !headlightsValid)
                throw new InvalidOperationException("Material repair preservation validation failed.");
            result.repair = new RepairInfo
            {
                backupDirectory = backupDirectory, existingObjectAndComponentIdsPreserved = idsPreserved,
                sceneDirtyStatesPreserved = sceneStatesPreserved
            };
            if (!isolated) Selection.activeGameObject = root.transform.Find("Headlight_Left").gameObject;
        }
        catch
        {
            for (var index = 0; index < renderers.Count; index++)
                if (renderers[index]) renderers[index].sharedMaterials = previousMaterials[index];
            if (saved)
            {
                File.Copy(Path.Combine(backupDirectory, "PorscheV.2.prefab"), Path.Combine(ProjectRoot, PrefabPath), true);
                AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            }
            throw;
        }
        finally { if (isolated && root) PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ValidateMaterialSlots(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh && renderer.sharedMaterials.Length != filter.sharedMesh.subMeshCount)
                throw new InvalidOperationException("Renderer material slots differ from mesh submeshes: " + renderer.name +
                    " materials=" + renderer.sharedMaterials.Length + ", submeshes=" + filter.sharedMesh.subMeshCount);
        }
    }

    private static void Build(Result result, string requestId)
    {
        Inspect(result);
        var beforeScenes = result.scenes;
        var beforePrefabNodes = result.prefabNodes;
        var unchangedAssets = result.assets.Where(asset => asset.path != PrefabPath).ToArray();
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == PrefabPath && stage.scene.isDirty)
            throw new InvalidOperationException("The matching prefab stage contains unsaved edits; build was not started.");

        var selectionPath = Path.Combine(WorkRoot, "lens_selection.json");
        var selection = JsonUtility.FromJson<LensSelection>(File.ReadAllText(selectionPath));
        if (selection == null || selection.groups == null || selection.groups.Length != 2 ||
            selection.groups.Count(group => group.side == "Left") != 1 ||
            selection.groups.Count(group => group.side == "Right") != 1 ||
            selection.groups.Any(group => group.expectedTriangles < 1 || group.corners == null || group.corners.Length < 3))
            throw new InvalidOperationException("Lens selection must contain exactly Left and Right with expected triangle counts and position/UV corners.");

        var imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (!imported) throw new InvalidOperationException("The original FBX is not available.");
        var originalBody = imported.transform.Find("Body").GetComponent<MeshFilter>().sharedMesh;
        var originalLight = imported.transform.Find("Light").GetComponent<MeshFilter>().sharedMesh;
        var body = ReadRawMesh(originalBody);
        var light = ReadRawMesh(originalLight);
        RequireSameLayout(body, light);
        if (body.parts.Count != 5 || light.parts.Count != 1)
            throw new InvalidOperationException("Unexpected source material/submesh layout.");

        var leftLens = new List<Triangle>();
        var rightLens = new List<Triangle>();
        var bodyParts = Enumerable.Range(0, body.parts.Count).Select(index => new List<Triangle>()).ToArray();
        foreach (var partIndex in Enumerable.Range(0, body.parts.Count))
        {
            foreach (var triangle in Triangles(body, body.parts[partIndex]))
            {
                var matches = selection.groups.Where(group => Matches(group, body, triangle.a) &&
                    Matches(group, body, triangle.b) && Matches(group, body, triangle.c)).ToArray();
                if (matches.Length > 1) throw new InvalidOperationException("A lens triangle matched both sides.");
                if (matches.Length == 0) bodyParts[partIndex].Add(triangle);
                else
                {
                    if (partIndex != 0) throw new InvalidOperationException("A selected lens triangle uses a non-atlas material slot.");
                    if (matches[0].side == "Left") leftLens.Add(triangle); else rightLens.Add(triangle);
                }
            }
        }
        if (leftLens.Count != selection.groups.Single(group => group.side == "Left").expectedTriangles ||
            rightLens.Count != selection.groups.Single(group => group.side == "Right").expectedTriangles)
            throw new InvalidOperationException("Selected lens triangle counts differ from the approved classifier: Left=" + leftLens.Count + ", Right=" + rightLens.Count);

        var rear = new List<Triangle>();
        var leftBulbs = new List<Triangle>();
        var rightBulbs = new List<Triangle>();
        foreach (var triangle in Triangles(light, light.parts[0]))
        {
            var centroid = (light.positions[triangle.a] + light.positions[triangle.b] + light.positions[triangle.c]) / 3f;
            if (centroid.z <= 1f) { rear.Add(triangle); continue; }
            foreach (var vertex in new[] { triangle.a, triangle.b, triangle.c })
            {
                var position = light.positions[vertex];
                if (Mathf.Abs(position.x) < .58f || Mathf.Abs(position.x) > .70f ||
                    position.y < .64f || position.y > .75f || position.z < 1.66f || position.z > 1.73f)
                    throw new InvalidOperationException("A selected front bulb is outside the inspected front-light bounds: " + position);
                if (Mathf.Sign(position.x) != Mathf.Sign(centroid.x))
                    throw new InvalidOperationException("A bulb triangle crosses the car center.");
            }
            if (centroid.x < 0) leftBulbs.Add(triangle); else rightBulbs.Add(triangle);
        }
        if (rear.Count < 1 || leftBulbs.Count != 148 || rightBulbs.Count != 148 ||
            ComponentCount(leftBulbs) != 1 || ComponentCount(rightBulbs) != 1)
            throw new InvalidOperationException("Expected two distinct connected front bulbs and remaining rear lights.");

        // Everything above is read-only. Prepare all output meshes and verify their bytes before touching assets or the prefab.
        var generated = new[]
        {
            CreateDerivedMesh("Body_HeadlightsSeparated", bodyParts, null),
            CreateDerivedMesh("Light_RearOnly", new[] { rear }, null),
            CreateDerivedMesh("Headlight_Left", new[] { leftLens.Concat(leftBulbs).ToList() }, null),
            CreateDerivedMesh("Headlight_Right", new[] { rightLens.Concat(rightBulbs).ToList() }, null)
        };
        var meshPaths = generated.Select(mesh => AssetRoot + "/Meshes/" + mesh.name + ".asset").ToArray();
        var materialPath = AssetRoot + "/Material/Headlights.mat";
        var expectedTriangles = body.TriangleCount + light.TriangleCount;
        if (generated.Sum(mesh => (int)DescribeMesh(mesh).triangles) != expectedTriangles)
            throw new InvalidOperationException("Triangle conservation failed before asset changes.");

        var backupDirectory = Path.Combine(WorkRoot, "Backup", requestId);
        Directory.CreateDirectory(backupDirectory);
        File.WriteAllText(Path.Combine(backupDirectory, "before.json"), JsonUtility.ToJson(result, true));
        var backupFiles = new List<string> { PrefabPath, PrefabPath + ".meta" };
        backupFiles.AddRange(meshPaths.SelectMany(path => new[] { path, path + ".meta" }));
        backupFiles.AddRange(new[] { materialPath, materialPath + ".meta" });
        foreach (var path in backupFiles.Where(path => File.Exists(Path.Combine(ProjectRoot, path))))
        {
            var destination = Path.Combine(backupDirectory, path);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(Path.Combine(ProjectRoot, path), destination, false);
        }
        // saveAsCopy preserves the current scene path and unsaved-work flag.
        foreach (var info in beforeScenes)
        {
            var scene = FindLoadedScene(info.handle);
            if (!scene.IsValid() || !scene.isLoaded) continue;
            var destination = Path.Combine(backupDirectory, "Scene_" + info.handle.ToString().Replace('-', 'N') + ".unity");
            if (!EditorSceneManager.SaveScene(scene, destination, true) || scene.isDirty != info.isDirty || scene.path != info.path)
                throw new InvalidOperationException("Could not snapshot a loaded scene while retaining its original state.");
        }

        GameObject root = null;
        var isolatedContents = false;
        MeshFilter bodyFilter = null, lightFilter = null;
        Mesh previousBody = null, previousLight = null;
        var addedChildren = new List<GameObject>();
        var savedPrefab = false;
        try
        {
            root = stage != null && stage.assetPath == PrefabPath ? stage.prefabContentsRoot : PrefabUtility.LoadPrefabContents(PrefabPath);
            isolatedContents = stage == null || stage.assetPath != PrefabPath;
            if (!root) throw new InvalidOperationException("Could not load the requested prefab.");
            bodyFilter = root.transform.Find("Body").GetComponent<MeshFilter>();
            lightFilter = root.transform.Find("Light").GetComponent<MeshFilter>();
            previousBody = bodyFilter.sharedMesh;
            previousLight = lightFilter.sharedMesh;
            if (root.transform.Find("Headlight_Left") || root.transform.Find("Headlight_Right"))
                throw new InvalidOperationException("Headlight children already exist; inspect the current prefab before rebuilding.");
            foreach (var transform in root.GetComponentsInChildren<Transform>(true).Where(transform => transform != root.transform))
                if (transform.localPosition.sqrMagnitude > 1e-12f || Quaternion.Angle(transform.localRotation, Quaternion.identity) > .001f ||
                    (transform.localScale - Vector3.one).sqrMagnitude > 1e-12f)
                    throw new InvalidOperationException("Unexpected existing child transform; build refused to change alignment.");

            var material = new Material(AssetDatabase.LoadAssetAtPath<Material>(AssetRoot + "/Material/tripo_mat_c3595359.001.mat"));
            material.name = "Headlights";
            material.SetTexture("_EmissionMap", material.GetTexture("_BaseMap"));
            material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
            EnsureAssetFolder(AssetRoot + "/Meshes");
            for (var index = 0; index < generated.Length; index++) generated[index] = SaveOwnedMesh(generated[index], meshPaths[index]);
            var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existingMaterial)
            {
                EditorUtility.CopySerialized(material, existingMaterial);
                UnityEngine.Object.DestroyImmediate(material);
                material = existingMaterial;
                EditorUtility.SetDirty(material);
            }
            else AssetDatabase.CreateAsset(material, materialPath);
            foreach (var mesh in generated) AssetDatabase.SaveAssetIfDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(material);

            Undo.RecordObject(bodyFilter, "Separate Porsche headlights");
            Undo.RecordObject(lightFilter, "Separate Porsche headlights");
            bodyFilter.sharedMesh = generated[0];
            lightFilter.sharedMesh = generated[1];
            var bodyRenderer = bodyFilter.GetComponent<MeshRenderer>();
            for (var side = 0; side < 2; side++)
            {
                var child = new GameObject(side == 0 ? "Headlight_Left" : "Headlight_Right");
                SceneManager.MoveGameObjectToScene(child, root.scene);
                child.transform.SetParent(root.transform, false);
                child.layer = bodyFilter.gameObject.layer;
                var filter = child.AddComponent<MeshFilter>();
                filter.sharedMesh = generated[side + 2];
                var renderer = child.AddComponent<MeshRenderer>();
                CopyRendererSettings(bodyRenderer, renderer);
                renderer.sharedMaterials = new[] { material };
                addedChildren.Add(child);
                Undo.RegisterCreatedObjectUndo(child, "Separate Porsche headlights");
            }
            ValidateMaterialSlots(root);
            // Save only this prefab. The open main scene is never saved over its original path.
            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out success);
            if (!success) throw new InvalidOperationException("Saving the separated-headlight prefab failed.");
            savedPrefab = true;
            foreach (var mesh in generated) AssetDatabase.SaveAssetIfDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(material);

            Inspect(result);
            ValidateMaterialSlots(root);
            var idsPreserved = beforePrefabNodes.All(before =>
            {
                var after = result.prefabNodes.SingleOrDefault(node => node.path == before.path);
                return after != null && before.localId == after.localId && before.assetGuid == after.assetGuid &&
                    (before.componentReferences ?? new string[0]).SequenceEqual(after.componentReferences ?? new string[0]);
            });
            var sourcesUnchanged = unchangedAssets.All(before => FileHash(Path.Combine(ProjectRoot, before.path)) == before.sha256 &&
                FileHash(Path.Combine(ProjectRoot, before.path) + ".meta") == before.metaSha256);
            var dirtyStatesPreserved = beforeScenes.All(before =>
            {
                var scene = FindLoadedScene(before.handle);
                return scene.IsValid() && scene.isDirty == before.isDirty && scene.path == before.path;
            });
            var generatedInfo = generated.Select(DescribeMesh).ToArray();
            if (!idsPreserved || !sourcesUnchanged || !dirtyStatesPreserved || generatedInfo.Sum(info => info.triangles) != expectedTriangles)
                throw new InvalidOperationException("Post-save validation failed: original IDs=" + idsPreserved +
                    ", source files=" + sourcesUnchanged + ", scene states=" + dirtyStatesPreserved);
            result.build = new BuildInfo
            {
                backupDirectory = backupDirectory, selectionSha256 = FileHash(selectionPath),
                originalTriangles = expectedTriangles, generatedTriangles = (int)generatedInfo.Sum(info => info.triangles),
                originalCarTriangles = (int)result.sourceMeshes.Sum(info => info.triangles),
                finalCarTriangles = (int)result.sourceMeshes.Where(info => info.name != "Body" && info.name != "Light").Sum(info => info.triangles) + expectedTriangles,
                leftLensTriangles = leftLens.Count, rightLensTriangles = rightLens.Count,
                leftBulbTriangles = leftBulbs.Count, rightBulbTriangles = rightBulbs.Count,
                rawAttributesPreserved = true, sourceAssetsUnchanged = sourcesUnchanged,
                existingObjectAndComponentIdsPreserved = idsPreserved, sceneDirtyStatesPreserved = dirtyStatesPreserved,
                generatedMeshes = generatedInfo, generatedAssets = meshPaths.Concat(new[] { materialPath, PrefabPath }).ToArray()
            };
            if (stage != null && stage.assetPath == PrefabPath) Selection.activeGameObject = root.transform.Find("Headlight_Left").gameObject;
        }
        catch
        {
            if (bodyFilter) bodyFilter.sharedMesh = previousBody;
            if (lightFilter) lightFilter.sharedMesh = previousLight;
            foreach (var child in addedChildren.Where(child => child)) UnityEngine.Object.DestroyImmediate(child);
            foreach (var path in backupFiles)
            {
                var backup = Path.Combine(backupDirectory, path);
                var destination = Path.Combine(ProjectRoot, path);
                if (File.Exists(backup)) File.Copy(backup, destination, true);
                else if (path != PrefabPath && path != PrefabPath + ".meta" && File.Exists(destination)) File.Delete(destination);
            }
            if (savedPrefab) AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            foreach (var path in meshPaths.Concat(new[] { materialPath })) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            throw;
        }
        finally
        {
            if (isolatedContents && root) PrefabUtility.UnloadPrefabContents(root);
            foreach (var mesh in generated.Where(mesh => mesh && !AssetDatabase.Contains(mesh))) UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    private static RawMesh ReadRawMesh(Mesh mesh)
    {
        var raw = new RawMesh { original = mesh, layout = mesh.GetVertexAttributes(), parts = new List<int[]>() };
        using (var array = MeshUtility.AcquireReadOnlyMeshData(mesh))
        {
            var data = array[0];
            raw.streams = new byte[data.vertexBufferCount][];
            raw.strides = new int[data.vertexBufferCount];
            for (var stream = 0; stream < data.vertexBufferCount; stream++)
            {
                raw.streams[stream] = data.GetVertexData<byte>(stream).ToArray();
                raw.strides[stream] = mesh.GetVertexBufferStride(stream);
            }
            using (var vertices = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp))
            using (var uv = new NativeArray<Vector2>(data.vertexCount, Allocator.Temp))
            {
                data.GetVertices(vertices);
                data.GetUVs(0, uv);
                raw.positions = vertices.ToArray();
                raw.uv = uv.ToArray();
            }
            var indices = data.indexFormat == IndexFormat.UInt16 ?
                data.GetIndexData<ushort>().ToArray().Select(index => (int)index).ToArray() :
                data.GetIndexData<uint>().ToArray().Select(index => checked((int)index)).ToArray();
            for (var subMesh = 0; subMesh < data.subMeshCount; subMesh++)
            {
                var part = data.GetSubMesh(subMesh);
                if (part.topology != MeshTopology.Triangles || part.indexCount % 3 != 0)
                    throw new InvalidOperationException("Only triangle source meshes are supported.");
                raw.parts.Add(indices.Skip(part.indexStart).Take(part.indexCount).Select(index => index + part.baseVertex).ToArray());
            }
        }
        return raw;
    }

    private static Scene FindLoadedScene(int handle)
    {
        for (var index = 0; index < SceneManager.sceneCount; index++)
        {
            var scene = SceneManager.GetSceneAt(index);
            if (scene.handle == handle) return scene;
        }
        return new Scene();
    }

    private static IEnumerable<Triangle> Triangles(RawMesh source, int[] indices)
    {
        for (var index = 0; index < indices.Length; index += 3)
            yield return new Triangle { source = source, a = indices[index], b = indices[index + 1], c = indices[index + 2] };
    }

    private static bool Matches(LensGroup group, RawMesh source, int index)
    {
        var position = source.positions[index];
        var uv = source.uv[index];
        return group.corners.Any(corner => Mathf.Abs(corner.x - position.x) <= .00002f &&
            Mathf.Abs(corner.y - position.y) <= .00002f && Mathf.Abs(corner.z - position.z) <= .00002f &&
            Mathf.Abs(corner.u - uv.x) <= .000002f && Mathf.Abs(corner.v - uv.y) <= .000002f);
    }

    private static int ComponentCount(List<Triangle> triangles)
    {
        // Position keys bridge imported UV/normal splits without changing any source vertices.
        Func<int, string> key = index =>
        {
            var p = triangles[0].source.positions[index];
            return Mathf.RoundToInt(p.x * 100000f) + ":" + Mathf.RoundToInt(p.y * 100000f) + ":" + Mathf.RoundToInt(p.z * 100000f);
        };
        var adjacency = new Dictionary<string, HashSet<string>>();
        foreach (var triangle in triangles)
        {
            var keys = new[] { key(triangle.a), key(triangle.b), key(triangle.c) };
            foreach (var vertex in keys)
            {
                if (!adjacency.ContainsKey(vertex)) adjacency[vertex] = new HashSet<string>();
                foreach (var neighbour in keys) adjacency[vertex].Add(neighbour);
            }
        }
        var visited = new HashSet<string>();
        var count = 0;
        foreach (var vertex in adjacency.Keys)
        {
            if (!visited.Add(vertex)) continue;
            count++;
            var queue = new Queue<string>();
            queue.Enqueue(vertex);
            while (queue.Count > 0)
                foreach (var neighbour in adjacency[queue.Dequeue()]) if (visited.Add(neighbour)) queue.Enqueue(neighbour);
        }
        return count;
    }

    private static void RequireSameLayout(RawMesh first, RawMesh second)
    {
        if (!first.layout.SequenceEqual(second.layout) || !first.strides.SequenceEqual(second.strides))
            throw new InvalidOperationException("Source vertex layouts differ; exact raw attribute combination is not supported.");
    }

    private static Mesh CreateDerivedMesh(string name, List<Triangle>[] parts, RawMesh retainAllVertices)
    {
        var triangles = parts.SelectMany(part => part).ToArray();
        if (triangles.Length == 0) throw new InvalidOperationException("A generated mesh would be empty.");
        var layout = triangles[0].source;
        foreach (var source in triangles.Select(triangle => triangle.source).Distinct()) RequireSameLayout(layout, source);
        var vertices = new List<Tuple<RawMesh, int>>();
        var lookup = new Dictionary<Tuple<RawMesh, int>, int>();
        if (retainAllVertices != null)
            for (var index = 0; index < retainAllVertices.positions.Length; index++)
            {
                var key = Tuple.Create(retainAllVertices, index);
                lookup.Add(key, vertices.Count);
                vertices.Add(key);
            }
        var outputParts = new List<int[]>();
        foreach (var part in parts)
        {
            var indices = new List<int>();
            foreach (var triangle in part)
                foreach (var sourceIndex in new[] { triangle.a, triangle.b, triangle.c })
                {
                    var key = Tuple.Create(triangle.source, sourceIndex);
                    int outputIndex;
                    if (!lookup.TryGetValue(key, out outputIndex))
                    {
                        outputIndex = vertices.Count;
                        vertices.Add(key);
                        lookup.Add(key, outputIndex);
                    }
                    indices.Add(outputIndex);
                }
            outputParts.Add(indices.ToArray());
        }
        var mesh = new Mesh { name = name };
        mesh.SetVertexBufferParams(vertices.Count, layout.layout);
        for (var stream = 0; stream < layout.streams.Length; stream++)
        {
            if (mesh.GetVertexBufferStride(stream) != layout.strides[stream])
                throw new InvalidOperationException("Generated vertex stride differs from the original source.");
            var bytes = new byte[vertices.Count * layout.strides[stream]];
            for (var index = 0; index < vertices.Count; index++)
            {
                var vertex = vertices[index];
                Buffer.BlockCopy(vertex.Item1.streams[stream], vertex.Item2 * layout.strides[stream],
                    bytes, index * layout.strides[stream], layout.strides[stream]);
            }
            mesh.SetVertexBufferData(bytes, 0, 0, bytes.Length, stream, MeshUpdateFlags.DontRecalculateBounds);
            using (var data = MeshUtility.AcquireReadOnlyMeshData(mesh))
                if (!data[0].GetVertexData<byte>(stream).ToArray().SequenceEqual(bytes))
                    throw new InvalidOperationException("Raw vertex attributes changed while creating a derived mesh.");
        }
        var outputIndices = outputParts.SelectMany(part => part).ToArray();
        var format = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetIndexBufferParams(outputIndices.Length, format);
        if (format == IndexFormat.UInt16) mesh.SetIndexBufferData(outputIndices.Select(index => checked((ushort)index)).ToArray(), 0, 0, outputIndices.Length);
        else mesh.SetIndexBufferData(outputIndices.Select(index => (uint)index).ToArray(), 0, 0, outputIndices.Length);
        mesh.subMeshCount = parts.Length;
        var totalBounds = new Bounds(triangles[0].source.positions[triangles[0].a], Vector3.zero);
        var start = 0;
        for (var subMesh = 0; subMesh < outputParts.Count; subMesh++)
        {
            var part = outputParts[subMesh];
            var bounds = part.Length == 0 ? new Bounds() : new Bounds(vertices[part[0]].Item1.positions[vertices[part[0]].Item2], Vector3.zero);
            foreach (var index in part)
            {
                var position = vertices[index].Item1.positions[vertices[index].Item2];
                bounds.Encapsulate(position);
                totalBounds.Encapsulate(position);
            }
            mesh.SetSubMesh(subMesh, new SubMeshDescriptor(start, part.Length, MeshTopology.Triangles)
            {
                bounds = bounds, firstVertex = 0, vertexCount = vertices.Count, baseVertex = 0
            }, MeshUpdateFlags.DontRecalculateBounds);
            start += part.Length;
        }
        mesh.bounds = totalBounds;
        return mesh;
    }

    private static void EnsureAssetFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureAssetFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }

    private static Mesh SaveOwnedMesh(Mesh generated, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!existing) { AssetDatabase.CreateAsset(generated, path); return generated; }
        EditorUtility.CopySerialized(generated, existing);
        UnityEngine.Object.DestroyImmediate(generated);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static void CopyRendererSettings(MeshRenderer source, MeshRenderer destination)
    {
        var original = new SerializedObject(source);
        var target = new SerializedObject(destination);
        var property = original.GetIterator();
        var excluded = new HashSet<string>
        {
            "m_GameObject", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset", "m_ObjectHideFlags"
        };
        if (property.Next(true))
            do { if (!excluded.Contains(property.propertyPath)) target.CopyFromSerializedProperty(property); }
            while (property.Next(false));
        target.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string ToAssetPath(string path)
    {
        return Path.GetFullPath(path).Substring(ProjectRoot.Length + 1).Replace('\\', '/');
    }

    private static AssetInfo DescribeAsset(string path)
    {
        var absolute = Path.Combine(ProjectRoot, path);
        return new AssetInfo
        {
            path = path, guid = AssetDatabase.AssetPathToGUID(path),
            length = File.Exists(absolute) ? new FileInfo(absolute).Length : 0,
            sha256 = FileHash(absolute), metaSha256 = FileHash(absolute + ".meta")
        };
    }

    private static MeshInfo DescribeMesh(Mesh mesh)
    {
        var info = new MeshInfo
        {
            name = mesh.name, isReadable = mesh.isReadable, vertices = mesh.vertexCount,
            subMeshes = mesh.subMeshCount, indexFormat = mesh.indexFormat.ToString(),
            boundsCenter = mesh.bounds.center, boundsSize = mesh.bounds.size,
            attributes = mesh.GetVertexAttributes().Select(attribute => attribute.ToString()).ToArray()
        };
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out info.guid, out info.localId);
        try
        {
            using (var dataArray = MeshUtility.AcquireReadOnlyMeshData(mesh))
            {
                var data = dataArray[0];
                var streams = new string[data.vertexBufferCount];
                for (var stream = 0; stream < streams.Length; stream++)
                    streams[stream] = BytesHash(data.GetVertexData<byte>(stream).ToArray());
                info.vertexBufferSha256 = streams;
                info.indexBufferSha256 = BytesHash(data.GetIndexData<byte>().ToArray());
                var parts = new List<SubMeshInfo>();
                for (var index = 0; index < data.subMeshCount; index++)
                {
                    var part = data.GetSubMesh(index);
                    parts.Add(new SubMeshInfo
                    {
                        index = index, topology = part.topology.ToString(), indexStart = part.indexStart,
                        indexCount = part.indexCount, baseVertex = part.baseVertex,
                        firstVertex = part.firstVertex, vertexCount = part.vertexCount
                    });
                    if (part.topology == MeshTopology.Triangles) info.triangles += part.indexCount / 3;
                }
                info.parts = parts.ToArray();
            }
        }
        catch (Exception exception) { info.readError = exception.ToString(); }
        return info;
    }

    private static NodeInfo[] DescribeNodes(GameObject root)
    {
        if (!root) return new NodeInfo[0];
        return root.GetComponentsInChildren<Transform>(true).Select(transform =>
        {
            var gameObject = transform.gameObject;
            var info = new NodeInfo
            {
                path = transform == root.transform ? root.name : root.name + "/" + AnimationUtility.CalculateTransformPath(transform, root.transform),
                globalId = GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString(),
                position = transform.localPosition, rotation = transform.localRotation,
                scale = transform.localScale, active = gameObject.activeSelf
            };
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(gameObject, out info.assetGuid, out info.localId);
            info.componentReferences = gameObject.GetComponents<Component>().Select(ObjectReference).ToArray();
            var filter = gameObject.GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh)
            {
                info.meshName = filter.sharedMesh.name;
                info.meshSubMeshes = filter.sharedMesh.subMeshCount;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(filter.sharedMesh, out info.meshGuid, out info.meshLocalId);
            }
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer)
            {
                var materials = renderer.sharedMaterials;
                info.materialNames = materials.Select(material => material ? material.name : "").ToArray();
                info.materialReferences = materials.Select(ObjectReference).ToArray();
                info.baseMapReferences = materials.Select(material =>
                    material && material.HasProperty("_BaseMap") ? ObjectReference(material.GetTexture("_BaseMap")) : "").ToArray();
            }
            return info;
        }).ToArray();
    }

    private static string ObjectReference(UnityEngine.Object value)
    {
        if (!value) return "";
        string guid;
        long localId;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out guid, out localId);
        return guid + ":" + localId + ":" + AssetDatabase.GetAssetPath(value);
    }

    private static string FileHash(string path)
    {
        if (!File.Exists(path)) return "";
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(stream));
    }

    private static string BytesHash(byte[] bytes)
    {
        using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(bytes));
    }

    private static string Hex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
