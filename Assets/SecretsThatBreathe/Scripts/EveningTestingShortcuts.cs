using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    public sealed class EveningTestingShortcuts : MonoBehaviour
    {
        public PorschePassengerRide ride;
        [Tooltip("Enable testing shortcuts independently of the TestingShortcutHints object's visibility.")]
        public bool enableTestingShortcuts = true;
        public GameObject testSkipHint;
        public GameObject testSkipIntroHint;

        private const string MidnightScene = "MainScene_2_MidNight";
        private enum Checkpoint { None, Parked, Driving, AfterEvidence }
        private static Checkpoint pendingMidnightCheckpoint;
        private MidnightGhostSequence midnightGhost;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPendingCheckpoint() => pendingMidnightCheckpoint = Checkpoint.None;

        private IEnumerator Start()
        {
            if (gameObject.scene.name != MidnightScene) yield break;
            midnightGhost = FindFirstObjectByType<MidnightGhostSequence>();
            if (pendingMidnightCheckpoint == Checkpoint.None) yield break;

            // Let the new car and intro finish initialization before applying the checkpoint.
            yield return null;
            var checkpoint = pendingMidnightCheckpoint;
            pendingMidnightCheckpoint = Checkpoint.None;
            if (ride == null) yield break;
            switch (checkpoint)
            {
                case Checkpoint.Parked: ride.SkipToExitForTesting(); break;
                case Checkpoint.Driving: ride.SkipToDriveForTesting(); break;
                case Checkpoint.AfterEvidence:
                    GetComponent<EveningDriveThoughts>()?.SkipAfterExitThoughtForTesting();
                    if (midnightGhost != null) midnightGhost.StartPostEvidenceCheckpoint();
                    break;
            }
        }

        private void Update()
        {
            if (GameMenuController.BlocksGameplayInput || !enableTestingShortcuts || ride == null
                || Keyboard.current == null || !Application.isFocused) return;
            // Intro pauses time; allow the shortcut there, but respect a normal game pause.
            if (Time.timeScale <= 0f && !EveningDriveIntro.IsInputBlocked(gameObject.scene)) return;
            if (gameObject.scene.name == MidnightScene)
            {
                if (pendingMidnightCheckpoint != Checkpoint.None) return;
                if (Keyboard.current.f1Key.wasPressedThisFrame) ReloadMidnight(Checkpoint.Parked);
                else if (Keyboard.current.f2Key.wasPressedThisFrame) ReloadMidnight(Checkpoint.Driving);
                else if (Keyboard.current.f3Key.wasPressedThisFrame && midnightGhost != null && midnightGhost.f3SkipAfterEvidencePhotos)
                    ReloadMidnight(Checkpoint.AfterEvidence);
                return;
            }
            if (Keyboard.current.f1Key.wasPressedThisFrame) ride.SkipToExitForTesting();
            else if (Keyboard.current.f2Key.wasPressedThisFrame) ride.SkipToDriveForTesting();
        }

        private void ReloadMidnight(Checkpoint checkpoint)
        {
            var scene = gameObject.scene;
            if (scene.buildIndex < 0 || !Application.CanStreamedLevelBeLoaded(scene.buildIndex))
            {
                Debug.LogWarning("[Midnight] Add the scene to the Build Profile to use testing checkpoints.", this);
                return;
            }
            // Recreate the scene so old photos, rain, camera locks and ending coroutines cannot leak into the checkpoint.
            pendingMidnightCheckpoint = checkpoint;
            BananaEatDriver.ResetForTesting();
            BananaEatInteraction.ResetProgressForTesting();
            SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
        }
    }
}
