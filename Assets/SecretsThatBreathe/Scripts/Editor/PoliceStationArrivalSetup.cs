#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using StarterAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class PoliceStationArrivalSetup
    {
        const string Folder = "Tools/PoliceStationArrival";
        const string Request = Folder + "/install.request";
        const string StationPath = "Assets/MainScenes/From_PM/Inside_Police_Station.unity";
        static PoliceStationArrivalSetup() => EditorApplication.update += Install;

        public static void Install()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            Scene scene = SceneManager.GetSceneByPath(StationPath);
            bool loaded = scene.IsValid() && scene.isLoaded;
            Scene original = SceneManager.GetActiveScene();
            try
            {
                if (!loaded) scene = EditorSceneManager.OpenScene(StationPath, OpenSceneMode.Additive);
                if (!File.Exists(Folder + "/before-arrival.unity"))
                    if (!EditorSceneManager.SaveScene(scene, Folder + "/before-arrival.unity", true)) throw new Exception("Backup failed.");
                var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                var door = transforms.Single(t => t.name == "Door (4)");
                var player = transforms.Select(t => t.GetComponent<FirstPersonController>()).First(p => p != null);
                Transform point = transforms.FirstOrDefault(t => t.name == "PoliceStationArrivalPoint");
                Bounds bounds = new Bounds(door.position, Vector3.zero);
                var renderers = door.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) throw new Exception("Door has no renderers.");
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                if (point == null || File.Exists(Folder + "/placement.request"))
                {
                    if (point == null)
                    { point = new GameObject("PoliceStationArrivalPoint").transform; SceneManager.MoveGameObjectToScene(point.gameObject, scene); }
                    Vector3 inward = Vector3.ProjectOnPlane(door.forward, Vector3.up).normalized;
                    if (Vector3.Dot(inward, player.transform.position - bounds.center) < 0f) inward = -inward;
                    Vector3 position = bounds.center + inward * 1.4f;
                    position.y = bounds.min.y;
                    Physics.SyncTransforms();
                    var hits = Physics.RaycastAll(position + Vector3.up * 1.5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore)
                        .Where(h => h.collider.gameObject.scene == scene && !h.transform.IsChildOf(player.transform.root) && h.normal.y > .5f)
                        .OrderBy(h => h.distance).ToArray();
                    if (hits.Length == 0) throw new Exception("No supporting floor found in front of Door (4).");
                    position.y = hits[0].point.y;
                    point.SetPositionAndRotation(position, Quaternion.LookRotation(inward));
                }
                var canvasRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "SeatingCanvas");
                if (canvasRoot == null)
                {
                    canvasRoot = new GameObject("SeatingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    SceneManager.MoveGameObjectToScene(canvasRoot, scene);
                    canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    var scaler = canvasRoot.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
                }
                var existing = canvasRoot.transform.Find("PoliceStationIntroUI");
                var panel = existing != null ? existing.gameObject : new GameObject("PoliceStationIntroUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(Image), typeof(PoliceStationArrival));
                panel.transform.SetParent(canvasRoot.transform, false);
                var rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var overlay = panel.GetComponent<Canvas>(); overlay.overrideSorting = true; overlay.sortingOrder = 32700;
                var image = panel.GetComponent<Image>(); image.color = Color.black; image.raycastTarget = true;
                var group = panel.GetComponent<CanvasGroup>(); group.alpha = 1f; group.blocksRaycasts = true;
                var arrival = panel.GetComponent<PoliceStationArrival>();
                arrival.blackScreen = group; arrival.arrivalPoint = point; arrival.player = player;
                arrival.initializeArrivalFromDoor = false;
                // Place the authored player too, so the starting location is visible outside Play Mode.
                var root = player.transform.root;
                root.rotation = Quaternion.Euler(0f, point.eulerAngles.y, 0f);
                player.transform.rotation = root.rotation;
                var cc = player.GetComponent<CharacterController>();
                float bottom = (cc.center.y - cc.height * .5f) * player.transform.lossyScale.y;
                root.position += point.position + Vector3.up * (.015f - bottom) - player.transform.position;
                var build = EditorBuildSettings.scenes.ToList();
                int index = build.FindIndex(s => s.path == StationPath);
                if (index < 0) build.Add(new EditorBuildSettingsScene(StationPath, true));
                else build[index] = new EditorBuildSettingsScene(StationPath, true);
                EditorBuildSettings.scenes = build.ToArray();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Station save failed.");
                if (File.Exists(Folder + "/placement.request")) File.Delete(Folder + "/placement.request");
                File.WriteAllText(Folder + "/install.result.txt", "PASS\nDoor centre: " + bounds.center.ToString("F3") +
                    "\nArrival feet: " + point.position.ToString("F3") + "\nArrival yaw: " + point.eulerAngles.y +
                    "\nPlayer capsule: " + player.transform.position.ToString("F3") +
                    "\nSaved SeatingCanvas/PoliceStationIntroUI; black hold 2s, reveal 1.5s; no Intro text.\nBuild scene enabled.");
            }
            catch (Exception error) { File.WriteAllText(Folder + "/install.result.txt", "FAIL\n" + error); Debug.LogException(error); }
            finally
            {
                if (!loaded && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }
    }
}
#endif
