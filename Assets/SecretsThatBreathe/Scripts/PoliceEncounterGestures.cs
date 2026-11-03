using UnityEngine;

namespace SecretsThatBreathe
{
    // Generic-rig two-bone arm animation layered over the saved talking Animator.
    [DefaultExecutionOrder(31000)]
    public sealed class PoliceEncounterGestures : MonoBehaviour
    {
        public enum Gesture { Talk, Radio, GunAndPush }
        public Transform policeRoot;
        [Min(.1f)] public float blendSpeed = 5f;
        [Header("Radio gesture timing (seconds)")]
        [Min(.1f)] public float radioRaiseSeconds = 1.1f;
        [Min(.1f)] public float radioLowerSeconds = .9f;
        [Tooltip("Optional wrist target on the officer's shoulder radio. Found by name when left empty.")]
        public Transform radioGrip;
        [Tooltip("Elbow bend direction relative to the officer; keep it below and outside the shoulder.")]
        public Vector3 radioElbowDirection = new Vector3(-.45f, -.8f, .15f);
        public Vector3 radioHandOffset = new Vector3(-.18f, 1.5f, .13f);
        [Header("Natural radio pose (replaces the fixed elbow/wrist solve above when enabled)")]
        [Tooltip("Turn off to get the previous radio arm solve back.")]
        public bool naturalRadioPose = true;
        [Tooltip("On: the hand goes to a spot measured from the shoulder (Radio Shoulder Reach). Off: the hand follows the PoliceRadioGrip object on the radio.")]
        public bool radioUseShoulderReach = true;
        [Tooltip("Wrist position measured from the shoulder joint as fractions of the arm length: x inward (+) / outward (-), y up, z forward. Default: just below the shoulder, slightly toward the chest, held out in front of the radio.")]
        public Vector3 radioShoulderReach = new Vector3(.06f, -.15f, .44f);
        [Tooltip("How far the upper arm angles out from the side of the body (degrees from hanging straight down). The elbow picks the position on its circle that is closest to this angle.")]
        [Range(0f, 90f)] public float radioUpperArmOutDegrees = 21f;
        [Tooltip("How far behind the shoulder's depth (metres) the elbow may sit. 0 keeps it level with the shoulder; larger lets it tuck behind the ribs.")]
        [Range(0f, .2f)] public float radioElbowBackLimit = .03f;
        [Tooltip("The wrist stops this many metres before the radio grip, so the palm lands on the radio instead of the wrist bone.")]
        [Min(0f)] public float radioPalmOffset = .02f;
        [Tooltip("Nudges the hand target in the officer's space (x right, y up, z forward, metres).")]
        public Vector3 radioTargetOffset = Vector3.zero;
        [Tooltip("How much the palm is turned toward the chest while holding the radio. 0 keeps the animated wrist roll.")]
        [Range(0f, 1f)] public float radioPalmRoll = .85f;
        [Range(0f, 120f)] public float radioMaxPalmRollDegrees = 75f;
        [Tooltip("Tiny hand movement while the radio is held, so the pose is not frozen. Metres.")]
        [Range(0f, .02f)] public float radioHoldSway = .004f;
        [Header("Natural open-palm shove (left arm). Reach values are fractions of the arm length from the shoulder.")]
        [Tooltip("Turn off to get the previous push arm back.")]
        public bool naturalPush = true;
        [Tooltip("Hand raised beside the face/shoulder before the shove (x right, y up, z forward).")]
        public Vector3 pushReadyReach = new Vector3(-.06f, .18f, .28f);
        [Tooltip("Hand at full extension, arm almost straight, slightly above the shoulder.")]
        public Vector3 pushExtendReach = new Vector3(-.04f, .08f, .98f);
        [Tooltip("Elbow direction while the hand is raised: down, a little out, forward.")]
        public Vector3 pushReadyElbow = new Vector3(-.2f, -1f, .5f);
        [Tooltip("Elbow direction at full extension.")]
        public Vector3 pushExtendElbow = new Vector3(-.35f, -1f, 0f);
        [Range(0f, 1f)] public float pushPalmFacing = 1f;
        [Header("Radio gaze")]
        [Tooltip("The officer looks down at the radio while holding it.")]
        public bool radioLook = true;
        [Tooltip("Most the head and neck turn toward the radio, in degrees.")]
        [Range(0f, 70f)] public float radioLookDegrees = 35f;
        [Header("Testing")]
        [Tooltip("Tick during Play Mode to hold the radio pose, then change the Natural Radio Pose values above and watch the arm. Untick to lower it.")]
        public bool previewRadio;
        [Tooltip("Prints where the radio grip, wrist and elbow end up (once per radio gesture) to the Console.")]
        public bool logRadioPose = true;
        [Header("Holstered gun and chest push (seconds)")]
        [Min(.1f)] public float gunReachSeconds = .55f;
        [Min(.1f)] public float pushPrepareSeconds = .45f;
        [Min(.1f)] public float pushExtendSeconds = .3f;
        [Min(0f)] public float pushHoldSeconds = .12f;
        [Min(.1f)] public float pushRecoverSeconds = .5f;
        [Min(.1f)] public float gunLowerSeconds = .65f;
        [Tooltip("Optional wrist target on the holstered gun; found by name when empty.")]
        public Transform gunGrip;
        public Vector3 gunHandOffset = new Vector3(.22f, .92f, .05f);
        public Vector3 pushingHandOffset = new Vector3(-.18f, 1.3f, .68f);
        public float PushContactDelay => gunReachSeconds + pushPrepareSeconds + pushExtendSeconds;
        public float GunAndPushDuration => PushContactDelay + pushHoldSeconds + pushRecoverSeconds;
        private Transform upperLeft, lowerLeft, handLeft, upperRight, lowerRight, handRight;
        private Gesture gesture;
        private Gesture targetGesture;
        private float weight;
        private Transform[] armBones;
        private Quaternion[] baseArmPose;
        private bool hasAppliedPose;
        private float gestureTime;
        private enum HandPose { Keep, Radio, Gun, Push }
        // Local direction of the left palm, measured while the arm hangs at the officer's side.
        private Vector3 palmLocal;
        private bool hasPalmLocal;

