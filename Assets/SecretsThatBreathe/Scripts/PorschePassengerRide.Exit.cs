using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    public sealed partial class PorschePassengerRide
    {
        [Header("Exit after arrival")]
        public bool allowExitAfterArrival = true;
        [Header("Saved UI under SeatingCanvas — edit text and style there")]
        public GameObject exitUI;
        public CanvasGroup exitBlack;
        public TMP_Text exitLabel;
        public AudioSource doorAudio;
        public AudioClip openDoorClip;
        public AudioClip closeDoorClip;
        [Min(0f)] public float exitPromptDelay = 2f;
        [Min(.1f)] public float exitFadeOutSeconds = .85f;
        [Min(0f)] public float exitBlackHoldSeconds = .65f;
        [Min(.1f)] public float exitFadeInSeconds = 1.1f;
        [Range(0f, 1f)] public float doorVolume = .7f;
        [Tooltip("Optional exit marker. Position is the player's feet; blue arrow is the facing direction. Parent to the car to move with it, or use a scene object for a fixed destination.")]
        public Transform exitPoint;
        [Tooltip("Used only when Exit Point is unassigned; position relative to the car.")]
        public Vector3 exitLocalPosition = new Vector3(-1.8f, 0f, .12f);
        [Tooltip("Enable F to toggle the existing phone flashlight after the exit reveal finishes.")]
        public bool enableFlashlightAfterExit = true;
        [Header("Exterior sound after leaving the car")]
        public VillageExteriorAmbience exteriorAmbience;
        public bool HasExited => hasExited;
        private bool exitInProgress, passengerDetached, hasExited;
        private float stoppedSeconds;

        private void UpdateExitInteraction()
        {
            if (!allowExitAfterArrival || exitInProgress) return;
            if (!HasArrived || IsMoving || EveningDriveIntro.IsInputBlocked(gameObject.scene))
            {
                stoppedSeconds = 0f;
                if (exitLabel != null) exitLabel.gameObject.SetActive(false);
                return;
            }
            stoppedSeconds += Time.deltaTime;
            if (stoppedSeconds < exitPromptDelay) return;
            if (!EnsureExitUI()) return;
            exitLabel.gameObject.SetActive(true);
            if (Application.isFocused && Cursor.lockState == CursorLockMode.Locked
                && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                StartCoroutine(ExitCar());
        }

        private bool EnsureExitUI()
        {
            if (exitUI == null || exitBlack == null || exitLabel == null || doorAudio == null)
            {
                Debug.LogError("Assign the saved CarExitUI under SeatingCanvas on the ride component.", this);
                allowExitAfterArrival = false;
                return false;
            }
            exitUI.SetActive(true);
            doorAudio.volume = doorVolume;
            return true;
        }

        private IEnumerator ExitCar()
        {
            exitInProgress = true;
            exitLabel.gameObject.SetActive(false);
            // The latch/door starts immediately; the physical move is hidden at full black.
            if (openDoorClip != null) doorAudio.PlayOneShot(openDoorClip);
            yield return FadeExit(1f, exitFadeOutSeconds);
            if (!TryFindExitPosition(out var position))
            {
                Debug.LogWarning("No clear ground beside the car; exit cancelled. Move the exit offset or clear the roadside.", this);
                yield return FadeExit(0f, exitFadeInSeconds);
                stoppedSeconds = 0f;
                exitInProgress = false;
                yield break;
            }
            passengerDetached = true;
            float exitYaw = exitPoint != null ? exitPoint.eulerAngles.y : transform.eulerAngles.y;
            PlaceWalkingBody(position, Quaternion.Euler(0f, exitYaw, 0f));
            Vector3 settleOffset = SettleWalkingBody();
            float exitPitch = exitPoint != null ? Mathf.DeltaAngle(0f, exitPoint.eulerAngles.x) : 0f;
            var walking = playerRoot.GetComponentInChildren<StarterAssets.FirstPersonController>(true);
            if (walking != null) walking.ResetAfterTeleport(exitPitch);
            // The scene camera is a sibling of the walking body, not its child.
            Vector3 eyePosition = walking != null && walking.CinemachineCameraTarget != null
                ? walking.CinemachineCameraTarget.transform.position
                : playerRoot.TransformPoint(cameraOffsetInPlayer) + settleOffset;
            playerCamera.transform.SetPositionAndRotation(eyePosition,
                Quaternion.Euler(walking != null ? Mathf.Clamp(exitPitch, walking.BottomClamp, walking.TopClamp) : exitPitch, exitYaw, 0f));
            playerCamera.fieldOfView = originalFov;
            playerCamera.nearClipPlane = originalNearClip;
            Physics.SyncTransforms();
            if (exteriorAmbience != null) exteriorAmbience.BeginExterior();
            // Finish the opening sound before closing; keep all controls suspended.
            float openRemaining = openDoorClip != null ? openDoorClip.length - exitFadeOutSeconds : 0f;
            yield return new WaitForSeconds(Mathf.Max(exitBlackHoldSeconds, openRemaining + .15f));
            if (closeDoorClip != null) doorAudio.PlayOneShot(closeDoorClip);
            yield return new WaitForSeconds(.15f);
            yield return FadeExit(0f, exitFadeInSeconds);
            ClearWalkingInput();
            hasExited = true;
            // Restore walking after the reveal. The saved UI and AudioSource remain
            // in SeatingCanvas, so the closing sound can finish without being destroyed.
            enabled = false;
        }

        private IEnumerator FadeExit(float target, float seconds)
        {
            float from = exitBlack.alpha;
            for (float t = 0f; t < seconds;)
            {
                t += Time.deltaTime;
                exitBlack.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            exitBlack.alpha = target;
        }

        private bool TryFindExitPosition(out Vector3 position)
        {
            // Prefer the driver's side; use a slightly wider offset if a wheel/body collider overlaps.
            foreach (float extra in exitPoint != null ? new[] { 0f } : new[] { 0f, .4f, .8f })
            {
                Vector3 local = exitLocalPosition;
                local.x += Mathf.Sign(local.x) * extra;
                Vector3 candidate = exitPoint != null ? exitPoint.position : transform.TransformPoint(local);
                var hits = Physics.RaycastAll(candidate + Vector3.up * 2f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(playerRoot) || hit.normal.y < .65f) continue;
                    var controller = characterControllers.Length > 0 ? characterControllers[0] : null;
                    float radius = controller != null ? controller.radius * Mathf.Max(Mathf.Abs(controller.transform.lossyScale.x), Mathf.Abs(controller.transform.lossyScale.z)) : .3f;
                    float height = controller != null ? controller.height * Mathf.Abs(controller.transform.lossyScale.y) : 1.8f;
                    Vector3 centerOffset = controller != null ? controller.transform.TransformVector(controller.center) : Vector3.up * .9f;
                    // The marker denotes feet, while a controller's pivot may be at its centre.
                    Vector3 foot = hit.point + Vector3.up * (.04f + height * .5f - centerOffset.y);
                    Vector3 center = foot + Vector3.up * centerOffset.y;
                    float half = Mathf.Max(0f, height * .5f - radius);
                    bool clear = true;
                    foreach (var collider in Physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half,
                        radius, ~0, QueryTriggerInteraction.Ignore))
                        if (!collider.transform.IsChildOf(playerRoot)) { clear = false; break; }
                    if (clear) { position = foot; return true; }
                }
            }
            position = default;
            return false;
        }

        private void PlaceWalkingBody(Vector3 position, Quaternion rotation)
        {
            Transform body = characterControllers.Length > 0 && characterControllers[0] != null
                ? characterControllers[0].transform : playerRoot;
            // Preserve the existing hierarchy and child offsets. Align the actual walking
            // capsule, then translate its container so its pivot reaches the ground target.
            Quaternion bodyInRoot = Quaternion.Inverse(playerRoot.rotation) * body.rotation;
            playerRoot.rotation = rotation * Quaternion.Inverse(bodyInRoot);
            playerRoot.position += position - body.position;
        }

        private Vector3 SettleWalkingBody()
        {
            if (characterControllers.Length == 0 || characterControllers[0] == null) return Vector3.zero;
            var controller = characterControllers[0];
            Vector3 before = controller.transform.position;
            Physics.SyncTransforms();
            // Resolve the safety clearance using the actual capsule/skin width under black,
            // before revealing the view. Walking input remains disabled throughout.
            controller.enabled = true;
            controller.Move(Vector3.down * .3f);
            controller.enabled = false;
            Physics.SyncTransforms();
            return controller.transform.position - before;
        }

        private void SetRidingFlashlightState(bool available)
        {
            foreach (var phone in playerRoot.GetComponentsInChildren<PhoneFlashlightView>(true))
                phone.enabled = available;
            foreach (var flashlight in playerRoot.GetComponentsInChildren<StarterAssets.FirstPersonFlashlight>(true))
            {
                flashlight.ForceOff();
                flashlight.enabled = available;
            }
        }

        private void ClearWalkingInput()
        {
            foreach (var input in playerRoot.GetComponentsInChildren<StarterAssets.StarterAssetsInputs>(true))
            {
                input.move = input.look = Vector2.zero;
                input.jump = input.sprint = false;
            }
        }

        private void CleanupExitUI()
        {
            if (exitLabel != null) exitLabel.gameObject.SetActive(false);
            if (exitBlack != null) exitBlack.alpha = 0f;
        }

        private void DrawExitGizmo()
        {
            Vector3 position = exitPoint != null ? exitPoint.position : transform.TransformPoint(exitLocalPosition);
            Quaternion facing = Quaternion.Euler(0f, exitPoint != null ? exitPoint.eulerAngles.y : transform.eulerAngles.y, 0f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(position + Vector3.up * .08f, .3f);
            Gizmos.DrawLine(position, position + Vector3.up * 1.8f);
            Gizmos.color = Color.blue;
            Vector3 tip = position + facing * Vector3.forward;
            Gizmos.DrawLine(position, tip);
            Gizmos.DrawLine(tip, tip + facing * new Vector3(-.18f, 0f, -.25f));
            Gizmos.DrawLine(tip, tip + facing * new Vector3(.18f, 0f, -.25f));
        }
    }
}
