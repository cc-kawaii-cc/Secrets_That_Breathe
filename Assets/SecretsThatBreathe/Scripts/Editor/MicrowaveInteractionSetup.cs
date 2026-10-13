using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class MicrowaveInteractionSetup
    {
        static MicrowaveInteractionSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
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
                    if (target.name == "microwave" || (microwave == null && target.name.StartsWith("microwave ("))) microwave = target.gameObject;
            }
            if (microwave == null) return;
            var interaction = microwave.GetComponent<MicrowaveInteraction>();
            if (interaction != null && interaction.hintPanel != null && interaction.messagePanel != null && interaction.interactionZone != null) return;
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
            EditorUtility.SetDirty(interaction);
            PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