        // Unity keeps the values already stored on this component in the scene and ignores new defaults written
        // in code. This bumps the stored values to the current defaults once, whenever PoseVersion goes up.
        private const int PoseVersion = 6;
        [SerializeField, HideInInspector] private int poseVersion;
        private void ApplyPoseDefaultsIfOld()
        {
            if (poseVersion >= PoseVersion) return;
            poseVersion = PoseVersion;
            naturalRadioPose = true;
            radioUseShoulderReach = true;
            radioShoulderReach = new Vector3(.06f, -.15f, .44f);
            radioUpperArmOutDegrees = 21f;
            radioElbowBackLimit = .03f;
            radioPalmOffset = .02f;
            radioTargetOffset = Vector3.zero;
            radioPalmRoll = .85f;
            radioMaxPalmRollDegrees = 75f;
            radioHoldSway = .004f;
            radioLook = true;
            radioLookDegrees = 35f;
            naturalPush = true;
            pushReadyReach = new Vector3(-.06f, .18f, .28f);
            pushExtendReach = new Vector3(-.04f, .08f, .98f);
            pushReadyElbow = new Vector3(-.2f, -1f, .5f);
            pushExtendElbow = new Vector3(-.35f, -1f, 0f);
            pushPalmFacing = 1f;
        }
        private void Awake() => ApplyPoseDefaultsIfOld();
        private void OnValidate() => ApplyPoseDefaultsIfOld();

