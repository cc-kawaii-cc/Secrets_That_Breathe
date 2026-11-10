using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class GameMenuSetup
    {
        private const string Folder = "Assets/MainScenes/From_PM/";
        private const string MenuPath = Folder + "MainMenu.unity";
        private const string FontPath = "Assets/Nemo_Upgrade/Font/Press_Start_2P/PressStart2P-Regular.ttf";
        private const string FontAssetPath = "Assets/Nemo_Upgrade/Font/Press_Start_2P/PressStart2P-Regular SDF.asset";
        private const string Key = "Secrets.Menu.Install.v1";
        static GameMenuSetup() { EditorApplication.delayCall += InstallOnce; }
        private static void InstallOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Key, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += InstallOnce; return; }
            SessionState.SetBool(Key, true);
            try { Install(); } catch (Exception e) { Debug.LogException(e); }
        }
        [MenuItem("Tools/Secrets That Breathe/Install Menu and Pause")]
        public static void Install()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                if (source == null) throw new Exception("Press Start 2P font not found.");
                font = TMP_FontAsset.CreateFontAsset(source);
                AssetDatabase.CreateAsset(font, FontAssetPath);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var atlas in font.atlasTextures) if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, font);
                EditorUtility.SetDirty(font); AssetDatabase.SaveAssets();
            }
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath))
            {
                if (!AssetDatabase.CopyAsset(Folder + "Lawyer Room.unity", MenuPath)) throw new Exception("Could not copy Lawyer Room for menu.");
            }
            var paths = EditorBuildSettings.scenes.Where(x => x.enabled && x.path != MenuPath).Select(x => x.path).ToArray();
            foreach (var path in paths) SetupScene(path, font, false);
            SetupScene(MenuPath, font, true);
            var list = EditorBuildSettings.scenes.Where(x => x.path != MenuPath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(MenuPath, true)); EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Secrets That Breathe/Align Menu Camera to Scene View")]
        private static void AlignMenuCamera()
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null || view.orthographic) return;
            var source = view.camera;
            Vector3 position = source.transform.position;
            Quaternion rotation = source.transform.rotation;
            float fieldOfView = source.fieldOfView;
            var scene = SceneManager.GetSceneByPath(MenuPath);
            bool loaded = scene.IsValid() && scene.isLoaded;
            if (!loaded) scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Additive);
            try
            {
                var camera = scene.GetRootGameObjects().First(x => x.name == "MenuCamera").GetComponent<Camera>();
                Undo.RecordObjects(new UnityEngine.Object[] { camera, camera.transform }, "Align Menu Camera");
                camera.transform.SetPositionAndRotation(position, rotation);
                camera.fieldOfView = fieldOfView;
                EditorSceneManager.MarkSceneDirty(scene);
                if (!loaded) EditorSceneManager.SaveScene(scene);
            }
            finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
        }

        private static void SetupScene(string path, TMP_FontAsset font, bool menu)
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool loaded = scene.IsValid() && scene.isLoaded;
            if (loaded && scene.isDirty) { Debug.LogWarning("Save scene first, then run Tools > Secrets That Breathe > Install Menu and Pause: " + path); return; }
            if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                if (roots.SelectMany(r => r.GetComponentsInChildren<GameMenuController>(true)).Any()) return;
                var seating = roots.FirstOrDefault(x => x.name == "SeatingCanvas");
                if (menu)
                {
                    foreach (var root in roots)
                        if (root.name == "Player" || root.name == "SeatingCanvas" || root.name == "OfficeIntroTrigger" || root.name == "DrawerSearchAudio") root.SetActive(false);
                    var cameraObject = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
                    SceneManager.MoveGameObjectToScene(cameraObject, scene);
                    cameraObject.tag = "MainCamera";
                    cameraObject.transform.position = new Vector3(4.6f, 2.22f, 5.95f);
                    cameraObject.transform.rotation = Quaternion.Euler(8.5f, -163f, 0f);
                    var camera = cameraObject.GetComponent<Camera>();
                    camera.fieldOfView = 55f;
                    camera.nearClipPlane = .05f;
                    camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                    var ambience = roots.FirstOrDefault(x => x.name == "RoomAmbience");
                    if (ambience != null && ambience.TryGetComponent(out AudioSource audio)) audio.playOnAwake = true;
                    seating = null;
                }
                var holder = new GameObject(menu ? "MainMenuUI" : "PauseMenuUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameMenuController));
                SceneManager.MoveGameObjectToScene(holder, scene);
                if (seating != null)
                {
                    holder.transform.SetParent(seating.transform, false);
                    Stretch((RectTransform)holder.transform);
                }
                var canvas = holder.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.overrideSorting = true; canvas.sortingOrder = 32000;
                var scaler = holder.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
                var controller = holder.GetComponent<GameMenuController>(); controller.mainMenu = menu;
                controller.background = Panel(holder.transform, "DimBackground", new Color(0, 0, 0, menu ? 0 : .65f));
                controller.menuPanel = Panel(holder.transform, "Menu", Color.clear);
                controller.settingsPanel = Panel(holder.transform, "Settings", Color.clear);
                float x = menu ? .5f : .137f;
                bool left = !menu;
                Button(controller.menuPanel.transform, font, menu ? "Start" : "Resume", new Vector2(x, menu ? .385f : .59f), menu ? controller.StartGame : controller.Resume, left);
                Button(controller.menuPanel.transform, font, "Setting", new Vector2(x, menu ? .265f : .50f), controller.ShowSettings, left);
                Button(controller.menuPanel.transform, font, "Exit", new Vector2(x, menu ? .13f : .41f), controller.ExitGame, left);
                var heading = Label(controller.settingsPanel.transform, font, "Setting", new Vector2(.085f, .87f), 45);
                heading.rectTransform.pivot = new Vector2(0, .5f);
                heading.alignment = TextAlignmentOptions.MidlineLeft;
                Label(controller.settingsPanel.transform, font, "Frame limiter", new Vector2(.5f, .67f), 45);
                controller.fpsValue = Label(controller.settingsPanel.transform, font, "60", new Vector2(.5f, .56f), 45);
                Button(controller.settingsPanel.transform, font, "<", new Vector2(.5f, .56f), controller.StepLeft, offsetX: -210);
                Button(controller.settingsPanel.transform, font, ">", new Vector2(.5f, .56f), controller.StepRight, offsetX: 210);
                Button(controller.settingsPanel.transform, font, "Back", new Vector2(.085f, .135f), controller.ShowMenu, true);
                Button(controller.settingsPanel.transform, font, "Apply", new Vector2(.91f, .135f), controller.Apply);
                controller.background.SetActive(false);
                controller.menuPanel.SetActive(menu);
                controller.settingsPanel.SetActive(false);
                if (!roots.SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).Any())
                {
                    var ev = new GameObject("MenuEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                    SceneManager.MoveGameObjectToScene(ev, scene);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static GameObject Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = color.a > 0;
            return go;
        }
        private static TMP_Text Label(Transform parent, TMP_FontAsset font, string text, Vector2 pos, float size)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = pos; rt.sizeDelta = new Vector2(600, 90);
            var label = go.GetComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center; label.color = Color.white; label.raycastTarget = false;
            return label;
        }
        private static void Button(Transform parent, TMP_FontAsset font, string text, Vector2 pos, UnityEngine.Events.UnityAction action, bool left = false, float offsetX = 0f)
        {
            var go = new GameObject(text + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = pos;
            rect.pivot = new Vector2(left ? 0 : .5f, .5f);
            float fontSize = text == "<" || text == ">" ? 55f : 45f;
            rect.sizeDelta = new Vector2(fontSize * text.Length + 24f, fontSize + 19f);
            rect.anchoredPosition = new Vector2(offsetX, 0);
            var image = go.GetComponent<Image>(); image.color = Color.clear; image.raycastTarget = true;
            var label = Label(go.transform, font, text, new Vector2(.5f, .5f), fontSize);
            label.rectTransform.sizeDelta = rect.sizeDelta;
            if (left)
            {
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0, .5f);
                label.rectTransform.pivot = new Vector2(0, .5f);
                label.alignment = TextAlignmentOptions.MidlineLeft;
            }
            var button = go.GetComponent<Button>(); button.targetGraphic = label; UnityEventTools.AddPersistentListener(button.onClick, action);
        }
    }
}
