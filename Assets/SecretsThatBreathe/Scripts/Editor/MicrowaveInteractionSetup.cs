using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class MicrowaveInteractionSetup
    {
        private static bool installQueued;
        static MicrowaveInteractionSetup()
        {
            QueueInstall();
            EditorSceneManager.sceneOpened += (scene, mode) => QueueInstall();
            EditorApplication.hierarchyChanged += QueueInstall;
            EditorApplication.projectChanged += QueueInstall;
        }
        private static void QueueInstall()
        {
            if (installQueued || EditorApplication.isPlayingOrWillChangePlaymode) return;
            installQueued = true;
            EditorApplication.delayCall += () =>
            {
                installQueued = false;
                Install();
            };
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            GameObject microwave = null;
            PlayerSeatController player = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (player == null) player = root.GetComponentInChildren<PlayerSeatController>(true);
                foreach (var target in root.GetComponentsInChildren<Transform>(true))
                    if (target.name.Equals("microwave", System.StringComparison.OrdinalIgnoreCase)
                        || (microwave == null && target.name.StartsWith("microwave (", System.StringComparison.OrdinalIgnoreCase)))
                        microwave = target.gameObject;
            }
            if (microwave == null) return;
            var interaction = microwave.GetComponent<MicrowaveInteraction>();
            if (interaction != null && interaction.hintPanel != null && interaction.messagePanel != null
                && interaction.interactionZone != null && interaction.lookTarget != null
                && interaction.restoredInteractionSettingsVersion >= 1) return;
            var canvas = SeatPromptUI.FindOrCreate(scene);
            var group = SeatingCanvasGroups.Group(canvas, "MicrowaveUI");
            var hint = canvas.EnsureNamedText("MicrowavePrompt");
            var message = canvas.EnsureNamedText("MicrowaveMessage");
            if (hint.text == "[E] สำรวจ")
            {
                Undo.RecordObject(hint, "Rename microwave prompt");
                hint.text = "[E] อะไรน่ะ";
                EditorUtility.SetDirty(hint);
            }
            hint.transform.parent.SetParent(group, false);
            message.transform.parent.SetParent(group, false);
            if (interaction == null) interaction = Undo.AddComponent<MicrowaveInteraction>(microwave);
            Undo.RecordObject(interaction, "Configure microwave interaction");
            interaction.hintPanel = hint.transform.parent.gameObject;
            interaction.messagePanel = message.transform.parent.gameObject;
            interaction.player = player;
            if (interaction.interactionZone == null)
            {
                var existingZone = microwave.transform.Find("InteractionZone");
                GameObject zone;
                if (existingZone == null)
                {
                    zone = new GameObject("InteractionZone");
                    Undo.RegisterCreatedObjectUndo(zone, "Create microwave interaction area");
                    zone.transform.SetParent(microwave.transform, false);
                }
                else zone = existingZone.gameObject;
                var box = zone.GetComponent<BoxCollider>();
                if (box == null)
                {
                    box = Undo.AddComponent<BoxCollider>(zone);
                    Bounds bounds = new Bounds(microwave.transform.position, Vector3.zero);
                    bool found = false;
                    foreach (var renderer in microwave.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!found) { bounds = renderer.bounds; found = true; }
                        else bounds.Encapsulate(renderer.bounds);
                    }
                    zone.transform.position = bounds.center;
                    Vector3 scale = zone.transform.lossyScale;
                    box.size = new Vector3(3f / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                        3f / Mathf.Max(.001f, Mathf.Abs(scale.y)), 3f / Mathf.Max(.001f, Mathf.Abs(scale.z)));
                    box.isTrigger = true;
                }
                interaction.interactionZone = box;
            }
            if (interaction.restoredInteractionSettingsVersion < 1)
            {
                // Recovered from Lawyer Room at commit 221c80e: local size 2.1370645
                // at scale 1.4037949, equivalent to a 3 metre box in world space.
                var box = interaction.interactionZone;
                Undo.RecordObject(box, "Restore original microwave interaction range");
                Vector3 scale = box.transform.lossyScale;
                box.size = new Vector3(3f / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                    3f / Mathf.Max(.001f, Mathf.Abs(scale.y)), 3f / Mathf.Max(.001f, Mathf.Abs(scale.z)));
                box.center = Vector3.zero;
                box.isTrigger = true;
                interaction.messageDuration = 3f;
                interaction.interactOnce = true;
                interaction.restoredInteractionSettingsVersion = 1;
                EditorUtility.SetDirty(box);
                PrefabUtility.RecordPrefabInstancePropertyModifications(box);
            }
            if (interaction.lookTarget == null)
            {
                var existingTarget = microwave.transform.Find("LookTarget");
                GameObject target;
                if (existingTarget == null)
                {
                    target = new GameObject("LookTarget");
                    Undo.RegisterCreatedObjectUndo(target, "Create microwave crosshair target");
                    target.transform.SetParent(microwave.transform, false);
                }
                else target = existingTarget.gameObject;
                var box = target.GetComponent<BoxCollider>();
                if (box == null)
                {
                    box = Undo.AddComponent<BoxCollider>(target);
                    Bounds localBounds = default;
                    bool found = false;
                    foreach (var renderer in microwave.GetComponentsInChildren<Renderer>(true))
                    {
                        Bounds world = renderer.bounds;
                        for (int corner = 0; corner < 8; corner++)
                        {
                            Vector3 point = world.center + Vector3.Scale(world.extents, new Vector3(
                                (corner & 1) == 0 ? -1f : 1f,
                                (corner & 2) == 0 ? -1f : 1f,
                                (corner & 4) == 0 ? -1f : 1f));
                            point = target.transform.InverseTransformPoint(point);
                            if (!found) { localBounds = new Bounds(point, Vector3.zero); found = true; }
                            else localBounds.Encapsulate(point);
                        }
                    }
                    box.center = localBounds.center;
                    box.size = found ? localBounds.size : Vector3.one * .5f;
                    box.isTrigger = true;
                }
                interaction.lookTarget = box;
            }
            EditorUtility.SetDirty(interaction);
            PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
