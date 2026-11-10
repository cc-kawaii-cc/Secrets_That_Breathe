#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using StarterAssets;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // MainScene_2_MidNight gameplay, set up one step at a time. Each step runs once by itself after the scripts compile;
    // menu: Tools > Midnight Scene. Nothing here runs at game time.
    //   Step 1: what Khem thinks while driving back to the crash site (SeatingCanvas/DriveThought1, DriveThought2); no "arrived" line.
    //   Step 3: close to the braking skid mark Khem thinks "นี่รอยรถนี่ ต้องถ่ายไว้"; then E -> he raises his phone sideways and
    //           photographs it (MidnightSkidPhoto). While its prompt is up, the banana "[E] กินกล้วย" prompt stays hidden.
    //   Step 4: after the photo a light rain slowly starts, the ground turns wet, and Khem thinks "ฝนตกแล้ว ต้องรีบแล้วสิ" (MidnightRain).
    //   Step 5: the blood stains fade in the rain; Khem photographs RoadBlood_Smear, _Drops, _Pool in that order (MidnightBloodPhoto_*),
    //           then is walked to CarDebris_B_CarbonLip and thinks four tired lines (MidnightGhostSequence). The debris is hidden until later.
    //   Steps 6-8: heavy blink + blurry view, the ghost student (Characters/GhostStudent) shoves Khem aside, a ghost car (CarDE02.3)
    //           rushes through where he stood. Also sets the photo shutter sound to freesound_community-iphone-camera-capture-6448.
    //   Step 9: "เชี่ยอะไรวะนั่น", "กูต้องตาฝาดไปแน่", the debris appears; the fender shard and the carbon lip are photographed then picked up (E), nearest first
    //           (MidnightDebrisPhoto_*: MidnightSkidPhoto + MidnightDebrisPickup); then the two last lines. The ghost car gets its
    //           original shape and colours, just see-through (M_GhostCar).
    //   Ending (10): after the last lines, "กลับไปที่รถ ..." -> E at the car boards it -> on black the closing lines type in ->
    //           the key art with "Coming soon on Steam" (MidnightEnding, MidnightEndingCanvas).
    [InitializeOnLoad]
    public static class MidnightGameplaySetup
    {
        const string ScenePath = "Assets/MainScenes/From_PM/MainScene_2_MidNight.unity";
        const string Tag = "[Midnight] ";
        const string Step1Key = "MidnightGameplay.Step1.AutoInstall.v1";
        const string Step3Key = "MidnightGameplay.Step3.AutoInstall.v1";
        const string Step3bKey = "MidnightGameplay.Step3b.AutoInstall.v1";
        const string Step4Key = "MidnightGameplay.Step4.AutoInstall.v1";
        const string Step5Key = "MidnightGameplay.Step5.AutoInstall.v1";
        const string Step4bKey = "MidnightGameplay.Step4b.AutoInstall.v1";
        const string Step68Key = "MidnightGameplay.Step68.AutoInstall.v1";
        const string Step9Key = "MidnightGameplay.Step9.AutoInstall.v1";

        // ---- step 9 -------------------------------------------------------------------------------------------------------------------
        const string DebrisRootPrefix = "MidnightDebrisPhoto_";
        const string DebrisUiName = "MidnightDebrisUI";
        const string DebrisZoneName = "Debris Interaction Zone";
        const float DebrisZoneMargin = 1.3f;
        const string DebrisPromptText = "[E] ถ่ายรูปเศษชิ้นส่วนรถ";
        const string PickupPromptText = "[E] เก็บชิ้นส่วน";
        static readonly string[] ShockLines = { "เชี่ยอะไรวะนั่น", "กูต้องตาฝาดไปแน่" };
        static readonly string[] FoundLines = { "ดูเหมือนเป็นของที่สั่งทำพิเศษ", "พอมีเพื่อนทำอู่อยู่ เราเดี๋ยวลองเอาไปให้มันดูหน่อยดีกว่า" };
        const string OldFoundLine = "เหมือนเป็นที่เหมือนจะสั่งทำพิเศษ";   // replaced by FoundLines[0]
        const string Step9bKey = "MidnightGameplay.Step9b.AutoInstall.v1";

        // ---- steps 6-8 ----------------------------------------------------------------------------------------------------------------
        const string GhostFolder = "Assets/SecretsThatBreathe/Characters/GhostStudent";
        const string GhostFbx = GhostFolder + "/GhostStudent.fbx";
        const string GhostTexture = GhostFolder + "/Textures/GhostStudent_BaseColor.png";
        const string GhostPrefabPath = GhostFolder + "/GhostStudent.prefab";
        const string GhostControllerPath = GhostFolder + "/GhostStudent.controller";
        const string GhostFx = "Assets/SecretsThatBreathe/Environment/Ghost";
        const string GhostShaderName = "SecretsThatBreathe/Ghost";
        const string GhostClipName = "Ghost_Push";
        const string EyelidsName = "MidnightEyelids";
        const string GhostCarAsset = "CarDE02.3";
        const string ShutterClipName = "freesound_community-iphone-camera-capture-6448";

        // ---- step 5 -------------------------------------------------------------------------------------------------------------------
        static readonly string[] BloodNames = { "RoadBlood_Smear", "RoadBlood_Drops", "RoadBlood_Pool" };   // photographed in this order
        const string BloodRootPrefix = "MidnightBloodPhoto_";
        const string BloodUiName = "MidnightBloodUI";
        const string BloodZoneName = "Blood Photo Interaction Zone";
        const string BloodPromptText = "[E] ถ่ายรูปรอยเลือด";
        const string BloodThoughtText = "รอยเลือด ต้องรีบถ่าย\nฝนยิ่งตกอยู่ เดี๋ยวรอยจางหมด";
        const float BloodZoneMargin = 1.1f;
        const string GhostRootName = "MidnightGhostSequence";
        const string GhostUiName = "MidnightGhostUI";
        static readonly string[] TiredLines = { "ฝนตกหนักมากแล้ว", "ฉันควรกลับบ้าน", "เริ่มตาลายแล้วสิ", "นี่ไม่ได้นอนมาจะ 24 ชม." };
        static readonly string[] DebrisNames = { "CarDebris_A_FenderShard", "CarDebris_B_CarbonLip", "CarDebris_C_TailLightCluster" };
        // Of the debris that appears, only these are photographed and picked up (the tail light stays on the road).
        static readonly string[] PickupNames = { "CarDebris_A_FenderShard", "CarDebris_B_CarbonLip" };
        const string Fixes2Key = "MidnightGameplay.Fixes2.AutoInstall.v1";

        // ---- the ending -------------------------------------------------------------------------------------------------------------
        const string EndingKey = "MidnightGameplay.Ending.AutoInstall.v1";
        const string Fixes3Key = "MidnightGameplay.Fixes3.AutoInstall.v1";
        const string Fixes4Key = "MidnightGameplay.Fixes4.AutoInstall.v1";
        const string BottomShadePath = "Assets/SecretsThatBreathe/Environment/Ending/Textures/Ending_BottomShade.png";
        // The key art fills the screen (cropping top and bottom); this point of it (0 = bottom, 1 = top) stays where it is on screen.
        const float KeyArtFocus = .6f;
        const string EndingRootName = "MidnightEnding";
        const string EndingUiName = "MidnightEndingUI";
        const string EndingCanvasName = "MidnightEndingCanvas";
        const string KeyArtPath = "Assets/SecretsThatBreathe/Environment/Ending/Textures/Ending_KeyArt.png";
        const string GarageObjectiveText = "\"กลับไปที่รถ เพื่อไปที่อู่\"";
        const string BoardGaragePromptText = "[E] ขึ้นรถไปที่อู่";
        static readonly string[] EndingLines = { "Thank You for Playing", "ติดตามได้ที่ Secrets That Breathe" };
        const string ComingSoonText = "Wishlist now on Steam";
        static readonly string[] OldEndingTexts = { "เข้มจะสามารถช่วยลุงได้หรือมั้ย", "Coming soon on Steam" };
        const string EndingTexturesFolder = "Assets/SecretsThatBreathe/Environment/Ending/Textures";
        const string Fixes5Key = "MidnightGameplay.Fixes5.AutoInstall.v1";
        const float SteamLogoHeight = 80f;
        const string KeyArtBlurPath = "Assets/SecretsThatBreathe/Environment/Ending/Textures/Ending_KeyArtBlur.jpg";
        const string Fixes6Key = "MidnightGameplay.Fixes6.AutoInstall.v1";

        // ---- step 4 -------------------------------------------------------------------------------------------------------------------
        const string RainRootName = "MidnightRain";
        const string RainUiName = "MidnightRainUI";
        const string RainThoughtName = "RainThought";
        const string RainThoughtText = "ฝนเริ่มตกหนักแล้วสิ";
        const float RainThoughtDelay = .7f;   // seconds after the skid photo (phone lowered)
        const string Fixes1Key = "MidnightGameplay.Fixes1.AutoInstall.v1";
        const string RainFolder = "Assets/SecretsThatBreathe/Environment/Rain";
        const string RainShaderName = "SecretsThatBreathe/Rain Particle";

        // ---- step 1 -------------------------------------------------------------------------------------------------------------------
        static readonly string[] DriveLines =
        {
            "แถวนี้ตอนมืดแล้ว น่ากลัวจัง",
            "จะเจออะไรแปลกๆมั้ยเนี้ย",
        };
        // The first drive line copied over from the evening scene. While it is still in the midnight scene, step 1 has not been done.
        const string EveningFirstLine = "สงสารลุง";

        // ---- step 3 -------------------------------------------------------------------------------------------------------------------
        const string SkidName = "CarDebris_SkidMark_Braking";
        const string PhotoRootName = "MidnightSkidPhoto";
        const string PhotoUiName = "MidnightSkidPhotoUI";
        const string ZoneName = "Skid Photo Interaction Zone";
        const string PhonePrefabPath = "Assets/SecretsThatBreathe/Props/Phone/Prefabs/Phone_Blank.prefab";
        const string FeedMaterialPath = "Assets/SecretsThatBreathe/Props/Phone/Materials/Screen_CameraFeed.mat";
        const string RaiseSoundPath = "Assets/SecretsThatBreathe/Audio/Phone/Phone_PickUp.wav";
        const string ShutterSoundPath = "Assets/SecretsThatBreathe/Audio/Phone/Phone_CameraShutter.wav";
        const string PromptText = "[E] ถ่ายรูปรอยล้อ";
        const string ShutterText = "[E] ถ่ายรูป";
        const string SavedText = "บันทึกรูปแล้ว";
        const string ThoughtName = "SkidThought";
        const string ThoughtText = "นี่รอยรถนี่ ต้องถ่ายไว้";
        // Interaction zone around the skid mark, in metres: this much floor around it, and this tall.
        const float ZoneMargin = 1.6f, ZoneHeight = 3f;

        static MidnightGameplaySetup()
        {
            EditorApplication.delayCall += AutoInstall;
            EditorApplication.playModeStateChanged += state =>
            { if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += AutoInstall; };
        }

        [MenuItem("Tools/Midnight Scene/1) Set Drive Lines")]
        static void MenuDriveLines() => Run("Step 1", (scene, all, log) => { SetDriveLines(all, log); RemoveArrivalLine(all, log); });

        [MenuItem("Tools/Midnight Scene/3) Add Skid Mark Photo")]
        static void MenuSkidPhoto() => Run("Step 3", (scene, all, log) => { RemoveArrivalLine(all, log); AddSkidPhoto(scene, all, log, false); });

        [MenuItem("Tools/Midnight Scene/3) Update Skid Mark Photo (thought line)")]
        static void MenuSkidPhotoUpdate() => Run("Step 3 update", (scene, all, log) => UpdateSkidPhoto(scene, all, log));

        [MenuItem("Tools/Midnight Scene/4) Add Rain After Photo")]
        static void MenuRain() => Run("Step 4", (scene, all, log) => AddRain(scene, all, log, false));

        [MenuItem("Tools/Midnight Scene/4) Update Rain (starts on the drive, lands on Terrain + car)")]
        static void MenuRainUpdate() => Run("Step 4 update", (scene, all, log) => UpdateRain(all, log));

        [MenuItem("Tools/Midnight Scene/4) Rebuild Rain After Photo")]
        static void MenuRainRebuild() => Run("Step 4", (scene, all, log) => AddRain(scene, all, log, true));

        [MenuItem("Tools/Midnight Scene/5) Add Blood Photos + Walk")]
        static void MenuBlood() => Run("Step 5", (scene, all, log) => AddBlood(scene, all, log, false));

        [MenuItem("Tools/Midnight Scene/5) Rebuild Blood Photos + Walk")]
        static void MenuBloodRebuild() => Run("Step 5", (scene, all, log) => AddBlood(scene, all, log, true));

        [MenuItem("Tools/Midnight Scene/6-8) Add Blink, Ghost, Ghost Car")]
        static void MenuGhost() => Run("Steps 6-8", (scene, all, log) => AddGhost(scene, all, log, false));

        [MenuItem("Tools/Midnight Scene/6-8) Rebuild Blink, Ghost, Ghost Car")]
        static void MenuGhostRebuild() => Run("Steps 6-8", (scene, all, log) => AddGhost(scene, all, log, true));

        [MenuItem("Tools/Midnight Scene/9) Add Debris Photos + Pickup")]
        static void MenuDebris() => Run("Step 9", (scene, all, log) => AddDebris(scene, all, log, false));

        [MenuItem("Tools/Midnight Scene/9) Rebuild Debris Photos + Pickup")]
        static void MenuDebrisRebuild() => Run("Step 9", (scene, all, log) => AddDebris(scene, all, log, true));

        [MenuItem("Tools/Midnight Scene/9) Update Found Line")]
        static void MenuFoundLine() => Run("Step 9 line", (scene, all, log) => UpdateFoundLine(all, log));

        [MenuItem("Tools/Midnight Scene/Fix: rain line 0.7 s after the skid photo + ghost car faces forward")]
        static void MenuFixes1() => Run("Fixes 1", (scene, all, log) => Fixes1(all, log));

        [MenuItem("Tools/Midnight Scene/10) Add Ending (car to the garage, closing lines, key art)")]
        static void MenuEnding() => Run("Ending", (scene, all, log) => AddEnding(scene, all, log, false));

        [MenuItem("Tools/Midnight Scene/10) Rebuild Ending")]
        static void MenuEndingRebuild() => Run("Ending", (scene, all, log) => AddEnding(scene, all, log, true));

        [MenuItem("Tools/Midnight Scene/Fix: ending uses the car Khem got out of")]
        static void MenuFixes3() => Run("Fixes 3", (scene, all, log) => Fixes3(all, log));

        [MenuItem("Tools/Midnight Scene/Fix: key art fills the screen + rain fades out after E at the car")]
        static void MenuFixes4() => Run("Fixes 4", (scene, all, log) => Fixes4(all, log));

        [MenuItem("Tools/Midnight Scene/Fix: Thank You for Playing + Wishlist now on Steam + Steam logo")]
        public static void MenuFixes5() => Run("Fixes 5", (scene, all, log) => Fixes5(all, log));

        [MenuItem("Tools/Midnight Scene/Fix: ending layout 2 (whole poster, blurred sides) + Steam logo")]
        static void MenuFixes6() => Run("Fixes 6", (scene, all, log) => Fixes6(all, log));

        [MenuItem("Tools/Midnight Scene/Set Photo Shutter Sound")]
        static void MenuShutter() => Run("Shutter sound", (scene, all, log) => SetShutter(all, log));

        [MenuItem("Tools/Midnight Scene/3) Rebuild Skid Mark Photo")]
        static void MenuSkidPhotoRebuild() => Run("Step 3", (scene, all, log) => { RemoveArrivalLine(all, log); AddSkidPhoto(scene, all, log, true); });

        static void AutoInstall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoInstall; return; }
            if (!File.Exists(ScenePath)) return;
            string file = File.ReadAllText(ScenePath);
            if (!SessionState.GetBool(Step1Key, false))
            {
                SessionState.SetBool(Step1Key, true);
                if (file.Contains(YamlEscape(EveningFirstLine)) || file.Contains(EveningFirstLine))
                { MenuDriveLines(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Step3Key, false))
            {
                SessionState.SetBool(Step3Key, true);
                if (!file.Contains(PhotoRootName)) { MenuSkidPhoto(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Step3bKey, false))
            {
                SessionState.SetBool(Step3bKey, true);
                if (file.Contains(PhotoRootName) && !file.Contains(ThoughtName)) { MenuSkidPhotoUpdate(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Step4Key, false))
            {
                SessionState.SetBool(Step4Key, true);
                // the scene may hold the photo setup only in memory (not saved yet); AddRain checks the loaded scene itself
                if (!file.Contains(RainRootName)) { MenuRain(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Step4bKey, false))
            {
                SessionState.SetBool(Step4bKey, true);
                // the new rain settings are written into the scene the first time it is saved with them
                if (file.Contains(RainRootName) && !file.Contains("heaviestDropsPerSecond")) { MenuRainUpdate(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Step5Key, false))
            {
                SessionState.SetBool(Step5Key, true);
                if (!file.Contains(BloodRootPrefix)) { MenuBlood(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Step68Key, false))
            {
                SessionState.SetBool(Step68Key, true);
                if (file.Contains(GhostRootName) && !file.Contains(EyelidsName)) { MenuGhost(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Step9Key, false))
            {
                SessionState.SetBool(Step9Key, true);
                // steps 6-8 may be in the open scene only (not saved yet); AddDebris checks the loaded scene itself
                if (file.Contains(GhostRootName) && !file.Contains(DebrisUiName)) { MenuDebris(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Step9bKey, false))
            {
                SessionState.SetBool(Step9bKey, true);
                if (HasOldFoundLine(file)) { MenuFoundLine(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(EndingKey, false))
            {
                SessionState.SetBool(EndingKey, true);
                if (SceneHasRoot(file, DebrisRootPrefix) && !SceneHasRoot(file, EndingRootName)) { MenuEnding(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Fixes6Key, false))
            {
                SessionState.SetBool(Fixes6Key, true);
                if (SceneHasRoot(file, EndingRootName)) { MenuFixes6(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Fixes5Key, false))
            {
                SessionState.SetBool(Fixes5Key, true);
                if (SceneHasRoot(file, EndingRootName)) { MenuFixes5(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Fixes4Key, false))
            {
                SessionState.SetBool(Fixes4Key, true);
                if (SceneHasRoot(file, EndingRootName)) { MenuFixes4(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Fixes3Key, false))
            {
                SessionState.SetBool(Fixes3Key, true);
                if (SceneHasRoot(file, EndingRootName)) { MenuFixes3(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Fixes2Key, false))
            {
                SessionState.SetBool(Fixes2Key, true);
                // step 9 was built with three pieces to pick up; now only the fender shard and the carbon lip
                if (SceneHasRoot(file, DebrisRootPrefix + "3_")) { MenuDebrisRebuild(); file = File.ReadAllText(ScenePath); }
            }
            if (!SessionState.GetBool(Fixes1Key, false))
            {
                SessionState.SetBool(Fixes1Key, true);
                // the scene may hold the rain only in memory (not saved yet); Fixes1 works on the open scene itself
                if (file.Contains(RainRootName) && !file.Contains(YamlEscape(RainThoughtText)) && !file.Contains(RainThoughtText)) MenuFixes1();
            }
        }

        // Opens MainScene_2_MidNight (if it is not open), applies the change and saves it, unless it already had unsaved changes.
        static void Run(string step, Action<Scene, Transform[], StringBuilder> change)
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
                change(scene, all, log);
                EditorSceneManager.MarkSceneDirty(scene);
                string saved;
                if (wasDirty) saved = "MainScene_2_MidNight had unsaved changes, so it was NOT saved - press Ctrl+S to keep this.";
                else if (EditorSceneManager.SaveScene(scene)) saved = "Saved " + ScenePath + ".";
                else throw new Exception("Saving MainScene_2_MidNight failed.");
                Debug.Log(Tag + step + " PASS. " + saved + "\n" + log);
            }
            catch (Exception error) { Debug.LogError(Tag + step + " FAIL " + error.Message + "\n" + log + "\n" + error); }
            finally
            {
                if (!wasLoaded && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }

        // ---- step 1 -------------------------------------------------------------------------------------------------------------------
        static EveningDriveThoughts Thoughts(Transform[] all)
        {
            var thoughts = all.Select(t => t.GetComponent<EveningDriveThoughts>()).FirstOrDefault(c => c != null);
            if (thoughts == null) throw new Exception("EveningDriveThoughts was not found in MainScene_2_MidNight.");
            return thoughts;
        }

        static void SetDriveLines(Transform[] all, StringBuilder log)
        {
            var thoughts = Thoughts(all);
            var labels = new TMP_Text[DriveLines.Length];
            for (int i = 0; i < DriveLines.Length; i++)
            {
                string name = "DriveThought" + (i + 1);
                var holder = all.FirstOrDefault(t => t.name == name);
                if (holder == null) throw new Exception(name + " was not found under SeatingCanvas.");
                var label = holder.GetComponentInChildren<TMP_Text>(true);
                if (label == null) throw new Exception(name + " has no text label.");
                Undo.RecordObject(label, "Midnight drive lines");
                label.text = DriveLines[i];
                EditorUtility.SetDirty(label);
                labels[i] = label;
                log.AppendLine(name + ": " + DriveLines[i]);
            }
            Undo.RecordObject(thoughts, "Midnight drive lines");
            thoughts.drivingThoughts = labels;   // the third evening line is no longer said
            EditorUtility.SetDirty(thoughts);
            log.AppendLine("Driving lines: " + labels.Length + ".");
        }

        // The user asked for no "ถึงแล้ว อยู่ข้างหน้านี่ไง" line on arrival (DriveThought4 stays in the scene, unused).
        static void RemoveArrivalLine(Transform[] all, StringBuilder log)
        {
            var thoughts = Thoughts(all);
            if (thoughts.arrivalThought == null) return;
            Undo.RecordObject(thoughts, "Midnight arrival line");
            log.AppendLine("Removed the arrival line: " + thoughts.arrivalThought.text);
            thoughts.arrivalThought = null;
            EditorUtility.SetDirty(thoughts);
        }

        // ---- step 3 -------------------------------------------------------------------------------------------------------------------
        static void AddSkidPhoto(Scene scene, Transform[] all, StringBuilder log, bool rebuild)
        {
            var existing = all.Where(t => t.parent == null && t.name == PhotoRootName).ToList();
            if (existing.Count > 0 && !rebuild)
            { log.AppendLine("SKIPPED: " + PhotoRootName + " is already in the scene (Tools > Midnight Scene > 3) Rebuild Skid Mark Photo to redo it)."); return; }

            var player = all.Select(t => t.GetComponent<FirstPersonController>()).FirstOrDefault(c => c != null);
            if (player == null) throw new Exception("The player (FirstPersonController) was not found.");
            var skids = all.Where(t => t.name == SkidName && (t.parent == null || t.parent.name != SkidName)).ToList();
            if (skids.Count == 0) throw new Exception(SkidName + " was not found (it is part of the evidence prefab).");
            var skid = skids[0];
            if (skids.Count > 1) log.AppendLine("Note: " + skids.Count + " objects are called " + SkidName + "; using the one at " + skid.position.ToString("F2"));
            var canvas = all.FirstOrDefault(t => t.parent == null && t.name == "SeatingCanvas");
            var template = all.FirstOrDefault(t => t.name == "ExitPrompt" && t.GetComponent<TMP_Text>() != null);
            if (canvas == null || template == null) throw new Exception("SeatingCanvas/CarExitUI/ExitPrompt (the '[F]' prompt used as the label style) was not found.");

            var phonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PhonePrefabPath);
            if (phonePrefab == null) throw new Exception("Phone_Blank.prefab was not found at " + PhonePrefabPath);
            var feedMaterial = FeedMaterial(log);
            var raiseSound = AssetDatabase.LoadAssetAtPath<AudioClip>(RaiseSoundPath);
            var shutterSound = AssetDatabase.LoadAssetAtPath<AudioClip>(ShutterSoundPath);
            if (raiseSound == null) log.AppendLine("WARNING: " + RaiseSoundPath + " not found (no sound when raising the phone).");
            if (shutterSound == null) log.AppendLine("WARNING: " + ShutterSoundPath + " not found (no shutter sound).");

            // keep settings tuned in the Inspector when rebuilding
            MidnightSkidPhoto old = existing.Select(t => t.GetComponent<MidnightSkidPhoto>()).FirstOrDefault(c => c != null);
            string oldJson = old != null ? EditorJsonUtility.ToJson(old) : null;
            foreach (var t in existing) UnityEngine.Object.DestroyImmediate(t.gameObject);
            var oldUi = canvas.Find(PhotoUiName);
            if (oldUi != null) UnityEngine.Object.DestroyImmediate(oldUi.gameObject);

            // labels, in the same style as the '[F] ลงจากรถ' prompt
            var ui = new GameObject(PhotoUiName, typeof(RectTransform));
            ui.transform.SetParent(canvas, false);
            var rect = (RectTransform)ui.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var prompt = Label(template, ui.transform, scene, "SkidPrompt", PromptText);
            var shutter = Label(template, ui.transform, scene, "ShutterHint", ShutterText);
            var saved = Label(template, ui.transform, scene, "SavedLabel", SavedText);

            // the zone, like the CCTV Interaction Zone: a trigger box that is a child of the skid mark, so it follows it
            var zone = Zone(skid, log);

            var root = new GameObject(PhotoRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = skid.position;
            var photo = root.AddComponent<MidnightSkidPhoto>();
            if (oldJson != null) EditorJsonUtility.FromJsonOverwrite(oldJson, photo);
            photo.player = player;
            photo.skidMark = skid;
            photo.interactionZone = zone;
            photo.prompt = prompt;
            photo.shutterHint = shutter;
            photo.savedLabel = saved;
            photo.phonePrefab = phonePrefab;
            photo.feedMaterial = feedMaterial;
            if (raiseSound != null) photo.raiseSound = raiseSound;
            if (shutterSound != null) photo.shutterSound = shutterSound;
            EditorUtility.SetDirty(photo);
            EnsureThought(all, ui.transform, scene, photo, log);
            log.AppendLine("Skid mark: " + skid.name + " at " + skid.position.ToString("F2") + "; player: " + player.name);
            log.AppendLine("Labels: SeatingCanvas/" + PhotoUiName + "/{SkidPrompt, ShutterHint, SavedLabel}");
        }

        // ---- step 5 -------------------------------------------------------------------------------------------------------------------
        static Transform Named(Transform[] all, string name)
            => all.FirstOrDefault(t => t.name == name && (t.parent == null || t.parent.name != name));

        static void AddBlood(Scene scene, Transform[] all, StringBuilder log, bool rebuild)
        {
            var existing = all.Where(t => t.parent == null && (t.name.StartsWith(BloodRootPrefix) || t.name == GhostRootName)).ToList();
            if (existing.Count > 0 && !rebuild)
            { log.AppendLine("SKIPPED: the blood photos are already in the scene (Tools > Midnight Scene > 5) Rebuild Blood Photos + Walk to redo it)."); return; }
            var skidPhoto = all.Select(t => t.GetComponent<MidnightSkidPhoto>())
                               .FirstOrDefault(c => c != null && c.requires == null && c.skidMark != null && c.skidMark.name == SkidName);
            if (skidPhoto == null) throw new Exception("The skid mark photo (step 3) was not found.");
            var rain = all.Select(t => t.GetComponent<MidnightRain>()).FirstOrDefault(c => c != null);
            if (rain == null) throw new Exception("MidnightRain (step 4) was not found.");
            var canvas = all.FirstOrDefault(t => t.parent == null && t.name == "SeatingCanvas");
            var template = all.FirstOrDefault(t => t.name == "ExitPrompt" && t.GetComponent<TMP_Text>() != null);
            if (canvas == null || template == null) throw new Exception("SeatingCanvas/CarExitUI/ExitPrompt was not found.");
            var targets = BloodNames.Select(n => Named(all, n)).ToArray();
            for (int i = 0; i < targets.Length; i++) if (targets[i] == null) throw new Exception(BloodNames[i] + " was not found (evidence prefab).");
            var walkTarget = Named(all, "CarDebris_B_CarbonLip");
            if (walkTarget == null) throw new Exception("CarDebris_B_CarbonLip was not found.");

            // keep Inspector tuning when rebuilding
            var oldPhotos = existing.Select(t => t.GetComponent<MidnightSkidPhoto>()).Where(c => c != null).ToDictionary(c => c.gameObject.name, c => EditorJsonUtility.ToJson(c));
            var oldGhost = existing.Select(t => t.GetComponent<MidnightGhostSequence>()).FirstOrDefault(c => c != null);
            string oldGhostJson = oldGhost != null ? EditorJsonUtility.ToJson(oldGhost) : null;
            foreach (var t in existing) UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach (var name in new[] { BloodUiName, GhostUiName })
            { var old = canvas.Find(name); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject); }

            var bloodUi = Container(canvas, BloodUiName);
            var prompt = Label(template, bloodUi, scene, "BloodPrompt", BloodPromptText);
            var thought = ThoughtBox(all, bloodUi, scene, "BloodThought", BloodThoughtText);

            string skidJson = EditorJsonUtility.ToJson(skidPhoto);
            MidnightSkidPhoto previous = skidPhoto;
            for (int i = 0; i < targets.Length; i++)
            {
                var zone = Zone(targets[i], log, BloodZoneName, BloodZoneMargin);
                string rootName = BloodRootPrefix + (i + 1) + "_" + BloodNames[i].Replace("RoadBlood_", "");
                var root = new GameObject(rootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.position = targets[i].position;
                var photo = root.AddComponent<MidnightSkidPhoto>();
                EditorJsonUtility.FromJsonOverwrite(oldPhotos.TryGetValue(rootName, out var oldJson) ? oldJson : skidJson, photo);
                photo.skidMark = targets[i];
                photo.interactionZone = zone;
                photo.requires = previous;
                if (!oldPhotos.ContainsKey(rootName)) photo.availableDelay = i == 0 ? 4f : .4f;   // the first waits for "ฝนเริ่มตกหนักแล้วสิ" to finish
                photo.prompt = prompt;
                photo.shutterHint = skidPhoto.shutterHint;
                photo.savedLabel = skidPhoto.savedLabel;
                photo.thought = i == 0 ? thought : null;
                if (i == 0 && !oldPhotos.ContainsKey(rootName)) photo.thoughtSeconds = 3.5f;
                photo.player = skidPhoto.player;
                EditorUtility.SetDirty(photo);
                log.AppendLine((i + 1) + ") " + BloodNames[i] + " at " + targets[i].position.ToString("F2") + " (zone " + zone.size.ToString("F1") + " m)");
                previous = photo;
            }

            // stains fade while it rains
            Undo.RecordObject(rain, "Blood fades in the rain");
            rain.fadingMarks = all.Where(t => t.name.StartsWith("RoadBlood_")).SelectMany(t => t.GetComponents<Renderer>()).Distinct().ToArray();
            EditorUtility.SetDirty(rain);
            log.AppendLine("Stains that fade in the rain: " + rain.fadingMarks.Length);

            // walk + tired lines (the rest of the night continues from this object)
            var ghostUi = Container(canvas, GhostUiName);
            var lines = new TMP_Text[TiredLines.Length];
            for (int i = 0; i < TiredLines.Length; i++) lines[i] = ThoughtBox(all, ghostUi, scene, "Tired" + (i + 1), TiredLines[i]);
            var ghostRoot = new GameObject(GhostRootName);
            SceneManager.MoveGameObjectToScene(ghostRoot, scene);
            var ghost = ghostRoot.AddComponent<MidnightGhostSequence>();
            if (oldGhostJson != null) EditorJsonUtility.FromJsonOverwrite(oldGhostJson, ghost);
            ghost.afterPhoto = previous;
            ghost.rain = rain;
            ghost.player = skidPhoto.player;
            ghost.walkTarget = walkTarget;
            ghost.tiredLines = lines;
            ghost.hiddenDebris = DebrisNames.Select(n => Named(all, n)).Where(t => t != null).Select(t => t.gameObject).ToArray();
            EditorUtility.SetDirty(ghost);
            log.AppendLine("Walk to " + walkTarget.name + " at " + walkTarget.position.ToString("F2") + "; lines: SeatingCanvas/" + GhostUiName + "/Tired1-" + TiredLines.Length);
            log.AppendLine("Hidden until later: " + string.Join(", ", ghost.hiddenDebris.Select(g => g.name)));
        }

        // ---- steps 6-8 -------------------------------------------------------------------------------------------------------------------
        static void AddGhost(Scene scene, Transform[] all, StringBuilder log, bool rebuild)
        {
            var sequence = all.Select(t => t.GetComponent<MidnightGhostSequence>()).FirstOrDefault(c => c != null);
            if (sequence == null) throw new Exception("MidnightGhostSequence (step 5) was not found.");
            var canvas = all.FirstOrDefault(t => t.parent == null && t.name == "SeatingCanvas");
            if (canvas == null) throw new Exception("SeatingCanvas was not found.");
            var oldLids = canvas.Find(EyelidsName);
            if (!rebuild && sequence.ghost != null && oldLids != null)
            { log.AppendLine("SKIPPED: steps 6-8 are already in the scene (Tools > Midnight Scene > 6-8) Rebuild ... to redo them)."); SetShutter(all, log); return; }
            Undo.RecordObject(sequence, "Midnight ghost");
            if (sequence.ghost != null) UnityEngine.Object.DestroyImmediate(sequence.ghost);
            if (sequence.ghostCar != null) UnityEngine.Object.DestroyImmediate(sequence.ghostCar);
            if (oldLids != null) UnityEngine.Object.DestroyImmediate(oldLids.gameObject);

            // 6) eyelids: two black halves over everything on the canvas
            var lids = Container(canvas, EyelidsName);
            lids.SetAsLastSibling();
            sequence.eyelidTop = Lid(lids, "Top", true);
            sequence.eyelidBottom = Lid(lids, "Bottom", false);
            sequence.dizzySound = Clip(GhostFx + "/Audio/Khem_Dizzy.wav", log);

            // 8) the ghost student
            var prefab = GhostPrefab(log);
            var ghost = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            ghost.name = "GhostStudent";
            ghost.transform.SetParent(sequence.transform, false);
            ghost.SetActive(false);
            sequence.ghost = ghost;
            sequence.ghostClip = GhostClipName;
            sequence.jumpScareSound = Clip(GhostFx + "/Audio/Ghost_JumpScare.wav", log);
            sequence.pushSound = Clip(GhostFx + "/Audio/Ghost_Push.wav", log);

            // 8) the ghost car
            sequence.ghostMaterial = GhostCarMaterial(log);
            sequence.carSound = Clip(GhostFx + "/Audio/Ghost_CarPass.wav", log);
            var skidPhoto = all.Select(t => t.GetComponent<MidnightSkidPhoto>()).FirstOrDefault(c => c != null && c.skidMark != null && c.skidMark.name == SkidName);
            sequence.comesFrom = skidPhoto != null ? skidPhoto.skidMark : null;
            var carAsset = FindCar(log);
            if (carAsset != null)
            {
                // a holder at the car's floor centre, so the car sits on the road and turns about its middle whatever the model's pivot
                var holder = new GameObject("GhostCar");
                SceneManager.MoveGameObjectToScene(holder, scene);
                var car = (GameObject)PrefabUtility.InstantiatePrefab(carAsset, scene);
                car.name = GhostCarAsset;
                car.SetActive(true);
                car.transform.SetParent(holder.transform, false);
                car.transform.localPosition = Vector3.zero;   // keeps the model's own rotation
                bool any = false;
                var box = new Bounds();
                foreach (var renderer in car.GetComponentsInChildren<Renderer>(true))
                {
                    if (!any) { box = renderer.bounds; any = true; } else box.Encapsulate(renderer.bounds);
                }
                if (any) car.transform.localPosition = new Vector3(-box.center.x, -box.min.y, -box.center.z);
                holder.transform.SetParent(sequence.transform, false);
                holder.transform.localPosition = Vector3.zero;
                holder.transform.localRotation = Quaternion.identity;
                // which way is the car long? (CarDE02.3's front is its -Z side, so it is turned 180 degrees; longer along X: 90)
                sequence.carYawOffset = any && box.size.x > box.size.z * 1.2f ? 90f : 180f;
                holder.SetActive(false);
                sequence.ghostCar = holder;
                log.AppendLine("Ghost car: " + AssetDatabase.GetAssetPath(carAsset) + " (size " + box.size.ToString("F1") + " m, yaw offset " + sequence.carYawOffset + ")");
            }
            EditorUtility.SetDirty(sequence);
            log.AppendLine("Eyelids: SeatingCanvas/" + EyelidsName + "; ghost: " + GhostPrefabPath);
            SetShutter(all, log);
        }

        // ---- step 9 -------------------------------------------------------------------------------------------------------------------
        static void AddDebris(Scene scene, Transform[] all, StringBuilder log, bool rebuild)
        {
            var sequence = all.Select(t => t.GetComponent<MidnightGhostSequence>()).FirstOrDefault(c => c != null);
            if (sequence == null) throw new Exception("MidnightGhostSequence (step 5) was not found.");
            if (sequence.ghost == null && sequence.ghostCar == null) throw new Exception("Steps 6-8 are not in the scene yet (Tools > Midnight Scene > 6-8) Add ...).");
            var canvas = all.FirstOrDefault(t => t.parent == null && t.name == "SeatingCanvas");
            var template = all.FirstOrDefault(t => t.name == "ExitPrompt" && t.GetComponent<TMP_Text>() != null);
            if (canvas == null || template == null) throw new Exception("SeatingCanvas/CarExitUI/ExitPrompt was not found.");
            var skidPhoto = all.Select(t => t.GetComponent<MidnightSkidPhoto>())
                               .FirstOrDefault(c => c != null && c.requires == null && c.skidMark != null && c.skidMark.name == SkidName);
            if (skidPhoto == null) throw new Exception("The skid mark photo (step 3) was not found.");

            // the ghost car: its own shape and colours, just see-through
            var carMaterial = GhostCarMaterial(log);
            ClearCarLook(carMaterial, log);
            Undo.RecordObject(sequence, "Midnight debris");
            sequence.carLightIntensity = 1.2f;

            var existing = all.Where(t => t.parent == null && t.name.StartsWith(DebrisRootPrefix)).ToList();
            if (existing.Count > 0 && !rebuild)
            { log.AppendLine("SKIPPED: the debris photos are already in the scene (Tools > Midnight Scene > 9) Rebuild ... to redo them)."); EditorUtility.SetDirty(sequence); return; }

            // pieces to photograph and pick up, nearest first (from where Khem was walked to); the others only appear
            var shown = DebrisNames.Select(n => Named(all, n)).Where(t => t != null).ToList();
            foreach (var missing in DebrisNames.Where(n => shown.All(t => t.name != n))) log.AppendLine("WARNING: " + missing + " was not found.");
            var pieces = shown.Where(t => PickupNames.Contains(t.name)).ToList();
            if (pieces.Count == 0) throw new Exception("None of the car debris pieces to pick up were found (" + string.Join(", ", PickupNames) + ").");
            foreach (var other in shown.Where(t => !PickupNames.Contains(t.name)))
            {
                var oldZone = other.Find(DebrisZoneName);
                if (oldZone != null) { UnityEngine.Object.DestroyImmediate(oldZone.gameObject); log.AppendLine(other.name + ": no photo / pickup (removed its zone)"); }
            }
            var order = new List<Transform>();
            Vector3 from = sequence.walkTarget != null ? sequence.walkTarget.position : pieces[0].position;
            while (pieces.Count > 0)
            {
                var next = pieces.OrderBy(t => (t.position - from).sqrMagnitude).First();
                order.Add(next);
                pieces.Remove(next);
                from = next.position;
            }

            // keep Inspector tuning when rebuilding
            var oldPhotos = existing.Select(t => t.GetComponent<MidnightSkidPhoto>()).Where(c => c != null).ToDictionary(c => c.gameObject.name, c => EditorJsonUtility.ToJson(c));
            var oldPickups = existing.Select(t => t.GetComponent<MidnightDebrisPickup>()).Where(c => c != null).ToDictionary(c => c.gameObject.name, c => EditorJsonUtility.ToJson(c));
            foreach (var t in existing) UnityEngine.Object.DestroyImmediate(t.gameObject);
            var oldUi = canvas.Find(DebrisUiName);
            if (oldUi != null) UnityEngine.Object.DestroyImmediate(oldUi.gameObject);

            var ui = Container(canvas, DebrisUiName);
            var photoPrompt = Label(template, ui, scene, "DebrisPrompt", DebrisPromptText);
            var pickupPrompt = Label(template, ui, scene, "PickupPrompt", PickupPromptText);
            var shock = ShockLines.Select((text, i) => ThoughtBox(all, ui, scene, "Shock" + (i + 1), text)).ToArray();
            var found = FoundLines.Select((text, i) => ThoughtBox(all, ui, scene, "Found" + (i + 1), text)).ToArray();
            var pickupSound = Clip(GhostFx + "/Audio/Debris_PickUp.wav", log);
            var stowSound = Clip(GhostFx + "/Audio/Debris_Stow.wav", log);

            string skidJson = EditorJsonUtility.ToJson(skidPhoto);
            var pickups = new List<MidnightDebrisPickup>();
            MidnightDebrisPickup previous = null;
            for (int i = 0; i < order.Count; i++)
            {
                var piece = order[i];
                var zone = Zone(piece, log, DebrisZoneName, DebrisZoneMargin);
                string rootName = DebrisRootPrefix + (i + 1) + "_" + piece.name.Replace("CarDebris_", "");
                var root = new GameObject(rootName);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.position = piece.position;

                var photo = root.AddComponent<MidnightSkidPhoto>();
                EditorJsonUtility.FromJsonOverwrite(oldPhotos.TryGetValue(rootName, out var oldPhoto) ? oldPhoto : skidJson, photo);
                photo.player = skidPhoto.player;
                photo.skidMark = piece;
                photo.interactionZone = zone;
                photo.requires = null;
                photo.requiresPickup = previous;
                if (oldPhoto == null) { photo.availableDelay = i == 0 ? 0f : .4f; photo.facingDotMinimum = .2f; }
                photo.thought = null;
                photo.prompt = photoPrompt;
                photo.shutterHint = skidPhoto.shutterHint;
                photo.savedLabel = skidPhoto.savedLabel;
                EditorUtility.SetDirty(photo);

                var pickup = root.AddComponent<MidnightDebrisPickup>();
                if (oldPickups.TryGetValue(rootName, out var oldPickup)) EditorJsonUtility.FromJsonOverwrite(oldPickup, pickup);
                pickup.player = skidPhoto.player;
                pickup.piece = piece;
                pickup.afterPhoto = photo;
                pickup.interactionZone = zone;
                pickup.viewCamera = skidPhoto.viewCamera;
                pickup.prompt = pickupPrompt;
                if (pickupSound != null) pickup.pickupSound = pickupSound;
                if (stowSound != null) pickup.stowSound = stowSound;
                EditorUtility.SetDirty(pickup);
                pickups.Add(pickup);
                previous = pickup;
                log.AppendLine((i + 1) + ") " + piece.name + " at " + piece.position.ToString("F2") + " (zone " + zone.size.ToString("F1") + " m)");
            }

            sequence.shockLines = shock;
            sequence.foundLines = found;
            sequence.debrisPickups = pickups.ToArray();
            sequence.hiddenDebris = shown.Select(t => t.gameObject).ToArray();
            EditorUtility.SetDirty(sequence);
            log.AppendLine("Lines: SeatingCanvas/" + DebrisUiName + "/Shock1-2, Found1-2; prompts: DebrisPrompt, PickupPrompt");
        }

        // "ฝนเริ่มตกหนักแล้วสิ" 0.7 s after the skid photo (was "ฝนตกแล้ว ต้องรีบแล้วสิ" after 4.5 s), the first blood photo is ready
        // sooner to match, and the ghost car drives front first.
        static void Fixes1(Transform[] all, StringBuilder log)
        {
            var rain = all.Select(t => t.GetComponent<MidnightRain>()).FirstOrDefault(c => c != null);
            if (rain == null) throw new Exception("MidnightRain (step 4) was not found.");
            Undo.RecordObject(rain, "Rain line");
            rain.thoughtDelay = RainThoughtDelay;
            EditorUtility.SetDirty(rain);
            if (rain.thought != null)
            {
                Undo.RecordObject(rain.thought, "Rain line");
                rain.thought.text = RainThoughtText;
                EditorUtility.SetDirty(rain.thought);
            }
            log.AppendLine("Rain line: " + RainThoughtText + " (" + RainThoughtDelay + " s after the skid photo)");

            var firstBlood = all.Where(t => t.parent == null && t.name.StartsWith(BloodRootPrefix + "1_")).Select(t => t.GetComponent<MidnightSkidPhoto>()).FirstOrDefault(c => c != null);
            if (firstBlood != null && firstBlood.availableDelay > 4f)
            {
                Undo.RecordObject(firstBlood, "Blood photo delay");
                firstBlood.availableDelay = 4f;
                EditorUtility.SetDirty(firstBlood);
                log.AppendLine(firstBlood.name + ": ready 4 s after the skid photo (was longer)");
            }

            var sequence = all.Select(t => t.GetComponent<MidnightGhostSequence>()).FirstOrDefault(c => c != null);
            if (sequence != null && sequence.ghostCar != null)
            {
                Undo.RecordObject(sequence, "Ghost car facing");
                sequence.carYawOffset = 180f;
                EditorUtility.SetDirty(sequence);
                log.AppendLine("Ghost car: front first (Car Yaw Offset 180)");
            }
        }

        // ---- the ending -------------------------------------------------------------------------------------------------------------
        static void AddEnding(Scene scene, Transform[] all, StringBuilder log, bool rebuild)
        {
            var sequence = all.Select(t => t.GetComponent<MidnightGhostSequence>()).FirstOrDefault(c => c != null);
            if (sequence == null) throw new Exception("MidnightGhostSequence (steps 5-9) was not found.");
            var ride = DriveRide(all);
            if (ride == null) throw new Exception("The car ride (PorschePassengerRide on FD) was not found.");
            var canvas = all.FirstOrDefault(t => t.parent == null && t.name == "SeatingCanvas");
            if (canvas == null) throw new Exception("SeatingCanvas was not found.");
            var existing = all.Where(t => t.parent == null && (t.name == EndingRootName || t.name == EndingCanvasName)).ToList();
            if (existing.Count > 0 && !rebuild)
            { log.AppendLine("SKIPPED: the ending is already in the scene (Tools > Midnight Scene > 10) Rebuild Ending to redo it)."); return; }
            var oldEnding = existing.Select(t => t.GetComponent<MidnightEnding>()).FirstOrDefault(c => c != null);
            string oldJson = oldEnding != null ? EditorJsonUtility.ToJson(oldEnding) : null;
            foreach (var t in existing) UnityEngine.Object.DestroyImmediate(t.gameObject);
            var oldUi = canvas.Find(EndingUiName);
            if (oldUi != null) UnityEngine.Object.DestroyImmediate(oldUi.gameObject);

            // objective + prompt, copied from the evening "back to the car" ones (same panels and font)
            var ui = Container(canvas, EndingUiName);
            var objectiveSource = canvas.Find("PoliceEncounter/ReturnToCarObjective");
            var promptSource = canvas.Find("PoliceEncounter/BoardCarPrompt");
            TMP_Text objective = objectiveSource != null ? PanelCopy(objectiveSource, ui, scene, "GarageObjective", GarageObjectiveText)
                                                         : ThoughtBox(all, ui, scene, "GarageObjective", GarageObjectiveText);
            TMP_Text prompt;
            if (promptSource != null) prompt = PanelCopy(promptSource, ui, scene, "BoardToGaragePrompt", BoardGaragePromptText);
            else
            {
                var template = all.FirstOrDefault(t => t.name == "ExitPrompt" && t.GetComponent<TMP_Text>() != null);
                if (template == null) throw new Exception("No '[E]' prompt label to copy (PoliceEncounter/BoardCarPrompt or CarExitUI/ExitPrompt).");
                var holder = Container(ui, "BoardToGaragePrompt");
                prompt = Label(template, holder, scene, "PromptText", BoardGaragePromptText);
                prompt.gameObject.SetActive(true);
                holder.gameObject.SetActive(false);
            }
            log.AppendLine("Objective: " + GarageObjectiveText + "; prompt: " + BoardGaragePromptText + " (SeatingCanvas/" + EndingUiName + ")");

            // the ending screen: its own canvas above everything
            var labelTemplate = sequence.tiredLines != null ? sequence.tiredLines.FirstOrDefault(l => l != null) : null;
            if (labelTemplate == null) throw new Exception("No Thai text label to copy the font from (MidnightGhostUI/Tired1).");
            var root = new GameObject(EndingCanvasName, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(root, scene);
            var endingCanvas = root.AddComponent<Canvas>();
            endingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            endingCanvas.sortingOrder = 32000;
            var scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;

            var screen = UiChild(root.transform, "EndingScreen");
            var screenGroup = screen.gameObject.AddComponent<CanvasGroup>();
            var black = screen.gameObject.AddComponent<UnityEngine.UI.Image>();
            black.color = Color.black;
            black.raycastTarget = false;

            var lines = new TMP_Text[EndingLines.Length];
            for (int i = 0; i < EndingLines.Length; i++)
                lines[i] = TextCopy(labelTemplate, screen, scene, "EndingLine" + (i + 1), EndingLines[i], new Vector2(.5f, .5f),
                                    new Vector2(0f, (EndingLines.Length - 1) * 50f - i * 100f), new Vector2(1700f, 100f), i == 0 ? 58f : 46f);

            var art = UiChild(screen, "KeyArt");
            var artGroup = art.gameObject.AddComponent<CanvasGroup>();
            var poster = UiChild(art, "Poster");
            var posterImage = poster.gameObject.AddComponent<UnityEngine.UI.Image>();
            posterImage.sprite = SpriteAt(KeyArtPath, 2048, log, true);
            posterImage.raycastTarget = false;
            var soon = UiChild(art, "ComingSoon");
            var soonGroup = soon.gameObject.AddComponent<CanvasGroup>();
            TextCopy(labelTemplate, soon, scene, "ComingSoonText", ComingSoonText, new Vector2(.5f, 0f), new Vector2(0f, 120f), new Vector2(1400f, 110f), 64f);
            var steamLogo = SteamRow(soon, log);

            AudioSource typing = null;
            var typingSource = all.Select(t => t.GetComponent<AudioSource>()).FirstOrDefault(a => a != null && a.name == "DialogueTypingAudio" && a.clip != null);
            if (typingSource != null)
            {
                typing = screen.gameObject.AddComponent<AudioSource>();
                typing.clip = typingSource.clip;
                typing.volume = typingSource.volume;
                typing.loop = true;
                typing.playOnAwake = false;
                typing.spatialBlend = 0f;
            }

            FillScreen(art, log);

            var endingRoot = new GameObject(EndingRootName);
            SceneManager.MoveGameObjectToScene(endingRoot, scene);
            var ending = endingRoot.AddComponent<MidnightEnding>();
            if (oldJson != null) EditorJsonUtility.FromJsonOverwrite(oldJson, ending);
            ending.after = sequence;
            ending.ride = ride;
            ending.player = sequence.player;
            ending.objective = objective;
            ending.boardPrompt = prompt;
            ending.endingScreen = screenGroup;
            ending.endingLines = lines;
            ending.keyArt = artGroup;
            ending.comingSoon = soonGroup;
            ending.typingAudio = typing;
            ending.rain = all.Select(t => t.GetComponent<MidnightRain>()).FirstOrDefault(c => c != null);
            ending.steamLogo = steamLogo;
            EditorUtility.SetDirty(ending);
            log.AppendLine("Ending lines: " + string.Join(" / ", EndingLines) + "; then the key art (" + KeyArtPath + ") + " + ComingSoonText);
            log.AppendLine("Ending screen: " + EndingCanvasName + " (edit the texts there); typing sound: " + (typing != null ? typing.clip.name : "none"));
        }

        // The car Khem drives and gets out of (the scene has a second, switched-off PorschePassengerRide on another car):
        // the one the rain, the F1/F2 shortcuts or the drive lines use.
        static PorschePassengerRide DriveRide(Transform[] all)
        {
            var rain = all.Select(t => t.GetComponent<MidnightRain>()).FirstOrDefault(c => c != null);
            if (rain != null && rain.ride != null) return rain.ride;
            foreach (var shortcuts in all.Select(t => t.GetComponent<EveningTestingShortcuts>()).Where(c => c != null))
                if (shortcuts.ride != null) return shortcuts.ride;
            var thoughts = all.Select(t => t.GetComponent<EveningDriveThoughts>()).FirstOrDefault(c => c != null);
            if (thoughts != null && thoughts.ride != null) return thoughts.ride;
            return all.Select(t => t.GetComponent<PorschePassengerRide>()).FirstOrDefault(c => c != null && c.enabled);
        }

        static void Fixes3(Transform[] all, StringBuilder log)
        {
            var ending = all.Select(t => t.GetComponent<MidnightEnding>()).FirstOrDefault(c => c != null);
            if (ending == null) { log.AppendLine("No ending in the scene yet."); return; }
            var ride = DriveRide(all);
            if (ride == null) throw new Exception("The car ride (PorschePassengerRide on FD) was not found.");
            if (ending.ride == ride) { log.AppendLine("Ending car: already " + ride.name + "."); return; }
            Undo.RecordObject(ending, "Ending car");
            ending.ride = ride;
            EditorUtility.SetDirty(ending);
            log.AppendLine("Ending car: now " + ride.name + " (the car Khem got out of; it was a switched-off copy on another car).");
        }

        static TMP_Text PanelCopy(Transform source, Transform parent, Scene scene, string name, string text)
        {
            var copy = UnityEngine.Object.Instantiate(source.gameObject);
            copy.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(copy, scene);
            copy.name = name;
            copy.transform.SetParent(parent, false);
            var label = copy.GetComponentInChildren<TMP_Text>(true);
            if (label == null) throw new Exception(source.name + " has no text label.");
            label.text = text;
            label.gameObject.SetActive(true);
            copy.SetActive(false);
            EditorUtility.SetDirty(label);
            return label;
        }

        static RectTransform UiChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static TMP_Text TextCopy(TMP_Text template, Transform parent, Scene scene, string name, string text, Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
        {
            var copy = UnityEngine.Object.Instantiate(template.gameObject);
            copy.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(copy, scene);
            copy.name = name;
            copy.transform.SetParent(parent, false);
            copy.SetActive(true);
            var rect = (RectTransform)copy.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            var label = copy.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            EditorUtility.SetDirty(label);
            return label;
        }

        static Sprite SpriteAt(string path, int maxSize, StringBuilder log, bool mipmaps = false)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { log.AppendLine("WARNING: " + path + " was not found."); return null; }
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled != mipmaps)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = mipmaps;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = maxSize;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Layout 2: the whole key art, full height in the middle; the sides are filled by a blurred, darkened copy of it (no plain
        // black bars); a dark shade at the bottom keeps "Wishlist now on Steam" readable. Returns true if it changed anything.
        static bool FillScreen(Transform art, StringBuilder log)
        {
            var poster = art.Find("Poster") as RectTransform;
            if (poster == null) { log.AppendLine("WARNING: KeyArt/Poster was not found."); return false; }
            bool changed = false;
            var image = poster.GetComponent<UnityEngine.UI.Image>();
            if (image != null && image.preserveAspect) { Undo.RecordObject(image, "Key art"); image.preserveAspect = false; EditorUtility.SetDirty(image); changed = true; }
            var fitter = poster.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            if (fitter == null) { fitter = Undo.AddComponent<UnityEngine.UI.AspectRatioFitter>(poster.gameObject); changed = true; }
            float aspect = image != null && image.sprite != null ? image.sprite.rect.width / Mathf.Max(1f, image.sprite.rect.height) : 1728f / 2048f;
            if (fitter.aspectMode != UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent || Mathf.Abs(fitter.aspectRatio - aspect) > .001f)
            {
                Undo.RecordObject(fitter, "Key art");
                fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = aspect;
                EditorUtility.SetDirty(fitter);
                changed = true;
            }
            if (Mathf.Abs(poster.pivot.x - .5f) > .001f || Mathf.Abs(poster.pivot.y - .5f) > .001f)
            {
                Undo.RecordObject(poster, "Key art");
                poster.pivot = new Vector2(.5f, .5f);
                EditorUtility.SetDirty(poster);
                changed = true;
            }
            if (image != null && image.sprite != null) SpriteAt(AssetDatabase.GetAssetPath(image.sprite), 2048, log, true);   // smooth when scaled down

            // the blurred copy behind it, covering the whole screen
            if (art.Find("BlurredBackground") == null)
            {
                var blur = new GameObject("BlurredBackground", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(blur, "Key art background");
                blur.transform.SetParent(art, false);
                blur.transform.SetAsFirstSibling();
                var rect = (RectTransform)blur.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                var blurImage = blur.AddComponent<UnityEngine.UI.Image>();
                blurImage.sprite = SpriteAt(KeyArtBlurPath, 2048, log);
                blurImage.raycastTarget = false;
                var cover = blur.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                cover.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
                cover.aspectRatio = 16f / 9f;
                changed = true;
            }

            if (art.Find("BottomShade") == null)
            {
                var shade = new GameObject("BottomShade", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(shade, "Key art shade");
                shade.transform.SetParent(art, false);
                shade.transform.SetSiblingIndex(poster.GetSiblingIndex() + 1);
                var rect = (RectTransform)shade.transform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(.5f, 0f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, 430f);
                var shadeImage = shade.AddComponent<UnityEngine.UI.Image>();
                shadeImage.sprite = SpriteAt(BottomShadePath, 256, log);
                shadeImage.color = Color.white;
                shadeImage.raycastTarget = false;
                changed = true;
            }
            if (changed) log.AppendLine("Key art: the whole poster in the middle, a blurred copy fills the sides (KeyArt/BlurredBackground), bottom shade under the text.");
            return changed;
        }

        static void Fixes6(Transform[] all, StringBuilder log)
        {
            var ending = all.Select(t => t.GetComponent<MidnightEnding>()).FirstOrDefault(c => c != null);
            if (ending == null) { log.AppendLine("No ending in the scene yet."); return; }
            if (ending.keyArt != null) FillScreen(ending.keyArt.transform, log);
            if (ending.comingSoon != null)
            {
                var logo = SteamRow(ending.comingSoon.transform, log);
                if (ending.steamLogo != logo) { Undo.RecordObject(ending, "Steam logo"); ending.steamLogo = logo; EditorUtility.SetDirty(ending); }
            }
        }

        // "[Steam logo] Wishlist now on Steam" in one centred row (the logo is hidden while it has no image). Returns the logo Image.
        static UnityEngine.UI.Image SteamRow(Transform comingSoon, StringBuilder log)
        {
            var row = comingSoon.Find("Row") as RectTransform;
            var text = comingSoon.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "ComingSoonText");
            if (row == null)
            {
                var go = new GameObject("Row", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Steam row");
                go.transform.SetParent(comingSoon, false);
                row = (RectTransform)go.transform;
                row.anchorMin = row.anchorMax = new Vector2(.5f, 0f);
                row.pivot = new Vector2(.5f, .5f);
                row.anchoredPosition = new Vector2(0f, 120f);
                row.sizeDelta = new Vector2(1700f, 120f);
                var layout = go.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 28f;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            }
            var logoTransform = row.Find("SteamLogo");
            UnityEngine.UI.Image logo;
            if (logoTransform == null)
            {
                var go = new GameObject("SteamLogo", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Steam logo");
                go.transform.SetParent(row, false);
                go.transform.SetAsFirstSibling();
                logo = go.AddComponent<UnityEngine.UI.Image>();
                logo.preserveAspect = true;
                logo.raycastTarget = false;
                var element = go.AddComponent<UnityEngine.UI.LayoutElement>();
                element.preferredHeight = SteamLogoHeight;
                element.preferredWidth = SteamLogoHeight;
            }
            else logo = logoTransform.GetComponent<UnityEngine.UI.Image>();
            if (text != null && text.transform.parent != row)
            {
                Undo.SetTransformParent(text.transform, row, "Steam row");
                text.transform.SetAsLastSibling();
            }
            AssignSteamLogo(logo, log);
            return logo;
        }

        // The official Steam logo (any image with "steam" in its name in the ending's Textures folder), if it has been added.
        static void AssignSteamLogo(UnityEngine.UI.Image logo, StringBuilder log)
        {
            if (logo == null) return;
            if (logo.sprite == null)
            {
                string path = AssetDatabase.FindAssets("t:Texture2D", new[] { EndingTexturesFolder }).Select(AssetDatabase.GUIDToAssetPath)
                                           .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Contains("steam"));
                if (path == null)
                {
                    log.AppendLine("Steam logo: not added yet - put the official logo (PNG, from Steamworks > Steam Branding Guidelines) in "
                                   + EndingTexturesFolder + " with \"steam\" in its file name, or drag it into SteamLogo's Source Image.");
                    logo.gameObject.SetActive(false);
                    return;
                }
                Undo.RecordObject(logo, "Steam logo");
                logo.sprite = SpriteAt(path, 512, log, true);
                EditorUtility.SetDirty(logo);
                log.AppendLine("Steam logo: " + path);
            }
            var element = logo.GetComponent<UnityEngine.UI.LayoutElement>();
            if (element != null && logo.sprite != null)
            {
                Undo.RecordObject(element, "Steam logo");
                element.preferredHeight = SteamLogoHeight;
                element.preferredWidth = SteamLogoHeight * logo.sprite.rect.width / Mathf.Max(1f, logo.sprite.rect.height);
                EditorUtility.SetDirty(element);
            }
            logo.gameObject.SetActive(logo.sprite != null);
        }

        // "Thank You for Playing" and "Wishlist now on Steam" (were "เข้มจะสามารถช่วยลุงได้หรือมั้ย" and "Coming soon on Steam"), plus the Steam logo.
        static void Fixes5(Transform[] all, StringBuilder log)
        {
            var ending = all.Select(t => t.GetComponent<MidnightEnding>()).FirstOrDefault(c => c != null);
            if (ending == null) { log.AppendLine("No ending in the scene yet."); return; }
            var renames = new Dictionary<string, string> { { OldEndingTexts[0], EndingLines[0] }, { OldEndingTexts[1], ComingSoonText } };
            var canvas = all.FirstOrDefault(t => t.parent == null && t.name == EndingCanvasName);
            if (canvas != null)
                foreach (var label in canvas.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (!renames.TryGetValue(label.text, out var text)) continue;
                    Undo.RecordObject(label, "Ending text");
                    log.AppendLine("Text: " + label.text + " -> " + text);
                    label.text = text;
                    EditorUtility.SetDirty(label);
                }
            if (ending.comingSoon != null)
            {
                var logo = SteamRow(ending.comingSoon.transform, log);
                if (ending.steamLogo != logo) { Undo.RecordObject(ending, "Steam logo"); ending.steamLogo = logo; EditorUtility.SetDirty(ending); }
            }
        }

        static void Fixes4(Transform[] all, StringBuilder log)
        {
            var ending = all.Select(t => t.GetComponent<MidnightEnding>()).FirstOrDefault(c => c != null);
            if (ending == null) { log.AppendLine("No ending in the scene yet."); return; }
            var rain = all.Select(t => t.GetComponent<MidnightRain>()).FirstOrDefault(c => c != null);
            if (ending.rain != rain)
            {
                Undo.RecordObject(ending, "Ending sound");
                ending.rain = rain;
                EditorUtility.SetDirty(ending);
                log.AppendLine("The rain sound fades out after E at the car (" + ending.soundFadeSeconds + " s, with the other looping sounds).");
            }
            var art = ending.keyArt != null ? ending.keyArt.transform : null;
            if (art != null) FillScreen(art, log);
        }

        // The saved scene file or the open (maybe unsaved) scene has a root object whose name starts with this.
        static bool SceneHasRoot(string file, string prefix)
        {
            if (file.Contains("m_Name: " + prefix)) return true;
            var scene = SceneManager.GetSceneByPath(ScenePath);
            return scene.IsValid() && scene.isLoaded && scene.GetRootGameObjects().Any(g => g.name.StartsWith(prefix));
        }

        // The old wording of the first found line is still in the scene (file or the open, unsaved scene).
        static bool HasOldFoundLine(string file)
        {
            if (file.Contains(OldFoundLine) || file.Contains(YamlEscape(OldFoundLine))) return true;
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TMP_Text>(true)).Any(t => t.text == OldFoundLine);
        }

        static void UpdateFoundLine(Transform[] all, StringBuilder log)
        {
            int count = 0;
            foreach (var label in all.Select(t => t.GetComponent<TMP_Text>()).Where(t => t != null && t.text == OldFoundLine))
            {
                Undo.RecordObject(label, "Found line");
                label.text = FoundLines[0];
                EditorUtility.SetDirty(label);
                count++;
            }
            log.AppendLine(count > 0 ? "Found line: " + OldFoundLine + " -> " + FoundLines[0] + " (" + count + " label)" : "Found line already up to date.");
        }

        // The ghost car keeps the model's own shape and colours, just see-through with a faint cold edge (no smoke, no glitch).
        static void ClearCarLook(Material material, StringBuilder log)
        {
            if (material == null) return;
            Undo.RecordObject(material, "Ghost car look");
            material.SetColor("_Tint", new Color(.85f, .9f, 1f, 1f));
            material.SetFloat("_Saturation", .85f);
            material.SetFloat("_Alpha", .3f);
            material.SetFloat("_RimPower", 3.5f);
            material.SetFloat("_RimAlpha", .35f);
            material.SetFloat("_WispAmount", .08f);
            material.SetFloat("_WispScale", 1.4f);
            material.SetFloat("_Flicker", .25f);
            material.SetFloat("_Jitter", 0f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            log.AppendLine("Ghost car look: original shape and colours, see-through (" + AssetDatabase.GetAssetPath(material) + ")");
        }

        static RectTransform Lid(Transform parent, string name, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, top ? .5f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : .5f);
            rect.pivot = new Vector2(.5f, top ? 1f : 0f);
            rect.offsetMin = new Vector2(0f, top ? 0f : -2f);
            rect.offsetMax = new Vector2(0f, top ? 2f : 0f);    // a little overlap in the middle when closed
            rect.localScale = new Vector3(1f, 0f, 1f);
            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            return rect;
        }

        static AudioClip Clip(string path, StringBuilder log)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) log.AppendLine("WARNING: " + path + " not found.");
            return clip;
        }

        static GameObject FindCar(StringBuilder log)
        {
            var paths = AssetDatabase.FindAssets("CarDE02").Select(AssetDatabase.GUIDToAssetPath).Distinct().ToList();
            string pick = paths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == GhostCarAsset && p.EndsWith(".prefab"))
                       ?? paths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == GhostCarAsset && AssetDatabase.LoadAssetAtPath<GameObject>(p) != null)
                       ?? paths.FirstOrDefault(p => Path.GetFileName(p).StartsWith(GhostCarAsset) && AssetDatabase.LoadAssetAtPath<GameObject>(p) != null);
            if (pick == null) { log.AppendLine("WARNING: " + GhostCarAsset + " was not found in the project - no ghost car (found: " + string.Join(", ", paths.Take(8)) + ")."); return null; }
            return AssetDatabase.LoadAssetAtPath<GameObject>(pick);
        }

        static void SetShutter(Transform[] all, StringBuilder log)
        {
            var paths = AssetDatabase.FindAssets("iphone-camera-capture t:AudioClip").Select(AssetDatabase.GUIDToAssetPath).ToList();
            string path = paths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == ShutterClipName)
                       ?? AssetDatabase.FindAssets("t:AudioClip").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == ShutterClipName);
            var clip = path != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(path) : null;
            if (clip == null) { log.AppendLine("WARNING: the sound " + ShutterClipName + " was not found in the project - the shutter sound was not changed."); return; }
            int count = 0;
            foreach (var photo in all.Select(t => t.GetComponent<MidnightSkidPhoto>()).Where(c => c != null))
            {
                Undo.RecordObject(photo, "Shutter sound");
                photo.shutterSound = clip;
                EditorUtility.SetDirty(photo);
                count++;
            }
            log.AppendLine("Shutter sound: " + path + " on " + count + " photo(s).");
        }

        static Material GhostCarMaterial(StringBuilder log)
        {
            string path = GhostFx + "/Materials/M_GhostCar.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find(GhostShaderName);
            if (shader == null) throw new Exception("The shader " + GhostShaderName + " was not found (Environment/Ghost/Shaders/Ghost.shader).");
            if (!AssetDatabase.IsValidFolder(GhostFx + "/Materials")) AssetDatabase.CreateFolder(GhostFx, "Materials");
            material = new Material(shader) { name = "M_GhostCar" };
            AssetDatabase.CreateAsset(material, path);
            ClearCarLook(material, log);
            AssetDatabase.SaveAssets();
            log.AppendLine("Created " + path);
            return material;
        }

        // Imports Characters/GhostStudent (model, clip, see-through material, Animator Controller, prefab) the first time.
        static GameObject GhostPrefab(StringBuilder log)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GhostPrefabPath);
            if (prefab != null) return prefab;
            var shader = Shader.Find(GhostShaderName);
            if (shader == null) throw new Exception("The shader " + GhostShaderName + " was not found (Environment/Ghost/Shaders/Ghost.shader).");
            AssetDatabase.ImportAsset(GhostFbx, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(GhostFbx) as ModelImporter;
            if (importer == null) throw new Exception(GhostFbx + " was not found.");
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
            importer = (ModelImporter)AssetImporter.GetAtPath(GhostFbx);
            var clips = importer.defaultClipAnimations;
            if (clips != null && clips.Length > 0)
            {
                var c = clips[0];
                c.name = GhostClipName;
                c.loopTime = false;
                importer.clipAnimations = new[] { c };
                importer.SaveAndReimport();
            }

            if (!AssetDatabase.IsValidFolder(GhostFx + "/Materials")) AssetDatabase.CreateFolder(GhostFx, "Materials");
            string materialPath = GhostFx + "/Materials/M_GhostStudent.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "M_GhostStudent" };
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(GhostTexture));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var clip = AssetDatabase.LoadAllAssetsAtPath(GhostFbx).OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__preview__"));
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GhostControllerPath);
            if (controller == null && clip != null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(GhostControllerPath);
                var state = controller.AddMotion(clip);
                state.name = GhostClipName;
                EditorUtility.SetDirty(controller);
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(GhostFbx);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = "GhostStudent";
            var animator = go.GetComponent<Animator>();
            if (animator == null) animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.sharedMaterial = material;
                skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                skin.receiveShadows = false;
                skin.updateWhenOffscreen = true;
            }
            PrefabUtility.SaveAsPrefabAsset(go, GhostPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            log.AppendLine("Created the ghost student: " + GhostPrefabPath + " (clip " + GhostClipName + ", material " + materialPath + ")");
            return AssetDatabase.LoadAssetAtPath<GameObject>(GhostPrefabPath);
        }

        static Transform Container(Transform canvas, string name)
        {
            var ui = new GameObject(name, typeof(RectTransform));
            ui.transform.SetParent(canvas, false);
            var rect = (RectTransform)ui.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return ui.transform;
        }

        // ---- step 4 -------------------------------------------------------------------------------------------------------------------
        static void AddRain(Scene scene, Transform[] all, StringBuilder log, bool rebuild)
        {
            var existing = all.Where(t => t.parent == null && t.name == RainRootName).ToList();
            if (existing.Count > 0 && !rebuild)
            { log.AppendLine("SKIPPED: " + RainRootName + " is already in the scene (Tools > Midnight Scene > 4) Rebuild Rain After Photo to redo it)."); return; }
            var photo = all.Select(t => t.GetComponent<MidnightSkidPhoto>()).FirstOrDefault(c => c != null);
            if (photo == null) throw new Exception("MidnightSkidPhoto (step 3) was not found - the rain starts after that photo.");
            var canvas = all.FirstOrDefault(t => t.parent == null && t.name == "SeatingCanvas");
            if (canvas == null) throw new Exception("SeatingCanvas was not found.");

            var drop = RainMaterial("RainDrop", "T_Rain_Streak", log);
            var ripple = RainMaterial("RainRipple", "T_Rain_Ripple", log);
            var splash = RainMaterial("RainSplash", "T_Rain_Droplet", log);
            var loop = AssetDatabase.LoadAssetAtPath<AudioClip>(RainFolder + "/Audio/Rain_Light_Loop.wav");
            if (loop == null) log.AppendLine("WARNING: Rain_Light_Loop.wav not found (no rain sound).");

            MidnightRain old = existing.Select(t => t.GetComponent<MidnightRain>()).FirstOrDefault(c => c != null);
            string oldJson = old != null ? EditorJsonUtility.ToJson(old) : null;
            foreach (var t in existing) UnityEngine.Object.DestroyImmediate(t.gameObject);
            var oldUi = canvas.Find(RainUiName);
            if (oldUi != null) UnityEngine.Object.DestroyImmediate(oldUi.gameObject);

            var ui = new GameObject(RainUiName, typeof(RectTransform));
            ui.transform.SetParent(canvas, false);
            var rect = (RectTransform)ui.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var thought = ThoughtBox(all, ui.transform, scene, RainThoughtName, RainThoughtText);

            var root = new GameObject(RainRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            var rain = root.AddComponent<MidnightRain>();
            if (oldJson != null) EditorJsonUtility.FromJsonOverwrite(oldJson, rain);
            rain.afterPhoto = photo;
            rain.thought = thought;
            rain.dropMaterial = drop;
            rain.rippleMaterial = ripple;
            rain.splashMaterial = splash;
            if (loop != null) rain.rainLoop = loop;
            EditorUtility.SetDirty(rain);
            log.AppendLine("Rain starts after " + photo.name + "; Khem's line: SeatingCanvas/" + RainUiName + "/" + RainThoughtName + " = " + RainThoughtText);
            log.AppendLine("Wet area: " + rain.wetRadius + " m around the skid mark; materials in " + RainFolder + "/Materials");
        }

        // Revision 2 of the rain: it starts when the drive car begins its first turn and lands only on the Terrain and that car.
        static void UpdateRain(Transform[] all, StringBuilder log)
        {
            var rain = all.Select(t => t.GetComponent<MidnightRain>()).FirstOrDefault(c => c != null);
            if (rain == null) throw new Exception("MidnightRain (step 4) was not found.");
            var thoughts = all.Select(t => t.GetComponent<EveningDriveThoughts>()).FirstOrDefault(c => c != null);
            var ride = thoughts != null && thoughts.ride != null ? thoughts.ride
                     : all.SelectMany(t => t.GetComponents<PorschePassengerRide>()).FirstOrDefault(r => r != null && r.enabled);
            if (ride == null) throw new Exception("The drive car (PorschePassengerRide) was not found.");
            Undo.RecordObject(rain, "Rain on the drive");
            rain.ride = ride;
            rain.car = ride.transform;
            EditorUtility.SetDirty(rain);
            log.AppendLine("Rain starts when " + ride.name + " begins its first turn; heaviest after the skid mark photo.");
            log.AppendLine("Drops land only on the Terrain and on " + ride.transform.name + ".");
        }

        static Material RainMaterial(string name, string texture, StringBuilder log)
        {
            string path = RainFolder + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find(RainShaderName);
            if (shader == null) throw new Exception("The shader " + RainShaderName + " was not found (Environment/Rain/Shaders/RainParticle.shader).");
            string texturePath = RainFolder + "/Textures/" + texture + ".png";
            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
            {
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            if (!AssetDatabase.IsValidFolder(RainFolder + "/Materials")) AssetDatabase.CreateFolder(RainFolder, "Materials");
            material = new Material(shader) { name = name };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", Color.white);   // the colour comes from the rain settings (MidnightRain)
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            log.AppendLine("Created " + path);
            return material;
        }

        // Revision 2: Khem's thought before the photo, and the phone screen tipped up towards him.
        static void UpdateSkidPhoto(Scene scene, Transform[] all, StringBuilder log)
        {
            var photo = all.Select(t => t.GetComponent<MidnightSkidPhoto>()).FirstOrDefault(c => c != null);
            if (photo == null) { RemoveArrivalLine(all, log); AddSkidPhoto(scene, all, log, false); return; }
            var canvas = all.FirstOrDefault(t => t.parent == null && t.name == "SeatingCanvas");
            var ui = canvas != null ? canvas.Find(PhotoUiName) : null;
            if (ui == null) throw new Exception("SeatingCanvas/" + PhotoUiName + " was not found (use 3) Rebuild Skid Mark Photo).");
            Undo.RecordObject(photo, "Skid photo thought");
            EnsureThought(all, ui, scene, photo, log);
            if (photo.heldRotation == new Vector3(6f, 180f, -90f))
            {
                photo.heldRotation = new Vector3(-8f, 180f, -90f);
                log.AppendLine("Held Rotation: (-8, 180, -90) - the screen now tips up towards Khem.");
            }
            EditorUtility.SetDirty(photo);
        }

        // Khem's thought, in the same box as his drive thoughts (a copy of DriveThought3, which is no longer used).
        static void EnsureThought(Transform[] all, Transform ui, Scene scene, MidnightSkidPhoto photo, StringBuilder log)
        {
            var existing = ui.Find(ThoughtName);
            if (existing != null && existing.GetComponentInChildren<TMP_Text>(true) != null)
            { photo.thought = existing.GetComponentInChildren<TMP_Text>(true); return; }
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var label = ThoughtBox(all, ui, scene, ThoughtName, ThoughtText);
            photo.thought = label;
            EditorUtility.SetDirty(photo);
            log.AppendLine("Khem's thought: SeatingCanvas/" + PhotoUiName + "/" + ThoughtName + " = " + ThoughtText);
        }

        // A copy of Khem's drive-thought box (DriveThought3, no longer used while driving), hidden until the script shows it.
        static TMP_Text ThoughtBox(Transform[] all, Transform parent, Scene scene, string name, string text)
        {
            var template = all.FirstOrDefault(t => t.name == "DriveThought3") ?? all.FirstOrDefault(t => t.name == "DriveThought1");
            if (template == null || template.GetComponentInChildren<TMP_Text>(true) == null)
                throw new Exception("SeatingCanvas/DriveThought3 (Khem's thought box) was not found.");
            var box = UnityEngine.Object.Instantiate(template.gameObject);
            box.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(box, scene);
            box.name = name;
            box.transform.SetParent(parent, false);
            var label = box.GetComponentInChildren<TMP_Text>(true);
            label.text = text;
            label.gameObject.SetActive(true);
            box.SetActive(false);
            EditorUtility.SetDirty(label);
            return label;
        }

        static TMP_Text Label(Transform template, Transform parent, Scene scene, string name, string text)
        {
            var clone = UnityEngine.Object.Instantiate(template.gameObject);
            clone.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(clone, scene);
            clone.name = name;
            clone.transform.SetParent(parent, false);
            var label = clone.GetComponent<TMP_Text>();
            label.text = text;
            clone.SetActive(false);
            EditorUtility.SetDirty(label);
            return label;
        }

        static BoxCollider Zone(Transform skid, StringBuilder log) => Zone(skid, log, ZoneName, ZoneMargin);

        static BoxCollider Zone(Transform skid, StringBuilder log, string ZoneName, float ZoneMargin)
        {
            var found = skid.Find(ZoneName);
            var box = found != null ? found.GetComponent<BoxCollider>() : null;
            if (box != null) { log.AppendLine("Using the existing " + ZoneName + "."); return box; }
            if (found != null) UnityEngine.Object.DestroyImmediate(found.gameObject);

            var zone = new GameObject(ZoneName);
            zone.layer = 2;   // Ignore Raycast, like the other interaction zones
            zone.transform.SetParent(skid, false);
            zone.transform.localPosition = Vector3.zero;
            // level and in metres, whatever rotation / scale the model has
            zone.transform.rotation = Quaternion.Euler(0f, skid.eulerAngles.y, 0f);
            Vector3 parentScale = skid.lossyScale;
            zone.transform.localScale = new Vector3(1f / Mathf.Max(.0001f, Mathf.Abs(parentScale.x)), 1f / Mathf.Max(.0001f, Mathf.Abs(parentScale.y)),
                                                    1f / Mathf.Max(.0001f, Mathf.Abs(parentScale.z)));
            box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;

            // the skid mark's size, measured in the zone's own (metre) space
            bool any = false;
            Vector3 min = Vector3.zero, max = Vector3.zero;
            foreach (var filter in skid.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                var b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    var p = zone.transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (!any) { min = max = p; any = true; } else { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
                }
            }
            if (!any) { min = new Vector3(-1f, 0f, -2f); max = new Vector3(1f, 0f, 2f); log.AppendLine("WARNING: the skid mark has no mesh; the zone is a 4 m guess."); }
            var size = max - min;
            box.center = new Vector3((min.x + max.x) * .5f, min.y - .3f + ZoneHeight * .5f, (min.z + max.z) * .5f);
            box.size = new Vector3(size.x + ZoneMargin * 2f, ZoneHeight, size.z + ZoneMargin * 2f);
            log.AppendLine("Added " + ZoneName + ": " + box.size.ToString("F1") + " m (skid mark " + size.x.ToString("F1") + " x " + size.z.ToString("F1")
                           + " m + " + ZoneMargin + " m around it). Move / resize it in the Scene view.");
            return box;
        }

        static Material FeedMaterial(StringBuilder log)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FeedMaterialPath);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new Exception("The URP Unlit shader was not found.");
            material = new Material(shader) { name = "Screen_CameraFeed" };
            AssetDatabase.CreateAsset(material, FeedMaterialPath);
            AssetDatabase.SaveAssets();
            log.AppendLine("Created " + FeedMaterialPath);
            return material;
        }

        // Unity writes non-ASCII text in scene files as \uXXXX.
        static string YamlEscape(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text) sb.Append(c < 128 ? c.ToString() : "\\u" + ((int)c).ToString("X4"));
            return sb.ToString();
        }
    }

    // When the Steam logo is added to the ending's Textures folder, it is put on the ending screen by itself.
    public sealed class MidnightSteamLogoWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported.Concat(moved))
            {
                if (!path.StartsWith("Assets/SecretsThatBreathe/Environment/Ending/Textures/")) continue;
                if (!Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Contains("steam")) continue;
                EditorApplication.delayCall += () =>
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.Log("[Midnight] Steam logo added - stop Play Mode and use Tools > Midnight Scene > Fix: Thank You for Playing ... to put it on the ending screen."); return; }
                    MidnightGameplaySetup.MenuFixes5();
                };
                return;
            }
        }
    }
}
#endif
