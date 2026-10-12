using TMPro;
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
