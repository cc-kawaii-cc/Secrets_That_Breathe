using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class EveningDriveIntro : MonoBehaviour
    {
        [Header("Saved Canvas — edit the sentence and style on its TMP text")]
        public CanvasGroup screen;
        public TMP_Text storyText;
        public CanvasGroup storyGroup;
        [Tooltip("Plays only while the Intro sentence is being typed. Adjust Volume on this AudioSource.")]
        public AudioSource typingAudio;

        [Header("Real seconds")]
        [Min(0f)] public float blackDelaySeconds = 2f;
        [Tooltip("Real seconds between characters. Lower values type faster.")]
        [Min(.001f)] public float secondsPerCharacter = .05f;
        [Min(0f)] public float completedTextHoldSeconds = 2f;
        [Min(0f)] public float textFadeOutSeconds = 1f;
        [Min(0f)] public float revealGameSeconds = 2f;
        [Tooltip("Start the car once this fraction of the story text's opacity has faded away.")]
        [Range(0f, 1f)] public float movementStartTextFadeProgress = .7f;
        [Min(0f)] public float maximumStreamingWaitSeconds = 10f;

        public bool IsFinished { get; private set; }
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private float previousTimeScale;
        private bool ownsPause;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private static EveningDriveIntro owner;

        public static bool IsInputBlocked(Scene scene)
            => owner != null && owner.ownsPause && owner.gameObject.scene == scene;

        public static float GetGameRevealAmount(Scene scene)
            => IsInputBlocked(scene) && owner.screen != null
                ? 1f - Mathf.Clamp01(owner.screen.alpha) : 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOwner() => owner = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= ActivateSavedCanvas;
            SceneManager.sceneLoaded += ActivateSavedCanvas;
        }

        private static void ActivateSavedCanvas(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "MainScene_2_Evening") return;
            foreach (var root in scene.GetRootGameObjects())
            {
                var intro = root.GetComponentInChildren<EveningDriveIntro>(true);
                if (intro == null) continue;
                intro.gameObject.SetActive(true);
                return;
            }
            Debug.LogError("Evening scene is missing its EveningDriveIntroCanvas.");
        }

        private void Awake()
        {
            if (owner != null && owner != this) { gameObject.SetActive(false); return; }
            if (screen == null || storyText == null || storyGroup == null)
            {
                Debug.LogError("Assign the Evening intro screen and story text/group.", this);
                gameObject.SetActive(false);
                return;
            }
            owner = this;
            previousTimeScale = Time.timeScale;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            ownsPause = true;
            Time.timeScale = 0f;
            screen.alpha = 1f;
            screen.blocksRaycasts = true;
            storyGroup.alpha = 0f;
            storyText.maxVisibleCharacters = 0;
            if (typingAudio != null) typingAudio.Stop();

            // The ride initializes its camera behind black and owns its walking controls.
            // Do not restore those controls over the ride when the intro ends.
            var rideOwnedControls = new HashSet<Behaviour>();
            foreach (var ride in FindObjectsByType<PorschePassengerRide>(FindObjectsSortMode.None))
                if (ride.gameObject.scene == gameObject.scene && ride.isActiveAndEnabled && ride.suspendedControls != null)
                    foreach (var control in ride.suspendedControls)
                        if (control != null) rideOwnedControls.Add(control);
            foreach (var control in FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (control.gameObject.scene != gameObject.scene || !control.isActiveAndEnabled || rideOwnedControls.Contains(control)) continue;
                switch (control.GetType().Name)
                {
                    case "PlayerInput":
                    case "FirstPersonFlashlight":
                    case "FirstPersonController":
                    case "PlayerMovement":
                    case "MouseLook":
                    case "PlayerInteractor":
                        suspended.Add(control);
                        control.enabled = false;
                        break;
                }
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private IEnumerator Start()
        {
            // Allow Start, rendering and resource uploads behind the opaque screen.
            yield return null;
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(blackDelaySeconds);
            storyGroup.alpha = 1f;
            storyText.ForceMeshUpdate();
            int characters = storyText.textInfo.characterCount;
            float elapsed = 0f;
            if (characters > 0 && typingAudio != null && typingAudio.clip != null) typingAudio.Play();
            while (storyText.maxVisibleCharacters < characters)
            {
                elapsed += Time.unscaledDeltaTime;
                storyText.maxVisibleCharacters = Mathf.Min(characters,
                    Mathf.FloorToInt(elapsed / Mathf.Max(.001f, secondsPerCharacter)));
                if (storyText.maxVisibleCharacters >= characters && typingAudio != null) typingAudio.Stop();
                yield return null;
            }
            if (typingAudio != null) typingAudio.Stop();
            yield return new WaitForSecondsRealtime(completedTextHoldSeconds);

            // Let texture streaming settle, with a bounded wait so a stalled upload
            // cannot leave the player permanently on a black screen.
            int readyFrames = 0;
            elapsed = 0f;
            while (readyFrames < 3 && elapsed < maximumStreamingWaitSeconds)
            {
                readyFrames = Texture.streamingTexturePendingLoadCount == 0
                    && Texture.streamingTextureLoadingCount == 0 ? readyFrames + 1 : 0;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return FadeStoryAndStartMovement();
            yield return Fade(screen, 0f, revealGameSeconds);
            IsFinished = true;
            ReleaseControls();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            gameObject.SetActive(false);
        }

        private IEnumerator FadeStoryAndStartMovement()
        {
            float elapsed = 0f;
            bool movementResumed = false;
            while (elapsed < textFadeOutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / textFadeOutSeconds);
                storyGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
                if (!movementResumed && storyGroup.alpha <= 1f - Mathf.Clamp01(movementStartTextFadeProgress))
                {
                    // Movement starts under black when the text has faded 70%.
                    // Input stays blocked through the following full-screen reveal.
                    Time.timeScale = previousTimeScale;
                    movementResumed = true;
                }
                yield return null;
            }
            storyGroup.alpha = 0f;
            if (!movementResumed) Time.timeScale = previousTimeScale;
        }

        private static IEnumerator Fade(CanvasGroup group, float target, float seconds)
        {
            float initial = group.alpha;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(initial, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds)));
                yield return null;
            }
            group.alpha = target;
        }

        private void ReleaseControls()
        {
            if (!ownsPause) return;
            ownsPause = false;
            foreach (var input in FindObjectsByType<StarterAssets.StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (input.gameObject.scene == gameObject.scene)
                {
                    input.move = input.look = Vector2.zero;
                    input.jump = input.sprint = false;
                }
            foreach (var control in suspended) if (control != null) control.enabled = true;
            suspended.Clear();
            Time.timeScale = previousTimeScale;
            screen.blocksRaycasts = false;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (typingAudio != null) typingAudio.Stop();
            ReleaseControls();
            if (screen != null) screen.alpha = 0f;
            if (owner == this) owner = null;
        }
    }
}
