using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class DialogueTypingAudioSetup
    {
        static DialogueTypingAudioSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Install;
                return;
            }
            var scene = SceneManager.GetSceneByPath("Assets/Tonpalm/Lawyer Room.unity");
            if (!scene.IsValid() || !scene.isLoaded) return;
            var roots = scene.GetRootGameObjects();
            var phone = roots.SelectMany(g => g.GetComponentsInChildren<PhoneCallDialogueUI>(true)).FirstOrDefault();
            var visitor = roots.SelectMany(g => g.GetComponentsInChildren<AuntDialogueUI>(true)).FirstOrDefault();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/video-game-text-330163.mp3");
            if (phone == null || visitor == null || clip == null || visitor.typingAudioSetupRevision >= 1 || !visitor.visitorDialogue.IsValid) return;
            EditorSceneManager.SaveScene(scene, "Tools/DialogueAudit/LawyerRoom-before-dialogue-audio.unity", true);
            phone.presentation.typingAudio = Source(phone.transform, clip);
            visitor.presentation.typingAudio = Source(visitor.transform, clip);
            visitor.visitorName = "ลุงป้อง";
            visitor.visitorDialogue.speaker.text = "ลุงป้อง";
            visitor.typingAudioSetupRevision = 1;
            EditorUtility.SetDirty(phone);
            EditorUtility.SetDirty(visitor);
            EditorUtility.SetDirty(visitor.visitorDialogue.speaker);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("DIALOGUE_TYPING_AUDIO_OK: video-game-text-330163 assigned to phone and uncle dialogue; speaker renamed to ลุงป้อง.");
        }
        private static AudioSource Source(Transform parent, AudioClip clip)
        {
            var child = parent.Find("DialogueTypingAudio");
            if (child == null)
            {
                child = new GameObject("DialogueTypingAudio", typeof(AudioSource)).transform;
                child.SetParent(parent, false);
            }
            var source = child.GetComponent<AudioSource>();
            if (source == null) source = child.gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = .35f;
            EditorUtility.SetDirty(source);
            return source;
        }
    }
}
