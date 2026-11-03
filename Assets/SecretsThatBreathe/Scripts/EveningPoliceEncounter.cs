using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using StarterAssets;

namespace SecretsThatBreathe
{
    [DefaultExecutionOrder(30000)]
    public sealed class EveningPoliceEncounter : MonoBehaviour
    {
        [Header("Existing scene actors and range")]
        public PoliceJumpscareRange range;
        public Transform policeRoot, sunglassesMan;
        public PorschePassengerRide ride;
        public PoliceEncounterGestures gestures;
        public AudioSource scareAudio;
        [Header("Police animation while talking to Khem")]
        [Tooltip("Idle Talk controller used by Uncle Pong. Original conversation controller is restored afterward.")]
        public RuntimeAnimatorController policePlayerTalkController;
        [Header("Lawyer Room camera timing")]
        [Min(.05f)] public float turnSeconds = .18f;
        [Range(0f, 1f)] public float zoomStartTurnFraction = .65f;
        [Min(.05f)] public float zoomSeconds = .6f;
        [Range(10f, 90f)] public float scareFieldOfView = 30f;
        [Min(0f)] public float dialogueStartDelay = .5f;
        [Min(.05f)] public float focusSeconds = .35f;
        [Range(10f, 90f)] public float sunglassesFieldOfView = 35f;
        [Min(.05f)] public float cameraReturnSeconds = .6f;
        [Header("Edit sentence text/font/size/colour on Lines and NarrationLines")]
        public DialogueCanvasView policeDialogue = new DialogueCanvasView();
        public DialogueCanvasView khemDialogue = new DialogueCanvasView();
        public DialogueCanvasView narrationDialogue = new DialogueCanvasView();
        public FlowingDialogueSettings presentation = new FlowingDialogueSettings();
        public TMP_Text[] introduction, lieReplies, lawyerReplies, refusedReplies, evidenceReplies, finalPoliceReplies;
        public TMP_Text radioNarration, sunglassesReply, threatNarration;
        public AuntMoneyChoiceUI firstChoices, secondChoices;
        [Min(0f)] public float narrationHoldSeconds = 3f;
        [Header("Return to the car (no destination scene assigned yet)")]
        public TMP_Text returnObjective, boardingPrompt;
        [Min(0f)] public float returnObjectiveDelay = 1f;
        [Min(.1f)] public float boardingDistance = 2f;
        [Min(0f)] public float pushDistance = .4f;
        [Min(.05f)] public float pushSeconds = .3f;
        public UnityEvent onReadyToChangeScene = new UnityEvent();
        public bool ReadyToChangeScene { get; private set; }
        public bool Completed { get; private set; }
        public bool IsRunning { get; private set; }
        private bool triggered, boarding, cameraOwned, changingCamera, restoringCamera;
        private FirstPersonController player;
        private Camera view;
        private Vector3 cameraPosition;
        private Quaternion originalCameraRotation, policeRotation, manRotation;
        private float originalFov;
        private Animator[] manAnimators;
        private float[] manSpeeds;
        private Animator policeAnimator;
        private RuntimeAnimatorController originalPoliceController;
        private float originalPoliceSpeed, originalPoliceAnimationTime;
        private int originalPoliceAnimationState;
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private FlowingDialogue flowing;
        private int selected = -1;
        private AuntMoneyChoiceUI activeChoices;
        private Vector2 choiceCursorPosition;
        private bool hasChoiceCursorPosition;
        private Coroutine pushRoutine;
        private static EveningPoliceEncounter current;
        public static bool BlocksFlashlightToggle(UnityEngine.SceneManagement.Scene scene)
            => current != null && current.gameObject.scene == scene &&
                (current.IsRunning || current.boarding || (current.Completed && current.ride != null && current.player != null && current.IsNearCar()));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCurrent() => current = null;

