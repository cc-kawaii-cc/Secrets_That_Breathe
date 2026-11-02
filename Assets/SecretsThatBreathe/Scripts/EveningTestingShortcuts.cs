using UnityEngine;
using UnityEngine.InputSystem;

namespace SecretsThatBreathe
{
    public sealed class EveningTestingShortcuts : MonoBehaviour
    {
        public PorschePassengerRide ride;
        [Tooltip("Enable F1/F2 independently of the TestingShortcutHints object's visibility.")]
        public bool enableTestingShortcuts = true;
        public GameObject testSkipHint;
        public GameObject testSkipIntroHint;

        private void Update()
        {
            if (testSkipHint != null && testSkipHint.activeSelf != enableTestingShortcuts)
                testSkipHint.SetActive(enableTestingShortcuts);
            if (testSkipIntroHint != null && testSkipIntroHint.activeSelf != enableTestingShortcuts)
                testSkipIntroHint.SetActive(enableTestingShortcuts);
            if (!enableTestingShortcuts || ride == null || Keyboard.current == null || !Application.isFocused) return;
            // Intro pauses time; allow the shortcut there, but respect a normal game pause.
            if (Time.timeScale <= 0f && !EveningDriveIntro.IsInputBlocked(gameObject.scene)) return;
            if (Keyboard.current.f1Key.wasPressedThisFrame) ride.SkipToExitForTesting();
            else if (Keyboard.current.f2Key.wasPressedThisFrame) ride.SkipToDriveForTesting();
        }
    }
}
