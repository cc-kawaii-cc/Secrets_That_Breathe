using System.Collections;
using System.Collections.Generic;
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
        [Header("Loading scene (enabled only in Lawyer Room Intro scene)")]
        public bool loadGameplayScene;
        public string gameplayScenePath = "Assets/Tonpalm/Lawyer Room.unity";

        [Header("Saved Canvas objects — edit text and colours on their TMP components")]
        public CanvasGroup screen;
        public CanvasGroup[] paragraphs;
        public CanvasGroup pressAnyKey;

        [Header("Timing (real seconds)")]
        [Min(0f)] public float textFadeSeconds = 1f;
        [Min(0f)] public float paragraphHoldSeconds = 3f;
        [Min(0f)] public float promptFadeSeconds = 1f;
        [Min(0f)] public float fadeToBlackSeconds = 1f;
        [Min(0f)] public float blackHoldSeconds = 0.25f;
        [Min(0f)] public float revealGameSeconds = 2f;

        [Header("Player controls suspended until the reveal finishes")]
        public Behaviour[] playerControls;

        private readonly List<Behaviour> suspended = new List<Behaviour>();
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
            if (scene.path != "Assets/Tonpalm/Lawyer Room.unity" || owner != null) return;
            var prefab = Resources.Load<LawyerRoomIntro>("LawyerRoomIntroCanvas");
            if (prefab == null)
            {
                Debug.LogError("Lawyer Room Intro Canvas prefab is missing from Resources.");
                return;
            }
            var instance = Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(instance.gameObject, scene);
        }

        private void Awake()
        {
            // The loading screen persists into gameplay; hide the direct-Play copy.
            if (owner != null && owner != this)
            {
                gameObject.SetActive(false);
                return;
            }
            if (screen == null || pressAnyKey == null || paragraphs == null || paragraphs.Length != 3
                || System.Array.Exists(paragraphs, p => p == null))
            {
                Debug.LogError("Intro requires its screen, three paragraphs and key prompt.", this);
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
            SuspendSceneControls(gameObject.scene);
            if (playerControls != null)
                foreach (var control in playerControls)
                    Suspend(control);
            if (loadGameplayScene)
            {
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnGameplayLoaded;
            }
        }

        private void OnGameplayLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path == gameplayScenePath) SuspendSceneControls(scene);
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
            // Present the black Canvas before starting disk IO / scene activation.
            yield return null;
            AsyncOperation loading = null;
            if (loadGameplayScene)
            {
                if (!Application.CanStreamedLevelBeLoaded(gameplayScenePath))
                {
                    Debug.LogError("Add the Lawyer Room gameplay scene to the Build scene list.", this);
                    yield break;
                }
                loading = SceneManager.LoadSceneAsync(gameplayScenePath, LoadSceneMode.Single);
            }
            // Let the loaded scene run Awake/Start and upload its first rendered frames.
            yield return null;
            yield return null;
            foreach (var paragraph in paragraphs)
            {
                yield return Fade(paragraph, 1f, textFadeSeconds);
                yield return new WaitForSecondsRealtime(paragraphHoldSeconds);
            }

            // Keep the black cover while visible textures finish streaming.
            while (loading != null && !loading.isDone) yield return null;
            while (Texture.streamingTexturePendingLoadCount > 0)
                yield return null;
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
            yield return new WaitForSecondsRealtime(blackHoldSeconds);
            yield return Fade(screen, 0f, revealGameSeconds);
            ReleaseControls();
            gameObject.SetActive(false);
            if (loadGameplayScene) Destroy(gameObject);
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
            SceneManager.sceneLoaded -= OnGameplayLoaded;
            StopAllCoroutines();
            ReleaseControls();
            if (screen != null) screen.alpha = 0f;
            if (owner == this) owner = null;
        }
    }
}
