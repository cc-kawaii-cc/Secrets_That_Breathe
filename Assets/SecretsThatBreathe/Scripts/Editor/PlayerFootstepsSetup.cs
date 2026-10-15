using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class PlayerFootstepsSetup
    {
        private static bool queued;
        static PlayerFootstepsSetup()
        {
            QueueInstall();
            EditorSceneManager.sceneOpened += (scene, mode) => QueueInstall();
            EditorApplication.hierarchyChanged += QueueInstall;
            EditorApplication.projectChanged += QueueInstall;
        }
        private static void QueueInstall()
        {
            if (queued || EditorApplication.isPlayingOrWillChangePlaymode) return;
            queued = true;
            EditorApplication.delayCall += () => { queued = false; Install(); };
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var player in root.GetComponentsInChildren<PlayerSeatController>(true))
                {
                    var footsteps = player.GetComponent<PlayerFootsteps>();
                    bool created = footsteps == null;
                    if (!created && footsteps.audioSource != null && footsteps.concreteClipsRevision >= 1
                        && footsteps.cadenceSettingsRevision >= 1) continue;
                    if (created) footsteps = Undo.AddComponent<PlayerFootsteps>(player.gameObject);
                    Undo.RecordObject(footsteps, "Configure player footsteps");
                    if (footsteps.cadenceSettingsRevision < 1)
                    {
                        footsteps.sprintStepDistance = Mathf.Clamp(footsteps.sprintStepDistance, .1f,
                            Mathf.Max(.1f, footsteps.walkStepDistance));
                        footsteps.cadenceSettingsRevision = 1;
                    }
                    if (footsteps.audioSource == null)
                    {
                        var child = player.transform.Find("FootstepAudio");
                        if (child == null)
                        {
                            var go = new GameObject("FootstepAudio");
                            Undo.RegisterCreatedObjectUndo(go, "Create player footstep audio");
                            go.transform.SetParent(player.transform, false);
                            child = go.transform;
                        }
                        var source = child.GetComponent<AudioSource>();
                        if (source == null)
                        {
                            source = Undo.AddComponent<AudioSource>(child.gameObject);
                            source.playOnAwake = false;
                            source.loop = false;
                            source.spatialBlend = 0f;
                            source.volume = 1f;
                            source.dopplerLevel = 0f;
                        }
                        footsteps.audioSource = source;
                    }
                    if (created)
                    {
                        var clips = new System.Collections.Generic.List<AudioClip>();
                        const string folder = "Assets/Nemo_Upgrade/Starter Assets/Runtime/ThirdPersonController/Character/Sfx/";
                        for (int i = 1; i <= 10; i++)
                        {
                            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(folder + "Player_Footstep_" + i.ToString("00") + ".wav");
                            if (clip != null) clips.Add(clip);
                        }
                        footsteps.footstepClips = clips.ToArray();
                    }
                    if (footsteps.concreteClipsRevision < 1)
                    {
                        var concrete = new System.Collections.Generic.List<AudioClip>();
                        for (int i = 1; i <= 10; i++)
                        {
                            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SecretsThatBreathe/Audio/Footsteps/Concrete/Concrete_Footstep_" + i.ToString("00") + ".wav");
                            if (clip != null) concrete.Add(clip);
                        }
                        if (concrete.Count == 10)
                        {
                            footsteps.footstepClips = concrete.ToArray();
                            footsteps.concreteClipsRevision = 1;
                        }
                    }
                    EditorUtility.SetDirty(footsteps);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(footsteps);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
    }
}
