using System.Collections;
using UnityEngine;

namespace SecretsThatBreathe
{
    public sealed partial class PorschePassengerRide
    {
        private bool boardingForTransition;
        // Called by the saved encounter controller. Leave the screen black and the
        // walking controls suspended; the next scene is intentionally not chosen yet.
        public IEnumerator BoardForSceneTransition()
        {
            if (!hasExited || boardingForTransition || !EnsureExitUI()) yield break;
            boardingForTransition = true;
            exitInProgress = true;
            ClearWalkingInput(); SetRidingFlashlightState(false);
            foreach (var control in suspendedControls) if (control != null && control != this) control.enabled = false;
            if (openDoorClip != null) doorAudio.PlayOneShot(openDoorClip);
            yield return FadeExit(1f, exitFadeOutSeconds);
            foreach (var controller in characterControllers) if (controller != null) controller.enabled = false;
            hasExited = false; passengerDetached = false; IsMoving = false;
            UpdatePassengerPose();
            yield return new WaitForSeconds(exitBlackHoldSeconds);
            if (closeDoorClip != null) doorAudio.PlayOneShot(closeDoorClip);
            exitBlack.alpha = 1f;
        }
    }
}
