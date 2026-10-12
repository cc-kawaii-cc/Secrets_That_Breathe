using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsThatBreathe
{
    public sealed class ScreenFadeUI : MonoBehaviour
    {
        public CanvasGroup fadeGroup;
        private void Start() { Clear(); }
        private void OnDisable() { Clear(); }

        public static ScreenFadeUI FindOrCreate(SeatPromptUI canvas)
        {
            var existing = canvas.GetComponentInChildren<ScreenFadeUI>(true);
            if (existing != null) return existing;
            var panel = new GameObject("StandUpFade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(Image));
            panel.transform.SetParent(SeatingCanvasGroups.Group(canvas, SeatingCanvasGroups.Gameplay), false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var overlay = panel.GetComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = 1000;
            var image = panel.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            var ui = panel.AddComponent<ScreenFadeUI>();
            ui.fadeGroup = panel.GetComponent<CanvasGroup>();
            ui.Clear();
            return ui;
        }

        public void Clear()
        {
            if (fadeGroup == null) return;
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;
        }

        public IEnumerator FadeTo(float targetAlpha, float seconds)
        {
            if (fadeGroup == null) yield break;
            float start = fadeGroup.alpha;
            seconds = Mathf.Max(0.05f, seconds);
            for (float time = 0f; time < seconds; time += Time.deltaTime)
            {
                fadeGroup.alpha = Mathf.Lerp(start, Mathf.Clamp01(targetAlpha), Mathf.SmoothStep(0f, 1f, time / seconds));
                yield return null;
            }
            fadeGroup.alpha = Mathf.Clamp01(targetAlpha);
        }
    }
}
