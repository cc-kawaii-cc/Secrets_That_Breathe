using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class DialoguePresentationUpgrade
    {
        private const string Request = "Tools/DialogueAudit/apply.request";
        static DialoguePresentationUpgrade() => EditorApplication.update += Poll;
        private static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Move(Request, Request + ".consumed");
            try { Apply(); }
            catch (Exception error) { File.WriteAllText("Tools/DialogueAudit/upgrade-error.txt", error.ToString()); Debug.LogException(error); }
        }
        [MenuItem("Tools/Secrets That Breathe/Upgrade Single Speaker Dialogue")]
        public static void Apply()
        {
            var scene = SceneManager.GetSceneByPath("Assets/Tonpalm/Lawyer Room.unity");
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Tonpalm/Lawyer Room.unity");
            EditorSceneManager.SaveScene(scene, "Tools/DialogueAudit/LawyerRoom-before-live-upgrade.unity", true);
            var phone = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PhoneCallDialogueUI>(true)).Single();
            var visitor = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<AuntDialogueUI>(true)).Single();
            var sequence = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DrawerSearchScareSequence>(true)).Single();
            var choice = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<AuntMoneyChoiceUI>(true)).Single();

            foreach (var t in phone.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Colon").ToArray())
                UnityEngine.Object.DestroyImmediate(t.gameObject);
            phone.presentation.completedLineHold = 1f;
            visitor.presentation.completedLineHold = 1f;
            visitor.visitorName = "ลุง";
            foreach (var label in new[] { visitor.firstLine, visitor.secondLine, visitor.moneyLine, visitor.refuseLine,
                visitor.firstThought, visitor.secondThought, visitor.thirdThought, visitor.thankYouLine })
            {
                label.text = label.text.Replace("ป้า", "ลุง");
                EditorUtility.SetDirty(label);
            }
            if (sequence.auntObject != null) sequence.auntObject.name = "UncleVisitor";
            foreach (var image in choice.GetComponentsInChildren<Image>(true))
            {
                image.color = Color.clear;
                EditorUtility.SetDirty(image);
            }
            EditorUtility.SetDirty(phone);
            EditorUtility.SetDirty(visitor);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var report = new
            {
                phoneLines = phone.sentences.Length,
                callbackLines = phone.callBackSentences.Length,
                colonObjects = phone.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Colon"),
                phoneFont = phone.kemText.font.name,
                phoneFontSize = phone.kemText.fontSize,
                visitorFont = visitor.firstLine.font.name,
                visitorFontSize = visitor.firstLine.fontSize,
                visitorName = visitor.visitorName,
                npcName = sequence.auntObject != null ? sequence.auntObject.name : "missing",
                transparentChoices = choice.GetComponentsInChildren<Image>(true).All(i => i.color.a == 0f)
            };
            File.WriteAllText("Tools/DialogueAudit/scene-validation.txt", report.ToString());
            if (report.colonObjects != 0 || !report.transparentChoices || report.npcName == "missing")
                throw new Exception("Dialogue presentation validation failed.");
            Debug.Log("DIALOGUE_UPGRADE_OK " + report);
        }
    }
}
