using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.FinalGameplay
{
    public sealed class OfficeExitDoor : InteractionTarget
    {
        [SerializeField] private string nextSceneName = "Main2_Day";
        [SerializeField] private bool unlocked;

        private bool loading;

        public override bool CanInteract => unlocked && !loading;

        public void Configure(string sceneName)
        {
            nextSceneName = sceneName;
            SetPromptText("[E] ออกจากสำนักงาน");
        }

        public void SetUnlocked(bool value)
        {
            unlocked = value;
        }

        public override void Interact(CameraRayInteractor interactor)
        {
            if (!CanInteract) return;
            StartCoroutine(LoadNextScene());
        }

        private IEnumerator LoadNextScene()
        {
            if (string.IsNullOrWhiteSpace(nextSceneName) ||
                !Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"[OfficeExitDoor] Scene '{nextSceneName}' is not available in Build Settings.", this);
                yield break;
            }

            loading = true;
            AsyncOperation operation = SceneManager.LoadSceneAsync(nextSceneName);
            if (operation == null)
            {
                loading = false;
                yield break;
            }

            while (!operation.isDone)
                yield return null;
        }
    }
}
