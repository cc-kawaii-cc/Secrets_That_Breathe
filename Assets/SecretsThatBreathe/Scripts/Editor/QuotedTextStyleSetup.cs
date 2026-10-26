using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class QuotedTextStyleSetup
    {
        static QuotedTextStyleSetup()
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
            var visitor = roots.SelectMany(g => g.GetComponentsInChildren<AuntDialogueUI>(true)).FirstOrDefault();
            var search = roots.SelectMany(g => g.GetComponentsInChildren<DocumentSearchSequence>(true)).FirstOrDefault();
            if (visitor == null || search == null || search.returnToDeskMessage == null || visitor.quotedTextStyleRevision >= 2) return;
            if (visitor.quotedTextStyleRevision == 1)
            {
                EditorSceneManager.SaveScene(scene, "Tools/DialogueAudit/LawyerRoom-before-white-money-thoughts.unity", true);
                WhiteMoneyThoughts(visitor);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return;
            }
            EditorSceneManager.SaveScene(scene, "Tools/DialogueAudit/LawyerRoom-before-yellow-quotes.unity", true);
            var reference = search.returnToDeskMessage;
            int count = 0;
            foreach (var label in roots.SelectMany(g => g.GetComponentsInChildren<TMP_Text>(true)))
            {
                string text = (label.text ?? "").Trim();
                if (text.Length < 2 || text[0] != '"' || text[text.Length - 1] != '"') continue;
                label.font = reference.font;
                label.fontSharedMaterial = reference.fontSharedMaterial;
                label.enableAutoSizing = reference.enableAutoSizing;
                label.fontSize = reference.fontSize;
                label.color = reference.color;
                label.alignment = reference.alignment;
                label.margin = reference.margin;
                CopyRect(reference.transform.parent as RectTransform, label.transform.parent as RectTransform);
                CopyRect(reference.rectTransform, label.rectTransform);
                EditorUtility.SetDirty(label);
                EditorUtility.SetDirty(label.rectTransform);
                EditorUtility.SetDirty(label.transform.parent);
                count++;
            }
            if (!visitor.thoughtDialogue.IsValid) visitor.thoughtDialogue = CreateThoughtCanvas(visitor, reference);
            WhiteMoneyThoughts(visitor);
            EditorUtility.SetDirty(visitor);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("QUOTED_TEXT_STYLE_OK: objective quotes use the yellow reminder style; money thoughts are white and stationary.");
        }
        private static void WhiteMoneyThoughts(AuntDialogueUI visitor)
        {
            foreach (var label in new[] { visitor.firstThought, visitor.secondThought, visitor.thirdThought, visitor.thoughtDialogue.sentence })
            {
                if (label == null) continue;
                label.color = Color.white;
                EditorUtility.SetDirty(label);
            }
            visitor.moneyThoughtHoldDuration = 3f;
            visitor.quotedTextStyleRevision = 2;
            EditorUtility.SetDirty(visitor);
            Debug.Log("WHITE_MONEY_THOUGHTS_OK: three instant, stationary thoughts with a 3-second hold.");
        }
        private static void CopyRect(RectTransform from, RectTransform to)
        {
            if (from == null || to == null || from == to) return;
            to.anchorMin = from.anchorMin; to.anchorMax = from.anchorMax;
            to.pivot = from.pivot; to.sizeDelta = from.sizeDelta;
            to.anchoredPosition3D = from.anchoredPosition3D;
            to.localRotation = from.localRotation; to.localScale = from.localScale;
        }
        private static DialogueCanvasView CreateThoughtCanvas(AuntDialogueUI owner, TMP_Text template)
        {
            var canvas = new GameObject("QuotedThoughtCanvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            canvas.transform.SetParent(owner.transform, false);
            var rect = (RectTransform)canvas.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var panel = Object.Instantiate(template.transform.parent.gameObject, canvas.transform);
            panel.name = "FlowingDialogue";
            panel.SetActive(true);
            var sentence = panel.GetComponentInChildren<TMP_Text>(true);
            sentence.name = "Sentence";
            sentence.text = owner.firstThought.text;
            var speaker = Object.Instantiate(sentence, panel.transform);
            speaker.name = "Speaker";
            speaker.text = "";
            canvas.gameObject.SetActive(false);
            return new DialogueCanvasView { canvas = canvas, motionRoot = (RectTransform)panel.transform,
                sentence = sentence, speaker = speaker, normalizeFontBaseline = false, layoutRevision = 2 };
        }
    }
}
