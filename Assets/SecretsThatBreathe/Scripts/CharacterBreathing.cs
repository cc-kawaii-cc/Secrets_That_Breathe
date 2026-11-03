using UnityEngine;

namespace SecretsThatBreathe
{
    // Visible breathing layered over whatever the Animator is playing (talking, idle, or paused).
    // On each breath in, the chest lifts and moves forward, the shoulders rise a little and the
    // spine leans back slightly; the neck and head cancel most of that so the gaze stays steady.
    // Arms are untouched: they are children of the shoulders, so they follow naturally.
    [DefaultExecutionOrder(30500)]
    [DisallowMultipleComponent]
    public sealed class CharacterBreathing : MonoBehaviour
    {
        [Header("Rhythm")]
        [Tooltip("Resting adults breathe about 12-16 times a minute.")]
        [Range(6f, 40f)] public float breathsPerMinute = 16f;
        [Tooltip("Share of one breath spent breathing in; the breath out is slower.")]
        [Range(.25f, .6f)] public float inhaleFraction = .4f;
        [Tooltip("How much the speed and depth of each breath wander. 0 is a metronome.")]
        [Range(0f, .5f)] public float irregularity = .1f;
        [Header("Depth at the top of a breath")]
        [Tooltip("Metres the chest moves forward.")]
        [Range(0f, .15f)] public float chestForward = .025f;
        [Tooltip("Metres the chest moves up.")]
        [Range(0f, .15f)] public float chestLift = .018f;
        [Tooltip("Degrees each shoulder rises.")]
        [Range(0f, 20f)] public float shoulderLiftDegrees = 5f;
        [Range(0f, 10f)] public float spineDegrees = 1.5f;
        [Range(0f, 10f)] public float chestDegrees = 2.5f;
        [Tooltip("How much of the chest movement the neck and head cancel out. 1 keeps the head perfectly still.")]
        [Range(0f, 1f)] public float headStabilize = .5f;
        [Tooltip("Multiplies every depth above. Raise it to make the breathing easier to see.")]
        [Range(0f, 10f)] public float intensity = 1f;
        [Header("Gaze (driven by PoliceEncounterGestures while the officer talks into the radio)")]
        [Range(0f, 1f)] public float neckShareOfGaze = .35f;
        [Tooltip("Prints which bones were found (once, when the scene starts) to the Console.")]
        public bool logOnStart = true;

        private enum Slot { Spine, Chest, Neck, Head, ShoulderA, ShoulderB }
        private Transform[] bones;
        private Vector3[] basePosition;
        private Quaternion[] baseRotation;
        private bool applied, ready, failed;
        // Gaze: turn the head toward a world point. Set every frame by SetLook; weight 0 means no gaze.
        private Vector3 lookPoint, headForwardLocal;
        private float lookWeight, lookMaxDegrees;
        private bool hasHeadForward;

        public void SetLook(Vector3 worldPoint, float weight, float maxDegrees)
        {
            lookPoint = worldPoint;
            lookWeight = Mathf.Clamp01(weight);
            lookMaxDegrees = maxDegrees;
        }
        private float phase, seed, clock;

        // Adds breathing to a character root once. Safe to call repeatedly.
        public static CharacterBreathing Attach(Transform characterRoot)
        {
            if (characterRoot == null) return null;
            var existing = characterRoot.GetComponent<CharacterBreathing>();
            return existing != null ? existing : characterRoot.gameObject.AddComponent<CharacterBreathing>();
        }

        private void Start() => Initialize();

