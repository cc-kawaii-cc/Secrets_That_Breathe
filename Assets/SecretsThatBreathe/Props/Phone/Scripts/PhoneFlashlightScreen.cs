using UnityEngine;

namespace SecretsThatBreathe.Phone
{
    /// <summary>Switches this phone's screen and light without changing shared material assets.</summary>
    public sealed class PhoneFlashlightScreen : MonoBehaviour
    {
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private Light flashlight;
        [SerializeField] private Material offMaterial;
        [SerializeField] private Material onMaterial;
        [SerializeField] private bool flashlightOn;

        public bool IsOn => flashlightOn;

        public void Configure(Renderer screen, Light source, Material off, Material on)
        {
            screenRenderer = screen;
            flashlight = source;
            offMaterial = off;
            onMaterial = on;
            SetFlashlightOn(false);
        }

        private void OnEnable() => SetFlashlightOn(flashlightOn);
        private void OnValidate() => SetFlashlightOn(flashlightOn);

        // Existing gameplay may toggle the assigned Light directly; follow that state too.
        private void LateUpdate()
        {
            if (flashlight != null && flashlight.enabled != flashlightOn)
                SetFlashlightOn(flashlight.enabled);
        }

        public void SetFlashlightOn(bool value)
        {
            flashlightOn = value;
            if (flashlight != null) flashlight.enabled = value;
            Material material = value ? onMaterial : offMaterial;
            if (screenRenderer != null && material != null) screenRenderer.sharedMaterial = material;
        }

        [ContextMenu("Flashlight/Toggle")]
        public void ToggleFlashlight() => SetFlashlightOn(!flashlightOn);

        [ContextMenu("Flashlight/Turn On")]
        public void TurnOn() => SetFlashlightOn(true);

        [ContextMenu("Flashlight/Turn Off")]
        public void TurnOff() => SetFlashlightOn(false);
    }
}
