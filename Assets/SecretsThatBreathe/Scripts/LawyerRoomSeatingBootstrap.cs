using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe
{
    // Only installs the first seating step in this exact scene. Serialized setup takes priority.
    public static class LawyerRoomSeatingBootstrap
    {
        private const string ScenePath = "Assets/Tonpalm/Lawyer Room.unity";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= Install;
            SceneManager.sceneLoaded += Install;
        }

        private static void Install(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != ScenePath) return;
            Transform chairTransform = null, bowl = null;
            FirstPersonController fps = null;
            Camera camera = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                {
                    if (item.name == "Chair Kam") chairTransform = item;
                    if (item.name == "MEMEEE") bowl = item;
                    if (fps == null) fps = item.GetComponent<FirstPersonController>();
                    Camera candidate = item.GetComponent<Camera>();
                    if (candidate != null && candidate.CompareTag("MainCamera")) camera = candidate;
                }
            }
            if (fps == null || camera == null || chairTransform == null)
            {
                Debug.LogWarning("[Seating] Lawyer Room requires Chair Kam, a Starter Assets FPS player, and MainCamera.");
                return;
            }
            var chair = chairTransform.GetComponent<ChairInteractable>();
            if (chair == null) chair = chairTransform.gameObject.AddComponent<ChairInteractable>();
            chair.CreateDefaultPoints(bowl);
            chair.CreateDefaultInteractionZone();
            var player = fps.GetComponent<PlayerSeatController>();
            if (player == null) player = fps.gameObject.AddComponent<PlayerSeatController>();
            player.playerBody = fps.transform;
            player.firstPersonController = fps;
            player.characterController = fps.GetComponent<CharacterController>();
            player.playerCamera = camera;
            if (fps.GetComponent<SeatInteractor>() == null) fps.gameObject.AddComponent<SeatInteractor>();
            var eating = fps.GetComponent<NoodleEatingController>();
            if (eating == null) eating = fps.gameObject.AddComponent<NoodleEatingController>();
            if (eating.noodles == null) eating.noodles = bowl;
            if (fps.GetComponent<DrawerSearchScareSequence>() == null) fps.gameObject.AddComponent<DrawerSearchScareSequence>();
            OfficeIntroDialogue.Install(SeatPromptUI.FindOrCreate(scene));
            ChairExplorationInstaller.Install(scene, player, SeatPromptUI.FindOrCreate(scene));
        }
    }
}
