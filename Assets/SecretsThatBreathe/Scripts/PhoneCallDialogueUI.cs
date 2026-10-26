using System.Collections;
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
        [HideInInspector] public string khemName = "เข้ม";
        [HideInInspector] public string tonName = "พี่ต้น";
        public DialogueCanvasView khemDialogue = new DialogueCanvasView();
        public DialogueCanvasView tonDialogue = new DialogueCanvasView();
        public FlowingDialogueSettings presentation = new FlowingDialogueSettings();
        private FlowingDialogue flowing;
        private FlowingDialogue khemFlowing, tonFlowing;
        [HideInInspector] public int dialogueRevision;
        public void Hide()
        {
            flowing?.Hide();
            khemDialogue.Hide();
            tonDialogue.Hide();
            if (conversationPanel != null) conversationPanel.SetActive(false);
            if (ringThought != null) ringThought.SetActive(false);
            if (answerPrompt != null) answerPrompt.SetActive(false);
        }
        public void Begin()
        {
            Hide();
            kemText.text = tonText.text = "";
            kemText.maxVisibleCharacters = tonText.maxVisibleCharacters = 0;
            if (khemFlowing == null) khemFlowing = new FlowingDialogue(khemDialogue, presentation);
            if (tonFlowing == null) tonFlowing = new FlowingDialogue(tonDialogue, presentation);
        }
        public IEnumerator PlayLine(TMP_Text source, bool isKhem)
        {
            flowing?.Hide();
            flowing = isKhem ? khemFlowing : tonFlowing;
            yield return flowing.Play(source != null ? source.text : "");
        }
    }
}
