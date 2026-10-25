using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    [DefaultExecutionOrder(20000)]
    [DisallowMultipleComponent]
    public sealed class PorschePassengerRide : MonoBehaviour
    {
        [Header("Route (world positions)")]
        public Vector3 startPosition = new Vector3(209.3862f, 5.09909f, 142.75f);
        public Vector3 endPosition = new Vector3(209.3862f, 5.09909f, 284.65f);
        [Tooltip("Metres per second. Set to zero to pause. Can be changed during Play.")]
        [Min(0f)] public float speed = 1f;
        public bool playOnStart = true;

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

        [Header("Controls suspended only while riding")]
        [Tooltip("Walking, seating and Cinemachine controls. Phone flashlight stays enabled.")]
        public Behaviour[] suspendedControls = new Behaviour[0];

        public bool IsMoving { get; private set; }
        public bool HasArrived { get; private set; }
        public float RemainingDistance => Vector3.Distance(transform.position, endPosition);
        private bool initialized;
        private bool[] controlStates;
        private CharacterController[] characterControllers;
        private bool[] controllerStates;
        private Vector3 cameraOffsetInPlayer;
        private float originalFov, originalNearClip, yaw, pitch;

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
            if (!initialized || Time.timeScale <= 0f) return;
            if (IsMoving)
            {
                transform.position = Advance(transform.position, endPosition, speed, Time.deltaTime);
                HasArrived = (transform.position - endPosition).sqrMagnitude < 0.000001f;
                if (HasArrived) { transform.position = endPosition; IsMoving = false; }
            }
            if (Cursor.lockState != CursorLockMode.Locked || !Application.isFocused) return;
            Vector2 look = Mouse.current != null ? Mouse.current.delta.ReadValue() * mouseSensitivity : Vector2.zero;
            if (Gamepad.current != null)
                look += Gamepad.current.rightStick.ReadValue() * (gamepadLookSpeed * Time.deltaTime);
            yaw = ClampLook(yaw + look.x, yawLimit);
            pitch = ClampLook(pitch - look.y, pitchLimit);
        }

        private void LateUpdate()
        {
            if (initialized) UpdatePassengerPose();
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
            transform.position = startPosition;
            yaw = pitch = 0f;
            HasArrived = false;
            IsMoving = true;
        }

        [ContextMenu("Pause Ride (Play Mode)")]
        public void PauseRide() { IsMoving = false; }
        [ContextMenu("Resume Ride (Play Mode)")]
        public void ResumeRide() { if (initialized) IsMoving = !HasArrived; }

        private void OnDisable()
        {
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
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(startPosition, endPosition);
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
