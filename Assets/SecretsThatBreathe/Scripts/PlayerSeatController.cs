using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SeatCameraAnimation))]
    public sealed class PlayerSeatController : MonoBehaviour
    {
        public enum SeatState { Exploring, SittingDown, Seated }
        public SeatState State { get; private set; }
        public bool CanSit => isActiveAndEnabled && State == SeatState.Exploring;
        public bool SeatedLookLocked { get; set; }

        [Header("References")]
        public Transform playerBody;
        public Camera playerCamera;
        public CharacterController characterController;
        public FirstPersonController firstPersonController;
        [Tooltip("Other movement, look, or interaction components to suspend while seated.")]
        public Behaviour[] additionalControls = new Behaviour[0];

        [Header("Seated look")]
        [Range(0f, 120f)] public float yawLimit = 65f;
        [Range(0f, 80f)] public float pitchLimit = 35f;
        [Min(0f)] public float mouseSensitivity = 0.08f;
        [Min(0f)] public float gamepadLookSpeed = 90f;

        [Header("Events")]
        public UnityEvent onSitStarted = new UnityEvent();
        public UnityEvent onSeated = new UnityEvent();

        private readonly Dictionary<Behaviour, bool> savedControls = new Dictionary<Behaviour, bool>();
        private SeatCameraAnimation cameraAnimation;
        private ChairInteractable chair;
        private Coroutine transition;
        private Vector3 originalBodyPosition, originalCameraPosition;
        private Quaternion originalBodyRotation, originalCameraRotation;
        private bool controllerWasEnabled, captured;
        private float yaw, pitch;

        private void Awake()
        {
            cameraAnimation = GetComponent<SeatCameraAnimation>();
            if (firstPersonController == null) firstPersonController = GetComponentInChildren<FirstPersonController>();
            if (playerBody == null) playerBody = firstPersonController != null ? firstPersonController.transform : transform;
            if (characterController == null) characterController = playerBody.GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = Camera.main;
            if (additionalControls.Length == 0)
            {
                var controls = new List<Behaviour>();
                foreach (Behaviour control in playerBody.root.GetComponentsInChildren<Behaviour>(true))
                    if (control is PlayerMovement || control is MouseLook || control is PlayerInteractor) controls.Add(control);
                additionalControls = controls.ToArray();
            }
        }

        public bool TrySit(ChairInteractable target)
        {
            if (!CanSit || target == null || !target.CanSit || playerBody == null || playerCamera == null ||
                !target.IsPlayerInZone(playerBody) || !target.IsLookingAtChair(playerCamera))
                return false;

            chair = target;
            originalBodyPosition = playerBody.position;
            originalBodyRotation = playerBody.rotation;
            originalCameraPosition = playerCamera.transform.localPosition;
            originalCameraRotation = playerCamera.transform.localRotation;
            Vector3 startPosition = playerCamera.transform.position;
            Quaternion startRotation = playerCamera.transform.rotation;
            controllerWasEnabled = characterController != null && characterController.enabled;
            captured = true;
            State = SeatState.SittingDown;
            Suspend(firstPersonController);
            Suspend(playerCamera.GetComponent<CinemachineBrain>());
            foreach (Behaviour control in additionalControls) Suspend(control);
            if (characterController != null) characterController.enabled = false;

            playerBody.SetPositionAndRotation(target.seatPoint.position, target.seatPoint.rotation);
            cameraAnimation.Begin(playerCamera.transform, startPosition, startRotation);
            yaw = pitch = 0f;
            transition = StartCoroutine(SitTransition(startPosition, startRotation));
            onSitStarted.Invoke();
            return true;
        }

        private void Suspend(Behaviour control)
        {
            if (control == null || control == this || control == cameraAnimation || savedControls.ContainsKey(control)) return;
            savedControls.Add(control, control.enabled);
            control.enabled = false;
        }

        private IEnumerator SitTransition(Vector3 startPosition, Quaternion startRotation)
        {
            float elapsed = 0f;
            float seconds = Mathf.Max(0.05f, cameraAnimation.duration);
            while (elapsed < seconds)
            {
                if (chair == null || !chair.CanSit || playerCamera == null) { CancelSeating(); yield break; }
                cameraAnimation.Sample(startPosition, startRotation, chair.seatedCameraPoint, elapsed / seconds);
                yield return null;
                elapsed += Time.deltaTime;
            }
            if (chair == null || !chair.CanSit) { CancelSeating(); yield break; }
            cameraAnimation.SetPose(chair.seatedCameraPoint.position, chair.seatedCameraPoint.rotation);
            State = SeatState.Seated;
            transition = null;
            onSeated.Invoke();
        }

        private void Update()
        {
            if (State == SeatState.Exploring) return;
            if (chair == null || !chair.CanSit || playerCamera == null) { CancelSeating(); return; }
            if (State != SeatState.Seated || SeatedLookLocked || Time.timeScale == 0f || Cursor.lockState != CursorLockMode.Locked) return;
            Vector2 look = Mouse.current != null ? Mouse.current.delta.ReadValue() * mouseSensitivity : Vector2.zero;
            if (Gamepad.current != null) look += Gamepad.current.rightStick.ReadValue() * gamepadLookSpeed * Time.deltaTime;
            yaw = Mathf.Clamp(yaw + look.x, -yawLimit, yawLimit);
            pitch = Mathf.Clamp(pitch - look.y, -pitchLimit, pitchLimit);
            cameraAnimation.SetPose(chair.seatedCameraPoint.position,
                chair.seatedCameraPoint.rotation * Quaternion.Euler(pitch, yaw, 0f));
        }

        // Restore the pre-seat pose when cancelled/disabled; normal gameplay stays seated.
        public void CancelSeating()
        {
            if (transition != null) { StopCoroutine(transition); transition = null; }
            if (!captured) return;
            cameraAnimation.Release();
            if (playerBody != null) playerBody.SetPositionAndRotation(originalBodyPosition, originalBodyRotation);
            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = originalCameraPosition;
                playerCamera.transform.localRotation = originalCameraRotation;
            }
            Physics.SyncTransforms();
            if (characterController != null) characterController.enabled = controllerWasEnabled;
            foreach (var pair in savedControls) if (pair.Key != null) pair.Key.enabled = pair.Value;
            savedControls.Clear();
            captured = false;
            SeatedLookLocked = false;
            chair = null;
            State = SeatState.Exploring;
        }

        private void OnDisable() { CancelSeating(); }
    }
}
