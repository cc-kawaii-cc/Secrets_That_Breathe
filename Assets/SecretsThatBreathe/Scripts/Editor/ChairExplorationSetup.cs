using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class ChairExplorationSetup
    {
        static ChairExplorationSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        }

        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            PlayerSeatController player = null;
            foreach (var root in scene.GetRootGameObjects()) if (player == null) player = root.GetComponentInChildren<PlayerSeatController>(true);
            if (player == null) return;
            var canvas = SeatPromptUI.FindOrCreate(scene);
            var existingUI = canvas.GetComponentInChildren<ChairInspectionUI>(true);
            bool changed = existingUI == null;
            if (existingUI != null && existingUI.NeedsMessageUpgrade) changed = true;
            if (existingUI != null && existingUI.UpgradeHintLayout())
            {
                changed = true;
                EditorUtility.SetDirty(existingUI);
                EditorUtility.SetDirty(existingUI.hintText);
                EditorUtility.SetDirty(existingUI.hintText.rectTransform);
                EditorUtility.SetDirty(existingUI.hintText.transform.parent);
            }
            int before = CountInspections(scene);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var inspection in root.GetComponentsInChildren<ChairInspectionInteractable>(true))
                    if (inspection.NeedsMessageAssignment) changed = true;
            ChairInteractable main = null;
            foreach (var root in scene.GetRootGameObjects()) foreach (var chair in root.GetComponentsInChildren<ChairInteractable>(true))
                if (chair.name == "Chair Kam") main = chair;
            var oldGate = main != null ? main.unlockTrigger : null;
            ChairExplorationInstaller.Install(scene, player, canvas);
            changed |= before != CountInspections(scene) || (main != null && main.unlockTrigger != oldGate);
            if (!changed) return;
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Nemo_Upgrade/Font/Kanit/Kanit-Medium SDF.asset");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var inspection in root.GetComponentsInChildren<ChairInspectionInteractable>(true))
                { EditorUtility.SetDirty(inspection); PrefabUtility.RecordPrefabInstancePropertyModifications(inspection); }
            if (main != null) { EditorUtility.SetDirty(main); PrefabUtility.RecordPrefabInstancePropertyModifications(main); }
            var interactor = player.GetComponent<SeatInteractor>();
            if (interactor != null)
            {
                EditorUtility.SetDirty(interactor); PrefabUtility.RecordPrefabInstancePropertyModifications(interactor);
                if (interactor.chairInspectionUI != null && font != null)
                {
                    EditorUtility.SetDirty(interactor.chairInspectionUI);
                    foreach (var text in interactor.chairInspectionUI.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    {
                        if (text.font == null) text.font = font;
                        EditorUtility.SetDirty(text);
                        EditorUtility.SetDirty(text.transform.parent);
                    }
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static int CountInspections(Scene scene)
        {
            int count = 0;
            foreach (var root in scene.GetRootGameObjects()) count += root.GetComponentsInChildren<ChairInspectionInteractable>(true).Length;
            return count;
        }
    }
}
