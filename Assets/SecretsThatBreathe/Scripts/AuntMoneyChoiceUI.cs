using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
        [Header("Transparent button feedback")]
        public Color normalTextColor = new Color(.96f, .96f, .96f, 1f);
        public Color highlightedTextColor = Color.white;
        [Range(1f, 1.25f)] public float highlightedTextScale = 1.08f;
        [Min(.01f)] public float hoverTransitionDuration = .12f;
        private bool cursorCaptured;
        private CursorLockMode previousLock;
        private bool previousVisible;
        private Vector2 lastChoiceCursorPosition;
        private bool hasChoiceCursorPosition;
        private bool restoreChoiceCursorPending;

        public void ShowChoices()
        {
            PrepareButtons();
            EnsureEventSystem();
            if (!cursorCaptured)
            {
                previousLock = Cursor.lockState;
                previousVisible = Cursor.visible;
                cursorCaptured = true;
                restoreChoiceCursorPending = hasChoiceCursorPosition;
            }
            gameObject.SetActive(true);
            ShowChoiceCursor();
        }

        private void LateUpdate()
        {
            // StarterAssetsInputs re-locks the cursor when the window gains focus.
            // Apply the visible menu's cursor policy after focus callbacks, while
            // preserving the gameplay cursor state captured by ShowChoices.
            if (cursorCaptured && Application.isFocused)
            {
                ShowChoiceCursor();
                if (restoreChoiceCursorPending && Mouse.current != null)
                {
                    // Restore once after unlocking; do not pin the mouse while choosing.
                    var position = new Vector2(
                        Mathf.Clamp(lastChoiceCursorPosition.x, 0f, Mathf.Max(0, Screen.width - 1)),
                        Mathf.Clamp(lastChoiceCursorPosition.y, 0f, Mathf.Max(0, Screen.height - 1)));
                    Mouse.current.WarpCursorPosition(position);
                    restoreChoiceCursorPending = false;
                }
            }
        }

        private static void ShowChoiceCursor()
        {
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
        }

        public void PrepareButtons()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null) raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                raycaster.enabled = true;
            }
            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = group.blocksRaycasts = true;
            ConfigureButton(acceptButton, acceptLabel);
            ConfigureButton(refuseButton, refuseLabel);
        }

        private void ConfigureButton(Button button, TMP_Text label)
        {
            if (button == null || label == null) return;
            button.gameObject.SetActive(true);
            label.gameObject.SetActive(true);
            label.enabled = true;
            var image = button.GetComponent<Image>();
            if (image == null) image = button.gameObject.AddComponent<Image>();
            image.enabled = true;
            image.color = Color.clear;
            image.raycastTarget = true;
            image.alphaHitTestMinimumThreshold = 0f;
            button.enabled = button.interactable = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            label.raycastTarget = false;
            label.color = normalTextColor;
            var feedback = button.GetComponent<MoneyChoiceButtonFeedback>();
            if (feedback == null) feedback = button.gameObject.AddComponent<MoneyChoiceButtonFeedback>();
            feedback.Configure(button, label, normalTextColor, highlightedTextColor, highlightedTextScale, hoverTransitionDuration);
        }

        public void HideChoices()
        {
            if (cursorCaptured)
            {
                // Locking the gameplay cursor recentres it, so save the click position first.
                if (Mouse.current != null)
                {
                    lastChoiceCursorPosition = Mouse.current.position.ReadValue();
                    hasChoiceCursorPosition = true;
                }
                restoreChoiceCursorPending = false;
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
            image.color = Color.clear;
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
            label.color = SeatPromptUI.DefaultTextColor;
            label.raycastTarget = false;
            return button;
        }
    }
}
