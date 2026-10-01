#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using StarterAssets;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsThatBreathe.FinalGameplay.Editor
{
    public static class LawyerRoomGameplaySetup
    {
        private const string ScenePath = "Assets/Tonpalm/Lawyer Room.unity";
        private const string ChairPath = "Room/Object Can Interact/Chair Kam";
        private const string DoorPath = "Room/Object Can Interact/Main Door";
        private const string OfficePath = "OfficeGameplay";

        [MenuItem("Tools/Secrets That Breathe/Setup Lawyer Room Gameplay")]
        public static void SetupFromMenu()
        {
            SetupLawyerRoomGameplay();
        }

        public static void SetupLawyerRoomGameplay()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
                throw new InvalidOperationException($"Could not open scene: {ScenePath}");

            GameObject office = FindRequired(scene, OfficePath);
            GameObject chair = FindRequired(scene, ChairPath);
            GameObject door = FindRequired(scene, DoorPath);
            GameObject player = FindUniqueRoot(scene, "Player");

            Transform sit = FindRequiredChild(office.transform, "MARK_PlayerSit");
            Transform stand = FindRequiredChild(office.transform, "MARK_PlayerStand");
            Transform auntSpawn = FindRequiredChild(office.transform, "MARK_AuntSpawn");
            Transform auntStop = FindRequiredChild(office.transform, "MARK_AuntStop");
            GameObject aunt = FindRequiredChild(office.transform, "NPC_Aunt").gameObject;
            GameObject money = FindRequiredChild(office.transform, "Money").gameObject;

            FirstPersonController controller = player.GetComponentInChildren<FirstPersonController>(true);
            StarterAssetsInputs inputs = player.GetComponentInChildren<StarterAssetsInputs>(true);
            CharacterController capsule = player.GetComponentInChildren<CharacterController>(true);
            PlayerInput playerInput = player.GetComponentInChildren<PlayerInput>(true);
            Camera playerCamera = player.GetComponentInChildren<Camera>(true);
            FirstPersonFlashlight flashlight = player.GetComponentInChildren<FirstPersonFlashlight>(true);
            Require(controller, "Nemo Player is missing FirstPersonController.");
            Require(inputs, "Nemo Player is missing StarterAssetsInputs.");
            Require(capsule, "Nemo Player is missing CharacterController.");
            Require(playerInput, "Nemo Player is missing PlayerInput.");
            Require(playerCamera, "Nemo Player is missing its Camera.");

            STBPlayerBridge bridge = GetOrAdd<STBPlayerBridge>(player);
            bridge.Configure(controller, inputs, capsule, playerInput, playerCamera, flashlight);

            GameplayUI ui = CreateOrUpdateUI(office.transform);
            CameraRayInteractor interactor = GetOrAdd<CameraRayInteractor>(player);
            interactor.Configure(bridge, playerCamera, ui, 4f);

            OfficeExitDoor exitDoor = GetOrAdd<OfficeExitDoor>(door);
            exitDoor.Configure("Main2_Day");
            exitDoor.SetUnlocked(false);

            HoldQTEController qte = GetOrAdd<HoldQTEController>(office);
            qte.Configure(ui, 2.5f);

            OfficeSequenceDirector director = GetOrAdd<OfficeSequenceDirector>(office);
            director.Configure(
                bridge,
                interactor,
                sit,
                stand,
                aunt,
                auntSpawn,
                auntStop,
                money,
                ui,
                qte,
                exitDoor);

            OfficeChairInteractable chairTarget = GetOrAdd<OfficeChairInteractable>(chair);
            chairTarget.Configure(director);

            EnsureCollider(chair);
            EnsureCollider(door);
            aunt.SetActive(false);
            money.SetActive(false);
            EnsureSingleEventSystem(scene);

            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(office);
            EditorUtility.SetDirty(chair);
            EditorUtility.SetDirty(door);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            ValidateScene(scene);
            Debug.Log("[FinalGameplay] Lawyer Room gameplay setup completed successfully.");
        }

        private static GameplayUI CreateOrUpdateUI(Transform officeRoot)
        {
            Transform old = officeRoot.Find("FinalGameplayUI");
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);

            GameObject canvasObject = NewUIObject("FinalGameplayUI", officeRoot);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            GameplayUI ui = canvasObject.AddComponent<GameplayUI>();

            TextMeshProUGUI interaction = CreateText(
                canvasObject.transform,
                "InteractionPrompt",
                "[E] Interact",
                38f,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 120f),
                new Vector2(900f, 70f));

            TextMeshProUGUI eat = CreateText(
                canvasObject.transform,
                "EatPrompt",
                "[E] กินมาม่า",
                42f,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 200f),
                new Vector2(900f, 80f));

            GameObject subtitlePanel = CreatePanel(
                canvasObject.transform,
                "SubtitlePanel",
                new Color(0f, 0f, 0f, 0.72f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 90f),
                new Vector2(1500f, 180f));
            TextMeshProUGUI subtitle = CreateText(
                subtitlePanel.transform,
                "SubtitleText",
                string.Empty,
                38f,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(-60f, -30f));

            GameObject qtePanel = CreatePanel(
                canvasObject.transform,
                "QTEPanel",
                new Color(0f, 0f, 0f, 0.82f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(900f, 230f));
            CreateText(
                qtePanel.transform,
                "QTELabel",
                "กด [E] ค้างเพื่อดันเงินคืน",
                38f,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -60f),
                new Vector2(780f, 70f));

            GameObject barBackground = CreatePanel(
                qtePanel.transform,
                "ProgressBackground",
                new Color(0.18f, 0.18f, 0.18f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -45f),
                new Vector2(720f, 48f));
            GameObject fillObject = CreatePanel(
                barBackground.transform,
                "ProgressFill",
                new Color(0.9f, 0.68f, 0.18f, 1f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(-8f, -8f));
            Image fill = fillObject.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;

            ui.Configure(
                interaction.gameObject,
                interaction,
                eat.gameObject,
                eat,
                subtitlePanel,
                subtitle,
                qtePanel,
                fill);

            interaction.gameObject.SetActive(false);
            eat.gameObject.SetActive(false);
            subtitlePanel.SetActive(false);
            qtePanel.SetActive(false);
            return ui;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject go = NewUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject go = NewUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private static GameObject NewUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void EnsureCollider(GameObject target)
        {
            if (target.GetComponentInChildren<Collider>(true) != null) return;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            BoxCollider collider = target.AddComponent<BoxCollider>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            collider.center = target.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = target.transform.InverseTransformVector(bounds.size);
            collider.size = new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z));
        }

        private static void EnsureSingleEventSystem(Scene scene)
        {
            EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            List<EventSystem> sceneSystems = systems.Where(item => item.gameObject.scene == scene).ToList();

            if (sceneSystems.Count == 0)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
                eventSystemObject.AddComponent<EventSystem>();
                InputSystemUIInputModule module = eventSystemObject.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
                return;
            }

            if (sceneSystems.Count > 1)
                throw new InvalidOperationException("Lawyer Room contains more than one EventSystem.");
        }

        private static void ValidateScene(Scene scene)
        {
            int activePlayers = scene.GetRootGameObjects().Count(go => go.activeInHierarchy && go.name == "Player");
            int activeCameras = UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Count(item => item.gameObject.scene == scene);
            int activeListeners = UnityEngine.Object.FindObjectsByType<AudioListener>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Count(item => item.gameObject.scene == scene);
            int eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Count(item => item.gameObject.scene == scene);

            if (activePlayers != 1)
                Debug.LogWarning($"[FinalGameplay] Expected one active Player root, found {activePlayers}.");
            if (activeCameras != 1)
                Debug.LogWarning($"[FinalGameplay] Expected one active Camera, found {activeCameras}.");
            if (activeListeners != 1)
                Debug.LogWarning($"[FinalGameplay] Expected one active AudioListener, found {activeListeners}.");
            if (eventSystems != 1)
                Debug.LogWarning($"[FinalGameplay] Expected one active EventSystem, found {eventSystems}.");

            if (!EditorBuildSettings.scenes.Any(item => item.enabled &&
                item.path.EndsWith("/Main2_Day.unity", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Main2_Day is not enabled in Build Settings.");
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static GameObject FindRequired(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            GameObject root = FindUniqueRoot(scene, parts[0]);
            Transform current = root.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                List<Transform> matches = new List<Transform>();
                for (int childIndex = 0; childIndex < current.childCount; childIndex++)
                {
                    Transform child = current.GetChild(childIndex);
                    if (child.name == parts[i]) matches.Add(child);
                }

                if (matches.Count != 1)
                    throw new InvalidOperationException(
                        $"Expected exactly one object at '{string.Join("/", parts.Take(i + 1))}', found {matches.Count}.");
                current = matches[0];
            }

            return current.gameObject;
        }

        private static GameObject FindUniqueRoot(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects().Where(go => go.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"Expected exactly one root object named '{name}', found {matches.Length}.");
            return matches[0];
        }

        private static Transform FindRequiredChild(Transform parent, string name)
        {
            List<Transform> matches = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name) matches.Add(child);
            }

            if (matches.Count != 1)
                throw new InvalidOperationException(
                    $"Expected exactly one direct child named '{name}' under '{parent.name}', found {matches.Count}.");
            return matches[0];
        }

        private static void Require(UnityEngine.Object value, string message)
        {
            if (value == null) throw new InvalidOperationException(message);
        }
    }
}
#endif
