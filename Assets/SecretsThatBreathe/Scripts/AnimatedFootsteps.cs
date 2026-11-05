using UnityEngine;

namespace SecretsThatBreathe
{
    // Footstep sounds for a character that walks along a route with a looping walk animation (no CharacterController).
    // A step is played each time the walk cycle reaches a foot-landing phase, so the sound follows the animation:
    // it stops when the Animator is frozen and speeds up or slows down with the Animator speed.
    [DisallowMultipleComponent]
    public sealed class AnimatedFootsteps : MonoBehaviour
    {
        public Animator animator;
        [Tooltip("3D sound source on this character, so the steps are heard coming from where he walks.")]
        public AudioSource audioSource;
        [Tooltip("Short recordings of one footstep each.")]
        public AudioClip[] footstepClips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Points in the walk cycle (0-1) where a foot lands. SuitWalk is two steps per cycle: left at 0, right at 0.5.")]
        public float[] footLandingPhases = { 0f, .5f };
        [Tooltip("Shifts every landing slightly later in the cycle (fraction of a cycle).")]
        [Range(0f, .2f)] public float phaseDelay = .03f;
        public Vector2 pitchRange = new Vector2(.9f, 1f);
        [Tooltip("No steps while the Animator runs slower than this (it is frozen with speed 0 while he waits).")]
        [Min(0f)] public float minimumAnimatorSpeed = .1f;

        // Test switch (off): logs every step to the Console so the footsteps can be confirmed without listening.
        public static bool LogSteps = false;

        private float previousTime;
        private bool hasPrevious;
        private int lastClip = -1;

        private void OnEnable() => hasPrevious = false;

        private void Update()
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.speed < minimumAnimatorSpeed
                || Time.timeScale <= 0f || audioSource == null)
            { hasPrevious = false; return; }
            float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            if (!hasPrevious) { previousTime = time; hasPrevious = true; return; }
            if (time < previousTime) previousTime = time;   // the animation was restarted
            if (footLandingPhases != null)
                foreach (float phase in footLandingPhases)
                {
                    float landing = phase + phaseDelay;
                    if (Mathf.FloorToInt(time - landing) != Mathf.FloorToInt(previousTime - landing)) PlayStep();
                }
            previousTime = time;
        }

        private void PlayStep()
        {
            if (footstepClips == null || footstepClips.Length == 0) return;
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
            if (LogSteps) Debug.Log("[AnimatedFootsteps] " + name + " step " + footstepClips[selected].name + " t=" + Time.time.ToString("F2"), this);
        }

        private void OnDisable()
        {
            hasPrevious = false;
            if (audioSource != null) audioSource.Stop();
        }
    }
}
