#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using StarterAssets;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // One-time scene setup for Inside_Police_Station step 1. Nothing here runs at game time.
    // Drop Tools/PoliceStationOpening/install.request in the project to run it (or add reinstall.request to rebuild).
    [InitializeOnLoad]
    public static class PoliceStationOpeningSetup
    {
        const string Folder = "Tools/PoliceStationOpening";
        const string Request = Folder + "/install.request";
        const string Reinstall = Folder + "/reinstall.request";
        const string Result = Folder + "/install.result.txt";
        const string StationPath = "Assets/MainScenes/From_PM/Inside_Police_Station.unity";
        const string EveningPath = "Assets/MainScenes/From_PM/MainScene_2_Evening.unity";
        const string Line = "ไอ้นี่อีกแล้วหรอวะ";
        static PoliceStationOpeningSetup() => EditorApplication.update += Install;

        public static void Install()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            // Wait until the walking model (controller + prefab) has been built.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SuitWalkAssets.PrefabPath);
            if (prefab == null) return;
            File.Delete(Request);
            bool reinstall = File.Exists(Reinstall);
            if (reinstall) File.Delete(Reinstall);

            Scene station = SceneManager.GetSceneByPath(StationPath);
            bool stationWasLoaded = station.IsValid() && station.isLoaded;
            Scene evening = SceneManager.GetSceneByPath(EveningPath);
            bool eveningWasLoaded = evening.IsValid() && evening.isLoaded;
            Scene original = SceneManager.GetActiveScene();
            var log = new StringBuilder();
            try
            {
                if (!stationWasLoaded) station = EditorSceneManager.OpenScene(StationPath, OpenSceneMode.Additive);
                if (station.isDirty) throw new Exception("Inside_Police_Station has unsaved changes. Save or revert it first, then try again.");
                var all = station.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                var existing = all.FirstOrDefault(t => t.name == "StationGameplay");
                if (existing != null && !reinstall)
                { File.WriteAllText(Result, "SKIPPED: StationGameplay already exists in the scene."); return; }
                if (!File.Exists(Folder + "/before-opening.unity"))
                    if (!EditorSceneManager.SaveScene(station, Folder + "/before-opening.unity", true)) throw new Exception("Backup failed.");

                var canvasRoot = station.GetRootGameObjects().First(g => g.name == "SeatingCanvas");
                var arrivalPoint = all.First(t => t.name == "PoliceStationArrivalPoint");
                var door = all.Single(t => t.name == "Door (4)");
                var player = all.Select(t => t.GetComponent<FirstPersonController>()).First(p => p != null);
                var arrival = all.Select(t => t.GetComponent<PoliceStationArrival>()).First(p => p != null);

                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                var oldUi = canvasRoot.transform.Find("StationOpening");
                if (oldUi != null) UnityEngine.Object.DestroyImmediate(oldUi.gameObject);

                // ---- UI: copy Khem's dialogue box, typing sound and a line label from the Evening scene -----------------------------
                if (!eveningWasLoaded) evening = EditorSceneManager.OpenScene(EveningPath, OpenSceneMode.Additive);
                var eveningCanvas = evening.GetRootGameObjects().First(g => g.name == "SeatingCanvas").transform;
                var khemSource = eveningCanvas.Find("PoliceEncounter/KhemDialogueCanvas");
                var audioSource = eveningCanvas.Find("PoliceEncounter/DialogueTypingAudio");
                var lineSource = eveningCanvas.Find("PoliceEncounter/Lines/03_lieReply1");
                if (khemSource == null || audioSource == null || lineSource == null) throw new Exception("Evening dialogue objects were not found.");
                var encounter = evening.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EveningPoliceEncounter>(true)).First();

                var container = new GameObject("StationOpening", typeof(RectTransform));
                container.transform.SetParent(canvasRoot.transform, false);
                var rect = container.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                var khem = CloneInto(khemSource, container.transform, station, "KhemDialogueCanvas");
                var typing = CloneInto(audioSource, container.transform, station, "DialogueTypingAudio");
                var lines = new GameObject("Lines", typeof(RectTransform));
                lines.transform.SetParent(container.transform, false);
                var linesRect = lines.GetComponent<RectTransform>();
                linesRect.anchorMin = Vector2.zero; linesRect.anchorMax = Vector2.one; linesRect.offsetMin = linesRect.offsetMax = Vector2.zero;
                var lineObject = CloneInto(lineSource, lines.transform, station, "01_ManInSunglassesPassesKhem");
                var label = lineObject.GetComponentInChildren<TMP_Text>(true);
                if (label == null) throw new Exception("The copied line has no text label.");
                label.text = Line;
                EditorUtility.SetDirty(label);
                foreach (var clone in new[] { khem, typing, lineObject })
                    foreach (var component in clone.GetComponentsInChildren<Component>(true))
                    {
                        if (component == null) continue;
                        var serialized = new SerializedObject(component);
                        var property = serialized.GetIterator();
                        while (property.NextVisible(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is Component target
                                && target.gameObject.scene.IsValid() && target.gameObject.scene != station)
                                log.AppendLine("WARNING: " + component.name + "." + property.propertyPath + " points into scene " + target.gameObject.scene.name);
                    }

                // ---- Actors and route ---------------------------------------------------------------------------------------------------
                var gameplay = new GameObject("StationGameplay");
                SceneManager.MoveGameObjectToScene(gameplay, station);
                var routeRoot = new GameObject("SunglassesManRoute");
                routeRoot.transform.SetParent(gameplay.transform, false);
                Physics.SyncTransforms();

                var renderers = door.GetComponentsInChildren<Renderer>();
                Bounds doorBounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) doorBounds.Encapsulate(renderer.bounds);
                Vector3 arrivalFeet = arrivalPoint.position;
                Vector3 inward = Vector3.ProjectOnPlane(arrivalPoint.position - doorBounds.center, Vector3.up).normalized;

                // The sketch: the man is on Khem's left side and walks straight past him to the door. He starts 2.6 m in front of Khem
                // (the sketch is only 1.1 m) so he is in view for the whole approach instead of at the edge of the screen.
                Vector3 start = arrivalFeet + new Vector3(.8f, 0f, -2.6f);
                Vector3 beside = new Vector3(start.x, arrivalFeet.y, arrivalFeet.z);
                float exitX = Mathf.Clamp(start.x, doorBounds.min.x + .45f, doorBounds.max.x - .45f);
                Vector3 exit = new Vector3(exitX, arrivalFeet.y, doorBounds.center.z) + inward * .3f;
                var markers = new[]
                {
                    Marker(routeRoot.transform, "01_Start", start, station, player),
                    Marker(routeRoot.transform, "02_BesideKhem", beside, station, player),
                    Marker(routeRoot.transform, "03_ExitThroughDoor", exit, station, player),
                };
                log.AppendLine("Door bounds centre " + doorBounds.center.ToString("F3") + " size " + doorBounds.size.ToString("F3") + "; inward " + inward.ToString("F3"));
                for (int i = 0; i < markers.Length; i++) log.AppendLine(markers[i].name + " " + markers[i].position.ToString("F3"));
                for (int i = 1; i < markers.Length; i++) CheckLeg(markers[i - 1].position, markers[i].position, station, player, log);
                foreach (var marker in markers)
                    foreach (var blocker in Physics.OverlapCapsule(marker.position + Vector3.up * .35f, marker.position + Vector3.up * 1.5f, .25f, ~0, QueryTriggerInteraction.Ignore)
                        .Where(c => c.gameObject.scene == station && !c.transform.IsChildOf(player.transform.root)))
                        log.AppendLine("Marker " + marker.name + " overlaps " + blocker.name);

                var man = (GameObject)PrefabUtility.InstantiatePrefab(prefab, station);
                man.name = "SunglassesMan_Walking";
                man.transform.SetParent(gameplay.transform, false);
                Vector3 first = Vector3.ProjectOnPlane(markers[1].position - markers[0].position, Vector3.up);
                man.transform.SetPositionAndRotation(markers[0].position, Quaternion.LookRotation(first.normalized, Vector3.up));
                var animator = man.GetComponentInChildren<Animator>();

                var opening = gameplay.AddComponent<PoliceStationOpening>();
                opening.arrival = arrival;
                opening.player = player;
                opening.sunglassesMan = man.transform;
                opening.sunglassesAnimator = animator;
                opening.route = markers;
                opening.khemSpeaksAtRoutePoint = 1;
                opening.startWhenBlackScreenBelow = .02f;
                opening.khemDialogue = new DialogueCanvasView
                {
                    canvas = khem.GetComponent<Canvas>(),
                    motionRoot = (RectTransform)khem.transform.Find("FlowingDialogue"),
                    speaker = khem.transform.Find("FlowingDialogue/Speaker").GetComponent<TMP_Text>(),
                    sentence = khem.transform.Find("FlowingDialogue/Sentence").GetComponent<TMP_Text>(),
                    normalizeFontBaseline = encounter.khemDialogue.normalizeFontBaseline,
                    layoutRevision = encounter.khemDialogue.layoutRevision,
                };
                var source = encounter.presentation;
                opening.presentation = new FlowingDialogueSettings
                {
                    screenAnchor = source.screenAnchor, widthFraction = source.widthFraction,
                    secondsPerCharacter = source.secondsPerCharacter, completedLineHold = source.completedLineHold, scrollSpeed = source.scrollSpeed,
                    typingAudio = typing.GetComponent<AudioSource>(),
                    sentenceColor = source.sentenceColor, khemColor = source.khemColor, otherSpeakerColor = source.otherSpeakerColor,
                };
                opening.manPassesLines = new[] { label };
                EditorUtility.SetDirty(opening);

                log.AppendLine("Typing audio volume " + typing.GetComponent<AudioSource>().volume + " loop " + typing.GetComponent<AudioSource>().loop +
                               " clip " + (typing.GetComponent<AudioSource>().resource != null ? typing.GetComponent<AudioSource>().resource.name : "none"));
                log.AppendLine("Presentation: " + source.secondsPerCharacter + " s/char, hold " + source.completedLineHold + ", scroll " + source.scrollSpeed);
                EditorSceneManager.MarkSceneDirty(station);
                if (!EditorSceneManager.SaveScene(station)) throw new Exception("Station save failed.");
                File.WriteAllText(Result, "PASS\nSaved SeatingCanvas/StationOpening (Khem dialogue canvas, typing audio, line) and StationGameplay (man + route + PoliceStationOpening).\n" + log);
            }
            catch (Exception error) { File.WriteAllText(Result, "FAIL\n" + error + "\n" + log); Debug.LogException(error); }
            finally
            {
                if (!eveningWasLoaded && evening.IsValid() && evening.isLoaded) EditorSceneManager.CloseScene(evening, true);
                if (!stationWasLoaded && station.IsValid() && station.isLoaded) EditorSceneManager.CloseScene(station, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }

        static GameObject CloneInto(Transform source, Transform parent, Scene scene, string name)
        {
            var clone = UnityEngine.Object.Instantiate(source.gameObject);
            clone.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(clone, scene);
            clone.name = name;
            clone.transform.SetParent(parent, false);
            return clone;
        }

        static Transform Marker(Transform parent, string name, Vector3 position, Scene scene, FirstPersonController player)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            position.y = FloorHeight(position, scene, player, position.y);
            marker.position = position;
            return marker;
        }

        static float FloorHeight(Vector3 position, Scene scene, FirstPersonController player, float fallback)
        {
            float nearest = float.PositiveInfinity, height = fallback;
            foreach (var hit in Physics.RaycastAll(position + Vector3.up * 1.5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.gameObject.scene == scene && !hit.transform.IsChildOf(player.transform.root) && hit.normal.y > .5f && hit.distance < nearest)
                { nearest = hit.distance; height = hit.point.y; }
            return height;
        }

        static void CheckLeg(Vector3 from, Vector3 to, Scene scene, FirstPersonController player, StringBuilder log)
        {
            Vector3 direction = to - from; float distance = direction.magnitude; direction /= Mathf.Max(.001f, distance);
            var hits = Physics.SphereCastAll(from + Vector3.up * .9f, .25f, direction, distance, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.gameObject.scene == scene && !h.transform.IsChildOf(player.transform.root)).ToArray();
            if (hits.Length == 0) log.AppendLine("Leg clear: " + from.ToString("F2") + " -> " + to.ToString("F2") + " (" + distance.ToString("F2") + " m)");
            foreach (var hit in hits)
                log.AppendLine("Leg blocked by " + hit.collider.name + " at " + hit.point.ToString("F2") + " (" + from.ToString("F2") + " -> " + to.ToString("F2") + ")");
        }
    }
}
#endif
