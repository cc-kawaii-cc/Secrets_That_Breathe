using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsThatBreathe
{
    // MainScene_2_MidNight, the end: once Khem has said his last lines about the friend with the garage, the objective
    // "กลับไปที่รถ ..." appears. Close to the car, "[E] ขึ้นรถไปที่อู่" boards it (door, black screen). On the black screen the
    // closing lines fade in one by one, followed by the typed continuation and poster.
    // Every object this uses is saved in the scene: edit the wording of the labels in the Hierarchy
    // (SeatingCanvas/MidnightEndingUI and MidnightEndingCanvas).
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(15060)]
    public sealed class MidnightEnding : MonoBehaviour
    {
        [Header("Start")]
        [Tooltip("Starts once this has finished (the debris lines).")]
        public MidnightGhostSequence after;
        public PorschePassengerRide ride;
        public FirstPersonController player;
        [Min(0f)] public float objectiveDelay = .8f;

        [Header("Back to the car")]
        [Tooltip("Objective label (its panel is shown and hidden).")]
        public TMP_Text objective;
        [Tooltip("'[E] ...' label shown close to the car.")]
        public TMP_Text boardPrompt;
        [Tooltip("How close (metres, along the floor) Khem must be to the left-side exit point.")]
        [Min(.5f)] public float boardDistance = 2f;

        [Header("Sound")]
        [Tooltip("The rain (its sound fades out once E is pressed at the car).")]
        public MidnightRain rain;

        [Header("Ending screen")]
        [Tooltip("Black, full screen, above everything else.")]
        public CanvasGroup endingScreen;
        [Tooltip("Typed one after another on the black screen.")]
        public TMP_Text[] endingLines;
        public TMP_Text continuedLine;
        public Button menuButton, exitButton;
        [Tooltip("The key art (fades in after the lines).")]
        public CanvasGroup keyArt;
        [Tooltip("'Wishlist now on Steam' (fades in a moment after the key art).")]
        public CanvasGroup comingSoon;
        [Tooltip("The Steam logo next to it (hidden while it has no image: drag the official logo into its Source Image).")]
        public UnityEngine.UI.Image steamLogo;
        public AudioSource typingAudio;
        [Min(.005f)] public float secondsPerCharacter = .07f;
        [Min(0f)] public float blackBeforeText = 1.2f;
        [Min(0f)] public float lineGap = .7f;
        [Min(0f)] public float holdText = 2.5f;
        [Min(.05f)] public float textFadeSeconds = 1.2f;
        [Min(0f)] public float blackBeforeArt = .8f;
        [Min(.05f)] public float artFadeSeconds = 2f;
        [Min(0f)] public float posterButtonDelay = 1f;

        public bool ObjectiveShown { get; private set; }
        public bool Boarding { get; private set; }
        public bool Finished { get; private set; }

        private float objectiveAt = -1f;
        private bool promptShown = true, objectiveVisible = true;   // true so the first Set...(false) really hides them

        private void Awake()
        {
            SetPanel(objective, false, ref objectiveVisible);
            SetPanel(boardPrompt, false, ref promptShown);
            SetGroup(endingScreen, 0f);
            SetGroup(keyArt, 0f);
            SetGroup(comingSoon, 0f);
            if (steamLogo != null) steamLogo.gameObject.SetActive(steamLogo.sprite != null);
            if (endingLines != null) foreach (var line in endingLines) if (line != null) line.maxVisibleCharacters = 0;
            if (continuedLine != null) continuedLine.maxVisibleCharacters = 0;
            if (menuButton != null) menuButton.gameObject.SetActive(false);
            if (exitButton != null) exitButton.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Boarding || Finished) return;
            if (!ObjectiveShown)
            {
                if (after == null || !after.Finished) return;
                if (objectiveAt < 0f) objectiveAt = Time.time + objectiveDelay;
                if (Time.time < objectiveAt) return;
                ObjectiveShown = true;
                SetPanel(objective, true, ref objectiveVisible);
            }
            bool near = CanBoard();
            SetPanel(boardPrompt, near, ref promptShown);
            if (near && Application.isFocused && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                StartCoroutine(Board());
        }

        private bool CanBoard()
        {
            if (ride == null || player == null || !ride.HasExited || Time.timeScale <= 0f) return false;
            if (!player.isActiveAndEnabled || player.MovementLocked) return false;
            if (MidnightSkidPhoto.BlocksOtherPrompts(gameObject.scene)) return false;
            Vector3 feet = player.transform.position;
            if (ride.transform.InverseTransformPoint(feet).x >= 0f) return false;
            Vector3 door = ride.exitPoint != null ? ride.exitPoint.position : ride.transform.TransformPoint(ride.exitLocalPosition);
            Vector3 delta = feet - door;
            return Mathf.Abs(delta.y) < 2f && new Vector2(delta.x, delta.z).sqrMagnitude <= boardDistance * boardDistance;
        }

        private IEnumerator Board()
        {
            Boarding = true;
            SetPanel(boardPrompt, false, ref promptShown);
            SetPanel(objective, false, ref objectiveVisible);
            yield return ride.BoardForSceneTransition(CreateSoundFade());
            if (ride.HasExited) { Boarding = false; SetPanel(objective, true, ref objectiveVisible); yield break; }   // could not board
            if (endingScreen != null)
            {
                var canvas = endingScreen.GetComponentInParent<Canvas>(true);
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(true);
                    if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
                }
            }
            SetGroup(endingScreen, 1f);
            yield return Ending();
        }

        private System.Action<float> CreateSoundFade()
        {
            var playing = new List<(AudioSource source, float volume)>();
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source.isPlaying && (rain == null || source.transform != rain.transform))
                    playing.Add((source, source.volume));
            }
            return progress =>
            {
                if (rain != null) rain.SetSoundGain(1f - progress);
                foreach (var (source, volume) in playing)
                {
                    if (source == null || source == ride.doorAudio) continue;
                    source.volume = volume * (1f - progress);
                    if (progress >= 1f)
                    {
                        source.Stop();
                        if (source == typingAudio) source.volume = volume;
                    }
                }
            };
        }

        private IEnumerator Ending()
        {
            yield return GameMenuController.WaitForGameplaySeconds(blackBeforeText);
            if (endingLines != null)
                for (int i = 0; i < endingLines.Length; i++)
                {
                    var line = endingLines[i];
                    if (line == null) continue;
                    if (i > 0 && lineGap > 0f) yield return GameMenuController.WaitForGameplaySeconds(lineGap);
                    line.maxVisibleCharacters = int.MaxValue;
                    yield return Fade(t => line.alpha = t, 1f);
                }
            yield return GameMenuController.WaitForGameplaySeconds(holdText);

            yield return Fade(t => { if (endingLines != null) foreach (var line in endingLines) if (line != null) line.alpha = 1f - t; }, textFadeSeconds);
            if (continuedLine != null)
            {
                yield return GameMenuController.WaitForGameplaySeconds(1f);
                yield return Type(continuedLine);
                yield return GameMenuController.WaitForGameplaySeconds(1f);
                yield return Fade(t => continuedLine.alpha = 1f - t, textFadeSeconds);
            }
            yield return GameMenuController.WaitForGameplaySeconds(blackBeforeArt);
            yield return Fade(t => SetGroup(keyArt, t), artFadeSeconds);
            yield return GameMenuController.WaitForGameplaySeconds(posterButtonDelay);
            Finished = true;
            if (endingScreen != null) endingScreen.interactable = true;
            if (menuButton != null) menuButton.gameObject.SetActive(true);
            if (exitButton != null) exitButton.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene("MainMenu");
        }

        public void ExitGame() => Application.Quit();

        private IEnumerator Type(TMP_Text line)
        {
            line.alpha = 1f;
            line.ForceMeshUpdate();
            int count = line.textInfo != null ? line.textInfo.characterCount : line.text.Length;
            line.maxVisibleCharacters = 0;
            if (typingAudio != null && typingAudio.clip != null) { typingAudio.loop = true; typingAudio.Play(); }
            float shown = 0f;
            while (line.maxVisibleCharacters < count)
            {
                shown += GameMenuController.GameplayUnscaledDeltaTime / secondsPerCharacter;
                line.maxVisibleCharacters = Mathf.Min(count, Mathf.FloorToInt(shown));
                yield return null;
            }
            line.maxVisibleCharacters = int.MaxValue;
            if (typingAudio != null) typingAudio.Stop();
        }

        private static IEnumerator Fade(System.Action<float> apply, float seconds)
        {
            for (float t = 0f; t < seconds; t += GameMenuController.GameplayUnscaledDeltaTime) { apply(Mathf.SmoothStep(0f, 1f, t / seconds)); yield return null; }
            apply(1f);
        }

        private static void SetGroup(CanvasGroup group, float alpha)
        {
            if (group == null) return;
            if (alpha > 0f && !group.gameObject.activeSelf) group.gameObject.SetActive(true);
            group.alpha = alpha;
            group.blocksRaycasts = alpha > .5f;
            group.interactable = false;
        }

        private static void SetPanel(TMP_Text label, bool show, ref bool shown)
        {
            if (label == null || shown == show) return;
            shown = show;
            if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
            label.maxVisibleCharacters = int.MaxValue;
            var panel = label.transform.parent != null ? label.transform.parent.gameObject : label.gameObject;
            panel.SetActive(show);
        }

        private void OnDisable()
        {
            if (typingAudio != null) typingAudio.Stop();
        }
    }
}
