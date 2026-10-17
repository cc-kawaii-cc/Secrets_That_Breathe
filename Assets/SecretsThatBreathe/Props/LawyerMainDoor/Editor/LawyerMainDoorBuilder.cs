using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SecretsThatBreathe.LawyerMainDoor.Editor
{
    // A project-scoped file bridge. It only exposes the four door operations below.
    [InitializeOnLoad]
    public static class LawyerMainDoorBuilder
    {
        public const string Root = "Assets/SecretsThatBreathe/Props/LawyerMainDoor";
        const string ModelPath = Root + "/Models/LawyerMainDoor.fbx";
        const string PrefabPath = Root + "/Prefabs/LawyerMainDoor.prefab";
        const string OwnedName = "MainDoor_LowPoly";
        const string SessionKey = "STB.LawyerMainDoor.LastRequest";
        static string Project => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string Source => Path.Combine(Project, "ArtSource/LawyerMainDoor");
        static string RequestPath => Path.Combine(Project, "Tools/LawyerMainDoor/bridge-request.json");
        static string ResultPath => Path.Combine(Source, "bridge-result.json");
        static double nextPoll;
        static bool busy;
        static readonly string[] Names = { "LM_Wood", "LM_PanelInset", "LM_Brass", "LM_DarkMetal", "LM_Plaque" };

        static LawyerMainDoorBuilder()
        {
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + .7;
            JObject request;
            try
            {
                if (!File.Exists(RequestPath)) return;
                request = JObject.Parse(File.ReadAllText(RequestPath));
            }
            catch { return; }
            string id = (string)request["requestId"];
            if (string.IsNullOrEmpty(id)) return;
            if (id == SessionState.GetString(SessionKey, ""))
            {
                try { if (File.Exists(ResultPath) && (string)JObject.Parse(File.ReadAllText(ResultPath))["requestId"] == id) ConsumeRequest(id); } catch { }
                return;
            }
            SessionState.SetString(SessionKey, id);
            busy = true;
            bool recorded = false;
            try
            {
                Directory.CreateDirectory(Source);
                string op = ((string)request["operation"] ?? "").ToLowerInvariant();
                object data;
                switch (op)
                {
                    case "inspect": data = Inspect(true); SessionState.SetBool(SessionKey + ".Inspected", true); break;
                    case "build": data = Build(); break;
                    case "apply": data = Apply((bool?)request["saveScene"] ?? false); break;
                    case "preview": data = Preview(); break;
                    case "finalize": data = FinalizeScene(); break;
                    default: throw new InvalidOperationException("Only inspect, build, apply, preview, and finalize are supported.");
                }
                Write(ResultPath, new { requestId = id, operation = op, success = true, utc = DateTime.UtcNow.ToString("o"), data });
                recorded = true;
                Debug.Log("[LawyerMainDoor] " + op + " completed: " + id);
            }
            catch (Exception ex)
            {
                Write(ResultPath, new { requestId = id, success = false, error = ex.ToString(), utc = DateTime.UtcNow.ToString("o") });
                recorded = true;
                Debug.LogError("[LawyerMainDoor] " + ex);
            }
            finally { if (recorded) ConsumeRequest(id); busy = false; }
        }

        static void ConsumeRequest(string id)
        {
            try
            {
                if (File.Exists(RequestPath) && (string)JObject.Parse(File.ReadAllText(RequestPath))["requestId"] == id) File.Delete(RequestPath);
            }
            catch (Exception ex) { Debug.LogWarning("[LawyerMainDoor] Could not consume request " + id + ": " + ex.Message); }
        }

        static void Write(string path, object value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented));
        }

        static GameObject Door()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Lawyer Room") throw new InvalidOperationException("The active scene must already be Lawyer Room. This tool never opens scenes.");
            var found = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Where(x => x.name == "Main Door").ToArray();
            if (found.Length != 1) throw new InvalidOperationException("Expected exactly one Main Door, found " + found.Length);
            return found[0].gameObject;
        }

        static string Id(Object obj) => obj ? GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString() : null;
        static float[] V(Vector3 v) => new[] { v.x, v.y, v.z };
        static float[] Q(Quaternion q) => new[] { q.x, q.y, q.z, q.w };
        static string Hierarchy(Transform t) => t.parent ? Hierarchy(t.parent) + "/" + t.name : t.name;

        static object Snapshot(GameObject go)
        {
            return new
            {
                name = go.name, path = Hierarchy(go.transform), id = Id(go), instanceId = go.GetInstanceID(), layer = go.layer,
                active = go.activeSelf, staticFlags = (int)GameObjectUtility.GetStaticEditorFlags(go),
                localPosition = V(go.transform.localPosition), localRotation = Q(go.transform.localRotation), localScale = V(go.transform.localScale),
                worldPosition = V(go.transform.position), worldRotation = Q(go.transform.rotation), lossyScale = V(go.transform.lossyScale),
                components = go.GetComponents<Component>().Select(c => new { type = c ? c.GetType().FullName : "Missing", id = Id(c), instanceId = c ? c.GetInstanceID() : 0, json = c ? EditorJsonUtility.ToJson(c, false) : null }).ToArray(),
                descendants = go.GetComponentsInChildren<Transform>(true).Where(t => t != go.transform).Select(t => new { name = t.name, path = Hierarchy(t), id = Id(t.gameObject), localPosition = V(t.localPosition), localRotation = Q(t.localRotation), localScale = V(t.localScale), componentTypes = t.GetComponents<Component>().Select(c => c ? c.GetType().FullName : "Missing").ToArray() }).ToArray(),
                renderers = go.GetComponentsInChildren<Renderer>(true).Select(r => new { name = r.name, enabled = r.enabled, boundsCenter = V(r.bounds.center), boundsSize = V(r.bounds.size), materials = r.sharedMaterials.Select(m => new { name = m ? m.name : null, asset = AssetDatabase.GetAssetPath(m), shader = m && m.shader ? m.shader.name : null }).ToArray() }).ToArray()
            };
        }

        static object Inspect(bool baseline)
        {
            var go = Door(); var scene = go.scene; bool dirtyBefore = scene.isDirty;
            string backup = Path.Combine(Source, "LawyerRoom_Before_MainDoor.unity");
            if (baseline && !File.Exists(backup))
            {
                if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new InvalidOperationException("Could not save the original scene backup copy.");
            }
            if (baseline && !File.Exists(Path.Combine(Source, "Before_InScene.png"))) Capture(go, Path.Combine(Source, "Before_InScene.png"), 0);
            var nearby = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Renderer>(true))
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && Vector3.Distance(r.bounds.ClosestPoint(go.transform.position), go.transform.position) < 5f)
                .OrderBy(r => Vector3.Distance(r.bounds.center, go.transform.position)).Take(90)
                .Select(r => new { path = Hierarchy(r.transform), id = Id(r.gameObject), boundsCenter = V(r.bounds.center), boundsSize = V(r.bounds.size), materials = r.sharedMaterials.Select(m => new { name = m ? m.name : null, shader = m && m.shader ? m.shader.name : null, asset = AssetDatabase.GetAssetPath(m) }).ToArray() }).ToArray();
            var data = new { scene = scene.path, sceneName = scene.name, dirtyBefore, dirtyAfter = scene.isDirty, selection = Selection.objects.Select(x => new { name = x.name, id = Id(x) }).ToArray(), door = Snapshot(go), nearby, backup, beforeImage = Path.Combine(Source, "Before_InScene.png") };
            if (baseline && !File.Exists(Path.Combine(Source, "Before_Inspection.json"))) Write(Path.Combine(Source, "Before_Inspection.json"), data);
            return data;
        }

        static Material[] BuildMaterials()
        {
            Directory.CreateDirectory(Root + "/Materials");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit shader is unavailable.");
            var mats = new List<Material>();
            foreach (string name in Names)
            {
                string p = Root + "/Materials/" + name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (!mat) { mat = new Material(shader) { name = name }; AssetDatabase.CreateAsset(mat, p); }
                mat.shader = shader;
                Color tint = Color.white; float metal = 0, smooth = .24f;
                if (name == "LM_PanelInset") tint = new Color(.72f, .69f, .64f, 1);
                if (name == "LM_Brass") { tint = new Color(.48f, .31f, .125f, 1); metal = .72f; smooth = .32f; }
                if (name == "LM_DarkMetal") { tint = new Color(.105f, .092f, .073f, 1); metal = .65f; smooth = .25f; }
                mat.SetColor("_BaseColor", tint); mat.SetFloat("_Metallic", metal); mat.SetFloat("_Smoothness", smooth);
                string texture = name == "LM_Plaque" ? "Door_Plaque.png" : name == "LM_Wood" || name == "LM_PanelInset" ? "Door_Wood_BaseColor.png" : null;
                if (texture != null)
                {
                    string tp = Root + "/Textures/" + texture;
                    var importer = AssetImporter.GetAtPath(tp) as TextureImporter;
                    if (!importer) throw new InvalidOperationException("Missing required texture: " + tp);
                    importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true; importer.mipmapEnabled = true;
                    importer.wrapMode = name == "LM_Plaque" ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                    importer.maxTextureSize = name == "LM_Plaque" ? 1024 : 2048; importer.SaveAndReimport();
                    mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp));
                }
                else mat.SetTexture("_BaseMap", null);
                mat.doubleSidedGI = false; EditorUtility.SetDirty(mat); mats.Add(mat);
            }
            AssetDatabase.SaveAssets(); return mats.ToArray();
        }

        static object Build()
        {
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var mats = BuildMaterials();
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (!importer) throw new InvalidOperationException("Missing FBX: " + ModelPath);
            importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true;
            importer.preserveHierarchy = true; importer.sortHierarchyByName = false; importer.optimizeGameObjects = false;
            importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
            importer.importBlendShapes = false; importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk; importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false; importer.generateSecondaryUV = true; importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var mat in mats) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mat.name), mat);
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!model) throw new InvalidOperationException("FBX did not import as a model.");
            GameObject wrapper = null;
            try
            {
                wrapper = new GameObject("LawyerMainDoor");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.transform.SetParent(wrapper.transform, false);
                foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterials = r.sharedMaterials.Select(m => mats.FirstOrDefault(x => x.name == (m ? m.name : "")) ?? throw new InvalidOperationException("Unexpected source material: " + (m ? m.name : "null"))).ToArray();
                    r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
                }
                var top = instance.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "LM_AxisTop");
                var front = instance.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "LM_AxisFront");
                if (top && front)
                {
                    Vector3 u = wrapper.transform.InverseTransformPoint(top.position).normalized;
                    Vector3 f = wrapper.transform.InverseTransformPoint(front.position).normalized;
                    instance.transform.localRotation = Quaternion.Inverse(Quaternion.LookRotation(-f, u)) * instance.transform.localRotation;
                }
                var report = ModelReport(wrapper);
                var b = BoundsOf(wrapper);
                if (b.size.x < 1.8f || b.size.x > 2.6f || b.size.y < 2.3f || b.size.y > 2.95f || b.size.z > .7f) throw new InvalidOperationException("Imported FBX orientation/dimensions outside door envelope: " + b.size);
                foreach (var t in wrapper.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;
                PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabPath);
                AssetDatabase.SaveAssets();
                Write(Path.Combine(Source, "Unity_ImportVerification.json"), new { report, axisMarkersFound = top && front, top = top ? V(wrapper.transform.InverseTransformPoint(top.position)) : null, front = front ? V(wrapper.transform.InverseTransformPoint(front.position)) : null, prefab = PrefabPath, generateSecondaryUV = importer.generateSecondaryUV });
                return report;
            }
            finally { if (wrapper) Object.DestroyImmediate(wrapper); }
        }

        static Bounds BoundsOf(GameObject root)
        {
            var rs = root.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) throw new InvalidOperationException("No renderers.");
            var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
        }

        static object ModelReport(GameObject root)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(true); var b = BoundsOf(root);
            return new { name = root.name, meshObjects = filters.Length, renderers = root.GetComponentsInChildren<Renderer>(true).Length, triangles = filters.Sum(m => m.sharedMesh ? (long)m.sharedMesh.GetIndexCount(0) / 3 + Enumerable.Range(1, m.sharedMesh.subMeshCount - 1).Sum(i => (long)m.sharedMesh.GetIndexCount(i) / 3) : 0), vertices = filters.Sum(m => m.sharedMesh ? m.sharedMesh.vertexCount : 0), boundsCenter = V(b.center), boundsSize = V(b.size), meshes = filters.Select(m => new { name = m.name, mesh = m.sharedMesh.name, vertices = m.sharedMesh.vertexCount, subMeshes = m.sharedMesh.subMeshCount, uv0 = m.sharedMesh.GetVertexAttributeDimension(VertexAttribute.TexCoord0), uv2 = m.sharedMesh.GetVertexAttributeDimension(VertexAttribute.TexCoord1) }).ToArray() };
        }

        static object Apply(bool saveScene)
        {
            var door = Door(); var scene = door.scene;
            if (!File.Exists(Path.Combine(Source, "Before_Inspection.json"))) throw new InvalidOperationException("Inspect must save a baseline before applying.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); if (!prefab) throw new InvalidOperationException("Build the door prefab first.");
            var before = JObject.FromObject(Snapshot(door));
            var colliders = door.GetComponentsInChildren<Collider>(true).Select(c => new { c, json = EditorJsonUtility.ToJson(c), id = Id(c) }).ToArray();
            var position = door.transform.localPosition; var rotation = door.transform.localRotation; var scale = door.transform.localScale;
            int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Replace Main Door visual with low-poly model");
            var old = door.transform.Find(OwnedName); if (old) Undo.DestroyObjectImmediate(old.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene); Undo.RegisterCreatedObjectUndo(instance, "Add low-poly door");
            instance.name = OwnedName; instance.transform.SetParent(door.transform, false); instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            foreach (var t in instance.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = door.layer; GameObjectUtility.SetStaticEditorFlags(t.gameObject, GameObjectUtility.GetStaticEditorFlags(door)); }
            var renderer = door.GetComponent<MeshRenderer>(); if (!renderer) throw new InvalidOperationException("Original Main Door renderer is missing.");
            Undo.RecordObject(renderer, "Disable placeholder visual"); renderer.enabled = false;
            if (door.transform.localPosition != position || door.transform.localRotation != rotation || door.transform.localScale != scale) throw new InvalidOperationException("Original Main Door transform changed.");
            foreach (var entry in colliders) if (!entry.c || Id(entry.c) != entry.id || EditorJsonUtility.ToJson(entry.c) != entry.json) throw new InvalidOperationException("Original collision/trigger data changed.");
            if (instance.GetComponentsInChildren<Collider>(true).Length > 0) throw new InvalidOperationException("Visual model unexpectedly contains colliders.");
            EditorSceneManager.MarkSceneDirty(scene); Undo.CollapseUndoOperations(undo);
            Capture(door, Path.Combine(Source, "After_InScene.png"), 0); Capture(door, Path.Combine(Source, "After_InScene_Close.png"), 1);
            File.Copy(Path.Combine(Source, "After_InScene_Close.png"), Path.Combine(Source, "After_Detail_InScene.png"), true);
            var after = JObject.FromObject(Snapshot(door));
            string stagedCopy = Path.Combine(Source, "LawyerRoom_AfterPreview.unity");
            if (!EditorSceneManager.SaveScene(scene, stagedCopy, true)) throw new InvalidOperationException("Could not serialize the staged scene copy for reference comparison.");
            bool saved = false;
            if (saveScene) saved = EditorSceneManager.SaveScene(scene);
            var data = new { scene = scene.path, rootIdPreserved = (string)before["id"] == (string)after["id"], originalTransformPreserved = true, originalCollidersPreserved = true, originalColliderCount = colliders.Length, saved, dirty = scene.isDirty, stagedCopy, inverseLocalScale = V(instance.transform.localScale), model = ModelReport(instance), before, after };
            Write(Path.Combine(Source, "Unity_ApplyVerification.json"), data);
            return data;
        }

        static object Preview()
        {
            var go = Door(); Capture(go, Path.Combine(Source, "After_InScene.png"), 0); Capture(go, Path.Combine(Source, "After_InScene_Close.png"), 1);
            File.Copy(Path.Combine(Source, "After_InScene_Close.png"), Path.Combine(Source, "After_Detail_InScene.png"), true);
            var owned = go.transform.Find(OwnedName); var original = go.GetComponent<MeshRenderer>();
            if (owned && original)
            {
                Capture(go, Path.Combine(Source, "After_Flashlight_InScene.png"), 0, true);
                Capture(go, Path.Combine(Source, "After_Flashlight_Detail_InScene.png"), 1, true);
                bool previousActive = owned.gameObject.activeSelf; bool previousEnabled = original.enabled;
                try
                {
                    owned.gameObject.SetActive(false); original.enabled = true;
                    Capture(go, Path.Combine(Source, "Before_Flashlight_InScene.png"), 0, true);
                }
                finally { owned.gameObject.SetActive(previousActive); original.enabled = previousEnabled; }
            }
            string stagedCopy = Path.Combine(Source, "LawyerRoom_AfterPreview.unity");
            if (!EditorSceneManager.SaveScene(go.scene, stagedCopy, true)) throw new InvalidOperationException("Could not serialize staged scene copy.");
            return new { scene = go.scene.path, dirty = go.scene.isDirty, stagedCopy, door = Snapshot(go), images = new[] { Path.Combine(Source, "After_InScene.png"), Path.Combine(Source, "After_InScene_Close.png") } };
        }

        static object FinalizeScene()
        {
            var go = Door();
            if (!go.transform.Find(OwnedName) || go.GetComponent<MeshRenderer>().enabled) throw new InvalidOperationException("Replacement visual must be applied before finalizing.");
            if (go.scene.GetRootGameObjects().Any(x => x.name.StartsWith("LawyerDoor_Temporary", StringComparison.Ordinal))) throw new InvalidOperationException("A diagnostic object has not been cleaned up.");
            var baseline = (JObject)JObject.Parse(File.ReadAllText(Path.Combine(Source, "Before_Inspection.json")))["door"];
            var current = JObject.Parse(JsonConvert.SerializeObject(Snapshot(go)));
            foreach (var key in new[] { "id", "localPosition", "localRotation", "localScale", "layer", "staticFlags" })
                if (!JToken.DeepEquals(baseline[key], current[key])) throw new InvalidOperationException("Original Main Door field changed: " + key);
            foreach (JObject originalComponent in baseline["components"])
            {
                var component = current["components"].OfType<JObject>().SingleOrDefault(x => (string)x["id"] == (string)originalComponent["id"]);
                if (component == null) throw new InvalidOperationException("Original component ID is missing.");
                var expected = JObject.Parse((string)originalComponent["json"]);
                if ((string)originalComponent["type"] == "UnityEngine.MeshRenderer") expected["MeshRenderer"]["m_Enabled"] = false;
                if (!JToken.DeepEquals(expected, JObject.Parse((string)component["json"]))) throw new InvalidOperationException("Original component changed beyond the allowed renderer switch: " + (string)originalComponent["type"]);
            }
            foreach (JObject originalChild in baseline["descendants"])
            {
                var child = current["descendants"].OfType<JObject>().SingleOrDefault(x => (string)x["id"] == (string)originalChild["id"]);
                if (child == null || !JToken.DeepEquals(child, originalChild)) throw new InvalidOperationException("Original LookTarget hierarchy or transform changed.");
            }
            var referenceAudit = JObject.Parse(File.ReadAllText(Path.Combine(Source, "Unity_SceneReferenceAudit.json")));
            if ((bool?)referenceAudit["success"] != true) throw new InvalidOperationException("Scene reference audit must pass before final save.");
            bool saved = EditorSceneManager.SaveScene(go.scene);
            if (!saved) throw new InvalidOperationException("Unity did not save the current Lawyer Room scene.");
            var data = new { scene = go.scene.path, saved, dirty = go.scene.isDirty, door = Snapshot(go), selection = Selection.objects.Select(x => new { name = x.name, id = Id(x) }).ToArray(), utc = DateTime.UtcNow.ToString("o") };
            Write(Path.Combine(Source, "Unity_FinalVerification.json"), data);
            return data;
        }

        static void Capture(GameObject door, string path, int view, bool flashlight = false)
        {
            GameObject cameraObject = null; RenderTexture target = null; Texture2D image = null;
            var active = RenderTexture.active;
            try
            {
                cameraObject = new GameObject("LawyerDoor_TemporaryPreviewCamera") { hideFlags = HideFlags.HideAndDontSave };
                var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
                var sourceCamera = door.scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.enabled && c.gameObject.activeInHierarchy);
                if (sourceCamera) camera.CopyFrom(sourceCamera);
                var extra = cameraObject.AddComponent<UniversalAdditionalCameraData>();
                if (sourceCamera && sourceCamera.TryGetComponent<UniversalAdditionalCameraData>(out var originalData)) EditorUtility.CopySerialized(originalData, extra);
                extra.renderType = CameraRenderType.Base; extra.cameraStack.Clear();
                var t = door.transform; Vector3 center = t.position;
                camera.transform.position = center - t.forward * (view == 0 ? 4.3f : 3.1f) + t.right * (view == 0 ? 1.1f : -.6f) + t.up * .16f;
                camera.transform.rotation = Quaternion.LookRotation(center + t.up * .02f - camera.transform.position, t.up);
                camera.fieldOfView = view == 0 ? 48 : 50; camera.nearClipPlane = .03f; camera.farClipPlane = 150;
                camera.cullingMask = -1; camera.allowMSAA = true; camera.allowHDR = true; camera.aspect = 1400f / 1100f;
                if (flashlight)
                {
                    var torchObject = new GameObject("LawyerDoor_TemporaryDiagnosticFlashlight") { hideFlags = HideFlags.HideAndDontSave };
                    torchObject.transform.SetParent(cameraObject.transform, false); torchObject.transform.localPosition = new Vector3(.15f, -.1f, .1f);
                    var torch = torchObject.AddComponent<Light>();
                    var originalTorch = door.scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Light>(true))
                        .FirstOrDefault(l => l != torch && l.type == LightType.Spot && (Hierarchy(l.transform).IndexOf("flashlight", StringComparison.OrdinalIgnoreCase) >= 0 || Hierarchy(l.transform).IndexOf("phone", StringComparison.OrdinalIgnoreCase) >= 0));
                    if (originalTorch)
                    {
                        EditorUtility.CopySerialized(originalTorch, torch);
                        if (originalTorch.TryGetComponent<UniversalAdditionalLightData>(out var lightData)) EditorUtility.CopySerialized(lightData, torchObject.AddComponent<UniversalAdditionalLightData>());
                    }
                    else
                    {
                        torch.type = LightType.Spot; torch.range = 8; torch.spotAngle = 50; torch.innerSpotAngle = 32;
                        torch.intensity = 4; torch.color = new Color(1f, .95f, .85f); torch.shadows = LightShadows.Soft;
                    }
                    torch.enabled = true; torch.lightmapBakeType = LightmapBakeType.Realtime;
                    Write(Path.Combine(Source, "Flashlight_Diagnostic.json"), new { diagnosticOnly = true, source = originalTorch ? Hierarchy(originalTorch.transform) : "Fallback: range8 angle50 intensity4 warm neutral", settingsCopied = originalTorch != null, torch.intensity, torch.range, torch.spotAngle, torch.innerSpotAngle, color = new[] { torch.color.r, torch.color.g, torch.color.b }, lightmapBakeType = torch.lightmapBakeType.ToString(), persistentSceneLightsChanged = false, capture = path });
                }
                target = new RenderTexture(1400, 1100, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                target.Create(); camera.targetTexture = target; camera.Render(); camera.Render(); RenderTexture.active = target;
                image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false, false); image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = active;
                if (cameraObject) Object.DestroyImmediate(cameraObject);
                if (image) Object.DestroyImmediate(image);
                if (target) { target.Release(); Object.DestroyImmediate(target); }
            }
        }
    }
}
