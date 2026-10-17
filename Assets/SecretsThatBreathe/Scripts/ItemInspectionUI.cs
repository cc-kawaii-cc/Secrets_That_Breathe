using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

namespace SecretsThatBreathe
{
    [MovedFrom(true, sourceNamespace: "SecretsThatBreathe", sourceAssembly: "Assembly-CSharp", sourceClassName: "SamudNotebookUI")]
    public sealed class ItemInspectionUI : MonoBehaviour
    {
        public TMP_Text lookPrompt;
        public TMP_Text heldPrompt;
        public TMP_Text noteText;
        public TMP_Text closePrompt;
        public GameObject noteCanvas;
        public GameObject notePanel;
        public Image dimBackground;
        public Button readButton, placeButton, closeButton;
        public GameObject rotateHint;
        private ItemInspectionInteraction owner;

        public void Bind(ItemInspectionInteraction interaction)
        {
            owner = interaction;
            bool canRead = owner.allowReading;
            if (placeButton != null)
                ((RectTransform)placeButton.transform).anchoredPosition = new Vector2(canRead ? 0f : -140f, 0f);
            if (rotateHint != null)
                ((RectTransform)rotateHint.transform).anchoredPosition = new Vector2(canRead ? 280f : 140f, 0f);
            // Retain any user-added Inspector events.
            if (readButton != null) { readButton.onClick.RemoveListener(Read); readButton.onClick.AddListener(Read); }
            if (placeButton != null) { placeButton.onClick.RemoveListener(Place); placeButton.onClick.AddListener(Place); }
            if (closeButton != null) { closeButton.onClick.RemoveListener(Close); closeButton.onClick.AddListener(Close); }
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var system = new GameObject("SeatingEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(system, gameObject.scene);
                system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }
        private void Read() { if (owner != null) owner.ReadNote(); }
        private void Place() { if (owner != null) owner.PlaceNotebook(); }
        private void Close() { if (owner != null) owner.CloseReading(); }
        [Range(0f, 1f)] public float backgroundDarkness = .5f;
        [Min(.01f)] public float backgroundFadeDuration = .3f;

        public void ShowLookPrompt(bool show) => Panel(lookPrompt, show);
        public void ShowHeldPrompt(bool show)
        {
            Panel(heldPrompt, show);
            if (readButton != null) readButton.gameObject.SetActive(show && (owner == null || owner.allowReading));
            if (placeButton != null) placeButton.gameObject.SetActive(show);
            if (rotateHint != null) rotateHint.SetActive(show);
        }
        private static void Panel(TMP_Text label, bool show)
        {
            if (label != null) label.transform.parent.gameObject.SetActive(show);
        }
        public IEnumerator OpenNote()
        {
            ShowHeldPrompt(false);
            noteCanvas.SetActive(true);
            notePanel.SetActive(true);
            if (closeButton != null) closeButton.gameObject.SetActive(true);
            noteText.maxVisibleCharacters = int.MaxValue;
            yield return FadeBackground(backgroundDarkness);
        }
        public IEnumerator CloseNote()
        {
            notePanel.SetActive(false);
            if (closeButton != null) closeButton.gameObject.SetActive(false);
            yield return FadeBackground(0f);
            noteCanvas.SetActive(false);
            ShowHeldPrompt(true);
        }
        private IEnumerator FadeBackground(float target)
        {
            float start = dimBackground.color.a;
            float duration = Mathf.Max(.01f, backgroundFadeDuration);
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                dimBackground.color = new Color(0f, 0f, 0f, Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, time / duration)));
                yield return null;
            }
            dimBackground.color = new Color(0f, 0f, 0f, target);
        }
        public void Clear()
        {
            ShowLookPrompt(false);
            ShowHeldPrompt(false);
            if (closeButton != null) closeButton.gameObject.SetActive(false);
            if (dimBackground != null) dimBackground.color = Color.clear;
            if (noteCanvas != null) noteCanvas.SetActive(false);
        }
        private void Awake() => Clear();
        private void OnDisable() => Clear();
    }
}
