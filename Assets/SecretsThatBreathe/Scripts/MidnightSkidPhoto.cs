using System;
using System.Collections;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    // MainScene_2_MidNight, step 3: Khem walks up to the braking skid mark and thinks "นี่รอยรถนี่ ต้องถ่ายไว้". Then he can press E: he
    // raises his phone (Phone_Blank), held
    // sideways like a camera, and his view turns to the skid mark. The phone screen shows what its camera sees. E (or a left click)
    // takes the photo: shutter sound, flash, the screen freezes on the picture, then he lowers the phone.
    // Every label is saved in the scene (SeatingCanvas/MidnightSkidPhoto); edit the wording in the Hierarchy.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(15000)]
    public sealed class MidnightSkidPhoto : MonoBehaviour
    {
        [Header("Scene objects")]
        public FirstPersonController player;
        [Tooltip("What Khem photographs (the skid mark, a blood stain ...). His view turns to the middle of it.")]
        public Transform skidMark;
        [Tooltip("Optional: this photo can only be taken after that one (photos in order).")]
        public MidnightSkidPhoto requires;
        [Tooltip("Optional: this photo can only be taken after that piece of debris has been picked up (step 9).")]
        public MidnightDebrisPickup requiresPickup;
        [Tooltip("Seconds after the required photo / pickup before this one can start (lets other lines finish first).")]
        [Min(0f)] public float availableDelay;
        [Tooltip("Like the CCTV Interaction Zone: the '[E]' prompt shows while Khem stands inside this trigger box (a child of the skid mark). Move / resize it in the Scene view.")]
        public BoxCollider interactionZone;
        [Tooltip("Photo zone width (X) and depth (Y) in metres, centred on the Box Collider. Set an axis to 0 to use the collider's original size.")]
        public Vector2 photoZoneSizeMetres;
        [Tooltip("1 = facing straight at the skid mark. Khem must face roughly towards it (along the floor).")]
        [Range(-1f, 1f)] public float facingDotMinimum = .3f;
        [Tooltip("Optional. Main camera; empty = Camera.main.")]
        public Camera viewCamera;

        [Header("Labels (saved in the scene)")]
        [Tooltip("Khem's thought when he first comes close (the box around it is shown and hidden).")]
        public TMP_Text thought;
        public TMP_Text followUpThought;
        public BoxCollider thoughtZone;
        [Tooltip("Briefing zone width (X) and depth (Y) in metres. Set an axis to 0 to use the collider's original size.")]
        public Vector2 briefingZoneSizeMetres;
        public EveningDriveThoughts afterExitThoughts;
        [Min(0f)] public float thoughtSeconds = 3f;
        public TMP_Text prompt;
        public TMP_Text shutterHint;
        public bool leftClickShutterOnly;
        public TMP_Text savedLabel;

        [Header("Phone held sideways")]
        public GameObject phonePrefab;
        [Tooltip("URP Unlit material that shows the phone camera picture on the screen.")]
        public Material feedMaterial;
        [Tooltip("Viewport position of the phone: (0.5, 0.5) is the screen centre.")]
        public Vector2 screenPosition = new Vector2(.5f, .38f);
        [Min(.1f)] public float cameraDistance = .27f;
        [Tooltip("Phone rotation relative to the view. Y 180 = screen towards Khem, Z -90 = sideways (top to the left), X = tilt (negative = screen tipped up towards Khem).")]
        public Vector3 heldRotation = new Vector3(-8f, 180f, -90f);
        [Min(.01f)] public float phoneScale = 1f;
        [Tooltip("Field of view of the phone camera (what ends up in the photo).")]
        [Range(15f, 90f)] public float phoneCameraFov = 48f;
        [Tooltip("While aiming, light the scene with the same beam as Khem's flashlight (from where he holds it), so the evidence is visible at night.")]
        public bool torchWhileAiming = true;
        [Tooltip("The camera flash: this many times brighter than the flashlight beam for a moment (same beam, same shadows).")]
        [Min(1f)] public float flashStrength = 2.5f;
        [Tooltip("How long the flash takes to fade back to the torch (seconds).")]
        [Min(0f)] public float flashFadeSeconds = .18f;

        [Header("Timing")]
        [Min(.05f)] public float raiseSeconds = .5f;
        [Min(.05f)] public float lookSmoothSeconds = .25f;
        [Min(0f)] public float minimumAimSeconds = .35f;
        [Min(0f)] public float savedHoldSeconds = 1.2f;
        [Min(.05f)] public float lowerSeconds = .4f;
        [Tooltip("Never look further up/down than this while aiming.")]
        public Vector2 pitchLimits = new Vector2(-20f, 70f);

        [Header("Sound")]
        public AudioClip raiseSound;
        public AudioClip shutterSound;
        [Range(0f, 1f)] public float volume = .8f;

        [Tooltip("Runs once the photo is taken (step 4, the rain, starts from here).")]
        public UnityEvent onPhotoTaken = new UnityEvent();
        public static event Action<MidnightSkidPhoto> PhotoTaken;

        public bool Completed { get; private set; }
        public bool IsRunning { get; private set; }
        /// <summary>The photo (the frozen phone camera picture).</summary>
        public Texture Photo => photo;

        private static readonly System.Collections.Generic.List<MidnightSkidPhoto> active = new System.Collections.Generic.List<MidnightSkidPhoto>();

        /// <summary>The phone is up (or being raised / lowered).</summary>
        public static bool IsBusy(Scene scene)
        {
            foreach (var item in active)
                if (item != null && item.IsRunning && item.gameObject.scene == scene) return true;
            return false;
        }

        /// <summary>Other "[E]" prompts (the banana trees) must stay hidden: Khem's thought, the photo prompt or the phone is on screen.</summary>
        public static bool BlocksOtherPrompts(Scene scene)
        {
            foreach (var item in active)
                if (item != null && item.gameObject.scene == scene && (item.IsRunning || item.promptShown || item.thinking)) return true;
            return MidnightDebrisPickup.BlocksOtherPrompts(scene);
        }

        // ---- runtime state ----------------------------------------------------------------------------------------------------------
        private Renderer[] skidRenderers;
        private bool promptShown, thinking, thoughtDone;
        private float requiredSince = -1f;
        private GameObject phone;
        private Camera phoneCamera;
        private RenderTexture photo;
        private Renderer feed;
        private Material feedInstance;
        private Light flash;              // a copy of Khem's flashlight beam, used as the torch and the camera flash
        private float flashIntensity, flashAngle;
        private AudioSource audioSource;
        private Quaternion heldLocalRotation;
        private float raise;            // 0 = lowered out of view, 1 = held
        private bool holdingControl;
        private PhoneFlashlightView[] flashlightViews;
        private bool[] flashlightPhoneWasShown;

        private Transform cameraTarget;
        private StarterAssetsInputs[] inputs;
        private bool[] inputLookEnabled;
        private bool looking;
        private float yaw, pitch, targetYaw, targetPitch, yawVelocity, pitchVelocity;

        private void Awake()
        {
            SetThought(false);
            SetThoughtBox(followUpThought, false);
            if (requires == null)   // the first photo of a chain hides the (shared) labels once
            {
                SetLabel(prompt, false);
                SetLabel(shutterHint, false);
                SetLabel(savedLabel, false);
            }
        }

        private void Start()
        {
            if (player == null || skidMark == null || interactionZone == null || phonePrefab == null)
            { Debug.LogError("[Midnight] Assign the player, the skid mark, its Interaction Zone and the phone prefab.", this); enabled = false; return; }
            skidRenderers = skidMark.GetComponentsInChildren<Renderer>(true);   // the debris is switched off at first
        }

        private void OnEnable() { if (!active.Contains(this)) active.Add(this); }

        private void Update()
        {
            if (GameMenuController.BlocksGameplayInput) return;
            if (holdingControl && player != null) player.MovementLocked = true;
            if (looking) StepLook();
            if (Completed || IsRunning || thinking) { ShowPrompt(false); return; }
            bool ready = CanInteract();
            if (!thoughtDone && thoughtZone != null)
            {
                ShowPrompt(false);
                if (Time.timeScale > 0f && player != null && player.isActiveAndEnabled && !player.MovementLocked
                    && (afterExitThoughts == null || afterExitThoughts.AfterExitThoughtFinished)
                    && IsInZone(player.transform.position + Vector3.up * .9f, thoughtZone, briefingZoneSizeMetres))
                    StartCoroutine(Think());
                return;
            }
            if (ready && !thoughtDone) { ShowPrompt(false); StartCoroutine(Think()); return; }   // first time close: Khem notices the marks
            ShowPrompt(ready);
            if (ready && Application.isFocused && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                StartCoroutine(TakePhoto());
        }

        private IEnumerator Think()
        {
            thinking = true;
            if (thought != null)
            {
                SetThought(true);
                yield return new WaitForSeconds(thoughtSeconds);
                SetThought(false);
            }
            if (followUpThought != null)
            {
                SetThoughtBox(followUpThought, true);
                yield return new WaitForSeconds(thoughtSeconds);
                SetThoughtBox(followUpThought, false);
            }
            thinking = false;
            thoughtDone = true;
        }

        // Several photos can share the same labels, so a label is only switched when this photo's own state changes.
        private void ShowPrompt(bool show)
        {
            if (promptShown == show) return;
            promptShown = show;
            SetLabel(prompt, show);
        }

        private void SetThought(bool show) => SetThoughtBox(thought, show);

        private static void SetThoughtBox(TMP_Text thought, bool show)
        {
            if (thought == null) return;
            var box = thought.transform.parent != null ? thought.transform.parent.gameObject : thought.gameObject;
            if (!thought.gameObject.activeSelf) thought.gameObject.SetActive(true);
            if (box.activeSelf != show) box.SetActive(show);
        }

        private bool Available()
        {
            if (requires == null && requiresPickup == null) return true;
            bool done = (requires == null || (requires.Completed && !requires.IsRunning))
                        && (requiresPickup == null || (requiresPickup.Completed && !requiresPickup.IsRunning));
            if (!done) { requiredSince = -1f; return false; }
            if (requiredSince < 0f) requiredSince = Time.time;
            return Time.time - requiredSince >= availableDelay;
        }

        private bool CanInteract()
        {
            if (Time.timeScale <= 0f || player == null || skidMark == null || interactionZone == null) return false;
            if (!skidMark.gameObject.activeInHierarchy) return false;   // hidden (the debris before it appears) or picked up
            if (!Available()) return false;
            if (!player.isActiveAndEnabled || player.MovementLocked) return false;
            if (EveningDriveIntro.IsInputBlocked(gameObject.scene)) return false;
            if (!IsInZone(player.transform.position + Vector3.up * .9f, interactionZone, photoZoneSizeMetres)) return false;
            var view = ViewCamera();
            if (view == null) return false;
            Vector3 toMark = AimPoint() - view.transform.position;
            toMark.y = 0f;
            Vector3 forward = view.transform.forward;
            forward.y = 0f;
            if (toMark.sqrMagnitude < .25f || forward.sqrMagnitude < .0001f) return true;   // standing on it
            return Vector3.Dot(forward.normalized, toMark.normalized) >= facingDotMinimum;
        }

        private Camera ViewCamera() => viewCamera != null ? viewCamera : Camera.main;

        private Vector3 AimPoint()
        {
            var renderers = skidRenderers;
            if (renderers == null || renderers.Length == 0) return skidMark.position;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.center;
        }

        // ---- the sequence ---------------------------------------------------------------------------------------------------------------
        private IEnumerator TakePhoto()
        {
            IsRunning = true;
            ShowPrompt(false);
            holdingControl = true;
            player.MovementLocked = true;
            if (!EnsurePhone()) { Finish(); yield break; }
            HideFlashlightPhone();
            BeginLook();

            // raise the phone sideways; the view turns to the skid mark at the same time
            Play(raiseSound);
            phone.SetActive(true);
            SetTorch(torchWhileAiming ? 1f : 0f);
            phoneCamera.enabled = true;
            feedInstance.mainTexture = photo;
            for (float t = 0f; t < raiseSeconds; t += Time.deltaTime) { raise = Smooth(t / raiseSeconds); yield return null; }
            raise = 1f;
            PosePhone();

            // aim: wait for E or a click
            SetLabel(shutterHint, true);
            float aimed = 0f;
            while (true)
            {
                aimed += Time.deltaTime;
                bool pressed = !GameMenuController.BlocksGameplayInput && Application.isFocused && ((!leftClickShutterOnly && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                                                         || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame));
                if (pressed && aimed >= minimumAimSeconds) break;
                yield return null;
            }
            SetLabel(shutterHint, false);

            // shutter: sound, flash, white screen for a moment, then the picture freezes on the screen
            Play(shutterSound);
            SetTorch(flashStrength);
            feedInstance.mainTexture = Texture2D.whiteTexture;
            yield return new WaitForSeconds(.07f);
            phoneCamera.enabled = false;                 // the last rendered frame (lit by the flash) stays as the photo
            feedInstance.mainTexture = photo;
            float after = torchWhileAiming ? 1f : 0f;
            for (float t = 0f; t < flashFadeSeconds; t += Time.deltaTime) { SetTorch(Mathf.Lerp(flashStrength, after, Smooth(t / flashFadeSeconds))); yield return null; }
            SetTorch(after);
            Completed = true;
            SetLabel(savedLabel, true);
            yield return new WaitForSeconds(savedHoldSeconds);
            SetLabel(savedLabel, false);

            // lower the phone and give the view back
            SetTorch(0f);
            for (float t = 0f; t < lowerSeconds; t += Time.deltaTime) { raise = 1f - Smooth(t / lowerSeconds); yield return null; }
            raise = 0f;
            phone.SetActive(false);
            EndLook();
            RestoreFlashlightPhone();
            Finish();
            onPhotoTaken.Invoke();
            PhotoTaken?.Invoke(this);
        }

        private static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        private void Finish()
        {
            holdingControl = false;
            if (player != null) player.MovementLocked = false;
            IsRunning = false;
        }

        private void LateUpdate()
        {
            if (phone == null || !phone.activeSelf) return;
            PosePhone();
            // the flashlight phone must not appear on top of the camera phone
            if (flashlightViews != null)
                foreach (var view in flashlightViews)
                    if (view != null && view.heldPhone != null && view.heldPhone.activeSelf) view.heldPhone.SetActive(false);
        }

        // ---- the phone ----------------------------------------------------------------------------------------------------------------
        private bool EnsurePhone()
        {
            var view = ViewCamera();
            if (view == null || phonePrefab == null) return false;
            if (phone != null) { if (phone.transform.parent != view.transform) phone.transform.SetParent(view.transform, false); return true; }

            heldLocalRotation = Quaternion.Euler(heldRotation);
            phone = Instantiate(phonePrefab, view.transform);
            phone.name = "CameraPhone";
            foreach (var script in phone.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;   // the flashlight screen switcher
            foreach (var collider in phone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var renderer in phone.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                if (renderer.name == "Screen") renderer.enabled = false;   // the camera picture covers the screen
            }
            float ownIntensity = 0f;
            foreach (var light in phone.GetComponentsInChildren<Light>(true)) { ownIntensity = Mathf.Max(ownIntensity, light.intensity); light.enabled = false; }
            if (flash == null) flash = MakeFlash(view, ownIntensity);   // the camera phone's own torch stays off

            // what the phone camera sees -> a picture in landscape (the screen is 155.65 x 72 mm)
            photo = new RenderTexture(704, 320, 24) { name = "SkidMarkPhoto", antiAliasing = 1 };
            photo.Create();
            var cameraObject = new GameObject("PhoneCamera");
            phoneCamera = cameraObject.AddComponent<Camera>();
            phoneCamera.CopyFrom(view);                                                   // also copies the main camera's position, so place it afterwards
            cameraObject.transform.SetParent(phone.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -.012f);          // just past the back of the phone
            cameraObject.transform.localRotation = Quaternion.Inverse(heldLocalRotation); // looks where Khem looks, not tilted
            cameraObject.transform.localScale = Vector3.one;
            phoneCamera.targetTexture = photo;
            phoneCamera.fieldOfView = phoneCameraFov;
            phoneCamera.nearClipPlane = .03f;
            phoneCamera.depth = view.depth - 1f;
            phoneCamera.enabled = false;
            var mainData = view.GetUniversalAdditionalCameraData();
            var phoneData = phoneCamera.GetUniversalAdditionalCameraData();
            if (mainData != null && phoneData != null)
            {
                phoneData.renderPostProcessing = mainData.renderPostProcessing;
                phoneData.volumeLayerMask = mainData.volumeLayerMask;
            }

            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "CameraFeed";
            var quadCollider = screen.GetComponent<Collider>();
            if (quadCollider != null) Destroy(quadCollider);
            screen.transform.SetParent(phone.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0f, .0052f);              // on the screen glass, facing Khem
            // upright in landscape and flat on the (tilted) screen, otherwise half of it ends up inside the phone
            screen.transform.localRotation = Quaternion.Inverse(heldLocalRotation) * Quaternion.Euler(-heldRotation.x, 0f, 0f);
            screen.transform.localScale = new Vector3(.150f, .0685f, 1f);
            feed = screen.GetComponent<Renderer>();
            feed.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            feed.receiveShadows = false;
            var baseMaterial = feedMaterial != null ? feedMaterial : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            feedInstance = new Material(baseMaterial) { name = "CameraFeed (runtime)" };
            if (feedInstance.HasProperty("_BaseColor")) feedInstance.SetColor("_BaseColor", Color.white);
            feed.sharedMaterial = feedInstance;

            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            raise = 0f;
            PosePhone();
            phone.SetActive(false);
            return true;
        }

        private void PosePhone()
        {
            var view = ViewCamera();
            if (view == null || phone == null) return;
            float distance = Mathf.Max(cameraDistance, view.nearClipPlane + .03f);
            // held pose in front of the view; lowered = further down and tipped forward
            Vector3 held = view.transform.InverseTransformPoint(view.ViewportToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance)));
            Vector3 lowered = held + new Vector3(.04f, -.24f, -.04f);
            phone.transform.localPosition = Vector3.LerpUnclamped(lowered, held, raise);
            phone.transform.localRotation = Quaternion.Euler(35f * (1f - raise), 0f, 0f) * heldLocalRotation;
            phone.transform.localScale = Vector3.one * phoneScale;
        }

        private void SetTorch(float multiplier)
        {
            if (flash == null) return;
            flash.enabled = multiplier > 0f;
            flash.intensity = flashIntensity * Mathf.Max(0f, multiplier);
            // the flash pops a little wider than the torch beam
            if (flash.type == LightType.Spot) flash.spotAngle = Mathf.Min(150f, flashAngle * (1f + .2f * Mathf.Clamp01(multiplier - 1f)));
        }

        // The torch / flash: the same beam as Khem's flashlight (cone, softness, colour, cookie, shadows), shining from where he
        // holds the flashlight phone (lower right), so the light has depth and shadows instead of lighting everything flat from the lens.
        private Light MakeFlash(Camera view, float ownIntensity)
        {
            var holder = new GameObject("PhotoFlash");
            holder.transform.SetParent(view.transform, false);
            var light = holder.AddComponent<Light>();
            Vector3 localPosition = new Vector3(.16f, -.14f, .22f);
            Quaternion localRotation = Quaternion.Euler(-2f, -4f, 0f);
            Light beam = FlashlightBeam(view);
            if (beam != null)
            {
                Vector3 position = view.transform.InverseTransformPoint(beam.transform.position);
                if (position.magnitude < 1.5f)
                {
                    localPosition = position;
                    localRotation = Quaternion.Inverse(view.transform.rotation) * beam.transform.rotation;
                }
                light.type = beam.type;
                light.spotAngle = beam.spotAngle;
                light.innerSpotAngle = beam.innerSpotAngle;
                light.range = beam.range;
                light.color = beam.color;
                light.useColorTemperature = beam.useColorTemperature;
                light.colorTemperature = beam.colorTemperature;
                light.cookie = beam.cookie;
                light.shadows = beam.shadows;
                light.shadowStrength = beam.shadowStrength;
                light.shadowBias = beam.shadowBias;
                light.shadowNormalBias = beam.shadowNormalBias;
                light.shadowNearPlane = beam.shadowNearPlane;
                light.cullingMask = beam.cullingMask;
                light.renderMode = beam.renderMode;
                flashIntensity = beam.intensity > .01f ? beam.intensity : ownIntensity;
            }
            else
            {
                light.type = LightType.Spot;
                light.spotAngle = 62f;
                light.innerSpotAngle = 28f;
                light.range = 22f;
                light.color = new Color(1f, .96f, .9f);
                light.shadows = LightShadows.Soft;
                flashIntensity = ownIntensity;
            }
            if (flashIntensity <= .01f) flashIntensity = 2f;
            holder.transform.localPosition = localPosition;
            holder.transform.localRotation = localRotation;
            flashAngle = light.spotAngle;
            light.intensity = 0f;
            light.enabled = false;
            return light;
        }

        // The light of Khem's flashlight phone (FlashlightAnchor on the phone PhoneFlashlightView holds), if this scene has one.
        private Light FlashlightBeam(Camera view)
        {
            foreach (var flashlight in FindObjectsByType<PhoneFlashlightView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (flashlight == null || flashlight.gameObject.scene != gameObject.scene || flashlight.heldPhone == null) continue;
                foreach (var anchor in flashlight.heldPhone.GetComponentsInChildren<Transform>(true))
                {
                    if (anchor.name != "FlashlightAnchor") continue;
                    var light = anchor.GetComponent<Light>();
                    if (light == null) light = anchor.GetComponentInChildren<Light>(true);
                    if (light != null) return light;
                }
            }
            return null;
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, volume);
        }

        private void HideFlashlightPhone()
        {
            flashlightViews = FindObjectsByType<PhoneFlashlightView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            flashlightPhoneWasShown = new bool[flashlightViews.Length];
            for (int i = 0; i < flashlightViews.Length; i++)
            {
                var held = flashlightViews[i] != null ? flashlightViews[i].heldPhone : null;
                flashlightPhoneWasShown[i] = held != null && held.activeSelf;
                if (flashlightPhoneWasShown[i]) held.SetActive(false);
            }
        }

        private void RestoreFlashlightPhone()
        {
            if (flashlightViews == null) return;
            for (int i = 0; i < flashlightViews.Length; i++)
                if (flashlightPhoneWasShown[i] && flashlightViews[i] != null && flashlightViews[i].heldPhone != null)
                    flashlightViews[i].heldPhone.SetActive(true);
            flashlightViews = null;
        }

        // ---- Khem's view ------------------------------------------------------------------------------------------------------------------
        private void BeginLook()
        {
            if (player.CinemachineCameraTarget == null) return;
            cameraTarget = player.CinemachineCameraTarget.transform;
            inputs = player.transform.root.GetComponentsInChildren<StarterAssetsInputs>(true);
            inputLookEnabled = new bool[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) { inputLookEnabled[i] = inputs[i].cursorInputForLook; inputs[i].cursorInputForLook = false; }
            yaw = player.transform.eulerAngles.y;
            pitch = Mathf.DeltaAngle(0f, cameraTarget.localEulerAngles.x);
            Vector3 direction = AimPoint() - cameraTarget.position;
            targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(-Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg, pitchLimits.x, pitchLimits.y);
            yawVelocity = pitchVelocity = 0f;
            looking = true;
        }

        private void StepLook()
        {
            if (player == null || cameraTarget == null) { looking = false; return; }
            yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, lookSmoothSeconds);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, lookSmoothSeconds);
            player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            if (inputs != null) foreach (var input in inputs) if (input != null) input.look = Vector2.zero;
        }

        private void EndLook()
        {
            if (!looking) return;
            looking = false;
            if (player != null && cameraTarget != null) player.SyncLookPitch(pitch);
            if (inputs != null)
                for (int i = 0; i < inputs.Length; i++)
                    if (inputs[i] != null) { inputs[i].look = Vector2.zero; inputs[i].cursorInputForLook = inputLookEnabled[i]; }
            inputs = null;
        }

        // ---- misc -------------------------------------------------------------------------------------------------------------------------
        private static void SetLabel(TMP_Text label, bool show)
        {
            if (label != null && label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
        }

        private void OnDrawGizmosSelected()
        {
            DrawZone(interactionZone, photoZoneSizeMetres, Color.cyan);
            DrawZone(thoughtZone, briefingZoneSizeMetres, Color.yellow);
        }

        private static bool IsInZone(Vector3 point, BoxCollider zone, Vector2 sizeMetres)
        {
            return zone != null && zone.enabled && zone.gameObject.activeInHierarchy
                && new Bounds(zone.center, GetZoneSize(zone, sizeMetres)).Contains(zone.transform.InverseTransformPoint(point));
        }

        private static Vector3 GetZoneSize(BoxCollider zone, Vector2 sizeMetres)
        {
            Vector3 size = zone.size;
            if (sizeMetres.x > 0f)
                size.x = sizeMetres.x / Mathf.Max(.0001f, zone.transform.TransformVector(Vector3.right).magnitude);
            if (sizeMetres.y > 0f)
                size.z = sizeMetres.y / Mathf.Max(.0001f, zone.transform.TransformVector(Vector3.forward).magnitude);
            return size;
        }

        private static void DrawZone(BoxCollider zone, Vector2 sizeMetres, Color color)
        {
            if (zone == null) return;
            Color previous = Gizmos.color;
            Matrix4x4 saved = Gizmos.matrix;
            Gizmos.color = color;
            Gizmos.matrix = zone.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(zone.center, GetZoneSize(zone, sizeMetres));
            Gizmos.matrix = saved;
            Gizmos.color = previous;
        }

        /// <summary>Testing (F3): this photo counts as taken; a photo in progress is put away.</summary>
        public void MarkDoneForTesting()
        {
            StopPhoto();
            thoughtDone = true;
            Completed = true;
        }

        private void OnDisable()
        {
            active.Remove(this);
            StopPhoto();
        }

        private void StopPhoto()
        {
            StopAllCoroutines();
            if (thinking) { thinking = false; thoughtDone = true; }
            SetThought(false);
            SetThoughtBox(followUpThought, false);
            ShowPrompt(false);
            if (phone != null) phone.SetActive(false);
            if (phoneCamera != null) phoneCamera.enabled = false;
            SetTorch(0f);
            EndLook();
            RestoreFlashlightPhone();
            if (holdingControl && player != null) player.MovementLocked = false;
            holdingControl = false;
            if (IsRunning) { SetLabel(shutterHint, false); SetLabel(savedLabel, false); }
            IsRunning = false;
        }

        private void OnDestroy()
        {
            if (phone != null) Destroy(phone);
            if (flash != null) Destroy(flash.gameObject);
            if (feedInstance != null) Destroy(feedInstance);
            if (photo != null) { photo.Release(); Destroy(photo); }
        }
    }
}
