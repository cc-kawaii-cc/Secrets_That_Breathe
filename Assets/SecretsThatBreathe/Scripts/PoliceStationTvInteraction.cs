using System.Collections;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    // Inside_Police_Station, a small extra: Khem can walk up to a TV (Tv (5)) and press E. The TV turns on (its hidden "on" screen is
    // shown, the "off" screen is hidden), the song plays from 3:10 to 3:20 and fades out towards the end, then Khem says his line.
    // The TV stays on afterwards. Every object this uses is saved in the scene.
    [DisallowMultipleComponent]
    public sealed class PoliceStationTvInteraction : MonoBehaviour
    {
        [Header("Scene objects")]
        [Tooltip("Step 1. The TV can be used once it has finished.")]
        public PoliceStationOpening opening;
        public FirstPersonController player;
        [Tooltip("The TV Khem presses E at.")]
        public Transform tv;
        [Tooltip("The screen that is hidden until the TV is turned on (TV_Screen_on).")]
        public GameObject screenOn;
        [Tooltip("The dark screen of the TV that is switched off. It is hidden when the TV turns on (TV_Screen_off). Can be empty.")]
        public GameObject screenOff;

        [Header("Pressing E at the TV")]
        [Min(.5f)] public float interactDistance = 3f;
        [Tooltip("How close to the middle of the screen the TV must be (degrees from straight ahead).")]
        [Range(5f, 60f)] public float lookAngle = 20f;
        [Tooltip("The saved '[E] ...' label. Edit its text in the Hierarchy.")]
        public TMP_Text prompt;

        [Header("The song (only this part is played)")]
        [Tooltip("Plays from the TV's position. The clip and the 3D settings are on this AudioSource.")]
        public AudioSource music;
        [Tooltip("Seconds into the song where it starts. 190 = 3:10.")]
        [Min(0f)] public float songStartSeconds = 190f;
        [Tooltip("Seconds into the song where it ends. 200 = 3:20.")]
        [Min(1f)] public float songEndSeconds = 200f;
        [Tooltip("The last seconds of the part get quieter and quieter until silent at the end.")]
        [Min(.1f)] public float fadeOutSeconds = 3f;
        [Range(0f, 1f)] public float songVolume = .8f;

        [Header("Timing")]
        [Min(0f)] public float musicDelay = .3f;
        [Tooltip("Silence between the end of the song and Khem's line.")]
        [Min(0f)] public float lineDelay = .6f;
        [Min(0f)] public float holdAfterLine = .5f;

        [Header("Dialogue (same box, font and typing sound as step 1)")]
        public DialogueCanvasView khemDialogue = new DialogueCanvasView();
        public FlowingDialogueSettings presentation = new FlowingDialogueSettings();
        [Tooltip("Saved text label. Edit the wording in the Hierarchy.")]
        public TMP_Text[] khemLines;

        public UnityEvent onFinished = new UnityEvent();
        public bool Completed { get; private set; }
        public bool IsRunning { get; private set; }

        private FlowingDialogue flowing;
        private bool holdingControl, promptShown = true;   // true so the first SetPrompt(false) really hides the panel
        private Renderer[] tvRenderers;

        private void Awake()
        {
            HideDialogue();
            SetPrompt(false);
        }

        private void Start()
        {
            if (player == null || tv == null || prompt == null || !khemDialogue.IsValid || khemLines == null || khemLines.Length == 0)
            { Debug.LogError("Assign the player, the TV, the prompt, the dialogue Canvas and the line.", this); enabled = false; return; }
            tvRenderers = tv.GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            if (holdingControl && player != null) player.MovementLocked = true;
            if (Completed || IsRunning) { SetPrompt(false); return; }
            bool ready = CanUse();
            SetPrompt(ready);
            if (ready && Application.isFocused && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                StartCoroutine(Moment());
        }

        private bool CanUse()
        {
            if (Time.timeScale <= 0f || player == null || tv == null || !tv.gameObject.activeInHierarchy) return false;
            if (opening != null && !opening.Completed) return false;
            if (!player.isActiveAndEnabled || player.MovementLocked) return false;
            return InteractionLook.IsLookingAt(Camera.main, tvRenderers, lookAngle, interactDistance);
        }

        private static void SetPanel(TMP_Text label, bool show)
        {
            if (label == null) return;
            var root = label.transform.parent != null ? label.transform.parent.gameObject : label.gameObject;
            label.gameObject.SetActive(true);
            root.SetActive(show);
        }

        private void SetPrompt(bool show)
        {
            if (promptShown == show) return;
            promptShown = show;
            SetPanel(prompt, show);
        }

        // ---- The moment --------------------------------------------------------------------------------------------------------------
        private IEnumerator Moment()
        {
            IsRunning = true;
            holdingControl = true;
            player.MovementLocked = true;
            SetPrompt(false);
            // 1. The TV turns on.
            if (screenOff != null) screenOff.SetActive(false);
            if (screenOn != null) screenOn.SetActive(true);
            yield return new WaitForSeconds(musicDelay);
            // 2. The part of the song, getting quieter at the end.
            yield return PlaySong();
            yield return new WaitForSeconds(lineDelay);
            // 3. Khem speaks.
            yield return SayAll(khemDialogue, khemLines);
            HideDialogue();
            yield return new WaitForSeconds(holdAfterLine);
            holdingControl = false;
            if (player != null) player.MovementLocked = false;
            IsRunning = false;
            Completed = true;
            onFinished.Invoke();
        }

        private IEnumerator PlaySong()
        {
            if (music == null || music.clip == null) yield break;
            float end = Mathf.Min(songEndSeconds, music.clip.length);
            float start = Mathf.Clamp(songStartSeconds, 0f, Mathf.Max(0f, end - .5f));
            music.volume = songVolume;
            music.time = start;
            music.Play();
            yield return null;
            Debug.Log("[TV] song playing from " + music.time.ToString("F1") + " s (playing: " + music.isPlaying + ", volume " + music.volume.ToString("F2") + ")", this);
            float waited = 0f, limit = end - start + 5f;   // never waits longer than the part itself (+5 s) even if the audio stalls
            while (music.isPlaying && music.time < end && waited < limit)
            {
                float quiet = Mathf.Clamp01((end - music.time) / Mathf.Max(.1f, fadeOutSeconds));
                music.volume = songVolume * Mathf.SmoothStep(0f, 1f, quiet);
                waited += Time.deltaTime;
                yield return null;
            }
            Debug.Log("[TV] song stopped at " + music.time.ToString("F1") + " s", this);
            music.Stop();
            music.volume = songVolume;
        }

        private IEnumerator SayAll(DialogueCanvasView canvas, TMP_Text[] lines)
        {
            if (lines == null) yield break;
            foreach (var line in lines)
            {
                if (line == null) continue;
                flowing?.Hide();
                ApplySentenceStyle(canvas, line);
                flowing = new FlowingDialogue(canvas, presentation);
                yield return flowing.Play(line.text);
            }
        }

        private static void ApplySentenceStyle(DialogueCanvasView canvas, TMP_Text line)
        {
            if (line == null || canvas.sentence == null) return;
            var target = canvas.sentence;
            target.font = line.font; target.fontSharedMaterial = line.fontSharedMaterial;
            target.fontSize = line.fontSize; target.fontStyle = line.fontStyle; target.fontWeight = line.fontWeight;
            target.color = line.color; target.characterSpacing = line.characterSpacing;
            target.wordSpacing = line.wordSpacing; target.lineSpacing = line.lineSpacing; target.paragraphSpacing = line.paragraphSpacing;
            target.enableAutoSizing = line.enableAutoSizing; target.fontSizeMin = line.fontSizeMin; target.fontSizeMax = line.fontSizeMax;
        }

        // When Play Mode stops, Unity destroys the Canvas objects before this component's OnDisable runs.
        private static bool Alive(DialogueCanvasView view) => view != null && view.IsValid && view.sentence != null;

        private void HideDialogue()
        {
            if (Alive(khemDialogue)) flowing?.Hide();
            flowing = null;
            if (Alive(khemDialogue)) khemDialogue.Hide();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            HideDialogue();
            SetPrompt(false);
            if (music != null) { music.Stop(); music.volume = songVolume; }
            if (holdingControl && player != null) player.MovementLocked = false;
            holdingControl = false;
            IsRunning = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (tv == null) return;
            Gizmos.color = new Color(.3f, .7f, 1f, .9f);
            var renderers = tv.GetComponentsInChildren<Renderer>(true);
            Vector3 centre = tv.position;
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                centre = bounds.center;
            }
            Vector3 ring = new Vector3(centre.x, .3f, centre.z);
            const int segments = 48;
            Vector3 previous = ring + new Vector3(interactDistance, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 next = ring + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * interactDistance;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
