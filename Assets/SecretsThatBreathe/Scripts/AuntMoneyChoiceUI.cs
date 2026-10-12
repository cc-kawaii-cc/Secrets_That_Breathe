using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsThatBreathe
{
    public sealed class AuntMoneyChoiceUI : MonoBehaviour
    {
        public Button acceptButton;
        public Button refuseButton;
        public TMP_Text acceptLabel;
        public TMP_Text refuseLabel;
        private bool cursorCaptured;
        private CursorLockMode previousLock;
        private bool previousVisible;

        public void ShowChoices()
        {
            EnsureEventSystem();
            if (!cursorCaptured)
            {
                previousLock = Cursor.lockState;
                previousVisible = Cursor.visible;
                cursorCaptured = true;
            }
            gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void HideChoices()
        {
            if (cursorCaptured)
            {
                cursorCaptured = false;
                Cursor.lockState = previousLock;
                Cursor.visible = previousVisible;
            }
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void OnDisable() { HideChoices(); }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var system = new GameObject("SeatingEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(system, gameObject.scene);
            system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public static AuntMoneyChoiceUI FindOrCreate(SeatPromptUI canvas, TMP_Text textTemplate)
        {
            var existing = canvas.GetComponentInChildren<AuntMoneyChoiceUI>(true);
            if (existing != null) return existing;
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
            var panel = new GameObject("AuntMoneyChoices", typeof(RectTransform));
            panel.transform.SetParent(SeatingCanvasGroups.Group(canvas, SeatingCanvasGroups.Aunt), false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.35f);
            rect.sizeDelta = new Vector2(560f, 180f);
            var ui = panel.AddComponent<AuntMoneyChoiceUI>();
            ui.acceptButton = MakeButton(panel.transform, "AcceptMoney", "รับเงิน", 45f, textTemplate, out var acceptText);
            ui.refuseButton = MakeButton(panel.transform, "RefuseMoney", "ไม่รับเงิน", -45f, textTemplate, out var refuseText);
            ui.acceptLabel = acceptText;
            ui.refuseLabel = refuseText;
            var topNavigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = ui.refuseButton };
            var bottomNavigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = ui.acceptButton };
            ui.acceptButton.navigation = topNavigation;
            ui.refuseButton.navigation = bottomNavigation;
            panel.SetActive(false);
            return ui;
        }

        private static Button MakeButton(Transform parent, string objectName, string text, float y, TMP_Text template, out TMP_Text label)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(560f, 70f);
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.20f, 0.24f, 0.9f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 6f);
            labelRect.offsetMax = new Vector2(-12f, -6f);
            label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = template != null ? template.font : TMP_Settings.defaultFontAsset;
            label.fontSize = 30f;
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return button;
        }
    }
}
