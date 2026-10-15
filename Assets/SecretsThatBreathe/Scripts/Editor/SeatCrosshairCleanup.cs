using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // Install the requested white crosshair without overwriting later Inspector edits.
    [InitializeOnLoad]
    public static class SeatCrosshairCleanup
    {
        static SeatCrosshairCleanup()
        {
            EditorApplication.delayCall += InstallDot;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += InstallDot;
        }

        private static void InstallDot()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var ui in root.GetComponentsInChildren<SeatPromptUI>(true))
                {
                    var dot = ui.transform.Find("CrosshairDot");
                    if (dot != null)
                    {
                        var graphic = dot.GetComponent<CrosshairDotGraphic>();
                        if (graphic != null && graphic.outlineVersion >= 1 && dot.GetComponent<UnityEngine.CanvasRenderer>() != null) continue;
                        Undo.RecordObject(dot.GetComponent<UnityEngine.RectTransform>(), "Resize white crosshair ring");
                        if (graphic != null) Undo.RecordObject(graphic, "Use white outline crosshair");
                        CrosshairDotGraphic.Install(ui);
                        if (graphic != null) EditorUtility.SetDirty(graphic);
                        EditorUtility.SetDirty(dot.GetComponent<UnityEngine.RectTransform>());
                        EditorSceneManager.MarkSceneDirty(scene);
                        continue;
                    }
                    var created = CrosshairDotGraphic.Install(ui);
                    Undo.RegisterCreatedObjectUndo(created, "Create white circle crosshair");
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
    }
}
