using StarterAssets;
using UnityEngine;

namespace SecretsThatBreathe
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        public AudioSource audioSource;
        [Tooltip("Short recordings of one footstep each, not a looping walking track.")]
        public AudioClip[] footstepClips;
        [HideInInspector] public int concreteClipsRevision;
        [HideInInspector] public int cadenceSettingsRevision;
        [Range(0f, 1f)] public float volume = .4f;
        [Header("Distance travelled between steps, in metres")]
        [Min(.1f)] public float walkStepDistance = 1.2f;
        [Tooltip("Running step distance. Smaller values make steps faster. Capped at Walk Step Distance so sprint cadence increases with actual speed.")]
        [Min(.1f)] public float sprintStepDistance = .9f;
        [Min(.01f)] public float firstStepDistance = .2f;
        [Header("Small pitch variation")]
        public Vector2 pitchRange = new Vector2(.95f, 1.05f);
        private CharacterController body;
        private FirstPersonController movement;
        private PlayerSeatController seating;
        private StarterAssetsInputs input;
        private Vector3 previousPosition;
        private float travelled;
        private bool walking;
        private int lastClip = -1;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            movement = GetComponent<FirstPersonController>();
            seating = GetComponent<PlayerSeatController>();
            input = GetComponent<StarterAssetsInputs>();
        }
        private void OnEnable() => ResetDistance();
        private void LateUpdate()
        {
            Vector3 delta = transform.position - previousPosition;
            previousPosition = transform.position;
            delta.y = 0f;
            bool canWalk = Time.timeScale > 0f && body != null && body.enabled
                && (movement == null || (movement.isActiveAndEnabled && !movement.MovementLocked))
                && (seating == null || (seating.isActiveAndEnabled && seating.State == PlayerSeatController.SeatState.Exploring));
            bool grounded = body != null && (body.isGrounded || (movement != null && movement.Grounded));
            // Ignore teleports and movement during fades, calls, or seating.
            float maximumSpeed = movement != null ? Mathf.Max(movement.MoveSpeed, movement.SprintSpeed) : 8f;
            float distance = delta.magnitude;
            if (!canWalk || !grounded || distance < .00001f || distance > Mathf.Max(.3f, maximumSpeed * Time.deltaTime * 2f))
            {
                travelled = 0f;
                walking = false;
                return;
            }
            float walkStride = Mathf.Max(.1f, walkStepDistance);
            float sprintStride = Mathf.Clamp(sprintStepDistance, .1f, walkStride);
            // Keep the cadence tied to real travel, including acceleration after Shift.
            float sprintBlend = 0f;
            if (input != null && input.sprint)
            {
                float speed = distance / Mathf.Max(.00001f, Time.deltaTime);
                sprintBlend = movement != null && movement.SprintSpeed > movement.MoveSpeed
                    ? Mathf.InverseLerp(movement.MoveSpeed, movement.SprintSpeed, speed) : 1f;
            }
            float stride = Mathf.Lerp(walkStride, sprintStride, sprintBlend);
            if (!walking)
            {
                walking = true;
                travelled = Mathf.Max(0f, stride - firstStepDistance);
            }
            travelled += distance;
            if (travelled >= Mathf.Max(.1f, stride))
            {
                travelled %= Mathf.Max(.1f, stride);
                PlayStep();
            }
        }
        private void PlayStep()
        {
            if (audioSource == null || footstepClips == null || footstepClips.Length == 0) return;
            int validCount = 0;
            for (int i = 0; i < footstepClips.Length; i++)
                if (footstepClips[i] != null && i != lastClip) validCount++;
            int selected = -1;
            if (validCount > 0)
            {
                int choice = Random.Range(0, validCount);
                for (int i = 0; i < footstepClips.Length; i++)
                    if (footstepClips[i] != null && i != lastClip && choice-- == 0) { selected = i; break; }
            }
            else if (lastClip >= 0 && lastClip < footstepClips.Length && footstepClips[lastClip] != null) selected = lastClip;
            if (selected < 0) return;
            lastClip = selected;
            audioSource.pitch = Random.Range(Mathf.Max(.1f, Mathf.Min(pitchRange.x, pitchRange.y)),
                Mathf.Max(.1f, Mathf.Max(pitchRange.x, pitchRange.y)));
            audioSource.PlayOneShot(footstepClips[selected], volume);
        }
        private void ResetDistance()
        {
            previousPosition = transform.position;
            travelled = 0f;
            walking = false;
        }
        private void OnDisable()
        {
            ResetDistance();
            if (audioSource != null) audioSource.Stop();
        }
    }
}
