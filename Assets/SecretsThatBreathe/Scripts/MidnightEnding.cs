using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    // MainScene_2_MidNight, the end: once Khem has said his last lines about the friend with the garage, the objective
    // "กลับไปที่รถ ..." appears. Close to the car, "[E] ขึ้นรถไปที่อู่" boards it (door, black screen). On the black screen the
    // closing lines type in one after another, then fade, and the key art fades in with "Coming soon on Steam".
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
        [Tooltip("How close (metres, along the floor) Khem must be to where he got out of the car.")]
        [Min(.5f)] public float boardDistance = 2.4f;
        [Tooltip("...or this close (metres, along the floor) to the car body, on any side.")]
        [Min(.2f)] public float carBodyDistance = 1.6f;

        [Header("Sound")]
        [Tooltip("The rain (its sound fades out once E is pressed at the car).")]
        public MidnightRain rain;
        [Tooltip("Seconds for the rain and the other looping sounds of the scene to fade out after E at the car.")]
        [Min(0f)] public float soundFadeSeconds = 2.5f;

        [Header("Ending screen")]
        [Tooltip("Black, full screen, above everything else.")]
        public CanvasGroup endingScreen;
        [Tooltip("Typed one after another on the black screen.")]
        public TMP_Text[] endingLines;
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
        [Min(0f)] public float comingSoonDelay = .8f;
        [Min(.05f)] public float comingSoonFadeSeconds = 1.2f;

        public bool ObjectiveShown { get; private set; }
        public bool Boarding { get; private set; }
        public bool Finished { get; private set; }

        private float objectiveAt = -1f;
        private Renderer[] carRenderers;
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
            Vector3 door = ride.exitPoint != null ? ride.exitPoint.position : ride.transform.TransformPoint(ride.exitLocalPosition);
            Vector3 delta = feet - door;
            if (Mathf.Abs(delta.y) < 2f && new Vector2(delta.x, delta.z).sqrMagnitude <= boardDistance * boardDistance) return true;
            // anywhere close to the car body
            if (carRenderers == null) carRenderers = ride.GetComponentsInChildren<Renderer>();
            bool any = false;
            var body = new Bounds();
            foreach (var renderer in carRenderers)
            {
                if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer) continue;
                if (!any) { body = renderer.bounds; any = true; } else body.Encapsulate(renderer.bounds);
            }
            if (!any || feet.y < body.min.y - 2f || feet.y > body.max.y + 1f) return false;
            Vector3 closest = body.ClosestPoint(new Vector3(feet.x, body.center.y, feet.z));
            return new Vector2(closest.x - feet.x, closest.z - feet.z).sqrMagnitude <= carBodyDistance * carBodyDistance;
        }

        private IEnumerator Board()
        {
            Boarding = true;
            SetPanel(boardPrompt, false, ref promptShown);
            SetPanel(objective, false, ref objectiveVisible);
            StartCoroutine(FadeOutSounds());
            yield return ride.BoardForSceneTransition();   // door, black screen, Khem back in his seat
            if (ride.HasExited) { Boarding = false; SetPanel(objective, true, ref objectiveVisible); yield break; }   // could not board
            SetGroup(endingScreen, 1f);
            yield return Ending();
        }

        // The rain and every other looping sound in the scene fade out and stop (the car door sounds and the typing are kept).
        private IEnumerator FadeOutSounds()
        {
            if (rain != null) rain.FadeOutSound(soundFadeSeconds);
            var sources = new List<AudioSource>();
            var volumes = new List<float>();
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source == null || source.gameObject.scene != gameObject.scene || !source.isPlaying || !source.loop) continue;
                if (source == typingAudio || (ride != null && source == ride.doorAudio)) continue;
                if (rain != null && source.transform.IsChildOf(rain.transform)) continue;   // the rain fades itself
                sources.Add(source);
                volumes.Add(source.volume);
            }
            for (float t = 0f; t < soundFadeSeconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.SmoothStep(0f, 1f, t / soundFadeSeconds);
                for (int i = 0; i < sources.Count; i++) if (sources[i] != null) sources[i].volume = volumes[i] * k;
                yield return null;
            }
            foreach (var source in sources) if (source != null) source.Stop();
        }

        private IEnumerator Ending()
        {
            yield return new WaitForSecondsRealtime(blackBeforeText);
            if (endingLines != null)
                for (int i = 0; i < endingLines.Length; i++)
                {
                    var line = endingLines[i];
                    if (line == null) continue;
                    if (i > 0 && lineGap > 0f) yield return new WaitForSecondsRealtime(lineGap);
                    yield return Type(line);
                }
            yield return new WaitForSecondsRealtime(holdText);

            // the lines fade, then the key art comes in
            yield return Fade(t => { if (endingLines != null) foreach (var line in endingLines) if (line != null) line.alpha = 1f - t; }, textFadeSeconds);
            yield return new WaitForSecondsRealtime(blackBeforeArt);
            yield return Fade(t => SetGroup(keyArt, t), artFadeSeconds);
            yield return new WaitForSecondsRealtime(comingSoonDelay);
            yield return Fade(t => SetGroup(comingSoon, t), comingSoonFadeSeconds);
            Finished = true;
        }

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
                shown += Time.unscaledDeltaTime / secondsPerCharacter;
                line.maxVisibleCharacters = Mathf.Min(count, Mathf.FloorToInt(shown));
                yield return null;
            }
            line.maxVisibleCharacters = int.MaxValue;
            if (typingAudio != null) typingAudio.Stop();
        }

        private static IEnumerator Fade(System.Action<float> apply, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) { apply(Mathf.SmoothStep(0f, 1f, t / seconds)); yield return null; }
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
