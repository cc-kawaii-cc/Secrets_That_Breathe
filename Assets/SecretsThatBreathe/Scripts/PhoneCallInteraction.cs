using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    public sealed class PhoneCallInteraction : MonoBehaviour
    {
        public OfficeIntroDialogue intro;
        public PlayerSeatController player;
        public Camera playerCamera;
        public BoxCollider interactionZone;
        public PhoneCallDialogueUI dialogueUI;
        public Light indicatorLight;
        [Header("Sound clips (assign your sounds here)")]
        public AudioClip ringSound;
        public AudioClip answerSound;
        public AudioClip hangUpSound;
        [Range(0f, 1f)] public float volume = 1f;
        [Min(.1f)] public float ringAudibleDistance = 15f;
        [Header("Ringing light pulse")]
        [Min(0f)] public float minimumIntensity = .1f;
        [Min(0f)] public float maximumIntensity = 2f;
        [Min(.1f)] public float pulseDuration = 1.5f;
        [Header("Interaction and automatic dialogue")]
        [FormerlySerializedAs("afterFlashlightDelay")]
        [Tooltip("Wait after the [F] hint disappears before ringing. F1 testing skip bypasses this wait.")]
        [Min(0f)] public float afterFlashlightHintDelay = 5f;
        [Range(1f, 80f)] public float lookAngle = 40f;
        [Min(.1f)] public float thoughtDuration = 3f;
        [Min(.001f)] public float secondsPerCharacter = .05f;
        [Min(0f)] public float lineReadDuration = 2f;
        [Min(0f)] public float hangUpDelay = .5f;
        public bool IsInCall { get; private set; }
        public bool HasAnswered { get; private set; }
        private static PhoneCallInteraction activeCall;
        public static bool IsBusy(UnityEngine.SceneManagement.Scene scene) => activeCall != null && activeCall.gameObject.scene == scene && activeCall.IsInCall;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => activeCall = null;
        private AudioSource ringSource, effectSource;
        private Renderer[] meshes;
        private FirstPersonController movement;
        private bool wasMovementLocked;
        private bool ringing;
        private float ringStarted, thoughtRemaining;

        private IEnumerator Start()
        {
            meshes = GetComponentsInChildren<Renderer>(true);
            ringSource = CreateSource("PhoneRingAudio", 1f);
            ringSource.loop = true;
            ringSource.rolloffMode = AudioRolloffMode.Linear;
            ringSource.minDistance = 1f;
            ringSource.maxDistance = ringAudibleDistance;
            effectSource = CreateSource("PhoneButtonAudio", 0f);
            dialogueUI.Hide();
            if (indicatorLight != null) indicatorLight.enabled = false;
            while (intro == null || !intro.IsFinished) yield return null;
            if (!intro.SkippedForTesting)
                yield return new WaitForSeconds(afterFlashlightHintDelay);
            ringing = true;
            ringStarted = Time.time;
            ringSource.clip = ringSound;
            ringSource.volume = volume;
            if (ringSound != null) ringSource.Play();
            if (indicatorLight != null)
            {
                indicatorLight.gameObject.SetActive(true);
                indicatorLight.enabled = true;
            }
            dialogueUI.ringThought.SetActive(true);
            thoughtRemaining = thoughtDuration;
        }
        private AudioSource CreateSource(string name, float spatialBlend)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            return source;
        }
        private void Update()
        {
            if (!ringing) return;
            if (indicatorLight != null)
            {
                float phase = (Time.time - ringStarted) / Mathf.Max(.1f, pulseDuration) * Mathf.PI * 2f;
                indicatorLight.intensity = Mathf.Lerp(minimumIntensity, maximumIntensity, .5f - .5f * Mathf.Cos(phase));
            }
            if (thoughtRemaining > 0f)
            {
                thoughtRemaining -= Time.deltaTime;
                if (thoughtRemaining <= 0f) dialogueUI.ringThought.SetActive(false);
            }
            if (playerCamera == null) playerCamera = Camera.main;
            bool available = Time.timeScale > 0f && player != null && player.CanExplore && InZone()
                && InteractionLook.IsLookingAt(playerCamera, meshes, lookAngle, float.MaxValue);
            dialogueUI.answerPrompt.SetActive(available);
            if (available && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) Answer();
        }
        private bool InZone()
        {
            if (interactionZone == null || !interactionZone.enabled || !interactionZone.gameObject.activeInHierarchy) return false;
            var controller = player.GetComponent<CharacterController>();
            Vector3 position = controller != null ? controller.transform.TransformPoint(controller.center) : player.transform.position;
            Vector3 delta = interactionZone.transform.InverseTransformPoint(position) - interactionZone.center;
            Vector3 half = interactionZone.size * .5f;
            return Mathf.Abs(delta.x) <= half.x && Mathf.Abs(delta.y) <= half.y && Mathf.Abs(delta.z) <= half.z;
        }
        private void Answer()
        {
            if (!ringing || HasAnswered) return;
            ringing = false;
            HasAnswered = true;
            IsInCall = true;
            activeCall = this;
            ringSource.Stop();
            if (indicatorLight != null) indicatorLight.enabled = false;
            if (answerSound != null) effectSource.PlayOneShot(answerSound, volume);
            movement = player.GetComponent<FirstPersonController>();
            if (movement != null) { wasMovementLocked = movement.MovementLocked; movement.MovementLocked = true; }
            StartCoroutine(Conversation());
        }
        private IEnumerator Conversation()
        {
            dialogueUI.Begin();
            // Kem always occupies the top row; Ton always occupies the bottom row.
            int[] speakers = { 0, 1, 0, 1, 1, 1, 0, 1, 1, 0 };
            for (int i = 0; i < dialogueUI.sentences.Length; i++)
            {
                var text = speakers[i] == 0 ? dialogueUI.kemText : dialogueUI.tonText;
                text.text = dialogueUI.sentences[i].text;
                text.maxVisibleCharacters = 0;
                text.ForceMeshUpdate();
                int count = text.textInfo.characterCount;
                for (int character = 1; character <= count; character++)
                {
                    yield return new WaitForSeconds(secondsPerCharacter);
                    text.maxVisibleCharacters = character;
                }
                if (i + 1 < dialogueUI.sentences.Length) yield return new WaitForSeconds(lineReadDuration);
            }
            yield return new WaitForSeconds(hangUpDelay);
            if (hangUpSound != null) effectSource.PlayOneShot(hangUpSound, volume);
            dialogueUI.Hide();
            ReleaseMovement();
        }
        private void ReleaseMovement()
        {
            if (movement != null) movement.MovementLocked = wasMovementLocked;
            movement = null;
            IsInCall = false;
            if (activeCall == this) activeCall = null;
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            if (ringSource != null) ringSource.Stop();
            if (indicatorLight != null) indicatorLight.enabled = false;
            if (dialogueUI != null) dialogueUI.Hide();
            ReleaseMovement();
        }
    }
}
