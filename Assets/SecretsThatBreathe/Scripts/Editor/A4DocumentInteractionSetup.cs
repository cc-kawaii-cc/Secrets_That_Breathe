using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class A4DocumentInteractionSetup
    {
        private static bool queued;
        static A4DocumentInteractionSetup()
        {
            Queue();
            EditorApplication.hierarchyChanged += Queue;
            EditorSceneManager.sceneOpened += (scene, mode) => Queue();
        }
        private static void Queue()
        {
            if (queued || EditorApplication.isPlayingOrWillChangePlaymode) return;
            queued = true;
            EditorApplication.delayCall += () => { queued = false; Install(); };
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            Transform document = null;
            PlayerSeatController player = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (player == null) player = root.GetComponentInChildren<PlayerSeatController>(true);
                foreach (var item in root.GetComponentsInChildren<Transform>(true)) if (item.name == "A4Document_Single") document = item;
            }
            if (document == null || player == null) return;
            var interaction = document.GetComponent<LookTextInteraction>();
            if (interaction != null && interaction.repeatSettingsRevision < 1)
            {
                Undo.RecordObject(interaction, "Allow repeated A4 document inspection");
                interaction.interactOnce = false;
                interaction.repeatSettingsRevision = 1;
                Save(interaction);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            if (interaction != null && interaction.player != null && interaction.promptText != null && interaction.messageText != null
                && interaction.interactionZone != null && interaction.lookTarget != null) return;
            if (interaction == null) interaction = Undo.AddComponent<LookTextInteraction>(document.gameObject);
            Undo.RecordObject(interaction, "Configure A4 document thought");
            if (interaction.repeatSettingsRevision < 1)
            {
                interaction.interactOnce = false;
                interaction.repeatSettingsRevision = 1;
            }
            interaction.player = player;
            if (interaction.interactionZone == null) interaction.interactionZone = Box(document, "InteractionZone", true);
            if (interaction.lookTarget == null) interaction.lookTarget = Box(document, "LookTarget", false);
            var canvas = SeatPromptUI.FindOrCreate(scene);
            var group = SeatingCanvasGroups.Group(canvas, "A4DocumentUI");
            if (interaction.promptText == null) interaction.promptText = Text(canvas, group, "A4DocumentPrompt", "[E] ดู");
            if (interaction.messageText == null) interaction.messageText = Text(canvas, group, "A4DocumentMessage", "เอกสารพวกนี้สำคัญมาก หายไม่ได้เลยนะ");
            Save(interaction);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        private static TMP_Text Text(SeatPromptUI canvas, Transform group, string name, string content)
        {
            var existing = group.Find(name);
            if (existing != null) return existing.GetComponentInChildren<TMP_Text>(true);
            var panel = Object.Instantiate(canvas.promptPanel, group, false);
            Undo.RegisterCreatedObjectUndo(panel, "Create A4 document text");
            panel.name = name;
            var rect = panel.GetComponent<RectTransform>();
            SeatPromptUI.PositionCommonTextPanel(rect);
            rect.sizeDelta = new Vector2(1400f, 120f);
            var image = panel.GetComponent<UnityEngine.UI.Image>();
            if (image != null) image.enabled = false;
            var label = panel.GetComponentInChildren<TMP_Text>(true);
            label.text = content;
            panel.SetActive(false);
            return label;
        }
        private static BoxCollider Box(Transform owner, string name, bool proximity)
        {
            var existing = owner.Find(name);
            if (existing != null && existing.TryGetComponent<BoxCollider>(out var previous)) return previous;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create A4 interaction box");
            go.transform.SetParent(owner, false);
            var box = Undo.AddComponent<BoxCollider>(go);
            box.isTrigger = true;
            Bounds bounds = default;
            bool found = false;
            foreach (var renderer in owner.GetComponentsInChildren<Renderer>(true))
                for (int i = 0; i < 8; i++)
                {
                    var local = renderer.localBounds;
                    var corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var point = go.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point);
                }
            box.center = found ? bounds.center : Vector3.zero;
            var scale = go.transform.lossyScale;
            var perMetre = new Vector3(1f / Mathf.Max(.001f, Mathf.Abs(scale.x)), 1f / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1f / Mathf.Max(.001f, Mathf.Abs(scale.z)));
            box.size = proximity ? perMetre * 2.5f : (found ? bounds.size : Vector3.one * .2f) + perMetre * .02f;
            Save(box);
            return box;
        }
        private static void Save(Object item)
        {
            EditorUtility.SetDirty(item);
            PrefabUtility.RecordPrefabInstancePropertyModifications(item);
        }
    }
}
