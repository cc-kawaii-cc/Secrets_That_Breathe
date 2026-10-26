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
        public BoxCollider lookTarget;
        public PhoneCallDialogueUI dialogueUI;
        public Light indicatorLight;
        public PhoneHandsetPickup handsetPickup;
        public DocumentSearchSequence documentSearch;
        [Header("Scene audio sources (edit volume and 3D settings on these objects)")]
        public AudioSource ringSource;
        public AudioSource effectSource;
        [Header("Button sound clips")]
        [HideInInspector] public AudioClip ringSound;
        public AudioClip answerSound;
        public AudioClip hangUpSound;
        public AudioClip dialSound;
        [Min(0f)] public float dialDelay = .5f;
        [HideInInspector] public float volume = 1f;
        [HideInInspector] public float ringAudibleDistance = 15f;
        [Header("Ringing light pulse")]
        [Min(0f)] public float minimumIntensity = .1f;
        [Min(0f)] public float maximumIntensity = 2f;
        [Min(.1f)] public float pulseDuration = 1.5f;
        [Header("Interaction and automatic dialogue")]
        [FormerlySerializedAs("afterFlashlightDelay")]
        [Tooltip("Wait after the [F] hint disappears before ringing. F1 testing skip bypasses this wait.")]
        [Min(0f)] public float afterFlashlightHintDelay = 5f;
        [Tooltip("Wait after the incoming ring and light start before showing the 'Who is calling?' thought.")]
        [Min(0f)] public float ringThoughtDelay = 1.5f;
        [Min(.1f)] public float thoughtDuration = 3f;
        [HideInInspector, Min(.001f)] public float secondsPerCharacter = .05f;
        [HideInInspector, Min(0f)] public float lineReadDuration = 1f;
        [Min(0f)] public float hangUpDelay = .5f;
        public bool IsInCall { get; private set; }
        public bool HasAnswered { get; private set; }
        private static PhoneCallInteraction activeCall;
        public static bool IsBusy(UnityEngine.SceneManagement.Scene scene) => activeCall != null && activeCall.gameObject.scene == scene && activeCall.IsInCall;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => activeCall = null;
        private FirstPersonController movement;
        private bool wasMovementLocked;
        private bool ringing;
        private bool callingBack;
        private bool skipIncoming;
        private float ringStarted, thoughtRemaining;

        private IEnumerator Start()
        {
            ringSource = ResolveSource(ringSource, "PhoneRingAudio", 1f);
            ringSource.loop = true;
            effectSource = ResolveSource(effectSource, "PhoneButtonAudio", 0f);
            dialogueUI.Hide();
            if (indicatorLight != null) indicatorLight.enabled = false;
            while (!skipIncoming && (intro == null || !intro.IsFinished)) yield return null;
            if (skipIncoming) yield break;
            if (!intro.SkippedForTesting)
                yield return new WaitForSeconds(afterFlashlightHintDelay);
            if (skipIncoming) yield break;
            yield return BeginRinging();
        }
        private IEnumerator BeginRinging()
        {
            ringing = true;
            ringStarted = Time.time;
            if (ringSource.clip != null) ringSource.Play();
            if (indicatorLight != null)
            {
                indicatorLight.gameObject.SetActive(true);
                indicatorLight.enabled = true;
            }
            if (ringThoughtDelay > 0f) yield return new WaitForSeconds(ringThoughtDelay);
            if (!ringing || HasAnswered || skipIncoming) yield break;
            dialogueUI.ringThought.SetActive(true);
            thoughtRemaining = thoughtDuration;
        }
        private AudioSource ResolveSource(AudioSource assigned, string name, float spatialBlend)
        {
            if (assigned != null) return assigned;
            var existing = transform.Find(name);
            if (existing != null && existing.TryGetComponent<AudioSource>(out var existingSource)) return existingSource;
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.volume = volume;
            if (name == "PhoneRingAudio")
            {
                source.clip = ringSound;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1f;
                source.maxDistance = ringAudibleDistance;
            }
            return source;
        }
        private void Update()
        {
            bool readyToCallBack = documentSearch != null && documentSearch.CanCallBack;
            if (!ringing && !readyToCallBack) return;
            if (indicatorLight != null)
            {
                indicatorLight.gameObject.SetActive(true);
                indicatorLight.enabled = true;
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
                && InteractionLook.IsLookingAtBox(playerCamera, lookTarget);
            dialogueUI.answerPrompt.SetActive(available && !readyToCallBack);
            if (documentSearch != null && documentSearch.callBackPrompt != null)
                documentSearch.callBackPrompt.transform.parent.gameObject.SetActive(available && readyToCallBack);
            if (available && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) Answer(readyToCallBack);
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
        private void Answer(bool callBack)
        {
            if (IsInCall || (callBack ? documentSearch == null || !documentSearch.CanCallBack : !ringing || HasAnswered)) return;
            callingBack = callBack;
            ringing = false;
            HasAnswered = true;
            IsInCall = true;
            activeCall = this;
            if (documentSearch != null)
            {
                if (callBack) documentSearch.BeginCallBack(); else documentSearch.BeginFirstCall();
                if (documentSearch.callBackPrompt != null) documentSearch.callBackPrompt.transform.parent.gameObject.SetActive(false);
            }
            var flashlight = player.GetComponent<FirstPersonFlashlight>();
            if (flashlight != null) flashlight.ForceOff();
            ringSource.Stop();
            dialogueUI.Hide();
            if (indicatorLight != null) indicatorLight.enabled = false;
            if (answerSound != null) effectSource.PlayOneShot(answerSound);
            movement = player.GetComponent<FirstPersonController>();
            if (movement != null) { wasMovementLocked = movement.MovementLocked; movement.MovementLocked = true; }
            StartCoroutine(Conversation());
        }
        private IEnumerator Conversation()
        {
            if (handsetPickup == null) handsetPickup = GetComponent<PhoneHandsetPickup>();
            if (handsetPickup != null)
            {
                handsetPickup.playerCamera = playerCamera;
                yield return handsetPickup.PickUp();
            }
            if (callingBack)
            {
                if (dialSound != null) effectSource.PlayOneShot(dialSound);
                yield return new WaitForSeconds((dialSound != null ? dialSound.length : 0f) + dialDelay);
            }
            dialogueUI.Begin();
            // One active speaker, with a one-second hold after the last character.
            int[] speakers = callingBack ? new[] { 0, 1, 0, 1, 0 } : new[] { 0, 1, 0, 1, 0, 1, 1, 1, 0, 1, 1, 0 };
            var sentences = callingBack ? dialogueUI.callBackSentences : dialogueUI.sentences;
            for (int i = 0; sentences != null && i < sentences.Length && i < speakers.Length; i++)
            {
                yield return dialogueUI.PlayLine(sentences[i], speakers[i] == 0);
            }
            yield return new WaitForSeconds(hangUpDelay);
            dialogueUI.Hide();
            if (handsetPickup != null) yield return handsetPickup.PutDown();
            if (hangUpSound != null) effectSource.PlayOneShot(hangUpSound);
            ReleaseMovement();
            if (documentSearch != null)
            {
                if (callingBack) documentSearch.FinishCallBack(); else documentSearch.BeginSearch();
            }
        }
        public void SkipToDocumentSearch()
        {
            skipIncoming = true;
            StopAllCoroutines();
            if (intro != null) intro.SkipToPhoneCall();
            ringing = false;
            HasAnswered = true;
            callingBack = false;
            thoughtRemaining = 0f;
            if (handsetPickup != null) handsetPickup.Restore();
            if (ringSource != null) ringSource.Stop();
            if (effectSource != null) effectSource.Stop();
            if (indicatorLight != null) indicatorLight.enabled = false;
            if (dialogueUI != null) dialogueUI.Hide();
            ReleaseMovement();
        }
        public void RestartIncomingCallForTesting()
        {
            SkipToDocumentSearch();
            ringSource = ResolveSource(ringSource, "PhoneRingAudio", 1f);
            ringSource.loop = true;
            effectSource = ResolveSource(effectSource, "PhoneButtonAudio", 0f);
            skipIncoming = false;
            HasAnswered = false;
            StartCoroutine(BeginRinging());
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
            if (handsetPickup != null) handsetPickup.Restore();
            if (ringSource != null) ringSource.Stop();
            if (indicatorLight != null) indicatorLight.enabled = false;
            if (dialogueUI != null) dialogueUI.Hide();
            ReleaseMovement();
        }
    }
}
