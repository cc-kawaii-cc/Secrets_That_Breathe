using System.Collections;
using TMPro;
using UnityEngine;

namespace SecretsThatBreathe
{
    public sealed class ChairInspectionUI : MonoBehaviour
    {
        public TMP_Text hintText;
        public TMP_Text[] messages = new TMP_Text[5];
        [SerializeField, HideInInspector] private int messageSetVersion;
        public bool NeedsMessageUpgrade => messageSetVersion < 1 || messages == null || messages.Length < 5;
        private static readonly string[] ChairMessages =
        {
            "หนังสือนี้เราเคยอ่านตอนสอบ", "ทำไมไม่ยอมเก็บแก้วกาแฟนะ", "ไม่น่าใช่โต๊ะนี้", "เก้าอี้ตัวนี้ใครนั่งนะ", "เราไม่ได้ปิดโน๊ตบุ๊คหนิ"
        };
        private static readonly string[] PanelNames = { "01_Chair1", "02_Chair2", "03_Chair", "04_Chair3", "05_Chair4" };
        [Min(0.1f)] public float messageDuration = 5f;
        private TMP_Text activeMessage;
        private Coroutine showing;
        [SerializeField, HideInInspector] private int hintLayoutVersion;
        public bool IsShowingMessage => showing != null;

        private void Awake() { UpgradeHintLayout(); }

        public bool UpgradeHintLayout()
        {
            if (hintLayoutVersion >= 1 || hintText == null || messages == null || messages.Length == 0 || messages[0] == null) return false;
            hintText.text = "[E] นั่ง";
            CopyLayout(messages[0].transform.parent as RectTransform, hintText.transform.parent as RectTransform);
            CopyLayout(messages[0].rectTransform, hintText.rectTransform);
            hintLayoutVersion = 1;
            return true;
        }

        private static void CopyLayout(RectTransform source, RectTransform target)
        {
            if (source == null || target == null) return;
            target.anchorMin = source.anchorMin; target.anchorMax = source.anchorMax; target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta; target.anchoredPosition3D = source.anchoredPosition3D;
            target.localRotation = source.localRotation; target.localScale = source.localScale;
        }

        public static ChairInspectionUI FindOrCreate(SeatPromptUI canvas)
        {
            var existing = canvas.GetComponentInChildren<ChairInspectionUI>(true);
            if (existing != null) { existing.UpgradeHintLayout(); existing.UpgradeChairMessages(canvas); return existing; }
            Transform parent = SeatingCanvasGroups.Group(canvas, SeatingCanvasGroups.Gameplay);
            var group = new GameObject("ChairInspectionUI", typeof(RectTransform));
            group.transform.SetParent(parent, false);
            var rect = group.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var ui = group.AddComponent<ChairInspectionUI>();
            ui.hintText = ui.CreateText(canvas, "InspectPrompt", "[E] นั่ง", 0.16f);
            for (int i = 0; i < 5; i++) ui.messages[i] = ui.CreateText(canvas, PanelNames[i], ChairMessages[i], 0.16f);
            ui.messageSetVersion = 1;
            ui.UpgradeHintLayout();
            return ui;
        }

        // One-time replacement requested by the user. Subsequent Canvas edits are kept.
        public void UpgradeChairMessages(SeatPromptUI canvas)
        {
            if (!NeedsMessageUpgrade) return;
            var old = messages;
            messages = new TMP_Text[5];
            for (int i = 0; i < 5; i++)
            {
                var label = old != null && i < old.Length ? old[i] : null;
                if (label == null) label = CreateText(canvas, PanelNames[i], ChairMessages[i], 0.16f);
                label.text = ChairMessages[i];
                label.transform.parent.name = PanelNames[i];
                messages[i] = label;
            }
            messageSetVersion = 1;
        }

        private TMP_Text CreateText(SeatPromptUI canvas, string name, string text, float y)
        {
            var panel = Instantiate(canvas.promptPanel, transform);
            panel.name = name;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, y);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1400f, 120f);
            var background = panel.GetComponent<UnityEngine.UI.Image>();
            if (background != null) background.enabled = false;
            var label = panel.GetComponentInChildren<TMP_Text>(true);
            label.text = text;
            panel.SetActive(false);
            return label;
        }

        public void SetHint(bool visible) { if (hintText != null) hintText.transform.parent.gameObject.SetActive(visible && !IsShowingMessage); }

        public bool ShowMessage(TMP_Text label)
        {
            if (label == null || label.gameObject.scene != gameObject.scene) return false;
            HideMessages();
            var canvas = GetComponentInParent<SeatPromptUI>();
            if (canvas != null) canvas.ShowRoomThought(label);
            else label.transform.parent.gameObject.SetActive(true);
            activeMessage = label;
            showing = StartCoroutine(HideAfterDelay());
            SetHint(false);
            return true;
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, messageDuration));
            showing = null;
            HideMessages();
        }

        public void HideMessages()
        {
            if (showing != null) { StopCoroutine(showing); showing = null; }
            if (activeMessage != null) activeMessage.transform.parent.gameObject.SetActive(false);
            activeMessage = null;
            if (messages != null) foreach (var label in messages) if (label != null) label.transform.parent.gameObject.SetActive(false);
        }
        private void OnDisable() { SetHint(false); HideMessages(); }
    }
}