        private bool initialized, previewing, radioLogged;
        private CharacterBreathing breathing;
        private void Start() => Initialize();
        // Also runs lazily: if Unity recompiles scripts during Play Mode, private fields are lost
        // but Start is not called again.
        private void Initialize()
        {
            ApplyPoseDefaultsIfOld();
            if (policeRoot == null) return;
            initialized = true;
            Animator active = null;
            foreach (var animator in policeRoot.GetComponentsInChildren<Animator>())
                if (animator.enabled && animator.gameObject.activeInHierarchy) { active = animator; break; }
            Transform skeleton = active != null ? active.transform : policeRoot;
            foreach (var bone in skeleton.GetComponentsInChildren<Transform>(true))
                switch (bone.name)
                {
                    case "UpperArm.L": upperLeft = bone; break;
                    case "Forearm.L": lowerLeft = bone; break;
                    case "Hand.L": handLeft = bone; break;
                    case "UpperArm.R": upperRight = bone; break;
                    case "Forearm.R": lowerRight = bone; break;
                    case "Hand.R": handRight = bone; break;
                }
            if (handLeft == null || handRight == null)
                Debug.LogWarning("Police gestures need UpperArm/Forearm/Hand .L and .R bones on the active model.", this);
            armBones = new[] { upperLeft, lowerLeft, handLeft, upperRight, lowerRight, handRight };
            baseArmPose = new Quaternion[armBones.Length];
            if (logRadioPose)
                Debug.Log("[PoliceGestures] rig '" + skeleton.name + "': left arm = " + (upperLeft != null ? upperLeft.name : "none") + "/"
                    + (lowerLeft != null ? lowerLeft.name : "none") + "/" + (handLeft != null ? handLeft.name : "none"), this);
            if (handLeft != null)
            {
                // The palm faces the body when the arm hangs, so remember that side of the hand.
                Vector3 inward = policeRoot.right * (Vector3.Dot(handLeft.position - policeRoot.position, policeRoot.right) >= 0f ? -1f : 1f);
                Vector3 local = Vector3.ProjectOnPlane(Quaternion.Inverse(handLeft.rotation) * inward, Vector3.up);
                if (local.sqrMagnitude > .01f) { palmLocal = local.normalized; hasPalmLocal = true; }
            }
        }

        public void SetGesture(Gesture next)
        {
            if (next != Gesture.Talk && next != targetGesture)
            {
                if (next != gesture) weight = 0f;
                gesture = next;
                gestureTime = 0f;
                radioLogged = false;
                if (logRadioPose) Debug.Log("[PoliceGestures] gesture -> " + next, this);
            }
            targetGesture = next;
        }

        // Unity can skip constant animation curves. Remove our previous overlay
        // before Animator evaluation so a held wrist cannot become the next base pose.
        private void Update() => RestoreBasePose();
        private void RestoreBasePose()
        {
            if (!hasAppliedPose || armBones == null) return;
            for (int i = 0; i < armBones.Length; i++)
                if (armBones[i] != null) armBones[i].localRotation = baseArmPose[i];
            hasAppliedPose = false;
        }
        private void OnDisable()
        {
            RestoreBasePose();
            weight = 0f;
            gestureTime = 0f;
            gesture = targetGesture = Gesture.Talk;
        }
        private void LateUpdate()
        {
            if (previewRadio) { SetGesture(Gesture.Radio); previewing = true; }
            else if (previewing) { previewing = false; SetGesture(Gesture.Talk); }
            AdvanceGesture(Time.deltaTime);
        }

