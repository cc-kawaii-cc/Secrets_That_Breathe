using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // One-time scene changes (runs by itself after the scripts compile; menu: Tools > Secrets That Breathe > Scene Fixes):
    //   Po: E at the exit doors goes on to MainScene_2_MidNight (PoGoBackToMidnight), and that scene is added to the Build Settings.
    //   MainScene_2_MidNight, MainScene_2_Evening: every banana tree gets one CapsuleCollider around its trunk only (child
    //   "TrunkCollider", sized from the trunk part of the mesh); the leaves have no collider, so nobody gets stuck in them.
    //   Po, MainScene_2_MidNight, MainScene_2_Evening, Lawyer Room: the "[F1] Skip ..." / "[F2] ..." testing-shortcut texts are hidden,
    //   the shortcut keys themselves stay on.
    // A scene that already had unsaved changes is changed but not saved (press Ctrl+S); the changes are applied again next time
    // until every scene has been saved with them.
    [InitializeOnLoad]
    public static class SceneFixesSetup
    {
        const string Folder = "Assets/MainScenes/From_PM/";
        const string PoPath = Folder + "Po.unity";
        const string MidnightPath = Folder + "MainScene_2_MidNight.unity";
        const string EveningPath = Folder + "MainScene_2_Evening.unity";
        const string LawyerPath = Folder + "Lawyer Room.unity";
        const string DoneKey = "SecretsThatBreathe.SceneFixes3.Done";      // EditorPrefs: every scene was saved with the changes
        const string SessionKey = "SecretsThatBreathe.SceneFixes3.Session";
        const string TrunkColliderName = "TrunkCollider";
        const string Tag = "[Scene fixes] ";

        static SceneFixesSetup()
        {
            EditorApplication.delayCall += AutoInstall;
        }

        static void AutoInstall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoInstall; return; }
            if (EditorPrefs.GetBool(DoneKey, false) || SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            RunAll();
        }

        [MenuItem("Tools/Secrets That Breathe/Scene Fixes/Run All (Po exit, banana colliders, hide shortcut texts)")]
        static void RunAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning(Tag + "Stop Play Mode first."); return; }
            bool saved = true;
            saved &= Apply(PoPath, (all, log) => LinkPoToMidnight(all, log) | HideShortcutTexts(all, log));
            saved &= Apply(MidnightPath, (all, log) => BananaTrunkColliders(all, log) | HideShortcutTexts(all, log));
            saved &= Apply(EveningPath, (all, log) => BananaTrunkColliders(all, log) | HideShortcutTexts(all, log));
            saved &= Apply(LawyerPath, (all, log) => HideShortcutTexts(all, log));
            EnsureInBuild(MidnightPath);
            if (saved) EditorPrefs.SetBool(DoneKey, true);
        }

        // Opens the scene (if it is not open), applies the change and saves it, unless it already had unsaved changes.
        // Returns true when nothing is left unsaved.
        static bool Apply(string path, Func<Transform[], StringBuilder, bool> change)
        {
            if (!System.IO.File.Exists(path)) { Debug.LogWarning(Tag + path + " was not found."); return true; }
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            bool wasDirty = wasLoaded && scene.isDirty;
            Scene original = SceneManager.GetActiveScene();
            var log = new StringBuilder();
            bool done = true;
            try
            {
                if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                bool changed = change(all, log);
                if (!changed) { Debug.Log(Tag + name + " PASS (already up to date).\n" + log); return true; }
                EditorSceneManager.MarkSceneDirty(scene);
                string result;
                if (wasDirty) { result = name + " had unsaved changes, so it was NOT saved - press Ctrl+S to keep this."; done = false; }
                else if (EditorSceneManager.SaveScene(scene)) result = "Saved " + path + ".";
                else throw new Exception("Saving " + path + " failed.");
                Debug.Log(Tag + name + " PASS. " + result + "\n" + log);
            }
            catch (Exception error) { Debug.LogError(Tag + name + " FAIL " + error.Message + "\n" + log + "\n" + error); done = false; }
            finally
            {
                if (!wasLoaded && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
            return done;
        }

        // ---- Po: the exit doors lead on to the midnight scene ---------------------------------------------------------------------
        static bool LinkPoToMidnight(Transform[] all, StringBuilder log)
        {
            var station = all.Select(t => t.GetComponent<PoliceStationNoEntryTv>()).FirstOrDefault(c => c != null);
            if (station == null) { log.AppendLine("WARNING: PoliceStationNoEntryTv (E at the exit doors) was not found in Po."); return false; }
            var link = station.GetComponent<PoGoBackToMidnight>();
            bool changed = false;
            if (link == null) { link = Undo.AddComponent<PoGoBackToMidnight>(station.gameObject); changed = true; }
            if (link.station != station || link.destinationScene != "MainScene_2_MidNight")
            {
                Undo.RecordObject(link, "Po exit");
                link.station = station;
                link.destinationScene = "MainScene_2_MidNight";
                EditorUtility.SetDirty(link);
                changed = true;
            }
            log.AppendLine("Exit doors (" + (station.endLocation != null ? station.endLocation.name : "end") + "): E -> black screen -> MainScene_2_MidNight (loaded while the screen goes black). Component: " + station.name + "/PoGoBackToMidnight");
            return changed;
        }

        static void EnsureInBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var entry = scenes.FirstOrDefault(s => s.path == path);
            if (entry != null && entry.enabled) return;
            if (entry != null) entry.enabled = true;
            else scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log(Tag + "Added " + path + " to the Build Settings (needed to load it from Po).");
        }

        // ---- banana trees: a collider around the trunk only -----------------------------------------------------------------------
        // Each tree has three meshes (LOD0/1/2), each with trunk + leaves + fruit, so a MeshCollider would also make the leaves solid.
        // Instead: remove any MeshCollider on the tree and add one CapsuleCollider fitted to the trunk (material slot 0 of LOD0,
        // BananaTrunk), from the ground up to where the leaves start.
        static bool BananaTrunkColliders(Transform[] all, StringBuilder log)
        {
            int trees = 0, removed = 0, added = 0, guessed = 0;
            foreach (var tree in all.Where(t => BananaEatInteraction.IsBananaTreeName(t.name)
                                                && (t.parent == null || !BananaEatInteraction.IsBananaTreeName(t.parent.name))))
            {
                if (tree.GetComponentInChildren<Renderer>(true) == null) continue;
                trees++;
                foreach (var meshCollider in tree.GetComponentsInChildren<MeshCollider>(true))
                { Undo.DestroyObjectImmediate(meshCollider); removed++; }
                if (tree.Find(TrunkColliderName) != null) continue;

                var holder = new GameObject(TrunkColliderName);
                Undo.RegisterCreatedObjectUndo(holder, "Banana trunk collider");
                holder.transform.SetParent(tree, false);
                holder.transform.position = tree.position;
                holder.transform.rotation = Quaternion.identity;   // upright, metres
                Vector3 lossy = tree.lossyScale;
                holder.transform.localScale = new Vector3(1f / Mathf.Max(.0001f, Mathf.Abs(lossy.x)), 1f / Mathf.Max(.0001f, Mathf.Abs(lossy.y)),
                                                          1f / Mathf.Max(.0001f, Mathf.Abs(lossy.z)));
                var capsule = holder.AddComponent<CapsuleCollider>();
                capsule.direction = 1;
                if (!FitTrunk(tree, out Vector3 bottom, out float radius, out float height))
                {
                    guessed++;
                    var bounds = tree.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                    bottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    radius = .15f;
                    height = Mathf.Clamp(bounds.size.y * .55f, .8f, 2.5f);
                }
                Vector3 local = bottom - tree.position;
                capsule.radius = radius;
                capsule.height = Mathf.Max(height, radius * 2f);
                capsule.center = new Vector3(local.x, local.y + capsule.height * .5f, local.z);
                added++;
            }
            log.AppendLine("Banana trees: " + trees + ", trunk colliders added: " + added + (guessed > 0 ? " (" + guessed + " sized from the tree bounds)" : "")
                           + ", MeshColliders removed: " + removed + ".");
            return added > 0 || removed > 0;
        }

        // Trunk position, radius and height (world metres) from the trunk part (sub-mesh 0) of the most detailed mesh.
        static bool FitTrunk(Transform tree, out Vector3 bottom, out float radius, out float height)
        {
            bottom = Vector3.zero; radius = 0f; height = 0f;
            try
            {
                var filter = tree.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null)
                                 .OrderByDescending(f => f.name.EndsWith("_LOD0") ? 1 : 0).ThenByDescending(f => f.sharedMesh.vertexCount).FirstOrDefault();
                if (filter == null) return false;
                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                var indices = mesh.subMeshCount > 0 ? mesh.GetIndices(0) : null;
                if (vertices == null || vertices.Length == 0 || indices == null || indices.Length == 0) return false;
                var points = indices.Distinct().Select(i => filter.transform.TransformPoint(vertices[i])).ToList();
                float ground = points.Min(p => p.y);
                // the trunk near the ground: middle and radius (medians, so the small shoots beside it do not count much)
                var low = points.Where(p => p.y > ground + .15f && p.y < ground + 1.1f).ToList();
                if (low.Count < 8) return false;
                float cx = Median(low.Select(p => p.x)), cz = Median(low.Select(p => p.z));
                float r = Mathf.Clamp(Median(low.Select(p => new Vector2(p.x - cx, p.z - cz).magnitude)), .05f, .5f);
                radius = r;
                // up the trunk while the trunk part stays close to the middle (the leaf stalks spread out above that)
                float top = points.Where(p => new Vector2(p.x - cx, p.z - cz).magnitude <= r * 1.6f).Max(p => p.y);
                height = Mathf.Clamp(top - ground, .6f, 4f);
                bottom = new Vector3(cx, ground, cz);
                return true;
            }
            catch (Exception) { return false; }
        }

        static float Median(System.Collections.Generic.IEnumerable<float> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            return sorted.Count == 0 ? 0f : sorted[sorted.Count / 2];
        }

        // ---- testing shortcuts: texts hidden, keys on ----------------------------------------------------------------------------
        static bool HideShortcutTexts(Transform[] all, StringBuilder log)
        {
            bool changed = false;
            int hidden = 0;
            foreach (var t in all)
            {
                bool hint = t.name == "TestingShortcutHints" || (t.name.StartsWith("TestSkip") && t.name.EndsWith("Hint"));
                if (!hint || !t.gameObject.activeSelf) continue;
                if (t.name != "TestingShortcutHints" && t.parent != null && t.parent.name == "TestingShortcutHints") continue;   // hidden with its holder
                Undo.RecordObject(t.gameObject, "Hide shortcut text");
                t.gameObject.SetActive(false);
                EditorUtility.SetDirty(t.gameObject);
                hidden++;
                changed = true;
            }
            int keys = 0;
            foreach (var shortcuts in all.Select(t => t.GetComponent<EveningTestingShortcuts>()).Where(c => c != null))
            {
                keys++;
                if (shortcuts.enableTestingShortcuts && shortcuts.enabled) continue;
                Undo.RecordObject(shortcuts, "Shortcut keys on");
                shortcuts.enableTestingShortcuts = true;
                shortcuts.enabled = true;
                EditorUtility.SetDirty(shortcuts);
                changed = true;
            }
            foreach (var search in all.Select(t => t.GetComponent<DocumentSearchSequence>()).Where(c => c != null))
            {
                keys++;
                if (search.enableTestingShortcuts) continue;
                Undo.RecordObject(search, "Shortcut keys on");
                search.enableTestingShortcuts = true;
                EditorUtility.SetDirty(search);
                changed = true;
            }
            log.AppendLine("Shortcut texts hidden now: " + hidden + " (the rest were already hidden); shortcut keys on in " + keys + " component(s).");
            return changed;
        }
    }
}
