using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    // MainScene_2_MidNight, step 9: once a piece of car debris has been photographed, E picks it up. The piece comes up in front
    // of Khem's view, turns slowly while he looks at it, then he puts it away (the piece is switched off).
    // Khem cannot walk while he holds it, but he can still look around (the piece stays in front of his view).
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(15040)]
    public sealed class MidnightDebrisPickup : MonoBehaviour
    {
        [Header("What")]
        public FirstPersonController player;
        [Tooltip("The debris piece (switched off once he puts it away).")]
        public Transform piece;
        [Tooltip("It can be picked up once this photo has been taken.")]
        public MidnightSkidPhoto afterPhoto;
        [Tooltip("Khem has to stand in this box (his body, 0.9 m above his feet).")]
        public BoxCollider interactionZone;
        [Tooltip("How directly Khem must face the piece (1 = straight at it, 0 = anything in front of him).")]
        [Range(-1f, 1f)] public float facingDotMinimum = .2f;
        [Tooltip("Empty = the main camera.")]
        public Camera viewCamera;

        [Header("Prompt")]
        public TMP_Text prompt;

        [Header("Looking at it")]
        [Min(.05f)] public float liftSeconds = .55f;
        [Min(0f)] public float holdSeconds = 1.8f;
        [Min(.05f)] public float stowSeconds = .45f;
        [Tooltip("How much of the view the piece fills while he looks at it.")]
        [Range(.2f, .9f)] public float viewFill = .5f;
        [Tooltip("Where on the screen he holds it (0-1, 0.5 = middle).")]
        public Vector2 screenPosition = new Vector2(.5f, .44f);
        [Tooltip("How fast it turns while he looks at it (degrees per second).")]
        public float turnSpeed = 32f;

        [Header("Sound")]
        public AudioClip pickupSound;
        public AudioClip stowSound;
        [Range(0f, 1f)] public float volume = .85f;

        public UnityEvent onPickedUp = new UnityEvent();

        public bool Completed { get; private set; }
        public bool IsRunning { get; private set; }

        private static readonly List<MidnightDebrisPickup> active = new List<MidnightDebrisPickup>();

        // True while a pickup prompt is up or a piece is being held (other "[E]" prompts stay hidden then).
        public static bool BlocksOtherPrompts(Scene scene)
        {
            foreach (var item in active)
                if (item != null && item.gameObject.scene == scene && (item.IsRunning || item.promptShown)) return true;
            return false;
        }

        private bool promptShown, holdingControl;
        private float readyAt = -1f;
        private AudioSource audioSource;

        private void OnEnable() { if (!active.Contains(this)) active.Add(this); }

        private void Update()
        {
            if (holdingControl && player != null) player.MovementLocked = true;
            if (Completed || IsRunning) { ShowPrompt(false); return; }
            bool ready = CanInteract();
            ShowPrompt(ready);
            if (ready && Application.isFocused && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                StartCoroutine(PickUp());
        }

        private bool CanInteract()
        {
            if (Time.timeScale <= 0f || player == null || piece == null || interactionZone == null) return false;
            if (!piece.gameObject.activeInHierarchy) return false;
            if (afterPhoto != null && (!afterPhoto.Completed || afterPhoto.IsRunning)) { readyAt = -1f; return false; }
            if (readyAt < 0f) readyAt = Time.time + .35f;   // the E that took the photo must not pick it up too
            if (Time.time < readyAt) return false;
            if (!player.isActiveAndEnabled || player.MovementLocked) return false;
            if (EveningDriveIntro.IsInputBlocked(gameObject.scene) || MidnightSkidPhoto.IsBusy(gameObject.scene)) return false;
            if (!InteractionLook.IsPointInBox(player.transform.position + Vector3.up * .9f, interactionZone)) return false;
            var view = ViewCamera();
            if (view == null) return false;
            Vector3 toPiece = Centre(PieceBounds()) - view.transform.position;
            toPiece.y = 0f;
            Vector3 forward = view.transform.forward;
            forward.y = 0f;
            if (toPiece.sqrMagnitude < .25f || forward.sqrMagnitude < .0001f) return true;   // standing on it
            return Vector3.Dot(forward.normalized, toPiece.normalized) >= facingDotMinimum;
        }

        private IEnumerator PickUp()
        {
            IsRunning = true;
            ShowPrompt(false);
            holdingControl = true;
            player.MovementLocked = true;
            var view = ViewCamera();

            // nothing on it may touch Khem or the floor while he holds it
            foreach (var collider in piece.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in piece.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            foreach (var renderer in piece.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.Off;

            Bounds bounds = PieceBounds();
            Vector3 startPosition = piece.position;
            Quaternion startRotation = piece.rotation;
            Vector3 centreOffset = Quaternion.Inverse(startRotation) * (Centre(bounds) - startPosition);   // the pivot may not be in the middle
            float radius = Mathf.Max(.05f, bounds.extents.magnitude);
            float fov = view != null ? view.fieldOfView : 60f;
            float near = view != null ? view.nearClipPlane : .1f;
            float distance = Mathf.Max(radius / Mathf.Tan(fov * .5f * Mathf.Deg2Rad) / viewFill, radius + near + .08f);
            Quaternion relative = view != null ? Quaternion.Inverse(view.transform.rotation) * startRotation : Quaternion.identity;
            float spin = 0f;
            Vector3 heldPosition;
            Quaternion heldRotation;

            // up from the floor into his hands (a small arc)
            Play(pickupSound);
            for (float t = 0f; t < liftSeconds; t += Time.deltaTime)
            {
                float k = Smooth(t / liftSeconds);
                spin += turnSpeed * .5f * Time.deltaTime;
                Hold(view, distance, relative, spin, centreOffset, out heldPosition, out heldRotation);
                Vector3 position = Vector3.Lerp(startPosition, heldPosition, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * .15f;
                piece.SetPositionAndRotation(position, Quaternion.Slerp(startRotation, heldRotation, k));
                yield return null;
            }

            // he looks at it, turning it slowly
            for (float t = 0f; t < holdSeconds; t += Time.deltaTime)
            {
                spin += turnSpeed * Time.deltaTime;
                Hold(view, distance, relative, spin, centreOffset, out heldPosition, out heldRotation);
                piece.SetPositionAndRotation(heldPosition, heldRotation);
                yield return null;
            }

            // puts it away (down out of the view)
            Play(stowSound);
            for (float t = 0f; t < stowSeconds; t += Time.deltaTime)
            {
                float k = Smooth(t / stowSeconds);
                spin += turnSpeed * Time.deltaTime;
                Hold(view, distance, relative, spin, centreOffset, out heldPosition, out heldRotation);
                Vector3 down = view != null ? -view.transform.up : Vector3.down;
                piece.SetPositionAndRotation(heldPosition + down * distance * 1.3f * k, heldRotation);
                yield return null;
            }
            piece.gameObject.SetActive(false);

            Completed = true;
            holdingControl = false;
            if (player != null) player.MovementLocked = false;
            IsRunning = false;
            onPickedUp.Invoke();
        }

        // Where the piece goes so its middle sits at the hold point in front of the view.
        private void Hold(Camera view, float distance, Quaternion relative, float spin, Vector3 centreOffset, out Vector3 position, out Quaternion rotation)
        {
            if (view == null) { position = piece.position; rotation = piece.rotation; return; }
            Vector3 centre = view.ViewportToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance));
            rotation = Quaternion.AngleAxis(spin, view.transform.up) * view.transform.rotation * relative;
            position = centre - rotation * centreOffset;
        }

        private Bounds PieceBounds()
        {
            bool any = false;
            var bounds = new Bounds(piece.position, Vector3.zero);
            foreach (var renderer in piece.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer) continue;
                if (!any) { bounds = renderer.bounds; any = true; } else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private static Vector3 Centre(Bounds bounds) => bounds.center;

        private Camera ViewCamera() => viewCamera != null ? viewCamera : Camera.main;

        private static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        private void ShowPrompt(bool show)
        {
            if (promptShown == show) return;
            promptShown = show;
            if (prompt == null) return;
            if (prompt.gameObject.activeSelf != show) prompt.gameObject.SetActive(show);
        }

        private void Play(AudioClip clip)
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

        private void OnDisable()
        {
            active.Remove(this);
            StopAllCoroutines();
            ShowPrompt(false);
            if (holdingControl && player != null) player.MovementLocked = false;
            holdingControl = false;
            if (IsRunning && piece != null) piece.gameObject.SetActive(false);   // stopped half way: it counts as put away
            if (IsRunning) Completed = true;
            IsRunning = false;
        }
    }
}
