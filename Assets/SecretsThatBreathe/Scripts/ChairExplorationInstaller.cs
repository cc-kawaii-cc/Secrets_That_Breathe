using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    public static class ChairExplorationInstaller
    {
        private static readonly string[] Names = { "Chair (1)", "Chair (2)", "Chair", "Chair (3)", "Chair (4)" };
        private static bool IsInspectionName(string name) { foreach (var target in Names) if (name == target) return true; return false; }

        public static void Install(Scene scene, PlayerSeatController player, SeatPromptUI canvas)
        {
            ChairInteractable main = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var item in root.GetComponentsInChildren<ChairInteractable>(true))
                    if (item.name == "Chair Kam") main = item;
            if (main == null) return;
            main.unlockTrigger = MiddleRoomThoughtTrigger.InstallDeskThought(scene, player, canvas);
            var interactor = player.GetComponent<SeatInteractor>();
            if (interactor != null && interactor.chairInspectionUI == null) interactor.chairInspectionUI = ChairInspectionUI.FindOrCreate(canvas);
            var inspectionUI = interactor != null ? interactor.chairInspectionUI : ChairInspectionUI.FindOrCreate(canvas);
            inspectionUI.UpgradeChairMessages(canvas);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!IsInspectionName(item.name) || item.GetComponentInChildren<Renderer>(true) == null) continue;
                    bool container = false;
                    foreach (var child in item.GetComponentsInChildren<Transform>(true))
                        if (child != item && IsInspectionName(child.name)) { container = true; break; }
                    if (container) continue; // The Room/Chair folder is not a chair model.
                    var inspection = item.GetComponent<ChairInspectionInteractable>();
                    if (inspection == null) inspection = item.gameObject.AddComponent<ChairInspectionInteractable>();
                    inspection.referenceChair = main;
                    if (inspection.NeedsMessageAssignment && inspectionUI.messages != null && inspectionUI.messages.Length > 0)
                    {
                        int index = System.Array.IndexOf(Names, item.name) % inspectionUI.messages.Length;
                        inspection.AssignInitialMessage(inspectionUI.messages[index]);
                    }
                    inspection.CreateZoneFromReference();
                }
        }
    }
}
