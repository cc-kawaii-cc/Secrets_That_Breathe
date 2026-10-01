using System.Reflection;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe.FinalGameplay
{
    /// <summary>
    /// Keeps story code independent from the concrete Starter Assets controller.
    /// </summary>
    public sealed class STBPlayerBridge : MonoBehaviour
    {
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private StarterAssetsInputs starterInputs;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private FirstPersonFlashlight flashlight;

        public Camera PlayerCamera => playerCamera;
        public Transform PlayerBody => characterController != null
            ? characterController.transform
            : firstPersonController != null ? firstPersonController.transform : transform;

        public bool ControlsEnabled { get; private set; } = true;

        private void Awake()
        {
            AutoBind();
        }

        public void Configure(
            FirstPersonController controller,
            StarterAssetsInputs inputs,
            CharacterController capsule,
            PlayerInput input,
            Camera camera,
            FirstPersonFlashlight playerFlashlight)
        {
            firstPersonController = controller;
            starterInputs = inputs;
            characterController = capsule;
            playerInput = input;
            playerCamera = camera;
            flashlight = playerFlashlight;
        }

        public void AutoBind()
        {
            if (firstPersonController == null)
                firstPersonController = GetComponentInChildren<FirstPersonController>(true);
            if (starterInputs == null)
                starterInputs = GetComponentInChildren<StarterAssetsInputs>(true);
            if (characterController == null)
                characterController = GetComponentInChildren<CharacterController>(true);
            if (playerInput == null)
                playerInput = GetComponentInChildren<PlayerInput>(true);
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>(true);
            if (flashlight == null)
                flashlight = GetComponentInChildren<FirstPersonFlashlight>(true);
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        public void SetControlEnabled(bool enabled)
        {
            AutoBind();
            ControlsEnabled = enabled;

            if (!enabled && starterInputs != null)
            {
                starterInputs.MoveInput(Vector2.zero);
                starterInputs.LookInput(Vector2.zero);
                starterInputs.JumpInput(false);
                starterInputs.SprintInput(false);
            }

            if (firstPersonController != null)
                firstPersonController.enabled = enabled;
            if (starterInputs != null)
            {
                starterInputs.cursorInputForLook = enabled;
                starterInputs.enabled = enabled;
            }
            if (playerInput != null)
                playerInput.enabled = enabled;
            if (flashlight != null)
                flashlight.enabled = enabled;
        }

        public void TeleportTo(Transform destination)
        {
            if (destination == null) return;

            AutoBind();
            Transform body = PlayerBody;
            bool controllerWasEnabled = characterController != null && characterController.enabled;
            if (characterController != null)
                characterController.enabled = false;

            body.SetPositionAndRotation(destination.position, destination.rotation);
            ResetStarterMotion();
            Physics.SyncTransforms();

            if (characterController != null)
                characterController.enabled = controllerWasEnabled;
        }

        private void ResetStarterMotion()
        {
            if (starterInputs != null)
            {
                starterInputs.MoveInput(Vector2.zero);
                starterInputs.LookInput(Vector2.zero);
                starterInputs.JumpInput(false);
                starterInputs.SprintInput(false);
            }

            if (firstPersonController == null) return;

            // Starter Assets keeps these values private. Clearing them prevents old fall
            // velocity from being applied after a story teleport. Missing fields are safe.
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo verticalVelocity = typeof(FirstPersonController).GetField("_verticalVelocity", flags);
            FieldInfo speed = typeof(FirstPersonController).GetField("_speed", flags);
            verticalVelocity?.SetValue(firstPersonController, 0f);
            speed?.SetValue(firstPersonController, 0f);
        }
    }
}
