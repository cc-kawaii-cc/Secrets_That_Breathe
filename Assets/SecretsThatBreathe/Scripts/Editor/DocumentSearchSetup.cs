using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class DocumentSearchSetup
    {
        private static bool queued;
        private static readonly string[] CallbackLines = {
            "ฮัลโหลครับพี่ต้น", "เจอไหมเข้ม", "เจอครับพี่",
            "โอเคเข้ม\n<pos=-30>:<pos=0>พี่ตกใจหมดนึกว่าพี่ทำหายแล้ว\n<pos=-30>:<pos=0>วางไว้ตรงนั้นแหละเข้ม เดี๋ยวพรุ่งนี้พี่เข้าไปเอา", "ได้เลยครับพี่"
        };
        static DocumentSearchSetup()
        {
            Queue();
            EditorApplication.hierarchyChanged += Queue;
            EditorApplication.projectChanged += Queue;
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
            PhoneCallInteraction phone = null;
            var envelopes = new List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (phone == null) phone = root.GetComponentInChildren<PhoneCallInteraction>(true);
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                    if (item.name == "KraftEnvelope_Open_WithA4" || item.name == "KraftEnvelope_Open_WithA4 (1)" || item.name == "KraftEnvelope_Open_WithA4 (2)") envelopes.Add(item);
            }
            if (phone == null || phone.dialogueUI == null || envelopes.Count != 3) return;
            if (phone.documentSearch != null)
            {
                RepairDocumentReferences(phone.documentSearch, envelopes);
                EnsureCallBackThought(phone.documentSearch);
                EnsureUnfinishedWorkThought(phone.documentSearch);
                EnsurePersistentReminders(phone.documentSearch);
                EnsureTestingHints(phone.documentSearch);
                return;
            }
            var canvas = SeatPromptUI.FindOrCreate(scene);
            var group = SeatingCanvasGroups.Group(canvas, "DocumentSearchUI");
            var sequence = group.GetComponent<DocumentSearchSequence>();
            if (sequence == null) sequence = Undo.AddComponent<DocumentSearchSequence>(group.gameObject);
            Undo.RecordObject(sequence, "Configure document search");
            sequence.phone = phone;
            sequence.checkPrompt = Text(canvas, group, "DocumentCheckPrompt", "[E] เช็คเอกสาร");
            sequence.checkMessages = new[] {
                Text(canvas, group, "DocumentCheckMessage1", "ไม่น่าใช่อันนี้"),
                Text(canvas, group, "DocumentCheckMessage2", "อันนี้ก็ยัง"),
                Text(canvas, group, "DocumentCheckMessage3", "อยู่นี้ไง")
            };
            sequence.callBackPrompt = Text(canvas, group, "PhoneCallBackPrompt", "[E] โทรกลับหาพี่ต้น");
            EnsureCallBackThought(sequence);
            sequence.returnToDeskMessage = Text(canvas, group, "ReturnToDeskMessage", "กลับไปที่โต๊ะทำงาน");
            EnsureUnfinishedWorkThought(sequence);
            EnsurePersistentReminders(sequence);
            var hint = Text(canvas, group, "TestSkipDocumentHint", "[F2] Skip ไปหาเอกสาร");
            var hintRect = hint.transform.parent as RectTransform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0f, 1f);
            hintRect.pivot = new Vector2(0f, 1f);
            hintRect.anchoredPosition = new Vector2(24f, -90f);
            hint.alignment = TextAlignmentOptions.Left;
            sequence.testSkipHint = hintRect.gameObject;
            sequence.testSkipHint.SetActive(true);
            sequence.documents = new DocumentSearchItem[3];
            for (int i = 0; i < envelopes.Count; i++)
            {
                var owner = envelopes[i];
                var item = owner.GetComponent<DocumentSearchItem>();
                if (item == null) item = Undo.AddComponent<DocumentSearchItem>(owner.gameObject);
                Undo.RecordObject(item, "Configure document interaction");
                if (item.lookTarget == null) item.lookTarget = Box(owner, "LookTarget", false);
                if (item.interactionZone == null) item.interactionZone = Box(owner, "InteractionZone", true);
                if (item.indicatorLight == null) item.indicatorLight = owner.GetComponentInChildren<Light>(true);
                if (item.indicatorLight != null)
                {
                    Undo.RecordObject(item.indicatorLight.gameObject, "Name document search light");
                    item.indicatorLight.name = "DocumentSearchIndicatorLight";
                    Undo.RecordObject(item.indicatorLight, "Prepare document search light");
                    item.maximumIntensity = Mathf.Max(.05f, item.indicatorLight.intensity);
                    item.indicatorLight.lightmapBakeType = LightmapBakeType.Realtime;
                    item.indicatorLight.enabled = false;
                    Save(item.indicatorLight);
                }
                sequence.documents[i] = item;
                Save(item);
            }
            var ui = phone.dialogueUI;
            Undo.RecordObject(ui, "Create callback dialogue");
            var templates = new GameObject("CallBackLines", typeof(RectTransform)).transform;
            Undo.RegisterCreatedObjectUndo(templates.gameObject, "Create callback text templates");
            templates.SetParent(ui.transform, false);
            ui.callBackSentences = new TMP_Text[CallbackLines.Length];
            for (int i = 0; i < CallbackLines.Length; i++) ui.callBackSentences[i] = Text(canvas, templates, "CallBackLine" + (i + 1).ToString("00"), CallbackLines[i]);
            templates.gameObject.SetActive(false);
            Undo.RecordObject(phone, "Connect document search to phone");
            phone.documentSearch = sequence;
            EnsureTestingHints(sequence);
            Save(phone); Save(ui); Save(sequence);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        private static void EnsureTestingHints(DocumentSearchSequence sequence)
        {
            var canvas = SeatPromptUI.FindOrCreate(sequence.gameObject.scene);
            if (sequence.phone != null && sequence.phone.intro != null)
                MoveHintToCanvas(sequence.phone.intro.testSkipHint, canvas.transform);
            MoveHintToCanvas(sequence.testSkipHint, canvas.transform);
            if (sequence.testSkipAfterCallHint != null) return;
            var existing = canvas.transform.Find("TestSkipAfterCallHint");
            Undo.RecordObject(sequence, "Assign F3 testing hint");
            if (existing != null) sequence.testSkipAfterCallHint = existing.gameObject;
            else
            {
                var hint = Text(canvas, canvas.transform, "TestSkipAfterCallHint", "[F3] Skip ไปหลังคุยโทรกลับกับพี่ต้น");
                var rect = hint.transform.parent as RectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(24f, -156f);
                hint.alignment = TextAlignmentOptions.Left;
                sequence.testSkipAfterCallHint = rect.gameObject;
                rect.gameObject.SetActive(sequence.enableF3Skip);
            }
            Save(sequence);
            EditorSceneManager.MarkSceneDirty(sequence.gameObject.scene);
        }
        private static void MoveHintToCanvas(GameObject hint, Transform canvas)
        {
            if (hint == null || hint.transform.parent == canvas) return;
            var rect = hint.transform as RectTransform;
            Vector2 position = rect.anchoredPosition;
            Undo.SetTransformParent(rect, canvas, "Move testing shortcut directly under SeatingCanvas");
            Undo.RecordObject(rect, "Preserve testing shortcut position");
            rect.anchoredPosition = position;
            Save(rect);
            EditorSceneManager.MarkSceneDirty(hint.scene);
        }
        private static void EnsurePersistentReminders(DocumentSearchSequence sequence)
        {
            if (sequence.persistentReminderRevision >= 1 || sequence.returnToDeskMessage == null || sequence.callBackThought == null) return;
            Undo.RecordObject(sequence, "Configure persistent objective reminders");
            Undo.RecordObject(sequence.returnToDeskMessage, "Quote desk reminder");
            Undo.RecordObject(sequence.callBackThought, "Quote callback reminder");
            sequence.returnToDeskMessage.text = "\"กลับไปที่โต๊ะทำงาน\"";
            sequence.callBackThought.text = "\"โทรกลับหาพี่ต้น\"";
            sequence.persistentReminderRevision = 1;
            Save(sequence.returnToDeskMessage);
            Save(sequence.callBackThought);
            Save(sequence);
            EditorSceneManager.MarkSceneDirty(sequence.gameObject.scene);
        }
        private static void EnsureUnfinishedWorkThought(DocumentSearchSequence sequence)
        {
            if (sequence.unfinishedWorkThought != null) return;
            var canvas = SeatPromptUI.FindOrCreate(sequence.gameObject.scene);
            var existing = sequence.transform.Find("UnfinishedWorkThought");
            Undo.RecordObject(sequence, "Add unfinished work thought");
            sequence.unfinishedWorkThought = existing != null ? existing.GetComponentInChildren<TMP_Text>(true) : null;
            if (sequence.unfinishedWorkThought == null)
                sequence.unfinishedWorkThought = Text(canvas, sequence.transform, "UnfinishedWorkThought", "ลืมไปเลยว่าเรายังมีงานต้องทำอยู่");
            Save(sequence);
            EditorSceneManager.MarkSceneDirty(sequence.gameObject.scene);
        }
        private static void EnsureCallBackThought(DocumentSearchSequence sequence)
        {
            if (sequence.callBackThought != null) return;
            var canvas = SeatPromptUI.FindOrCreate(sequence.gameObject.scene);
            var existing = sequence.transform.Find("PhoneCallBackThought");
            Undo.RecordObject(sequence, "Add callback reminder");
            sequence.callBackThought = existing != null ? existing.GetComponentInChildren<TMP_Text>(true) : null;
            if (sequence.callBackThought == null)
                sequence.callBackThought = Text(canvas, sequence.transform, "PhoneCallBackThought", "\"โทรกลับหาพี่ต้น\"");
            Save(sequence);
            EditorSceneManager.MarkSceneDirty(sequence.gameObject.scene);
        }
        private static void RepairDocumentReferences(DocumentSearchSequence sequence, List<Transform> envelopes)
        {
            var repaired = new DocumentSearchItem[envelopes.Count];
            bool changed = sequence.documents == null || sequence.documents.Length != repaired.Length;
            for (int i = 0; i < envelopes.Count; i++)
            {
                var owner = envelopes[i];
                var item = owner.GetComponent<DocumentSearchItem>();
                if (item == null) item = Undo.AddComponent<DocumentSearchItem>(owner.gameObject);
                if (item.lookTarget == null || item.interactionZone == null || item.indicatorLight == null)
                {
                    Undo.RecordObject(item, "Repair document interaction references");
                    if (item.lookTarget == null) item.lookTarget = Box(owner, "LookTarget", false);
                    if (item.interactionZone == null) item.interactionZone = Box(owner, "InteractionZone", true);
                    if (item.indicatorLight == null) item.indicatorLight = owner.GetComponentInChildren<Light>(true);
                    Save(item);
                    changed = true;
                }
                repaired[i] = item;
                // The list's order has no effect on message order; preserve it when its membership is valid.
                if (sequence.documents == null || System.Array.IndexOf(sequence.documents, item) < 0) changed = true;
            }
            if (!changed) return;
            Undo.RecordObject(sequence, "Reconnect all three document search items");
            sequence.documents = repaired;
            Save(sequence);
            EditorSceneManager.MarkSceneDirty(sequence.gameObject.scene);
        }
        private static TMP_Text Text(SeatPromptUI canvas, Transform parent, string name, string content)
        {
            var panel = Object.Instantiate(canvas.promptPanel, parent, false);
            panel.name = name;
            Undo.RegisterCreatedObjectUndo(panel, "Create " + name);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .16f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1400f, 150f);
            if (parent.name != "CallBackLines") SeatPromptUI.PositionCommonTextPanel(rect);
            var background = panel.GetComponent<Image>();
            if (background != null) background.enabled = false;
            var text = panel.GetComponentInChildren<TMP_Text>(true);
            text.text = content;
            text.maxVisibleCharacters = int.MaxValue;
            panel.SetActive(false);
            return text;
        }
        private static BoxCollider Box(Transform owner, string name, bool proximity)
        {
            var existing = owner.Find(name);
            if (existing != null && existing.TryGetComponent<BoxCollider>(out var previous)) return previous;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create document " + name);
            go.transform.SetParent(owner, false);
            var box = Undo.AddComponent<BoxCollider>(go);
            box.isTrigger = true;
            Bounds bounds = default;
            bool found = false;
            foreach (var renderer in owner.GetComponentsInChildren<Renderer>(true))
                for (int c = 0; c < 8; c++)
                {
                    var local = renderer.localBounds;
                    var corner = local.center + Vector3.Scale(local.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                    var point = go.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point);
                }
            box.center = found ? bounds.center : Vector3.zero;
            var scale = go.transform.lossyScale;
            if (proximity) box.size = new Vector3(3f / Mathf.Max(.001f, Mathf.Abs(scale.x)), 3f / Mathf.Max(.001f, Mathf.Abs(scale.y)), 3f / Mathf.Max(.001f, Mathf.Abs(scale.z)));
            else
            {
                var padding = new Vector3(.015f / Mathf.Max(.001f, Mathf.Abs(scale.x)), .015f / Mathf.Max(.001f, Mathf.Abs(scale.y)), .015f / Mathf.Max(.001f, Mathf.Abs(scale.z)));
                box.size = (found ? bounds.size : Vector3.one * .2f) + padding;
            }
            Save(box);
            return box;
        }
        private static void Save(Object target)
        {
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
