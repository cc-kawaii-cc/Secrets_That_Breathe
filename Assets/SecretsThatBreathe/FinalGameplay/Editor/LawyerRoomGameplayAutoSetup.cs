#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.FinalGameplay.Editor
{
    /// <summary>
    /// Materializes the editable Canvas once when Lawyer Room is open. The regular
    /// menu command remains available for intentionally rebuilding the setup later.
    /// </summary>
    [InitializeOnLoad]
    public static class LawyerRoomGameplayAutoSetup
    {
        private const string ScenePath = "Assets/Tonpalm/Lawyer Room.unity";
        private static bool setupQueued;
        private static bool setupRunning;

        static LawyerRoomGameplayAutoSetup()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += TryQueueActiveScene;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            TryQueue(scene);
        }

        private static void TryQueueActiveScene()
        {
            TryQueue(SceneManager.GetActiveScene());
        }

        private static void TryQueue(Scene scene)
        {
            if (setupQueued || setupRunning || !NeedsEditableSetup(scene))
                return;

            setupQueued = true;
            EditorApplication.delayCall += RunSetup;
        }

        private static bool NeedsEditableSetup(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                return false;

            GameObject[] roots = scene.GetRootGameObjects();
            GameObject office = null;
            int officeMatches = 0;
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name != "OfficeGameplay") continue;
                office = roots[i];
                officeMatches++;
            }

            // Exact-path validation and useful error reporting are handled by the
            // setup tool. Only decide here whether an editable UI is absent.
            return officeMatches == 1 && office.transform.Find("FinalGameplayUI") == null;
        }

        private static void RunSetup()
        {
            setupQueued = false;
            if (setupRunning || !NeedsEditableSetup(SceneManager.GetActiveScene()))
                return;

            setupRunning = true;
            try
            {
                LawyerRoomGameplaySetup.SetupLawyerRoomGameplay();
                Debug.Log("[FinalGameplay] Editable Lawyer Room Canvas was created and saved automatically.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FinalGameplay] Automatic editable Canvas setup failed: {exception.Message}\n{exception}");
            }
            finally
            {
                setupRunning = false;
            }
        }
    }
}
#endif
