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
    // One-time scene setup for Inside_Police_Station step 3 (the "No Entry" sign and the TV). Nothing here runs at game time.
    // Drop Tools/PoliceStationNoEntry/install.request in the project to run it (add reinstall.request to rebuild).
    [InitializeOnLoad]
    public static class PoliceStationNoEntrySetup
    {
        const string Folder = "Tools/PoliceStationNoEntry";
        const string Request = Folder + "/install.request";
        const string Reinstall = Folder + "/reinstall.request";
        const string Result = Folder + "/install.result.txt";
        const string StationPath = "Assets/MainScenes/From_PM/Inside_Police_Station.unity";
        const string LawyerRoomPath = "Assets/MainScenes/From_PM/Lawyer Room.unity";
        const string SignName = "\"No Entry\" sign location";
        const string PromptLine = "[E] ดู";
        const string GoBackLine = "[E] กลับไปดูที่เกิดเหตุ";
        const string SignText = "บุคคลภายนอกห้ามเข้าโดยไม่ได้รับอนุญาต";
        const string TvName = "Tv (2)";
        const string EndName = "end";
        const string LineA = "ก็ไม่เสียนี่หว่า ไอตำรวจพวกนี้เงินเดือนมันไม่พอกินกันรึไงวะ";
        const string LineB = "งั้นกลับไปดูที่เกิดเหตุหน่อยดีกว่า นี่ก็ตี 3 แล้ว คงไม่มีใครอยู่แล้วมั้ง";

        static PoliceStationNoEntrySetup() => EditorApplication.update += Install;

        public static void Install()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            bool reinstall = File.Exists(Reinstall);
            if (reinstall) File.Delete(Reinstall);

            Scene station = SceneManager.GetSceneByPath(StationPath);
            bool stationWasLoaded = station.IsValid() && station.isLoaded;
            Scene lawyer = SceneManager.GetSceneByPath(LawyerRoomPath);
            bool lawyerWasLoaded = lawyer.IsValid() && lawyer.isLoaded;
            Scene original = SceneManager.GetActiveScene();
            var log = new StringBuilder();
            try
            {
                if (!stationWasLoaded) station = EditorSceneManager.OpenScene(StationPath, OpenSceneMode.Additive);
                var all = station.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                var signs = all.Where(t => t.name == SignName).ToArray();
                if (signs.Length == 0) throw new Exception("The object named " + SignName + " was not found in the scene.");
                if (signs.Length > 1) log.AppendLine("WARNING: " + signs.Length + " objects are named " + SignName + "; using the first.");
                var sign = signs[0];
                if (station.isDirty)
                {
                    // The scene holds unsaved work (the sign location object). Keep it: it is saved together with the new objects.
                    log.AppendLine("The scene had unsaved changes (including the sign location object). They are saved with this setup.");
                }
                var gameplay = all.FirstOrDefault(t => t.name == "StationGameplay");
                if (gameplay == null) throw new Exception("StationGameplay (step 1) was not found.");
                var existing = gameplay.Find("NoEntryTvMoment");
                if (existing != null && !reinstall)
                { File.WriteAllText(Result, "SKIPPED: NoEntryTvMoment already exists in the scene."); return; }
                // Backup of the scene exactly as it is on disk now (before the sign location object was ever saved).
                string backup = Folder + "/before-noentry.unity";
                if (!File.Exists(backup)) File.Copy(StationPath, backup, false);

                var canvasRoot = station.GetRootGameObjects().First(g => g.name == "SeatingCanvas");
                var player = all.Select(t => t.GetComponent<FirstPersonController>()).First(p => p != null);
                var opening = gameplay.GetComponent<PoliceStationOpening>();
                var dutyTransform = gameplay.Find("DutyOfficerConversation");
                var duty = dutyTransform != null ? dutyTransform.GetComponent<PoliceStationDutyOfficer>() : null;
                if (opening == null || duty == null) throw new Exception("Steps 1 and 2 (PoliceStationOpening, DutyOfficerConversation) were not found.");
                var ends = all.Where(t => t.name == EndName).ToArray();
                if (ends.Length == 0) throw new Exception("The object named " + EndName + " (the exit doors) was not found in the scene.");
                if (ends.Length > 1) log.AppendLine("WARNING: " + ends.Length + " objects are named " + EndName + "; using the first at the scene root.");
                var end = ends.FirstOrDefault(t => t.parent == null) ?? ends[0];
                var tvs = all.Where(t => t.name == TvName).ToArray();
                if (tvs.Length == 0) throw new Exception("The TV named " + TvName + " was not found.");
                var tv = tvs.FirstOrDefault(t => t.parent == null);
                if (tv == null) tv = tvs[0];
                var lineSource = canvasRoot.transform.Find("StationOfficer/Lines/01_KhemAsksForCctvFiles");
                if (lineSource == null) throw new Exception("SeatingCanvas/StationOfficer/Lines/01_KhemAsksForCctvFiles (step 2) was not found.");

                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                var oldUi = canvasRoot.transform.Find("StationNoEntry");
                if (oldUi != null) UnityEngine.Object.DestroyImmediate(oldUi.gameObject);

                // ---- The sign's prompt and text: the same two panels the Lawyer Room uses for its document ('[E] ดู' and the text) -------------
                if (!lawyerWasLoaded) lawyer = EditorSceneManager.OpenScene(LawyerRoomPath, OpenSceneMode.Additive);
                var lawyerCanvas = lawyer.GetRootGameObjects().FirstOrDefault(g => g.name == "SeatingCanvas");
                if (lawyerCanvas == null) throw new Exception("SeatingCanvas was not found in the Lawyer Room.");
                var promptSource = lawyerCanvas.transform.Find("A4DocumentUI/A4DocumentPrompt");
                var messageSource = lawyerCanvas.transform.Find("A4DocumentUI/A4DocumentMessage");
                if (promptSource == null || messageSource == null) throw new Exception("The Lawyer Room's A4DocumentPrompt / A4DocumentMessage were not found.");

                // ---- The two lines, copied from step 2's line (same font, size and colour as Lawyer Room / Evening) --------------------------
                var container = new GameObject("StationNoEntry", typeof(RectTransform));
                container.transform.SetParent(canvasRoot.transform, false);
                Stretch(container.GetComponent<RectTransform>());
                var promptObject = CloneInto(promptSource, container.transform, station, "SignPrompt");
                var messageObject = CloneInto(messageSource, container.transform, station, "SignMessage");
                var goBackObject = CloneInto(promptSource, container.transform, station, "GoBackPrompt");
                var promptLabel = SetText(promptObject, PromptLine);
                var messageLabel = SetText(messageObject, SignText);
                var goBackLabel = SetText(goBackObject, GoBackLine);
                promptObject.SetActive(false); messageObject.SetActive(false); goBackObject.SetActive(false);
                foreach (var clone in new[] { promptObject, messageObject, goBackObject })
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
                var lines = new GameObject("Lines", typeof(RectTransform));
                lines.transform.SetParent(container.transform, false);
                Stretch(lines.GetComponent<RectTransform>());
                var first = CloneInto(lineSource, lines.transform, station, "01_KhemSeesTheTvIsOn");
                var second = CloneInto(lineSource, lines.transform, station, "02_KhemGoesBackToTheScene");
                var labelA = SetText(first, LineA);
                var labelB = SetText(second, LineB);
                first.SetActive(false); second.SetActive(false);

                // ---- The moment ---------------------------------------------------------------------------------------------------------------
                var momentObject = new GameObject("NoEntryTvMoment");
                momentObject.transform.SetParent(gameplay, false);
                var moment = momentObject.AddComponent<PoliceStationNoEntryTv>();
                moment.opening = opening;
                moment.duty = duty;
                moment.player = player;
                moment.signLocation = sign;
                moment.tv = tv;
                moment.endLocation = end;
                moment.blackScreen = opening.arrival != null ? opening.arrival.blackScreen : null;
                if (moment.blackScreen == null) log.AppendLine("WARNING: the arrival black screen was not found; E at the end will not fade to black.");
                moment.khemDialogue = opening.khemDialogue;
                var source = opening.presentation;
                moment.presentation = new FlowingDialogueSettings
                {
                    screenAnchor = source.screenAnchor, widthFraction = source.widthFraction,
                    secondsPerCharacter = source.secondsPerCharacter, completedLineHold = source.completedLineHold, scrollSpeed = source.scrollSpeed,
                    typingAudio = source.typingAudio,
                    sentenceColor = source.sentenceColor, khemColor = source.khemColor, otherSpeakerColor = source.otherSpeakerColor,
                };
                moment.khemLines = new[] { labelA };
                moment.khemLinesAfterZoom = new[] { labelB };
                moment.goBackPrompt = goBackLabel;
                moment.prompt = promptLabel;
                moment.signMessage = messageLabel;
                EditorUtility.SetDirty(moment);

                // ---- Facts for the report --------------------------------------------------------------------------------------------------------
                Physics.SyncTransforms();
                var renderers = tv.GetComponentsInChildren<Renderer>();
                Bounds tvBounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(tv.position, Vector3.one);
                foreach (var renderer in renderers) tvBounds.Encapsulate(renderer.bounds);
                log.AppendLine("Sign location: " + sign.position.ToString("F3") + " rot " + sign.eulerAngles.ToString("F0") + " (parent " + (sign.parent != null ? sign.parent.name : "scene root") + ")");
                log.AppendLine("TV " + tv.name + ": pivot " + tv.position.ToString("F3") + ", bounds centre " + tvBounds.center.ToString("F2") + " size " + tvBounds.size.ToString("F2"));
                Vector3 eye = new Vector3(sign.position.x, 1.7f, sign.position.z + moment.interactDistance * .8f);
                log.AppendLine("From an eye point " + eye.ToString("F2") + " (in front of the sign) the TV is " + Vector3.Distance(eye, tvBounds.center).ToString("F1") + " m away.");
                foreach (var hit in Physics.RaycastAll(eye, (tvBounds.center - eye).normalized, Vector3.Distance(eye, tvBounds.center), ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                    log.AppendLine("  line of sight crosses collider " + hit.collider.name + " at " + hit.distance.ToString("F1") + " m");
                var endRenderers = end.GetComponentsInChildren<Renderer>(true);
                Bounds endBounds = endRenderers.Length > 0 ? endRenderers[0].bounds : new Bounds(end.position, Vector3.one);
                foreach (var renderer in endRenderers) endBounds.Encapsulate(renderer.bounds);
                log.AppendLine("end object: " + end.position.ToString("F3") + " (parent " + (end.parent != null ? end.parent.name : "scene root") + "), "
                    + endRenderers.Length + " renderers, bounds centre " + endBounds.center.ToString("F2") + " size " + endBounds.size.ToString("F2")
                    + (endRenderers.Length == 0 ? "  WARNING: nothing to look at, the E prompt can never appear" : ""));
                log.AppendLine("E prompt within " + moment.interactDistance + " m looking at the sign; sign text shown " + moment.messageSeconds + " s; zoom field of view " + moment.zoomFieldOfView + "; lines: 1 while zoomed + 1 after the zoom; at the end object (within " + moment.endInteractDistance + " m) the prompt \"" + GoBackLine + "\", then the screen fades to black over " + moment.fadeSeconds + " s using " + (moment.blackScreen != null ? moment.blackScreen.name : "(nothing)"));

                EditorSceneManager.MarkSceneDirty(station);
                if (!EditorSceneManager.SaveScene(station)) throw new Exception("Station save failed.");
                File.WriteAllText(Result, "PASS\nSaved SeatingCanvas/StationNoEntry and StationGameplay/NoEntryTvMoment.\n" + log);
            }
            catch (Exception error) { File.WriteAllText(Result, "FAIL\n" + error + "\n" + log); Debug.LogException(error); }
            finally
            {
                if (!lawyerWasLoaded && lawyer.IsValid() && lawyer.isLoaded) EditorSceneManager.CloseScene(lawyer, true);
                if (!stationWasLoaded && station.IsValid() && station.isLoaded) EditorSceneManager.CloseScene(station, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
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
