using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class ItemInspectionSetup
    {
        private static bool queued;
        private const string Note = "พี่ต้นชอบใช้แต่คนอื่น ทำอะไรเองไม่เป็น แค่ปริ้นเอกสารแผ่นสองแผ่นยังต้องใช้คนอื่นเลย ชงกาแฟก็ใช้คนอื่นไปทำให้ ทุกคนในออฟฟิศแทบจะทำให้ทุกอย่างและ เห็นเป็นรุ่นพี่ที่ทำงานเฉยๆนะ เลยไม่มีใครกล้าทำอะไร";
        static ItemInspectionSetup()
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
            Transform model = null;
            PlayerSeatController player = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (player == null) player = root.GetComponentInChildren<PlayerSeatController>(true);
                foreach (var item in root.GetComponentsInChildren<Transform>(true)) if (item.name == "Samud") model = item;
            }
            if (model == null || player == null) return;
            var interaction = model.GetComponent<ItemInspectionInteraction>();
            if (interaction != null)
            {
                UpgradeHeldFacing(interaction);
                UpgradeInspectionControls(interaction);
                UpgradeReadAudio(interaction);
                UpgradeTemporaryRenderingLayer(interaction);
                InstallInspectableProps(scene, interaction);
                OrganizeInspectionUI(interaction);
            }
            if (interaction != null && interaction.player != null && interaction.inspectionUI != null
                && interaction.lookTarget != null && interaction.interactionZone != null) return;
            if (interaction == null) interaction = Undo.AddComponent<ItemInspectionInteraction>(model.gameObject);
            UpgradeHeldFacing(interaction);
            Undo.RecordObject(interaction, "Set up Samud notebook inspection");
            interaction.player = player;
            if (interaction.lookTarget == null) interaction.lookTarget = Box(model, "LookTarget", false);
            if (interaction.interactionZone == null) interaction.interactionZone = Box(model, "InteractionZone", true);
            // Moving a statically batched renderer would leave its visible mesh on the desk.
            foreach (var item in model.GetComponentsInChildren<Transform>(true))
            {
                Undo.RecordObject(item.gameObject, "Allow notebook pickup");
                GameObjectUtility.SetStaticEditorFlags(item.gameObject, (StaticEditorFlags)0);
                Save(item.gameObject);
            }
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Undo.RecordObject(renderer, "Use lighting for a moving notebook");
                if (renderer is MeshRenderer meshRenderer) meshRenderer.receiveGI = ReceiveGI.LightProbes;
                renderer.lightmapIndex = -1;
                Save(renderer);
            }
            var canvas = SeatPromptUI.FindOrCreate(scene);
            var group = SeatingCanvasGroups.Group(canvas, "SamudUI");
            var ui = group.GetComponent<ItemInspectionUI>();
            if (ui == null)
            {
                ui = Undo.AddComponent<ItemInspectionUI>(group.gameObject);
                ui.lookPrompt = Hint(canvas, group, "SamudLookPrompt", "[E] ดู");
                ui.heldPrompt = Hint(canvas, group, "SamudHeldPrompt", "[R] อ่าน    [E] วาง");
                var overlay = Rect("SamudNoteCanvas", group);
                Stretch(overlay);
                var noteCanvas = Undo.AddComponent<Canvas>(overlay.gameObject);
                noteCanvas.overrideSorting = true;
                noteCanvas.sortingOrder = 200;
                ui.noteCanvas = overlay.gameObject;
                var background = Rect("DimBackground", overlay);
                Stretch(background);
                ui.dimBackground = Undo.AddComponent<Image>(background.gameObject);
                ui.dimBackground.color = Color.clear;
                ui.dimBackground.raycastTarget = false;
                var paper = Rect("NotePanel", overlay);
                paper.anchorMin = paper.anchorMax = new Vector2(.5f, .5f);
                paper.sizeDelta = new Vector2(1000f, 550f);
                var paperImage = Undo.AddComponent<Image>(paper.gameObject);
                paperImage.color = new Color(.94f, .92f, .85f, 1f);
                paperImage.raycastTarget = false;
                ui.notePanel = paper.gameObject;
                ui.noteText = Label("NoteText", paper, canvas, Note);
                var body = ui.noteText.rectTransform;
                Stretch(body);
                body.offsetMin = new Vector2(55f, 90f);
                body.offsetMax = new Vector2(-55f, -55f);
                ui.noteText.alignment = TextAlignmentOptions.TopLeft;
                ui.noteText.fontSize = 30f;
                ui.noteText.lineSpacing = 12f;
                ui.noteText.color = new Color(.12f, .11f, .09f, 1f);
                ui.closePrompt = Label("ClosePrompt", paper, canvas, "[R] ปิด");
                var close = ui.closePrompt.rectTransform;
                close.anchorMin = close.anchorMax = new Vector2(.5f, 0f);
                close.pivot = new Vector2(.5f, 0f);
                close.anchoredPosition = new Vector2(0f, 22f);
                close.sizeDelta = new Vector2(500f, 55f);
                ui.closePrompt.alignment = TextAlignmentOptions.Center;
                ui.closePrompt.color = new Color(.2f, .18f, .15f, 1f);
                ui.Clear();
                Save(ui);
            }
            interaction.inspectionUI = ui;
            UpgradeInspectionControls(interaction);
            UpgradeReadAudio(interaction);
            UpgradeTemporaryRenderingLayer(interaction);
            InstallInspectableProps(scene, interaction);
            OrganizeInspectionUI(interaction);
            Save(interaction);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        private static void OrganizeInspectionUI(ItemInspectionInteraction interaction)
        {
            if (interaction.inspectionUI == null) return;
            var canvas = interaction.inspectionUI.GetComponentInParent<SeatPromptUI>();
            if (canvas == null) return;
            var existing = canvas.transform.Find("ItemInspectionUI");
            var group = existing != null ? existing : SeatingCanvasGroups.Group(canvas, "ItemInspectionUI");
            if (existing == null) Undo.RegisterCreatedObjectUndo(group.gameObject, "Group item inspection UI");
            bool changed = existing == null;
            foreach (var ui in canvas.GetComponentsInChildren<ItemInspectionUI>(true))
            {
                if (ui.transform.parent == group) continue;
                var rect = ui.transform as RectTransform;
                if (rect == null) continue;
                Vector2 min = rect.anchorMin, max = rect.anchorMax, pivot = rect.pivot, size = rect.sizeDelta;
                Vector3 position = rect.anchoredPosition3D, scale = rect.localScale;
                Quaternion rotation = rect.localRotation;
                Undo.SetTransformParent(rect, group, "Group item inspection UI");
                Undo.RecordObject(rect, "Preserve inspection UI layout");
                rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
                rect.sizeDelta = size; rect.anchoredPosition3D = position;
                rect.localScale = scale; rect.localRotation = rotation;
                Save(rect);
                changed = true;
            }
            if (changed) EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);
        }
        private static void UpgradeTemporaryRenderingLayer(ItemInspectionInteraction interaction)
        {
            if (interaction.inspectionControlsRevision < 2 || interaction.inspectionControlsRevision >= 3) return;
            foreach (var renderer in interaction.GetComponentsInChildren<Renderer>(true))
            {
                Undo.RecordObject(renderer, "Enable inspection lighting only while held");
                renderer.renderingLayerMask &= ~ItemInspectionInteraction.InspectionRenderingLayer;
                Save(renderer);
            }
            Undo.RecordObject(interaction, "Upgrade temporary inspection lighting");
            interaction.inspectionControlsRevision = 3;
            Save(interaction);
            EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);
        }
        private static void InstallInspectableProps(Scene scene, ItemInspectionInteraction template)
        {
            if (template.inspectionUI == null || template.inspectionControlsRevision < 3) return;
            Transform group = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                    if (item.name == "InspectableObjects") group = item;
            if (group == null) return;
            foreach (Transform item in group)
            {
                if (item == template.transform || !(item.name.StartsWith("Tabletop photo frame")
                    || item.name == "Book1" || item.name.StartsWith("Book1 (")
                    || item.name == "Book2" || item.name.StartsWith("Book2 (")
                    || item.name == "Book3" || item.name.StartsWith("Book3 ("))) continue;
                var existing = item.GetComponent<ItemInspectionInteraction>();
                if (existing != null)
                {
                    RepairPropReferences(existing, template);
                    continue;
                }
                var prop = Undo.AddComponent<ItemInspectionInteraction>(item.gameObject);
                prop.player = template.player;
                prop.playerCamera = template.playerCamera;
                prop.allowReading = false;
                prop.heldPosition = template.heldPosition;
                prop.enableHeldRotation = template.enableHeldRotation;
                prop.heldRotation = template.heldRotation;
                prop.heldFacingRevision = template.heldFacingRevision;
                prop.heldSize = template.heldSize;
                prop.pickupDuration = template.pickupDuration;
                prop.putDownDuration = template.putDownDuration;
                prop.mouseRotationSensitivity = template.mouseRotationSensitivity;
                prop.zoomMinDistance = template.zoomMinDistance;
                prop.zoomMaxDistance = template.zoomMaxDistance;
                prop.startZoomAtMidpoint = template.startZoomAtMidpoint;
                prop.zoomSensitivity = template.zoomSensitivity;
                prop.zoomSmoothTime = template.zoomSmoothTime;
                prop.inspectionLight = template.inspectionLight;
                prop.enableInspectionLight = template.enableInspectionLight;
                prop.inspectionControlsRevision = 3;
                prop.lookTarget = Box(item, "LookTarget", false);
                prop.interactionZone = Box(item, "InteractionZone", true);
                if (template.interactionZone != null)
                {
                    Vector3 sourceScale = template.interactionZone.transform.lossyScale;
                    Vector3 targetScale = prop.interactionZone.transform.lossyScale;
                    Vector3 size = template.interactionZone.size;
                    prop.interactionZone.size = new Vector3(
                        size.x * Mathf.Abs(sourceScale.x) / Mathf.Max(.001f, Mathf.Abs(targetScale.x)),
                        size.y * Mathf.Abs(sourceScale.y) / Mathf.Max(.001f, Mathf.Abs(targetScale.y)),
                        size.z * Mathf.Abs(sourceScale.z) / Mathf.Max(.001f, Mathf.Abs(targetScale.z)));
                    Save(prop.interactionZone);
                }
                foreach (var child in item.GetComponentsInChildren<Transform>(true))
                {
                    Undo.RecordObject(child.gameObject, "Allow inspectable prop to move");
                    GameObjectUtility.SetStaticEditorFlags(child.gameObject, (StaticEditorFlags)0);
                    Save(child.gameObject);
                }
                foreach (var renderer in item.GetComponentsInChildren<Renderer>(true))
                {
                    Undo.RecordObject(renderer, "Use dynamic inspection lighting");
                    if (renderer is MeshRenderer mesh) mesh.receiveGI = ReceiveGI.LightProbes;
                    renderer.lightmapIndex = -1;
                    renderer.renderingLayerMask &= ~ItemInspectionInteraction.InspectionRenderingLayer;
                    Save(renderer);
                }
                // Separate prompts prevent idle props from hiding the active prop's UI.
                var uiObject = Object.Instantiate(template.inspectionUI.gameObject, template.inspectionUI.transform.parent, false);
                Undo.RegisterCreatedObjectUndo(uiObject, "Create inspect-only prop controls");
                uiObject.name = item.name + "InspectionUI";
                var ui = uiObject.GetComponent<ItemInspectionUI>();
                if (ui.readButton != null) Undo.DestroyObjectImmediate(ui.readButton.gameObject);
                if (ui.noteCanvas != null) Undo.DestroyObjectImmediate(ui.noteCanvas);
                ui.readButton = ui.closeButton = null;
                ui.noteCanvas = ui.notePanel = null;
                ui.noteText = ui.closePrompt = null;
                ui.dimBackground = null;
                ui.heldPrompt.text = "[E] วาง    [RMB] หมุน";
                ui.Clear();
                prop.inspectionUI = ui;
                Save(ui); Save(prop);
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
        private static void RepairPropReferences(ItemInspectionInteraction prop, ItemInspectionInteraction template)
        {
            bool wrongZone = prop.interactionZone == null || !prop.interactionZone.transform.IsChildOf(prop.transform);
            bool wrongTarget = prop.lookTarget == null || !prop.lookTarget.transform.IsChildOf(prop.transform);
            ItemInspectionUI ownUI = null;
            var canvas = template.inspectionUI.GetComponentInParent<SeatPromptUI>();
            if (canvas != null)
                foreach (var ui in canvas.GetComponentsInChildren<ItemInspectionUI>(true))
                    if (ui.name == prop.name + "InspectionUI") { ownUI = ui; break; }
            bool wrongUI = ownUI != null && prop.inspectionUI != ownUI;
            if (!wrongZone && !wrongTarget && !wrongUI) return;
            Undo.RecordObject(prop, "Repair inspectable prop references");
            if (wrongZone) prop.interactionZone = Box(prop.transform, "InteractionZone", true);
            if (wrongTarget) prop.lookTarget = Box(prop.transform, "LookTarget", false);
            if (wrongUI) prop.inspectionUI = ownUI;
            Save(prop);
            EditorSceneManager.MarkSceneDirty(prop.gameObject.scene);
        }
        private static void UpgradeReadAudio(ItemInspectionInteraction interaction)
        {
            if (interaction.readAudioSource != null) return;
            var child = interaction.transform.Find("SamudReadAudio");
            if (child == null)
            {
                var go = new GameObject("SamudReadAudio");
                Undo.RegisterCreatedObjectUndo(go, "Create notebook reading sound");
                go.transform.SetParent(interaction.transform, false);
                child = go.transform;
            }
            var source = child.GetComponent<AudioSource>();
            if (source == null)
            {
                source = Undo.AddComponent<AudioSource>(child.gameObject);
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
                Save(source);
            }
            Undo.RecordObject(interaction, "Assign notebook reading sound");
            interaction.readAudioSource = source;
            Save(interaction);
            EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);
        }
        private static void UpgradeInspectionControls(ItemInspectionInteraction interaction)
        {
            var ui = interaction.inspectionUI;
            if (ui == null) return;
            if (interaction.inspectionControlsRevision >= 1)
            {
                UpgradeZoomAndLighting(interaction);
                return;
            }
            var camera = interaction.playerCamera;
            if (camera == null)
                foreach (var root in interaction.gameObject.scene.GetRootGameObjects())
                    foreach (var candidate in root.GetComponentsInChildren<Camera>(true))
                        if (candidate.CompareTag("MainCamera")) camera = candidate;
            if (camera == null) return;
            Undo.RecordObject(interaction, "Add notebook inspection controls");
            Undo.RecordObject(ui, "Add clickable notebook controls");
            interaction.playerCamera = camera;
            if (interaction.inspectionLight == null)
            {
                var lightObject = new GameObject("SamudInspectionLight");
                Undo.RegisterCreatedObjectUndo(lightObject, "Create temporary notebook light");
                lightObject.transform.SetParent(camera.transform, false);
                lightObject.transform.localPosition = new Vector3(0f, .1f, .25f);
                var light = Undo.AddComponent<Light>(lightObject);
                light.type = LightType.Point;
                light.color = Color.white;
                light.intensity = .5f;
                light.range = 1.2f;
                light.shadows = LightShadows.None;
                light.enabled = false;
                interaction.inspectionLight = light;
                Save(light);
            }
            var canvas = ui.GetComponentInParent<SeatPromptUI>();
            if (canvas.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            if (ui.noteCanvas.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(ui.noteCanvas);
            var held = (RectTransform)ui.heldPrompt.transform.parent;
            Undo.RecordObject(ui.heldPrompt, "Replace notebook keyboard hint with clickable buttons");
            ui.heldPrompt.enabled = false;
            ui.readButton = Control("ReadButton", held, canvas, "[R] อ่าน", new Vector2(-140f, 0f));
            ui.placeButton = Control("PlaceButton", held, canvas, "[E] วาง", new Vector2(140f, 0f));
            // Place close outside the note panel so it shares the normal bottom hint position.
            var closePanel = Rect("CloseControls", ui.noteCanvas.transform);
            SeatPromptUI.PositionCommonTextPanel(closePanel);
            closePanel.sizeDelta = new Vector2(1400f, 120f);
            ui.closeButton = Control("CloseButton", closePanel, canvas, "[R] ปิด", Vector2.zero);
            Undo.DestroyObjectImmediate(ui.closePrompt.gameObject);
            ui.closePrompt = ui.closeButton.GetComponentInChildren<TMP_Text>(true);
            var paper = ui.notePanel.GetComponent<Image>();
            if (paper != null) { Undo.RecordObject(paper, "Remove notebook paper background"); paper.enabled = false; Save(paper); }
            Undo.RecordObject(ui.noteText, "Use white notebook text");
            ui.noteText.color = Color.white;
            ui.Clear();
            interaction.inspectionControlsRevision = 1;
            Save(ui.heldPrompt);
            Save(ui.noteText);
            Save(ui);
            Save(interaction);
            UpgradeZoomAndLighting(interaction);
            EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);
        }
        private static void UpgradeZoomAndLighting(ItemInspectionInteraction interaction)
        {
            if (interaction.inspectionControlsRevision >= 2 || interaction.inspectionLight == null) return;
            const uint notebookLayer = 1u << 7;
            Undo.RecordObject(interaction, "Isolate notebook inspection light and add zoom hint");
            var light = interaction.inspectionLight;
            var data = light.GetComponent<UniversalAdditionalLightData>();
            if (data == null) data = Undo.AddComponent<UniversalAdditionalLightData>(light.gameObject);
            Undo.RecordObject(data, "Restrict inspection light to Samud");
            data.renderingLayers = notebookLayer;
            Save(data);
            // Keep the original room-light layers on Samud. Reserve bit 7 for this light only.
            foreach (var root in interaction.gameObject.scene.GetRootGameObjects())
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    bool notebook = renderer.transform == interaction.transform || renderer.transform.IsChildOf(interaction.transform);
                    uint mask = notebook ? renderer.renderingLayerMask | notebookLayer : renderer.renderingLayerMask & ~notebookLayer;
                    if (mask == renderer.renderingLayerMask) continue;
                    Undo.RecordObject(renderer, "Reserve Samud inspection rendering layer");
                    renderer.renderingLayerMask = mask;
                    Save(renderer);
                }
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets.Length > 0)
            {
                var tags = new SerializedObject(assets[0]);
                var layers = tags.FindProperty("m_RenderingLayers");
                if (layers != null && layers.arraySize > 7)
                {
                    layers.GetArrayElementAtIndex(7).stringValue = "SamudInspection";
                    tags.ApplyModifiedProperties();
                }
            }
            var ui = interaction.inspectionUI;
            var canvas = ui.GetComponentInParent<SeatPromptUI>();
            Undo.RecordObject(ui, "Add notebook rotate hint");
            var read = ui.readButton.GetComponent<RectTransform>();
            var place = ui.placeButton.GetComponent<RectTransform>();
            Undo.RecordObject(read, "Arrange notebook controls");
            Undo.RecordObject(place, "Arrange notebook controls");
            read.anchoredPosition = new Vector2(-280f, 0f);
            place.anchoredPosition = Vector2.zero;
            var hint = Rect("RotateHint", ui.heldPrompt.transform.parent);
            hint.anchorMin = hint.anchorMax = hint.pivot = new Vector2(.5f, .5f);
            hint.anchoredPosition = new Vector2(280f, 0f);
            hint.sizeDelta = new Vector2(260f, 70f);
            var label = Label("Text", hint, canvas, "[RMB] หมุน");
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            ui.rotateHint = hint.gameObject;
            ui.Clear();
            interaction.inspectionControlsRevision = 2;
            Save(read); Save(place); Save(label); Save(ui); Save(interaction);
            EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);
        }
        private static Button Control(string name, Transform parent, SeatPromptUI canvas, string content, Vector2 position)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(260f, 70f);
            var image = Undo.AddComponent<Image>(rect.gameObject);
            image.color = Color.clear;
            image.raycastTarget = true;
            var button = Undo.AddComponent<Button>(rect.gameObject);
            button.targetGraphic = image;
            var label = Label("Text", rect, canvas, content);
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            Save(image); Save(button); Save(label);
            return button;
        }
        private static void UpgradeHeldFacing(ItemInspectionInteraction interaction)
        {
            if (interaction.heldFacingRevision >= 1) return;
            Undo.RecordObject(interaction, "Face the notebook toward the player without turning it upside down");
            interaction.enableHeldRotation = true;
            interaction.heldRotation = new Vector3(90f, 180f, 0f);
            interaction.heldFacingRevision = 1;
            Save(interaction);
            EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create notebook UI");
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static TMP_Text Label(string name, Transform parent, SeatPromptUI canvas, string content)
        {
            var rect = Rect(name, parent);
            var label = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            label.font = canvas.promptLabel.font;
            label.fontSize = canvas.promptLabel.fontSize;
            label.raycastTarget = false;
            label.text = content;
            return label;
        }
        private static TMP_Text Hint(SeatPromptUI canvas, Transform parent, string name, string content)
        {
            var panel = Rect(name, parent);
            SeatPromptUI.PositionCommonTextPanel(panel);
            panel.sizeDelta = new Vector2(1400f, 120f);
            var label = Label("Text", panel, canvas, content);
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            panel.gameObject.SetActive(false);
            return label;
        }
        private static BoxCollider Box(Transform owner, string name, bool proximity)
        {
            var existing = owner.Find(name);
            if (existing != null && existing.TryGetComponent<BoxCollider>(out var foundBox)) return foundBox;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create notebook interaction box");
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
            var padding = new Vector3(1f / Mathf.Max(.001f, Mathf.Abs(scale.x)), 1f / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1f / Mathf.Max(.001f, Mathf.Abs(scale.z)));
            box.size = proximity ? padding * 2.5f : (found ? bounds.size : Vector3.one * .2f) + padding * .02f;
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
