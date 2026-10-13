using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class MiddleRoomThoughtSetup
    {
        static MiddleRoomThoughtSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        }

        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                var player = root.GetComponentInChildren<PlayerSeatController>(true);
                if (player == null) continue;
                bool hasFirst = false, hasDesk = false;
                foreach (var sceneRoot in scene.GetRootGameObjects())
                    foreach (var candidate in sceneRoot.GetComponentsInChildren<MiddleRoomThoughtTrigger>(true))
                    {
                        if (candidate.name == "MiddleRoomThoughtTrigger") hasFirst = true;
                        if (candidate.name == "FindDeskThoughtTrigger") hasDesk = true;
                    }
                if (hasFirst && hasDesk) return;
                var canvas = SeatPromptUI.FindOrCreate(scene);
                var trigger = MiddleRoomThoughtTrigger.Install(scene, player, canvas);
                var deskTrigger = MiddleRoomThoughtTrigger.InstallDeskThought(scene, player, canvas);
                if (!hasFirst) Undo.RegisterCreatedObjectUndo(trigger.gameObject, "Create middle-room thought trigger");
                if (!hasDesk) Undo.RegisterCreatedObjectUndo(deskTrigger.gameObject, "Create desk thought trigger");
                var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Champ&Kichzz/Fonts/PKFbasicDemo SDF.asset");
                if (font != null && trigger.messageText != null && (trigger.messageText.font == null || !trigger.messageText.font.HasCharacter('น')))
                    trigger.messageText.font = font;
                if (font != null && deskTrigger.messageText != null && (deskTrigger.messageText.font == null || !deskTrigger.messageText.font.HasCharacter('น')))
                    deskTrigger.messageText.font = font;
                EditorUtility.SetDirty(trigger);
                EditorUtility.SetDirty(deskTrigger);
                EditorSceneManager.MarkSceneDirty(scene);
                return;
            }
        }
    }
}