        // Also runs lazily from LateUpdate: if Unity recompiles scripts during Play Mode, private fields are
        // lost but Start is not called again.
        private void Initialize()
        {
            seed = Random.value * 100f;
            phase = Random.value;
            Animator active = null;
            foreach (var animator in GetComponentsInChildren<Animator>())
                if (animator.enabled && animator.gameObject.activeInHierarchy) { active = animator; break; }
            Transform skeletonRoot = active != null ? active.transform : transform;

            Transform spine = null, chest = null, neck = null, head = null, highestSpine = null;
            var shoulders = new System.Collections.Generic.List<Transform>();
            int spineDepth = int.MaxValue, chestDepth = int.MaxValue, neckDepth = int.MaxValue, headDepth = int.MaxValue, highestDepth = -1;
            foreach (var bone in skeletonRoot.GetComponentsInChildren<Transform>(true))
            {
                string n = bone.name.ToLowerInvariant();
                if (n.Contains("twist") || n.Contains("_end") || n.EndsWith("end") || n.Contains("top")) continue;
                int depth = Depth(bone);
                if (n.Contains("chest")) { if (depth < chestDepth) { chest = bone; chestDepth = depth; } }
                else if (n.Contains("spine"))
                {
                    if (depth < spineDepth) { spine = bone; spineDepth = depth; }
                    if (depth > highestDepth) { highestSpine = bone; highestDepth = depth; }
                }
                else if (n.Contains("neck")) { if (depth < neckDepth) { neck = bone; neckDepth = depth; } }
                else if (n == "head" || n.EndsWith(":head") || n.EndsWith("_head") || n.EndsWith(".head")) { if (depth < headDepth) { head = bone; headDepth = depth; } }
                else if (n.StartsWith("shoulder") || n.Contains("clavicle") || n.Contains("_shoulder") || n.Contains(":shoulder")) shoulders.Add(bone);
            }
            if (chest == null) { chest = highestSpine; if (spine == chest) spine = null; }
            if (chest == null)
            {
                Debug.LogWarning("CharacterBreathing found no spine/chest bone on " + name + "; breathing is off.", this);
                failed = true;
                return;
            }
            bones = new Transform[6];
            bones[(int)Slot.Spine] = spine; bones[(int)Slot.Chest] = chest; bones[(int)Slot.Neck] = neck; bones[(int)Slot.Head] = head;
            // Keep the two shallowest shoulder/clavicle bones (one per side).
            shoulders.Sort((x, y) => Depth(x).CompareTo(Depth(y)));
            if (shoulders.Count > 0) bones[(int)Slot.ShoulderA] = shoulders[0];
            if (shoulders.Count > 1) bones[(int)Slot.ShoulderB] = shoulders[1];
            basePosition = new Vector3[bones.Length];
            baseRotation = new Quaternion[bones.Length];
            if (head != null)
            {
                // The face looks along the character's forward direction at rest.
                headForwardLocal = Quaternion.Inverse(head.rotation) * transform.forward;
                hasHeadForward = true;
            }
            ready = true;
            if (logOnStart)
                Debug.Log("[CharacterBreathing] " + name + " using rig '" + skeletonRoot.name + "': spine=" + Label(spine) + " chest=" + Label(chest)
                    + " neck=" + Label(neck) + " head=" + Label(head) + " shoulders=" + Label(bones[4]) + "/" + Label(bones[5]), this);
        }

        private static string Label(Transform t) => t != null ? t.name : "none";
        private static int Depth(Transform t) { int d = 0; while (t.parent != null) { t = t.parent; d++; } return d; }

        // Unity can leave a pose untouched when an animation has a constant curve, so the previous
        // breath is removed before the Animator runs; otherwise it would build up frame after frame.
        private void Update() => RemoveBreath();
        private void OnDisable() => RemoveBreath();

        private void RemoveBreath()
        {
            if (!applied || bones == null) return;
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) { bones[i].localPosition = basePosition[i]; bones[i].localRotation = baseRotation[i]; }
            applied = false;
        }

