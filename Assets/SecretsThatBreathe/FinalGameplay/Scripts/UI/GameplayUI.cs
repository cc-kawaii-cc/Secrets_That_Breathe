using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsThatBreathe.FinalGameplay
{
    public sealed class GameplayUI : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private GameObject interactionPromptRoot;
        [SerializeField] private TextMeshProUGUI interactionPromptText;

        [Header("Office sequence")]
        [SerializeField] private GameObject eatPromptRoot;
        [SerializeField] private TextMeshProUGUI eatPromptText;
        [SerializeField] private GameObject subtitleRoot;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private GameObject qteRoot;
        [SerializeField] private Image qteProgressFill;

        public void Configure(
            GameObject interactionRoot,
            TextMeshProUGUI interactionText,
            GameObject eatRoot,
            TextMeshProUGUI eatText,
            GameObject subtitlePanel,
            TextMeshProUGUI subtitleLabel,
            GameObject qtePanel,
            Image progressFill)
        {
            interactionPromptRoot = interactionRoot;
            interactionPromptText = interactionText;
            eatPromptRoot = eatRoot;
            eatPromptText = eatText;
            subtitleRoot = subtitlePanel;
            subtitleText = subtitleLabel;
            qteRoot = qtePanel;
            qteProgressFill = progressFill;
        }

        private void Awake()
        {
            HideAll();
        }

        public void HideAll()
        {
            HideInteractionPrompt();
            ShowEatPrompt(false);
            if (subtitleRoot != null) subtitleRoot.SetActive(false);
            ShowQTE(false);
        }

        public void ShowInteractionPrompt(string value)
        {
            if (interactionPromptText != null) interactionPromptText.text = value;
            if (interactionPromptRoot != null) interactionPromptRoot.SetActive(true);
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptRoot != null) interactionPromptRoot.SetActive(false);
        }

        public void ShowEatPrompt(bool visible)
        {
            if (eatPromptText != null) eatPromptText.text = "[E] กินมาม่า";
            if (eatPromptRoot != null) eatPromptRoot.SetActive(visible);
        }

        public void ShowQTE(bool visible)
        {
            if (qteRoot != null) qteRoot.SetActive(visible);
            SetQTEProgress(0f);
        }

        public void SetQTEProgress(float value)
        {
            if (qteProgressFill != null)
                qteProgressFill.fillAmount = Mathf.Clamp01(value);
        }

        public IEnumerator ShowSubtitle(string line, float duration)
        {
            if (subtitleText == null || subtitleRoot == null)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            subtitleText.text = line;
            subtitleRoot.SetActive(true);
            yield return new WaitForSeconds(duration);
            subtitleRoot.SetActive(false);
        }

        public IEnumerator ShowSubtitles(string[] lines, float durationPerLine)
        {
            if (lines == null || lines.Length == 0) yield break;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    yield return ShowSubtitle(lines[i], durationPerLine);
            }
        }
    }
}
