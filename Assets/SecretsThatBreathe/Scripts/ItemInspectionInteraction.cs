using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(16000)]
    [MovedFrom(true, sourceNamespace: "SecretsThatBreathe", sourceAssembly: "Assembly-CSharp", sourceClassName: "SamudNotebookInteraction")]
    public sealed class ItemInspectionInteraction : MonoBehaviour
    {
        public enum NotebookState { OnTable, PickingUp, Held, OpeningNote, Reading, ClosingNote, PuttingDown, Finished }
        public PlayerSeatController player;
        public Camera playerCamera;
        public BoxCollider interactionZone;
        public BoxCollider lookTarget;
        [FormerlySerializedAs("notebookUI")]
        public ItemInspectionUI inspectionUI;
        [Tooltip("Disable for inspect-only props without a reading page.")]
        public bool allowReading = true;
        public const uint InspectionRenderingLayer = 1u << 7;
        [Header("Sound when opening the reading page (edit clip and volume on this Audio Source)")]
        public AudioSource readAudioSource;
        [Tooltip("Extra gain for the reading sound, multiplied by the Audio Source volume.")]
        [Range(1f, 10f)] public float readVolumeBoost = 1f;
        [Header("Inspection controls and temporary light")]
        public Light inspectionLight;
        public bool enableInspectionLight = true;
        [Min(.01f)] public float mouseRotationSensitivity = .25f;
        [HideInInspector] public int inspectionControlsRevision;
        [Header("Mouse wheel zoom (camera distance in metres)")]
        [Min(.1f)] public float zoomMinDistance = .35f;
        [Min(.1f)] public float zoomMaxDistance = 1.1f;
        [Tooltip("Start halfway between Zoom Min Distance and Zoom Max Distance. Disable to use Held Position Z instead.")]
        public bool startZoomAtMidpoint = true;
        [Tooltip("Normalised zoom travel per mouse-wheel notch.")]
        [Range(.01f, 30f)] public float zoomSensitivity = .1f;
        [Min(.01f)] public float zoomSmoothTime = .15f;
        [Header("Held item centre, relative to camera")]
        public Vector3 heldPosition = new Vector3(0f, -.025f, .65f);
        [Tooltip("Enable to rotate toward Held Rotation. Disable to keep the item's original world orientation throughout pickup and putdown.")]
        public bool enableHeldRotation = true;
        public Vector3 heldRotation = new Vector3(90f, 180f, 0f);
        [HideInInspector] public int heldFacingRevision;
        [Tooltip("Largest item dimension in metres while held.")]
        [Min(.05f)] public float heldSize = .28f;
        [Min(.05f)] public float pickupDuration = .55f;
        [Min(.05f)] public float putDownDuration = .45f;
        public NotebookState State { get; private set; }
        public bool HasInteracted { get; private set; }
        public bool HasRead { get; private set; }
        private static ItemInspectionInteraction activeInspection;
        public static bool IsBusy(Scene scene) => activeInspection != null && activeInspection.gameObject.scene == scene;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActive() => activeInspection = null;
        private bool captured, previousMovementLock, previousLookInput, previousInspectionLock;
        private FirstPersonController movement;
        private StarterAssetsInputs input;
        private Transform originalParent;
        private Vector3 originalPosition, originalScale, originalCentre, localCentre, heldScale;
        private Quaternion originalRotation, originalWorldRotation;
        private Collider[] colliders;
        private bool[] colliderStates;
        private Rigidbody[] bodies;
        private bool[] kinematicStates, collisionStates;
        private Renderer[] inspectedRenderers;
        private uint[] originalRenderingLayers;
        private float progress;
        private Quaternion manualRotation = Quaternion.identity;
        private bool previousCursorLocked, previousCursorVisible;
        private CursorLockMode previousCursorLock;
        private float zoomTarget, zoomCurrent, zoomVelocity;

        private void ResetZoom()
        {
            if (startZoomAtMidpoint)
            {
                zoomTarget = zoomCurrent = .5f;
                zoomVelocity = 0f;
                return;
            }
            float min = Mathf.Max(.1f, zoomMinDistance);
            float max = Mathf.Max(min + .01f, zoomMaxDistance);
            float distance = Mathf.InverseLerp(min, max, heldPosition.z);
            // Invert the easing curve to preserve the configured starting distance.
            float low = 0f, high = 1f;
            for (int i = 0; i < 20; i++)
            {
                float mid = (low + high) * .5f;
                if (Mathf.SmoothStep(0f, 1f, mid) < distance) low = mid; else high = mid;
            }
            zoomTarget = zoomCurrent = (low + high) * .5f;
            zoomVelocity = 0f;
        }
        private void UpdateZoom(float scroll)
        {
            zoomTarget = Mathf.Clamp01(zoomTarget - scroll / 120f * zoomSensitivity);
            zoomCurrent = Mathf.SmoothDamp(zoomCurrent, zoomTarget, ref zoomVelocity, Mathf.Max(.01f, zoomSmoothTime));
        }

        public void ReadNote() { if (allowReading && State == NotebookState.Held && Time.timeScale > 0f) StartCoroutine(OpenNote()); }
        public void CloseReading() { if (State == NotebookState.Reading && Time.timeScale > 0f) StartCoroutine(CloseNote()); }
        public void PlaceNotebook() { if (State == NotebookState.Held && Time.timeScale > 0f) StartCoroutine(PutDown()); }

        private void Start()
        {
            if (inspectionUI != null) inspectionUI.Clear();
            if (inspectionLight != null) inspectionLight.enabled = false;
        }
        private void Update()
        {
            if (inspectionUI == null) return;
            if (playerCamera == null) playerCamera = Camera.main;
            bool available = State == NotebookState.OnTable && Time.timeScale > 0f
                && player != null && player.CanExplore && playerCamera != null && CanLook();
            inspectionUI.ShowLookPrompt(available);
            if (Time.timeScale <= 0f) return;
            if (State == NotebookState.Held && Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue() * mouseRotationSensitivity;
                manualRotation = Quaternion.Euler(delta.y, -delta.x, 0f) * manualRotation;
            }
            if (State == NotebookState.Held)
                UpdateZoom(Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f);
            if (Keyboard.current == null) return;
            if (available && Keyboard.current.eKey.wasPressedThisFrame) TryPickUp();
            else if (State == NotebookState.Held)
            {
                if (allowReading && Keyboard.current.rKey.wasPressedThisFrame) ReadNote();
                else if (Keyboard.current.eKey.wasPressedThisFrame) PlaceNotebook();
            }
            else if (State == NotebookState.Reading && Keyboard.current.rKey.wasPressedThisFrame)
                CloseReading();
        }
        private bool CanLook()
        {
            var controller = player.GetComponent<CharacterController>();
            Vector3 position = controller != null ? controller.transform.TransformPoint(controller.center) : player.transform.position;
            return InteractionLook.IsPointInBox(position, interactionZone) && InteractionLook.IsLookingAtBox(playerCamera, lookTarget);
        }
        public bool TryPickUp()
        {
            if (!isActiveAndEnabled || State != NotebookState.OnTable || inspectionUI == null || player == null
                || !player.CanExplore || playerCamera == null || Time.timeScale <= 0f || !CanLook()) return false;
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return false;
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            inspectedRenderers = renderers;
            originalRenderingLayers = new uint[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalRenderingLayers[i] = renderers[i].renderingLayerMask & ~InspectionRenderingLayer;
                renderers[i].renderingLayerMask = originalRenderingLayers[i] | InspectionRenderingLayer;
            }
            originalParent = transform.parent;
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
            originalWorldRotation = transform.rotation;
            originalScale = transform.localScale;
            originalCentre = bounds.center;
            localCentre = transform.InverseTransformPoint(bounds.center);
            float largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            heldScale = originalScale * (Mathf.Max(.05f, heldSize) / Mathf.Max(.001f, largest));
            colliders = GetComponentsInChildren<Collider>(true);
            colliderStates = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++) { colliderStates[i] = colliders[i].enabled; colliders[i].enabled = false; }
            bodies = GetComponentsInChildren<Rigidbody>(true);
            kinematicStates = new bool[bodies.Length]; collisionStates = new bool[bodies.Length];
            for (int i = 0; i < bodies.Length; i++)
            {
                kinematicStates[i] = bodies[i].isKinematic; collisionStates[i] = bodies[i].detectCollisions;
                bodies[i].detectCollisions = false; bodies[i].isKinematic = true;
            }
            movement = player.GetComponent<FirstPersonController>();
            if (movement != null) { previousMovementLock = movement.MovementLocked; movement.MovementLocked = true; }
            input = player.GetComponent<StarterAssetsInputs>();
            if (input != null)
            {
                previousLookInput = input.cursorInputForLook;
                previousCursorLocked = input.cursorLocked;
                input.cursorInputForLook = false;
                input.cursorLocked = false;
                input.LookInput(Vector2.zero);
            }
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Leave it off after putdown; the player must press F again.
            var flashlight = player.GetComponent<FirstPersonFlashlight>();
            if (flashlight != null) flashlight.ForceOff();
            manualRotation = Quaternion.identity;
            ResetZoom();
            if (inspectionLight != null) inspectionLight.enabled = enableInspectionLight;
            inspectionUI.Bind(this);
            previousInspectionLock = player.ItemInspectionActive;
            player.ItemInspectionActive = true;
            activeInspection = this;
            captured = true;
            HasInteracted = true;
            progress = 0f;
            State = NotebookState.PickingUp;
            inspectionUI.Clear();
            StartCoroutine(PickUp());
            return true;
        }
        private IEnumerator PickUp()
        {
            yield return Animate(1f, pickupDuration);
            State = NotebookState.Held;
            inspectionUI.ShowHeldPrompt(true);
        }
        private IEnumerator OpenNote()
        {
            HasRead = true;
            State = NotebookState.OpeningNote;
            if (readAudioSource != null && readAudioSource.clip != null)
                readAudioSource.PlayOneShot(readAudioSource.clip, readVolumeBoost);
            yield return inspectionUI.OpenNote();
            State = NotebookState.Reading;
        }
        private IEnumerator CloseNote()
        {
            State = NotebookState.ClosingNote;
            yield return inspectionUI.CloseNote();
            State = NotebookState.Held;
        }
        private IEnumerator PutDown()
        {
            State = NotebookState.PuttingDown;
            inspectionUI.Clear();
            yield return Animate(0f, putDownDuration);
            Restore();
            State = NotebookState.OnTable;
        }
        private IEnumerator Animate(float target, float duration)
        {
            float from = progress;
            duration = Mathf.Max(.05f, duration);
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                progress = Mathf.Lerp(from, target, time / duration);
                ApplyPose();
                yield return null;
            }
            progress = target;
            ApplyPose();
        }
        private void LateUpdate() { if (captured) ApplyPose(); }
        private void ApplyPose()
        {
            if (playerCamera == null) return;
            float t = Mathf.SmoothStep(0f, 1f, progress);
            transform.localScale = Vector3.Lerp(originalScale, heldScale, t);
            Quaternion facing = enableHeldRotation
                ? playerCamera.transform.rotation * Quaternion.Euler(heldRotation) : originalWorldRotation;
            Quaternion cameraRotation = playerCamera.transform.rotation;
            facing = cameraRotation * manualRotation * Quaternion.Inverse(cameraRotation) * facing;
            transform.rotation = Quaternion.Slerp(originalWorldRotation, facing, t);
            Vector3 zoomedPosition = heldPosition;
            float minDistance = Mathf.Max(.1f, zoomMinDistance);
            zoomedPosition.z = Mathf.Lerp(minDistance, Mathf.Max(minDistance + .01f, zoomMaxDistance),
                Mathf.SmoothStep(0f, 1f, zoomCurrent));
            Vector3 centre = Vector3.Lerp(originalCentre, playerCamera.transform.TransformPoint(zoomedPosition), t);
            transform.position = centre - transform.TransformVector(localCentre);
        }
        private void Restore()
        {
            if (!captured) return;
            if (readAudioSource != null) readAudioSource.Stop();
            for (int i = 0; i < inspectedRenderers.Length; i++)
                if (inspectedRenderers[i] != null) inspectedRenderers[i].renderingLayerMask = originalRenderingLayers[i];
            transform.SetParent(originalParent, false);
            transform.localPosition = originalPosition; transform.localRotation = originalRotation; transform.localScale = originalScale;
            for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
            for (int i = 0; i < bodies.Length; i++) if (bodies[i] != null)
            { bodies[i].isKinematic = kinematicStates[i]; bodies[i].detectCollisions = collisionStates[i]; }
            if (movement != null) movement.MovementLocked = previousMovementLock;
            if (input != null)
            {
                input.cursorInputForLook = previousLookInput;
                input.cursorLocked = previousCursorLocked;
                input.LookInput(Vector2.zero);
            }
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            if (inspectionLight != null) inspectionLight.enabled = false;
            if (player != null) player.ItemInspectionActive = previousInspectionLock;
            if (activeInspection == this) activeInspection = null;
            captured = false;
        }
        public void CancelForTesting()
        {
            StopAllCoroutines();
            Restore();
            if (inspectionUI != null) inspectionUI.Clear();
            State = NotebookState.OnTable;
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            Restore();
            if (inspectionUI != null) inspectionUI.Clear();
            State = NotebookState.OnTable;
        }
    }
}
