using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    public interface IPhoneFlashlightPresentation
    {
        bool CanToggle { get; }
        void ShowOn();
        void ShowOff();
        void HidePhone();
    }

    public class FirstPersonFlashlight : MonoBehaviour
    {
        [Header("Flashlight")]
        [Tooltip("The Spot Light child of PlayerCameraRoot. Edit color, intensity, range, angle, and cookie on the Light itself.")]
        [SerializeField] private Light flashlightSource;
        [SerializeField] private bool startOn;

        [Header("Phone presentation")]
        [SerializeField] private MonoBehaviour phonePresentation;
        [SerializeField, Min(0f)] private float phoneOnDelay = 0.3f;
        [SerializeField, Min(0f)] private float phoneOffDelay = 0.3f;
        private Coroutine phoneTransition;
        private IPhoneFlashlightPresentation Phone => phonePresentation as IPhoneFlashlightPresentation;
        public void ConfigurePhone(MonoBehaviour presentation) { phonePresentation = presentation; }

        [Header("Input")]
        [SerializeField] private Key toggleKey = Key.F;
        [Tooltip("Time after turning the flashlight on before it can be turned off.")]
        [SerializeField, Min(0f)] private float delayAfterTurningOn = 0.6f;
        [Tooltip("Time after turning the flashlight off before it can be turned on again.")]
        [SerializeField, Min(0f)] private float delayAfterTurningOff = 0.6f;

        [Header("Sound")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip turnOnSound;
        [SerializeField] private AudioClip turnOffSound;
        [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

        private float nextToggleTime;
        private bool isOn;

        public bool IsOn => isOn;

        private void Start()
        {
            if (flashlightSource == null)
            {
                Debug.LogWarning("Assign the Flashlight Spot Light to FirstPersonFlashlight.", this);
                enabled = false;
                return;
            }

            isOn = startOn;
            flashlightSource.enabled = isOn;
            if (Phone != null) ForceOff();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && toggleKey != Key.None && keyboard[toggleKey].wasPressedThisFrame)
                Toggle();
        }

        public void Toggle()
        {
            if (Phone != null && !Phone.CanToggle) return;
            if (!isActiveAndEnabled || Time.timeScale <= 0f || flashlightSource == null || phoneTransition != null || Time.unscaledTime < nextToggleTime) return;

            if (Phone != null)
            {
                if (isOn)
                {
                    SetLightAndSound(false);
                    Phone.ShowOff();
                    phoneTransition = StartCoroutine(StorePhone());
                }
                else
                {
                    Phone.ShowOff();
                    phoneTransition = StartCoroutine(OpenPhone());
                }
                return;
            }

            isOn = !isOn;
            float delay = isOn ? delayAfterTurningOn : delayAfterTurningOff;
            nextToggleTime = Time.unscaledTime + delay;
            flashlightSource.enabled = isOn;

            AudioClip clip = isOn ? turnOnSound : turnOffSound;
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, soundVolume);
        }

        private IEnumerator OpenPhone()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, phoneOnDelay));
            Phone?.ShowOn();
            SetLightAndSound(true);
            phoneTransition = null;
        }

        private IEnumerator StorePhone()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, phoneOffDelay));
            Phone?.HidePhone();
            phoneTransition = null;
        }

        private void SetLightAndSound(bool value)
        {
            isOn = value;
            flashlightSource.enabled = value;
            AudioClip clip = value ? turnOnSound : turnOffSound;
            if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, soundVolume);
        }

        // Clear the remembered on-state as well as the Light, so re-enabling input cannot switch it back on.
        public void ForceOff()
        {
            if (phoneTransition != null) { StopCoroutine(phoneTransition); phoneTransition = null; }
            isOn = false;
            if (flashlightSource != null) flashlightSource.enabled = false;
            Phone?.HidePhone();
            nextToggleTime = Time.unscaledTime;
        }

        private void OnEnable()
        {
            if (Application.isPlaying && flashlightSource != null)
                flashlightSource.enabled = isOn;
        }

        private void OnDisable()
        {
            if (Application.isPlaying) ForceOff();
        }
    }
}
