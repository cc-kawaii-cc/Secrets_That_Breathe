using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
    public class FirstPersonFlashlight : MonoBehaviour
    {
        [Header("Flashlight")]
        [Tooltip("The Spot Light child of PlayerCameraRoot. Edit color, intensity, range, angle, and cookie on the Light itself.")]
        [SerializeField] private Light flashlightSource;
        [SerializeField] private bool startOn;

        [Header("Input")]
        [SerializeField] private Key toggleKey = Key.F;
        [SerializeField, Min(0f)] private float toggleDelay = 0.6f;

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
            if (flashlightSource == null || Time.unscaledTime < nextToggleTime) return;

            isOn = !isOn;
            nextToggleTime = Time.unscaledTime + toggleDelay;
            flashlightSource.enabled = isOn;

            AudioClip clip = isOn ? turnOnSound : turnOffSound;
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, soundVolume);
        }

        private void OnEnable()
        {
            if (Application.isPlaying && flashlightSource != null)
                flashlightSource.enabled = isOn;
        }

        private void OnDisable()
        {
            if (Application.isPlaying && flashlightSource != null)
                flashlightSource.enabled = false;
        }
    }
}
