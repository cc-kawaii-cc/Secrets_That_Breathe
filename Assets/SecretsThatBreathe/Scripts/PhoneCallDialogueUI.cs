using TMPro;
using UnityEngine;

namespace SecretsThatBreathe
{
    public sealed class PhoneCallDialogueUI : MonoBehaviour
    {
        public GameObject conversationPanel;
        public TMP_Text kemText;
        public TMP_Text tonText;
        [Tooltip("Edit each sentence's Text Input under PhoneCallUI/Lines. Even repeated speakers have separate sentence objects.")]
        public TMP_Text[] sentences;
        public TMP_Text[] callBackSentences;
        public GameObject ringThought;
        public GameObject answerPrompt;
        [HideInInspector] public int dialogueRevision;
        public void Hide()
        {
            if (conversationPanel != null) conversationPanel.SetActive(false);
            if (ringThought != null) ringThought.SetActive(false);
            if (answerPrompt != null) answerPrompt.SetActive(false);
        }
        public void Begin()
        {
            Hide();
            kemText.text = tonText.text = "";
            kemText.maxVisibleCharacters = tonText.maxVisibleCharacters = 0;
            conversationPanel.SetActive(true);
        }
    }
}
