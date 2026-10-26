using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class MoneyChoiceButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        private TMP_Text label;
        private Button button;
        private Vector3 originalScale;
        private Color normalColor, highlightColor;
        private float highlightScale, transitionDuration;
        private bool hovered, selected;

        public void Configure(Button owner, TMP_Text text, Color normal, Color highlight, float scale, float duration)
        {
            if (label != text && text != null) originalScale = text.rectTransform.localScale;
            button = owner;
            label = text;
            normalColor = normal;
            highlightColor = highlight;
            highlightScale = Mathf.Max(1f, scale);
            transitionDuration = Mathf.Max(.01f, duration);
            ResetAppearance();
        }

        private void Update()
        {
            if (label == null || button == null) return;
            bool highlighted = button.IsInteractable() && (hovered || selected);
            float blend = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 5f / transitionDuration);
            label.rectTransform.localScale = Vector3.Lerp(label.rectTransform.localScale,
                originalScale * (highlighted ? highlightScale : 1f), blend);
            label.color = Color.Lerp(label.color, highlighted ? highlightColor : normalColor, blend);
        }

        private void ResetAppearance()
        {
            hovered = selected = false;
            if (label == null) return;
            label.rectTransform.localScale = originalScale;
            label.color = normalColor;
            label.canvasRenderer.SetAlpha(1f);
        }

        public void OnPointerEnter(PointerEventData data) => hovered = true;
        public void OnPointerExit(PointerEventData data) => hovered = false;
        public void OnSelect(BaseEventData data) => selected = true;
        public void OnDeselect(BaseEventData data) => selected = false;
        private void OnDisable() => ResetAppearance();
    }
}
