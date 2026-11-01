using UnityEngine;

namespace SecretsThatBreathe
{
    public sealed partial class PorschePassengerRide
    {
        public bool SkipToExitForTesting()
        {
            if (!Application.isPlaying || !initialized) return false;
            EveningDriveIntro.SkipForTesting(gameObject.scene);
            StopAllCoroutines();
            exitInProgress = passengerDetached = hasExited = false;
            enabled = true;
            // Also re-enter the passenger checkpoint after a completed exit.
            foreach (var control in suspendedControls)
                if (control != null && control != this) control.enabled = false;
            foreach (var controller in characterControllers)
                if (controller != null) controller.enabled = false;
            SetRidingFlashlightState(false);
            if (exteriorAmbience != null) exteriorAmbience.StopExteriorForTesting();
            arrivalStop = null;
            route = intermediateRoutePoints != null && intermediateRoutePoints.Length > 0
                ? new PassengerRidePath(startPosition, intermediateRoutePoints, endPosition, cornerRadius) : null;
            routeDistance = route != null ? route.Length : 0f;
            Vector3 forward = endPosition - startPosition;
            if (route != null) route.Evaluate(route.Length, out _, out forward);
            forward.y = 0f;
            transform.position = endPosition;
            if (forward.sqrMagnitude > .000001f) transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            IsMoving = false;
            HasArrived = true;
            CurrentSpeed = 0f;
            yaw = pitch = 0f;
            pendingMouseRotation = Vector2.zero;
            ClearWalkingInput();
            UpdatePassengerPose();
            Physics.SyncTransforms();
            if (drivingInteriorAudio != null) { drivingInteriorAudio.Stop(); drivingInteriorAudio.volume = 0f; }
            drivingAudioPaused = false;
            if (doorAudio != null) doorAudio.Stop();
            stoppedSeconds = exitPromptDelay;
            if (EnsureExitUI())
            {
                exitBlack.alpha = 0f;
                exitLabel.gameObject.SetActive(true);
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return true;
        }
    }
}
