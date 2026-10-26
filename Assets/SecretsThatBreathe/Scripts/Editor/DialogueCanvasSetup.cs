using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class DialogueCanvasSetup
    {
        static DialogueCanvasSetup()
        {
            EditorApplication.delayCall += InstallLoaded;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += InstallLoaded;
        }

        private static void InstallLoaded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InstallLoaded;
                return;
            }
            var scene = SceneManager.GetSceneByPath("Assets/Tonpalm/Lawyer Room.unity");
            if (!scene.IsValid() || !scene.isLoaded) return;
            var phone = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PhoneCallDialogueUI>(true)).FirstOrDefault();
            var visitor = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<AuntDialogueUI>(true)).FirstOrDefault();
            bool phoneNeedsSetup = phone != null && (NeedsSetup(phone.khemDialogue) || NeedsSetup(phone.tonDialogue));
            bool visitorNeedsSetup = visitor != null && (NeedsSetup(visitor.khemDialogue) || NeedsSetup(visitor.visitorDialogue));
            if (!phoneNeedsSetup && !visitorNeedsSetup) return;
            // Preserve unsaved work from an already-open scene before the one-time migration.
            EditorSceneManager.SaveScene(scene, "Tools/DialogueAudit/LawyerRoom-before-authored-canvases.unity", true);
            if (phoneNeedsSetup)
            {
                if (!phone.khemDialogue.IsValid) phone.khemDialogue = Create(phone.transform, "KhemDialogueCanvas", phone.kemText,
                    phone.khemName, phone.presentation.khemColor, phone.presentation, "สวัสดีครับ");
                if (!phone.tonDialogue.IsValid) phone.tonDialogue = Create(phone.transform, "TonDialogueCanvas", phone.tonText,
                    phone.tonName, phone.presentation.otherSpeakerColor, phone.presentation, "เข้มเหรอ");
                Normalize(phone.khemDialogue);
                Normalize(phone.tonDialogue);
                EditorUtility.SetDirty(phone);
            }
            if (visitorNeedsSetup)
            {
                if (!visitor.khemDialogue.IsValid) visitor.khemDialogue = Create(visitor.transform, "KhemDialogueCanvas", visitor.firstLine,
                    visitor.khemName, visitor.presentation.khemColor, visitor.presentation, "คดีนี้ผมทำให้ฟรี");
                if (!visitor.visitorDialogue.IsValid) visitor.visitorDialogue = Create(visitor.transform, "UncleDialogueCanvas", visitor.firstLine,
                    visitor.visitorName, visitor.presentation.otherSpeakerColor, visitor.presentation, "ช่วยลุงด้วยนะ");
                Normalize(visitor.khemDialogue);
                Normalize(visitor.visitorDialogue);
                EditorUtility.SetDirty(visitor);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("DIALOGUE_AUTHORED_CANVASES_OK: name and sentence layout/color/font are saved Canvas objects.");
        }

        private static bool NeedsSetup(DialogueCanvasView view) => !view.IsValid || view.layoutRevision < 2;
        private static void Normalize(DialogueCanvasView view)
        {
            if (view.layoutRevision >= 2) return;
            foreach (var label in new[] { view.sentence, view.speaker })
            {
                var position = label.rectTransform.anchoredPosition;
                position.y = label.font.faceInfo.ascentLine * label.fontSize / label.font.faceInfo.pointSize;
                label.rectTransform.anchoredPosition = position;
                EditorUtility.SetDirty(label.rectTransform);
            }
            // Restore white after the old global Thai-text setup's grey conversion.
            view.sentence.color = Color.white;
            EditorUtility.SetDirty(view.sentence);
            view.layoutRevision = 2;
        }

        private static DialogueCanvasView Create(Transform parent, string objectName, TMP_Text template,
            string speakerName, Color speakerColor, FlowingDialogueSettings settings, string preview)
        {
            var canvas = new GameObject(objectName, typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            canvas.transform.SetParent(parent, false);
            var canvasRect = (RectTransform)canvas.transform;
            canvasRect.anchorMin = Vector2.zero; canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = canvasRect.offsetMax = Vector2.zero;
            var motion = new GameObject("FlowingDialogue", typeof(RectTransform)).GetComponent<RectTransform>();
            motion.SetParent(canvas.transform, false);
            motion.anchorMin = settings.screenAnchor;
            motion.anchorMax = settings.screenAnchor + Vector2.right * settings.widthFraction;
            motion.pivot = Vector2.zero;
            motion.sizeDelta = motion.anchoredPosition = Vector2.zero;
            var sentence = Object.Instantiate(template, motion);
            sentence.name = "Sentence";
            sentence.text = preview;
            sentence.color = settings.sentenceColor;
            sentence.alignment = TextAlignmentOptions.TopLeft;
            sentence.maxVisibleCharacters = int.MaxValue;
            sentence.raycastTarget = false;
            sentence.overflowMode = TextOverflowModes.Overflow;
            var bodyRect = sentence.rectTransform;
            bodyRect.anchorMin = Vector2.zero; bodyRect.anchorMax = Vector2.right;
            bodyRect.pivot = new Vector2(0f, 1f);
            bodyRect.sizeDelta = new Vector2(0f, 1000f);
            bodyRect.anchoredPosition = Vector2.zero;
            sentence.ForceMeshUpdate(true);
            float baseline = sentence.textInfo.characterCount > 0 ? sentence.textInfo.characterInfo[0].baseLine : -sentence.fontSize;
            bodyRect.anchoredPosition = new Vector2(0f, -baseline);
            var speaker = Object.Instantiate(template, motion);
            speaker.name = "Speaker";
            speaker.text = speakerName;
            speaker.color = speakerColor;
            speaker.alignment = TextAlignmentOptions.TopRight;
            speaker.maxVisibleCharacters = int.MaxValue;
            speaker.raycastTarget = false;
            speaker.overflowMode = TextOverflowModes.Overflow;
            var nameRect = speaker.rectTransform;
            nameRect.anchorMin = nameRect.anchorMax = Vector2.zero;
            nameRect.pivot = Vector2.one;
            nameRect.sizeDelta = new Vector2(240f, 1000f);
            speaker.ForceMeshUpdate(true);
            float space = Mathf.Max(0f, speaker.GetPreferredValues("ก ก").x - speaker.GetPreferredValues("กก").x);
            float nameBaseline = speaker.textInfo.characterCount > 0 ? speaker.textInfo.characterInfo[0].baseLine : baseline;
            nameRect.anchoredPosition = new Vector2(-space * 2f, -nameBaseline);
            canvas.gameObject.SetActive(false);
            return new DialogueCanvasView { canvas = canvas, motionRoot = motion, speaker = speaker, sentence = sentence };
        }
    }
}
