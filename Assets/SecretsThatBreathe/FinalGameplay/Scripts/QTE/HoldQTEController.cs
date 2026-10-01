using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe.FinalGameplay
{
    public sealed class HoldQTEController : MonoBehaviour
    {
        [SerializeField] private GameplayUI gameplayUI;
        [SerializeField, Min(0.1f)] private float holdDuration = 2.5f;
        [SerializeField] private bool resetOnRelease = true;

        private bool active;
        private bool awaitingRelease;
        private float timer;
        private Action onComplete;

        public void Configure(GameplayUI ui, float duration = 2.5f)
        {
            gameplayUI = ui;
            holdDuration = duration;
        }

        public void Begin(Action completed)
        {
            timer = 0f;
            onComplete = completed;
            active = true;
            awaitingRelease = true;
            gameplayUI?.ShowQTE(true);
        }

        private void Update()
        {
            if (!active) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (awaitingRelease)
            {
                if (!keyboard.eKey.isPressed)
                    awaitingRelease = false;
                return;
            }

            if (keyboard.eKey.isPressed)
                timer += Time.deltaTime;
            else if (resetOnRelease)
                timer = 0f;

            gameplayUI?.SetQTEProgress(timer / holdDuration);
            if (timer >= holdDuration)
                Complete();
        }

        private void Complete()
        {
            active = false;
            gameplayUI?.ShowQTE(false);
            Action callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }
    }
}
