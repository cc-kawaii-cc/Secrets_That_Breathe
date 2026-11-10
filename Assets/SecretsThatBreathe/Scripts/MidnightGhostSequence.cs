using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace SecretsThatBreathe
{
    // MainScene_2_MidNight, after the three blood photos:
    //   5) Khem is walked to where the carbon lip debris will be found and thinks the tired lines one after another
    //      (he cannot move, but he can still look around);
    //   from here his view is held:
    //   6) he blinks heavily, his view goes blurry for a few seconds, a quick blink clears it;
    //   8) the ghost student appears right in front of him and shoves him aside, then a ghost car comes from the skid mark's
    //      side, over the skid mark and past the spot he stood on; his view follows it;
    //   9) still staring after it he says "เชี่ยอะไรวะนั่น", "กูต้องตาฝาดไปแน่"; the car debris is switched on and his view turns
    //      to it. Then he is free again: he photographs each piece and picks it up (MidnightSkidPhoto + MidnightDebrisPickup);
    //      once all are picked up he says the last two lines.
    // The car debris is switched off when the scene starts.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(15050)]
    public sealed class MidnightGhostSequence : MonoBehaviour
    {
        [Header("Start")]
        [Tooltip("Starts once this photo (the last blood stain) is taken and the phone is lowered.")]
        public MidnightSkidPhoto afterPhoto;
        public MidnightRain rain;
        public FirstPersonController player;
        [Min(0f)] public float startDelay = .8f;

        [Header("Walk (Khem cannot control it)")]
        [Tooltip("Where Khem walks to (CarDebris_B_CarbonLip).")]
        public Transform walkTarget;
        [Tooltip("He stops this far before the target (metres, along the floor).")]
        [Min(.2f)] public float stopDistance = 1.3f;
        [Min(.1f)] public float walkSpeed = 1.2f;
        [Tooltip("Gives up walking after this long (if something blocks the way).")]
        [Min(1f)] public float maxWalkSeconds = 15f;
        [Tooltip("How smoothly Khem's view turns once it is held (from the blink on).")]
        [Min(.05f)] public float lookSmoothSeconds = .35f;

        [Header("Khem's tired lines (saved labels, shown one after another)")]
        public TMP_Text[] tiredLines;
        [Min(.2f)] public float lineSeconds = 2.8f;
        [Min(0f)] public float lineGap = .35f;

        [Header("6) Blink and blurry view")]
        [Tooltip("Black eyelids (top and bottom halves of the screen); their Y scale goes 0 = open to 1 = closed.")]
        public RectTransform eyelidTop;
        public RectTransform eyelidBottom;
        public AudioClip dizzySound;
        [Min(.5f)] public float blurSeconds = 2.6f;
        [Range(.5f, 1.5f)] public float blurRadius = 1.5f;
        [Range(0f, 1f)] public float blurVignette = .45f;

        [Header("8) Ghost student")]
        [Tooltip("GhostStudent (switched off until she appears).")]
        public GameObject ghost;
        public string ghostClip = "Ghost_Push";
        [Tooltip("Seconds into the clip where she starts (just before the shove).")]
        [Min(0f)] public float ghostClipStart = .5f;
        [Tooltip("How far in front of Khem she appears (metres).")]
        [Min(.3f)] public float appearDistance = .85f;
        [Tooltip("Turn her if the model does not face Khem.")]
        public float ghostYawOffset;
        public AudioClip jumpScareSound;
        public AudioClip pushSound;
        [Min(0f)] public float pushDelay = .3f;
        [Min(0f)] public float pushDistance = 1.4f;
        [Min(.05f)] public float pushSeconds = .3f;
        [Min(0f)] public float ghostVanishAfter = .8f;
        [Min(.05f)] public float ghostVanishSeconds = .35f;

        [Header("8) Ghost car")]
        [Tooltip("CarDE02.3 (switched off until it passes). Its materials are swapped for see-through ghost ones when it appears.")]
        public GameObject ghostCar;
        [Tooltip("Ghost material (SecretsThatBreathe/Ghost); each car material becomes a copy of it with the car's own texture.")]
        public Material ghostMaterial;
        [Tooltip("The car comes from this side (the skid mark): it drives over it, then past where Khem stood.")]
        [FormerlySerializedAs("roadDirection")] public Transform comesFrom;
        [Tooltip("Turn the car model if it drives sideways or backwards.")]
        public float carYawOffset = 180f;
        [Min(1f)] public float carSpeed = 24f;
        [Tooltip("Where it starts: this far from Khem on the skid mark's side (always at least 15 m past the skid mark); it ends as far on the other side.")]
        [Min(5f)] public float carStartDistance = 45f;
        [Tooltip("The car passes this far to the side of where Khem stood (away from where he was shoved), metres.")]
        [Min(0f)] public float carSideOffset = .5f;
        [Min(0f)] public float carDelay = .5f;
        public AudioClip carSound;
        [Tooltip("Seconds into the car sound where it is loudest (the car passes Khem then).")]
        [Min(0f)] public float carSoundPeak = 2.2f;
        public Color carLightColour = new Color(.55f, .66f, .82f);
        [Min(0f)] public float carLightIntensity = 1.2f;

        [Header("9) After the ghost car")]
        [Tooltip("Khem's lines while he still stares after the car; the debris appears after the last one, then he turns to it.")]
        public TMP_Text[] shockLines;
        [Tooltip("Car debris pieces: switched off when the scene starts, switched on after the last line above.")]
        public GameObject[] hiddenDebris;
        [Tooltip("Photographing and picking up each piece; when all of them are picked up Khem says the lines below.")]
        public MidnightDebrisPickup[] debrisPickups;
        public TMP_Text[] foundLines;
        [Min(0f)] public float foundLinesDelay = .5f;

        [Header("Testing")]
        [Tooltip("F3 jumps straight to collecting the car debris: Khem gets out of the car if he is still in it, the photos count as taken, the rain is heavy, and he stands by the debris.")]
        public bool f3SkipToDebris = true;
        [Tooltip("How far from the debris Khem stands after F3 (metres).")]
        [Min(.5f)] public float skipStandDistance = 2.2f;

        public bool Running { get; private set; }
        public bool Finished { get; private set; }

        private bool started, holdingControl, skipping;
        private Coroutine runRoutine;
        private float lookSmooth;
        private float shake;
        private Vector3 standPoint, shovedAside;
        private bool standSet;
        private AudioSource audioSource;
        private readonly List<Material> ghostMaterials = new List<Material>();
        private readonly List<Material> carMaterials = new List<Material>();
        private Light carLight;
        private bool carPrepared;
        private float readyAt = -1f;
        private CharacterController body;
        private Transform cameraTarget;
        private StarterAssetsInputs[] inputs;
        private bool[] inputLookEnabled;
        private bool looking;
        private float yaw, pitch, targetYaw, targetPitch, yawVelocity, pitchVelocity;

        private void Awake()
        {
            lookSmooth = lookSmoothSeconds;
            SetLids(0f);
            if (ghost != null) ghost.SetActive(false);
            if (ghostCar != null) ghostCar.SetActive(false);
            foreach (var lines in new[] { tiredLines, shockLines, foundLines })
                if (lines != null) foreach (var line in lines) SetBox(line, false);
            if (hiddenDebris != null) foreach (var piece in hiddenDebris) if (piece != null) piece.SetActive(false);
        }

        private void Update()
        {
            if (holdingControl && player != null) player.MovementLocked = true;
            if (looking) StepLook();
            if (f3SkipToDebris && !skipping && !Finished && player != null && Application.isFocused
                && Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            { StartCoroutine(SkipToDebris()); return; }
            if (started || afterPhoto == null || player == null) return;
            if (!afterPhoto.Completed || afterPhoto.IsRunning) return;
            if (readyAt < 0f) readyAt = Time.time + startDelay;
            if (Time.time >= readyAt) { started = true; runRoutine = StartCoroutine(Run()); }
        }

        private IEnumerator Run()
        {
            Running = true;
            holdingControl = true;
            player.MovementLocked = true;
            if (rain != null) rain.MakeHeavy();
            // he is walked there, but his view stays free (mouse) until he blinks
            if (walkTarget != null) yield return WalkTo(walkTarget.position);
            yield return ShowLines(tiredLines);
            BeginLook();                                      // from here his view is held
            yield return BlinkAndBlur();
            yield return new WaitForSeconds(.35f);
            if (ghost != null) yield return JumpScare();
            if (ghostCar != null) yield return GhostCar();

            // 9) still staring after it; the debris is switched on while he looks away, then he turns to it
            yield return new WaitForSeconds(.3f);
            yield return ShowLines(shockLines);
            if (hiddenDebris != null) foreach (var piece in hiddenDebris) if (piece != null) piece.SetActive(true);
            yield return LookBack();
            EndLook();
            holdingControl = false;
            if (player != null) player.MovementLocked = false;
            Running = false;
            yield return AfterDebris();
        }

        // The player photographs and picks up the pieces; then the last lines.
        private IEnumerator AfterDebris()
        {
            if (debrisPickups != null && debrisPickups.Length > 0)
            {
                while (!AllPickedUp()) yield return null;
                yield return new WaitForSeconds(foundLinesDelay);
                yield return ShowLines(foundLines);
            }
            Finished = true;
        }

        // ---- testing: F3 -------------------------------------------------------------------------------------------------------------------
        private IEnumerator SkipToDebris()
        {
            skipping = true;
            // out of the car first (the drive and the arrival are skipped)
            var ride = rain != null ? rain.ride : null;
            if (ride != null && !ride.HasExited)
            {
                if (!ride.ExitNowForTesting()) { Debug.LogWarning("[Midnight] F3: Khem could not get out of the car.", this); skipping = false; yield break; }
                float until = Time.time + 15f;
                while (!ride.HasExited && Time.time < until) yield return null;
            }
            // stop whatever this sequence was doing
            if (runRoutine != null) { StopCoroutine(runRoutine); runRoutine = null; }
            StopSequence();
            started = true;
            // the rain is heavy (before the photos count, so the rain line is not said), every evidence photo counts as taken
            if (rain != null) rain.MakeHeavy();
            foreach (var photo in FindObjectsByType<MidnightSkidPhoto>(FindObjectsSortMode.None))
            {
                if (photo == null || photo.gameObject.scene != gameObject.scene || photo.Completed) continue;
                if (photo.skidMark != null && IsDebris(photo.skidMark)) continue;
                photo.MarkDoneForTesting();
            }
            // the debris is there; Khem stands next to it
            if (hiddenDebris != null) foreach (var piece in hiddenDebris) if (piece != null) piece.SetActive(true);
            PlaceNearDebris(ride);
            if (player != null) player.MovementLocked = false;
            skipping = false;
            Debug.Log("[Midnight] F3: skipped to collecting the car debris.", this);
            yield return AfterDebris();
        }

        private bool IsDebris(Transform target)
        {
            if (hiddenDebris == null) return false;
            foreach (var piece in hiddenDebris) if (piece != null && (target == piece.transform || target.IsChildOf(piece.transform))) return true;
            return false;
        }

        // Puts away everything the sequence may have on screen and gives the controls back.
        private void StopSequence()
        {
            SetLids(0f);
            foreach (var volume in GetComponentsInChildren<Volume>(true))
            { if (volume.sharedProfile != null) Destroy(volume.sharedProfile); Destroy(volume.gameObject); }
            if (ghost != null) ghost.SetActive(false);
            if (ghostCar != null) ghostCar.SetActive(false);
            foreach (var lines in new[] { tiredLines, shockLines, foundLines })
                if (lines != null) foreach (var line in lines) SetBox(line, false);
            if (audioSource != null) audioSource.Stop();
            shake = 0f;
            lookSmooth = lookSmoothSeconds;
            EndLook();
            holdingControl = false;
            if (player != null) player.MovementLocked = false;
            Running = false;
        }

        // Khem a few steps from the debris, on the side he would have walked from, looking at it.
        private void PlaceNearDebris(PorschePassengerRide ride)
        {
            if (player == null) return;
            Vector3 centre;
            if (!DebrisCentre(out centre)) centre = walkTarget != null ? walkTarget.position : player.transform.position;
            Vector3 from = afterPhoto != null && afterPhoto.skidMark != null ? afterPhoto.skidMark.position : player.transform.position;
            Vector3 direction = centre - from;
            direction.y = 0f;
            direction = direction.sqrMagnitude > .01f ? direction.normalized : Vector3.forward;
            Vector3 feet = centre - direction * skipStandDistance;
            feet.y = GroundHeight(feet, centre.y);
            var controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            // move the player's container (as the car does when Khem gets out), so the camera rig comes along
            Transform root = ride != null && ride.playerRoot != null ? ride.playerRoot : player.transform.root;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Quaternion bodyInRoot = Quaternion.Inverse(root.rotation) * player.transform.rotation;
            root.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Inverse(bodyInRoot);
            root.position += feet + Vector3.up * .02f - player.transform.position;
            if (controller != null) controller.enabled = wasEnabled;
            Physics.SyncTransforms();
            const float pitch = 25f;
            player.ResetAfterTeleport(pitch);
            var view = ride != null && ride.playerCamera != null ? ride.playerCamera : Camera.main;
            if (view != null && player.CinemachineCameraTarget != null)
                view.transform.SetPositionAndRotation(player.CinemachineCameraTarget.transform.position, Quaternion.Euler(pitch, yaw, 0f));
            foreach (var input in root.GetComponentsInChildren<StarterAssetsInputs>(true))
            { input.move = input.look = Vector2.zero; input.jump = input.sprint = false; }
        }

        private bool AllPickedUp()
        {
            foreach (var pickup in debrisPickups)
                if (pickup != null && pickup.isActiveAndEnabled && (!pickup.Completed || pickup.IsRunning)) return false;
            return true;
        }

        // ---- walking ----------------------------------------------------------------------------------------------------------------
        private IEnumerator WalkTo(Vector3 target)
        {
            body = player.GetComponent<CharacterController>();
            float began = Time.time;
            float until = began + maxWalkSeconds;
            Vector3 lastPosition = player.transform.position;
            float stuck = 0f;
            while (Time.time < until)
            {
                Vector3 to = target - player.transform.position;
                to.y = 0f;
                float distance = to.magnitude;
                if (distance <= stopDistance) break;
                Vector3 direction = to / distance;
                float speed = walkSpeed * Mathf.Lerp(.15f, 1f, Smooth((Time.time - began) / .6f));   // eases into the walk
                Vector3 step = direction * Mathf.Min(speed * Time.deltaTime, distance - stopDistance) + Vector3.down * 2f * Time.deltaTime;
                if (body != null && body.enabled) body.Move(step);
                else player.transform.position += new Vector3(step.x, 0f, step.z);
                // stop if something blocks the way for a while
                float moved = (player.transform.position - lastPosition).magnitude;
                lastPosition = player.transform.position;
                stuck = moved < speed * Time.deltaTime * .15f ? stuck + Time.deltaTime : 0f;
                if (stuck > .8f) break;
                yield return null;
            }
            yield return new WaitForSeconds(.3f);
        }

        // ---- 6) blink and blurry view ------------------------------------------------------------------------------------------------
        private void SwitchOffFlashlight()
        {
            if (player == null) return;
            foreach (var flashlight in player.transform.root.GetComponentsInChildren<FirstPersonFlashlight>(true))
                if (flashlight != null) flashlight.ForceOff();
        }

        private IEnumerator BlinkAndBlur()
        {
            Play(dizzySound, .9f);
            var volume = BlurVolume();
            yield return Lids(0f, 1f, .35f);                 // heavy, slow blink
            SwitchOffFlashlight();                           // his flashlight goes off while his eyes are closed
            yield return new WaitForSeconds(.25f);
            for (float t = 0f; t < .5f; t += Time.deltaTime)
            {
                SetLids(1f - Smooth(t / .5f));
                if (volume != null) volume.weight = Smooth(t / .5f);
                yield return null;
            }
            SetLids(0f);
            float baseYaw = targetYaw, basePitch = targetPitch;
            float until = Time.time + blurSeconds;
            while (Time.time < until)                         // blurry, the view drifts a little
            {
                float k = Time.time * 1.3f;
                targetYaw = baseYaw + 2.5f * Mathf.Sin(k);
                targetPitch = basePitch + 1.6f * Mathf.Sin(k * 1.7f);
                if (volume != null) volume.weight = .85f + .15f * Mathf.Sin(Time.time * 5f);
                yield return null;
            }
            targetYaw = baseYaw; targetPitch = basePitch;
            yield return Lids(0f, 1f, .12f);                 // a quick blink clears it
            if (volume != null) volume.weight = 0f;
            yield return new WaitForSeconds(.08f);
            yield return Lids(1f, 0f, .18f);
            if (volume != null) { Destroy(volume.sharedProfile); Destroy(volume.gameObject); }
        }

        private Volume BlurVolume()
        {
            var holder = new GameObject("BlinkBlur");
            holder.transform.SetParent(transform, false);
            var volume = holder.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            volume.weight = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var depth = profile.Add<DepthOfField>(true);
            depth.mode.Override(DepthOfFieldMode.Gaussian);
            depth.gaussianStart.Override(0f);
            depth.gaussianEnd.Override(.6f);
            depth.gaussianMaxRadius.Override(blurRadius);
            depth.highQualitySampling.Override(true);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(blurVignette);
            vignette.smoothness.Override(.8f);
            vignette.color.Override(Color.black);
            volume.sharedProfile = profile;
            return volume;
        }

        private IEnumerator Lids(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime) { SetLids(Mathf.Lerp(from, to, Smooth(t / seconds))); yield return null; }
            SetLids(to);
        }

        private void SetLids(float closed)
        {
            foreach (var lid in new[] { eyelidTop, eyelidBottom })
            {
                if (lid == null) continue;
                if (!lid.gameObject.activeSelf) lid.gameObject.SetActive(true);
                lid.localScale = new Vector3(1f, Mathf.Clamp01(closed), 1f);
            }
        }

        // ---- 8) the ghost student -----------------------------------------------------------------------------------------------------
        private IEnumerator JumpScare()
        {
            standPoint = player.transform.position;
            standSet = true;
            Vector3 forward = FlatForward();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 at = player.transform.position + forward * appearDistance;
            at.y = GroundHeight(at, player.transform.position.y);
            ghost.transform.SetPositionAndRotation(at, Quaternion.LookRotation(-forward, Vector3.up) * Quaternion.Euler(0f, ghostYawOffset, 0f));
            ghost.SetActive(true);
            CollectGhostMaterials();
            SetFade(ghostMaterials, 1f);
            var animator = ghost.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                float length = 2.4f;
                if (animator.runtimeAnimatorController != null)
                    foreach (var clip in animator.runtimeAnimatorController.animationClips)
                        if (clip != null && clip.name == ghostClip) length = clip.length;
                animator.Play(ghostClip, 0, Mathf.Clamp01(ghostClipStart / Mathf.Max(.01f, length)));
                animator.Update(0f);
            }
            Play(jumpScareSound, 1f);
            float appeared = Time.time;
            // the view snaps to her face
            lookSmooth = .06f;
            Aim(at + Vector3.up * 1.45f);
            shake = 1f;
            yield return new WaitForSeconds(pushDelay);

            // the shove: back and to the side, out of the way of what comes next
            Play(pushSound, 1f);
            shake = 1.4f;
            targetPitch -= 9f;
            // sideways off the line the ghost car will take (to his right-hand side, never forwards into her), and a little back
            Vector3 aside = Vector3.Cross(Vector3.up, TowardsSkid());
            if (Vector3.Dot(aside, right - forward) < 0f) aside = -aside;
            shovedAside = aside;
            Vector3 push = (-forward * .45f + aside * .9f).normalized * pushDistance;
            var body = player.GetComponent<CharacterController>();
            float done = 0f;
            for (float t = 0f; t < pushSeconds; t += Time.deltaTime)
            {
                float now = 1f - (1f - t / pushSeconds) * (1f - t / pushSeconds);   // fast, then slowing down
                Move(body, push * (now - done));
                done = now;
                yield return null;
            }
            Move(body, push * (1f - done));
            lookSmooth = .18f;
            Aim(ghost.transform.position + Vector3.up * 1.4f);

            // she flickers away
            while (Time.time < appeared + ghostVanishAfter) yield return null;
            for (float t = 0f; t < ghostVanishSeconds; t += Time.deltaTime)
            {
                float flicker = Random.value > .35f ? 1f : .2f;
                SetFade(ghostMaterials, (1f - t / ghostVanishSeconds) * flicker);
                yield return null;
            }
            ghost.SetActive(false);
        }

        private void CollectGhostMaterials()
        {
            if (ghostMaterials.Count > 0) return;
            foreach (var renderer in ghost.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                foreach (var material in renderer.materials) if (material != null) ghostMaterials.Add(material);
            }
        }

        // ---- 8) the ghost car ---------------------------------------------------------------------------------------------------------------
        private IEnumerator GhostCar()
        {
            PrepareCar();
            if (!standSet) { standPoint = player.transform.position; standSet = true; }
            // from the skid mark's side, over the skid mark, past where Khem stood (a little away from where he was shoved)
            Vector3 towardsSkid = TowardsSkid();
            float startDistance = carStartDistance;
            if (comesFrom != null)
            {
                Vector3 toSkid = comesFrom.position - standPoint;
                toSkid.y = 0f;
                startDistance = Mathf.Max(carStartDistance, toSkid.magnitude + 15f);
            }
            Vector3 start = standPoint + towardsSkid * startDistance - shovedAside * carSideOffset;
            Vector3 travel = -towardsSkid;
            float total = startDistance * 2f;
            yield return new WaitForSeconds(carDelay);

            // the sound is loudest when the car passes the spot Khem stood on
            float reachSeconds = startDistance / carSpeed;
            float lead = carSoundPeak - reachSeconds;
            if (lead >= 0f) { Play(carSound, 1f); yield return new WaitForSeconds(lead); }
            else StartCoroutine(PlayLater(carSound, -lead));

            ghostCar.SetActive(true);
            Quaternion facing = Quaternion.LookRotation(travel, Vector3.up) * Quaternion.Euler(0f, carYawOffset, 0f);
            lookSmooth = .14f;
            for (float travelled = 0f; travelled < total; travelled += carSpeed * Time.deltaTime)
            {
                Vector3 position = start + travel * travelled;
                position.y = TerrainHeight(position, standPoint.y);
                ghostCar.transform.SetPositionAndRotation(position, facing);
                float edge = Mathf.Min(travelled, total - travelled);
                SetFade(carMaterials, Mathf.Clamp01(edge / 12f));
                if (carLight != null) carLight.intensity = carLightIntensity * (Random.value > .12f ? 1f : .25f) * Mathf.Clamp01(edge / 12f);
                Aim(position + Vector3.up * .8f);        // the view follows it
                if (Mathf.Abs(travelled - startDistance) < carSpeed * .25f) shake = Mathf.Max(shake, .7f);
                yield return null;
            }
            ghostCar.SetActive(false);
        }

        private void PrepareCar()
        {
            if (carPrepared) return;
            carPrepared = true;
            foreach (var behaviour in ghostCar.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var collider in ghostCar.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var rigidbody in ghostCar.GetComponentsInChildren<Rigidbody>(true)) { rigidbody.isKinematic = true; rigidbody.detectCollisions = false; }
            foreach (var source in ghostCar.GetComponentsInChildren<AudioSource>(true)) source.enabled = false;
            foreach (var light in ghostCar.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (var renderer in ghostCar.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (ghostMaterial == null) continue;
                var swapped = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < swapped.Length; i++)
                {
                    var original = renderer.sharedMaterials[i];
                    var copy = new Material(ghostMaterial) { name = "GhostCar " + i };
                    Texture texture = null;
                    if (original != null)
                    {
                        texture = original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") : null;
                        // keep the car's own colours (paint, glass, tyres), just see-through
                        string colourName = original.HasProperty("_BaseColor") ? "_BaseColor" : original.HasProperty("_Color") ? "_Color" : null;
                        if (colourName != null)
                        {
                            Color own = original.GetColor(colourName), tint = copy.GetColor("_Tint");
                            copy.SetColor("_Tint", new Color(tint.r * own.r, tint.g * own.g, tint.b * own.b, 1f));
                        }
                    }
                    if (texture != null) copy.SetTexture("_BaseMap", texture);
                    swapped[i] = copy;
                    carMaterials.Add(copy);
                }
                renderer.sharedMaterials = swapped;
            }
            var lightObject = new GameObject("GhostCarGlow");
            lightObject.transform.SetParent(ghostCar.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            carLight = lightObject.AddComponent<Light>();
            carLight.type = LightType.Point;
            carLight.range = 6f;
            carLight.color = carLightColour;
            carLight.intensity = 0f;
            carLight.shadows = LightShadows.None;
        }

        private IEnumerator LookBack()
        {
            if (!looking) yield break;
            lookSmooth = lookSmoothSeconds * 1.6f;
            Vector3 debris;
            if (DebrisCentre(out debris)) Aim(debris);
            else if (walkTarget != null) Aim(walkTarget.position);
            yield return new WaitForSeconds(lookSmooth * 3f);
            lookSmooth = lookSmoothSeconds;
        }

        // The middle of the debris that is showing.
        private bool DebrisCentre(out Vector3 centre)
        {
            centre = Vector3.zero;
            int count = 0;
            if (hiddenDebris != null)
                foreach (var piece in hiddenDebris)
                {
                    if (piece == null || !piece.activeInHierarchy) continue;
                    var renderer = piece.GetComponentInChildren<Renderer>();
                    centre += renderer != null ? renderer.bounds.center : piece.transform.position;
                    count++;
                }
            if (count == 0) return false;
            centre /= count;
            return true;
        }

        // ---- helpers ---------------------------------------------------------------------------------------------------------------------------
        private static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        // Flat direction from where Khem stood towards the skid mark: the ghost car comes from that side.
        private Vector3 TowardsSkid()
        {
            Vector3 from = standSet ? standPoint : player.transform.position;
            if (comesFrom != null)
            {
                Vector3 to = comesFrom.position - from;
                to.y = 0f;
                if (to.sqrMagnitude > 1f) return to.normalized;
            }
            return FlatForward();
        }

        private Vector3 FlatForward()
        {
            var view = Camera.main;
            Vector3 forward = view != null ? view.transform.forward : player.transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > .0001f ? forward.normalized : Vector3.forward;
        }

        private void Aim(Vector3 point)
        {
            Vector3 eye = cameraTarget != null ? cameraTarget.position : player.transform.position + Vector3.up * 1.5f;
            Vector3 look = point - eye;
            if (new Vector2(look.x, look.z).sqrMagnitude < .0001f) return;
            targetYaw = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(-Mathf.Atan2(look.y, new Vector2(look.x, look.z).magnitude) * Mathf.Rad2Deg, -40f, 60f);
        }

        // The floor under a point: the nearest upward-facing surface (not Khem, not the ghost car).
        private float GroundHeight(Vector3 position, float fallback)
        {
            float best = float.MaxValue, height = fallback;
            foreach (var hit in Physics.RaycastAll(position + Vector3.up * 3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (player != null && hit.collider.transform.IsChildOf(player.transform.root)) continue;
                if (ghostCar != null && hit.collider.transform.IsChildOf(ghostCar.transform)) continue;
                if (hit.collider.GetComponentInParent<BananaEatInteraction>() != null) continue;   // banana leaves have colliders
                if (hit.normal.y < .5f || hit.distance >= best) continue;
                best = hit.distance;
                height = hit.point.y;
            }
            return height;
        }

        // The ghost car follows the Terrain (so it does not hop over cones and tape); without Terrain it keeps Khem's height.
        private static float TerrainHeight(Vector3 position, float fallback)
        {
            foreach (var terrain in Terrain.activeTerrains)
            {
                if (terrain == null || terrain.terrainData == null) continue;
                Vector3 origin = terrain.GetPosition(), size = terrain.terrainData.size;
                if (position.x < origin.x || position.z < origin.z || position.x > origin.x + size.x || position.z > origin.z + size.z) continue;
                return Mathf.Max(origin.y + terrain.SampleHeight(position), fallback - .5f);
            }
            return fallback;
        }

        private static void SetFade(List<Material> materials, float fade)
        {
            foreach (var material in materials) if (material != null) material.SetFloat("_Fade", fade);
        }

        private void Move(CharacterController body, Vector3 step)
        {
            if (body != null && body.enabled) body.Move(step + Vector3.down * .02f);
            else player.transform.position += step;
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip == null) return;
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
            audioSource.PlayOneShot(clip, volume);
        }

        private IEnumerator PlayLater(AudioClip clip, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Play(clip, 1f);
        }

        // ---- lines --------------------------------------------------------------------------------------------------------------------
        private IEnumerator ShowLines(TMP_Text[] lines)
        {
            if (lines == null) yield break;
            foreach (var line in lines)
            {
                if (line == null) continue;
                SetBox(line, true);
                yield return new WaitForSeconds(lineSeconds);
                SetBox(line, false);
                if (lineGap > 0f) yield return new WaitForSeconds(lineGap);
            }
        }

        private static void SetBox(TMP_Text line, bool show)
        {
            if (line == null) return;
            var box = line.transform.parent != null ? line.transform.parent.gameObject : line.gameObject;
            if (!line.gameObject.activeSelf) line.gameObject.SetActive(true);
            if (box.activeSelf != show) box.SetActive(show);
        }

        // ---- Khem's view ------------------------------------------------------------------------------------------------------------------
        private void BeginLook()
        {
            if (player.CinemachineCameraTarget == null) return;
            cameraTarget = player.CinemachineCameraTarget.transform;
            inputs = player.transform.root.GetComponentsInChildren<StarterAssetsInputs>(true);
            inputLookEnabled = new bool[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) { inputLookEnabled[i] = inputs[i].cursorInputForLook; inputs[i].cursorInputForLook = false; }
            yaw = targetYaw = player.transform.eulerAngles.y;
            pitch = targetPitch = Mathf.DeltaAngle(0f, cameraTarget.localEulerAngles.x);
            yawVelocity = pitchVelocity = 0f;
            looking = true;
        }

        private void StepLook()
        {
            if (player == null || cameraTarget == null) { looking = false; return; }
            yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, lookSmooth);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, lookSmooth);
            float shakeYaw = 0f, shakePitch = 0f;
            if (shake > 0f)
            {
                shakeYaw = Random.Range(-1.6f, 1.6f) * shake;
                shakePitch = Random.Range(-1.2f, 1.2f) * shake;
                shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 2f);
            }
            player.transform.rotation = Quaternion.Euler(0f, yaw + shakeYaw, 0f);
            cameraTarget.localRotation = Quaternion.Euler(Mathf.Clamp(pitch + shakePitch, -60f, 75f), 0f, 0f);
            if (inputs != null) foreach (var input in inputs) if (input != null) input.look = Vector2.zero;
        }

        private void EndLook()
        {
            if (!looking) return;
            looking = false;
            if (player != null && cameraTarget != null)
            {
                player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                player.SyncLookPitch(pitch);
            }
            if (inputs != null)
                for (int i = 0; i < inputs.Length; i++)
                    if (inputs[i] != null) { inputs[i].look = Vector2.zero; inputs[i].cursorInputForLook = inputLookEnabled[i]; }
            inputs = null;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            SetLids(0f);
            if (ghost != null) ghost.SetActive(false);
            if (ghostCar != null) ghostCar.SetActive(false);
            foreach (var lines in new[] { tiredLines, shockLines, foundLines })
                if (lines != null) foreach (var line in lines) SetBox(line, false);
            EndLook();
            if (holdingControl && player != null) player.MovementLocked = false;
            holdingControl = false;
            Running = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (walkTarget == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(walkTarget.position, stopDistance);
        }
    }
}
