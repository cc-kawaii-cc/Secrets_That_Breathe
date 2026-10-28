using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class OfficeDoorExitInteraction : MonoBehaviour
    {
        public PlayerSeatController player;
        public DrawerSearchScareSequence story;
        [Header("Interaction area (drag its green handles in Scene view)")]
        public BoxCollider interactionZone;
        [Header("Crosshair target (resize this separate Box Collider)")]
        public BoxCollider lookTarget;
        [Header("Saved UI on SeatingCanvas")]
        public TMP_Text promptText;
        public CanvasGroup fadeOverlay;
        [Header("Interaction")]
        [Min(.05f)] public float fadeOutSeconds = 1.5f;
        public string destinationScene = "MainScene_2_Evening";

        private bool unlocked, transitioning, ownsPause, previousInspection;
        private float previousTimeScale;
        private readonly List<Behaviour> suspended = new List<Behaviour>();

        private void Awake()
        {
            ShowPrompt(false);
            if (fadeOverlay != null) { fadeOverlay.alpha = 0f; fadeOverlay.blocksRaycasts = false; }
        }

        private void Update()
        {
            if (!unlocked && story != null && story.Stage == DrawerSearchScareSequence.StoryStage.Completed
                && story.exitObjectiveText != null && story.exitObjectiveText.gameObject.activeInHierarchy)
                unlocked = true;
            bool available = CanInteract();
            ShowPrompt(available);
            if (available && ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)))
                TryInteract();
        }

        public bool CanInteract()
        {
            if (!isActiveAndEnabled || !unlocked || transitioning || Time.timeScale <= 0f
                || player == null || !player.CanInteractWithRoom || promptText == null
                || fadeOverlay == null) return false;
            Camera view = player.playerCamera != null ? player.playerCamera : Camera.main;
            if (view == null) return false;
            Vector3 position = player.playerBody != null ? player.playerBody.position : player.transform.position;
            return InteractionLook.IsPointInBox(position, interactionZone)
                && InteractionLook.IsLookingAtBox(view, lookTarget);
        }

        public bool TryInteract()
        {
            if (!CanInteract()) return false;
            if (!Application.CanStreamedLevelBeLoaded(destinationScene))
            {
                Debug.LogError("Office exit destination must be enabled in Build Settings: " + destinationScene, this);
                return false;
            }
            transitioning = true;
            ShowPrompt(false);
            previousTimeScale = Time.timeScale;
            previousInspection = player.ItemInspectionActive;
            player.ItemInspectionActive = true;
            ownsPause = true;
            Time.timeScale = 0f;
            Suspend(player.firstPersonController);
            if (player.additionalControls != null)
                foreach (var control in player.additionalControls) Suspend(control);
            fadeOverlay.blocksRaycasts = true;
            StartCoroutine(Exit());
            return true;
        }

        private void Suspend(Behaviour control)
        {
            if (control == null || !control.enabled || suspended.Contains(control)) return;
            suspended.Add(control);
            control.enabled = false;
        }

        private IEnumerator Exit()
        {
            float start = fadeOverlay.alpha;
            float duration = Mathf.Max(.05f, fadeOutSeconds);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                fadeOverlay.alpha = Mathf.Lerp(start, 1f, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            fadeOverlay.alpha = 1f;
            // Render one fully black frame before scene loading begins.
            yield return null;
            Time.timeScale = previousTimeScale;
            var loading = SceneManager.LoadSceneAsync(destinationScene, LoadSceneMode.Single);
            if (loading == null)
            {
                RestoreControls();
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
                transitioning = false;
            }
        }

        private void ShowPrompt(bool visible)
        {
            if (promptText != null) promptText.transform.parent.gameObject.SetActive(visible);
        }

        private void RestoreControls()
        {
            if (!ownsPause) return;
            ownsPause = false;
            Time.timeScale = previousTimeScale;
            if (player != null) player.ItemInspectionActive = previousInspection;
            foreach (var control in suspended) if (control != null) control.enabled = true;
            suspended.Clear();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ShowPrompt(false);
            RestoreControls();
            if (fadeOverlay != null) { fadeOverlay.alpha = 0f; fadeOverlay.blocksRaycasts = false; }
        }
    }
}
