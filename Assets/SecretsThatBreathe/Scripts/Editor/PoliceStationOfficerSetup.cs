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
    // One-time scene setup for Inside_Police_Station step 2 and the footstep sounds. Nothing here runs at game time.
    // Drop Tools/PoliceStationOfficer/install.request in the project to run it (add reinstall.request to rebuild).
    [InitializeOnLoad]
    public static class PoliceStationOfficerSetup
    {
        const string Folder = "Tools/PoliceStationOfficer";
        const string Request = Folder + "/install.request";
        const string Reinstall = Folder + "/reinstall.request";
        const string Result = Folder + "/install.result.txt";
        const string StationPath = "Assets/MainScenes/From_PM/Inside_Police_Station.unity";
        const string EveningPath = "Assets/MainScenes/From_PM/MainScene_2_Evening.unity";
        const string OfficerPrefabPath = "Assets/SecretsThatBreathe/Characters/ThaiPolice/IdleTalk/ThaiPolice_IdleTalk.prefab";
        const string AskLine = "พอดีผมเป็นทนายในคดีอุบัติเหตุที่หมู่บ้านก่อนหน้านี้ ผมอยากจะขอดูไฟล์กล้องวงจรปิดหน่อยน่ะครับ";
        const string ReplyLine = "พอดีกล้องเสียพอดีครับยังไม่ได้เรียกช่างมาซ่อมเลย";
        const string ThoughtLine = "(อะไรก็ไม่ได้ดั่งใจซักอย่าง)";
        const string PromptLine = "[E] พูดคุยกับเจ้าหน้าที่";
        const string SpeakerName = "ตำรวจ";
        // The indoor concrete footsteps the Lawyer Room player uses.
        static readonly string[] FootstepGuids =
        {
            "baf66db5e210462499595e160b6137e4", "dc7967699afc45a6b52b02d35669e8d1", "64f1739e97fc4ff385877ed173726924", "ff13d5b2705f4a2da8aa643da2b7490e",
            "5964dac5eee043989db1558ccee598de", "16e291377f3a460ab584858c77aa8b98", "058ccc1f4f534eabb92057e28d2908ce", "d6a163ed7e2b41d0b03fe4f2f928d454",
            "a4b257bbde514f96a31b4ea82159d521", "3f26e6c2b34c466ea804d8e41f309c4a",
        };
        static PoliceStationOfficerSetup() => EditorApplication.update += Install;

        public static void Install()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
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
                var officerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OfficerPrefabPath);
                if (officerPrefab == null) throw new Exception("Officer prefab not found: " + OfficerPrefabPath);
                var clips = FootstepGuids.Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
                if (clips.Any(c => c == null)) throw new Exception("A footstep clip was not found.");

                if (!stationWasLoaded) station = EditorSceneManager.OpenScene(StationPath, OpenSceneMode.Additive);
                if (station.isDirty) throw new Exception("Inside_Police_Station has unsaved changes. Save or revert it first, then try again.");
                var all = station.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                var gameplay = all.FirstOrDefault(t => t.name == "StationGameplay");
                if (gameplay == null) throw new Exception("StationGameplay (step 1) was not found.");
                var existing = gameplay.Find("DutyOfficerConversation");
                if (existing != null && !reinstall)
                { File.WriteAllText(Result, "SKIPPED: DutyOfficerConversation already exists in the scene."); return; }
                if (!File.Exists(Folder + "/before-officer.unity"))
                    if (!EditorSceneManager.SaveScene(station, Folder + "/before-officer.unity", true)) throw new Exception("Backup failed.");

                var canvasRoot = station.GetRootGameObjects().First(g => g.name == "SeatingCanvas");
                var player = all.Select(t => t.GetComponent<FirstPersonController>()).First(p => p != null);
                var opening = gameplay.GetComponent<PoliceStationOpening>();
                var man = gameplay.Find("SunglassesMan_Walking");
                if (opening == null || man == null) throw new Exception("Step 1 objects (PoliceStationOpening, SunglassesMan_Walking) were not found.");
                var typing = canvasRoot.transform.Find("StationOpening/DialogueTypingAudio");
                if (typing == null) throw new Exception("SeatingCanvas/StationOpening/DialogueTypingAudio was not found.");

                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                foreach (var oldName in new[] { "DutyOfficer_ThaiPolice" })
                { var old = gameplay.Find(oldName); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject); }
                var oldUi = canvasRoot.transform.Find("StationOfficer");
                if (oldUi != null) UnityEngine.Object.DestroyImmediate(oldUi.gameObject);

                // ---- Footsteps: Khem (same indoor clips and levels as the Lawyer Room) --------------------------------------------------
                var playerBody = player.gameObject;
                var playerSteps = playerBody.GetComponent<PlayerFootsteps>();
                if (playerSteps == null) playerSteps = playerBody.AddComponent<PlayerFootsteps>();
                var playerAudio = ChildAudio(playerBody.transform, "FootstepAudio");
                playerAudio.spatialBlend = 0f; playerAudio.volume = .2f;
                playerSteps.audioSource = playerAudio;
                playerSteps.footstepClips = clips;
                playerSteps.roadClips = new AudioClip[0]; playerSteps.terrainClips = new AudioClip[0];
                playerSteps.volume = 1f; playerSteps.walkStepDistance = .8f; playerSteps.sprintStepDistance = .8f; playerSteps.firstStepDistance = .2f;
                playerSteps.pitchRange = new Vector2(.95f, 1.05f);
                playerSteps.concreteClipsRevision = 1; playerSteps.cadenceSettingsRevision = 1;
                EditorUtility.SetDirty(playerSteps); EditorUtility.SetDirty(playerAudio);
                log.AppendLine("Player footsteps on " + playerBody.name + ": " + clips.Length + " clips, step " + playerSteps.walkStepDistance + " m, volume " + playerSteps.volume + " x source " + playerAudio.volume);

                // ---- Footsteps: the man in sunglasses (3D, synced to his walk cycle) -------------------------------------------------------
                var manSteps = man.GetComponent<AnimatedFootsteps>();
                if (manSteps == null) manSteps = man.gameObject.AddComponent<AnimatedFootsteps>();
                var manAudio = ChildAudio(man, "FootstepAudio");
                manAudio.spatialBlend = 1f; manAudio.volume = .3f; manAudio.minDistance = 1.5f; manAudio.maxDistance = 14f;
                manAudio.rolloffMode = AudioRolloffMode.Linear; manAudio.dopplerLevel = 0f;
                manSteps.animator = man.GetComponentInChildren<Animator>();
                manSteps.audioSource = manAudio;
                manSteps.footstepClips = clips;
                manSteps.volume = 1f;
                manSteps.footLandingPhases = new[] { 0f, .5f };
                manSteps.phaseDelay = .03f;
                manSteps.pitchRange = new Vector2(.88f, .98f);
                EditorUtility.SetDirty(manSteps); EditorUtility.SetDirty(manAudio);

                // ---- UI: officer dialogue box, '[E]' prompt and three lines copied from the Evening scene --------------------------------------
                if (!eveningWasLoaded) evening = EditorSceneManager.OpenScene(EveningPath, OpenSceneMode.Additive);
                var eveningCanvas = evening.GetRootGameObjects().First(g => g.name == "SeatingCanvas").transform;
                var policeSource = eveningCanvas.Find("PoliceEncounter/PoliceDialogueCanvas");
                var promptSource = eveningCanvas.Find("PoliceEncounter/BoardCarPrompt");
                var lineSource = eveningCanvas.Find("PoliceEncounter/Lines/03_lieReply1");
                if (policeSource == null || promptSource == null || lineSource == null) throw new Exception("Evening dialogue objects were not found.");
                var encounter = evening.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EveningPoliceEncounter>(true)).First();

                var container = new GameObject("StationOfficer", typeof(RectTransform));
                container.transform.SetParent(canvasRoot.transform, false);
                Stretch(container.GetComponent<RectTransform>());
                var police = CloneInto(policeSource, container.transform, station, "OfficerDialogueCanvas");
                var promptObject = CloneInto(promptSource, container.transform, station, "OfficerPrompt");
                var lines = new GameObject("Lines", typeof(RectTransform));
                lines.transform.SetParent(container.transform, false);
                Stretch(lines.GetComponent<RectTransform>());
                var ask = CloneInto(lineSource, lines.transform, station, "01_KhemAsksForCctvFiles");
                var reply = CloneInto(lineSource, lines.transform, station, "02_OfficerCamerasAreBroken");
                var thought = CloneInto(lineSource, lines.transform, station, "03_KhemThinksToHimself");
                var askLabel = SetText(ask, AskLine);
                var replyLabel = SetText(reply, ReplyLine);
                var thoughtLabel = SetText(thought, ThoughtLine);
                var speaker = police.transform.Find("FlowingDialogue/Speaker").GetComponent<TMP_Text>();
                speaker.text = SpeakerName;
                var promptLabel = promptObject.GetComponentInChildren<TMP_Text>(true);
                if (promptLabel == null) throw new Exception("The copied prompt has no text label.");
                promptLabel.text = PromptLine;
                foreach (var item in new[] { speaker, promptLabel }) EditorUtility.SetDirty(item);
                police.SetActive(false); promptObject.SetActive(false); ask.SetActive(false); reply.SetActive(false); thought.SetActive(false);
                foreach (var clone in new[] { police, promptObject, ask, reply, thought })
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

                // ---- The officer at the right-hand counter ----------------------------------------------------------------------------------------
                Physics.SyncTransforms();
                Vector3 spot = new Vector3(-19.0f, .28775f, 6.85f);
                Vector3 talkSpot = new Vector3(-17.0f, .28775f, 6.2f);
                spot.y = FloorHeight(spot, station, player, spot.y);
                var officer = (GameObject)PrefabUtility.InstantiatePrefab(officerPrefab, station);
                officer.name = "DutyOfficer_ThaiPolice";
                officer.transform.SetParent(gameplay, false);
                Vector3 toKhem = Vector3.ProjectOnPlane(talkSpot - spot, Vector3.up);
                officer.transform.SetPositionAndRotation(spot, Quaternion.LookRotation(toKhem.normalized, Vector3.up));
                Physics.SyncTransforms();
                log.AppendLine("Officer at " + spot.ToString("F3") + " facing " + officer.transform.eulerAngles.y.ToString("F0") + " deg");
                foreach (var blocker in Physics.OverlapCapsule(spot + Vector3.up * .3f, spot + Vector3.up * 1.4f, .3f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => c.gameObject.scene == station && !c.transform.IsChildOf(officer.transform) && !c.transform.IsChildOf(player.transform.root)))
                    log.AppendLine("Officer capsule overlaps collider " + blocker.name + " " + blocker.bounds.ToString());
                foreach (var renderer in station.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(false)))
                    if (!renderer.transform.IsChildOf(officer.transform) && renderer.bounds.size.x < 6f && renderer.bounds.size.z < 6f
                        && renderer.bounds.Contains(spot + Vector3.up * 1f))
                        log.AppendLine("Officer chest height is inside renderer " + (renderer.transform.parent != null ? renderer.transform.parent.name : "") + "/" + renderer.name + " " + renderer.bounds.ToString());

                // ---- The conversation component ------------------------------------------------------------------------------------------------------
                var conversation = new GameObject("DutyOfficerConversation");
                conversation.transform.SetParent(gameplay, false);
                var duty = conversation.AddComponent<PoliceStationDutyOfficer>();
                duty.opening = opening;
                duty.player = player;
                duty.officer = officer.transform;
                duty.prompt = promptLabel;
                duty.khemDialogue = opening.khemDialogue;
                duty.officerDialogue = new DialogueCanvasView
                {
                    canvas = police.GetComponent<Canvas>(),
                    motionRoot = (RectTransform)police.transform.Find("FlowingDialogue"),
                    speaker = speaker,
                    sentence = police.transform.Find("FlowingDialogue/Sentence").GetComponent<TMP_Text>(),
                    normalizeFontBaseline = encounter.policeDialogue.normalizeFontBaseline,
                    layoutRevision = encounter.policeDialogue.layoutRevision,
                };
                var source = opening.presentation;
                duty.presentation = new FlowingDialogueSettings
                {
                    screenAnchor = source.screenAnchor, widthFraction = source.widthFraction,
                    secondsPerCharacter = source.secondsPerCharacter, completedLineHold = source.completedLineHold, scrollSpeed = source.scrollSpeed,
                    typingAudio = typing.GetComponent<AudioSource>(),
                    sentenceColor = source.sentenceColor, khemColor = source.khemColor, otherSpeakerColor = source.otherSpeakerColor,
                };
                duty.khemAsksLines = new[] { askLabel };
                duty.officerRepliesLines = new[] { replyLabel };
                duty.khemThoughtLines = new[] { thoughtLabel };
                EditorUtility.SetDirty(duty);
                log.AppendLine("Presentation: " + source.secondsPerCharacter + " s/char, hold " + source.completedLineHold + ", typing audio " + typing.name);

                EditorSceneManager.MarkSceneDirty(station);
                if (!EditorSceneManager.SaveScene(station)) throw new Exception("Station save failed.");
                File.WriteAllText(Result, "PASS\nSaved footsteps (Khem + the man), SeatingCanvas/StationOfficer and StationGameplay/{DutyOfficer_ThaiPolice, DutyOfficerConversation}.\n" + log);
            }
            catch (Exception error) { File.WriteAllText(Result, "FAIL\n" + error + "\n" + log); Debug.LogException(error); }
            finally
            {
                if (!eveningWasLoaded && evening.IsValid() && evening.isLoaded) EditorSceneManager.CloseScene(evening, true);
                if (!stationWasLoaded && station.IsValid() && station.isLoaded) EditorSceneManager.CloseScene(station, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }

        static AudioSource ChildAudio(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
            var source = child.GetComponent<AudioSource>();
            if (source == null) source = child.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = false; source.clip = null;
            return source;
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

        static float FloorHeight(Vector3 position, Scene scene, FirstPersonController player, float fallback)
        {
            float nearest = float.PositiveInfinity, height = fallback;
            foreach (var hit in Physics.RaycastAll(position + Vector3.up * 1.5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.gameObject.scene == scene && !hit.transform.IsChildOf(player.transform.root) && hit.normal.y > .5f && hit.distance < nearest)
                { nearest = hit.distance; height = hit.point.y; }
            return height;
        }
    }
}
#endif