        // Checkpoints are repeatable, including during choices or a boarding fade.
        public static void ResetForTesting(UnityEngine.SceneManagement.Scene scene)
        {
            if (current == null || current.gameObject.scene != scene) return;
            var encounter = current;
            encounter.StopAllCoroutines();
            if (encounter.scareAudio != null) encounter.scareAudio.Stop();
            encounter.gestures?.SetGesture(PoliceEncounterGestures.Gesture.Talk);
            encounter.HideDialogue();
            encounter.Release();
            encounter.triggered = encounter.boarding = encounter.restoringCamera = false;
            encounter.IsRunning = encounter.Completed = encounter.ReadyToChangeScene = false;
            encounter.selected = -1; encounter.activeChoices = null; encounter.pushRoutine = null;
            Panel(encounter.returnObjective, false); Panel(encounter.boardingPrompt, false);
        }

        private void Awake()
        {
            current = this;
            HideDialogue(); Panel(returnObjective, false); Panel(boardingPrompt, false);
            foreach (var choices in new[] { firstChoices, secondChoices }) if (choices != null)
            {
                choices.acceptButton.onClick.AddListener(ChooseFirst);
                choices.refuseButton.onClick.AddListener(ChooseSecond);
            }
        }

        private void Start()
        {
            if (range == null || policeRoot == null || sunglassesMan == null || firstChoices == null || secondChoices == null
                || !policeDialogue.IsValid || !khemDialogue.IsValid || !narrationDialogue.IsValid)
            { Debug.LogError("Assign the saved PoliceEncounter actors, range and Canvas references.", this); enabled = false; return; }
            // Both officers stand still while Khem talks to them, so give them a breathing layer.
            AttachBreathing();
        }

        private bool breathingAttached;
        // Also called from LateUpdate: after Unity recompiles during Play Mode, Start is not run again.
        private void AttachBreathing()
        {
            breathingAttached = true;
            CharacterBreathing.Attach(policeRoot);
            CharacterBreathing.Attach(sunglassesMan);
        }

        private void ChooseFirst() { if (IsRunning && activeChoices != null && selected < 0 && Time.timeScale > 0f) selected = 0; }
        private void ChooseSecond() { if (IsRunning && activeChoices != null && selected < 0 && Time.timeScale > 0f) selected = 1; }

