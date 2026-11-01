using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // Synchronize the already-open scene as well as newly-opened scenes. Editing the
    // serialized scene on disk alone does not update an in-memory dirty scene.
    [InitializeOnLoad]
    public static class EveningTestingShortcutsSetup
    {
        private const string ScenePath = "Assets/MainScenes/From_PM/MainScene_2_Evening.unity";
        private static bool installing;

        static EveningTestingShortcutsSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode) Install();
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install;
            };
        }

        [MenuItem("Tools/Secrets That Breathe/Evening/Repair F1 Testing Shortcut")]
        public static void Install()
        {
            if (installing || EditorApplication.isPlaying) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Install;
                return;
            }
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            installing = true;
            Scene preview = default;
            try
            {
                var roots = scene.GetRootGameObjects();
                var canvas = roots.FirstOrDefault(go => go.name == "SeatingCanvas");
                var car = roots.FirstOrDefault(go => go.name == "FD");
                var ride = car != null ? car.GetComponent<PorschePassengerRide>() : null;
                if (canvas == null || ride == null) throw new InvalidOperationException("SeatingCanvas or FD ride is missing in the open scene.");
                bool changed = false;
                var hints = canvas.transform.Find("TestingShortcutHints");
                if (hints == null)
                {
                    preview = EditorSceneManager.OpenPreviewScene(ScenePath);
                    var savedCanvas = preview.GetRootGameObjects().First(go => go.name == "SeatingCanvas");
                    var savedHints = savedCanvas.transform.Find("TestingShortcutHints");
                    if (savedHints == null) throw new InvalidOperationException("Saved F1 hint template is missing.");
                    var copy = UnityEngine.Object.Instantiate(savedHints.gameObject, canvas.transform, false);
                    copy.name = "TestingShortcutHints";
                    Undo.RegisterCreatedObjectUndo(copy, "Restore Evening testing hint");
                    hints = copy.transform;
                    changed = true;
                }
                var hint = hints.Find("TestSkipCarExitHint");
                if (hint == null) throw new InvalidOperationException("TestSkipCarExitHint is missing under TestingShortcutHints.");
                var introHint = hints.Find("TestSkipIntroHint");
                if (introHint == null)
                {
                    if (!preview.IsValid()) preview = EditorSceneManager.OpenPreviewScene(ScenePath);
                    var savedCanvas = preview.GetRootGameObjects().First(go => go.name == "SeatingCanvas");
                    var savedIntro = savedCanvas.transform.Find("TestingShortcutHints/TestSkipIntroHint");
                    if (savedIntro == null) throw new InvalidOperationException("Saved F2 hint template is missing.");
                    var copy = UnityEngine.Object.Instantiate(savedIntro.gameObject, hints, false);
                    copy.name = "TestSkipIntroHint";
                    Undo.RegisterCreatedObjectUndo(copy, "Add Evening F2 hint");
                    introHint = copy.transform;
                    changed = true;
                }
                var shortcut = canvas.GetComponent<EveningTestingShortcuts>();
                if (shortcut == null)
                {
                    shortcut = Undo.AddComponent<EveningTestingShortcuts>(canvas);
                    changed = true;
                }
                if (shortcut.ride != ride || shortcut.testSkipHint != hint.gameObject || shortcut.testSkipIntroHint != introHint.gameObject)
                {
                    Undo.RecordObject(shortcut, "Connect Evening F1 checkpoint");
                    shortcut.ride = ride;
                    shortcut.testSkipHint = hint.gameObject;
                    shortcut.testSkipIntroHint = introHint.gameObject;
                    EditorUtility.SetDirty(shortcut);
                    changed = true;
                }
                if (changed) EditorSceneManager.MarkSceneDirty(scene);
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Tools/EveningTesting"));
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "installation-report.json"), JsonUtility.ToJson(new Report
                {
                    success = true, changed = changed, scene = scene.path,
                    hintActive = hint.gameObject.activeInHierarchy,
                    shortcutsEnabled = shortcut.enabled && shortcut.enableTestingShortcuts,
                    ride = shortcut.ride.name
                }, true));
            }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                installing = false;
            }
        }

        [Serializable] private class Report
        {
            public bool success, changed, hintActive, shortcutsEnabled;
            public string scene, ride;
        }
    }
}
