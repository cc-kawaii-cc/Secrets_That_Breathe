using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    // Pause before other scene components can begin their opening gameplay.
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class LawyerRoomIntro : MonoBehaviour
    {
        [Header("Saved Canvas objects — edit text and colours on their TMP components")]
        public CanvasGroup screen;
        public CanvasGroup[] paragraphs;
        public CanvasGroup pressAnyKey;

        [Header("Intro 2 — text and style belong to the saved Canvas")]
        public TMP_Text secondIntroText;
        public CanvasGroup secondIntroGroup;
        [Tooltip("Plays only while Intro 2 characters are being revealed. Adjust Volume on this AudioSource.")]
        public AudioSource secondIntroTypingAudio;
        [Tooltip("Real seconds between characters. Lower values type faster.")]
        [Min(.001f)] public float secondIntroSecondsPerCharacter = .05f;
        [Min(0f)] public float secondIntroHoldSeconds = 1f;
        [Min(0f)] public float secondIntroFadeOutSeconds = 1f;

        [Header("Timing (real seconds)")]
        [Tooltip("Black-screen wait before the first Intro 1 paragraph starts fading in.")]
        [Min(0f)] public float firstTextDelaySeconds = 1.5f;
        [Min(0f)] public float textFadeSeconds = 1f;
        [Min(0f)] public float paragraphHoldSeconds = 3f;
        [Min(0f)] public float promptFadeSeconds = 1f;
        [Min(0f)] public float fadeToBlackSeconds = 1f;
        [Min(0f)] public float blackHoldSeconds = 0.25f;
        [Min(0f)] public float revealGameSeconds = 2f;

        [Header("Gameplay ambience — starts with the game reveal")]
        [Tooltip("These sources have Play On Awake disabled. Their saved Volume is the 100% level; Intro fades them in linearly with the picture.")]
        public AudioSource[] gameplayAudioSources;

        [Header("Player controls suspended until the reveal finishes")]
        public Behaviour[] playerControls;

        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private readonly Dictionary<AudioSource, float> gameplayAudioVolumes = new Dictionary<AudioSource, float>();
        private float previousTimeScale;
        private bool ownsPause;
        private static LawyerRoomIntro owner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOwner() => owner = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterRuntimeCanvas()
        {
            SceneManager.sceneLoaded -= CreateRuntimeCanvas;
            SceneManager.sceneLoaded += CreateRuntimeCanvas;
        }

        private static void CreateRuntimeCanvas(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Lawyer Room" || owner != null) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                var intro = root.GetComponentInChildren<LawyerRoomIntro>(true);
                if (intro == null) continue;
                intro.gameObject.SetActive(true);
                return;
            }
            Debug.LogError("Lawyer Room Intro Canvas is missing from SeatingCanvas.");
        }

        private void Awake()
        {
            // Only one runtime Intro may own the gameplay pause.
            if (owner != null && owner != this)
            {
                gameObject.SetActive(false);
                return;
            }
            if (screen == null || pressAnyKey == null || secondIntroText == null || secondIntroGroup == null
                || paragraphs == null || paragraphs.Length != 3
                || System.Array.Exists(paragraphs, p => p == null))
            {
                Debug.LogError("Assign the Intro screen, three paragraphs, key prompt and Intro 2 text/group.", this);
                enabled = false;
                if (screen != null) screen.gameObject.SetActive(false);
                return;
            }

            previousTimeScale = Time.timeScale;
            owner = this;
            ownsPause = true;
            Time.timeScale = 0f;
            screen.alpha = 1f;
            screen.blocksRaycasts = true;
            foreach (var paragraph in paragraphs) paragraph.alpha = 0f;
            pressAnyKey.alpha = 0f;
            secondIntroGroup.alpha = 0f;
            secondIntroText.maxVisibleCharacters = 0;
            if (secondIntroTypingAudio != null) secondIntroTypingAudio.Stop();
            PrepareGameplayAudio();
            SuspendSceneControls(gameObject.scene);
            if (playerControls != null)
                foreach (var control in playerControls)
                    Suspend(control);
        }

        private void SuspendSceneControls(Scene scene)
        {
            // Prefab player components do not have scene-local references until instantiated.
            foreach (var control in FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (control.gameObject.scene != scene) continue;
                switch (control.GetType().Name)
                {
                    case "FirstPersonController":
                    case "PlayerInput":
                    case "MouseLook":
                    case "PlayerMovement":
                    case "PlayerInteractor":
                    case "FirstPersonFlashlight":
                        Suspend(control);
                        break;
                }
            }
        }

        private void Suspend(Behaviour control)
        {
            if (control == null || control == this || !control.enabled || suspended.Contains(control)) return;
            suspended.Add(control);
            control.enabled = false;
        }

        private IEnumerator Start()
        {
            // Let scene Start callbacks and the first rendered frames prepare resources
            // behind the black Canvas. Gameplay remains paused throughout.
            yield return null;
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, firstTextDelaySeconds));
            foreach (var paragraph in paragraphs)
            {
                yield return Fade(paragraph, 1f, textFadeSeconds);
                yield return new WaitForSecondsRealtime(paragraphHoldSeconds);
            }

            // Require streaming to stay idle across frames before accepting input.
            int readyFrames = 0;
            while (readyFrames < 3)
            {
                readyFrames = Texture.streamingTexturePendingLoadCount == 0
                    && Texture.streamingTextureLoadingCount == 0 ? readyFrames + 1 : 0;
                yield return null;
            }
            yield return Fade(pressAnyKey, 1f, promptFadeSeconds);
            // A held key from before the prompt cannot dismiss it.
            while (AnyButtonHeld()) yield return null;
            do { yield return null; } while (!AnyButtonPressed());

            float elapsed = 0f;
            while (elapsed < fadeToBlackSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeToBlackSeconds));
                foreach (var paragraph in paragraphs) paragraph.alpha = alpha;
                pressAnyKey.alpha = alpha;
                yield return null;
            }
            foreach (var paragraph in paragraphs) paragraph.alpha = 0f;
            pressAnyKey.alpha = 0f;
            yield return PlaySecondIntro();
            yield return new WaitForSecondsRealtime(blackHoldSeconds);
            yield return RevealGame();
            ReleaseControls();
            // Loading can finish after the window's focus callback has already run.
            // Enter gameplay with mouse capture explicitly restored after enabling input.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            gameObject.SetActive(false);
        }

        private void PrepareGameplayAudio()
        {
            if (gameplayAudioSources == null) return;
            foreach (var source in gameplayAudioSources)
            {
                if (source == null || gameplayAudioVolumes.ContainsKey(source)) continue;
                gameplayAudioVolumes.Add(source, source.volume);
                source.Stop();
                source.volume = 0f;
            }
        }

        private void StartGameplayAudio()
        {
            foreach (var entry in gameplayAudioVolumes)
                if (entry.Key != null && entry.Key.isActiveAndEnabled && entry.Key.clip != null && !entry.Key.isPlaying)
                    entry.Key.Play();
        }

        private IEnumerator RevealGame()
        {
            StartGameplayAudio();
            float elapsed = 0f;
            float duration = Mathf.Max(0f, revealGameSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                // Visible picture and audio use the same linear 0-to-1 progress.
                screen.alpha = 1f - progress;
                foreach (var entry in gameplayAudioVolumes)
                    if (entry.Key != null) entry.Key.volume = entry.Value * progress;
                yield return null;
            }
            screen.alpha = 0f;
            RestoreGameplayAudio();
        }

        private void RestoreGameplayAudio()
        {
            // Also recover ambience if Intro is manually disabled before it finishes.
            StartGameplayAudio();
            foreach (var entry in gameplayAudioVolumes)
                if (entry.Key != null) entry.Key.volume = entry.Value;
            gameplayAudioVolumes.Clear();
        }

        private IEnumerator PlaySecondIntro()
        {
            // Full opacity immediately; reveal characters instead of fading the text in.
            secondIntroText.maxVisibleCharacters = 0;
            secondIntroGroup.alpha = 1f;
            secondIntroText.ForceMeshUpdate();
            int count = secondIntroText.textInfo.characterCount;
            float elapsed = 0f;
            if (count > 0 && secondIntroTypingAudio != null && secondIntroTypingAudio.clip != null)
                secondIntroTypingAudio.Play();
            while (secondIntroText.maxVisibleCharacters < count)
            {
                elapsed += Time.unscaledDeltaTime;
                secondIntroText.maxVisibleCharacters = Mathf.Min(count,
                    Mathf.FloorToInt(elapsed / Mathf.Max(.001f, secondIntroSecondsPerCharacter)));
                if (secondIntroText.maxVisibleCharacters >= count && secondIntroTypingAudio != null)
                    secondIntroTypingAudio.Stop();
                yield return null;
            }
            if (secondIntroTypingAudio != null) secondIntroTypingAudio.Stop();
            yield return new WaitForSecondsRealtime(secondIntroHoldSeconds);
            yield return Fade(secondIntroGroup, 0f, secondIntroFadeOutSeconds);
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

        private static bool AnyButtonHeld()
        {
            foreach (var device in InputSystem.devices)
                foreach (var control in device.allControls)
                    if (control is ButtonControl button && !button.synthetic && button.isPressed) return true;
            return false;
        }

        private static bool AnyButtonPressed()
        {
            foreach (var device in InputSystem.devices)
                foreach (var control in device.allControls)
                    if (control is ButtonControl button && !button.synthetic && button.wasPressedThisFrame) return true;
            return false;
        }

        private void ReleaseControls()
        {
            if (!ownsPause) return;
            ownsPause = false;
            foreach (var input in FindObjectsByType<StarterAssets.StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                input.move = input.look = Vector2.zero;
                input.jump = input.sprint = false;
            }
            foreach (var control in suspended)
                if (control != null) control.enabled = true;
            suspended.Clear();
            Time.timeScale = previousTimeScale;
            if (screen != null) screen.blocksRaycasts = false;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (secondIntroTypingAudio != null) secondIntroTypingAudio.Stop();
            RestoreGameplayAudio();
            ReleaseControls();
            if (screen != null) screen.alpha = 0f;
            if (owner == this) owner = null;
        }
    }
}
