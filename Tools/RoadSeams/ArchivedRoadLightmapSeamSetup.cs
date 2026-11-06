using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecretsThatBreathe.EditorTools
{
    // Editor-only, narrowly scoped repair. Original transforms, colliders and
    // gameplay references are retained; only their visual renderers are replaced.
    [InitializeOnLoad]
    public static class RoadLightmapSeamSetup
    {
        const string ScenePath = "Assets/MainScenes/From_PM/MainScene_2_Evening.unity";
        const string Request = "Tools/RoadSeams/install.request";
        const string Result = "Tools/RoadSeams/install.result.txt";
        const string MeshPath = "Assets/SecretsThatBreathe/Environment/Road/Road_ContinuousLighting.asset";
        const string ObjectName = "Road_ContinuousLighting";
        static RoadLightmapSeamSetup() { EditorApplication.update += Poll; }
        static double nextCheck;
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = EditorSceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            string id = File.ReadAllText(Request).Trim();
            if (id == "road-seams-restore-20261107-v1")
            {
                File.Move(Request, Request + ".restore.consumed");
                try { RestoreOriginalRoad(); }
                catch (Exception error) { File.WriteAllText(Result, "RESTORE FAILED\n" + error); Debug.LogException(error); }
                return;
            }
            if (id != "road-seams-20261107-v3") return;
            File.Move(Request, Request + ".v3.consumed");
            try { Repair(); }
            catch (Exception error) { File.WriteAllText(Result, "FAILED\n" + error); Debug.LogException(error); }
        }

        [MenuItem("Tools/Secrets That Breathe/Restore Original Evening Road")]
        public static void RestoreOriginalRoad()
        {
            var scene = EditorSceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Open Evening scene outside Play Mode.");
            var road = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Road");
            if (road == null) throw new InvalidOperationException("Road root not found.");
            Directory.CreateDirectory("Tools/RoadSeams");
            string backup = "Tools/RoadSeams/before-restore-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not back up current scene.");
            int restored = 0;
            foreach (var source in road.GetComponentsInChildren<MeshFilter>(true))
            {
                string path = AssetDatabase.GetAssetPath(source.sharedMesh);
                if (path != "Assets/Nemo_Upgrade/Prefabs/Bridge & Road/Model/Road.fbx"
                    && path != "Assets/Nemo_Upgrade/Prefabs/Bridge & Road/Model/IntersectionRoad.fbx") continue;
                var renderer = source.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                Undo.RecordObject(renderer, "Restore original road renderer");
                renderer.enabled = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer); restored++;
            }
            if (restored < 2) throw new InvalidOperationException("Original road tiles not found.");
            var combined = road.transform.Find(ObjectName);
            if (combined != null) Undo.DestroyObjectImmediate(combined.gameObject);
            if (File.Exists(MeshPath))
            {
                Directory.CreateDirectory("Tools/RoadSeams/RevertedAssets");
                File.Copy(MeshPath, "Tools/RoadSeams/RevertedAssets/Road_ContinuousLighting.asset", true);
                if (File.Exists(MeshPath+".meta")) File.Copy(MeshPath+".meta", "Tools/RoadSeams/RevertedAssets/Road_ContinuousLighting.asset.meta", true);
                AssetDatabase.DeleteAsset(MeshPath);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save restored scene.");
            File.WriteAllText(Result, "RESTORED\nOriginal renderers enabled: " + restored
                + "\nCombined road object and generated Mesh asset removed.\nOriginal model UVs, transforms, materials, colliders retained.\nOther scene changes preserved.\nBackup: " + backup
                + "\nRebake lighting if the lighting was baked after the road repair.\n");
        }

        [MenuItem("Tools/Secrets That Breathe/Repair Evening Road Lightmap Seams")]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before repairing the road.");
            var scene = EditorSceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open MainScene_2_Evening first.");
            var road = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Road");
            if (road == null) throw new InvalidOperationException("Road root not found.");
            var existing = road.transform.Find(ObjectName);
            var sources = road.GetComponentsInChildren<MeshFilter>(true).Where(f =>
            {
                string path = AssetDatabase.GetAssetPath(f.sharedMesh);
                var renderer = f.GetComponent<MeshRenderer>();
                return renderer != null && f.gameObject.activeInHierarchy && (existing != null || renderer.enabled)
                    && (path == "Assets/Nemo_Upgrade/Prefabs/Bridge & Road/Model/Road.fbx"
                        || path == "Assets/Nemo_Upgrade/Prefabs/Bridge & Road/Model/IntersectionRoad.fbx");
            }).ToArray();
            if (sources.Length < 2) throw new InvalidOperationException("Fewer than two road tiles found.");
            var material = sources[0].GetComponent<MeshRenderer>().sharedMaterial;
            if (sources.Any(f => f.GetComponent<MeshRenderer>().sharedMaterials.Length != 1 || f.GetComponent<MeshRenderer>().sharedMaterial != material))
                throw new InvalidOperationException("Road tiles use different materials; repair cancelled.");
            Directory.CreateDirectory("Tools/RoadSeams");
            string backup = "Tools/RoadSeams/before-road-seams-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not back up the open scene, including unsaved edits.");
            var combine = sources.Select(f => new CombineInstance { mesh = f.sharedMesh, subMeshIndex = 0,
                transform = road.transform.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray();
            var mesh = new Mesh { name = ObjectName, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(combine, true, true, false);
            int beforeVertices = mesh.vertexCount;
            int removedFaces;
            var repaired = ContinuousSurface(mesh, out removedFaces);
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = repaired;
            // One renderer lets the Progressive Lightmapper stitch its separate
            // charts; static batching alone does not change the bake boundaries.
            var unwrap = new UnwrapParam(); UnwrapParam.SetDefaults(out unwrap);
            unwrap.packMargin = 8f / 1024f;
            if (!Unwrapping.GenerateSecondaryUVSet(mesh, unwrap)) throw new InvalidOperationException("Could not unwrap road lightmap UVs.");
            mesh.RecalculateBounds();
            string directory = Path.GetDirectoryName(MeshPath).Replace('\\', '/');
            Directory.CreateDirectory(directory); AssetDatabase.Refresh();
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (saved == null) { AssetDatabase.CreateAsset(mesh, MeshPath); saved = mesh; }
            else { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
            GameObject visual;
            if (existing != null) visual = existing.gameObject;
            else { visual = new GameObject(ObjectName); visual.transform.SetParent(road.transform, false); Undo.RegisterCreatedObjectUndo(visual, "Repair road seams"); }
            var filter = visual.GetComponent<MeshFilter>();
            if (filter == null) filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = saved;
            var target = visual.GetComponent<MeshRenderer>();
            if (target == null) target = visual.AddComponent<MeshRenderer>();
            target.sharedMaterial = material; target.shadowCastingMode = ShadowCastingMode.On; target.receiveShadows = true;
            target.receiveGI = ReceiveGI.Lightmaps; target.stitchLightmapSeams = true;
            GameObjectUtility.SetStaticEditorFlags(visual, GameObjectUtility.GetStaticEditorFlags(sources[0].gameObject));
            target.lightmapIndex = -1;
            foreach (var source in sources)
            {
                var renderer = source.GetComponent<MeshRenderer>(); Undo.RecordObject(renderer, "Use continuous road lighting");
                renderer.enabled = false; EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save repaired scene.");
            File.WriteAllText(Result, "APPLIED\nTiles: " + sources.Length + "\nTriangles: " + saved.triangles.Length / 3
                + "\nSurface repair: continuous texture UVs, welded top vertices, removed " + removedFaces + " internal wall triangles.\nVertices before/after: " + beforeVertices + "/" + saved.vertexCount
                + "\nBackup: " + backup + "\nOriginal transforms, materials, colliders and references retained.\nRoad shadows remain enabled.\nA new lighting bake is REQUIRED; existing lightmaps were not changed.\n");
            Debug.Log("Road lighting mesh saved. Bake lighting again to update the new renderer's lightmap.");
        }

        // Combining renderers does NOT weld the original tiles or fix their UV
        // resets. Build a genuinely connected top surface before lightmap unwrap.
        static Mesh ContinuousSurface(Mesh source, out int removedFaces)
        {
            var positions = source.vertices; var normals = source.normals; var uv = source.uv;
            var triangles = source.triangles;
            var tops = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
                if (normals[triangles[i]].y > .99f) tops.Add(i);
            removedFaces = 0;
            var points = new List<Vector3>(); var newNormals = new List<Vector3>();
            var newUv = new List<Vector2>(); var indices = new List<int>();
            var welded = new Dictionary<string, int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i+1], c = triangles[i+2];
                Vector3 normal = normals[a];
                if (Mathf.Abs(normal.y) < .01f)
                {
                    Vector3 outside = (positions[a]+positions[b]+positions[c])/3f + normal*.002f;
                    bool internalWall = false;
                    foreach (int top in tops)
                    {
                        int ta=triangles[top],tb=triangles[top+1],tc=triangles[top+2];
                        if (positions[ta].y >= outside.y && InsideXZ(outside, positions[ta],positions[tb],positions[tc]))
                        { internalWall = true; break; }
                    }
                    if (internalWall) { removedFaces++; continue; }
                }
                for (int corner = 0; corner < 3; corner++)
                {
                    int old = triangles[i+corner]; Vector3 p=positions[old], n=normals[old];
                    // Original texture density is approximately 0.09627 repeats/m.
                    // A shared planar mapping prevents a restart at every 10m tile.
                    Vector2 tex = n.y > .99f ? new Vector2(p.z*.0962675f, p.x*.0962675f) : uv[old];
                    string key = Q(p.x)+","+Q(p.y)+","+Q(p.z)+"/"+Q(n.x)+","+Q(n.y)+","+Q(n.z)+"/"+Q(tex.x)+","+Q(tex.y);
                    if (!welded.TryGetValue(key, out int index))
                    {
                        index=points.Count; welded.Add(key,index); points.Add(p); newNormals.Add(n); newUv.Add(tex);
                    }
                    indices.Add(index);
                }
            }
            var result = new Mesh { name=ObjectName,indexFormat=IndexFormat.UInt32 };
            result.SetVertices(points); result.SetNormals(newNormals); result.SetUVs(0,newUv); result.SetTriangles(indices,0);
            result.RecalculateTangents(); result.RecalculateBounds(); return result;
        }
        static long Q(float value) => (long)Math.Round(value*10000);
        static bool InsideXZ(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            float d=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
            if(Mathf.Abs(d)<.000001f) return false;
            float u=((b.z-c.z)*(p.x-c.x)+(c.x-b.x)*(p.z-c.z))/d;
            float v=((c.z-a.z)*(p.x-c.x)+(a.x-c.x)*(p.z-c.z))/d;
            return u>=-.000001f && v>=-.000001f && u+v<=1.000001f;
        }
    }
}