        // Also used by the animation preview to evaluate the real runtime pose.
        public void AdvanceGesture(float deltaTime)
        {
            if (!initialized) Initialize();
            bool lowering = targetGesture == Gesture.Talk;
            float speed = gesture == Gesture.Radio
                ? 1f / Mathf.Max(.1f, lowering ? radioLowerSeconds : radioRaiseSeconds)
                : gesture == Gesture.GunAndPush ? 1f / Mathf.Max(.1f, lowering ? gunLowerSeconds : gunReachSeconds) : blendSpeed;
            gestureTime += Mathf.Max(0f, deltaTime);
            weight = Mathf.MoveTowards(weight, lowering ? 0f : 1f, speed * Mathf.Max(0f, deltaTime));
            UpdateGaze();
            if (policeRoot == null || weight <= 0f) return;
            if (armBones != null)
            {
                for (int i = 0; i < armBones.Length; i++)
                    if (armBones[i] != null) baseArmPose[i] = armBones[i].localRotation;
                hasAppliedPose = true;
            }
            if (gesture == Gesture.Radio)
            {
                if (radioGrip == null)
                    foreach (var target in policeRoot.GetComponentsInChildren<Transform>(true))
                        if (target.name == "PoliceRadioGrip") { radioGrip = target; break; }
                if (naturalRadioPose) SolveNaturalRadio();
                else Solve(upperLeft, lowerLeft, handLeft, radioHandOffset, radioElbowDirection, HandPose.Radio);
            }
            else if (gesture == Gesture.GunAndPush)
            {
                if (gunGrip == null)
                    foreach (var target in policeRoot.GetComponentsInChildren<Transform>(true))
                        if (target.name == "PoliceGunGrip") { gunGrip = target; break; }
                Solve(upperRight, lowerRight, handRight, gunHandOffset, new Vector3(.55f, -.6f, -.1f), HandPose.Gun);
                // Reach for the holster first; one open-palm shove, then recover.
                float prepare = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((gestureTime - gunReachSeconds) / Mathf.Max(.1f, pushPrepareSeconds)));
                float extend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((gestureTime - gunReachSeconds - pushPrepareSeconds) / Mathf.Max(.1f, pushExtendSeconds)));
                float recover = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((gestureTime - PushContactDelay - pushHoldSeconds) / Mathf.Max(.1f, pushRecoverSeconds)));
                if (naturalPush)
                {
                    // Fast out, slow in: the hand leaves the face quickly and settles at full reach.
                    float shoveRaw = Mathf.Clamp01((gestureTime - gunReachSeconds - pushPrepareSeconds) / Mathf.Max(.1f, pushExtendSeconds));
                    float shove = 1f - Mathf.Pow(1f - shoveRaw, 2.2f);
                    SolveNaturalPush(weight * prepare * (1f - recover), shove * (1f - .65f * recover));
                }
                else
                {
                    Vector3 ready = new Vector3(-.18f, 1.23f, .25f);
                    Vector3 push = pushingHandOffset;
                    if (upperLeft != null && lowerLeft != null && handLeft != null)
                    {
                        // Clamp the endpoint before interpolation, avoiding the sharp
                        // elbow acceleration caused by hitting full reach mid-thrust.
                        float reach = (Vector3.Distance(upperLeft.position, lowerLeft.position)
                            + Vector3.Distance(lowerLeft.position, handLeft.position)) * .94f;
                        Vector3 desired = policeRoot.position + policeRoot.rotation * push;
                        desired = upperLeft.position + Vector3.ClampMagnitude(desired - upperLeft.position, reach);
                        push = Quaternion.Inverse(policeRoot.rotation) * (desired - policeRoot.position);
                    }
                    Solve(upperLeft, lowerLeft, handLeft, Vector3.Lerp(ready, push, extend),
                        new Vector3(-.3f, -.6f, .1f), HandPose.Push, weight * prepare * (1f - recover));
                }
            }
        }

        // Open-palm shove with the officer's left arm: the hand rises beside the face with the elbow
        // tucked forward, then the arm drives straight out with the palm facing the player, fingers up.
        // armWeight fades the whole gesture in and out; extend (0..1) moves the hand from raised to full reach.
        private void SolveNaturalPush(float armWeight, float extend)
        {
            Transform upper = upperLeft, lower = lowerLeft, hand = handLeft;
            if (upper == null || lower == null || hand == null || armWeight <= 0f) return;
            Quaternion upperPose = upper.localRotation, lowerPose = lower.localRotation, handPose = hand.localRotation;
            float a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, hand.position);
            if (a < .001f || b < .001f) return;
            float armLength = a + b;
            float side = Vector3.Dot(upper.position - policeRoot.position, policeRoot.right) >= 0f ? 1f : -1f;
            Vector3 inward = -policeRoot.right * side;
            Vector3 reach = Vector3.Lerp(pushReadyReach, pushExtendReach, extend);
            // The reach values are written for the left arm (x < 0 is outward); mirror for a right-side rig.
            if (side > 0f) reach.x = -reach.x;
            Vector3 wrist = upper.position + policeRoot.rotation * (reach * armLength);
            Vector3 direction = wrist - upper.position;
            if (direction.sqrMagnitude < .000001f) return;
            float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .001f, a + b - Mathf.Min(a, b) * .02f);
            direction.Normalize();
            wrist = upper.position + direction * distance;
            Vector3 poleLocal = Vector3.Lerp(pushReadyElbow, pushExtendElbow, extend);
            if (side > 0f) poleLocal.x = -poleLocal.x;
            Vector3 bend = Vector3.ProjectOnPlane(policeRoot.rotation * poleLocal, direction);
            if (bend.sqrMagnitude < .0001f) bend = Vector3.ProjectOnPlane(lower.position - upper.position, direction);
            if (bend.sqrMagnitude < .0001f) return;
            bend.Normalize();
            float along = (a * a + distance * distance - b * b) / (2f * distance);
            Vector3 elbow = upper.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, wrist - lower.position) * lower.rotation;
            // Fingers up (leaning slightly toward the player at full reach), palm toward the player.
            Vector3 fingers = (policeRoot.up + policeRoot.forward * (.2f * extend) + inward * (.25f * (1f - extend))).normalized;
            hand.rotation = Quaternion.FromToRotation(hand.up, fingers) * hand.rotation;
            if (hasPalmLocal && pushPalmFacing > 0f)
            {
                Vector3 palm = Vector3.ProjectOnPlane(hand.rotation * palmLocal, fingers);
                Vector3 wanted = Vector3.ProjectOnPlane(policeRoot.forward + inward * (.5f * (1f - extend)), fingers);
                if (palm.sqrMagnitude > .0001f && wanted.sqrMagnitude > .0001f)
                    hand.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(palm, wanted, fingers) * pushPalmFacing, fingers) * hand.rotation;
            }
            Quaternion upperGoal = upper.localRotation, lowerGoal = lower.localRotation, handGoal = hand.localRotation;
            // The elbow and forearm lead; the hand follows a beat later.
            float upperBlend = Mathf.SmoothStep(0f, 1f, armWeight);
            float lowerBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(armWeight * 1.1f));
            float handBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((armWeight - .1f) / .9f));
            upper.localRotation = Quaternion.Slerp(upperPose, upperGoal, upperBlend);
            lower.localRotation = Quaternion.Slerp(lowerPose, lowerGoal, lowerBlend);
            hand.localRotation = Quaternion.Slerp(handPose, handGoal, handBlend);
        }

        private string Fmt(Vector3 world)
        {
            Vector3 local = policeRoot.InverseTransformPoint(world);
            return "(" + local.x.ToString("0.00") + ", " + local.y.ToString("0.00") + ", " + local.z.ToString("0.00") + ")";
        }

        // While the radio is held, ask CharacterBreathing (which owns the head and neck bones) to turn the face
        // toward the radio. It restores those bones every frame, so gaze never builds up.
        private void UpdateGaze()
        {
            if (policeRoot == null) return;
            if (breathing == null) breathing = policeRoot.GetComponent<CharacterBreathing>();
            if (breathing == null) return;
            float look = 0f;
            Vector3 point = policeRoot.position;
            if (radioLook && gesture == Gesture.Radio && weight > 0f)
            {
                // Starts a beat after the hand starts moving, so the head follows the hand.
                look = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((weight - .15f) / .85f));
                if (radioGrip != null) point = radioGrip.position;
                else if (upperLeft != null) point = upperLeft.position + policeRoot.rotation * new Vector3(.1f, -.14f, .1f);
                else look = 0f;
            }
            breathing.SetLook(point, look, radioLookDegrees);
        }

        // The elbow of a two-bone arm can sit anywhere on a circle around the shoulder-wrist line.
        // Choose the point where the upper arm angles out as requested, without the elbow drifting
        // inside the torso or far behind the shoulder.
        private Vector3 PickElbow(Vector3 shoulder, Vector3 wrist, float a, float b, Vector3 outward, float upperArmOutDegrees)
        {
            Vector3 direction = wrist - shoulder;
            float distance = Mathf.Max(.001f, direction.magnitude);
            direction /= distance;
            Vector3 u = Vector3.Cross(direction, policeRoot.up);
            if (u.sqrMagnitude < .0001f) u = Vector3.Cross(direction, policeRoot.forward);
            u.Normalize();
            Vector3 v = Vector3.Cross(direction, u);
            float along = (a * a + distance * distance - b * b) / (2f * distance);
            float radius = Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            Vector3 center = shoulder + direction * along;
            float shoulderDepth = Vector3.Dot(shoulder - policeRoot.position, policeRoot.forward);
            float bestCost = float.MaxValue;
            Vector3 best = center + u * radius;
            const int samples = 360;
            for (int i = 0; i < samples; i++)
            {
                float angle = i * (Mathf.PI * 2f / samples);
                Vector3 candidate = center + (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * radius;
                Vector3 arm = candidate - shoulder;
                if (Vector3.Dot(arm, outward) < -.01f) continue;
                float depth = Vector3.Dot(candidate - policeRoot.position, policeRoot.forward);
                float cost = Mathf.Abs(Vector3.Angle(arm, -policeRoot.up) - upperArmOutDegrees) / 45f
                    + Mathf.Max(0f, shoulderDepth - radioElbowBackLimit - depth) * 10f;
                if (cost < bestCost) { bestCost = cost; best = candidate; }
            }
            return best;
        }

        // Radio pose for the officer's left arm: the upper arm angles out and down, the forearm runs
        // roughly level across the chest and the hand wraps the radio from the outside, palm to the
        // chest, like holding a phone against the chest. The joints arrive in sequence (elbow, then
        // forearm, then hand) instead of all at once.
        private void SolveNaturalRadio()
        {
            Transform upper = upperLeft, lower = lowerLeft, hand = handLeft;
            if (upper == null || lower == null || hand == null) return;
            Quaternion upperPose = upper.localRotation, lowerPose = lower.localRotation, handPose = hand.localRotation;
            float side = Vector3.Dot(upper.position - policeRoot.position, policeRoot.right) >= 0f ? 1f : -1f;
            Vector3 outward = policeRoot.right * side, inward = -outward;
            float a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, hand.position);
            if (a < .001f || b < .001f) return;
            Vector3 grip;
            float palmBack = radioPalmOffset;
            if (radioUseShoulderReach)
            {
                // Left-arm space like the push: mirror x for a right-side rig.
                Vector3 reach = radioShoulderReach;
                if (side > 0f) reach.x = -reach.x;
                grip = upper.position + policeRoot.rotation * (reach * (a + b));
                palmBack = 0f; // the reach point is the wrist itself
            }
            else grip = radioGrip != null ? radioGrip.position : policeRoot.position + policeRoot.rotation * radioHandOffset;
            grip += policeRoot.rotation * radioTargetOffset;
            float hold = Mathf.Clamp01((weight - .9f) * 10f);
            Vector3 sway = (policeRoot.up * Mathf.Sin(gestureTime * 1.45f) + outward * Mathf.Sin(gestureTime * .9f) * .75f) * (radioHoldSway * hold);
            // The hand arrives along the forearm, so the wrist stops short of the grip by half a palm in that
            // direction. The forearm direction depends on the wrist, so solve twice.
            Vector3 along = (inward * .85f + policeRoot.up * .15f).normalized;
            Vector3 elbow = Vector3.zero, wrist = grip;
            for (int pass = 0; pass < 2; pass++)
            {
                wrist = grip - along * palmBack + sway;
                Vector3 toWrist = wrist - upper.position;
                if (toWrist.sqrMagnitude < .000001f) return;
                float reachDistance = Mathf.Clamp(toWrist.magnitude, Mathf.Abs(a - b) + .001f, a + b - Mathf.Min(a, b) * .05f);
                wrist = upper.position + toWrist.normalized * reachDistance;
                elbow = PickElbow(upper.position, wrist, a, b, outward, radioUpperArmOutDegrees);
                along = (grip - elbow).normalized;
            }
            if (logRadioPose && !radioLogged && weight >= .99f)
            {
                radioLogged = true;
                Debug.Log("[PoliceGestures] radio held. In officer space (x right, y up, z forward, metres): grip " + Fmt(grip) + ", wrist " + Fmt(wrist)
                    + ", elbow " + Fmt(elbow) + ", shoulder " + Fmt(upper.position) + ". Upper arm is " + Vector3.Angle(elbow - upper.position, -policeRoot.up).ToString("0")
                    + " deg from hanging. Grip object: " + (radioGrip != null ? radioGrip.name + " under " + (radioGrip.parent != null ? radioGrip.parent.name : "scene root") : "none"), this);
            }
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, wrist - lower.position) * lower.rotation;
            // Fingers continue along the forearm toward the radio.
            hand.rotation = Quaternion.FromToRotation(hand.up, along) * hand.rotation;
            if (hasPalmLocal && radioPalmRoll > 0f)
            {
                // Roll the hand about the finger axis so the palm faces the chest.
                Vector3 finger = hand.up;
                Vector3 palm = Vector3.ProjectOnPlane(hand.rotation * palmLocal, finger);
                Vector3 wanted = Vector3.ProjectOnPlane(-policeRoot.forward * .9f + inward * .15f, finger);
                if (palm.sqrMagnitude > .0001f && wanted.sqrMagnitude > .0001f)
                {
                    float roll = Mathf.Clamp(Vector3.SignedAngle(palm, wanted, finger), -radioMaxPalmRollDegrees, radioMaxPalmRollDegrees);
                    hand.rotation = Quaternion.AngleAxis(roll * radioPalmRoll, finger) * hand.rotation;
                }
            }
            Quaternion upperGoal = upper.localRotation, lowerGoal = lower.localRotation, handGoal = hand.localRotation;
            // Elbow leads, forearm follows closely, the hand settles last.
            float upperBlend = Mathf.SmoothStep(0f, 1f, weight);
            float lowerBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(weight * 1.12f));
            float handBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((weight - .12f) / .88f));
            upper.localRotation = Quaternion.Slerp(upperPose, upperGoal, upperBlend);
            lower.localRotation = Quaternion.Slerp(lowerPose, lowerGoal, lowerBlend);
            hand.localRotation = Quaternion.Slerp(handPose, handGoal, handBlend);
        }

        private void Solve(Transform upper, Transform lower, Transform hand, Vector3 offset, Vector3 elbowDirection, HandPose pose = HandPose.Keep, float armWeight = -1f)
        {
            if (upper == null || lower == null || hand == null) return;
            Quaternion upperPose = upper.localRotation, lowerPose = lower.localRotation, handPose = hand.localRotation;
            Quaternion handWorldPose = hand.rotation;
            // Offsets are in metres, independent of the imported FBX's scale.
            Vector3 target = pose == HandPose.Radio && radioGrip != null ? radioGrip.position
                : pose == HandPose.Gun && gunGrip != null ? gunGrip.position
                : policeRoot.position + policeRoot.rotation * offset;
            float a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, hand.position);
            if (a < .001f || b < .001f) return;
            Vector3 direction = target - upper.position;
            if (direction.sqrMagnitude < .000001f) return;
            // Keep a small elbow bend rather than snapping to a fully straight arm.
            float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .001f, a + b - Mathf.Min(a, b) * .05f);
            direction.Normalize();
            target = upper.position + direction * distance;
            Vector3 bend = Vector3.ProjectOnPlane(policeRoot.rotation * elbowDirection, direction);
            if (bend.sqrMagnitude < .0001f) bend = Vector3.ProjectOnPlane(lower.position - upper.position, direction);
            if (bend.sqrMagnitude < .0001f) return;
            bend.Normalize();
            float along = (a * a + distance * distance - b * b) / (2f * distance);
            Vector3 elbow = upper.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
            // Stabilize the palm without forcing a large compensating wrist twist.
            hand.rotation = pose == HandPose.Radio && radioGrip != null
                ? Quaternion.FromToRotation(handWorldPose * Vector3.up, radioGrip.up) * handWorldPose
                : pose == HandPose.Gun && gunGrip != null
                ? Quaternion.FromToRotation(handWorldPose * Vector3.up, gunGrip.up) * handWorldPose
                : pose == HandPose.Push ? Quaternion.LookRotation(policeRoot.forward, policeRoot.up)
                : Quaternion.RotateTowards(hand.rotation, handWorldPose, 35f);
            Quaternion upperGoal = upper.localRotation, lowerGoal = lower.localRotation, handGoal = hand.localRotation;
            float blend = armWeight < 0f ? Mathf.SmoothStep(0f, 1f, weight) : Mathf.Clamp01(armWeight);
            // Blend rotations, not just the hand target. Solving a moving target at
            // full weight caused the elbow to jump into its new bend plane immediately.
            upper.localRotation = Quaternion.Slerp(upperPose, upperGoal, blend);
            lower.localRotation = Quaternion.Slerp(lowerPose, lowerGoal, blend);
            hand.localRotation = Quaternion.Slerp(handPose, handGoal, blend);
        }
    }
}

