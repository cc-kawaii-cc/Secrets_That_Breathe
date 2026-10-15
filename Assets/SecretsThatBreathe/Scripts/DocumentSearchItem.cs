using UnityEngine;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class DocumentSearchItem : MonoBehaviour
    {
        public BoxCollider interactionZone;
        public BoxCollider lookTarget;
        public Light indicatorLight;
        [Min(0f)] public float minimumIntensity = .01f;
        [Min(0f)] public float maximumIntensity = .15f;
        [Min(.1f)] public float pulseDuration = 2.5f;
        public bool HasChecked { get; private set; }

        private void Awake() => SetLight(false);
        public void ResetForTesting() { HasChecked = false; SetLight(false); }
        public void Check() { HasChecked = true; SetLight(false); }
        public void Pulse(bool searching)
        {
            bool show = searching && !HasChecked;
            SetLight(show);
            if (!show || indicatorLight == null) return;
            float phase = Time.time * Mathf.PI * 2f / Mathf.Max(.1f, pulseDuration);
            indicatorLight.intensity = Mathf.Lerp(minimumIntensity, maximumIntensity, .5f - .5f * Mathf.Cos(phase));
        }
        private void SetLight(bool visible)
        {
            if (indicatorLight == null) return;
            if (visible) indicatorLight.gameObject.SetActive(true);
            indicatorLight.enabled = visible;
        }
        private void OnDisable() => SetLight(false);
    }
}
