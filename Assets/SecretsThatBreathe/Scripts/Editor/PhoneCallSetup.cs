using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class PhoneCallSetup
    {
        private static readonly string[] Sentences = {
            "สวัสดีครับ", "เข้มเหรอ", "ใช่ครับพี่ มีอะไรรึเปล่าครับ",
            "พอดีพี่น่าจะลืมเอกสารไว้ที่ออฟฟิศน่ะ",
            "พี่พยายามหาเอกสารที่บ้านแล้ว หาที่รถ หาในกระเป๋าก็ยังไม่เจอ",
            "พี่ฝากเข้มหาในออฟฟิศให้พี่หน่อยได้ไหม", "ได้ครับพี่",
            "มันน่าจะอยู่ที่โต๊ะเครื่องปริ้น ชั้นวางเอกสาร ไม่ก็ข้างบนลิ้นชักสักที่",
            "ฝากหาให้พี่ด้วยนะ", "โอเคครับ"
        };
        static PhoneCallSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            GameObject phone = null;
            PlayerSeatController player = null;
            Light indicator = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (player == null) player = root.GetComponentInChildren<PlayerSeatController>(true);
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                {
                    if (item.name == "Phone (5)") phone = item.gameObject;
                    if (item.name == "Point Light" || item.name == "PhoneCallIndicatorLight") indicator = item.GetComponent<Light>();
                }
            }
            if (phone == null || player == null) return;
            var call = phone.GetComponent<PhoneCallInteraction>();
            if (call != null && call.dialogueUI != null && call.interactionZone != null && call.intro != null) return;
            var canvas = SeatPromptUI.FindOrCreate(scene);
            var intro = OfficeIntroDialogue.Install(canvas);
            var group = SeatingCanvasGroups.Group(canvas, "PhoneCallUI");
            var ui = group.GetComponent<PhoneCallDialogueUI>();
            if (ui == null)
            {
                ui = Undo.AddComponent<PhoneCallDialogueUI>(group.gameObject);
                var thought = canvas.EnsureNamedText("PhoneRingThought");
                var prompt = canvas.EnsureNamedText("PhoneAnswerPrompt");
                thought.transform.parent.SetParent(group, false);
                prompt.transform.parent.SetParent(group, false);
                ui.ringThought = thought.transform.parent.gameObject;
                ui.answerPrompt = prompt.transform.parent.gameObject;
                var conversation = RectObject("Conversation", group);
                Stretch(conversation);
                ui.conversationPanel = conversation.gameObject;
                ui.kemText = Row("CallRowKem", "เข้ม", 55f, conversation, canvas);
                ui.tonText = Row("CallRowTon", "พี่ต้น", -65f, conversation, canvas);
                var templates = RectObject("Lines", group);
                Stretch(templates);
                ui.sentences = new TMP_Text[Sentences.Length];
                for (int i = 0; i < Sentences.Length; i++)
                {
                    var panel = RectObject("Line" + (i + 1).ToString("00"), templates);
                    ui.sentences[i] = Label("Text", panel, Sentences[i], 0f, 900f, canvas, TextAlignmentOptions.TopLeft);
                }
                templates.gameObject.SetActive(false);
                ui.Hide();
                EditorUtility.SetDirty(ui);
            }
            if (call == null) call = Undo.AddComponent<PhoneCallInteraction>(phone);
            Undo.RecordObject(call, "Configure incoming phone call");
            call.player = player;
            call.intro = intro;
            call.dialogueUI = ui;
            call.indicatorLight = indicator;
            if (indicator != null)
            {
                Undo.RecordObject(indicator.gameObject, "Name phone call light");
                indicator.gameObject.name = "PhoneCallIndicatorLight";
                Undo.RecordObject(indicator, "Use realtime phone light");
                indicator.lightmapBakeType = LightmapBakeType.Realtime;
                call.maximumIntensity = Mathf.Max(.1f, indicator.intensity);
                EditorUtility.SetDirty(indicator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(indicator);
            }
            if (call.interactionZone == null)
            {
                var zone = new GameObject("InteractionZone", typeof(BoxCollider));
                Undo.RegisterCreatedObjectUndo(zone, "Create phone answer area");
                zone.transform.SetParent(phone.transform, false);
                Bounds bounds = new Bounds(phone.transform.position, Vector3.zero);
                bool found = false;
                foreach (var mesh in phone.GetComponentsInChildren<Renderer>(true))
                {
                    if (!found) { bounds = mesh.bounds; found = true; } else bounds.Encapsulate(mesh.bounds);
                }
                zone.transform.position = bounds.center;
                var scale = zone.transform.lossyScale;
                var box = zone.GetComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(3f / Mathf.Max(.001f, Mathf.Abs(scale.x)), 3f / Mathf.Max(.001f, Mathf.Abs(scale.y)), 3f / Mathf.Max(.001f, Mathf.Abs(scale.z)));
                call.interactionZone = box;
            }
            EditorUtility.SetDirty(call);
            PrefabUtility.RecordPrefabInstancePropertyModifications(call);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        private static RectTransform RectObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static TMP_Text Row(string name, string speaker, float y, Transform parent, SeatPromptUI canvas)
        {
            var row = RectObject(name, parent);
            row.anchorMin = row.anchorMax = new Vector2(.05f, .5f);
            row.pivot = new Vector2(0f, .5f);
            row.anchoredPosition = new Vector2(0f, y);
            row.sizeDelta = new Vector2(1020f, 100f);
            Label("Speaker", row, speaker, 0f, 90f, canvas, TextAlignmentOptions.TopRight);
            Label("Colon", row, ":", 100f, 20f, canvas, TextAlignmentOptions.TopLeft);
            return Label("Sentence", row, "", 130f, 870f, canvas, TextAlignmentOptions.TopLeft);
        }
        private static TMP_Text Label(string name, Transform parent, string text, float x, float width, SeatPromptUI canvas, TextAlignmentOptions alignment)
        {
            var rect = RectObject(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, 100f);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = canvas.promptLabel.font;
            label.fontSize = 30f;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }
    }
}
