#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class EveningChoiceClickAreaSetup
    {
        const string Request = "Tools/ChoiceLayout/fit-click-area.request";
        const string Result = "Tools/ChoiceLayout/fit-click-area.result.txt";
        static EveningChoiceClickAreaSetup() => EditorApplication.update += Apply;

        public static void Apply()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            const string path = "Assets/MainScenes/From_PM/MainScene_2_Evening.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            try
            {
                if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (!EditorSceneManager.SaveScene(scene, "Tools/ChoiceLayout/Backups/MainScene_2_Evening-before-fit-click-area.unity", true))
                    throw new Exception("Could not back up the current scene.");
                int count = 0;
                var measurements = new List<string>();
                foreach (var root in scene.GetRootGameObjects())
                foreach (var choice in root.GetComponentsInChildren<AuntMoneyChoiceUI>(true))
                {
                    if (root.name != "SeatingCanvas") throw new Exception("Choices must be under SeatingCanvas.");
                    Undo.RecordObjects(new UnityEngine.Object[] { choice, choice.acceptButton.transform,
                        choice.refuseButton.transform, choice.acceptLabel.rectTransform, choice.refuseLabel.rectTransform }, "Fit choice click areas");
                    choice.fitClickAreaToText = true;
                    choice.clickPadding = new Vector2(36f, 18f);
                    var activated = new List<GameObject>();
                    foreach (var label in new[] { choice.acceptLabel, choice.refuseLabel })
                        for (Transform t = label.transform; t != null; t = t.parent)
                            if (!t.gameObject.activeSelf) { activated.Add(t.gameObject); t.gameObject.SetActive(true); }
                    try { Canvas.ForceUpdateCanvases(); choice.FitClickAreas(); }
                    finally { foreach (var go in activated) go.SetActive(false); }
                    if (choice.acceptLabel.textInfo.characterCount == 0 || choice.refuseLabel.textInfo.characterCount == 0)
                        throw new Exception("Text geometry was not generated: " + choice.name);
                    foreach (var button in new[] { choice.acceptButton, choice.refuseButton })
                    {
                        var size = ((RectTransform)button.transform).rect.size;
                        if (size.x <= 72f || size.y <= 36f) throw new Exception("Invalid click area: " + button.name);
                        measurements.Add(choice.name + "/" + button.name + ": " + size);
                    }
                    count++;
                }
                if (count == 0) throw new Exception("No saved choices found.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save the scene.");
                File.WriteAllText(Result, "PASS: Fitted " + count + " choice sets; padding 36 horizontal / 18 vertical per side.\n" + string.Join("\n", measurements));
            }
            catch (Exception error) { File.WriteAllText(Result, "FAIL: " + error); Debug.LogException(error); }
            finally { if (!wasLoaded && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
#endif
