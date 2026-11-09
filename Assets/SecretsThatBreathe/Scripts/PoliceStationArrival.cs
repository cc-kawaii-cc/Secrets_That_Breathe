using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;

namespace SecretsThatBreathe
{
    [DefaultExecutionOrder(-10000)]
    public sealed class PoliceStationArrival : MonoBehaviour
    {
        public CanvasGroup blackScreen;
        [Tooltip("Feet position and facing direction. Move/rotate this marker in the scene.")]
        public Transform arrivalPoint;
        public FirstPersonController player;
        [Tooltip("Initialize an unprepared arrival marker from Door (4). The scene setup turns this off after saving the exact position.")]
        public bool initializeArrivalFromDoor;
        [Min(0f)] public float blackHoldSeconds = 2f;
        [Min(.05f)] public float revealSeconds = 1.5f;
        [Min(0f)] public float maximumStreamingWaitSeconds = 10f;

        [Header("Optional arrival narration")]
        public TMP_Text storyText;
        public CanvasGroup storyGroup;
        public AudioSource typingAudio;
        [Min(.001f)] public float secondsPerCharacter = .06f;
        [Min(0f)] public float storyHoldSeconds = 2f;
        [Min(0f)] public float storyFadeSeconds = 1f;

        readonly List<Behaviour> suspended = new List<Behaviour>();
        bool wasMovementLocked, ownsControls;

        void Awake()
        {
            if (player == null)
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    player = root.GetComponentInChildren<FirstPersonController>();
                    if (player != null) break;
                }
            if (blackScreen == null || arrivalPoint == null || player == null)
            { Debug.LogError("Assign the station arrival screen, point and player.", this); enabled = false; return; }
            blackScreen.alpha = 1f;
            blackScreen.blocksRaycasts = true;
            if (storyGroup != null) storyGroup.alpha = 0f;
            if (storyText != null) storyText.maxVisibleCharacters = 0;
            wasMovementLocked = player.MovementLocked;
            player.MovementLocked = true;
            ownsControls = true;
            foreach (var control in player.transform.root.GetComponentsInChildren<Behaviour>(true))
            {
                if (!control.enabled) continue;
                switch (control.GetType().Name)
                {
                    case "PlayerInput": case "FirstPersonController":
                    case "FirstPersonFlashlight": case "PhoneFlashlightView":
                        suspended.Add(control); control.enabled = false; break;
                }
            }
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }

        IEnumerator Start()
        {
            if (!ownsControls) yield break;
            // Allow other Start methods and the camera rig to initialize behind black.
            yield return null;
            PlacePlayer();
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(blackHoldSeconds);
            if (storyText != null && storyGroup != null)
                yield return PlayNarration();
            float elapsed = 0f; int readyFrames = 0;
            while (readyFrames < 3 && elapsed < maximumStreamingWaitSeconds)
            {
                readyFrames = Texture.streamingTexturePendingLoadCount == 0
                    && Texture.streamingTextureLoadingCount == 0 ? readyFrames + 1 : 0;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            for (elapsed = 0f; elapsed < revealSeconds; elapsed += Time.unscaledDeltaTime)
            {
                blackScreen.alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(.05f, revealSeconds));
                yield return null;
            }
            blackScreen.alpha = 0f; blackScreen.blocksRaycasts = false;
            ReleaseControls();
        }

        IEnumerator PlayNarration()
        {
            storyGroup.alpha = 1f;
            storyText.ForceMeshUpdate();
            int characterCount = storyText.textInfo.characterCount;
            if (characterCount > 0 && typingAudio != null && typingAudio.clip != null)
                typingAudio.Play();
            float elapsed = 0f;
            while (storyText.maxVisibleCharacters < characterCount)
            {
                elapsed += Time.unscaledDeltaTime;
                storyText.maxVisibleCharacters = Mathf.Min(characterCount,
                    Mathf.FloorToInt(elapsed / Mathf.Max(.001f, secondsPerCharacter)));
                if (storyText.maxVisibleCharacters < characterCount) yield return null;
            }
            if (typingAudio != null) typingAudio.Stop();
            yield return new WaitForSecondsRealtime(storyHoldSeconds);
            for (elapsed = 0f; elapsed < storyFadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                storyGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / storyFadeSeconds);
                yield return null;
            }
            storyGroup.alpha = 0f;
        }

        void PlacePlayer()
        {
            if (initializeArrivalFromDoor)
            {
                foreach (var sceneRoot in gameObject.scene.GetRootGameObjects())
                foreach (var item in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (item.name != "Door (4)") continue;
                    var renderers = item.GetComponentsInChildren<Renderer>();
                    if (renderers.Length == 0) continue;
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    Vector3 inward = Vector3.ProjectOnPlane(item.forward, Vector3.up).normalized;
                    if (Vector3.Dot(inward, player.transform.position - bounds.center) < 0f) inward = -inward;
                    Vector3 position = bounds.center + inward * 1.4f; position.y = bounds.min.y;
                    arrivalPoint.SetPositionAndRotation(position, Quaternion.LookRotation(inward));
                    initializeArrivalFromDoor = false;
                }
            }
            Transform root = player.transform.root;
            var controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            root.rotation = Quaternion.Euler(0f, arrivalPoint.eulerAngles.y, 0f);
            player.transform.rotation = root.rotation;
            Vector3 feet = arrivalPoint.position;
            // Snap the feet to a supporting floor, excluding this player's colliders.
            float nearest = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(feet + Vector3.up * 1.5f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.gameObject.scene == gameObject.scene && !hit.transform.IsChildOf(root) && hit.normal.y > .5f && hit.distance < nearest)
                { nearest = hit.distance; feet.y = hit.point.y; }
            float bottom = controller != null
                ? (controller.center.y - controller.height * .5f) * player.transform.lossyScale.y : 0f;
            root.position += feet + Vector3.up * (.015f - bottom) - player.transform.position;
            if (controller != null) controller.enabled = wasEnabled;
            Physics.SyncTransforms();
            if (wasEnabled) controller.Move(Vector3.down * .05f);
            player.ResetAfterTeleport(0f);
            foreach (var input in root.GetComponentsInChildren<StarterAssetsInputs>(true))
            { input.move = input.look = Vector2.zero; input.jump = input.sprint = false; }
        }

        void ReleaseControls()
        {
            if (!ownsControls) return;
            ownsControls = false;
            player.MovementLocked = wasMovementLocked;
            foreach (var control in suspended) if (control != null) control.enabled = true;
            suspended.Clear();
        }
        void OnDisable()
        {
            if (typingAudio != null) typingAudio.Stop();
            ReleaseControls();
        }
    }
}
