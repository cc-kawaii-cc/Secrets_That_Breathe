#if UNITY_EDITOR
using System;
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
    // One-time scene setup for the Po scene:
    //  * a prisoner's voice when Khem walks up to the glass doors in front of the cells (PoGlassDoorVoice)
    //  * pressing E at the SleepingPolice shows his snoring, then Khem answers (PoSleepingPoliceInteraction)
    // It copies the dialogue Canvases / '[E]' prompt / text labels that the duty-officer conversation already uses, adds a
    // SleepingPolice to the scene if there is none, and saves the scene. Nothing here runs at game time.
    // Runs once by itself after the scripts compile; menu: Tools > Po Scene.
    [InitializeOnLoad]
    public static class PoCellBlockDialogueSetup
    {
        const string ScenePath = "Assets/MainScenes/From_PM/Po.unity";
        const string BackupFolder = "Tools/PoCellBlockDialogue";
        const string BackupPath = BackupFolder + "/before-install.unity";
        const string AutoKey = "PoCellBlockDialogue.AutoInstall.v4";
        const string Tag = "[PoCellBlock] ";

        // The user's wording, exactly as typed. Edit the saved labels in the Hierarchy to change it later.
        const string PrisonerLine1 = "ปล่อยกุ";
        const string PrisonerLine2 = "ไอพวกชั่ว";
        const string PrisonerLine3 = "พวกมึงยัดยากุไอเพื่อ เชี้....";
        const string SnoreLine = "คร่อก....ฟี้ คร่อก....ฟี้";
        // Khem thinks this to himself: in brackets and shown like his "(ไอนี่อีกแล้วเหรอวะ)" in the station opening (bottom of the screen, no name).
        const string KhemLine = "(นี่เวลางานยังนอนอีกหรอ ตำรวจพวกนี้ไม่ได้ความเลย ค...)";
        const string PrisonerSpeaker = "ผู้ต้องขัง";
        const string PoliceSpeaker = "ตำรวจ";
        const string PromptLine = "[E] ดูตำรวจ";
        // The glass doors in front of the cells (names of the prefab instances in Po.unity).
        static readonly string[] DoorNames = { "glass door", "glass door (1)" };
        // Bumped whenever the scene layout changes; an older scene is rebuilt automatically (the SleepingPolice and the trigger boxes are kept).
        const int Revision = 4;
        const float ZoneDepth = 2f, ZoneHeight = 2.4f;   // default size of the box in front of a door (metres); adjust in the Scene view

        static PoCellBlockDialogueSetup() => EditorApplication.delayCall += AutoInstall;

        [MenuItem("Tools/Po Scene/Add Glass Door Voice + Sleeping Police Dialogue")]
        static void MenuInstall() => Install(false);

        [MenuItem("Tools/Po Scene/Rebuild Glass Door Voice + Sleeping Police Dialogue")]
        static void MenuReinstall() => Install(true);

        static void AutoInstall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoInstall; return; }
            if (SessionState.GetBool(AutoKey, false)) return;
            SessionState.SetBool(AutoKey, true);
            if (!File.Exists(ScenePath)) return;
            if (File.ReadAllText(ScenePath).Contains("setupRevision: " + Revision)) return;
            Install(false);
        }

        static void Install(bool reinstall)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning(Tag + "Stop Play Mode first."); return; }
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            bool wasDirty = wasLoaded && scene.isDirty;
            Scene original = SceneManager.GetActiveScene();
            var log = new StringBuilder();
            try
            {
                if (!wasLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();

                var gameplay = all.FirstOrDefault(t => t.name == "StationGameplay");
                if (gameplay == null) throw new Exception("StationGameplay was not found in Po.");
                var existing = gameplay.Find("PoCellBlockDialogue");
                var oldVoice = existing != null ? existing.GetComponentInChildren<PoGlassDoorVoice>(true) : null;
                bool upgrade = existing != null && (oldVoice == null || oldVoice.setupRevision < Revision);
                if (existing != null && !reinstall && !upgrade)
                { Debug.Log(Tag + "SKIPPED: PoCellBlockDialogue is already in the Po scene (use Tools > Po Scene > Rebuild to redo it)."); return; }
                if (upgrade && !reinstall) log.AppendLine("Upgrading the earlier install to revision " + Revision + ".");

                var canvasRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "SeatingCanvas");
                if (canvasRoot == null) throw new Exception("SeatingCanvas was not found in Po.");
                var player = all.Select(t => t.GetComponent<FirstPersonController>()).FirstOrDefault(p => p != null);
                var opening = all.Select(t => t.GetComponent<PoliceStationOpening>()).FirstOrDefault(p => p != null);
                var duty = all.Select(t => t.GetComponent<PoliceStationDutyOfficer>()).FirstOrDefault(p => p != null);
                if (player == null || opening == null || duty == null)
                    throw new Exception("Po needs the player, PoliceStationOpening and PoliceStationDutyOfficer (the duty-officer conversation is copied).");
                var policeCanvasSource = canvasRoot.transform.Find("StationOfficer/OfficerDialogueCanvas");
                var promptSource = canvasRoot.transform.Find("StationOfficer/OfficerPrompt");
                var lineSource = canvasRoot.transform.Find("StationOfficer/Lines/02_OfficerCamerasAreBroken");
                // Khem's thought uses the label of his opening line ("(ไอนี่อีกแล้วเหรอวะ)") so it looks exactly the same.
                var thoughtSource = canvasRoot.transform.Find("StationOpening/Lines/01_ManInSunglassesPassesKhem") ?? lineSource;
                if (policeCanvasSource == null || promptSource == null || lineSource == null)
                    throw new Exception("SeatingCanvas/StationOfficer/{OfficerDialogueCanvas, OfficerPrompt, Lines/02_OfficerCamerasAreBroken} were not found.");

                var doors = DoorNames.Select(n => all.FirstOrDefault(t => t.name == n && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                                                  ?? all.FirstOrDefault(t => t.name == n)).Where(t => t != null).ToArray();
                if (doors.Length == 0) throw new Exception("No glass door (\"" + string.Join("\", \"", DoorNames) + "\") was found in Po.");

                Directory.CreateDirectory(BackupFolder);
                if (!File.Exists(BackupPath)) EditorSceneManager.SaveScene(scene, BackupPath, true);

                // ---- clean up a previous install (the SleepingPolice itself is kept) --------------------------------------------------------
                // settings already tuned in the Inspector and the trigger boxes are kept
                var oldTalk = existing != null ? existing.GetComponentInChildren<PoSleepingPoliceInteraction>(true) : null;
                bool keepTalk = oldTalk != null, keepVoice = oldVoice != null;
                float talkDistance = keepTalk ? oldTalk.interactDistance : 0f, talkDot = keepTalk ? oldTalk.facingDotMinimum : 0f;
                float talkHeight = keepTalk ? oldTalk.lookAtHeight : 0f;
                bool talkRepeat = keepTalk && oldTalk.canRepeat;
                float talkHold = keepTalk ? oldTalk.thoughtHoldSeconds : 3.5f;
                float voiceHeight = keepVoice ? oldVoice.checkHeight : 0f;
                bool voiceOnce = !keepVoice || oldVoice.playOnlyOnce;
                Transform keptZones = existing != null ? existing.Find("GlassDoorZones") : null;
                if (keptZones != null) keptZones.SetParent(null, true);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                var oldUi = canvasRoot.transform.Find("PoCellBlock");
                if (oldUi != null) UnityEngine.Object.DestroyImmediate(oldUi.gameObject);

                // ---- the SleepingPolice: use the one in the scene, otherwise add one near the doors ------------------------------------------
                var fresh = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                var police = FindSleepingPolice(fresh, canvasRoot.transform);
                Physics.SyncTransforms();
                if (police == null)
                {
                    var prefab = LoadPolicePrefab();
                    if (prefab == null) throw new Exception("SleepingPolice.prefab was not found in the project (CellCharacters pack).");
                    var door = doors[0];
                    Vector3 around = door.position + new Vector3(3f, 0f, 2.3f);
                    var avoid = new System.Collections.Generic.List<Vector3>();
                    var path = new System.Collections.Generic.List<Vector3>();
                    if (opening.route != null) foreach (var r in opening.route) if (r != null) path.Add(r.position);
                    avoid.Add(player.transform.position);
                    if (duty.officer != null) avoid.Add(duty.officer.position);
                    Vector3 spot = FindSpot(scene, player.transform.root, around, avoid, path, log);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.name = "SleepingPolice";
                    instance.transform.SetParent(gameplay, false);
                    Vector3 face = Vector3.ProjectOnPlane(player.transform.position - spot, Vector3.up);
                    if (face.sqrMagnitude < .01f) face = Vector3.forward;
                    instance.transform.SetPositionAndRotation(spot, Quaternion.LookRotation(face.normalized, Vector3.up));
                    police = instance.transform;
                    log.AppendLine("Added SleepingPolice at " + spot.ToString("F2") + " (move it wherever you like; the scripts follow it).");
                }
                else log.AppendLine("Using the SleepingPolice already in the scene: " + police.name + " at " + police.position.ToString("F2"));

                // ---- UI: two dialogue boxes, a '[E]' prompt and three text labels, copied from the duty-officer conversation ----------------
                var container = new GameObject("PoCellBlock", typeof(RectTransform));
                container.transform.SetParent(canvasRoot.transform, false);
                Stretch(container.GetComponent<RectTransform>());
                var voiceCanvas = CloneInto(policeCanvasSource, container.transform, scene, "PrisonerVoiceDialogueCanvas");
                var policeCanvas = CloneInto(policeCanvasSource, container.transform, scene, "SleepingPoliceDialogueCanvas");
                var promptObject = CloneInto(promptSource, container.transform, scene, "SleepingPolicePrompt");
                var lines = new GameObject("Lines", typeof(RectTransform));
                lines.transform.SetParent(container.transform, false);
                Stretch(lines.GetComponent<RectTransform>());
                var shout1 = CloneInto(lineSource, lines.transform, scene, "01_Prisoner_ReleaseMe");
                var shout2 = CloneInto(lineSource, lines.transform, scene, "02_Prisoner_YouScoundrels");
                var shout3 = CloneInto(lineSource, lines.transform, scene, "03_Prisoner_YouPlantedDrugsOnMe");
                var snore = CloneInto(lineSource, lines.transform, scene, "04_SleepingPoliceSnores");
                var complaint = CloneInto(thoughtSource, lines.transform, scene, "05_KhemThinksPoliceAsleep");
                var shoutLabels = new[] { SetText(shout1, PrisonerLine1), SetText(shout2, PrisonerLine2), SetText(shout3, PrisonerLine3) };
                var snoreLabel = SetText(snore, SnoreLine);
                var complaintLabel = SetText(complaint, KhemLine);
                var voiceSpeaker = voiceCanvas.transform.Find("FlowingDialogue/Speaker").GetComponent<TMP_Text>();
                var policeSpeaker = policeCanvas.transform.Find("FlowingDialogue/Speaker").GetComponent<TMP_Text>();
                voiceSpeaker.text = PrisonerSpeaker; policeSpeaker.text = PoliceSpeaker;
                var promptLabel = promptObject.GetComponentInChildren<TMP_Text>(true);
                if (promptLabel == null) throw new Exception("The copied prompt has no text label.");
                promptLabel.text = PromptLine;
                foreach (var item in new[] { voiceSpeaker, policeSpeaker, promptLabel }) EditorUtility.SetDirty(item);
                foreach (var clone in new[] { voiceCanvas, policeCanvas, promptObject, shout1, shout2, shout3, snore, complaint }) clone.SetActive(false);

                // ---- the two components ----------------------------------------------------------------------------------------------------------
                var root = new GameObject("PoCellBlockDialogue");
                root.transform.SetParent(gameplay, false);
                var presentation = CopyPresentation(duty.presentation);

                var voiceObject = new GameObject("GlassDoorPrisonerVoice");
                voiceObject.transform.SetParent(root.transform, false);
                var voice = voiceObject.AddComponent<PoGlassDoorVoice>();
                voice.opening = opening;
                voice.player = player;
                voice.setupRevision = Revision;
                if (keepVoice) { voice.checkHeight = voiceHeight; voice.playOnlyOnce = voiceOnce; }
                var zonesRoot = keptZones;
                if (zonesRoot == null)
                {
                    zonesRoot = new GameObject("GlassDoorZones").transform;
                    foreach (var door in doors) MakeZone(zonesRoot, door, player.transform.position, log);
                }
                zonesRoot.SetParent(root.transform, true);
                voice.triggerZones = zonesRoot.GetComponentsInChildren<BoxCollider>(true);
                voice.voiceDialogue = MakeView(voiceCanvas, voiceSpeaker, duty.officerDialogue);
                voice.presentation = presentation;
                voice.voiceLines = shoutLabels;
                EditorUtility.SetDirty(voice);

                var policeObject = new GameObject("SleepingPoliceInteraction");
                policeObject.transform.SetParent(root.transform, false);
                var interaction = policeObject.AddComponent<PoSleepingPoliceInteraction>();
                interaction.opening = opening;
                interaction.player = player;
                interaction.police = police;
                if (keepTalk)
                { interaction.interactDistance = talkDistance; interaction.facingDotMinimum = talkDot; interaction.lookAtHeight = talkHeight; interaction.canRepeat = talkRepeat; interaction.thoughtHoldSeconds = talkHold; }
                interaction.prompt = promptLabel;
                interaction.policeDialogue = MakeView(policeCanvas, policeSpeaker, duty.officerDialogue);
                interaction.presentation = CopyPresentation(duty.presentation);
                interaction.snoreLines = new[] { snoreLabel };
                interaction.khemThoughtLines = new[] { complaintLabel };
                EditorUtility.SetDirty(interaction);

                log.AppendLine("Trigger boxes: " + voice.triggerZones.Length + " (PoCellBlockDialogue/GlassDoorZones)");

                EditorSceneManager.MarkSceneDirty(scene);
                string saved;
                if (wasDirty) saved = "The Po scene had unsaved changes, so it was NOT saved - press Ctrl+S to keep this.";
                else if (EditorSceneManager.SaveScene(scene)) saved = "Saved Assets/MainScenes/From_PM/Po.unity.";
                else throw new Exception("Saving the Po scene failed.");
                Debug.Log(Tag + "PASS. " + saved + "\n" + log);
            }
            catch (Exception error) { Debug.LogError(Tag + "FAIL " + error.Message + "\n" + log + "\n" + error); }
            finally
            {
                if (!wasLoaded && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }

        // A box on the Khem side of the door, as wide as the door. It is only a marker for the script (trigger, Ignore Raycast layer).
        static void MakeZone(Transform parent, Transform door, Vector3 khemPosition, StringBuilder log)
        {
            var bounds = new Bounds(door.position, Vector3.zero);
            foreach (var r in door.GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(r.bounds);
            float width = Mathf.Max(bounds.size.x, bounds.size.z, 1f);
            float thickness = Mathf.Min(bounds.size.x, bounds.size.z);
            Quaternion yaw = Quaternion.Euler(0f, door.eulerAngles.y, 0f);
            Vector3 forward = yaw * Vector3.forward;
            Vector3 toKhem = khemPosition - door.position;
            toKhem.y = 0f;
            float side = Vector3.Dot(forward, toKhem) >= 0f ? 1f : -1f;
            Vector3 center = new Vector3(bounds.center.x, door.position.y, bounds.center.z) + forward * side * (thickness * .5f + ZoneDepth * .5f);
            var zone = new GameObject("Zone_" + door.name);
            zone.transform.SetParent(parent, false);
            zone.transform.SetPositionAndRotation(center, yaw);
            zone.layer = 2;   // Ignore Raycast
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, ZoneHeight * .5f, 0f);
            box.size = new Vector3(width, ZoneHeight, ZoneDepth);
            log.AppendLine("Glass door '" + door.name + "' at " + door.position.ToString("F2") + ", door width " + width.ToString("F2")
                + " m -> trigger box " + box.size.ToString("F2") + " at " + center.ToString("F2"));
        }

        static Transform FindSleepingPolice(Transform[] all, Transform canvasRoot)
        {
            // the top-most object named SleepingPolice* outside the UI (the prefab's own children share the name)
            return all.FirstOrDefault(t => t.name.StartsWith("SleepingPolice") && !t.IsChildOf(canvasRoot)
                && !(t.parent != null && t.parent.name.StartsWith("SleepingPolice")));
        }

        static GameObject LoadPolicePrefab()
        {
            foreach (var guid in AssetDatabase.FindAssets("SleepingPolice t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == "SleepingPolice") return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        // A free spot (no furniture / walls in a 1.2 m box) closest to 'around', away from Khem's start, the route and the officer.
        static Vector3 FindSpot(Scene scene, Transform playerRoot, Vector3 around, System.Collections.Generic.List<Vector3> avoid,
            System.Collections.Generic.List<Vector3> path, StringBuilder log)
        {
            var offsets = new System.Collections.Generic.List<Vector2>();
            for (float x = -5f; x <= 5f; x += .5f) for (float z = -4f; z <= 4f; z += .5f) offsets.Add(new Vector2(x, z));
            offsets.Sort((a, b) => a.sqrMagnitude.CompareTo(b.sqrMagnitude));
            foreach (var o in offsets)
            {
                Vector3 p = around + new Vector3(o.x, 0f, o.y);
                if (!FloorHeight(p, scene, playerRoot, out float y)) continue;
                p.y = y;
                bool tooClose = false;
                foreach (var a in avoid) if (new Vector2(a.x - p.x, a.z - p.z).magnitude < 1.8f) { tooClose = true; break; }
                for (int i = 1; !tooClose && i < path.Count; i++)
                    if (DistanceToSegment(p, path[i - 1], path[i]) < 1.6f) tooClose = true;
                if (tooClose) continue;
                bool blocked = false;
                foreach (var c in Physics.OverlapBox(p + Vector3.up * .95f, new Vector3(.6f, .8f, .6f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    if (c.gameObject.scene == scene && !c.transform.IsChildOf(playerRoot)) { blocked = true; break; }
                if (blocked) continue;
                return p;
            }
            log.AppendLine("WARNING: no free floor spot found near the glass door; the SleepingPolice was put at " + around.ToString("F2") + " - please move it.");
            if (FloorHeight(around, scene, playerRoot, out float fallbackY)) around.y = fallbackY;
            return around;
        }

        static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 ap = new Vector2(p.x - a.x, p.z - a.z), ab = new Vector2(b.x - a.x, b.z - a.z);
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
            return (ap - ab * t).magnitude;
        }

        static bool FloorHeight(Vector3 position, Scene scene, Transform playerRoot, out float height)
        {
            float nearest = float.PositiveInfinity;
            height = position.y;
            foreach (var hit in Physics.RaycastAll(position + Vector3.up * 1.5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.gameObject.scene == scene && !hit.transform.IsChildOf(playerRoot) && hit.normal.y > .5f && hit.distance < nearest)
                { nearest = hit.distance; height = hit.point.y; }
            return !float.IsPositiveInfinity(nearest);
        }

        static DialogueCanvasView MakeView(GameObject canvas, TMP_Text speaker, DialogueCanvasView like)
        {
            return new DialogueCanvasView
            {
                canvas = canvas.GetComponent<Canvas>(),
                motionRoot = (RectTransform)canvas.transform.Find("FlowingDialogue"),
                speaker = speaker,
                sentence = canvas.transform.Find("FlowingDialogue/Sentence").GetComponent<TMP_Text>(),
                normalizeFontBaseline = like.normalizeFontBaseline,
                layoutRevision = like.layoutRevision,
            };
        }

        static FlowingDialogueSettings CopyPresentation(FlowingDialogueSettings s)
        {
            return new FlowingDialogueSettings
            {
                screenAnchor = s.screenAnchor, widthFraction = s.widthFraction,
                secondsPerCharacter = s.secondsPerCharacter, completedLineHold = s.completedLineHold, scrollSpeed = s.scrollSpeed,
                typingAudio = s.typingAudio,
                sentenceColor = s.sentenceColor, khemColor = s.khemColor, otherSpeakerColor = s.otherSpeakerColor,
            };
        }

        static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }

        static TMP_Text SetText(GameObject line, string text)
        {
            var label = line.GetComponentInChildren<TMP_Text>(true);
            if (label == null) throw new Exception("A copied line has no text label.");
            label.text = text;
            EditorUtility.SetDirty(label);
            return label;
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
    }
}
#endif
