using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    // Po scene, the end: E at the exit doors (PoliceStationNoEntryTv) fades the screen to black. This starts loading
    // MainScene_2_MidNight the moment E is pressed (while the screen is going black), and switches to it once the screen is
    // black and the scene is ready - so the wait in the dark is as short as possible.
    [DisallowMultipleComponent]
    public sealed class PoGoBackToMidnight : MonoBehaviour
    {
        [Tooltip("The station moment whose 'go back' (E at the exit doors) leads to the next scene.")]
        public PoliceStationNoEntryTv station;
        [Tooltip("Scene loaded next (must be in File > Build Profiles / Build Settings).")]
        public string destinationScene = "MainScene_2_MidNight";
        [Tooltip("Seconds the screen stays fully black before the next scene shows.")]
        [Min(0f)] public float blackHoldSeconds = .5f;

        private AsyncOperation loading;
        private bool black;

        private void OnEnable()
        {
            if (station != null) station.onGoBack.AddListener(OnBlack);
        }

        private void OnDisable()
        {
            if (station != null) station.onGoBack.RemoveListener(OnBlack);
        }

        private void Update()
        {
            // E was pressed at the doors: start loading while the screen fades to black
            if (loading == null && station != null && station.WentBack) StartLoading();
        }

        private void StartLoading()
        {
            if (!Application.CanStreamedLevelBeLoaded(destinationScene))
            {
                Debug.LogError("[Po] " + destinationScene + " is not in the Build Settings, so going back cannot load it.", this);
                enabled = false;
                return;
            }
            loading = SceneManager.LoadSceneAsync(destinationScene, LoadSceneMode.Single);
            if (loading != null) loading.allowSceneActivation = false;
        }

        private void OnBlack()
        {
            if (black) return;
            black = true;
            if (loading == null) StartLoading();
            if (loading != null) StartCoroutine(SwitchWhenReady());
        }

        private IEnumerator SwitchWhenReady()
        {
            float blackSince = Time.unscaledTime;
            while (loading.progress < .9f || Time.unscaledTime - blackSince < blackHoldSeconds) yield return null;
            loading.allowSceneActivation = true;
        }
    }
}
