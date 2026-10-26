using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsThatBreathe.Editor
{
    // Explicit, file-triggered Play mode check; does nothing during normal editing.
    [InitializeOnLoad]
    public static class DialoguePresentationValidation
    {
        private const string Key = "STB.DialogueValidation";
        private static double started;
        static DialoguePresentationValidation()
        {
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += StateChanged;
        }
        private static void Poll()
        {
            if (File.Exists("Tools/DialogueAudit/validate.request") && !EditorApplication.isPlayingOrWillChangePlaymode
                && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                File.Move("Tools/DialogueAudit/validate.request", "Tools/DialogueAudit/validate.request.consumed");
                SessionState.SetBool(Key, true);
                EditorApplication.ExecuteMenuItem("Window/General/Game");
                EditorApplication.EnterPlaymode();
            }
            if (started > 0 && EditorApplication.timeSinceStartup - started > 100)
            {
                File.WriteAllText("Tools/DialogueAudit/play-validation-error.txt", "Validation timed out. Inspect Editor.log.");
                started = 0;
                SessionState.SetBool(Key, false);
                EditorApplication.isPlaying = false;
            }
        }
        private static void StateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            started = EditorApplication.timeSinceStartup;
            var phone = UnityEngine.Object.FindFirstObjectByType<PhoneCallDialogueUI>(FindObjectsInactive.Include);
            var visitor = UnityEngine.Object.FindFirstObjectByType<AuntDialogueUI>(FindObjectsInactive.Include);
            phone.StartCoroutine(Run(phone, visitor));
        }
        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception("Dialogue validation: " + message);
        }
        private static IEnumerator Run(PhoneCallDialogueUI phone, AuntDialogueUI visitor)
        {
            yield return null;
            var call = UnityEngine.Object.FindFirstObjectByType<PhoneCallInteraction>(FindObjectsInactive.Include);
            call.enabled = false;
            var story = UnityEngine.Object.FindFirstObjectByType<DrawerSearchScareSequence>(FindObjectsInactive.Include);
            story.enabled = false;
            phone.Begin();
            float begin = Time.time;
            yield return phone.PlayLine(phone.sentences[0], true);
            Require(Time.time - begin >= phone.presentation.completedLineHold, "missing completed-line hold");
            var root = phone.khemDialogue.motionRoot;
            var body = root.Find("Sentence").GetComponent<TMP_Text>();
            var speaker = root.Find("Speaker").GetComponent<TMP_Text>();
            Require(body.font == phone.kemText.font && body.fontSize == phone.kemText.fontSize, "phone font changed");
            Require(speaker.color == phone.presentation.khemColor, "Khem colour mismatch");
            Require(!phone.conversationPanel.activeSelf, "old two-row conversation still visible");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Tools/DialogueAudit/phone-khem.png");
            root = phone.tonDialogue.motionRoot;
            body = phone.tonDialogue.sentence;
            speaker = phone.tonDialogue.speaker;
            bool done = false;
            phone.StartCoroutine(PlayLongLine(phone, () => done = true));
            yield return null;
            float firstY = root.anchoredPosition.y;
            while (!done) yield return null;
            Require(root.anchoredPosition.y > firstY + 10f, "multiline sentence did not flow upward");
            Require(speaker.text == phone.tonName && speaker.color == phone.presentation.otherSpeakerColor, "Ton identity mismatch");
            Require(!body.text.Contains(":"), "legacy phone colon column remains in sentence");
            Require(root.GetComponentsInChildren<TMP_Text>().Count(t => !string.IsNullOrEmpty(t.text)) == 2, "more than one speaker displayed");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Tools/DialogueAudit/phone-ton-multiline.png");
            yield return phone.PlayLine(phone.callBackSentences[3], false);
            Require(!body.text.Contains(":"), "legacy callback colon column remains in sentence");
            phone.Hide();
            visitor.ShowLine(0, 0);
            while (!visitor.ReadyToAdvance) yield return null;
            var visitorRoot = visitor.visitorDialogue.motionRoot;
            var visitorBody = visitorRoot.Find("Sentence").GetComponent<TMP_Text>();
            Require(visitorBody.font == visitor.firstLine.font && visitorBody.fontSize == visitor.firstLine.fontSize, "visitor font changed");
            Require(visitorRoot.Find("Speaker").GetComponent<TMP_Text>().text == "ลุง", "visitor name mismatch");
            Require(!visitorBody.text.Contains("ป้า"), "old visitor pronoun remains");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Tools/DialogueAudit/uncle-dialogue.png");
            visitor.HideAll();
            var choices = UnityEngine.Object.FindFirstObjectByType<AuntMoneyChoiceUI>(FindObjectsInactive.Include);
            choices.ShowChoices();
            Require(choices.GetComponentsInChildren<Image>(true).All(i => i.color.a == 0f), "choice background visible");
            Require(choices.acceptButton.IsInteractable() && choices.refuseButton.IsInteractable(), "choices no longer interactable");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Tools/DialogueAudit/money-choices.png");
            choices.HideChoices();
            File.WriteAllText("Tools/DialogueAudit/play-validation.txt", "PASS: one speaker; Khem/Ton colours; 1s hold; upward multiline motion; original fonts/sizes; uncle wording; transparent interactable choices.");
            SessionState.SetBool(Key, false);
            started = 0;
            EditorApplication.isPlaying = false;
        }
        private static IEnumerator PlayLongLine(PhoneCallDialogueUI phone, Action done)
        {
            yield return phone.PlayLine(phone.sentences[9], false);
            done();
        }
    }
}
