using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    // Optional. The banana feature works without it (trees get the component and the prompt is created when the
    // game starts). Run this once in a scene to save both in the scene, so the prompt text and each tree's
    // settings can be edited in the Hierarchy/Inspector outside Play Mode, then save the scene.
    public static class BananaEatingSetup
    {
        private const string Menu = "Tools/Secrets That Breathe/Banana Eating/Save prompt and tree components in this scene";

        [MenuItem(Menu)]
        public static void Run()
        {
            if (Application.isPlaying) { Debug.LogWarning("[Banana] Stop Play Mode first, then run this again."); return; }
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return;

            int added = 0;
            if (!BananaEatInteraction.RequiresZone(scene))
                foreach (GameObject root in scene.GetRootGameObjects()) added += AddToTrees(root.transform);

            // The prompt label goes under the scene's SeatingCanvas (never a new canvas), the same place it is created
            // from at runtime, so editing its text in the Hierarchy changes what the player sees.
            string promptResult;
            Transform canvas = BananaEatDriver.FindCanvas(scene, false);
            if (canvas == null) promptResult = "no SeatingCanvas found, so no prompt was saved";
            else
            {
                bool existed = BananaEatDriver.FindDeep(canvas, "BananaEatPrompt") != null;
                TMP_Text prompt = BananaEatDriver.BuildLabel(canvas, "BananaEatPrompt", "[E] กินกล้วย", .16f, 700f, 90f, 0f);
                if (prompt != null && !existed) Undo.RegisterCreatedObjectUndo(prompt.gameObject, "Create banana prompt");
                promptResult = existed ? "the prompt was already saved" : "the prompt was saved under SeatingCanvas";
            }

            if (added > 0 || promptResult.Contains("saved under")) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Banana] Added the component to " + added + " banana tree(s); " + promptResult + ". Save the scene (Ctrl+S) to keep it.");
        }

        private static int AddToTrees(Transform candidate)
        {
            if (BananaEatInteraction.IsBananaTreeName(candidate.name))
            {
                if (candidate.GetComponentInChildren<Renderer>(true) == null || candidate.GetComponent<BananaEatInteraction>() != null) return 0;
                Undo.AddComponent<BananaEatInteraction>(candidate.gameObject);
                return 1;
            }
            int added = 0;
            for (int i = 0; i < candidate.childCount; i++) added += AddToTrees(candidate.GetChild(i));
            return added;
        }
    }
}
