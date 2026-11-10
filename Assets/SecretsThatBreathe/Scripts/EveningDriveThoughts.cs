using System.Collections;
using TMPro;
using UnityEngine;

namespace SecretsThatBreathe
{
    public sealed class EveningDriveThoughts : MonoBehaviour
    {
        public PorschePassengerRide ride;
        public TMP_Text[] drivingThoughts;
        public TMP_Text arrivalThought;
        public TMP_Text afterExitThought;
        [Min(0f)] public float afterExitThoughtDelay = 2f;
        [Min(0f)] public float initialDelay = 2f;
        [Min(0f)] public float holdSeconds = 3f;
        [Min(0f)] public float gapSeconds = 1f;
        [Min(0f)] public float arrivalDistance = 25f;
        private GameObject visible;
        private Coroutine afterExitRoutine;
        public bool AfterExitThoughtFinished { get; private set; }

        private IEnumerator Start()
        {
            HideAll();
            if (afterExitThought != null) afterExitRoutine = StartCoroutine(ShowAfterExit());
            else AfterExitThoughtFinished = true;
            while (ride != null && (!ride.IsMoving || EveningDriveIntro.IsInputBlocked(gameObject.scene))) yield return null;
            if (ride == null || ride.HasArrived) yield break;
            yield return new WaitForSeconds(initialDelay);
            if (drivingThoughts != null)
                foreach (var line in drivingThoughts)
                {
                    if (ride.HasArrived || ride.RemainingDistance <= arrivalDistance) break;
                    yield return Show(line);
                    yield return new WaitForSeconds(gapSeconds);
                }
            while (ride != null && !ride.HasArrived && ride.RemainingDistance > arrivalDistance) yield return null;
            if (ride != null && !ride.HasArrived) yield return Show(arrivalThought);
        }

        private IEnumerator ShowAfterExit()
        {
            while (ride != null && !ride.HasExited) yield return null;
            if (ride == null) yield break;
            yield return new WaitForSeconds(afterExitThoughtDelay);
            yield return Show(afterExitThought);
            AfterExitThoughtFinished = true;
        }

        internal void SkipAfterExitThoughtForTesting()
        {
            if (afterExitRoutine != null) StopCoroutine(afterExitRoutine);
            AfterExitThoughtFinished = true;
        }

        private IEnumerator Show(TMP_Text line)
        {
            if (line == null) yield break;
            visible = line.transform.parent.gameObject;
            line.maxVisibleCharacters = int.MaxValue;
            visible.SetActive(true);
            yield return new WaitForSeconds(holdSeconds);
            visible.SetActive(false);
            visible = null;
        }

        private void HideAll()
        {
            if (drivingThoughts != null)
                foreach (var line in drivingThoughts)
                    if (line != null && line.transform.parent != null) line.transform.parent.gameObject.SetActive(false);
            if (arrivalThought != null && arrivalThought.transform.parent != null) arrivalThought.transform.parent.gameObject.SetActive(false);
            if (afterExitThought != null && afterExitThought.transform.parent != null) afterExitThought.transform.parent.gameObject.SetActive(false);
        }

        private void OnDisable() { if (visible != null) visible.SetActive(false); visible = null; }
    }
}
