using UnityEngine;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class VillageExteriorAmbience : MonoBehaviour
    {
        public AudioClip villageClip;
        public AudioSource sourceA;
        public AudioSource sourceB;
        [Range(0f, 1f)] public float exteriorVolume = .28f;
        [Min(.1f)] public float fadeInSeconds = 3.5f;
        [Min(.1f)] public float loopCrossfadeSeconds = 2.5f;
        private bool requested, playing;
        private AudioSource current, next;
        private double began, currentStart, nextStart;
        private float overlap;

        public void BeginExterior()
        {
            if (requested || villageClip == null || sourceA == null || sourceB == null) return;
            requested = true;
            villageClip.LoadAudioData();
        }

        private void Update()
        {
            if (!requested) return;
            if (!playing)
            {
                if (villageClip.loadState != AudioDataLoadState.Loaded || villageClip.length <= 0f) return;
                Configure(sourceA); Configure(sourceB);
                current = sourceA; next = sourceB;
                overlap = Mathf.Clamp(loopCrossfadeSeconds, .01f, villageClip.length * .25f);
                began = currentStart = AudioSettings.dspTime + .05;
                nextStart = currentStart + villageClip.length - overlap;
                current.PlayScheduled(currentStart);
                next.PlayScheduled(nextStart);
                playing = true;
            }
            double now = AudioSettings.dspTime;
            if (now >= currentStart + villageClip.length)
            {
                current.Stop();
                var spare = current;
                current = next; next = spare;
                currentStart = nextStart;
                nextStart = currentStart + villageClip.length - overlap;
                next.volume = 0f;
                next.PlayScheduled(nextStart);
            }
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((float)(now - began) / Mathf.Max(.1f, fadeInSeconds)));
            float blend = Mathf.Clamp01((float)(now - nextStart) / overlap);
            // Overlap the recording ends instead of jumping to sample zero.
            // Keep the original stereo image and pitch; the environment stays steady.
            current.volume = exteriorVolume * fade * Mathf.Cos(blend * Mathf.PI * .5f);
            next.volume = exteriorVolume * fade * Mathf.Sin(blend * Mathf.PI * .5f);
        }

        private void Configure(AudioSource source)
        {
            source.Stop();
            source.clip = villageClip;
            source.playOnAwake = false;
            source.loop = false;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = 0f;
        }

        private void OnDisable()
        {
            if (sourceA != null) sourceA.Stop();
            if (sourceB != null) sourceB.Stop();
            requested = playing = false;
        }
    }
}
