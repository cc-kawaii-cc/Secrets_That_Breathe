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
        [Header("Ground surface clips (optional)")]
        [Tooltip("Used on colliders whose object or parent name contains Road.")]
        public AudioClip[] roadClips;
        [Tooltip("Used on TerrainCollider surfaces.")]
        public AudioClip[] terrainClips;
        public LayerMask surfaceLayers = ~0;
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
        private AudioClip[] lastClips;
        private readonly RaycastHit[] surfaceHits = new RaycastHit[32];

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
            if (input != null && input.sprint && (movement == null || movement.EnableSprint))
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
            AudioClip[] clips = GetSurfaceClips();
            if (audioSource == null || clips == null || clips.Length == 0) return;
            if (clips != lastClips) { lastClip = -1; lastClips = clips; }
            int validCount = 0;
            for (int i = 0; i < clips.Length; i++)
                if (clips[i] != null && i != lastClip) validCount++;
            int selected = -1;
            if (validCount > 0)
            {
                int choice = Random.Range(0, validCount);
                for (int i = 0; i < clips.Length; i++)
                    if (clips[i] != null && i != lastClip && choice-- == 0) { selected = i; break; }
            }
            else if (lastClip >= 0 && lastClip < clips.Length && clips[lastClip] != null) selected = lastClip;
            if (selected < 0) return;
            lastClip = selected;
            audioSource.pitch = Random.Range(Mathf.Max(.1f, Mathf.Min(pitchRange.x, pitchRange.y)),
                Mathf.Max(.1f, Mathf.Max(pitchRange.x, pitchRange.y)));
            audioSource.PlayOneShot(clips[selected], volume);
        }
        private AudioClip[] GetSurfaceClips()
        {
            if (body == null || ((roadClips == null || roadClips.Length == 0)
                && (terrainClips == null || terrainClips.Length == 0))) return footstepClips;
            Bounds bounds = body.bounds;
            int count = Physics.RaycastNonAlloc(bounds.center, Vector3.down, surfaceHits,
                bounds.extents.y + .4f, surfaceLayers, QueryTriggerInteraction.Ignore);
            Collider ground = null;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = surfaceHits[i].collider;
                if (candidate == null || candidate == body || candidate.transform.IsChildOf(transform)) continue;
                if (surfaceHits[i].distance >= nearest) continue;
                nearest = surfaceHits[i].distance;
                ground = candidate;
            }
            // Resolve the nearest supporting collider first: a road can sit above terrain.
            if (ground == null) return footstepClips;
            if (ground is TerrainCollider)
                return terrainClips != null && terrainClips.Length > 0 ? terrainClips : footstepClips;
            for (Transform parent = ground.transform; parent != null; parent = parent.parent)
                if (parent.name.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return roadClips != null && roadClips.Length > 0 ? roadClips : footstepClips;
            return footstepClips;
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
