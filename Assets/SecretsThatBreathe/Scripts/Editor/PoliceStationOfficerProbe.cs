#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using StarterAssets;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // Read-only look at the station scene (nothing is changed or saved). Drop Tools/PoliceStationOfficer/probe.request to run.
    [InitializeOnLoad]
    public static class PoliceStationOfficerProbe
    {
        const string Folder = "Tools/PoliceStationOfficer";
        const string Request = Folder + "/probe.request";
        const string Result = Folder + "/probe.result.txt";
        const string StationPath = "Assets/MainScenes/From_PM/Inside_Police_Station.unity";
        static PoliceStationOfficerProbe() => EditorApplication.update += Run;

        static string Path(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();
            for (; t != null; t = t.parent) parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        const string Look = Folder + "/look.request";
        // look.request holds: x y z yaw pitch size. It only moves the Scene view camera (nothing is saved).
        static void LookRequest()
        {
            string text = File.ReadAllText(Look);
            File.Delete(Look);
            var parts = text.Split(new[] { ' ', '\r', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            var view = SceneView.lastActiveSceneView;
            if (view == null || parts.Length < 6) return;
            float[] v = parts.Take(6).Select(p => float.Parse(p, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            view.in2DMode = false;
            view.LookAt(new Vector3(v[0], v[1], v[2]), Quaternion.Euler(v[4], v[3], 0f), v[5], false, true);
            view.Repaint();
        }

        const string Teleport = Folder + "/teleport.request";
        // teleport.request holds: x y z yaw [pitch]. Play Mode only: puts the player there (a testing aid, nothing is saved).
        static void TeleportRequest()
        {
            string text = File.ReadAllText(Teleport);
            File.Delete(Teleport);
            var parts = text.Split(new[] { ' ', '\r', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) return;
            float[] v = parts.Select(p => float.Parse(p, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            var player = Object.FindFirstObjectByType<FirstPersonController>();
            if (player == null) return;
            var controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(v[0], v[1], v[2]), Quaternion.Euler(0f, v[3], 0f));
            if (controller != null) controller.enabled = wasEnabled;
            Physics.SyncTransforms();
            player.ResetAfterTeleport(v.Length > 4 ? v[4] : 0f);
            Debug.Log("[Probe] Player moved to " + player.transform.position.ToString("F2") + " yaw " + v[3].ToString("F0"));
        }

        static void Run()
        {
            if (Application.isPlaying && File.Exists(Teleport) && !EditorApplication.isCompiling) TeleportRequest();
            if (File.Exists(Look) && !EditorApplication.isCompiling && !EditorApplication.isUpdating) LookRequest();
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            var log = new StringBuilder();
            try
            {
                Scene station = SceneManager.GetSceneByPath(StationPath);
                if (!station.IsValid() || !station.isLoaded) throw new System.Exception("Open Inside_Police_Station first.");
                log.AppendLine("Scene dirty: " + station.isDirty);
                var all = station.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                var player = all.Select(t => t.GetComponent<FirstPersonController>()).First(p => p != null);
                log.AppendLine("Player object: " + Path(player.transform) + " pos " + player.transform.position.ToString("F3") + " yaw " + player.transform.eulerAngles.y.ToString("F1"));
                foreach (var component in player.GetComponents<Component>()) log.AppendLine("  component " + (component != null ? component.GetType().Name : "missing"));
                var controller = player.GetComponent<CharacterController>();
                if (controller != null) log.AppendLine("  CharacterController height " + controller.height + " radius " + controller.radius + " center " + controller.center.ToString("F2"));
                log.AppendLine("  CinemachineCameraTarget " + (player.CinemachineCameraTarget != null ? player.CinemachineCameraTarget.transform.position.ToString("F3") : "none"));
                foreach (Transform child in player.transform.root.GetComponentsInChildren<Transform>(true).Where(t => t.parent == player.transform.root || t.parent == player.transform))
                    log.AppendLine("  child " + Path(child));
                log.AppendLine("Footstep audio sources in scene: " + all.Count(t => t.GetComponent<AudioSource>() != null));

                string[] guids = {
                    "baf66db5e210462499595e160b6137e4","dc7967699afc45a6b52b02d35669e8d1","64f1739e97fc4ff385877ed173726924","ff13d5b2705f4a2da8aa643da2b7490e",
                    "5964dac5eee043989db1558ccee598de","16e291377f3a460ab584858c77aa8b98","058ccc1f4f534eabb92057e28d2908ce","d6a163ed7e2b41d0b03fe4f2f928d454",
                    "a4b257bbde514f96a31b4ea82159d521","3f26e6c2b34c466ea804d8e41f309c4a", "703f5bbedc211a54bba77e758a020422", "bca392387fe1513cbb040163148918ab" };
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    log.AppendLine("guid " + guid + " -> " + path + (clip != null ? " (" + clip.length.ToString("F2") + " s)" : ""));
                }

                Physics.SyncTransforms();
                var region = new Bounds(new Vector3(-17.5f, 1.5f, 5f), new Vector3(8f, 5f, 12f));
                log.AppendLine("--- Renderers touching x[-21.5,-13.5] z[-1,11] ---");
                foreach (var renderer in station.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(false)))
                {
                    if (!renderer.bounds.Intersects(region)) continue;
                    var b = renderer.bounds;
                    if (b.size.x > 12f || b.size.z > 12f) continue;   // skip the floor and long walls
                    log.AppendLine(Path(renderer.transform) + " | centre " + b.center.ToString("F2") + " size " + b.size.ToString("F2") + " min " + b.min.ToString("F2") + " max " + b.max.ToString("F2"));
                }
                log.AppendLine("--- Walls and floors in the same area ---");
                foreach (var renderer in station.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(false)))
                {
                    if (!renderer.bounds.Intersects(region)) continue;
                    var b = renderer.bounds;
                    if (b.size.x <= 12f && b.size.z <= 12f) continue;
                    log.AppendLine(Path(renderer.transform) + " | centre " + b.center.ToString("F2") + " size " + b.size.ToString("F2"));
                }
                File.WriteAllText(Result, "PASS\n" + log);
            }
            catch (System.Exception error) { File.WriteAllText(Result, "FAIL\n" + error + "\n" + log); }
        }
    }
}
#endif
