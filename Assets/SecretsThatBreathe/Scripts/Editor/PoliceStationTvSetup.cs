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
    // One-time scene setup for the TV extra in Inside_Police_Station (E at Tv (5): the TV turns on, a part of the song plays, Khem speaks).
    // Nothing here runs at game time. Drop Tools/PoliceStationTv/install.request in the project to run it (add reinstall.request to rebuild).
    [InitializeOnLoad]
    public static class PoliceStationTvSetup
    {
        const string Folder = "Tools/PoliceStationTv";
        const string Request = Folder + "/install.request";
        const string Reinstall = Folder + "/reinstall.request";
        const string Result = Folder + "/install.result.txt";
        const string StationPath = "Assets/MainScenes/From_PM/Inside_Police_Station.unity";
        const string LawyerRoomPath = "Assets/MainScenes/From_PM/Lawyer Room.unity";
        const string TvName = "Tv (5)";
        const string ScreenOnName = "TV_Screen_on";
        const string ScreenOffName = "TV_Screen_off";
        const string SongPath = "Assets/Bua Loy_B Minor_146.mp3";
        const string PromptLine = "[E] เปิดทีวี";
        const string KhemLine = "จริงครับพี่ คนแม่งเอาแต่ประโยชน์ส่วนตนขนาดตำรวจดีๆ ยังหายากเลยสมัยนี้";

        static PoliceStationTvSetup() => EditorApplication.update += Install;

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
                if (station.isDirty)
                    log.AppendLine("The scene had unsaved changes (for example the TV screens added under " + TvName + "). They are saved with this setup.");
                var gameplay = all.FirstOrDefault(t => t.name == "StationGameplay");
                if (gameplay == null) throw new Exception("StationGameplay (step 1) was not found.");
                var existing = gameplay.Find("TvInteraction");
                if (existing != null && !reinstall)
                { File.WriteAllText(Result, "SKIPPED: TvInteraction already exists in the scene."); return; }

                var tvs = all.Where(t => t.name == TvName).ToArray();
                if (tvs.Length == 0) throw new Exception("The TV named " + TvName + " was not found.");
                if (tvs.Length > 1) log.AppendLine("WARNING: " + tvs.Length + " objects are named " + TvName + "; using the first at the scene root.");
                var tv = tvs.FirstOrDefault(t => t.parent == null);
                if (tv == null) tv = tvs[0];
                var inside = tv.GetComponentsInChildren<Transform>(true);
                var screenOn = inside.FirstOrDefault(t => t.name == ScreenOnName);
                if (screenOn == null) throw new Exception("The hidden screen named " + ScreenOnName + " was not found under " + TvName + ".");
                var screenOff = inside.FirstOrDefault(t => t.name == ScreenOffName);
                if (screenOff == null) log.AppendLine("WARNING: no " + ScreenOffName + " under " + TvName + "; only " + ScreenOnName + " is switched on.");
                var song = AssetDatabase.LoadAssetAtPath<AudioClip>(SongPath);
                if (song == null) throw new Exception("The song " + SongPath + " was not found.");
                if (song.length < 200f) throw new Exception("The song is only " + song.length.ToString("F1") + " s long, so 3:10-3:20 does not exist.");

                // Backup of the scene exactly as it is on disk now.
                string backup = Folder + "/before-tv.unity";
                if (!File.Exists(backup)) File.Copy(StationPath, backup, false);

                var canvasRoot = station.GetRootGameObjects().First(g => g.name == "SeatingCanvas");
                var player = all.Select(t => t.GetComponent<FirstPersonController>()).First(p => p != null);
                var opening = gameplay.GetComponent<PoliceStationOpening>();
                if (opening == null) throw new Exception("Step 1 (PoliceStationOpening) was not found.");
                var lineSource = canvasRoot.transform.Find("StationOfficer/Lines/01_KhemAsksForCctvFiles");
                if (lineSource == null) throw new Exception("SeatingCanvas/StationOfficer/Lines/01_KhemAsksForCctvFiles (step 2) was not found.");

                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                var oldUi = canvasRoot.transform.Find("StationTv");
                if (oldUi != null) UnityEngine.Object.DestroyImmediate(oldUi.gameObject);

                // ---- The prompt: the same panel the Lawyer Room uses for its '[E] ...' ---------------------------------------------------------
                if (!lawyerWasLoaded) lawyer = EditorSceneManager.OpenScene(LawyerRoomPath, OpenSceneMode.Additive);
                var lawyerCanvas = lawyer.GetRootGameObjects().FirstOrDefault(g => g.name == "SeatingCanvas");
                if (lawyerCanvas == null) throw new Exception("SeatingCanvas was not found in the Lawyer Room.");
                var promptSource = lawyerCanvas.transform.Find("A4DocumentUI/A4DocumentPrompt");
                if (promptSource == null) throw new Exception("The Lawyer Room's A4DocumentPrompt was not found.");

                var container = new GameObject("StationTv", typeof(RectTransform));
                container.transform.SetParent(canvasRoot.transform, false);
                Stretch(container.GetComponent<RectTransform>());
                var promptObject = CloneInto(promptSource, container.transform, station, "TvPrompt");
                var promptLabel = SetText(promptObject, PromptLine);
                promptObject.SetActive(false);
                foreach (var component in promptObject.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;
                    var serialized = new SerializedObject(component);
                    var property = serialized.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is Component target
                            && target.gameObject.scene.IsValid() && target.gameObject.scene != station)
                            log.AppendLine("WARNING: " + component.name + "." + property.propertyPath + " points into scene " + target.gameObject.scene.name);
                }
                // ---- Khem's line, copied from step 2's line (same font, size and colour as Lawyer Room / Evening) ---------------------------
                var lines = new GameObject("Lines", typeof(RectTransform));
                lines.transform.SetParent(container.transform, false);
                Stretch(lines.GetComponent<RectTransform>());
                var lineObject = CloneInto(lineSource, lines.transform, station, "01_KhemAgreesAboutPolice");
                var lineLabel = SetText(lineObject, KhemLine);
                lineObject.SetActive(false);

                // ---- The TV's position, for the song ---------------------------------------------------------------------------------------------
                var renderers = tv.GetComponentsInChildren<Renderer>(true);
                Bounds tvBounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(tv.position, Vector3.one);
                foreach (var renderer in renderers) tvBounds.Encapsulate(renderer.bounds);

                // ---- The moment ------------------------------------------------------------------------------------------------------------------
                var momentObject = new GameObject("TvInteraction");
                momentObject.transform.SetParent(gameplay, false);
                var moment = momentObject.AddComponent<PoliceStationTvInteraction>();
                moment.opening = opening;
                moment.player = player;
                moment.tv = tv;
                moment.screenOn = screenOn.gameObject;
                moment.screenOff = screenOff != null ? screenOff.gameObject : null;
                moment.prompt = promptLabel;
                moment.khemDialogue = opening.khemDialogue;
                var source = opening.presentation;
                moment.presentation = new FlowingDialogueSettings
                {
                    screenAnchor = source.screenAnchor, widthFraction = source.widthFraction,
                    secondsPerCharacter = source.secondsPerCharacter, completedLineHold = source.completedLineHold, scrollSpeed = source.scrollSpeed,
                    typingAudio = source.typingAudio,
                    sentenceColor = source.sentenceColor, khemColor = source.khemColor, otherSpeakerColor = source.otherSpeakerColor,
                };
                moment.khemLines = new[] { lineLabel };

                var musicObject = new GameObject("TvMusic");
                musicObject.transform.SetParent(momentObject.transform, false);
                musicObject.transform.position = tvBounds.center;
                var audio = musicObject.AddComponent<AudioSource>();
                audio.clip = song;
                audio.playOnAwake = false;
                audio.loop = false;
                audio.volume = moment.songVolume;
                audio.spatialBlend = 1f;
                audio.dopplerLevel = 0f;
                audio.rolloffMode = AudioRolloffMode.Linear;
                audio.minDistance = 2f;
                audio.maxDistance = 25f;
                moment.music = audio;
                EditorUtility.SetDirty(moment);

                // ---- Facts for the report --------------------------------------------------------------------------------------------------------
                log.AppendLine("TV " + tv.name + ": pivot " + tv.position.ToString("F3") + ", bounds centre " + tvBounds.center.ToString("F2") + " size " + tvBounds.size.ToString("F2")
                    + ", " + renderers.Length + " renderers (parent " + (tv.parent != null ? tv.parent.name : "scene root") + ")");
                log.AppendLine(ScreenOnName + " is " + (screenOn.gameObject.activeSelf ? "ON" : "hidden (as expected)") + " in the scene; "
                    + (screenOff != null ? ScreenOffName + " is " + (screenOff.gameObject.activeSelf ? "shown (as expected)" : "hidden") : "no off screen"));
                log.AppendLine("Song " + song.name + " is " + song.length.ToString("F2") + " s long (" + (int)(song.length / 60) + ":" + ((int)song.length % 60).ToString("00") + "); plays "
                    + moment.songStartSeconds + " s to " + moment.songEndSeconds + " s with a " + moment.fadeOutSeconds + " s fade-out; AudioSource at the TV, 3D, volume " + audio.volume);
                log.AppendLine("E prompt within " + moment.interactDistance + " m looking at the TV; one line.");

                EditorSceneManager.MarkSceneDirty(station);
                if (!EditorSceneManager.SaveScene(station)) throw new Exception("Station save failed.");
                File.WriteAllText(Result, "PASS\nSaved SeatingCanvas/StationTv and StationGameplay/TvInteraction.\n" + log);
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
