from pathlib import Path
p=Path('Assets/SecretsThatBreathe/Scripts/FlowingDialogue.cs')
s=p.read_text(encoding='utf-8')
a=s.index('        public FlowingDialogue(Transform parent')
b=s.index('        public IEnumerator Play(',a)
s=s[:a]+'''        private readonly DialogueCanvasView view;
        private readonly Vector2 authoredPosition;

        public FlowingDialogue(DialogueCanvasView view, FlowingDialogueSettings settings)
        {
            if (view == null || !view.IsValid) throw new InvalidOperationException("Assign the authored dialogue Canvas in the scene before playing.");
            this.settings = settings;
            this.view = view;
            root = view.motionRoot;
            sentence = view.sentence;
            speaker = view.speaker;
            authoredPosition = root.anchoredPosition;
            Hide();
        }

'''+s[b:]
s=s.replace('        private float sentenceOffsetX;\n','')
a=s.index('            root.gameObject.SetActive(true);')
b=s.index('            // Old phone templates',a)
s=s[:a]+'''            // All layout, font and colour values belong to the saved Canvas objects.
            view.canvas.gameObject.SetActive(true);
            root.anchoredPosition = authoredPosition;
'''+s[b:]
s=s.replace('public IEnumerator Play(string text, string name, bool isKhem)','public IEnumerator Play(string text)')
s=s.replace('private float FirstBaselineOffset() => sentence.textInfo.characterCount > 0 ? -sentence.textInfo.characterInfo[0].baseLine : 0f;', 'private float FirstBaselineOffset() => 0f;')
s=s.replace('return -sentence.textInfo.characterInfo[Mathf.Min(visible, sentence.textInfo.characterCount) - 1].baseLine;', 'return sentence.textInfo.characterInfo[0].baseLine - sentence.textInfo.characterInfo[Mathf.Min(visible, sentence.textInfo.characterCount) - 1].baseLine;')
a=s.index('            sentence.rectTransform.anchoredPosition = new Vector2(sentenceOffsetX, y);')
b=s.index('\n        public void Hide()',a)
s=s[:a]+'''            root.anchoredPosition = authoredPosition + Vector2.up * y;
        }
'''+s[b:]
s=s.replace('            root.gameObject.SetActive(false);','            root.anchoredPosition = authoredPosition;\n            view.canvas.gameObject.SetActive(false);')
for field in ['public Vector2 screenAnchor','[Range(.2f, .8f)] public float widthFraction','public Color sentenceColor','public Color khemColor','public Color otherSpeakerColor']:
 s=s.replace('        '+field,'        [HideInInspector] '+field)
p.write_text(s,encoding='utf-8')
