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
        public bool CanExplore => isActiveAndEnabled && State == SeatState.Exploring && !standControlsHeld
            && !PhoneCallInteraction.IsBusy(gameObject.scene);
        public bool ChairInteractionsLocked { get; private set; }
        public bool CanSit => CanExplore && !ChairInteractionsLocked && !OfficeIntroDialogue.IsBlocking(gameObject.scene);
        public bool SeatedLookLocked { get; set; }

        public void LockChairInteractions()
        {
            ChairInteractionsLocked = true;
            var interactor = GetComponent<SeatInteractor>();
            if (interactor != null) interactor.ClearChairUI();
        }

        [Header("References")]
        public Transform playerBody;
        public Camera playerCamera;
        public CharacterController characterController;
        public FirstPersonController firstPersonController;
        [Tooltip("Other movement, look, or interaction components to suspend while seated.")]
        public Behaviour[] additionalControls = new Behaviour[0];
        [Header("Sit fade (shares the stand-up timing settings)")]
        public ScreenFadeUI sitFade;
        private bool ownsSitFade;

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
        private bool standControlsHeld;
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
            // Clear the on-state before suspending input; standing up must not restore an already-on flashlight.
            foreach (FirstPersonFlashlight flashlight in playerBody.root.GetComponentsInChildren<FirstPersonFlashlight>(true))
            {
                flashlight.ForceOff();
                Suspend(flashlight);
            }
            if (characterController != null) characterController.enabled = false;

            var story = GetComponent<DrawerSearchScareSequence>();
            if (story != null && story.screenFade != null) sitFade = story.screenFade;
            if (sitFade == null) sitFade = ScreenFadeUI.FindOrCreate(SeatPromptUI.FindOrCreate(gameObject.scene));
            ownsSitFade = true;
            sitFade.Clear();
            cameraAnimation.Begin(playerCamera.transform, startPosition, startRotation);
            yaw = pitch = 0f;
            transition = StartCoroutine(SitTransition());
            onSitStarted.Invoke();
            return true;
        }

        private void Suspend(Behaviour control)
        {
            if (control == null || control == this || control == cameraAnimation || savedControls.ContainsKey(control)) return;
            savedControls.Add(control, control.enabled);
            control.enabled = false;
        }

        private IEnumerator SitTransition()
        {
            var story = GetComponent<DrawerSearchScareSequence>();
            float fadeOut = story != null ? story.standFadeOutDuration : 0.7f;
            float blackHold = story != null ? story.standBlackHold : 0.15f;
            float fadeIn = story != null ? story.standFadeInDuration : 0.7f;
            yield return sitFade.FadeTo(1f, fadeOut);
            if (chair == null || !chair.CanSit || playerCamera == null) { CancelSeating(); yield break; }
            playerBody.SetPositionAndRotation(chair.seatPoint.position, chair.seatPoint.rotation);
            Physics.SyncTransforms();
            cameraAnimation.SetPose(chair.seatedCameraPoint.position, chair.seatedCameraPoint.rotation);
            yield return new WaitForSeconds(Mathf.Max(0f, blackHold));
            yield return sitFade.FadeTo(0f, fadeIn);
            if (chair == null || !chair.CanSit) { CancelSeating(); yield break; }
            cameraAnimation.SetPose(chair.seatedCameraPoint.position, chair.seatedCameraPoint.rotation);
            ownsSitFade = false;
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
            ExitSeat(null, false);
        }

        // While the screen is black, return to standing height without allowing input until fade-in finishes.
        public void StandUp(Transform destination = null, bool holdControls = false)
        {
            ExitSeat(destination, holdControls);
        }

        private void ExitSeat(Transform destination, bool holdControls)
        {
            if (transition != null) { StopCoroutine(transition); transition = null; }
            if (ownsSitFade)
            {
                if (sitFade != null) sitFade.Clear();
                ownsSitFade = false;
            }
            if (!captured) return;
            cameraAnimation.Release();
            if (playerBody != null) playerBody.SetPositionAndRotation(originalBodyPosition, originalBodyRotation);
            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = originalCameraPosition;
                playerCamera.transform.localRotation = originalCameraRotation;
            }
            if (destination != null && playerBody != null)
            {
                Quaternion rotationDelta = destination.rotation * Quaternion.Inverse(originalBodyRotation);
                Vector3 cameraOffset = playerCamera != null ? playerCamera.transform.position - originalBodyPosition : Vector3.zero;
                Quaternion viewRotation = playerCamera != null ? playerCamera.transform.rotation : Quaternion.identity;
                playerBody.SetPositionAndRotation(destination.position, destination.rotation);
                if (playerCamera != null) playerCamera.transform.SetPositionAndRotation(destination.position + rotationDelta * cameraOffset, rotationDelta * viewRotation);
            }
            Physics.SyncTransforms();
            captured = false;
            SeatedLookLocked = false;
            chair = null;
            State = SeatState.Exploring;
            standControlsHeld = true;
            if (!holdControls) ResumeAfterStand();
        }

        public void ResumeAfterStand()
        {
            if (captured || !standControlsHeld) return;
            if (characterController != null) characterController.enabled = controllerWasEnabled;
            foreach (var pair in savedControls)
            {
                if (pair.Key == null) continue;
                if (pair.Key is FirstPersonFlashlight flashlight) flashlight.ForceOff();
                pair.Key.enabled = pair.Value;
            }
            savedControls.Clear();
            standControlsHeld = false;
        }

        private void OnDisable() { CancelSeating(); if (standControlsHeld) ResumeAfterStand(); }
    }
}
