using System;
using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsThatBreathe.FinalGameplay
{
    /// <summary>
    /// Installs the final office gameplay from exact hierarchy paths. This keeps the
    /// Lawyer Room playable even before the optional editor setup is saved to disk.
    /// </summary>
    public static class LawyerRoomRuntimeInstaller
    {
        private const string LawyerRoomPath = "Assets/Tonpalm/Lawyer Room.unity";
        private const string ChairPath = "Room/Object Can Interact/Chair Kam";
        private const string DoorPath = "Room/Object Can Interact/Main Door";
        private const string OfficePath = "OfficeGameplay";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Subscribe()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallInitialScene()
        {
            Install(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Install(scene);
        }

        private static void Install(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, LawyerRoomPath, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
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

                EnsureSingleEventSystem(scene);
                GameplayUI ui = CreateOrGetUI(office.transform);

                STBPlayerBridge bridge = GetOrAdd<STBPlayerBridge>(player);
                bridge.Configure(controller, inputs, capsule, playerInput, playerCamera, flashlight);

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
                director.InitializeRuntime();
                ValidateActiveSceneObjects(scene);
                Debug.Log("[FinalGameplay] Lawyer Room gameplay was connected from exact hierarchy paths.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FinalGameplay] Lawyer Room setup stopped: {exception.Message}\n{exception}");
            }
        }

        private static GameplayUI CreateOrGetUI(Transform officeRoot)
        {
            Transform existing = officeRoot.Find("FinalGameplayUI");
            if (existing != null)
            {
                GameplayUI existingUI = existing.GetComponent<GameplayUI>();
                if (existingUI == null)
                    throw new InvalidOperationException("OfficeGameplay/FinalGameplayUI exists but has no GameplayUI component.");
                return existingUI;
            }

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
                canvasObject.transform, "InteractionPrompt", "[E] Interact", 38f,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 120f), new Vector2(900f, 70f));

            TextMeshProUGUI eat = CreateText(
                canvasObject.transform, "EatPrompt", "[E] กินมาม่า", 42f,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 200f), new Vector2(900f, 80f));

            GameObject subtitlePanel = CreatePanel(
                canvasObject.transform, "SubtitlePanel", new Color(0f, 0f, 0f, 0.72f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 90f), new Vector2(1500f, 180f));
            TextMeshProUGUI subtitle = CreateText(
                subtitlePanel.transform, "SubtitleText", string.Empty, 38f,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one,
                Vector2.zero, new Vector2(-60f, -30f));

            GameObject qtePanel = CreatePanel(
                canvasObject.transform, "QTEPanel", new Color(0f, 0f, 0f, 0.82f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 230f));
            CreateText(
                qtePanel.transform, "QTELabel", "กด [E] ค้างเพื่อดันเงินคืน", 38f,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -60f), new Vector2(780f, 70f));

            GameObject barBackground = CreatePanel(
                qtePanel.transform, "ProgressBackground", new Color(0.18f, 0.18f, 0.18f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -45f), new Vector2(720f, 48f));
            GameObject fillObject = CreatePanel(
                barBackground.transform, "ProgressFill", new Color(0.9f, 0.68f, 0.18f, 1f),
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-8f, -8f));
            Image fill = fillObject.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;

            ui.Configure(
                interaction.gameObject, interaction,
                eat.gameObject, eat,
                subtitlePanel, subtitle,
                qtePanel, fill);
            ui.HideAll();
            return ui;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string objectName,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject go = NewUIObject(objectName, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private static GameObject CreatePanel(
            Transform parent,
            string objectName,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject go = NewUIObject(objectName, parent);
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

        private static GameObject NewUIObject(string objectName, Transform parent)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
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
            EventSystem[] allSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            List<EventSystem> sceneSystems = new List<EventSystem>();
            for (int i = 0; i < allSystems.Length; i++)
            {
                if (allSystems[i].gameObject.scene == scene)
                    sceneSystems.Add(allSystems[i]);
            }

            if (sceneSystems.Count > 1)
                throw new InvalidOperationException($"Lawyer Room contains {sceneSystems.Count} EventSystems. Expected exactly one.");
            if (sceneSystems.Count == 1)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule module = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static void ValidateActiveSceneObjects(Scene scene)
        {
            int players = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].activeInHierarchy && roots[i].name == "Player")
                    players++;
            }

            int cameras = CountActiveInScene<Camera>(scene);
            int listeners = CountActiveInScene<AudioListener>(scene);
            int eventSystems = CountActiveInScene<EventSystem>(scene);
            if (players != 1 || cameras != 1 || listeners != 1 || eventSystems != 1)
            {
                Debug.LogWarning(
                    $"[FinalGameplay] Active object check: Player={players}, Camera={cameras}, " +
                    $"AudioListener={listeners}, EventSystem={eventSystems}. Expected one of each.");
            }
        }

        private static int CountActiveInScene<T>(Scene scene) where T : Component
        {
            T[] components = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.scene == scene)
                    count++;
            }
            return count;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static GameObject FindRequired(Scene scene, string hierarchyPath)
        {
            string[] parts = hierarchyPath.Split('/');
            List<GameObject> rootMatches = new List<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == parts[0])
                    rootMatches.Add(roots[i]);
            }

            if (rootMatches.Count != 1)
                throw new InvalidOperationException($"Path '{hierarchyPath}' has {rootMatches.Count} matching root objects.");

            Transform current = rootMatches[0].transform;
            for (int partIndex = 1; partIndex < parts.Length; partIndex++)
            {
                List<Transform> matches = FindDirectChildren(current, parts[partIndex]);
                if (matches.Count != 1)
                {
                    throw new InvalidOperationException(
                        $"Path '{hierarchyPath}' has {matches.Count} matches for '{parts[partIndex]}' under '{GetPath(current)}'.");
                }
                current = matches[0];
            }
            return current.gameObject;
        }

        private static GameObject FindUniqueRoot(Scene scene, string objectName)
        {
            List<GameObject> matches = new List<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    matches.Add(roots[i]);
            }

            if (matches.Count != 1)
                throw new InvalidOperationException($"Expected one root object named '{objectName}', found {matches.Count}.");
            return matches[0];
        }

        private static Transform FindRequiredChild(Transform parent, string childName)
        {
            List<Transform> matches = FindDirectChildren(parent, childName);
            if (matches.Count != 1)
                throw new InvalidOperationException($"Expected one '{childName}' under '{GetPath(parent)}', found {matches.Count}.");
            return matches[0];
        }

        private static List<Transform> FindDirectChildren(Transform parent, string childName)
        {
            List<Transform> matches = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                    matches.Add(child);
            }
            return matches;
        }

        private static string GetPath(Transform transform)
        {
            string value = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                value = transform.name + "/" + value;
            }
            return value;
        }

        private static void Require(UnityEngine.Object value, string message)
        {
            if (value == null)
                throw new InvalidOperationException(message);
        }
    }
}
