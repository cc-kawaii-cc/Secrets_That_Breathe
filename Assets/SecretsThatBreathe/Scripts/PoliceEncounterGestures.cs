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

        private void Start()
        {
            if (policeRoot == null) return;
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
        }

        public void SetGesture(Gesture next)
        {
            if (next != Gesture.Talk && next != targetGesture)
            {
                if (next != gesture) weight = 0f;
                gesture = next;
                gestureTime = 0f;
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
        private void LateUpdate() => AdvanceGesture(Time.deltaTime);

        // Also used by the animation preview to evaluate the real runtime pose.
        public void AdvanceGesture(float deltaTime)
        {
            bool lowering = targetGesture == Gesture.Talk;
            float speed = gesture == Gesture.Radio
                ? 1f / Mathf.Max(.1f, lowering ? radioLowerSeconds : radioRaiseSeconds)
                : gesture == Gesture.GunAndPush ? 1f / Mathf.Max(.1f, lowering ? gunLowerSeconds : gunReachSeconds) : blendSpeed;
            gestureTime += Mathf.Max(0f, deltaTime);
            weight = Mathf.MoveTowards(weight, lowering ? 0f : 1f, speed * Mathf.Max(0f, deltaTime));
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
                Solve(upperLeft, lowerLeft, handLeft, radioHandOffset, radioElbowDirection, HandPose.Radio);
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

