using System.Collections;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SecretsThatBreathe
{
    // Po scene: when Khem walks into the box in front of a glass door by the cells, a prisoner's voice shouts from behind the door,
    // one phrase after another. Khem is not stopped - the lines just appear (same box, font and typing sound as the other station
    // dialogue) and go away. The boxes are saved in the scene (PoCellBlockDialogue/GlassDoorZones): select one in the Hierarchy and
    // move, rotate or resize it in the Scene view (or use Edit Collider) to choose exactly where the voice starts.
    // Edit the wording of the saved labels in the Hierarchy.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9000)]
    public sealed class PoGlassDoorVoice : MonoBehaviour
    {
        [Header("Scene objects")]
        [Tooltip("Optional. The voice waits until the station opening has finished.")]
        public PoliceStationOpening opening;
        public FirstPersonController player;

        [Header("Where the voice starts")]
        [Tooltip("Khem hears the voice when he walks into any of these boxes. Move / resize the boxes in the Scene view. Add more boxes to this list for more doors.")]
        public BoxCollider[] triggerZones;
        [Tooltip("Height above Khem's feet that has to be inside the box.")]
        [Min(0f)] public float checkHeight = .9f;
        [Tooltip("Off = the voice shouts again each time Khem walks back into a box.")]
        public bool playOnlyOnce = true;

        [Header("Dialogue (same boxes, font and typing sound as the station dialogue)")]
        public DialogueCanvasView voiceDialogue = new DialogueCanvasView();
        public FlowingDialogueSettings presentation = new FlowingDialogueSettings();
        [Tooltip("Saved text labels, played one after another. Edit the wording in the Hierarchy.")]
        public TMP_Text[] voiceLines;
        [Min(0f)] public float dialogueDelay = .2f;

        public UnityEvent onFinished = new UnityEvent();
        public bool Completed { get; private set; }
        public bool IsRunning { get; private set; }

        // Written by the editor setup so it knows whether this scene was built by the current version.
        [HideInInspector] public int setupRevision;

        private readonly PoDialoguePlayer speech = new PoDialoguePlayer();
        private bool inside;

        private void Awake() => HideDialogue();

        private void Start()
        {
            if (player == null || triggerZones == null || triggerZones.Length == 0 || !voiceDialogue.IsValid
                || voiceLines == null || voiceLines.Length == 0)
            { Debug.LogError("Assign the player, the trigger boxes, the dialogue Canvas and the voice lines.", this); enabled = false; }
        }

        private void Update()
        {
            if (IsRunning || (Completed && playOnlyOnce)) return;
            if (!IsInsideAZone()) { inside = false; return; }
            if (inside || !CanPlayNow()) return;
            if (!PoDialogueGate.TryEnter()) return;
            inside = true;
            StartCoroutine(Play());
        }

        private bool CanPlayNow()
        {
            if (Time.timeScale <= 0f || player == null || !player.isActiveAndEnabled || player.MovementLocked) return false;
            if (opening != null && !opening.Completed) return false;
            return true;
        }

        private bool IsInsideAZone()
        {
            Vector3 point = player.transform.position + Vector3.up * checkHeight;
            foreach (var zone in triggerZones)
                if (InteractionLook.IsPointInBox(point, zone)) return true;
            return false;
        }

        // Show the interaction area while its controller is selected, like the CCTV area.
        private void OnDrawGizmosSelected()
        {
            if (triggerZones == null) return;
            var previous = Gizmos.matrix;
            foreach (var zone in triggerZones)
            {
                if (zone == null) continue;
                Gizmos.matrix = zone.transform.localToWorldMatrix;
                Gizmos.color = new Color(1f, .6f, .1f, .12f);
                Gizmos.DrawCube(zone.center, zone.size);
                Gizmos.color = new Color(1f, .6f, .1f, 1f);
                Gizmos.DrawWireCube(zone.center, zone.size);
            }
            Gizmos.matrix = previous;
        }

        private IEnumerator Play()
        {
            IsRunning = true;
            if (dialogueDelay > 0f) yield return new WaitForSeconds(dialogueDelay);
            yield return speech.Say(voiceDialogue, presentation, voiceLines);
            HideDialogue();
            Completed = true;
            IsRunning = false;
            PoDialogueGate.Leave();
            onFinished.Invoke();
        }

        private void HideDialogue()
        {
            speech.Stop();
            if (PoDialoguePlayer.Alive(voiceDialogue)) voiceDialogue.Hide();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            HideDialogue();
            if (IsRunning) PoDialogueGate.Leave();
            IsRunning = false;
        }
    }
}
