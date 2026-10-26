using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class PhoneCallRequestCanvasSetup
    {
        private const string RequestText = "ฝากหาให้พี่ด้วยนะเข้ม";
        static PhoneCallRequestCanvasSetup()
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
            var ui = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PhoneCallDialogueUI>(true)).FirstOrDefault();
            if (ui == null || ui.tonRequestDialogue.IsValid || !ui.tonDialogue.IsValid || ui.sentences == null || ui.sentences.Length < 11) return;
            string previous = ui.sentences[9].text;
            int split = previous.IndexOf(RequestText, StringComparison.Ordinal);
            bool alreadySeparate = ui.sentences.Length == 12 && ui.sentences[10].text.Trim() == RequestText;
            if (split < 0 && !alreadySeparate) return;
            EditorSceneManager.SaveScene(scene, "Tools/DialogueAudit/LawyerRoom-before-split-ton-request.unity", true);
            var canvasObject = UnityEngine.Object.Instantiate(ui.tonDialogue.canvas.gameObject, ui.transform);
            canvasObject.name = "TonRequestDialogueCanvas";
            var motion = canvasObject.transform.Find("FlowingDialogue") as RectTransform;
            var sentence = motion.Find("Sentence").GetComponent<TMP_Text>();
            sentence.text = RequestText;
            sentence.maxVisibleCharacters = int.MaxValue;
            canvasObject.SetActive(false);
            ui.tonRequestDialogue = new DialogueCanvasView
            {
                canvas = canvasObject.GetComponent<Canvas>(), motionRoot = motion,
                speaker = motion.Find("Speaker").GetComponent<TMP_Text>(), sentence = sentence,
                normalizeFontBaseline = ui.tonDialogue.normalizeFontBaseline, layoutRevision = ui.tonDialogue.layoutRevision
            };
            ui.tonRequestSentence = sentence;
            if (alreadySeparate) ui.sentences[10] = sentence;
            else
            {
                ui.sentences[9].text = previous.Substring(0, split).TrimEnd();
                EditorUtility.SetDirty(ui.sentences[9]);
                var lines = ui.sentences.ToList();
                lines.Insert(10, sentence);
                ui.sentences = lines.ToArray();
            }
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("TON_REQUEST_CANVAS_OK: request has its own saved Canvas and appears before Khem's final reply.");
        }
    }
}
