using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace SecretsThatBreathe
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ThaiTextDisplay : MonoBehaviour, ITextPreprocessor
    {
        private TMP_Text label;
        private ITextPreprocessor previous;
        public void Bind()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (label == null) return;
            if (!ReferenceEquals(label.textPreprocessor, this))
            {
                previous = label.textPreprocessor;
                label.textPreprocessor = this;
            }
            var features = new List<OTL_FeatureTag>(label.fontFeatures);
            bool changed = false;
            if (!features.Contains(OTL_FeatureTag.mark)) { features.Add(OTL_FeatureTag.mark); changed = true; }
            if (!features.Contains(OTL_FeatureTag.mkmk)) { features.Add(OTL_FeatureTag.mkmk); changed = true; }
            if (changed) label.fontFeatures = features;
            label.SetAllDirty();
        }
        public string PreprocessText(string text) => ThaiTextPreparation.Prepare(previous != null ? previous.PreprocessText(text) : text);
        private void OnEnable() => Bind();
        private void OnDisable()
        {
            if (label != null && ReferenceEquals(label.textPreprocessor, this))
            {
                label.textPreprocessor = previous;
                label.SetAllDirty();
            }
        }
    }
}
