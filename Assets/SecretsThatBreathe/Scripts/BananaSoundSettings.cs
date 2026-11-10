using System;
using UnityEngine;

namespace SecretsThatBreathe
{
    // Sounds for eating a banana (BananaViewModel). The asset sits in a Resources folder
    // (Assets/SecretsThatBreathe/Audio/Resources/BananaSoundSettings.asset), so every scene with banana trees uses it.
    // With no asset, or no clip in it, the generated munch sound is played instead.
    [CreateAssetMenu(menuName = "Secrets That Breathe/Banana Sound Settings", fileName = ResourceName)]
    public sealed class BananaSoundSettings : ScriptableObject
    {
        public const string ResourceName = "BananaSoundSettings";

        [Tooltip("Played on every bite. Empty = the generated munch sound.")]
        public AudioClip biteSound;
        [Range(0f, 1f)] public float biteVolume = .9f;
        [Tooltip("Only this part of the clip is played (seconds into it).")]
        [Min(0f)] public float biteStart = 1.2f;
        [Tooltip("Where that part ends (seconds into the clip). 0 = to the end of the clip.")]
        [Min(0f)] public float biteEnd = 1.9f;

        private static BananaSoundSettings current;
        [NonSerialized] private AudioClip trimmed;
        [NonSerialized] private AudioClip trimmedFrom;

        public static BananaSoundSettings Current
        {
            get
            {
                if (current == null) current = Resources.Load<BananaSoundSettings>(ResourceName);
                return current;
            }
        }

        // The bite clip cut to biteStart..biteEnd (made once, with very short fades so it does not click).
        public AudioClip BiteClip()
        {
            if (biteSound == null) return null;
            if (trimmed != null && trimmedFrom == biteSound) return trimmed;
            trimmedFrom = biteSound;
            trimmed = Trim(biteSound, biteStart, biteEnd);
            if (trimmed == null)
            {
                Debug.LogWarning("[Banana] Could not cut " + biteSound.name + " (set its Load Type to Decompress On Load); the whole clip is played.", this);
                trimmed = biteSound;
            }
            return trimmed;
        }

        private static AudioClip Trim(AudioClip source, float start, float end)
        {
            if (start <= 0f && (end <= 0f || end >= source.length)) return source;
            if (source.loadState != AudioDataLoadState.Loaded) source.LoadAudioData();
            if (source.loadState != AudioDataLoadState.Loaded) return null;
            int rate = source.frequency, channels = Mathf.Max(1, source.channels), total = source.samples;
            int from = Mathf.Clamp(Mathf.RoundToInt(start * rate), 0, Mathf.Max(0, total - 1));
            int to = end > start ? Mathf.Clamp(Mathf.RoundToInt(end * rate), from + 1, total) : total;
            int count = to - from;
            if (count <= 0) return null;
            var data = new float[count * channels];
            if (!source.GetData(data, from)) return null;
            int fade = Mathf.Min(count / 4, Mathf.RoundToInt(rate * .008f));
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                for (int c = 0; c < channels; c++)
                {
                    data[i * channels + c] *= k;
                    data[(count - 1 - i) * channels + c] *= k;
                }
            }
            var clip = AudioClip.Create(source.name + " (bite)", count, channels, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnValidate() { trimmed = null; trimmedFrom = null; }
    }
}