        private void LateUpdate()
        {
            if (!breathingAttached && policeRoot != null && sunglassesMan != null) AttachBreathing();
            if (!triggered && Time.timeScale > 0f && range != null && range.IsPlayerInRange)
            {
                player = range.player; ride = range.arrivalRide != null ? range.arrivalRide : ride;
                view = ride != null && ride.playerCamera != null ? ride.playerCamera : Camera.main;
                if (player != null && view != null) { triggered = true; StartCoroutine(Encounter()); }
            }
            if (cameraOwned)
            {
                if (!restoringCamera) { FacePlayer(policeRoot); FacePlayer(sunglassesMan); }
                if (!changingCamera) view.transform.SetPositionAndRotation(cameraPosition, FaceRotation(focusSubject != null ? focusSubject : policeRoot));
            }
            bool near = Completed && !boarding && ride != null && ride.HasExited && player != null
                && Time.timeScale > 0f && IsNearCar();
            Panel(boardingPrompt, near);
            if (near && Application.isFocused && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                StartCoroutine(Board());
        }

        private bool IsNearCar()
        {
            Vector3 position = ride.exitPoint != null ? ride.exitPoint.position : ride.transform.TransformPoint(ride.exitLocalPosition);
            Vector3 delta = player.transform.position - position;
            return Mathf.Abs(delta.y) < 2f && new Vector2(delta.x, delta.z).sqrMagnitude <= boardingDistance * boardingDistance;
        }

        private void Suspend(Behaviour control)
        {
            if (control == null || !control.enabled || suspended.Contains(control)) return;
            suspended.Add(control); control.enabled = false;
        }

        private void Capture()
        {
            cameraPosition = view.transform.position; originalCameraRotation = view.transform.rotation; originalFov = view.fieldOfView;
            policeRotation = policeRoot.rotation; manRotation = sunglassesMan.rotation;
            if (policePlayerTalkController != null)
                foreach (var animator in policeRoot.GetComponentsInChildren<Animator>())
                    if (animator.isActiveAndEnabled)
                    {
                        policeAnimator = animator;
                        originalPoliceController = animator.runtimeAnimatorController;
                        originalPoliceSpeed = animator.speed;
                        var state = animator.GetCurrentAnimatorStateInfo(0);
                        originalPoliceAnimationState = state.fullPathHash;
                        originalPoliceAnimationTime = state.normalizedTime;
                        animator.runtimeAnimatorController = policePlayerTalkController;
                        animator.speed = 1f;
                        animator.Play(0, 0, 0f);
                        animator.Update(0f);
                        break;
                    }
            var root = ride != null ? ride.playerRoot : player.transform.root;
            foreach (var flashlight in root.GetComponentsInChildren<FirstPersonFlashlight>(true)) flashlight.ForceOff();
            foreach (var component in root.GetComponentsInChildren<Behaviour>(true))
                switch (component.GetType().Name)
                {
                    case "FirstPersonController": case "PlayerMovement": case "MouseLook": case "PlayerInteractor":
                    case "FirstPersonFlashlight": case "PhoneFlashlightView": Suspend(component); break;
                }
            foreach (var component in view.GetComponents<Behaviour>())
                if (component.GetType().Name == "CinemachineBrain") Suspend(component);
            manAnimators = sunglassesMan.GetComponentsInChildren<Animator>(true); manSpeeds = new float[manAnimators.Length];
            for (int i = 0; i < manAnimators.Length; i++) { manSpeeds[i] = manAnimators[i].speed; manAnimators[i].speed = 0f; }
            cameraOwned = true;
        }

        private IEnumerator Encounter()
        {
            IsRunning = true; Capture();
            changingCamera = true;
            float zoomAt = turnSeconds * zoomStartTurnFraction;
            float total = Mathf.Max(turnSeconds, zoomAt + zoomSeconds);
            bool soundStarted = false;
            for (float elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
            {
                if (!soundStarted && elapsed >= zoomAt)
                { if (scareAudio != null) scareAudio.Play(); soundStarted = true; }
                view.transform.SetPositionAndRotation(cameraPosition, Quaternion.Slerp(originalCameraRotation,
                    FaceRotation(policeRoot), Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(.05f, turnSeconds)))));
                view.fieldOfView = Mathf.Lerp(originalFov, scareFieldOfView,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - zoomAt) / Mathf.Max(.05f, zoomSeconds))));
                yield return null;
            }
            if (!soundStarted && scareAudio != null) scareAudio.Play();
            view.fieldOfView = scareFieldOfView; changingCamera = false;
            yield return new WaitForSeconds(dialogueStartDelay);
            yield return SayAll(policeDialogue, introduction);
            yield return Pick(firstChoices); int first = selected;
            yield return SayAll(khemDialogue, first == 0 ? lieReplies : lawyerReplies);
            if (first == 0)
            {
                gestures?.SetGesture(PoliceEncounterGestures.Gesture.Radio);
                yield return Narrate(radioNarration, narrationHoldSeconds + (gestures != null ? gestures.radioRaiseSeconds : 0f));
                gestures?.SetGesture(PoliceEncounterGestures.Gesture.Talk);
                if (gestures != null) yield return new WaitForSeconds(gestures.radioLowerSeconds);
            }
            else yield return SayAll(policeDialogue, refusedReplies);
            yield return Pick(secondChoices); int second = selected;
            if (second == 0)
            {
                yield return Focus(sunglassesMan, sunglassesFieldOfView);
                yield return Say(khemDialogue, sunglassesReply);
                yield return Focus(policeRoot, scareFieldOfView);
                gestures?.SetGesture(PoliceEncounterGestures.Gesture.GunAndPush);
                pushRoutine = StartCoroutine(PushBack());
                yield return Narrate(threatNarration, gestures != null
                    ? Mathf.Max(narrationHoldSeconds, gestures.GunAndPushDuration) : narrationHoldSeconds);
                if (pushRoutine != null) { yield return pushRoutine; pushRoutine = null; }
                gestures?.SetGesture(PoliceEncounterGestures.Gesture.Talk);
                if (gestures != null) yield return new WaitForSeconds(gestures.gunLowerSeconds);
            }
            else
            {
                yield return SayAll(khemDialogue, evidenceReplies);
                yield return SayAll(policeDialogue, finalPoliceReplies);
            }
            HideDialogue();
            yield return ReturnToWalking();
            IsRunning = false;
            yield return new WaitForSeconds(returnObjectiveDelay);
            Completed = true; Panel(returnObjective, true);
        }

        private IEnumerator SayAll(DialogueCanvasView canvas, TMP_Text[] lines)
        {
            if (lines != null) foreach (var line in lines) if (line != null) yield return Say(canvas, line);
        }

        private IEnumerator Say(DialogueCanvasView canvas, TMP_Text line, bool removeLieMarker = false)
        {
            flowing?.Hide(); ApplySentenceStyle(canvas, line); flowing = new FlowingDialogue(canvas, presentation);
            string text = line != null ? line.text : "";
            if (removeLieMarker) text = System.Text.RegularExpressions.Regex.Replace(text, @"^<color=[^>]+>\(โกหก\)</color>\s*", "");
            yield return flowing.Play(text);
        }

        private IEnumerator Narrate(TMP_Text line, float holdSeconds = -1f)
        {
            flowing?.Hide(); ApplySentenceStyle(narrationDialogue, line); flowing = new FlowingDialogue(narrationDialogue, presentation);
            yield return flowing.ShowInstant(line != null ? line.text : "", holdSeconds >= 0f ? holdSeconds : narrationHoldSeconds);
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

        private IEnumerator Pick(AuntMoneyChoiceUI choices)
        {
            HideDialogue(); activeChoices = choices;
            selected = -1; choices.ShowChoices();
            if (hasChoiceCursorPosition && Mouse.current != null) Mouse.current.WarpCursorPosition(choiceCursorPosition);
            while (selected < 0) yield return null;
            if (Mouse.current != null) { choiceCursorPosition = Mouse.current.position.ReadValue(); hasChoiceCursorPosition = true; }
            choices.HideChoices();
            activeChoices = null;
        }

        private IEnumerator Focus(Transform target, float fov)
        {
            changingCamera = true; Quaternion from = view.transform.rotation; float fromFov = view.fieldOfView;
            for (float t = 0f; t < focusSeconds; t += Time.deltaTime)
            {
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Mathf.Max(.05f, focusSeconds)));
                view.transform.SetPositionAndRotation(cameraPosition, Quaternion.Slerp(from, FaceRotation(target), a));
                view.fieldOfView = Mathf.Lerp(fromFov, fov, a); yield return null;
            }
            view.transform.SetPositionAndRotation(cameraPosition, FaceRotation(target)); view.fieldOfView = fov;
            // Keep the chosen subject while its line is typed.
            focusSubject = target; changingCamera = false;
        }
        private Transform focusSubject;
        private Quaternion FaceRotation(Transform actor)
        {
            Transform head = null;
            foreach (var item in actor.GetComponentsInChildren<Transform>()) if (item.name == "Head") { head = item; break; }
            Vector3 target = head != null ? head.position : actor.position + Vector3.up * 1.65f;
            return Quaternion.LookRotation(target - cameraPosition, Vector3.up);
        }
        private void FacePlayer(Transform actor)
        {
            Vector3 direction = player.transform.position - actor.position; direction.y = 0f;
            if (direction.sqrMagnitude > .001f) actor.rotation = Quaternion.Slerp(actor.rotation,
                Quaternion.LookRotation(direction), 1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        private IEnumerator PushBack()
        {
            yield return new WaitForSeconds(gestures != null ? gestures.PushContactDelay : .25f);
            var controller = player.GetComponent<CharacterController>();
            Vector3 away = player.transform.position - policeRoot.position; away.y = 0f; away.Normalize();
            for (float t = 0f; t < pushSeconds;)
            {
                float dt = Mathf.Min(Time.deltaTime, pushSeconds - t); t += dt;
                Vector3 before = player.transform.position;
                if (controller != null && controller.enabled) controller.Move(away * (pushDistance * dt / Mathf.Max(.05f, pushSeconds)));
                cameraPosition += player.transform.position - before; yield return null;
            }
        }

        private IEnumerator ReturnToWalking()
        {
            restoringCamera = true;
            changingCamera = true; Quaternion from = view.transform.rotation; float fov = view.fieldOfView;
            for (float t = 0f; t < cameraReturnSeconds; t += Time.deltaTime)
            {
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Mathf.Max(.05f, cameraReturnSeconds)));
                view.transform.SetPositionAndRotation(cameraPosition, Quaternion.Slerp(from, originalCameraRotation, a));
                view.fieldOfView = Mathf.Lerp(fov, originalFov, a);
                policeRoot.rotation = Quaternion.Slerp(policeRoot.rotation, policeRotation, a);
                sunglassesMan.rotation = Quaternion.Slerp(sunglassesMan.rotation, manRotation, a); yield return null;
            }
            Release();
        }

        private void Release()
        {
            if (!cameraOwned) return;
            cameraOwned = false; changingCamera = false; focusSubject = null;
            if (policeRoot != null) policeRoot.rotation = policeRotation;
            if (sunglassesMan != null) sunglassesMan.rotation = manRotation;
            if (policeAnimator != null)
            {
                policeAnimator.runtimeAnimatorController = originalPoliceController;
                policeAnimator.speed = originalPoliceSpeed;
                if (originalPoliceController != null && originalPoliceAnimationState != 0)
                    policeAnimator.Play(originalPoliceAnimationState, 0, originalPoliceAnimationTime);
                policeAnimator.Update(0f);
                policeAnimator = null; originalPoliceController = null;
            }
            if (manAnimators != null) for (int i = 0; i < manAnimators.Length; i++) if (manAnimators[i] != null) manAnimators[i].speed = manSpeeds[i];
            if (view != null) { view.fieldOfView = originalFov; view.transform.SetPositionAndRotation(cameraPosition, originalCameraRotation); }
            if (player != null)
                foreach (var input in player.GetComponentsInChildren<StarterAssetsInputs>(true))
                { input.look = input.move = Vector2.zero; input.jump = input.sprint = false; }
            foreach (var control in suspended) if (control != null) control.enabled = true;
            suspended.Clear();
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }

        private IEnumerator Board()
        {
            boarding = true; Panel(returnObjective, false); Panel(boardingPrompt, false);
            yield return ride.BoardForSceneTransition();
            if (ride.HasExited) { boarding = false; Panel(returnObjective, true); yield break; }
            ReadyToChangeScene = true; onReadyToChangeScene.Invoke();
        }

        private static void Panel(TMP_Text label, bool show)
        {
            if (label == null) return;
            label.gameObject.SetActive(true); label.maxVisibleCharacters = int.MaxValue;
            label.transform.parent.gameObject.SetActive(show);
        }
        private void HideDialogue()
        { flowing?.Hide(); policeDialogue.Hide(); khemDialogue.Hide(); narrationDialogue.Hide(); firstChoices?.HideChoices(); secondChoices?.HideChoices(); }
        private void OnDisable()
        { StopAllCoroutines(); if (scareAudio != null) scareAudio.Stop(); gestures?.SetGesture(PoliceEncounterGestures.Gesture.Talk); HideDialogue(); Release(); }
        private void OnDestroy()
        { if (current == this) current = null; foreach (var choices in new[] { firstChoices, secondChoices }) if (choices != null) { choices.acceptButton.onClick.RemoveListener(ChooseFirst); choices.refuseButton.onClick.RemoveListener(ChooseSecond); } }
    }
}
