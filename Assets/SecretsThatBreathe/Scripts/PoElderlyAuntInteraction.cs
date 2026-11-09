using System;
using System.Collections;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    // One line of the conversation with the old woman: who says it, and the saved text label that holds the wording.
    [Serializable]
    public sealed class PoAuntLine
    {
        [Tooltip("On = Khem says it (his dialogue box). Off = the old woman says it (her box, named in the Speaker label).")]
        public bool khemSpeaks;
        [Tooltip("Saved text label. Edit the wording in the Hierarchy.")]
        public TMP_Text text;
    }

    // Po scene: Khem walks up to the old woman (ElderlyAunt), looks at her and presses E. They talk, one line after another, in the
    // same dialogue boxes, font and typing sound as the duty-officer conversation: Khem's box for his lines, her own box for hers.
    // Khem cannot walk while the lines are on screen. Every object this uses is saved in the scene; edit the wording of the saved
    // labels in the Hierarchy, and change who says a line with the 'Khem Speaks' tick-box.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9000)]
    public sealed class PoElderlyAuntInteraction : MonoBehaviour
    {
        [Header("Scene objects")]
        [Tooltip("Optional. The conversation is available once the station opening has finished.")]
        public PoliceStationOpening opening;
        public FirstPersonController player;
        [Tooltip("The ElderlyAunt in the scene. Move her and this follows her.")]
        public Transform aunt;

        [Header("Pressing E")]
        [Tooltip("Like the CCTV Interaction Zone: Khem is offered the conversation while he stands inside this box (a trigger box that is a child of the old woman, so it follows her). Move / rotate / resize it in the Scene view. Leave empty to use the distance and looking settings below instead.")]
        public BoxCollider interactionZone;
        [Tooltip("Only used when there is no Interaction Zone. Khem must be this close (metres, along the floor) to the old woman.")]
        [Min(.5f)] public float interactDistance = 2.4f;
        [Tooltip("Only used when there is no Interaction Zone. 1 = looking straight at her. Khem must look roughly towards her.")]
        [Range(0f, 1f)] public float facingDotMinimum = .7f;
        [Tooltip("Only used when there is no Interaction Zone. Height above her feet that Khem has to look towards (her chest).")]
        [Min(0f)] public float lookAtHeight = .85f;
        [Tooltip("The saved '[E] ...' label. Edit its text in the Hierarchy.")]
        public TMP_Text prompt;
        [Tooltip("Off = Khem can only talk to her once.")]
        public bool canRepeat;

        [Header("Dialogue (same boxes, font and typing sound as the station dialogue)")]
        public DialogueCanvasView khemDialogue = new DialogueCanvasView();
        public DialogueCanvasView auntDialogue = new DialogueCanvasView();
        public FlowingDialogueSettings presentation = new FlowingDialogueSettings();
        [Tooltip("The conversation, in order.")]
        public PoAuntLine[] lines;
        [Min(0f)] public float dialogueDelay = .2f;

        public UnityEvent onFinished = new UnityEvent();
        public bool Completed { get; private set; }
        public bool IsRunning { get; private set; }

        private readonly PoDialoguePlayer speech = new PoDialoguePlayer();
        private bool holdingControl, promptShown = true;   // true so the first SetPrompt(false) really hides the label

        private void Awake()
        {
            HideDialogue();
            SetPrompt(false);
        }

        private void Start()
        {
            if (player == null || aunt == null || !khemDialogue.IsValid || !auntDialogue.IsValid || lines == null || lines.Length == 0)
            { Debug.LogError("Assign the old woman, player, both dialogue Canvases and the lines.", this); enabled = false; }
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
            if (Time.timeScale <= 0f || player == null || aunt == null || !aunt.gameObject.activeInHierarchy) return false;
            if (PoDialogueGate.Busy) return false;
            if (opening != null && !opening.Completed) return false;
            if (!player.isActiveAndEnabled || player.MovementLocked) return false;
            // Same test as the CCTV Interaction Zone: Khem's body (0.9 m above his feet) has to be inside the box.
            if (interactionZone != null)
                return InteractionLook.IsPointInBox(player.transform.position + Vector3.up * .9f, interactionZone);
            Vector3 flat = aunt.position - player.transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > interactDistance * interactDistance) return false;
            var view = Camera.main;
            if (view == null) return false;
            Vector3 toBody = aunt.position + Vector3.up * lookAtHeight - view.transform.position;
            if (toBody.sqrMagnitude < .0001f) return true;
            return Vector3.Dot(view.transform.forward, toBody.normalized) >= facingDotMinimum;
        }

        private void OnDrawGizmosSelected()
        {
            if (interactionZone != null)
            {
                Gizmos.color = Color.cyan;
                Matrix4x4 savedMatrix = Gizmos.matrix;
                Gizmos.matrix = interactionZone.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(interactionZone.center, interactionZone.size);
                Gizmos.matrix = savedMatrix;
                return;
            }
            if (aunt == null) return;
            Gizmos.color = Color.yellow;
            Vector3 origin = aunt.position + Vector3.up * .05f;
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
            foreach (var line in lines)
            {
                if (line == null || line.text == null) continue;
                // Each line goes into its speaker's box; the previous box is hidden as the next one starts typing.
                yield return speech.Say(line.khemSpeaks ? khemDialogue : auntDialogue, presentation, new[] { line.text });
            }
            HideDialogue();
            Finish();
            Completed = true;
            onFinished.Invoke();
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
            if (PoDialoguePlayer.Alive(khemDialogue)) khemDialogue.Hide();
            if (PoDialoguePlayer.Alive(auntDialogue)) auntDialogue.Hide();
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
