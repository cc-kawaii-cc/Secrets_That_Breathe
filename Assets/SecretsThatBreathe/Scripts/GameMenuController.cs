using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace SecretsThatBreathe
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameMenuController : MonoBehaviour
    {
        public bool mainMenu;
        public GameObject background, menuPanel, settingsPanel;
        public TMP_Text fpsValue;
        private int selection;
        private bool paused;
        private float previousTime;
        private CursorLockMode previousLock;
        private bool previousCursor;
        private bool previousAudioPause;
        private int resumeFrame = -1;
        private static GameMenuController pauseOwner;
        public static bool BlocksGameplayInput => pauseOwner != null;
        internal static float GameplayUnscaledDeltaTime => BlocksGameplayInput ? 0f : Time.unscaledDeltaTime;
        private GameObject previousSelection;

        internal static IEnumerator WaitForGameplaySeconds(float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += GameplayUnscaledDeltaTime)
                yield return null;
        }

        private void Start()
        {
            ConfigureHover(menuPanel);
            ConfigureHover(settingsPanel);
            if (mainMenu) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; ShowMenu(); }
            else { background.SetActive(false); menuPanel.SetActive(false); settingsPanel.SetActive(false); }
        }

        private static void ConfigureHover(GameObject panel)
        {
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;
                bool grow = button.name == "Start Button" || button.name == "Resume Button"
                    || button.name == "Setting Button" || button.name == "Exit Button";
                button.transition = Selectable.Transition.None;
                button.gameObject.AddComponent<MoneyChoiceButtonFeedback>()
                    .Configure(button, label, label.color * new Color(.88f, .88f, .88f, 1f), label.color, grow ? 1.07f : 1f, .18f);
            }
        }

        private void Update()
        {
            if (!paused && pauseOwner == this && resumeFrame < Time.frameCount) pauseOwner = null;
            if (mainMenu || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (paused) { if (settingsPanel.activeSelf) ShowMenu(); else Resume(); }
            else if (Time.timeScale > 0f || LawyerRoomIntro.IsInputBlocked(gameObject.scene)
                || EveningDriveIntro.IsInputBlocked(gameObject.scene)) Pause();
        }
        public void Pause()
        {
            if (paused || mainMenu) return;
            previousTime = Time.timeScale; previousLock = Cursor.lockState; previousCursor = Cursor.visible;
            previousAudioPause = AudioListener.pause;
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            previousSelection = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
            pauseOwner = this;
            paused = true; Time.timeScale = 0f; AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            background.SetActive(true); ShowMenu();
        }
        public void Resume()
        {
            if (!paused) return;
            paused = false; Time.timeScale = previousTime; AudioListener.pause = previousAudioPause;
            resumeFrame = Time.frameCount;
            Cursor.lockState = previousLock; Cursor.visible = previousCursor;
            background.SetActive(false); menuPanel.SetActive(false); settingsPanel.SetActive(false);
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null && previousSelection != null && previousSelection.activeInHierarchy)
                eventSystem.SetSelectedGameObject(previousSelection);
        }
        private void LateUpdate()
        {
            if (!paused || !Application.isFocused) return;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
        }
        public void ShowMenu() { menuPanel.SetActive(true); settingsPanel.SetActive(false); }
        public void ShowSettings()
        {
            selection = System.Array.IndexOf(GameFrameRateSettings.Options, GameFrameRateSettings.Current);
            if (selection < 0) selection = 0;
            menuPanel.SetActive(false); settingsPanel.SetActive(true); Refresh();
        }
        public void StepLeft() { selection = (selection + GameFrameRateSettings.Options.Length - 1) % GameFrameRateSettings.Options.Length; Refresh(); }
        public void StepRight() { selection = (selection + 1) % GameFrameRateSettings.Options.Length; Refresh(); }
        private void Refresh() { int fps = GameFrameRateSettings.Options[selection]; fpsValue.text = fps < 0 ? "Unlimit" : fps.ToString(); }
        public void Apply() { GameFrameRateSettings.Apply(GameFrameRateSettings.Options[selection]); ShowMenu(); }
        public void StartGame() { AudioListener.pause = false; Time.timeScale = 1f; SceneManager.LoadScene("Lawyer Room"); }
        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void OnDisable()
        {
            if (paused) Resume();
            if (pauseOwner == this) pauseOwner = null;
        }
    }
}
