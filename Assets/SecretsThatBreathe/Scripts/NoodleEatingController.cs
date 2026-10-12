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

        [Header("Empty cup after eating")]
        [Tooltip("Optional empty-cup object from the Hierarchy. Place it at the full cup position as a separate sibling object. Hidden until eating finishes.")]
        public GameObject emptyCupModel;

        [Header("Look at cup to eat")]
        [Tooltip("Allowed angle from the camera forward direction to the cup centre. 35 allows a broad 70-degree cone.")]
        [Range(1f, 80f)] public float lookAngle = 35f;
        [Tooltip("Maximum camera-to-cup distance in metres.")]
        [Min(0.1f)] public float lookDistance = 2.5f;

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

        [Header("Optional sound and events")]
        public AudioSource audioSource;
        public AudioClip eatingSound;
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
        private Transform cachedCup;
        private Renderer[] cupRenderers;

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
            if (emptyCupModel != null)
            {
                if (HasValidEmptyCup()) emptyCupModel.SetActive(false);
                else Debug.LogWarning("[Noodles] Empty Cup Model must be a separate scene object, outside MEMEEE and the Player hierarchy.", this);
            }
        }

        private void OnEnable() { eat?.Enable(); awaitingRelease = true; wasSeated = false; }

        private void Update()
        {
            bool seated = player.isActiveAndEnabled && player.State == PlayerSeatController.SeatState.Seated;
            if (State == MealState.Eating)
            {
                if (!seated || mealObject == null || player.playerCamera == null) { CancelEating(); return; }
                elapsed += Time.deltaTime;
                if (elapsed >= TotalDuration) FinishEating();
                return;
            }
            if (seated && !wasSeated) awaitingRelease = true;
            wasSeated = seated;
            if (seated && awaitingRelease && !eat.IsPressed()) awaitingRelease = false;
            bool ready = seated && State == MealState.Ready && noodles != null && noodles.gameObject.activeInHierarchy &&
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
            if (cachedCup != noodles || cupRenderers == null)
            {
                cachedCup = noodles;
                cupRenderers = noodles.GetComponentsInChildren<Renderer>(true);
            }
            return InteractionLook.IsLookingAt(player.playerCamera, cupRenderers, lookAngle, lookDistance);
        }

        public bool TryEat()
        {
            if (!isActiveAndEnabled || State != MealState.Ready || !player.isActiveAndEnabled ||
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
            if (audioSource != null && eatingSound != null) { audioSource.clip = eatingSound; audioSource.Play(); }
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
            if (State == MealState.Finished) return;
            RestoreObject();
            State = MealState.Ready;
            awaitingRelease = true;
            if (promptCanvas != null) promptCanvas.SetEatVisible(false);
        }

        private void RestoreObject()
        {
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
            if (audioSource != null && eatingSound != null && audioSource.clip == eatingSound) audioSource.Stop();
            CurrentTiltDegrees = 0f;
            captured = false;
            mealObject = null;
        }

        private void OnDisable() { eat?.Disable(); CancelEating(); }
        private void OnDestroy() { eat?.Dispose(); }
    }
}
