using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // Upgrade an already configured Lawyer Room once, making the new fields visible before Play.
    [InitializeOnLoad]
    public static class DrawerSearchScareSetup
    {
        static DrawerSearchScareSetup()
        {
            EditorApplication.delayCall += InstallInLoadedScene;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += InstallInLoadedScene;
        }

        private static void InstallInLoadedScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var canvas in root.GetComponentsInChildren<SeatPromptUI>(true))
                    if (SeatingCanvasGroups.Organize(canvas)) EditorSceneManager.MarkSceneDirty(scene);
                var player = root.GetComponentInChildren<PlayerSeatController>(true);
                if (player == null) continue;
                var eating = player.GetComponent<NoodleEatingController>();
                if (eating != null && eating.beforeEatingText == null)
                {
                    eating.ConfigureBeforeEatingUI();
                    var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Champ&Kichzz/Fonts/PKFbasicDemo SDF.asset");
                    if (font != null && (eating.beforeEatingText.font == null || !eating.beforeEatingText.font.HasCharacter('ท')))
                        eating.beforeEatingText.font = font;
                    EditorUtility.SetDirty(eating);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(eating);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
                var existing = player.GetComponent<DrawerSearchScareSequence>();
                if (existing != null)
                {
                    if (existing.auntDialogueText == null || existing.moneyChoiceUI == null || existing.auntDialogueUI == null ||
                        existing.auntDialogueUI.thankYouLine == null || existing.mainDoor == null || existing.auntExitPoint == null || existing.screenFade == null || existing.postMealThoughtUI == null)
                    {
                        existing.ConfigureScene();
                        existing.UseThaiFontIfNeeded(AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Champ&Kichzz/Fonts/PKFbasicDemo SDF.asset"));
                        EditorUtility.SetDirty(existing);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(existing);
                        EditorSceneManager.MarkSceneDirty(scene);
                    }
                    if (existing.screenFade != null && player.sitFade != existing.screenFade)
                    {
                        Undo.RecordObject(player, "Share sit and stand fade");
                        player.sitFade = existing.screenFade;
                        EditorUtility.SetDirty(player);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(player);
                        EditorSceneManager.MarkSceneDirty(scene);
                    }
                    continue;
                }
                var story = Undo.AddComponent<DrawerSearchScareSequence>(player.gameObject);
                if (story == null) continue;
                story.ConfigureScene();
                player.sitFade = story.screenFade;
                EditorUtility.SetDirty(player);
                PrefabUtility.RecordPrefabInstancePropertyModifications(player);
                story.UseThaiFontIfNeeded(AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Champ&Kichzz/Fonts/PKFbasicDemo SDF.asset"));
                EditorUtility.SetDirty(story);
                PrefabUtility.RecordPrefabInstancePropertyModifications(story);
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
