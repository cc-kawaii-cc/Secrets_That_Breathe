using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SecretsThatBreathe.LevelTools
{
    /// <summary>
    /// Rebuilds all three Chapter 2 levels back to back and prints one combined walkability
    /// report, so a change to the shared kit can be checked against every level at once.
    ///
    /// Menu: Tools > Secrets That Breathe > Build ALL Chapter 2 Levels
    /// </summary>
    public static class Ch2BuildAll
    {
        [MenuItem("Tools/Secrets That Breathe/Build ALL Chapter 2 Levels", false, 1)]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Ch2] leave play mode first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Ch2GarageBuilder.BuildScene(false);
            Ch2ParkingBuilder.BuildScene(false);
            Ch2ClubBuilder.BuildScene(false);

            Debug.Log("[Ch2] all three levels rebuilt. Scene paths:\n  " +
                      Ch2GarageBuilder.ScenePath + "\n  " +
                      Ch2ParkingBuilder.ScenePath + "\n  " +
                      Ch2ClubBuilder.ScenePath);
        }

        /// <summary>
        /// Adds the three Chapter 2 scenes to Build Settings if they are missing, and drops the
        /// stale SampleScene entry that currently sits at build index 0.
        /// </summary>
        [MenuItem("Tools/Secrets That Breathe/Fix Build Settings scene list", false, 41)]
        public static void FixBuildSettings()
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            var existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].path == "Assets/Scenes/SampleScene.unity") continue;
                list.Add(existing[i]);
            }

            string[] want =
            {
                Ch2GarageBuilder.ScenePath,
                Ch2ParkingBuilder.ScenePath,
                Ch2ClubBuilder.ScenePath
            };
            for (int i = 0; i < want.Length; i++)
            {
                bool have = false;
                for (int k = 0; k < list.Count; k++)
                    if (list[k].path == want[i]) { have = true; break; }
                if (!have) list.Add(new EditorBuildSettingsScene(want[i], true));
            }

            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[Ch2] Build Settings now lists " + list.Count + " scene(s).");
        }
    }
}
