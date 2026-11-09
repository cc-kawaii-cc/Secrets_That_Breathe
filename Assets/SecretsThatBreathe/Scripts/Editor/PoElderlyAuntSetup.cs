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
    // One-time scene setup for the Po scene: pressing E at the ElderlyAunt starts her conversation with Khem (PoElderlyAuntInteraction).
    // It copies the dialogue Canvas / '[E]' prompt / text labels that the duty-officer conversation already uses, adds an ElderlyAunt
    // to the scene if there is none (put her on a chair or bench yourself - the script follows her wherever she is), and saves the scene.
    // Nothing here runs at game time. Runs once by itself after the scripts compile; menu: Tools > Po Scene.
    [InitializeOnLoad]
    public static class PoElderlyAuntSetup
    {
        const string ScenePath = "Assets/MainScenes/From_PM/Po.unity";
        const string BackupFolder = "Tools/PoElderlyAunt";
        const string BackupPath = BackupFolder + "/before-install.unity";
        const string AutoKey = "PoElderlyAunt.AutoInstall.v2";
        const string RootName = "PoElderlyAuntDialogue";
        const string ZoneName = "Aunt Interaction Zone";
        // Size of the box in front of the old woman, in metres in the scene (a child of her, so it follows her). Adjust it in the Scene view.
        static readonly Vector3 ZoneSize = new Vector3(2.4f, 3f, 2.6f), ZoneCenter = new Vector3(0f, .9f, 1f);
        const string Tag = "[PoElderlyAunt] ";

        const string KhemTag = "K", AuntTag = "A";
        // The user's wording, in order. Edit the saved labels in the Hierarchy to change it later.
        static readonly string[][] Conversation =
        {
            new[] { KhemTag, "01_Khem_Grandma",           "ยายครับ" },
            new[] { KhemTag, "02_Khem_WhyHereSoLate",     "ดึกขนาดนี้มาทำอะไรครับ" },
            new[] { AuntTag, "03_Aunt_BailingOutGrandson", "ยายมาประกันตัวให้หลานจ๊ะ" },
            new[] { AuntTag, "04_Aunt_ThisGrandson",       "หลานยายนี้มัน ดือออ!!" },
            new[] { AuntTag, "05_Aunt_RacedCarsAndCaught", "มันพาเพื่อนไปแว้นรถแล้วโดนจับมาจ๊ะ" },
            new[] { AuntTag, "06_Aunt_KidsNeverListen",    "เด็กสมัยนี้นะพูดอะไรก็ไม่ค่อยจะฟังกัน" },
            new[] { KhemTag, "07_Khem_ItsOkGrandma",       "ไม่เป็นไรนะครับยาย" },
            new[] { KhemTag, "08_Khem_SomedayBetter",      "สักวันหลานยายจะต้องเป็นเด็กที่ดีขึ้น" },
            new[] { AuntTag, "09_Aunt_ThankYou",           "ขอบคุณจ๊ะ พ่อหนุ่ม" },
        };
        const string AuntSpeaker = "ยาย";
        const string PromptLine = "[E] คุยกับยาย";

        static PoElderlyAuntSetup()
        {
            EditorApplication.delayCall += AutoInstall;
            // Pressing Play with a pending change postpones the install; run it as soon as the Editor is back in Edit Mode.
            EditorApplication.playModeStateChanged += state =>
            { if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += AutoInstall; };
        }

        [MenuItem("Tools/Po Scene/Add Elderly Aunt Dialogue")]
        static void MenuInstall() => Install(false);

        [MenuItem("Tools/Po Scene/Rebuild Elderly Aunt Dialogue")]
        static void MenuReinstall() => Install(true);

        static void AutoInstall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoInstall; return; }
            if (SessionState.GetBool(AutoKey, false)) return;
            SessionState.SetBool(AutoKey, true);
            if (!File.Exists(ScenePath)) return;
            // The saved Po scene lists the zone by name once everything has been installed; skip it then.
            if (File.ReadAllText(ScenePath).Contains(ZoneName)) return;
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
                var existing = gameplay.Find(RootName);
                if (existing != null && !reinstall)
                {
                    // Installed by the first version, which had no Interaction Zone: add just the zone to it.
                    var current = existing.GetComponentInChildren<PoElderlyAuntInteraction>(true);
                    if (current == null || current.aunt == null || current.interactionZone != null)
                    { Debug.Log(Tag + "SKIPPED: " + RootName + " is already in the Po scene (use Tools > Po Scene > Rebuild Elderly Aunt Dialogue to redo it)."); return; }
                    current.interactionZone = FindOrMakeZone(current.aunt, log);
                    EditorUtility.SetDirty(current);
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log(Tag + "PASS. " + SaveScene(scene, wasDirty) + "\n" + log);
                    return;
                }

                var canvasRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "SeatingCanvas");
                if (canvasRoot == null) throw new Exception("SeatingCanvas was not found in Po.");
                var player = all.Select(t => t.GetComponent<FirstPersonController>()).FirstOrDefault(p => p != null);
                var opening = all.Select(t => t.GetComponent<PoliceStationOpening>()).FirstOrDefault(p => p != null);
                var duty = all.Select(t => t.GetComponent<PoliceStationDutyOfficer>()).FirstOrDefault(p => p != null);
                if (player == null || opening == null || duty == null)
                    throw new Exception("Po needs the player, PoliceStationOpening and PoliceStationDutyOfficer (the duty-officer conversation is copied).");
                if (!opening.khemDialogue.IsValid) throw new Exception("PoliceStationOpening has no Khem dialogue Canvas to reuse.");
                var policeCanvasSource = canvasRoot.transform.Find("StationOfficer/OfficerDialogueCanvas");
                var promptSource = canvasRoot.transform.Find("StationOfficer/OfficerPrompt");
                var lineSource = canvasRoot.transform.Find("StationOfficer/Lines/02_OfficerCamerasAreBroken");
                if (policeCanvasSource == null || promptSource == null || lineSource == null)
                    throw new Exception("SeatingCanvas/StationOfficer/{OfficerDialogueCanvas, OfficerPrompt, Lines/02_OfficerCamerasAreBroken} were not found.");

                Directory.CreateDirectory(BackupFolder);
                if (!File.Exists(BackupPath)) EditorSceneManager.SaveScene(scene, BackupPath, true);

                // ---- clean up a previous install (the ElderlyAunt herself is kept; settings tuned in the Inspector are kept) -----------------
                var oldTalk = existing != null ? existing.GetComponentInChildren<PoElderlyAuntInteraction>(true) : null;
                bool keep = oldTalk != null;
                float distance = keep ? oldTalk.interactDistance : 0f, dot = keep ? oldTalk.facingDotMinimum : 0f, height = keep ? oldTalk.lookAtHeight : 0f;
                bool repeat = keep && oldTalk.canRepeat;
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                var oldUi = canvasRoot.transform.Find("PoElderlyAunt");
                if (oldUi != null) UnityEngine.Object.DestroyImmediate(oldUi.gameObject);

                // ---- the ElderlyAunt: use the one in the scene, otherwise add one near the duty officer -------------------------------------
                var fresh = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                var aunt = FindAunt(fresh, canvasRoot.transform);
                Physics.SyncTransforms();
                if (aunt == null)
                {
                    var prefab = LoadAuntPrefab();
                    if (prefab == null) throw new Exception("ElderlyAunt.prefab was not found in the project (CellCharacters pack).");
                    Vector3 around = duty.officer != null ? duty.officer.position + new Vector3(2.5f, 0f, 1.5f) : player.transform.position + new Vector3(3f, 0f, 3f);
                    var avoid = new List<Vector3> { player.transform.position };
                    if (duty.officer != null) avoid.Add(duty.officer.position);
                    var path = new List<Vector3>();
                    if (opening.route != null) foreach (var r in opening.route) if (r != null) path.Add(r.position);
                    Vector3 spot = FindSpot(scene, player.transform.root, around, avoid, path, log);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.name = "ElderlyAunt";
                    instance.transform.SetParent(gameplay, false);
                    Vector3 face = Vector3.ProjectOnPlane(player.transform.position - spot, Vector3.up);
                    if (face.sqrMagnitude < .01f) face = Vector3.forward;
                    instance.transform.SetPositionAndRotation(spot, Quaternion.LookRotation(face.normalized, Vector3.up));
                    aunt = instance.transform;
                    log.AppendLine("Added ElderlyAunt at " + spot.ToString("F2") + " - she is sitting, so move her onto a chair or bench (seat about 0.46 m high); the script follows her.");
                }
                else log.AppendLine("Using the ElderlyAunt already in the scene: " + aunt.name + " at " + aunt.position.ToString("F2"));

                // ---- UI: her dialogue box, a '[E]' prompt and the nine text labels, copied from the duty-officer conversation ---------------
                var container = new GameObject("PoElderlyAunt", typeof(RectTransform));
                container.transform.SetParent(canvasRoot.transform, false);
                Stretch(container.GetComponent<RectTransform>());
                var auntCanvas = CloneInto(policeCanvasSource, container.transform, scene, "AuntDialogueCanvas");
                var promptObject = CloneInto(promptSource, container.transform, scene, "AuntPrompt");
                var linesRoot = new GameObject("Lines", typeof(RectTransform));
                linesRoot.transform.SetParent(container.transform, false);
                Stretch(linesRoot.GetComponent<RectTransform>());
                var auntSpeaker = auntCanvas.transform.Find("FlowingDialogue/Speaker").GetComponent<TMP_Text>();
                auntSpeaker.text = AuntSpeaker;
                var promptLabel = promptObject.GetComponentInChildren<TMP_Text>(true);
                if (promptLabel == null) throw new Exception("The copied prompt has no text label.");
                promptLabel.text = PromptLine;
                foreach (var item in new[] { auntSpeaker, promptLabel }) EditorUtility.SetDirty(item);
                var entries = new List<PoAuntLine>();
                var clones = new List<GameObject> { auntCanvas, promptObject };
                foreach (var row in Conversation)
                {
                    var clone = CloneInto(lineSource, linesRoot.transform, scene, row[1]);
                    entries.Add(new PoAuntLine { khemSpeaks = row[0] == KhemTag, text = SetText(clone, row[2]) });
                    clones.Add(clone);
                }
                foreach (var clone in clones) clone.SetActive(false);

                // ---- the component --------------------------------------------------------------------------------------------------------------
                var root = new GameObject(RootName);
                root.transform.SetParent(gameplay, false);
                var interaction = root.AddComponent<PoElderlyAuntInteraction>();
                interaction.opening = opening;
                interaction.player = player;
                interaction.aunt = aunt;
                if (keep) { interaction.interactDistance = distance; interaction.facingDotMinimum = dot; interaction.lookAtHeight = height; interaction.canRepeat = repeat; }
                interaction.prompt = promptLabel;
                interaction.interactionZone = FindOrMakeZone(aunt, log);
                interaction.khemDialogue = opening.khemDialogue;   // Khem's own box, as in the duty-officer conversation
                interaction.auntDialogue = MakeView(auntCanvas, auntSpeaker, duty.officerDialogue);
                interaction.presentation = CopyPresentation(duty.presentation);
                interaction.lines = entries.ToArray();
                EditorUtility.SetDirty(interaction);

                var khemSpeaker = opening.khemDialogue.speaker;
                log.AppendLine("Khem's lines use his existing box (speaker label: \"" + (khemSpeaker != null ? khemSpeaker.text : "none") + "\").");
                log.AppendLine("Lines: " + entries.Count + " (" + entries.Count(e => e.khemSpeaks) + " Khem, " + entries.Count(e => !e.khemSpeaks) + " old woman)");

                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log(Tag + "PASS. " + SaveScene(scene, wasDirty) + "\n" + log);
            }
            catch (Exception error) { Debug.LogError(Tag + "FAIL " + error.Message + "\n" + log + "\n" + error); }
            finally
            {
                if (!wasLoaded && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }

        static string SaveScene(Scene scene, bool wasDirty)
        {
            if (wasDirty) return "The Po scene had unsaved changes, so it was NOT saved - press Ctrl+S to keep this.";
            if (EditorSceneManager.SaveScene(scene)) return "Saved Assets/MainScenes/From_PM/Po.unity.";
            throw new Exception("Saving the Po scene failed.");
        }

        // Like "CCTV Interaction Zone": a trigger box (Ignore Raycast layer) that is a child of the object it belongs to. It is a marker for
        // the script only: Khem is offered the conversation while he stands inside it. A zone that already exists is left as it is.
        static BoxCollider FindOrMakeZone(Transform aunt, StringBuilder log)
        {
            var found = aunt.Find(ZoneName);
            var box = found != null ? found.GetComponent<BoxCollider>() : null;
            if (box != null) { log.AppendLine("Using the existing " + ZoneName + "."); return box; }
            if (found != null) UnityEngine.Object.DestroyImmediate(found.gameObject);
            var zone = new GameObject(ZoneName);
            zone.layer = 2;   // Ignore Raycast, like the other interaction zones in Po
            zone.transform.SetParent(aunt, false);
            zone.transform.localPosition = Vector3.zero;
            zone.transform.localRotation = Quaternion.identity;
            box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            // sizes are in metres in the scene, whatever scale the old woman has
            Vector3 scale = aunt.lossyScale;
            scale = new Vector3(Mathf.Max(.01f, Mathf.Abs(scale.x)), Mathf.Max(.01f, Mathf.Abs(scale.y)), Mathf.Max(.01f, Mathf.Abs(scale.z)));
            box.center = new Vector3(ZoneCenter.x / scale.x, ZoneCenter.y / scale.y, ZoneCenter.z / scale.z);
            box.size = new Vector3(ZoneSize.x / scale.x, ZoneSize.y / scale.y, ZoneSize.z / scale.z);
            log.AppendLine("Added " + ZoneName + " in front of the old woman: " + ZoneSize.ToString("F1") + " m. Select it in the Hierarchy and move / resize it in the Scene view.");
            return box;
        }

        static Transform FindAunt(Transform[] all, Transform canvasRoot)
        {
            // the top-most object named ElderlyAunt* outside the UI (the prefab's own children share the name)
            return all.FirstOrDefault(t => t.name.StartsWith("ElderlyAunt") && !t.IsChildOf(canvasRoot)
                && !(t.parent != null && t.parent.name.StartsWith("ElderlyAunt")));
        }

        static GameObject LoadAuntPrefab()
        {
            foreach (var guid in AssetDatabase.FindAssets("ElderlyAunt t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == "ElderlyAunt") return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        // A free spot (no furniture / walls in a 1.2 m box) closest to 'around', away from Khem's start, the route and the officer.
        static Vector3 FindSpot(Scene scene, Transform playerRoot, Vector3 around, List<Vector3> avoid, List<Vector3> path, StringBuilder log)
        {
            var offsets = new List<Vector2>();
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
            log.AppendLine("WARNING: no free floor spot found near the duty officer; the ElderlyAunt was put at " + around.ToString("F2") + " - please move her.");
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
