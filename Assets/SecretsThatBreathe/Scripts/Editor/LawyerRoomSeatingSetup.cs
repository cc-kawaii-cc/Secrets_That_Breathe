using System;
using System.IO;
using System.Linq;
using StarterAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    public static class LawyerRoomSeatingSetup
    {
        public const string ScenePath = "Assets/Tonpalm/Lawyer Room.unity";

        [MenuItem("Tools/Secrets That Breathe/Setup Lawyer Room Seating")]
        public static void SetupFromMenu()
        {
            try
            {
                SetupCurrentScene();
                EditorUtility.DisplayDialog("Seating setup complete",
                    "Added PlayerSeatController, SeatCameraAnimation, SeatInteractor and NoodleEatingController to PlayerCapsule.\n" +
                    "Added ChairInteractable, SeatPoint, SeatedCameraPoint and InteractionZone to Chair Kam.\n" +
                    "Adjust Chair Kam > InteractionZone with Box Collider > Edit Collider to set the prompt area.\n\n" +
                    "Canvas UI: SeatingCanvas > SitPrompt / EatPrompt > PromptText. Edit Text Input to change each sentence.\n\n" +
                    "PlayerCapsule is selected. Check the Inspector, then save the scene (Ctrl+S).", "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Seating setup failed", exception.Message +
                    "\n\nOpen Window > General > Console for the full error.", "OK");
            }
        }

        public static void SetupCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before setup.");
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open Assets/Tonpalm/Lawyer Room.unity first.");
            var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var chairs = objects.Where(item => item.name == "Chair Kam").ToArray();
            if (chairs.Length != 1) throw new InvalidOperationException("Expected one object named Chair Kam; found " + chairs.Length + ".");
            Transform chairTransform = chairs[0];
            FirstPersonController fps = objects.Select(item => item.GetComponent<FirstPersonController>()).FirstOrDefault(item => item != null);
            if (fps == null) throw new InvalidOperationException("No FirstPersonController found. Check the component on Player > PlayerCapsule, including Missing Script entries.");
            Camera camera = objects.Select(item => item.GetComponent<Camera>()).FirstOrDefault(item => item != null && item.CompareTag("MainCamera"));
            if (camera == null) throw new InvalidOperationException("No Camera with the MainCamera tag found. Check Player > MainCamera.");
            var renderers = chairTransform.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("Chair Kam has no visible mesh.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);

            Transform bowl = objects.FirstOrDefault(item => item.name == "MEMEEE");
            Vector3 deskPosition = bowl != null ? bowl.position : bounds.center + chairTransform.forward;
            if (bowl != null)
            {
                Renderer bowlRenderer = bowl.GetComponentInChildren<Renderer>();
                if (bowlRenderer != null) deskPosition = bowlRenderer.bounds.center;
            }
            Vector3 forward = deskPosition - bounds.center;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Quaternion facing = Quaternion.LookRotation(forward.normalized);
            float floorY = bounds.min.y;
            Physics.SyncTransforms();
            var floors = Physics.RaycastAll(new Vector3(bounds.center.x, bounds.min.y + 0.05f, bounds.center.z),
                Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore).OrderBy(hit => hit.distance);
            foreach (var hit in floors)
            {
                if (hit.transform.IsChildOf(chairTransform) || hit.transform.IsChildOf(fps.transform) || hit.normal.y < 0.7f) continue;
                floorY = hit.point.y;
                break;
            }

            // Replace only the obsolete missing chair component, whose source was already deleted.
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(chairTransform.gameObject);
            var chair = GetOrAdd<ChairInteractable>(chairTransform.gameObject);
            Vector3 seatPosition = new Vector3(bounds.center.x, floorY, bounds.center.z) + forward.normalized * 0.08f;
            chair.seatPoint = Anchor(chairTransform, "SeatPoint", seatPosition, facing);
            chair.seatedCameraPoint = Anchor(chairTransform, "SeatedCameraPoint", seatPosition + Vector3.up * 1.1f, facing);
            if (chairTransform.GetComponentsInChildren<Collider>().Length == 0)
            {
                var collider = Undo.AddComponent<BoxCollider>(chairTransform.gameObject);
                collider.center = chairTransform.InverseTransformPoint(bounds.center);
                collider.size = new Vector3(bounds.size.x / Mathf.Abs(chairTransform.lossyScale.x),
                    bounds.size.y / Mathf.Abs(chairTransform.lossyScale.y), bounds.size.z / Mathf.Abs(chairTransform.lossyScale.z));
            }

            Undo.RecordObject(chair, "Assign seating interaction area");
            bool hadZone = chair.interactionZone != null || chair.transform.Find("InteractionZone") != null;
            chair.CreateDefaultInteractionZone();
            if (!hadZone && chair.interactionZone != null)
                Undo.RegisterCreatedObjectUndo(chair.interactionZone.gameObject, "Create seating interaction area");

            var player = GetOrAdd<PlayerSeatController>(fps.gameObject);
            player.playerBody = fps.transform;
            player.playerCamera = camera;
            player.firstPersonController = fps;
            player.characterController = fps.GetComponent<CharacterController>();
            player.additionalControls = fps.transform.root.GetComponentsInChildren<Behaviour>(true)
                .Where(control => control is PlayerMovement || control is MouseLook || control is PlayerInteractor).ToArray();
            var interactor = GetOrAdd<SeatInteractor>(fps.gameObject);
            Undo.RecordObject(interactor, "Assign seating Canvas");
            if (interactor.promptCanvas == null && interactor.promptUI == null)
            {
                var existingUI = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SeatPromptUI>(true)).FirstOrDefault();
                interactor.promptCanvas = SeatPromptUI.FindOrCreate(scene);
                if (existingUI == null)
                {
                    Undo.RegisterCreatedObjectUndo(interactor.promptCanvas.gameObject, "Create seating Canvas");
                    var thaiFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Nemo_Upgrade/Font/Kanit/Kanit-Medium SDF.asset");
                    if (thaiFont != null) interactor.promptCanvas.promptLabel.font = thaiFont;
                }
            }
            EditorUtility.SetDirty(interactor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(interactor);
            var eating = GetOrAdd<NoodleEatingController>(fps.gameObject);
            Undo.RecordObject(eating, "Configure noodle eating");
            if (eating.noodles == null) eating.noodles = bowl;
            if (eating.promptCanvas == null)
                eating.promptCanvas = interactor.promptCanvas != null ? interactor.promptCanvas : SeatPromptUI.FindOrCreate(scene);
            var eatingUI = eating.promptCanvas;
            Undo.RecordObject(eatingUI, "Configure eating prompt");
            bool hadEatPrompt = eatingUI.eatPanel != null || eatingUI.transform.Find("EatPrompt") != null;
            eatingUI.EnsureEatPrompt();
            if (!hadEatPrompt && eatingUI.eatPanel != null) Undo.RegisterCreatedObjectUndo(eatingUI.eatPanel, "Create eating prompt");
            EditorUtility.SetDirty(eatingUI);
            EditorUtility.SetDirty(eating);
            PrefabUtility.RecordPrefabInstancePropertyModifications(eating);
            EditorUtility.SetDirty(chair);
            EditorUtility.SetDirty(player);
            PrefabUtility.RecordPrefabInstancePropertyModifications(chair);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            var drawerStory = GetOrAdd<DrawerSearchScareSequence>(fps.gameObject);
            drawerStory.ConfigureScene();
            drawerStory.UseThaiFontIfNeeded(AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Nemo_Upgrade/Font/Kanit/Kanit-Medium SDF.asset"));
            EditorUtility.SetDirty(drawerStory);
            PrefabUtility.RecordPrefabInstancePropertyModifications(drawerStory);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = fps.gameObject;
            EditorGUIUtility.PingObject(fps.gameObject);
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
            Debug.Log($"[Seating] Setup complete. Seat={seatPosition}, eye={chair.seatedCameraPoint.position}, facing={facing.eulerAngles}.");
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component == null) component = Undo.AddComponent<T>(target);
            if (component == null) throw new InvalidOperationException("Could not add " + typeof(T).Name + ". Check Console for compilation errors.");
            return component;
        }

        private static Transform Anchor(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing; // Preserve hand-adjusted points on repeated setup.
            var anchor = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(anchor, "Create seating point");
            anchor.transform.SetParent(parent, false);
            anchor.transform.SetPositionAndRotation(position, rotation);
            return anchor.transform;
        }

        // Used for unattended installation; the menu command never opens/saves another scene.
        public static void InstallAndVerify()
        {
            Directory.CreateDirectory("tmp/codex-backups");
            File.Copy(ScenePath, "tmp/codex-backups/Lawyer Room.before-seating." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetupCurrentScene();
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new InvalidOperationException("Scene save failed.");
            SessionState.SetInt("STB.Seating.Smoke", 1);
            EditorApplication.EnterPlaymode();
        }
    }

    [InitializeOnLoad]
    internal static class SeatingSmokeVerification
    {
        private static double deadline;
        private static PlayerSeatController player;
        private static ChairInteractable chair;
        private static Vector3 startPosition;

        static SeatingSmokeVerification() { EditorApplication.update += Tick; }

        private static void Tick()
        {
            int step = SessionState.GetInt("STB.Seating.Smoke", 0);
            if (step == 0 || !EditorApplication.isPlaying) return;
            try
            {
                if (step == 1)
                {
                    player = UnityEngine.Object.FindFirstObjectByType<PlayerSeatController>();
                    chair = UnityEngine.Object.FindFirstObjectByType<ChairInteractable>();
                    Require(player != null && chair != null, "Missing scene components");
                    if (chair.unlockTrigger != null && !chair.unlockTrigger.HasFinishedShowing)
                    {
                        Require(!chair.CanSit, "Chair unlocked before the desk thought trigger");
                        chair.unlockTrigger = null; // Isolate the seating/fade verification from the exploration prerequisite.
                    }
                    Vector3 outside = chair.interactionZone.transform.TransformPoint(chair.interactionZone.center + chair.interactionZone.size * 2f);
                    player.playerBody.position = outside;
                    Require(!player.TrySit(chair), "Sitting allowed outside the interaction zone");
                    player.playerBody.position = chair.seatPoint.position - chair.seatPoint.forward * 0.65f;
                    startPosition = player.playerBody.position;
                    var chairMeshes = chair.GetComponentsInChildren<Renderer>();
                    Bounds chairBounds = chairMeshes[0].bounds;
                    foreach (var mesh in chairMeshes) chairBounds.Encapsulate(mesh.bounds);
                    player.playerCamera.transform.position = startPosition + Vector3.up * 1.7f;
                    player.playerCamera.transform.rotation = Quaternion.LookRotation(player.playerCamera.transform.position - chairBounds.center);
                    Require(!player.TrySit(chair), "Sitting allowed while looking away from the chair");
                    player.playerCamera.transform.LookAt(chairBounds.center);
                    Require(player.TrySit(chair), "First sit rejected");
                    Require(!player.TrySit(chair), "Repeated sit accepted");
                    Require(!player.firstPersonController.enabled && !player.characterController.enabled, "Player controls not suspended");
                    var fadeTiming = player.GetComponent<DrawerSearchScareSequence>();
                    float fadeSeconds = fadeTiming != null ? Mathf.Max(0.05f, fadeTiming.standFadeOutDuration) +
                        Mathf.Max(0f, fadeTiming.standBlackHold) + Mathf.Max(0.05f, fadeTiming.standFadeInDuration) : 1.55f;
                    deadline = EditorApplication.timeSinceStartup + fadeSeconds + 1f;
                    SessionState.SetInt("STB.Seating.Smoke", 2);
                }
                else if (step == 2 && EditorApplication.timeSinceStartup >= deadline)
                {
                    Require(player.State == PlayerSeatController.SeatState.Seated, "Transition did not finish");
                    Require(Vector3.Distance(player.playerCamera.transform.position, chair.seatedCameraPoint.position) < 0.02f, "Camera missed seated point");
                    Require(player.sitFade.fadeGroup.alpha < 0.001f, "Screen remained black after sitting");
                    player.CancelSeating();
                    Require(Vector3.Distance(player.playerBody.position, startPosition) < 0.001f, "Cancel did not restore body");
                    Require(player.firstPersonController.enabled && player.characterController.enabled, "Cancel did not restore controls");
                    Require(player.TrySit(chair), "Cannot sit after cancellation");
                    player.enabled = false;
                    Require(player.State == PlayerSeatController.SeatState.Exploring && player.firstPersonController.enabled,
                        "Disable during transition left player locked");
                    Require(player.sitFade.fadeGroup.alpha < 0.001f, "Cancelled sitting left the screen black");
                    SessionState.SetInt("STB.Seating.Smoke", 0);
                    Debug.Log("[Seating] PASS: transition, repeated input, final camera, cancellation and disable restoration.");
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                SessionState.SetInt("STB.Seating.Smoke", 0);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[Seating] " + message);
        }
    }
}
