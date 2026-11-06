using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SecretsThatBreathe
{
    public sealed partial class PorschePassengerRide
    {
        private RenderTexture driveWarmupTarget;
        public void CancelDriveWarmup()
        {
            if (driveWarmupTarget == null) return;
            RenderTexture.ReleaseTemporary(driveWarmupTarget);
            driveWarmupTarget = null;
        }
        // A black UI alone only renders the starting viewpoint. Resources first
        // seen farther down the road otherwise still have to be prepared mid-drive.
        public IEnumerator WarmUpDriveViews(int positions)
        {
            if (!initialized || playerCamera == null || driverSeat == null) yield break;
            var request = new UniversalRenderPipeline.SingleCameraRequest();
            if (!RenderPipeline.SupportsRenderRequest(playerCamera, request)) yield break;
            var target = RenderTexture.GetTemporary(Mathf.Max(16, playerCamera.pixelWidth),
                Mathf.Max(16, playerCamera.pixelHeight), 24,
                playerCamera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            driveWarmupTarget = target;
            request.destination = target;
            Vector3 seatOffset = transform.InverseTransformPoint(driverSeat.position);
            Quaternion seatRotation = Quaternion.Inverse(transform.rotation) * driverSeat.rotation;
            int count = Mathf.Clamp(positions, 2, 24);
            var endOfFrame = new WaitForEndOfFrame();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    float fraction = (float)i / (count - 1);
                    Vector3 position = Vector3.Lerp(startPosition, endPosition, fraction);
                    Vector3 forward = endPosition - startPosition;
                    if (route != null) route.Evaluate(route.Length * fraction, out position, out forward);
                    Quaternion heading = forward.sqrMagnitude > .000001f
                        ? Quaternion.LookRotation(forward, Vector3.up) : transform.rotation;
                    for (int side = -1; side <= 1; side++)
                    {
                        yield return endOfFrame;
                        // Restore within the same frame: no vehicle/player teleport,
                        // gameplay triggers, or visible change to the main camera.
                        Transform view = playerCamera.transform;
                        Vector3 savedPosition = view.position;
                        Quaternion savedRotation = view.rotation;
                        try
                        {
                            view.SetPositionAndRotation(position + heading * Vector3.Scale(seatOffset, transform.lossyScale),
                                heading * seatRotation * Quaternion.Euler(0f, side * yawLimit, 0f));
                            RenderPipeline.SubmitRenderRequest(playerCamera, request);
                        }
                        finally { view.SetPositionAndRotation(savedPosition, savedRotation); }
                    }
                }
            }
            finally { CancelDriveWarmup(); }
        }
    }
}
