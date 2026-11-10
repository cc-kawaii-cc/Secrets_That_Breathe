using UnityEngine;

namespace SecretsThatBreathe
{
    public sealed partial class PorschePassengerRide
    {
        public bool SkipToExitForTesting()
        {
            if (!PrepareCheckpointForTesting()) return false;
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
                exitBlack.blocksRaycasts = false;
                exitLabel.gameObject.SetActive(true);
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return true;
        }

        public bool SkipToDriveForTesting()
        {
            if (!PrepareCheckpointForTesting()) return false;
            RestartRide();
            ClearWalkingInput();
            UpdatePassengerPose();
            Physics.SyncTransforms();
            stoppedSeconds = 0f;
            if (EnsureExitUI())
            {
                exitBlack.alpha = 0f;
                exitBlack.blocksRaycasts = false;
                exitLabel.gameObject.SetActive(false);
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return true;
        }

        // F3 in the midnight scene: arrive and get out straight away (the drive is skipped), so the debris checkpoint can start.
        public bool ExitNowForTesting()
        {
            if (hasExited || exitInProgress) return true;
            if (!SkipToExitForTesting()) return false;
            if (exitLabel != null) exitLabel.gameObject.SetActive(false);
            StartCoroutine(ExitCar());
            return true;
        }

        private bool PrepareCheckpointForTesting()
        {
            if (!Application.isPlaying || !initialized) return false;
            EveningDriveIntro.SkipForTesting(gameObject.scene);
            EveningPoliceEncounter.ResetForTesting(gameObject.scene);
            StopAllCoroutines();
            boardingForTransition = false;
            exitInProgress = passengerDetached = hasExited = false;
            enabled = true;
            foreach (var control in suspendedControls)
                if (control != null && control != this) control.enabled = false;
            foreach (var controller in characterControllers)
                if (controller != null) controller.enabled = false;
            SetRidingFlashlightState(false);
            if (exteriorAmbience != null) exteriorAmbience.StopExteriorForTesting();
            if (doorAudio != null) doorAudio.Stop();
            drivingAudioPaused = false;
            return true;
        }
    }
}
