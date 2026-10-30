using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    [DefaultExecutionOrder(20000)]
    [DisallowMultipleComponent]
    public sealed partial class PorschePassengerRide : MonoBehaviour
    {
        [Header("Route (world positions)")]
        public Vector3 startPosition = new Vector3(209.3862f, 5.09909f, 142.75f);
        public Vector3 endPosition = new Vector3(209.3862f, 5.09909f, 284.65f);
        [Tooltip("Metres per second. Set to zero to pause. Can be changed during Play.")]
        [Min(0f)] public float speed = 1f;
        public bool playOnStart = true;

        [Header("Optional smooth route (world positions)")]
        [Tooltip("Corners between Start and End. Leave empty for the original straight ride.")]
        public Vector3[] intermediateRoutePoints = new Vector3[0];
        [Min(.1f)] public float cornerRadius = 5f;
        [Tooltip("How quickly the car's heading follows the road curve.")]
        [Min(.1f)] public float headingResponse = 6f;

        [Header("Smooth arrival")]
        [Tooltip("Distance before the destination at which braking begins. Zero keeps a constant-speed arrival.")]
        [Min(0f)] public float brakingDistance = 12f;

        [Header("Passenger and camera")]
        [Tooltip("Move this child Transform to adjust the driver's eye position and forward direction.")]
        public Transform driverSeat;
        public Transform playerRoot;
        public Camera playerCamera;
        [Range(30f, 110f)] public float fieldOfView = 65f;
        [Range(0.01f, 0.3f)] public float nearClipPlane = 0.03f;

        [Header("Look limits (degrees to EACH side of forward)")]
        [Range(0f, 180f)] public float yawLimit = 55f;
        [Range(0f, 89f)] public float pitchLimit = 55f;
        [Min(0f)] public float mouseSensitivity = 0.08f;
        [Min(0f)] public float gamepadLookSpeed = 90f;
        [Tooltip("Use the walking controller's Look input, sensitivity and mouse smoothing while seated.")]
        public bool matchWalkingLook = true;

        [Header("Controls suspended only while riding")]
        [Tooltip("Walking, seating and Cinemachine controls. Phone flashlight stays enabled.")]
        public Behaviour[] suspendedControls = new Behaviour[0];

        [Header("Interior driving ambience")]
        public AudioClip drivingInteriorClip;
        [Range(0f, 1f)] public float drivingInteriorVolume = .45f;
        [Tooltip("Optional source. If unassigned, a stereo 2D source is created on the car at runtime.")]
        public AudioSource drivingInteriorAudio;

        public bool IsMoving { get; private set; }
        public bool HasArrived { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float RemainingDistance => route != null ? Mathf.Max(0f, route.Length - routeDistance)
            : Vector3.Distance(transform.position, endPosition);
        private PassengerRidePath route;
        private float routeDistance;
        private SmoothRideStop arrivalStop;
        private bool initialized;
        private bool[] controlStates;
        private CharacterController[] characterControllers;
        private bool[] controllerStates;
        private Vector3 cameraOffsetInPlayer;
        private float originalFov, originalNearClip, yaw, pitch;
        private bool drivingAudioPaused;
        private StarterAssets.FirstPersonController walkingLook;
        private StarterAssets.StarterAssetsInputs walkingInputs;
        private PlayerInput walkingPlayerInput;
        private Vector2 pendingMouseRotation;

        private void Start()
        {
            if (driverSeat == null || playerRoot == null || playerCamera == null)
            {
                Debug.LogError("Porsche ride needs DriverSeat, Player Root and Player Camera.", this);
                enabled = false;
                return;
            }
            cameraOffsetInPlayer = playerRoot.InverseTransformPoint(playerCamera.transform.position);
            originalFov = playerCamera.fieldOfView;
            originalNearClip = playerCamera.nearClipPlane;
            walkingLook = playerRoot.GetComponentInChildren<StarterAssets.FirstPersonController>(true);
            if (walkingLook != null)
            {
                walkingInputs = walkingLook.GetComponent<StarterAssets.StarterAssetsInputs>();
                walkingPlayerInput = walkingLook.GetComponent<PlayerInput>();
            }
            controlStates = new bool[suspendedControls.Length];
            for (int i = 0; i < suspendedControls.Length; i++)
            {
                var control = suspendedControls[i];
                if (control == null || control == this) continue;
                controlStates[i] = control.enabled;
                control.enabled = false;
            }
            characterControllers = playerRoot.GetComponentsInChildren<CharacterController>(true);
            controllerStates = new bool[characterControllers.Length];
            for (int i = 0; i < characterControllers.Length; i++)
            {
                controllerStates[i] = characterControllers[i].enabled;
                characterControllers[i].enabled = false;
            }
            initialized = true;
            SetRidingFlashlightState(false);
            ConfigureDrivingAudio();
            RestartRide();
            IsMoving = playOnStart;
            UpdatePassengerPose();
        }

        private void OnEnable()
        {
            if (!initialized) return;
            for (int i = 0; i < suspendedControls.Length; i++)
                if (suspendedControls[i] != null && suspendedControls[i] != this)
                    suspendedControls[i].enabled = false;
            foreach (var controller in characterControllers)
                if (controller != null) controller.enabled = false;
        }

        private void Update()
        {
            if (!initialized || hasExited || Time.timeScale <= 0f) return;
            UpdateExitInteraction();
            if (exitInProgress) return;
            if (IsMoving)
            {
                float travel = 0f;
                float requestedSpeed = Mathf.Max(0f, speed);
                if (requestedSpeed > 0f)
                {
                    if (arrivalStop == null && brakingDistance > 0f && RemainingDistance <= brakingDistance)
                        arrivalStop = new SmoothRideStop(RemainingDistance, requestedSpeed);
                    if (arrivalStop != null)
                    {
                        float before = arrivalStop.RemainingDistance;
                        arrivalStop.Advance(Time.deltaTime);
                        travel = Mathf.Max(0f, before - arrivalStop.RemainingDistance);
                        CurrentSpeed = arrivalStop.Speed;
                    }
                    else
                    {
                        CurrentSpeed = requestedSpeed;
                        travel = requestedSpeed * Time.deltaTime;
                    }
                }
                else CurrentSpeed = 0f;
                if (route != null)
                {
                    routeDistance = Mathf.Min(route.Length, routeDistance + travel);
                    route.Evaluate(routeDistance, out var position, out var forward);
                    transform.position = position;
                    if (forward.sqrMagnitude > .000001f)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward, Vector3.up),
                            1f - Mathf.Exp(-Mathf.Max(.1f, headingResponse) * Time.deltaTime));
                    HasArrived = routeDistance >= route.Length || (arrivalStop != null && arrivalStop.IsComplete);
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, endPosition, travel);
                    HasArrived = (transform.position - endPosition).sqrMagnitude < 0.000001f
                        || (arrivalStop != null && arrivalStop.IsComplete);
                }
                if (HasArrived)
                {
                    transform.position = endPosition;
                    if (route != null) routeDistance = route.Length;
                    CurrentSpeed = 0f;
                    IsMoving = false;
                }
            }
        }

        private void LateUpdate()
        {
            if (!initialized || hasExited) return;
            if (!passengerDetached)
            {
                UpdatePassengerLook();
                UpdatePassengerPose();
            }
            UpdateDrivingAudio();
        }

        private void UpdatePassengerLook()
        {
            if (exitInProgress || Time.timeScale <= 0f || EveningDriveIntro.IsInputBlocked(gameObject.scene)
                || Cursor.lockState != CursorLockMode.Locked || !Application.isFocused
                || (matchWalkingLook && walkingInputs != null && !walkingInputs.cursorInputForLook))
            {
                pendingMouseRotation = Vector2.zero;
                return;
            }

            Vector2 rotation;
            if (matchWalkingLook && walkingLook != null && walkingInputs != null && walkingPlayerInput != null)
            {
                // Use the same processed Look action as walking: pointer sensitivity,
                // inversion and gamepad deadzone are already applied by Input System.
                bool mouse = walkingPlayerInput.currentControlScheme == "KeyboardMouse";
                if (mouse)
                {
                    pendingMouseRotation += walkingInputs.look * walkingLook.RotationSpeed;
                    float response = walkingLook.MouseLookSmoothTime <= 0f ? 1f
                        : 1f - Mathf.Exp(-Time.unscaledDeltaTime / walkingLook.MouseLookSmoothTime);
                    rotation = pendingMouseRotation * response;
                    pendingMouseRotation -= rotation;
                }
                else
                {
                    pendingMouseRotation = Vector2.zero;
                    rotation = walkingInputs.look.sqrMagnitude >= .01f
                        ? walkingInputs.look * (walkingLook.RotationSpeed * Time.deltaTime) : Vector2.zero;
                }
            }
            else
            {
                pendingMouseRotation = Vector2.zero;
                rotation = Mouse.current != null ? Mouse.current.delta.ReadValue() * mouseSensitivity : Vector2.zero;
                if (Gamepad.current != null)
                    rotation += Gamepad.current.rightStick.ReadValue() * (gamepadLookSpeed * Time.deltaTime);
                rotation.y = -rotation.y;
            }
            float nextYaw = yaw + rotation.x;
            float nextPitch = pitch + rotation.y;
            yaw = ClampLook(nextYaw, yawLimit);
            pitch = ClampLook(nextPitch, pitchLimit);
            // Discard motion beyond the seat limit, so reversing the mouse responds immediately.
            if (yaw != nextYaw) pendingMouseRotation.x = 0f;
            if (pitch != nextPitch) pendingMouseRotation.y = 0f;
        }

        private void ConfigureDrivingAudio()
        {
            if (drivingInteriorClip == null) return;
            if (drivingInteriorAudio == null)
            {
                var host = new GameObject("DrivingInteriorAudio");
                host.transform.SetParent(transform, false);
                drivingInteriorAudio = host.AddComponent<AudioSource>();
            }
            drivingInteriorAudio.playOnAwake = false;
            drivingInteriorAudio.loop = true;
            drivingInteriorAudio.spatialBlend = 0f;
            drivingInteriorAudio.dopplerLevel = 0f;
            drivingInteriorAudio.clip = drivingInteriorClip;
            drivingInteriorAudio.volume = 0f;
        }

        private void UpdateDrivingAudio()
        {
            if (drivingInteriorAudio == null || drivingInteriorClip == null) return;
            // Keep the opening screen fade, and gradually silence the driving
            // ambience with actual vehicle speed as the brakes bring it to rest.
            float motionGain = IsMoving && speed > 0f ? Mathf.Clamp01(CurrentSpeed / speed) : 0f;
            drivingInteriorAudio.volume = drivingInteriorVolume
                * EveningDriveIntro.GetGameRevealAmount(gameObject.scene) * motionGain;
            // Audio does not follow Time.timeScale automatically. Keep the intro
            // silent until the existing text-fade cue releases vehicle movement.
            bool shouldPlay = IsMoving && speed > 0f && Time.timeScale > 0f;
            if (shouldPlay && !drivingInteriorAudio.isPlaying)
            {
                if (drivingAudioPaused) drivingInteriorAudio.UnPause();
                else drivingInteriorAudio.Play();
                drivingAudioPaused = false;
            }
            else if (!shouldPlay && drivingInteriorAudio.isPlaying)
            {
                drivingInteriorAudio.Pause();
                drivingAudioPaused = true;
            }
        }

        private void UpdatePassengerPose()
        {
            if (driverSeat == null || playerRoot == null || playerCamera == null) return;
            yaw = ClampLook(yaw, yawLimit);
            pitch = ClampLook(pitch, pitchLimit);
            playerRoot.rotation = driverSeat.rotation;
            playerRoot.position = driverSeat.position - playerRoot.TransformVector(cameraOffsetInPlayer);
            playerCamera.transform.SetPositionAndRotation(driverSeat.position,
                driverSeat.rotation * Quaternion.Euler(pitch, yaw, 0f));
            playerCamera.fieldOfView = fieldOfView;
            playerCamera.nearClipPlane = nearClipPlane;
        }

        public static Vector3 Advance(Vector3 current, Vector3 destination, float metresPerSecond, float deltaTime)
            => Vector3.MoveTowards(current, destination, Mathf.Max(0f, metresPerSecond) * Mathf.Max(0f, deltaTime));
        public static float ClampLook(float angle, float limit)
            => Mathf.Clamp(angle, -Mathf.Max(0f, limit), Mathf.Max(0f, limit));

        [ContextMenu("Restart Ride (Play Mode)")]
        public void RestartRide()
        {
            if (!Application.isPlaying || !initialized) return;
            route = intermediateRoutePoints != null && intermediateRoutePoints.Length > 0
                ? new PassengerRidePath(startPosition, intermediateRoutePoints, endPosition, cornerRadius) : null;
            routeDistance = 0f;
            arrivalStop = null;
            CurrentSpeed = 0f;
            transform.position = startPosition;
            if (route != null)
            {
                route.Evaluate(0f, out _, out var forward);
                if (forward.sqrMagnitude > .000001f) transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
            yaw = pitch = 0f;
            pendingMouseRotation = Vector2.zero;
            HasArrived = false;
            IsMoving = true;
        }

        [ContextMenu("Pause Ride (Play Mode)")]
        public void PauseRide() { IsMoving = false; CurrentSpeed = 0f; }
        [ContextMenu("Resume Ride (Play Mode)")]
        public void ResumeRide() { if (initialized) IsMoving = !HasArrived; }

        private void OnDisable()
        {
            pendingMouseRotation = Vector2.zero;
            StopAllCoroutines();
            CleanupExitUI();
            if (drivingInteriorAudio != null && drivingInteriorClip != null) drivingInteriorAudio.Stop();
            drivingAudioPaused = false;
            CurrentSpeed = 0f;
            if (!initialized) return;
            for (int i = 0; i < suspendedControls.Length; i++)
                if (suspendedControls[i] != null && suspendedControls[i] != this)
                    suspendedControls[i].enabled = controlStates[i];
            for (int i = 0; i < characterControllers.Length; i++)
                if (characterControllers[i] != null) characterControllers[i].enabled = controllerStates[i];
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = originalFov;
                playerCamera.nearClipPlane = originalNearClip;
            }
            if (hasExited && enableFlashlightAfterExit) SetRidingFlashlightState(true);
        }

        private void OnDrawGizmosSelected()
        {
            DrawExitGizmo();
            Gizmos.color = Color.cyan;
            if (intermediateRoutePoints != null && intermediateRoutePoints.Length > 0)
            {
                var preview = new PassengerRidePath(startPosition, intermediateRoutePoints, endPosition, cornerRadius);
                for (int i = 1; i < preview.SampleCount; i++) Gizmos.DrawLine(preview.GetSample(i - 1), preview.GetSample(i));
                foreach (var corner in intermediateRoutePoints) Gizmos.DrawWireSphere(corner, .35f);
            }
            else Gizmos.DrawLine(startPosition, endPosition);
            Gizmos.DrawWireSphere(startPosition, 0.3f);
            Gizmos.DrawWireSphere(endPosition, 0.3f);
            if (driverSeat == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(driverSeat.position, 0.06f);
            Gizmos.DrawRay(driverSeat.position, driverSeat.rotation * Quaternion.Euler(0, -yawLimit, 0) * Vector3.forward * 2);
            Gizmos.DrawRay(driverSeat.position, driverSeat.rotation * Quaternion.Euler(0, yawLimit, 0) * Vector3.forward * 2);
        }
    }
}
