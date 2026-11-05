using System.Collections;
using StarterAssets;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    // Inside_Police_Station, step 3: after the duty officer has said the cameras are broken, Khem walks up to the "No Entry" sign on the
    // glass door of the monitor room and presses E. The sign's text is shown to read (same panel and font as the Lawyer Room document),
    // then his view turns and zooms in on one of the TVs behind the glass and he says what he sees. The view zooms back out, he says
    // his last line and can walk again. He never enters the monitor room.
    // The end: once all that has finished, looking at the object named "end" (the exit doors) from close by shows "[E] ..." and
    // pressing E there is the last interaction of the scene: the screen fades to black (the same black screen the arrival uses)
    // and onGoBack is invoked.
    // Every object this uses is saved in the scene; the sign position and the TV are ordinary objects you can move or swap.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9000)]
    public sealed class PoliceStationNoEntryTv : MonoBehaviour
    {
        [Header("Scene objects")]
        [Tooltip("Step 1. Nothing happens until it has finished.")]
        public PoliceStationOpening opening;
        [Tooltip("Step 2. The moment starts once the conversation with the duty officer has finished.")]
        public PoliceStationDutyOfficer duty;
        public FirstPersonController player;
        [Tooltip("The \"No Entry\" sign location object. Khem presses E while looking at it.")]
        public Transform signLocation;
        [Tooltip("The TV Khem's view zooms in on.")]
        public Transform tv;
        [Tooltip("The object named \"end\" (the exit doors). After the moment, Khem looks at it and presses E to go back to the scene.")]
        public Transform endLocation;

        [Header("Pressing E at the sign")]
        [Tooltip("Khem must be this close (metres) to the sign to be offered the E prompt.")]
        [Min(.5f)] public float interactDistance = 2.6f;
        [Tooltip("How close to the middle of the screen the sign must be (degrees from straight ahead).")]
        [Range(5f, 60f)] public float lookAngle = 20f;
        [Tooltip("The saved '[E] ...' label. Edit its text in the Hierarchy.")]
        public TMP_Text prompt;
        [Tooltip("The saved label with the text of the sign. Edit the wording in the Hierarchy.")]
        public TMP_Text signMessage;
        [Min(.5f)] public float messageSeconds = 1.5f;

        [Header("Khem's view")]
        [Tooltip("Higher = slower, softer turns.")]
        [Min(.05f)] public float lookSmoothSeconds = .6f;
        [Min(10f)] public float lookMaxDegreesPerSecond = 55f;
        [Tooltip("Field of view while looking at the TV (the normal view is about 60). Lower = closer.")]
        [Range(8f, 60f)] public float zoomFieldOfView = 24f;
        [Min(.05f)] public float zoomSmoothSeconds = .55f;
        [Min(5f)] public float zoomMaxDegreesPerSecond = 40f;

        [Header("Timing")]
        [Min(0f)] public float dialogueDelay = .4f;
        [Tooltip("Seconds the zoom stays on the TV after the last line.")]
        [Min(0f)] public float holdAfterLines = .8f;

        [Header("Dialogue (same box, font and typing sound as step 1)")]
        public DialogueCanvasView khemDialogue = new DialogueCanvasView();
        public FlowingDialogueSettings presentation = new FlowingDialogueSettings();
        [Tooltip("Said while the view is zoomed in on the TV. Saved text labels; edit the wording in the Hierarchy.")]
        public TMP_Text[] khemLines;

        [Header("After the zoom")]
        [Tooltip("Said in his normal view once the zoom has finished (the last line). Saved text labels; edit the wording in the Hierarchy.")]
        public TMP_Text[] khemLinesAfterZoom;

        [Header("The end (E at the exit doors, then the screen goes black)")]
        [Tooltip("The saved '[E] ...' label shown while Khem looks at the end object. Edit its text in the Hierarchy.")]
        public TMP_Text goBackPrompt;
        [Min(.5f)] public float endInteractDistance = 2.6f;
        [Tooltip("The black screen (the one the arrival uses). It fades in when E is pressed at the end and stays black.")]
        public CanvasGroup blackScreen;
        [Min(.05f)] public float fadeSeconds = 1.5f;
        [Tooltip("Invoked once the screen is black after E at the end: the place to start whatever follows (the next scene, ...).")]
        public UnityEvent onGoBack = new UnityEvent();

        [Tooltip("Invoked when the zoom moment has finished (after the last line).")]
        public UnityEvent onFinished = new UnityEvent();
        public bool Completed { get; private set; }
        public bool IsRunning { get; private set; }
        public bool WentBack { get; private set; }

        private FlowingDialogue flowing;
        private bool holdingControl, promptShown = true, messageShown = true, goBackShown = true;   // true so the first Set...(false) really hides the panel
        private Renderer[] signRenderers, endRenderers;
        // Khem's view
        private bool looking;
        private StarterAssetsInputs[] inputs;
        private bool[] inputLookEnabled;
        private Transform cameraTarget;
        private float yaw, pitch, targetYaw, targetPitch, yawVelocity, pitchVelocity;
        private float startYaw, startPitch;
        // Zoom
        private CinemachineCamera virtualCamera;
        private float baseFov, fov, targetFov, fovVelocity;

        private void Awake()
        {
            HideDialogue();
            SetPrompt(false);
            SetMessage(false);
            SetGoBack(false);
        }

        private void Start()
        {
            if (player == null || signLocation == null || tv == null || signMessage == null || !khemDialogue.IsValid || khemLines == null || khemLines.Length == 0)
            { Debug.LogError("Assign the player, the sign location, the TV, the sign text, the dialogue Canvas and lines.", this); enabled = false; return; }
            signRenderers = signLocation.GetComponentsInChildren<Renderer>(true);
            if (endLocation != null) endRenderers = endLocation.GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            if (holdingControl && player != null) player.MovementLocked = true;
            if (IsRunning) { SetPrompt(false); SetGoBack(false); return; }
            if (Completed) { SetPrompt(false); UpdateEnd(); return; }
            bool ready = CanRead();
            SetPrompt(ready);
            if (ready && Application.isFocused && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                StartCoroutine(Moment());
        }

        private bool CanRead()
        {
            if (Time.timeScale <= 0f || player == null || signLocation == null || tv == null || !signLocation.gameObject.activeInHierarchy) return false;
            if (opening != null && !opening.Completed) return false;
            if (duty != null && !duty.Completed) return false;
            if (!player.isActiveAndEnabled || player.MovementLocked) return false;
            return InteractionLook.IsLookingAt(Camera.main, signRenderers, lookAngle, interactDistance);
        }

        // The end: after the moment, looking at the "end" object from close by offers "[E] ..."; E is the last interaction.
        private void UpdateEnd()
        {
            bool ready = !WentBack && goBackPrompt != null && endLocation != null && CanGoBack();
            SetGoBack(ready);
            if (!ready || !Application.isFocused || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
            StartCoroutine(GoBack());
        }

        private IEnumerator GoBack()
        {
            WentBack = true;
            IsRunning = true;
            holdingControl = true;
            player.MovementLocked = true;
            SetGoBack(false);
            if (blackScreen != null)
            {
                blackScreen.blocksRaycasts = true;
                float start = blackScreen.alpha;
                float seconds = Mathf.Max(.05f, fadeSeconds);
                for (float time = 0f; time < seconds; time += Time.deltaTime)
                {
                    blackScreen.alpha = Mathf.Lerp(start, 1f, Mathf.SmoothStep(0f, 1f, time / seconds));
                    yield return null;
                }
                blackScreen.alpha = 1f;
            }
            onGoBack.Invoke();
        }

        private bool CanGoBack()
        {
            if (Time.timeScale <= 0f || player == null || !endLocation.gameObject.activeInHierarchy) return false;
            if (!player.isActiveAndEnabled || player.MovementLocked) return false;
            return InteractionLook.IsLookingAt(Camera.main, endRenderers, lookAngle, endInteractDistance);
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

        private void SetMessage(bool show)
        {
            if (messageShown == show) return;
            messageShown = show;
            SetPanel(signMessage, show);
        }

        private void SetGoBack(bool show)
        {
            if (goBackShown == show) return;
            goBackShown = show;
            SetPanel(goBackPrompt, show);
        }

        private void LateUpdate()
        {
            if (!IsRunning) return;
            if (looking && cameraTarget != null && player != null)
            {
                yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, lookSmoothSeconds, lookMaxDegreesPerSecond, Time.deltaTime);
                pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, lookSmoothSeconds, lookMaxDegreesPerSecond * .5f, Time.deltaTime);
                ApplyLook();
                ClearLookInput();
            }
            if (virtualCamera != null)
            {
                fov = Mathf.SmoothDamp(fov, targetFov, ref fovVelocity, zoomSmoothSeconds, zoomMaxDegreesPerSecond, Time.deltaTime);
                SetFov(fov);
            }
        }

        // ---- The moment --------------------------------------------------------------------------------------------------------------
        private IEnumerator Moment()
        {
            IsRunning = true;
            holdingControl = true;
            player.MovementLocked = true;
            SetPrompt(false);
            // 0. The text of the sign, to read.
            SetMessage(true);
            yield return new WaitForSeconds(messageSeconds);
            SetMessage(false);
            yield return new WaitForSeconds(.3f);
            BeginLook();
            // 1. Turn towards the TV.
            float waited = 0f;
            while (looking && waited < 3f && !LookArrived()) { waited += Time.deltaTime; yield return null; }
            // 2. Zoom in.
            targetFov = zoomFieldOfView;
            waited = 0f;
            while (virtualCamera != null && waited < 3f && Mathf.Abs(fov - targetFov) > .6f) { waited += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(dialogueDelay);
            // 3. Khem speaks.
            yield return SayAll(khemDialogue, khemLines);
            HideDialogue();
            yield return new WaitForSeconds(holdAfterLines);
            // 4. Zoom out and look back the way he was facing.
            targetFov = baseFov;
            targetYaw = startYaw;
            targetPitch = startPitch;
            waited = 0f;
            while (waited < 3.5f && (!LookArrived() || (virtualCamera != null && Mathf.Abs(fov - targetFov) > .6f)))
            { waited += Time.deltaTime; yield return null; }
            // 5. Back in his own view: the last line.
            if (khemLinesAfterZoom != null && khemLinesAfterZoom.Length > 0)
            {
                yield return new WaitForSeconds(dialogueDelay);
                yield return SayAll(khemDialogue, khemLinesAfterZoom);
                HideDialogue();
            }
            Finish();
            Completed = true;
            onFinished.Invoke();
        }

        private bool LookArrived() =>
            Mathf.Abs(Mathf.DeltaAngle(yaw, targetYaw)) <= 1.5f && Mathf.Abs(pitch - targetPitch) <= 1.5f;

        private void Finish()
        {
            EndLook();
            holdingControl = false;
            if (player != null) player.MovementLocked = false;
            IsRunning = false;
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

        // ---- Khem's view -------------------------------------------------------------------------------------------------------------
        private Vector3 AimPoint()
        {
            var renderers = tv.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return tv.position;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.center;
        }

        private void BeginLook()
        {
            virtualCamera = player.transform.root.GetComponentInChildren<CinemachineCamera>(true);
            if (virtualCamera == null) virtualCamera = FindFirstObjectByType<CinemachineCamera>();
            if (virtualCamera != null) { baseFov = fov = targetFov = virtualCamera.Lens.FieldOfView; fovVelocity = 0f; }
            if (player.CinemachineCameraTarget == null) return;
            cameraTarget = player.CinemachineCameraTarget.transform;
            inputs = player.transform.root.GetComponentsInChildren<StarterAssetsInputs>(true);
            inputLookEnabled = new bool[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) { inputLookEnabled[i] = inputs[i].cursorInputForLook; inputs[i].cursorInputForLook = false; }
            yaw = startYaw = player.transform.eulerAngles.y;
            pitch = startPitch = Mathf.DeltaAngle(0f, cameraTarget.localEulerAngles.x);
            Vector3 direction = AimPoint() - cameraTarget.position;
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

        private void SetFov(float value)
        {
            if (virtualCamera == null) return;
            var lens = virtualCamera.Lens;
            lens.FieldOfView = value;
            virtualCamera.Lens = lens;
        }

        private void ClearLookInput()
        {
            if (inputs != null) foreach (var input in inputs) if (input != null) input.look = Vector2.zero;
        }

        private void EndLook()
        {
            if (virtualCamera != null) { SetFov(baseFov); virtualCamera = null; }
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
            SetMessage(false);
            SetGoBack(false);
            EndLook();
            if (holdingControl && player != null) player.MovementLocked = false;
            holdingControl = false;
            IsRunning = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (signLocation != null)
            {
                Gizmos.color = new Color(1f, .35f, .2f, .9f);
                DrawCircle(signLocation.position, interactDistance);
                if (tv != null) Gizmos.DrawLine(signLocation.position, tv.position);
            }
            if (endLocation != null)
            {
                Gizmos.color = new Color(.3f, .9f, .4f, .9f);
                var renderers = endLocation.GetComponentsInChildren<Renderer>(true);
                Vector3 centre = endLocation.position;
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                    centre = bounds.center;
                }
                DrawCircle(centre, endInteractDistance);
            }
        }

        private static void DrawCircle(Vector3 position, float radius)
        {
            Vector3 centre = new Vector3(position.x, .3f, position.z);
            const int segments = 48;
            Vector3 previous = centre + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 next = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
