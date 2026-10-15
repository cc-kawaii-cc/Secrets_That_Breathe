using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    [DefaultExecutionOrder(11000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerSeatController))]
    public sealed class NoodleEatingController : MonoBehaviour
    {
        public enum MealState { Ready, Eating, Finished }
        public MealState State { get; private set; }
        public float CurrentTiltDegrees { get; private set; }

        [Header("References")]
        [Tooltip("The actual MEMEEE object in the scene, not a copy or prefab asset.")]
        public Transform noodles;
        public SeatPromptUI promptCanvas;
        public TMPro.TMP_Text beforeEatingText;

        [Header("After sitting, before eating")]
        [Min(0f)] public float beforeEatingDelay = 3f;
        [Min(0.1f)] public float beforeEatingTextDuration = 3f;
        [Min(0f)] public float afterEatingTextDelay = 1f;
        private float introTime;
        private bool introFinished;

        [Header("Empty cup after eating")]
        [Tooltip("Optional empty-cup object from the Hierarchy. Place it at the full cup position as a separate sibling object. Hidden until eating finishes.")]
        public GameObject emptyCupModel;

        [Header("Look at cup to eat")]
        [Tooltip("Crosshair must aim inside this box to eat.")]
        public BoxCollider lookTarget;
        [Tooltip("The seated camera must be inside this box. Resize it to adjust eating range.")]
        public BoxCollider interactionZone;
        [HideInInspector] public float lookDistance = 2.5f;

        [Header("Cup position relative to camera (metres)")]
        [Tooltip("Position of the mesh centre, not the imported model pivot.")]
        public Vector3 heldOffset = new Vector3(0f, -0.20f, 0.48f);
        public Vector3 sipMovement = new Vector3(0f, 0.04f, -0.04f);
        [Min(0f)] public float liftArcHeight = 0.10f;

        [Header("Tilt")]
        [Range(0f, 90f)] public float initialTilt = 45f;
        [Range(0f, 90f)] public float finalTilt = 60f;
        [Tooltip("Axis in camera space. (-1,0,0) tips the opening towards the mouth.")]
        public Vector3 tiltAxis = Vector3.left;

        [Header("Timing (seconds)")]
        [Min(0.05f)] public float liftDuration = 1f;
        [Min(0.05f)] public float sipDuration = 2.5f;
        [Min(0f)] public float holdDuration = 0.4f;
        [Min(0.05f)] public float lowerDuration = 1f;

        [Header("Eating sound (loops until the eating animation ends)")]
        public AudioSource audioSource;
        [Tooltip("Loops throughout Lift + Sip + Hold + Lower, then stops immediately. Leave empty for no sound.")]
        public AudioClip eatingSound;
        [Tooltip("Volume multiplier during Lift. X: lift progress 0-1. Y: volume 0-1.")]
        public AnimationCurve eatingSoundFadeIn = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Volume multiplier during Lower. X: lowering progress 0-1. Y: volume 0-1.")]
        public AnimationCurve eatingSoundFadeOut = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [Header("Events")]
        public UnityEvent onEatingStarted = new UnityEvent();
        public UnityEvent onMealFinished = new UnityEvent();

        private PlayerSeatController player;
        private InputAction eat;
        private bool wasSeated, awaitingRelease, captured, previousLookLock;
        private float elapsed;
        private Vector3 originalLocalPosition, originalLocalScale, startCentre, localMeshCentre;
        private Quaternion originalLocalRotation, startRotation;
        private Collider[] colliders;
        private bool[] colliderStates;
        private Rigidbody[] bodies;
        private bool[] kinematicStates, collisionStates;
        private Transform mealObject;
        private AudioSource playingEatingSource;
        private bool previousAudioLoop;
        private float eatingBaseVolume;

        private void Awake()
        {
            player = GetComponent<PlayerSeatController>();
            eat = new InputAction("Eat noodles", InputActionType.Button, "<Keyboard>/e");
            eat.AddBinding("<Gamepad>/buttonSouth");
        }

        private void Start()
        {
            if (promptCanvas == null) promptCanvas = SeatPromptUI.FindOrCreate(gameObject.scene);
            promptCanvas.EnsureEatPrompt();
            promptCanvas.SetEatVisible(false);
            ConfigureBeforeEatingUI();
            ShowBeforeEatingText(false);
            if (emptyCupModel != null)
            {
                if (HasValidEmptyCup()) emptyCupModel.SetActive(false);
                else Debug.LogWarning("[Noodles] Empty Cup Model must be a separate scene object, outside MEMEEE and the Player hierarchy.", this);
            }
        }

        public void ConfigureBeforeEatingUI()
        {
            if (promptCanvas == null) promptCanvas = SeatPromptUI.FindOrCreate(gameObject.scene);
            if (beforeEatingText == null) beforeEatingText = promptCanvas.EnsureNamedText("BeforeEatingThought");
        }

        private void ShowBeforeEatingText(bool visible)
        {
            if (beforeEatingText == null) return;
            var panel = beforeEatingText.transform.parent.gameObject;
            if (visible && !panel.activeSelf && promptCanvas != null) promptCanvas.ShowRoomThought(beforeEatingText);
            else if (panel.activeSelf != visible) panel.SetActive(visible);
        }

        private void OnEnable() { eat?.Enable(); awaitingRelease = true; wasSeated = false; }

        private void Update()
        {
            bool seated = player.isActiveAndEnabled && player.State == PlayerSeatController.SeatState.Seated;
            if (State == MealState.Eating)
            {
                if (!seated || mealObject == null || player.playerCamera == null) { CancelEating(); return; }
                elapsed += Time.deltaTime;
                UpdateEatingSoundVolume();
                if (elapsed >= TotalDuration) FinishEating();
                return;
            }
            if (seated && !wasSeated)
            {
                awaitingRelease = true;
                introTime = 0f;
                introFinished = false;
            }
            else if (seated && State == MealState.Ready && !introFinished)
                introTime += Time.deltaTime;
            if (!seated)
            {
                introTime = 0f;
                introFinished = false;
                ShowBeforeEatingText(false);
            }
            else if (State == MealState.Ready && !introFinished)
            {
                float start = Mathf.Max(0f, beforeEatingDelay);
                float end = start + Mathf.Max(0.1f, beforeEatingTextDuration);
                ShowBeforeEatingText(introTime >= start && introTime < end);
                if (introTime >= end + Mathf.Max(0f, afterEatingTextDelay)) introFinished = true;
            }
            wasSeated = seated;
            if (seated && awaitingRelease && !eat.IsPressed()) awaitingRelease = false;
            bool ready = seated && introFinished && State == MealState.Ready && noodles != null && noodles.gameObject.activeInHierarchy &&
                Time.timeScale > 0f && IsLookingAtCup();
            if (promptCanvas != null) promptCanvas.SetEatVisible(ready);
            if (!ready || awaitingRelease) return;
            if (eat.WasPressedThisFrame()) TryEat();
        }

        private float TotalDuration => Mathf.Max(0.05f, liftDuration) + Mathf.Max(0.05f, sipDuration) +
            Mathf.Max(0f, holdDuration) + Mathf.Max(0.05f, lowerDuration);

        public bool IsLookingAtCup()
        {
            if (player == null || player.playerCamera == null || noodles == null || !noodles.gameObject.activeInHierarchy) return false;
            return InteractionLook.IsPointInBox(player.playerCamera.transform.position, interactionZone)
                && InteractionLook.IsLookingAtBox(player.playerCamera, lookTarget);
        }

        public bool TryEat()
        {
            if (!isActiveAndEnabled || !introFinished || State != MealState.Ready || !player.isActiveAndEnabled ||
                player.State != PlayerSeatController.SeatState.Seated || player.playerCamera == null ||
                noodles == null || !noodles.gameObject.activeInHierarchy || Time.timeScale <= 0f || !IsLookingAtCup()) return false;
            mealObject = noodles;
            Renderer[] renderers = mealObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return false;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            startCentre = bounds.center;
            localMeshCentre = mealObject.InverseTransformPoint(startCentre);
            startRotation = mealObject.rotation;
            originalLocalPosition = mealObject.localPosition;
            originalLocalRotation = mealObject.localRotation;
            originalLocalScale = mealObject.localScale;
            previousLookLock = player.SeatedLookLocked;
            player.SeatedLookLocked = true;
            colliders = mealObject.GetComponentsInChildren<Collider>(true);
            colliderStates = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++) { colliderStates[i] = colliders[i].enabled; colliders[i].enabled = false; }
            bodies = mealObject.GetComponentsInChildren<Rigidbody>(true);
            kinematicStates = new bool[bodies.Length];
            collisionStates = new bool[bodies.Length];
            for (int i = 0; i < bodies.Length; i++)
            {
                kinematicStates[i] = bodies[i].isKinematic;
                collisionStates[i] = bodies[i].detectCollisions;
                bodies[i].detectCollisions = false;
                bodies[i].isKinematic = true;
            }
            captured = true;
            elapsed = 0f;
            State = MealState.Eating;
            if (promptCanvas != null) promptCanvas.SetEatVisible(false);
            StartEatingSound();
            onEatingStarted.Invoke();
            return true;
        }

        // Runs after SeatCameraAnimation so the cup uses the final camera pose this frame.
        private void LateUpdate()
        {
            if (State != MealState.Eating || mealObject == null || player.playerCamera == null) return;
            Transform camera = player.playerCamera.transform;
            Vector3 heldCentre = camera.TransformPoint(heldOffset);
            Vector3 sippingCentre = camera.TransformPoint(heldOffset + sipMovement);
            Vector3 centre;
            float lift = Mathf.Max(0.05f, liftDuration), sip = Mathf.Max(0.05f, sipDuration);
            Vector3 axis = camera.TransformDirection(tiltAxis.sqrMagnitude > 0.0001f ? tiltAxis.normalized : Vector3.left);
            if (elapsed < lift)
            {
                float t = Mathf.Clamp01(elapsed / lift);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                centre = Vector3.Lerp(startCentre, heldCentre, eased) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * liftArcHeight);
                CurrentTiltDegrees = Mathf.Lerp(0f, initialTilt, eased);
            }
            else if (elapsed < lift + sip)
            {
                float t = Mathf.SmoothStep(0f, 1f, (elapsed - lift) / sip);
                centre = Vector3.Lerp(heldCentre, sippingCentre, t);
                CurrentTiltDegrees = Mathf.Lerp(initialTilt, finalTilt, t);
            }
            else if (elapsed < lift + sip + Mathf.Max(0f, holdDuration))
            {
                centre = sippingCentre;
                CurrentTiltDegrees = finalTilt;
            }
            else
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - lift - sip - Mathf.Max(0f, holdDuration)) / Mathf.Max(0.05f, lowerDuration)));
                centre = Vector3.Lerp(sippingCentre, startCentre, t);
                CurrentTiltDegrees = Mathf.Lerp(finalTilt, 0f, t);
            }
            mealObject.rotation = Quaternion.AngleAxis(CurrentTiltDegrees, axis) * startRotation;
            // Imported MEMEEE pivots can be far from the mesh; rotate about the actual cup centre.
            mealObject.position = centre - mealObject.TransformVector(localMeshCentre);
        }

        private void FinishEating()
        {
            RestoreObject();
            State = MealState.Finished;
            if (promptCanvas != null) promptCanvas.SetEatVisible(false);
            if (HasValidEmptyCup())
            {
                noodles.gameObject.SetActive(false);
                emptyCupModel.SetActive(true);
            }
            onMealFinished.Invoke();
        }

        private bool HasValidEmptyCup()
        {
            if (emptyCupModel == null || noodles == null || !emptyCupModel.scene.IsValid()) return false;
            Transform empty = emptyCupModel.transform;
            return !empty.IsChildOf(noodles) && !noodles.IsChildOf(empty) &&
                !empty.IsChildOf(transform.root) && !transform.IsChildOf(empty);
        }

        public void CancelEating()
        {
            ShowBeforeEatingText(false);
            if (State == MealState.Finished) return;
            RestoreObject();
            State = MealState.Ready;
            awaitingRelease = true;
            if (promptCanvas != null) promptCanvas.SetEatVisible(false);
        }

        private void RestoreObject()
        {
            StopEatingSound();
            if (!captured) return;
            if (mealObject != null)
            {
                mealObject.localPosition = originalLocalPosition;
                mealObject.localRotation = originalLocalRotation;
                mealObject.localScale = originalLocalScale;
            }
            for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
            for (int i = 0; i < bodies.Length; i++) if (bodies[i] != null)
            {
                bodies[i].isKinematic = kinematicStates[i];
                bodies[i].detectCollisions = collisionStates[i];
            }
            if (player != null) player.SeatedLookLocked = previousLookLock;
            CurrentTiltDegrees = 0f;
            captured = false;
            mealObject = null;
        }

        private void StartEatingSound()
        {
            if (eatingSound == null) return;
            if (audioSource == null)
            {
                var child = transform.Find("NoodleEatingAudio");
                if (child == null)
                {
                    child = new GameObject("NoodleEatingAudio").transform;
                    child.SetParent(transform, false);
                }
                audioSource = child.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = child.gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f;
                    audioSource.dopplerLevel = 0f;
                }
            }
            playingEatingSource = audioSource;
            previousAudioLoop = audioSource.loop;
            eatingBaseVolume = audioSource.volume;
            audioSource.loop = true;
            audioSource.clip = eatingSound;
            UpdateEatingSoundVolume();
            audioSource.Play();
        }

        private void UpdateEatingSoundVolume()
        {
            if (playingEatingSource == null) return;
            float lift = Mathf.Max(.05f, liftDuration);
            float lower = Mathf.Max(.05f, lowerDuration);
            float lowerStart = lift + Mathf.Max(.05f, sipDuration) + Mathf.Max(0f, holdDuration);
            float multiplier = 1f;
            if (elapsed < lift)
            {
                float progress = Mathf.Clamp01(elapsed / lift);
                multiplier = eatingSoundFadeIn != null && eatingSoundFadeIn.length > 0
                    ? eatingSoundFadeIn.Evaluate(progress) : progress;
            }
            else if (elapsed >= lowerStart)
            {
                float progress = Mathf.Clamp01((elapsed - lowerStart) / lower);
                multiplier = eatingSoundFadeOut != null && eatingSoundFadeOut.length > 0
                    ? eatingSoundFadeOut.Evaluate(progress) : 1f - progress;
            }
            playingEatingSource.volume = eatingBaseVolume * Mathf.Clamp01(multiplier);
        }

        private void StopEatingSound()
        {
            if (playingEatingSource == null) return;
            playingEatingSource.Stop();
            playingEatingSource.loop = previousAudioLoop;
            playingEatingSource.volume = eatingBaseVolume;
            playingEatingSource = null;
        }

        private void OnDisable() { eat?.Disable(); CancelEating(); }
        private void OnDestroy() { eat?.Dispose(); }
    }
}
