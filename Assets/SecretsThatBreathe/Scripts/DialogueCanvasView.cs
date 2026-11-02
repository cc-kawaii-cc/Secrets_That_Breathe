using System;
using TMPro;
using UnityEngine;

namespace SecretsThatBreathe
{
    [Serializable]
    public sealed class DialogueCanvasView
    {
        public Canvas canvas;
        public RectTransform motionRoot;
        [Tooltip("Optional. Leave empty for narration without a speaker name.")]
        public TMP_Text speaker;
        public TMP_Text sentence;
        public bool normalizeFontBaseline = true;
        [HideInInspector] public int layoutRevision;
        public bool IsValid => canvas != null && motionRoot != null && sentence != null;
        public void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
    }
}
