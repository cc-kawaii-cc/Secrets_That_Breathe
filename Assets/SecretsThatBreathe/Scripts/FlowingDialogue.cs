using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace SecretsThatBreathe
{
    [Serializable]
    public sealed class FlowingDialogueSettings
    {
        [HideInInspector] public Vector2 screenAnchor = new Vector2(.32f, .16f);
        [HideInInspector] [Range(.2f, .8f)] public float widthFraction = .5f;
        [Min(.001f)] public float secondsPerCharacter = .05f;
        [Min(0f)] public float completedLineHold = 1f;
        [Min(1f)] public float scrollSpeed = 18f;
        [HideInInspector] public Color sentenceColor = Color.white;
        [HideInInspector] public Color khemColor = new Color(.25f, .7f, 1f);
        [HideInInspector] public Color otherSpeakerColor = new Color(.08f, .22f, .14f);
    }

    // Owns the upward motion of one sentence. It deliberately does not use the
    // general Canvas overlap layout: only the currently typed lines set its height.
    internal sealed class FlowingDialogue
    {
        private readonly RectTransform root;
        private readonly TMP_Text sentence, speaker;
        private readonly FlowingDialogueSettings settings;
        public bool ReadyToAdvance { get; private set; }

        private readonly DialogueCanvasView view;
        private readonly Vector2 authoredPosition;
        private readonly Vector2 authoredSentencePosition, authoredSpeakerPosition;

        public FlowingDialogue(DialogueCanvasView view, FlowingDialogueSettings settings)
        {
            if (view == null || !view.IsValid) throw new InvalidOperationException("Assign the authored dialogue Canvas in the scene before playing.");
            this.settings = settings;
            this.view = view;
            root = view.motionRoot;
            sentence = view.sentence;
            speaker = view.speaker;
            authoredPosition = root.anchoredPosition;
            authoredSentencePosition = sentence.rectTransform.anchoredPosition;
            authoredSpeakerPosition = speaker.rectTransform.anchoredPosition;
            Hide();
        }

        public IEnumerator Play(string text)
        {
            ReadyToAdvance = false;
            // All layout, font and colour values belong to the saved Canvas objects.
            view.canvas.gameObject.SetActive(true);
            root.anchoredPosition = authoredPosition;
            // Old phone templates use positional tags for the colon column.
            string content = Regex.Replace(text ?? "", @"<pos=[^>]*>\s*:\s*<pos=[^>]*>", "");
            sentence.text = Regex.Replace(content, @"<pos=[^>]*>", "");
            sentence.maxVisibleCharacters = 0;
            sentence.ForceMeshUpdate();
            speaker.ForceMeshUpdate();
            if (view.normalizeFontBaseline)
            {
                AlignFontBaseline(sentence, authoredSentencePosition);
                AlignFontBaseline(speaker, authoredSpeakerPosition);
            }
            float y = FirstBaselineOffset();
            SetPosition(y);
            float elapsed = 0f;
            int count = sentence.textInfo.characterCount;
            while (sentence.maxVisibleCharacters < count)
            {
                elapsed += Time.deltaTime;
                int visible = Mathf.Min(count, Mathf.FloorToInt(elapsed / Mathf.Max(.001f, settings.secondsPerCharacter)));
                sentence.maxVisibleCharacters = visible;
                y = Mathf.Lerp(y, BaselineOffset(visible), 1f - Mathf.Exp(-settings.scrollSpeed * Time.deltaTime));
                SetPosition(y);
                yield return null;
            }
            for (float held = 0f; held < settings.completedLineHold; held += Time.deltaTime)
            {
                y = Mathf.Lerp(y, BaselineOffset(count), 1f - Mathf.Exp(-settings.scrollSpeed * Time.deltaTime));
                SetPosition(y);
                yield return null;
            }
            SetPosition(BaselineOffset(count));
            ReadyToAdvance = true;
        }

        public IEnumerator ShowInstant(string text, float holdDuration)
        {
            ReadyToAdvance = false;
            root.anchoredPosition = authoredPosition;
            sentence.rectTransform.anchoredPosition = authoredSentencePosition;
            speaker.rectTransform.anchoredPosition = authoredSpeakerPosition;
            sentence.text = text ?? "";
            sentence.maxVisibleCharacters = int.MaxValue;
            view.canvas.gameObject.SetActive(true);
            sentence.ForceMeshUpdate();
            yield return new WaitForSeconds(Mathf.Max(0f, holdDuration));
            ReadyToAdvance = true;
        }

        private float FirstBaselineOffset() => 0f;
        private static void AlignFontBaseline(TMP_Text label, Vector2 authored)
        {
            if (label.textInfo.characterCount == 0) return;
            // Thai marks can increase TMP's first-line ascent. Keep the configured
            // font baseline stable without changing the Canvas's authored layout.
            float ascent = label.font.faceInfo.ascentLine * label.fontSize / label.font.faceInfo.pointSize;
            label.rectTransform.anchoredPosition = authored + Vector2.up * (-label.textInfo.characterInfo[0].baseLine - ascent);
        }
        private float BaselineOffset(int visible)
        {
            if (visible <= 0 || sentence.textInfo.characterCount == 0) return FirstBaselineOffset();
            return sentence.textInfo.characterInfo[0].baseLine - sentence.textInfo.characterInfo[Mathf.Min(visible, sentence.textInfo.characterCount) - 1].baseLine;
        }
        private void SetPosition(float y)
        {
            root.anchoredPosition = authoredPosition + Vector2.up * y;
        }

        public void Hide()
        {
            ReadyToAdvance = false;
            root.anchoredPosition = authoredPosition;
            sentence.rectTransform.anchoredPosition = authoredSentencePosition;
            speaker.rectTransform.anchoredPosition = authoredSpeakerPosition;
            view.canvas.gameObject.SetActive(false);
        }
    }
}
