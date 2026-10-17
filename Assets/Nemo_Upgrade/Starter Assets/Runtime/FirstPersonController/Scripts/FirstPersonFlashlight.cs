using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    public interface IPhoneFlashlightPresentation
    {
        bool CanToggle { get; }
        void OnFlashlightActivationRequested();
        void ShowOn();
        void ShowOff();
        void HidePhone();
    }

    public class FirstPersonFlashlight : MonoBehaviour
    {
        [Header("Flashlight")]
        [Tooltip("PhoneFlashlightView assigns HeldPhone/FlashlightAnchor when the phone is created. Edit that light in the phone prefab.")]
        [SerializeField] private Light flashlightSource;
        [SerializeField] private bool startOn;

        [Header("Phone presentation")]
        [SerializeField] private MonoBehaviour phonePresentation;
        [SerializeField, Min(0f)] private float phoneOnDelay = 0.3f;
        [SerializeField, Min(0f)] private float phoneOffDelay = 0.3f;
        private Coroutine phoneTransition;
        private IPhoneFlashlightPresentation Phone => phonePresentation as IPhoneFlashlightPresentation;
        public void ConfigurePhone(MonoBehaviour presentation) { phonePresentation = presentation; }
        public void ConfigureLightSource(Light source)
        {
            if (source == null) return;
            if (flashlightSource != null && flashlightSource != source) flashlightSource.enabled = false;
            flashlightSource = source;
            flashlightSource.enabled = isOn;
        }

        [Header("Input")]
        [SerializeField] private Key toggleKey = Key.F;
        [Tooltip("Time after turning the flashlight on before it can be turned off.")]
        [SerializeField, Min(0f)] private float delayAfterTurningOn = 0.6f;
        [Tooltip("Time after turning the flashlight off before it can be turned on again.")]
        [SerializeField, Min(0f)] private float delayAfterTurningOff = 0.6f;

        [Header("Sound when the flashlight actually turns on/off")]
        [Tooltip("Both on and off use this source's clip, volume, pitch and 3D settings. Edit FlashlightAudio before Play.")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField, HideInInspector] private AudioSource turnOnAudioSource;
        [SerializeField, HideInInspector] private AudioSource turnOffAudioSource;
        [SerializeField, HideInInspector] private int sharedSwitchAudioRevision;
        [SerializeField, HideInInspector] private AudioClip turnOnSound;
        [SerializeField, HideInInspector] private AudioClip turnOffSound;
        [SerializeField, HideInInspector] private float soundVolume = 1f;

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
                audioSource = CreateSwitchSource("FlashlightAudio", turnOnSound != null ? turnOnSound : turnOffSound);
        }

        private AudioSource CreateSwitchSource(string name, AudioClip clip)
        {
            var child = transform.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(transform, false);
            }
            var source = child.GetComponent<AudioSource>();
            if (source != null) return source;
            source = child.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.clip = clip;
            source.volume = (audioSource != null ? audioSource.volume : 1f) * Mathf.Clamp01(soundVolume);
            return source;
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
                    Phone.OnFlashlightActivationRequested();
                    Phone.ShowOff();
                    phoneTransition = StartCoroutine(OpenPhone());
                }
                return;
            }

            isOn = !isOn;
            float delay = isOn ? delayAfterTurningOn : delayAfterTurningOff;
            nextToggleTime = Time.unscaledTime + delay;
            flashlightSource.enabled = isOn;

            PlaySwitchSound();
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
            PlaySwitchSound();
        }

        private void PlaySwitchSound()
        {
            if (audioSource != null && audioSource.clip != null) audioSource.PlayOneShot(audioSource.clip);
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
