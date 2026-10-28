using UnityEngine;

namespace SecretsThatBreathe
{
    // Integrate a smooth velocity curve analytically so the stop reaches the
    // destination in finite time and does not depend on frame-rate integration.
    public sealed class SmoothRideStop
    {
        private readonly float distance, initialSpeed;
        private float elapsed;
        public float Duration { get; }
        public float RemainingDistance { get; private set; }
        public float Speed { get; private set; }
        public bool IsComplete => elapsed >= Duration;

        public SmoothRideStop(float remainingDistance, float currentSpeed)
        {
            distance = Mathf.Max(0f, remainingDistance);
            initialSpeed = Mathf.Max(0f, currentSpeed);
            Duration = initialSpeed > 0f ? 2f * distance / initialSpeed : 0f;
            RemainingDistance = distance;
            Speed = initialSpeed;
        }

        public void Advance(float deltaTime)
        {
            elapsed = Mathf.Min(Duration, elapsed + Mathf.Max(0f, deltaTime));
            float t = Duration > 0f ? elapsed / Duration : 1f;
            float remaining = 1f - t;
            // Integral of v(t) = v0 * (1 - smoothstep(t)). Both acceleration
            // endpoints are zero: gentle brake application and gentle settling.
            RemainingDistance = distance * remaining * remaining * remaining * (1f + t);
            Speed = initialSpeed * (1f - t * t * (3f - 2f * t));
        }
    }
}
