using TMPro;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class SeatPromptUI : MonoBehaviour
    {
        [Tooltip("Only this panel is shown/hidden; the Canvas can contain other UI.")]
        public GameObject promptPanel;
        [Tooltip("Edit Text Input on this TextMeshPro component to write your own sentence.")]
        public TMP_Text promptLabel;
        [Header("Eating prompt")]
        public GameObject eatPanel;
        public TMP_Text eatLabel;
        [HideInInspector] public int commonTextLayoutRevision;

        public static void PositionCommonTextPanel(RectTransform panel)
        {
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .16f);
            panel.pivot = new Vector2(.5f, .5f);
            panel.anchoredPosition = new Vector2(0f, -40f);
        }

        private void Start()
        {
            CrosshairDotGraphic.Install(this);
            if (GetComponent<CanvasTextStackLayout>() == null) gameObject.AddComponent<CanvasTextStackLayout>();
            // Prepare dynamic font atlases before an interaction first makes text visible.
            // Include inactive panels, without changing their text or visibility.
            var labels = GetComponentsInChildren<TMP_Text>(true);
            var fontCharacters = new Dictionary<TMP_FontAsset, StringBuilder>();
            foreach (var label in labels)
            {
                var thai = label.GetComponent<ThaiTextDisplay>();
                if (thai == null) thai = label.gameObject.AddComponent<ThaiTextDisplay>();
                thai.Bind();
                if (label.font == null || string.IsNullOrEmpty(label.text)) continue;
                if (!fontCharacters.TryGetValue(label.font, out var characters))
                {
                    characters = new StringBuilder();
                    fontCharacters.Add(label.font, characters);
                }
                characters.Append(label.text);
                characters.Append(ThaiTextPreparation.Prepare(label.text));
            }
            foreach (var pair in fontCharacters)
                if (pair.Key.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                    pair.Key.TryAddCharacters(pair.Value.ToString(), out string missingCharacters, true);
            foreach (var label in labels) label.ForceMeshUpdate(true, true);
        }

        public void ShowRoomThought(TMP_Text text)
        {
            var inspection = GetComponentInChildren<ChairInspectionUI>(true);
            if (inspection != null) inspection.HideMessages();
            foreach (string name in new[] { "MiddleRoomThought", "FindDeskThought" })
            {
                var panel = SeatingCanvasGroups.FindPanel(this, name);
                if (panel != null) panel.gameObject.SetActive(false);
            }
            if (text != null) text.transform.parent.gameObject.SetActive(true);
        }

        public TMP_Text EnsureNamedText(string panelName)
        {
            var existing = SeatingCanvasGroups.FindPanel(this, panelName);
            if (existing != null) return existing.GetComponentInChildren<TMP_Text>(true);
            string text;
            float y, width, height = 90f;
            switch (panelName)
            {
                case "TestSkipPhoneHint": text = "[F1] Skip ไปรับโทรศัพท์"; y = 0.96f; width = 900f; height = 60f; break;
                case "PhoneRingThought": text = "หือ ใครโทรมา"; y = 0.16f; width = 1400f; height = 120f; break;
                case "PhoneAnswerPrompt": text = "[E] รับโทรศัพท์"; y = 0.16f; width = 800f; break;
                case "OfficeIntroLine1": text = "ตอนนี้กี่โมงแล้วเนี่ย"; y = 0.16f; width = 1400f; height = 120f; break;
                case "OfficeIntroLine2": text = "ทำไมไม่มีใครอยู่ออฟฟิศเลย"; y = 0.16f; width = 1400f; height = 120f; break;
                case "OfficeIntroLine3": text = "กลับกันหมดแล้วเหรอ"; y = 0.16f; width = 1400f; height = 120f; break;
                case "OfficeIntroLine4": text = "ทำไมห้องมันมืดๆแปลกๆจัง"; y = 0.16f; width = 1400f; height = 120f; break;
                case "OfficeIntroLine5": text = "[F] เพื่อเปิดไฟฉาย"; y = 0.16f; width = 1400f; height = 120f; break;
                case "MicrowavePrompt": text = "[E] อะไรน่ะ"; y = 0.16f; width = 700f; break;
                case "MicrowaveMessage": text = "ใครเวฟไก่ทิ้งไว้เนี่ย ไหม้หมดแล้วมั้ง"; y = 0.16f; width = 1400f; height = 120f; break;
                case "DrawerObjective": text = "เอกสารที่ต้องเซ็นต์อยู่ลิ้นชักฝั่งซ้าย"; y = 0.84f; width = 900f; break;
                case "DrawerSearchPrompt": text = "[E] ค้นหาเอกสาร"; y = 0.25f; width = 600f; break;
                case "DrawerSearching": text = "กำลังค้นหาเอกสาร..."; y = 0.25f; width = 700f; break;
                case "MiddleRoomThought": text = "นี่มันกี่โมงแล้วนะ คนอื่นๆกลับกันหมดแล้ว"; y = 0.16f; width = 1400f; height = 120f; break;
                case "FindDeskThought": text = "นี่โต๊ะเราอยู่ไหนนะ"; y = 0.16f; width = 1400f; height = 120f; break;
                case "BeforeEatingThought": text = "ทำงานเหนื่อนมาทั้งวันแล้ว เติมพลังหน่อยดีกว่า"; y = 0.16f; width = 1400f; height = 120f; break;
                case "AfterEatingThought1": text = "อยากกลับบ้านแล้ว"; y = 0.16f; width = 1400f; height = 120f; break;
                case "AfterEatingThought2": text = "ทำไมฉันไม่ทิ้งงานแล้วหนีกลับบ้านไปเลยล่ะ"; y = 0.16f; width = 1400f; height = 120f; break;
                case "AfterEatingThought3": text = "ก็เป็นความคิดที่ดี"; y = 0.16f; width = 1400f; height = 120f; break;
                case "AuntDialogue": text = AuntDialogueUI.DefaultFirstLine; y = 0.14f; width = 1400f; height = 180f; break;
                default: throw new System.ArgumentException("Unknown Canvas text panel: " + panelName);
            }
            var panel = Instantiate(promptPanel, SeatingCanvasGroups.Group(this,
                panelName == "AuntDialogue" ? SeatingCanvasGroups.Aunt : SeatingCanvasGroups.Gameplay));
            panel.name = panelName;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, y);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            if (panelName != "TestSkipPhoneHint") PositionCommonTextPanel(rect);
            var background = panel.GetComponent<Image>();
            if (background != null) background.enabled = false;
            var label = panel.GetComponentInChildren<TMP_Text>(true);
            label.text = text;
            if (label.font == null || !label.font.HasCharacter('ด'))
                foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                    if (font.HasCharacter('ด')) { label.font = font; break; }
            panel.SetActive(false);
            return label;
        }

        public void EnsureEatPrompt()
        {
            if (eatPanel != null || promptPanel == null) return;
            Transform existing = SeatingCanvasGroups.FindPanel(this, "EatPrompt");
            if (existing != null)
            {
                eatPanel = existing.gameObject;
                eatLabel = existing.GetComponentInChildren<TMP_Text>(true);
                return;
            }
            eatPanel = Instantiate(promptPanel, SeatingCanvasGroups.Group(this, SeatingCanvasGroups.Gameplay));
            eatPanel.name = "EatPrompt";
            eatLabel = eatPanel.GetComponentInChildren<TMP_Text>(true);
            if (eatLabel != null) eatLabel.text = "[E] Eat noodles";
            var background = eatPanel.GetComponent<Image>();
            if (background != null) background.enabled = false;
            eatPanel.SetActive(false);
        }

        public void SetEatVisible(bool visible)
        {
            if (eatPanel != null && eatPanel.activeSelf != visible) eatPanel.SetActive(visible);
        }

        public void SetVisible(bool visible)
        {
            // Existing scene prompts may still have the old background component.
            if (promptPanel != null)
            {
                var background = promptPanel.GetComponent<Image>();
                if (background != null) background.enabled = false;
            }
            if (promptPanel != null && promptPanel.activeSelf != visible) promptPanel.SetActive(visible);
        }

        // Does not overwrite existing text, font, position or styling on repeat setup.
        public static SeatPromptUI FindOrCreate(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var existing = root.GetComponentInChildren<SeatPromptUI>(true);
                if (existing != null) { SeatingCanvasGroups.Organize(existing); existing.SetVisible(false); return existing; }
            }
            var canvasObject = new GameObject("SeatingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("SitPrompt", typeof(RectTransform));
            panel.transform.SetParent(canvasObject.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.25f);
            panelRect.sizeDelta = new Vector2(560f, 76f);
            PositionCommonTextPanel(panelRect);

            var textObject = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f, 8f);
            textRect.offsetMax = new Vector2(-20f, -8f);
            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = "[E] Sit";
            label.fontSize = 30f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            var ui = canvasObject.AddComponent<SeatPromptUI>();
            ui.promptPanel = panel;
            ui.promptLabel = label;
            SeatingCanvasGroups.Organize(ui);
            ui.SetVisible(false);
            return ui;
        }
    }
}
