using System.Collections;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SecretsThatBreathe
{
    // Inside_Police_Station, step 1: when the arrival fade lifts, Khem is already looking at the man in sunglasses.
    // He walks towards Khem, the view follows him, and once he has passed Khem the view swings back to normal and
    // Khem says one line. Khem cannot walk from the start until a few seconds after the line appears.
    // Every object this uses is saved in the scene.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9000)]
    public sealed class PoliceStationOpening : MonoBehaviour
    {
        private const float DefaultWaypointCornerRadius = .8f;

        [Header("Scene objects")]
        public PoliceStationArrival arrival;
        public FirstPersonController player;
        public Transform sunglassesMan;
        public Animator sunglassesAnimator;
        [Tooltip("Optional door that silences the man's footsteps when he walks into it.")]
        public Collider footstepStopDoor;
        [InspectorName("Waypoints")]
        [Tooltip("Move the waypoint objects in the Scene view. The man follows this list in order.")]
        public Transform[] route;
        [Tooltip("Index of the route point where the man passes Khem (1 = beside Khem). The view returns to normal and the line appears there.")]
        [Min(0)] public int khemSpeaksAtRoutePoint = 1;

        [Header("Walking")]
        [Min(.1f)] public float walkSpeed = .95f;
        [Min(30f)] public float turnDegreesPerSecond = 360f;
        [Tooltip("Follow straight segments instead of rounding corners.")]
        public bool useStraightWaypoints;
        [Tooltip("How far from each waypoint the path starts rounding the corner. Old scenes with a zero value use the default.")]
        [Min(.05f)] public float waypointCornerRadius = DefaultWaypointCornerRadius;
        [Tooltip("Black screen opacity required to start walking. Set to 1 to start as the screen begins to reveal, after the intro text disappears.")]
        [Range(0f, 1f)] public float startWhenBlackScreenBelow = .02f;

        [Header("Khem's movement")]
        [Tooltip("Khem cannot walk (he can still look around) from the start until the line has been on screen for the time below.")]
        public bool lockMovementUntilFinished = true;
        [Tooltip("Seconds after the line starts to appear before Khem can walk again.")]
        [Min(0f)] public float movementLockSecondsAfterLine = 3f;

        [Header("Khem's view")]
        [Tooltip("The view is turned to the man before the screen fades in, follows him while he walks, and returns to normal when he has passed.")]
        public bool lookAtManWhileHeApproaches = true;
        [Tooltip("Bigger = a slower, softer camera. The view eases after the man instead of snapping to him.")]
        [Min(.05f)] public float lookSmoothSeconds = .55f;
        [Tooltip("The view never turns faster than this, so it cannot whip round when he passes close to Khem.")]
        [Min(10f)] public float lookMaxDegreesPerSecond = 55f;
        [Tooltip("The view follows him at most this far to the side of Khem's normal view; he leaves the screen edge after that.")]
        [Range(10f, 90f)] public float lookMaxTurnDegrees = 50f;
        [Tooltip("Height above the man's feet that the view aims at (a fixed point, so his walking bob does not shake the view).")]
        [Min(.5f)] public float lookAtHeight = 1.55f;

        [Header("Dialogue (same boxes, font and typing sound as MainScene_2_Evening)")]
        public DialogueCanvasView khemDialogue = new DialogueCanvasView();
        public FlowingDialogueSettings presentation = new FlowingDialogueSettings();
        [Tooltip("Saved text labels. Edit the wording in the Hierarchy.")]
        public TMP_Text[] manPassesLines;
        public bool showPassThoughtInstantly;
        [Min(0f)] public float passThoughtHoldSeconds = 3f;
        [Min(0f)] public float dialogueDelay = .15f;

        public UnityEvent onFinished = new UnityEvent();
        public bool Completed { get; private set; }
        public bool IsRunning { get; private set; }

        private enum LookState { Off, Following, Returning }
        private FlowingDialogue flowing;
        private GameObject visiblePassThought;
        private bool speaking, holdingMovement, passStarted;
        private float lockEndTime = float.PositiveInfinity;
        // Khem's view
        private LookState look = LookState.Off;
        private StarterAssetsInputs[] inputs;
        private bool[] inputLookEnabled;
        private Transform cameraTarget;
        private float baseYaw, basePitch, lookYaw, lookPitch, yawVelocity, pitchVelocity, returnElapsed;

        private void Awake() => HideDialogue();

        private void OnValidate()
        {
            if (waypointCornerRadius <= 0f) waypointCornerRadius = DefaultWaypointCornerRadius;
        }

        private void OnDrawGizmosSelected()
        {
            if (route == null) return;
            Gizmos.color = Color.cyan;
            bool showCurve = !useStraightWaypoints && route.Length >= 2 &&
                System.Array.TrueForAll(route, point => point != null);
            if (showCurve)
            {
                var path = BuildWalkingPath();
                for (int i = 1; i < path.SampleCount; i++)
                    Gizmos.DrawLine(path.GetSample(i - 1), path.GetSample(i));
            }
            for (int i = 0; i < route.Length; i++)
            {
                if (route[i] == null) continue;
                Vector3 position = route[i].position;
                Gizmos.DrawWireSphere(position, .18f);
                if (!showCurve && i > 0 && route[i - 1] != null)
                    Gizmos.DrawLine(route[i - 1].position, position);
            }
        }

        private IEnumerator Start()
        {
            if (!Valid()) { Debug.LogError("Assign the station opening actors, route, player and dialogue Canvas.", this); enabled = false; yield break; }
            IsRunning = true;
            sunglassesMan.SetPositionAndRotation(route[0].position, FaceRotation(route[0].position, route[1].position, sunglassesMan.rotation));
            sunglassesMan.gameObject.SetActive(true);
            if (sunglassesAnimator != null) sunglassesAnimator.speed = 0f;
            // Khem cannot walk from the very first frame. Update re-applies this, because the arrival script
            // gives the controls back at the end of the fade.
            holdingMovement = lockMovementUntilFinished;
            // Let the arrival script place Khem first (it does this a few frames in, behind the black screen).
            for (int i = 0; i < 5; i++) yield return null;
            if (lookAtManWhileHeApproaches) BeginLook();
            while (BlackAmount() >= 1f || BlackAmount() > startWhenBlackScreenBelow)
                yield return null;
            var walk = StartCoroutine(WalkRoute());
            yield return walk;
            while (speaking || Time.time < lockEndTime) yield return null;
            EndLook();
            holdingMovement = false;
            if (player != null) player.MovementLocked = false;
            IsRunning = false;
            Completed = true;
            onFinished.Invoke();
        }

        private bool Valid() => arrival != null && player != null && sunglassesMan != null && route != null && route.Length >= 2
            && khemSpeaksAtRoutePoint >= 0 && khemSpeaksAtRoutePoint < route.Length
            && System.Array.TrueForAll(route, point => point != null) && khemDialogue.IsValid && manPassesLines != null && manPassesLines.Length > 0;

        private float BlackAmount() => arrival != null && arrival.blackScreen != null ? arrival.blackScreen.alpha : 0f;

        private void Update()
        {
            if (holdingMovement && player != null) player.MovementLocked = true;
        }

        private void LateUpdate()
        {
            if (look == LookState.Off || player == null || cameraTarget == null) return;
            float targetYaw, targetPitch;
            if (look == LookState.Following) AimAtMan(out targetYaw, out targetPitch);
            else { targetYaw = baseYaw; targetPitch = basePitch; returnElapsed += Time.deltaTime; }
            // One continuous, speed-limited easing for both following and returning: no jump in speed when it switches.
            lookYaw = Mathf.SmoothDampAngle(lookYaw, targetYaw, ref yawVelocity, lookSmoothSeconds, lookMaxDegreesPerSecond, Time.deltaTime);
            lookPitch = Mathf.SmoothDamp(lookPitch, targetPitch, ref pitchVelocity, lookSmoothSeconds * 1.5f, lookMaxDegreesPerSecond * .5f, Time.deltaTime);
            ApplyLook();
            ClearLookInput();
        }

        // ---- Khem's view -------------------------------------------------------------------------------------------------------------
        private void BeginLook()
        {
            if (player.CinemachineCameraTarget == null) return;
            cameraTarget = player.CinemachineCameraTarget.transform;
            inputs = player.transform.root.GetComponentsInChildren<StarterAssetsInputs>(true);
            inputLookEnabled = new bool[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) { inputLookEnabled[i] = inputs[i].cursorInputForLook; inputs[i].cursorInputForLook = false; }
            baseYaw = player.transform.eulerAngles.y;
            basePitch = Mathf.DeltaAngle(0f, cameraTarget.localEulerAngles.x);
            AimAtMan(out lookYaw, out lookPitch);   // already facing him when the screen fades in
            yawVelocity = pitchVelocity = 0f;
            look = LookState.Following;
            ApplyLook();
            ClearLookInput();
        }

        private void AimAtMan(out float yaw, out float pitch)
        {
            // A fixed point above his feet, not his head bone: the walking bob would shake the view.
            Vector3 direction = sunglassesMan.position + Vector3.up * lookAtHeight - cameraTarget.position;
            float aimYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            // Never turn further than lookMaxTurnDegrees from Khem's normal view.
            yaw = baseYaw + Mathf.Clamp(Mathf.DeltaAngle(baseYaw, aimYaw), -lookMaxTurnDegrees, lookMaxTurnDegrees);
            float aimPitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(aimPitch, basePitch - 12f, basePitch + 12f);
        }

        private void ApplyLook()
        {
            player.transform.rotation = Quaternion.Euler(0f, lookYaw, 0f);
            cameraTarget.localRotation = Quaternion.Euler(lookPitch, 0f, 0f);
        }

        private void ClearLookInput()
        {
            if (inputs != null) foreach (var input in inputs) if (input != null) input.look = Vector2.zero;
        }

        private IEnumerator ReturnLook()
        {
            if (look != LookState.Following) yield break;
            returnElapsed = 0f;
            look = LookState.Returning;   // keeps its current turning speed and eases back to the normal view
            while (look == LookState.Returning && returnElapsed < lookSmoothSeconds * 6f
                && (Mathf.Abs(Mathf.DeltaAngle(lookYaw, baseYaw)) > .1f || Mathf.Abs(lookPitch - basePitch) > .1f
                    || Mathf.Abs(yawVelocity) > .5f || Mathf.Abs(pitchVelocity) > .5f))
                yield return null;
            EndLook();
        }

        private void EndLook()
        {
            if (look == LookState.Off) return;
            look = LookState.Off;
            if (player != null && cameraTarget != null)
                player.SyncLookPitch(Mathf.DeltaAngle(0f, cameraTarget.localEulerAngles.x));
            if (inputs != null)
                for (int i = 0; i < inputs.Length; i++)
                    if (inputs[i] != null) { inputs[i].look = Vector2.zero; inputs[i].cursorInputForLook = inputLookEnabled[i]; }
            inputs = null;
        }

        // ---- The man ---------------------------------------------------------------------------------------------------------------
        private IEnumerator WalkRoute()
        {
            if (sunglassesAnimator != null) sunglassesAnimator.speed = 1f;
            var footsteps = footstepStopDoor != null ? sunglassesMan.GetComponent<AnimatedFootsteps>() : null;
            if (!useStraightWaypoints) yield return WalkCurvedRoute(footsteps);
            else for (int i = 1; i < route.Length; i++)
            {
                Vector3 target = route[i].position;
                while ((target - sunglassesMan.position).sqrMagnitude > .0004f)
                {
                    Vector3 position = sunglassesMan.position;
                    Vector3 flat = Vector3.ProjectOnPlane(target - position, Vector3.up);
                    if (flat.sqrMagnitude > .0001f)
                    {
                        sunglassesMan.rotation = Quaternion.RotateTowards(sunglassesMan.rotation,
                            Quaternion.LookRotation(flat, Vector3.up), turnDegreesPerSecond * Time.deltaTime);
                    }
                    Vector3 nextPosition = Vector3.MoveTowards(position, target, walkSpeed * Time.deltaTime);
                    MoveMan(nextPosition, footsteps);
                    yield return null;
                }
                if (i == khemSpeaksAtRoutePoint) StartPass();
            }
            // Out through the door.
            if (sunglassesAnimator != null) sunglassesAnimator.speed = 0f;
            sunglassesMan.gameObject.SetActive(false);
        }

        private PassengerRidePath BuildWalkingPath()
        {
            var corners = new Vector3[route.Length - 2];
            for (int i = 0; i < corners.Length; i++) corners[i] = route[i + 1].position;
            float radius = waypointCornerRadius > 0f ? waypointCornerRadius : DefaultWaypointCornerRadius;
            return new PassengerRidePath(route[0].position, corners, route[route.Length - 1].position, radius);
        }

        private IEnumerator WalkCurvedRoute(AnimatedFootsteps footsteps)
        {
            var path = BuildWalkingPath();
            float passDistance = 0f, sampleDistance = 0f, closest = float.PositiveInfinity;
            Vector3 passPoint = route[khemSpeaksAtRoutePoint].position;
            for (int i = 0; i < path.SampleCount; i++)
            {
                Vector3 sample = path.GetSample(i);
                if (i > 0) sampleDistance += Vector3.Distance(path.GetSample(i - 1), sample);
                float distanceToPass = (sample - passPoint).sqrMagnitude;
                if (distanceToPass >= closest) continue;
                closest = distanceToPass;
                passDistance = sampleDistance;
            }

            float distance = 0f;
            do
            {
                distance = Mathf.Min(path.Length, distance + walkSpeed * Time.deltaTime);
                path.Evaluate(distance, out Vector3 position, out Vector3 tangent);
                sunglassesMan.rotation = FaceRotation(Vector3.zero, tangent, sunglassesMan.rotation);
                MoveMan(position, footsteps);
                if (distance >= passDistance && !passStarted) StartPass();
                yield return null;
            } while (distance < path.Length);
        }

        private void MoveMan(Vector3 position, AnimatedFootsteps footsteps)
        {
            Vector3 previous = sunglassesMan.position;
            Vector3 step = position - previous;
            if (footsteps != null && footsteps.enabled && step.sqrMagnitude > 0f &&
                footstepStopDoor.Raycast(new Ray(previous + Vector3.up, step.normalized), out _, step.magnitude + .15f))
                footsteps.enabled = false;
            sunglassesMan.position = position;
        }

        private void StartPass()
        {
            passStarted = true;
            speaking = true;
            StartCoroutine(PassKhemAndSpeak());
        }

        private static Quaternion FaceRotation(Vector3 from, Vector3 to, Quaternion fallback)
        {
            Vector3 flat = Vector3.ProjectOnPlane(to - from, Vector3.up);
            return flat.sqrMagnitude > .0001f ? Quaternion.LookRotation(flat.normalized, Vector3.up) : fallback;
        }

        // He has passed Khem: the view goes back to normal, then Khem says his line.
        private IEnumerator PassKhemAndSpeak()
        {
            yield return ReturnLook();
            yield return new WaitForSeconds(dialogueDelay);
            lockEndTime = Time.time + movementLockSecondsAfterLine;
            foreach (var line in manPassesLines)
            {
                if (line == null) continue;
                if (showPassThoughtInstantly)
                {
                    visiblePassThought = line.transform.parent.gameObject;
                    line.maxVisibleCharacters = int.MaxValue;
                    visiblePassThought.SetActive(true);
                    yield return new WaitForSeconds(passThoughtHoldSeconds);
                    visiblePassThought.SetActive(false);
                    visiblePassThought = null;
                    continue;
                }
                flowing?.Hide();
                ApplySentenceStyle(khemDialogue, line);
                flowing = new FlowingDialogue(khemDialogue, presentation);
                yield return flowing.Play(line.text);
            }
            HideDialogue();
            speaking = false;
        }

        private static void ApplySentenceStyle(DialogueCanvasView canvas, TMP_Text line)
        {
            if (line == null || canvas.sentence == null) return;
            var target = canvas.sentence;
            target.font = line.font; target.fontSharedMaterial = line.fontSharedMaterial;
            target.fontSize = line.fontSize; target.fontStyle = line.fontStyle; target.fontWeight = line.fontWeight;
            target.color = line.color; target.characterSpacing = line.characterSpacing;
            target.wordSpacing = line.wordSpacing; target.lineSpacing = line.lineSpacing; target.paragraphSpacing = line.paragraphSpacing;
            target.enableAutoSizing = line.enableAutoSizing; target.fontSizeMin = line.fontSizeMin; target.fontSizeMax = line.fontSizeMax;
        }

        // When Play Mode stops, Unity destroys the Canvas objects before this component's OnDisable runs.
        private bool DialogueAlive => khemDialogue != null && khemDialogue.IsValid && khemDialogue.speaker != null;

        private void HideDialogue()
        {
            if (visiblePassThought != null) visiblePassThought.SetActive(false);
            visiblePassThought = null;
            if (!DialogueAlive) { flowing = null; return; }
            flowing?.Hide();
            khemDialogue.Hide();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            speaking = false;
            HideDialogue();
            EndLook();
            if (holdingMovement && player != null) player.MovementLocked = false;
            holdingMovement = false;
        }
    }
}
