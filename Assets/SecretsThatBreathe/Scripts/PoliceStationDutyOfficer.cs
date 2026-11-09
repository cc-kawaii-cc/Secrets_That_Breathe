using System.Collections;
using Unity.Cinemachine;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    // Inside_Police_Station, step 2: once the man in sunglasses has left, Khem can walk up to the duty officer at the right-hand
    // counter, look at him and press E. Khem asks to see the CCTV files, the officer says the cameras are broken, and Khem
    // thinks one line to himself. Khem cannot walk while they talk. Every object this uses is saved in the scene.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9000)]
    public sealed class PoliceStationDutyOfficer : MonoBehaviour
    {
        [Header("Scene objects")]
        [Tooltip("Step 1. The conversation is available once it has finished.")]
        public PoliceStationOpening opening;
        public FirstPersonController player;
        public Transform officer;

        [Header("Starting the conversation")]
        [Tooltip("Khem must be this close (metres, along the floor) to the officer.")]
        [Min(.5f)] public float interactDistance = 3f;
        [Tooltip("Total angle of the interaction area in front of the officer.")]
        [Range(1f, 360f)] public float interactionAngle = 360f;
        [Tooltip("1 = looking straight at the officer. Khem must look roughly towards him to be offered the conversation.")]
        [Range(0f, 1f)] public float facingDotMinimum = .6f;
        [Tooltip("The saved '[E] ...' label. Edit its text in the Hierarchy.")]
        public TMP_Text prompt;

        [Header("Khem's view during the conversation")]
        [Min(.05f)] public float lookSmoothSeconds = .35f;
        [Min(10f)] public float lookMaxDegreesPerSecond = 160f;
        [Tooltip("Used only if the officer has no Head bone.")]
        [Min(.5f)] public float lookAtHeight = 1.6f;
        [Tooltip("The officer slowly turns to face Khem while they talk.")]
        [Min(0f)] public float officerTurnSpeed = 6f;
        [Tooltip("Degrees to subtract from the normal camera field of view during dialogue.")]
        [Range(0f, 30f)] public float conversationZoom = 0f;
        [Min(.05f)] public float zoomSeconds = .45f;

        [Header("Dialogue (same boxes, font and typing sound as MainScene_2_Evening)")]
        public DialogueCanvasView khemDialogue = new DialogueCanvasView();
        public DialogueCanvasView officerDialogue = new DialogueCanvasView();
        public FlowingDialogueSettings presentation = new FlowingDialogueSettings();
        [Tooltip("Saved text labels. Edit the wording in the Hierarchy.")]
        public TMP_Text[] khemAsksLines, officerRepliesLines, khemThoughtLines;
        [Tooltip("Show thought labels at their authored positions without typing.")]
        public bool showThoughtsInstantly;
        [Min(0f)] public float thoughtHoldSeconds = 3f;
        [Min(0f)] public float dialogueDelay = .3f;

        public UnityEvent onFinished = new UnityEvent();
        public bool Completed { get; private set; }
        public bool IsRunning { get; private set; }

        private FlowingDialogue flowing;
        private GameObject visibleThought;
        private FirstPersonFlashlight suspendedFlashlight;
        private bool holdingControl, promptShown = true;   // true so the first SetPrompt(false) really hides the label
        private Quaternion officerStartRotation;
        // Khem's view
        private bool looking;
        private StarterAssetsInputs[] inputs;
        private bool[] inputLookEnabled;
        private Transform cameraTarget, officerHead;
        private CinemachineCamera zoomCamera;
        private float normalFov;
        private float yaw, pitch, targetYaw, targetPitch, yawVelocity, pitchVelocity;

        private void Awake()
        {
            HideDialogue();
            SetPrompt(false);
        }

        private void Start()
        {
            if (player == null || officer == null || !khemDialogue.IsValid || !officerDialogue.IsValid
                || khemAsksLines == null || khemAsksLines.Length == 0 || officerRepliesLines == null || officerRepliesLines.Length == 0)
            { Debug.LogError("Assign the duty officer, player, dialogue Canvases and lines.", this); enabled = false; return; }
            officerStartRotation = officer.rotation;
            officerHead = FindDeep(officer, "Head");
            if (officerHead == null)
                foreach (var animator in officer.GetComponentsInChildren<Animator>())
                    if (animator.isActiveAndEnabled && animator.isHuman)
                    {
                        officerHead = animator.GetBoneTransform(HumanBodyBones.Head);
                        if (officerHead != null) break;
                    }
            // The officer stands still while talking, so give him the same breathing layer as the Evening police.
            CharacterBreathing.Attach(officer);
        }

        private void Update()
        {
            if (holdingControl && player != null) player.MovementLocked = true;
            if (Completed || IsRunning) { SetPrompt(false); return; }
            bool ready = CanTalk();
            SetPrompt(ready);
            if (ready && Application.isFocused && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                StartCoroutine(Conversation());
        }

        private bool CanTalk()
        {
            if (Time.timeScale <= 0f || player == null || officer == null || !officer.gameObject.activeInHierarchy) return false;
            if (opening != null && !opening.Completed) return false;
            if (!player.isActiveAndEnabled || player.MovementLocked) return false;
            Vector3 toOfficer = officer.position - player.transform.position;
            toOfficer.y = 0f;
            if (toOfficer.sqrMagnitude > interactDistance * interactDistance || toOfficer.sqrMagnitude < .0001f) return false;
            if (interactionAngle < 360f &&
                Vector3.Angle(OfficerForward(), -toOfficer) > interactionAngle * .5f) return false;
            var view = Camera.main;
            if (view == null) return false;
            Vector3 forward = view.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < .0001f) return false;
            return Vector3.Dot(forward.normalized, toOfficer.normalized) >= facingDotMinimum;
        }

        private Vector3 OfficerForward()
        {
            return Vector3.ProjectOnPlane(officer.forward, Vector3.up).normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (officer == null) return;
            Gizmos.color = Color.yellow;
            Vector3 origin = officer.position + Vector3.up * .05f;
            Vector3 forward = OfficerForward();
            Vector3 first = origin + Quaternion.AngleAxis(-interactionAngle * .5f, Vector3.up) * forward * interactDistance;
            Vector3 previous = first;
            if (interactionAngle < 360f) Gizmos.DrawLine(origin, first);
            for (int i = 1; i <= 32; i++)
            {
                float angle = Mathf.Lerp(-interactionAngle * .5f, interactionAngle * .5f, i / 32f);
                Vector3 point = origin + Quaternion.AngleAxis(angle, Vector3.up) * forward * interactDistance;
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
            if (interactionAngle < 360f) Gizmos.DrawLine(origin, previous);
        }

        private void SetPrompt(bool show)
        {
            if (prompt == null || promptShown == show) return;
            promptShown = show;
            var root = prompt.transform.parent != null ? prompt.transform.parent.gameObject : prompt.gameObject;
            prompt.gameObject.SetActive(true);
            root.SetActive(show);
        }

        private void LateUpdate()
        {
            if (!IsRunning) return;
            if (officer != null && player != null && officerTurnSpeed > 0f)
            {
                Vector3 direction = player.transform.position - officer.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > .001f)
                    officer.rotation = Quaternion.Slerp(officer.rotation, Quaternion.LookRotation(direction),
                        1f - Mathf.Exp(-officerTurnSpeed * Time.deltaTime));
            }
            if (!looking || cameraTarget == null || player == null) return;
            yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, lookSmoothSeconds, lookMaxDegreesPerSecond, Time.deltaTime);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, lookSmoothSeconds, lookMaxDegreesPerSecond * .5f, Time.deltaTime);
            ApplyLook();
            ClearLookInput();
        }

        // ---- The conversation --------------------------------------------------------------------------------------------------------
        private IEnumerator Conversation()
        {
            IsRunning = true;
            SetPrompt(false);
            holdingControl = true;
            player.MovementLocked = true;
            var flashlight = player.GetComponent<FirstPersonFlashlight>();
            if (flashlight != null && flashlight.enabled)
            {
                suspendedFlashlight = flashlight;
                flashlight.enabled = false; // OnDisable switches off the lamp and puts the phone away.
            }
            BeginLook();
            if (conversationZoom > 0f)
            {
                var view = Camera.main;
                var brain = view != null ? view.GetComponent<CinemachineBrain>() : null;
                zoomCamera = brain != null ? brain.ActiveVirtualCamera as CinemachineCamera : null;
                if (zoomCamera == null) zoomCamera = player.transform.root.GetComponentInChildren<CinemachineCamera>();
                if (zoomCamera != null)
                {
                    normalFov = zoomCamera.Lens.FieldOfView;
                    yield return ZoomTo(Mathf.Max(15f, normalFov - conversationZoom));
                }
            }
            float waited = 0f;
            while (looking && waited < 1.5f
                && (Mathf.Abs(Mathf.DeltaAngle(yaw, targetYaw)) > 1.5f || Mathf.Abs(pitch - targetPitch) > 1.5f))
            { waited += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(dialogueDelay);
            yield return SayAll(khemDialogue, khemAsksLines);
            yield return SayAll(officerDialogue, officerRepliesLines);
            EndLook();
            RestoreFlashlight();
            if (holdingControl && player != null) player.MovementLocked = false;
            holdingControl = false;
            Coroutine zoomOut = zoomCamera != null ? StartCoroutine(ZoomTo(normalFov)) : null;
            if (showThoughtsInstantly) yield return ShowThoughts();
            else yield return SayAll(khemDialogue, khemThoughtLines);
            HideDialogue();
            if (zoomOut != null) yield return zoomOut;
            Finish();
            Completed = true;
            onFinished.Invoke();
        }

        private void Finish()
        {
            RestoreZoom();
            EndLook();
            RestoreFlashlight();
            if (holdingControl && player != null) player.MovementLocked = false;
            holdingControl = false;
            IsRunning = false;
        }

        private IEnumerator ZoomTo(float targetFov)
        {
            float startFov = zoomCamera.Lens.FieldOfView;
            for (float elapsed = 0f; elapsed < zoomSeconds && zoomCamera != null; elapsed += Time.deltaTime)
            {
                SetFov(Mathf.SmoothStep(startFov, targetFov, elapsed / zoomSeconds));
                yield return null;
            }
            SetFov(targetFov);
        }

        private void SetFov(float value)
        {
            if (zoomCamera == null) return;
            var lens = zoomCamera.Lens;
            lens.FieldOfView = value;
            zoomCamera.Lens = lens;
        }

        private void RestoreZoom()
        {
            if (zoomCamera != null) SetFov(normalFov);
            zoomCamera = null;
        }

        private void RestoreFlashlight()
        {
            if (suspendedFlashlight != null) suspendedFlashlight.enabled = true;
            suspendedFlashlight = null;
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

        private IEnumerator ShowThoughts()
        {
            HideDialogue();
            if (khemThoughtLines == null) yield break;
            foreach (var line in khemThoughtLines)
            {
                if (line == null) continue;
                visibleThought = line.transform.parent.gameObject;
                line.maxVisibleCharacters = int.MaxValue;
                visibleThought.SetActive(true);
                yield return new WaitForSeconds(thoughtHoldSeconds);
                visibleThought.SetActive(false);
                visibleThought = null;
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

        // ---- Khem's view -------------------------------------------------------------------------------------------------------------
        private void BeginLook()
        {
            if (player.CinemachineCameraTarget == null) return;
            cameraTarget = player.CinemachineCameraTarget.transform;
            inputs = player.transform.root.GetComponentsInChildren<StarterAssetsInputs>(true);
            inputLookEnabled = new bool[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) { inputLookEnabled[i] = inputs[i].cursorInputForLook; inputs[i].cursorInputForLook = false; }
            yaw = player.transform.eulerAngles.y;
            pitch = Mathf.DeltaAngle(0f, cameraTarget.localEulerAngles.x);
            Vector3 point = officerHead != null ? officerHead.position : officer.position + Vector3.up * lookAtHeight;
            Vector3 direction = point - cameraTarget.position;
            targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(-Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg, -40f, 40f);
            yawVelocity = pitchVelocity = 0f;
            looking = true;
        }

        private void ApplyLook()
        {
            player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void ClearLookInput()
        {
            if (inputs != null) foreach (var input in inputs) if (input != null) input.look = Vector2.zero;
        }

        private void EndLook()
        {
            if (!looking) return;
            looking = false;
            if (player != null && cameraTarget != null)
            {
                ApplyLook();
                player.ResetAfterTeleport(pitch);   // the controller continues from the view Khem now has
            }
            if (inputs != null)
                for (int i = 0; i < inputs.Length; i++)
                    if (inputs[i] != null) { inputs[i].look = Vector2.zero; inputs[i].cursorInputForLook = inputLookEnabled[i]; }
            inputs = null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }

        // When Play Mode stops, Unity destroys the Canvas objects before this component's OnDisable runs.
        private static bool Alive(DialogueCanvasView view) => view != null && view.IsValid && view.sentence != null;

        private void HideDialogue()
        {
            if (visibleThought != null) visibleThought.SetActive(false);
            visibleThought = null;
            if (Alive(khemDialogue) && Alive(officerDialogue)) flowing?.Hide();
            flowing = null;
            if (Alive(khemDialogue)) khemDialogue.Hide();
            if (Alive(officerDialogue)) officerDialogue.Hide();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            HideDialogue();
            RestoreZoom();
            EndLook();
            RestoreFlashlight();
            if (holdingControl && player != null) player.MovementLocked = false;
            holdingControl = false;
            IsRunning = false;
            if (officer != null && officerStartRotation != default) officer.rotation = officerStartRotation;
            SetPrompt(false);
        }
    }
}
