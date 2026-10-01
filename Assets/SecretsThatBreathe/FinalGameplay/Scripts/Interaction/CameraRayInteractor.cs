using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe.FinalGameplay
{
    public sealed class CameraRayInteractor : MonoBehaviour
    {
        [SerializeField] private STBPlayerBridge playerBridge;
        [SerializeField] private Camera interactionCamera;
        [SerializeField, Min(0.5f)] private float interactionRange = 4f;
        [SerializeField] private GameplayUI gameplayUI;

        private bool interactionEnabled = true;

        public void Configure(
            STBPlayerBridge bridge,
            Camera camera,
            GameplayUI ui,
            float range = 4f)
        {
            playerBridge = bridge;
            interactionCamera = camera;
            gameplayUI = ui;
            interactionRange = range;
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;
            if (!enabled) gameplayUI?.HideInteractionPrompt();
        }

        private void Update()
        {
            if (!interactionEnabled || (playerBridge != null && !playerBridge.ControlsEnabled))
            {
                gameplayUI?.HideInteractionPrompt();
                return;
            }

            if (interactionCamera == null)
                interactionCamera = playerBridge != null ? playerBridge.PlayerCamera : Camera.main;
            if (interactionCamera == null)
            {
                gameplayUI?.HideInteractionPrompt();
                return;
            }

            InteractionTarget target = FindTarget();
            if (target == null || !target.CanInteract)
            {
                gameplayUI?.HideInteractionPrompt();
                return;
            }

            gameplayUI?.ShowInteractionPrompt(target.PromptText);
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                target.Interact(this);
        }

        private InteractionTarget FindTarget()
        {
            Ray ray = new Ray(interactionCamera.transform.position, interactionCamera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                interactionRange,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                    continue;

                InteractionTarget candidate = hitCollider.GetComponentInParent<InteractionTarget>();
                if (candidate != null)
                    return candidate.CanInteract ? candidate : null;

                if (!hitCollider.isTrigger)
                    return null;
            }

            return null;
        }
    }
}
