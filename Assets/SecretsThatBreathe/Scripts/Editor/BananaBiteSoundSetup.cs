using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SecretsThatBreathe
{
    // Makes eating a banana play freesound_community-cartoon-bite-39234 (1.2-1.9 s of it) on every bite (in every scene), by saving it in
    // Assets/SecretsThatBreathe/Audio/Resources/BananaSoundSettings.asset. Runs once by itself; after that the clip and its
    // volume can be changed on that asset in the Inspector (an asset that already has a clip is left alone).
    [InitializeOnLoad]
    public static class BananaBiteSoundSetup
    {
        private const string ClipName = "freesound_community-cartoon-bite-39234";
        private const string ParentFolder = "Assets/SecretsThatBreathe/Audio";
        private const string Folder = ParentFolder + "/Resources";
        private const string AssetPath = Folder + "/" + BananaSoundSettings.ResourceName + ".asset";
        private const string WarnedKey = "BananaBiteSound.Warned";

        static BananaBiteSoundSetup()
        {
            EditorApplication.delayCall += AutoInstall;
        }

        private static void AutoInstall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoInstall; return; }
            var settings = AssetDatabase.LoadAssetAtPath<BananaSoundSettings>(AssetPath);
            if (settings != null && settings.biteSound != null) { Decompressed(AssetDatabase.GetAssetPath(settings.biteSound)); return; }
            Install(false);
        }

        // Only part of the clip is played, which needs its samples: Load Type "Decompress On Load".
        private static void Decompressed(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;
            var settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.DecompressOnLoad && settings.preloadAudioData) return;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            Debug.Log("[Banana] " + path + ": Load Type set to Decompress On Load (so only 1.2-1.9 s of it can be played).");
        }

        [MenuItem("Tools/Secrets That Breathe/Banana Eating/Use cartoon bite sound")]
        private static void InstallFromMenu() => Install(true);

        private static void Install(bool fromMenu)
        {
            string path = AssetDatabase.FindAssets(ClipName + " t:AudioClip").Select(AssetDatabase.GUIDToAssetPath)
                                       .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == ClipName)
                       ?? AssetDatabase.FindAssets("t:AudioClip").Select(AssetDatabase.GUIDToAssetPath)
                                       .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == ClipName);
            var clip = path != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(path) : null;
            if (clip == null)
            {
                if (fromMenu || !SessionState.GetBool(WarnedKey, false))
                    Debug.LogWarning("[Banana] " + ClipName + " was not found in the project, so the bite still uses the generated sound.");
                SessionState.SetBool(WarnedKey, true);
                return;
            }
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(ParentFolder, "Resources");
            var settings = AssetDatabase.LoadAssetAtPath<BananaSoundSettings>(AssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<BananaSoundSettings>();
                AssetDatabase.CreateAsset(settings, AssetPath);
            }
            Decompressed(path);
            Undo.RecordObject(settings, "Banana bite sound");
            settings.biteSound = clip;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Banana] Bite sound PASS: " + path + " (saved in " + AssetPath + ").");
        }
    }
}