        private void LateUpdate()
        {
            if (!ready)
            {
                if (failed) return;
                Initialize();
                if (!ready) return;
            }
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused: keep the last pose rather than drifting
            clock += dt;
            // Breath rate and depth wander slowly, never repeating exactly.
            float rateWobble = 1f + irregularity * (Mathf.PerlinNoise(seed, clock * .11f) * 2f - 1f);
            float depthWobble = 1f + irregularity * 1.5f * (Mathf.PerlinNoise(seed + 31f, clock * .13f) * 2f - 1f);
            phase = (phase + dt * (breathsPerMinute / 60f) * rateWobble) % 1f;
            float s = BreathCurve(phase) * intensity * depthWobble;

            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) { basePosition[i] = bones[i].localPosition; baseRotation[i] = bones[i].localRotation; }
            applied = true;

            Vector3 right = transform.right, up = transform.up, forward = transform.forward;
            Transform spine = bones[(int)Slot.Spine], chest = bones[(int)Slot.Chest], neck = bones[(int)Slot.Neck], head = bones[(int)Slot.Head];
            // Leaning back as the ribs lift is a rotation about the left-right axis (negative = back).
            if (spine != null) Rotate(spine, right, -spineDegrees * s);
            if (chest != null)
            {
                Rotate(chest, right, -chestDegrees * s);
                chest.position += (forward * chestForward + up * chestLift) * s;
            }
            // Parents move before children, so shoulders, neck and head already follow the chest.
            Vector3 chestOffset = (forward * chestForward + up * chestLift) * s;
            for (int i = (int)Slot.ShoulderA; i <= (int)Slot.ShoulderB; i++)
            {
                Transform shoulder = bones[i];
                if (shoulder == null) continue;
                // Rotating the right-hand shoulder about +forward lifts it; the left-hand one lifts the other way.
                float side = Vector3.Dot(shoulder.position - transform.position, right) >= 0f ? 1f : -1f;
                Rotate(shoulder, forward, side * shoulderLiftDegrees * s);
            }
            float cancel = (spineDegrees + chestDegrees) * s * headStabilize;
            if (neck != null)
            {
                neck.position -= chestOffset * headStabilize;
                Rotate(neck, right, cancel * .45f);
            }
            if (head != null) Rotate(head, right, cancel * .55f);
            ApplyGaze(neck, head);
        }

        // Turns the face toward lookPoint, most of it at the head and some at the neck, up to lookMaxDegrees.
        private void ApplyGaze(Transform neck, Transform head)
        {
            if (head == null || !hasHeadForward || lookWeight <= 0f) return;
            Vector3 toTarget = lookPoint - head.position;
            if (toTarget.sqrMagnitude < .0001f) return;
            Vector3 faceNow = head.rotation * headForwardLocal;
            Quaternion turn = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(faceNow, toTarget.normalized), lookMaxDegrees);
            turn = Quaternion.Slerp(Quaternion.identity, turn, lookWeight);
            // The neck carries the head with it, so the head only adds the remainder.
            Quaternion neckTurn = Quaternion.Slerp(Quaternion.identity, turn, neckShareOfGaze);
            if (neck != null) neck.rotation = neckTurn * neck.rotation;
            Quaternion headTurn = Quaternion.Slerp(Quaternion.identity, turn, 1f - neckShareOfGaze);
            head.rotation = headTurn * head.rotation;
        }

        private static void Rotate(Transform bone, Vector3 axis, float degrees)
            => bone.rotation = Quaternion.AngleAxis(degrees, axis) * bone.rotation;

        // 0 at the end of a breath out, 1 at the top of a breath in. Smooth at both ends,
        // with a short rest after the exhale.
        private float BreathCurve(float p)
        {
            float inhale = Mathf.Clamp(inhaleFraction, .25f, .6f);
            if (p < inhale) return Mathf.SmoothStep(0f, 1f, p / inhale);
            float exhaleEnd = .92f;
            float x = Mathf.Clamp01((p - inhale) / Mathf.Max(.05f, exhaleEnd - inhale));
            return 1f - Mathf.SmoothStep(0f, 1f, x);
        }
    }
}
