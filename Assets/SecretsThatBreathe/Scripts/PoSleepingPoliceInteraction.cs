using System.Collections;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    // Po scene: Khem walks up to the sleeping policeman, looks at him and presses E. The policeman's snoring appears as dialogue,
    // then Khem thinks one line to himself - shown exactly like his thought in the station opening: the saved label appears at the
    // bottom of the screen, in brackets, without a name. Khem cannot walk while the lines are on screen.
    // Every object this uses is saved in the scene; edit the wording of the saved labels in the Hierarchy.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9000)]
    public sealed class PoSleepingPoliceInteraction : MonoBehaviour
    {
        [Header("Scene objects")]
        [Tooltip("Optional. The interaction is available once the station opening has finished.")]
        public PoliceStationOpening opening;
        public FirstPersonController player;
        [Tooltip("The SleepingPolice in the scene. Move the policeman and this follows him.")]
        public Transform police;

        [Header("Pressing E")]
        [Tooltip("Khem must be this close (metres, along the floor) to the policeman.")]
        [Min(.5f)] public float interactDistance = 2.2f;
        [Tooltip("1 = looking straight at the policeman. Khem must look roughly towards him to be offered the interaction.")]
        [Range(0f, 1f)] public float facingDotMinimum = .7f;
        [Tooltip("Height above the policeman's feet that Khem has to look towards (his chest).")]
        [Min(0f)] public float lookAtHeight = .85f;
        [Tooltip("The saved '[E] ...' label. Edit its text in the Hierarchy.")]
        public TMP_Text prompt;
        [Tooltip("Off = Khem can only do this once.")]
        public bool canRepeat;

        [Header("Dialogue (same boxes, font and typing sound as the station dialogue)")]
        public DialogueCanvasView policeDialogue = new DialogueCanvasView();
        public FlowingDialogueSettings presentation = new FlowingDialogueSettings();
        [Tooltip("The snoring, typed into the policeman's box. Saved text labels - edit the wording in the Hierarchy.")]
        public TMP_Text[] snoreLines;
        [Tooltip("Khem's thought, shown at its authored position at the bottom of the screen without typing. Saved text labels - edit the wording in the Hierarchy.")]
        public TMP_Text[] khemThoughtLines;
        [Tooltip("Seconds each thought stays on screen.")]
        [Min(0f)] public float thoughtHoldSeconds = 3.5f;
        [Min(0f)] public float dialogueDelay = .2f;

        public UnityEvent onFinished = new UnityEvent();
        public bool Completed { get; private set; }
        public bool IsRunning { get; private set; }

        private readonly PoDialoguePlayer speech = new PoDialoguePlayer();
        private GameObject visibleThought;
        private bool holdingControl, promptShown = true;   // true so the first SetPrompt(false) really hides the label

        private void Awake()
        {
            HideDialogue();
            SetPrompt(false);
        }

        private void Start()
        {
            if (player == null || police == null || !policeDialogue.IsValid
                || snoreLines == null || snoreLines.Length == 0 || khemThoughtLines == null || khemThoughtLines.Length == 0)
            { Debug.LogError("Assign the sleeping policeman, player, dialogue Canvases and lines.", this); enabled = false; }
        }

        private void Update()
        {
            if (holdingControl && player != null) player.MovementLocked = true;
            if ((Completed && !canRepeat) || IsRunning) { SetPrompt(false); return; }
            bool ready = CanInteract();
            SetPrompt(ready);
            if (ready && Application.isFocused && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && PoDialogueGate.TryEnter())
                StartCoroutine(Conversation());
        }

        private bool CanInteract()
        {
            if (Time.timeScale <= 0f || player == null || police == null || !police.gameObject.activeInHierarchy) return false;
            if (PoDialogueGate.Busy) return false;
            if (opening != null && !opening.Completed) return false;
            if (!player.isActiveAndEnabled || player.MovementLocked) return false;
            Vector3 flat = police.position - player.transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > interactDistance * interactDistance) return false;
            var view = Camera.main;
            if (view == null) return false;
            Vector3 toBody = police.position + Vector3.up * lookAtHeight - view.transform.position;
            if (toBody.sqrMagnitude < .0001f) return true;
            return Vector3.Dot(view.transform.forward, toBody.normalized) >= facingDotMinimum;
        }

        private void OnDrawGizmosSelected()
        {
            if (police == null) return;
            Gizmos.color = Color.yellow;
            Vector3 origin = police.position + Vector3.up * .05f;
            Vector3 previous = origin + Vector3.forward * interactDistance;
            for (int i = 1; i <= 32; i++)
            {
                float angle = i / 32f * Mathf.PI * 2f;
                Vector3 point = origin + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * interactDistance;
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }

        private void SetPrompt(bool show)
        {
            if (prompt == null || promptShown == show) return;
            promptShown = show;
            var root = prompt.transform.parent != null ? prompt.transform.parent.gameObject : prompt.gameObject;
            prompt.gameObject.SetActive(true);
            root.SetActive(show);
        }

        private IEnumerator Conversation()
        {
            IsRunning = true;
            SetPrompt(false);
            holdingControl = true;
            player.MovementLocked = true;
            if (dialogueDelay > 0f) yield return new WaitForSeconds(dialogueDelay);
            yield return speech.Say(policeDialogue, presentation, snoreLines);
            HideDialogue();
            yield return ShowThoughts();
            HideDialogue();
            Finish();
            Completed = true;
            onFinished.Invoke();
        }

        // Same as Khem's thought in the station opening: the label's parent holds the authored position at the bottom of the screen.
        private IEnumerator ShowThoughts()
        {
            foreach (var line in khemThoughtLines)
            {
                if (line == null) continue;
                visibleThought = line.transform.parent != null ? line.transform.parent.gameObject : line.gameObject;
                line.maxVisibleCharacters = int.MaxValue;
                visibleThought.SetActive(true);
                yield return new WaitForSeconds(thoughtHoldSeconds);
                visibleThought.SetActive(false);
                visibleThought = null;
            }
        }

        private void Finish()
        {
            holdingControl = false;
            if (player != null) player.MovementLocked = false;
            IsRunning = false;
            PoDialogueGate.Leave();
        }

        private void HideDialogue()
        {
            speech.Stop();
            if (visibleThought != null) visibleThought.SetActive(false);
            visibleThought = null;
            if (PoDialoguePlayer.Alive(policeDialogue)) policeDialogue.Hide();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            HideDialogue();
            if (IsRunning) PoDialogueGate.Leave();
            if (holdingControl && player != null) player.MovementLocked = false;
            holdingControl = false;
            IsRunning = false;
            SetPrompt(false);
        }
    }
}
