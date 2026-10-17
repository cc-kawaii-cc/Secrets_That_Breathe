using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // Plain constants: this type deliberately has no asset import callbacks.
    public static class LowPolyMaleSlowWalkAssets
    {
        public const string Folder = "Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk";
        public const string ModelPath = Folder + "/LowPolyMale_SlowWalk.fbx";
    }
    [InitializeOnLoad]
    public static class LowPolyMaleSlowWalkSetup
    {
        private static bool queued;
        static LowPolyMaleSlowWalkSetup() => Queue();
        internal static void Queue()
        {
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += () => { queued = false; Install(); };
        }
        [MenuItem("Tools/Secrets That Breathe/Set Up Slow Walking Character")]
        private static void Install()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var importer = AssetImporter.GetAtPath(LowPolyMaleSlowWalkAssets.ModelPath) as ModelImporter;
            if (importer == null) return;
            if (!importer.userData.Contains("slow-walk-rig-v1"))
            {
                // Set and reimport ONLY this new character, once. Existing settings are retained.
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.importCameras = false;
                importer.importLights = false;
                if (importer.clipAnimations.Length == 0)
                {
                    var clips = importer.defaultClipAnimations;
                    foreach (var clip in clips)
                    {
                        if (clip.name.Contains("SlowWalk_InPlace")) clip.name = "SlowWalk_InPlace";
                        else if (clip.name.Contains("SlowWalk_Forward")) clip.name = "SlowWalk_Forward";
                        clip.loopTime = true;
                        clip.keepOriginalOrientation = true;
                        clip.keepOriginalPositionY = true;
                        clip.lockRootRotation = true;
                        clip.lockRootHeightY = true;
                        clip.lockRootPositionXZ = clip.name == "SlowWalk_InPlace";
                    }
                    importer.clipAnimations = clips;
                }
                importer.userData += "slow-walk-rig-v1";
                importer.SaveAndReimport();
                Queue();
                return;
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(LowPolyMaleSlowWalkAssets.ModelPath);
            if (model == null) return;
            AnimationClip walk = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(LowPolyMaleSlowWalkAssets.ModelPath))
                // FBX imports also expose __preview__ clips; those do not inherit Loop Time.
                if (asset is AnimationClip clip && clip.name == "SlowWalk_InPlace") { walk = clip; break; }
            if (walk == null) return;
            string folder = LowPolyMaleSlowWalkAssets.Folder;
            string controllerPath = folder + "/LowPolyMale_SlowWalk.controller";
            var animation = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            bool repairedMotion = false;
            if (animation == null)
            {
                animation = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var machine = animation.layers[0].stateMachine;
                var state = machine.AddState("Slow Walk");
                state.motion = walk;
                machine.defaultState = state;
                EditorUtility.SetDirty(animation);
                AssetDatabase.SaveAssetIfDirty(animation);
            }
            else
            {
                // Clip IDs can change when the two FBX takes are named on first import.
                // Rebind the owned state to the current sub-asset instead of retaining a stale ID.
                foreach (var layer in animation.layers)
                    foreach (var child in layer.stateMachine.states)
                        if (child.state.name == "Slow Walk" && child.state.motion != walk)
                        {
                            child.state.motion = walk;
                            EditorUtility.SetDirty(child.state);
                            AssetDatabase.SaveAssetIfDirty(child.state);
                            repairedMotion = true;
                        }
            }
            if (repairedMotion || !SessionState.GetBool("SlowWalkActualClipLoopChecked", false))
            {
                ValidateImportedMotion(model, walk, animation);
                SessionState.SetBool("SlowWalkActualClipLoopChecked", true);
            }
            string prefabPath = folder + "/LowPolyMale_SlowWalk.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("LowPolyMale_SlowWalk");
                SceneManager.MoveGameObjectToScene(root, preview);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                instance.name = "Model";
                instance.transform.SetParent(root.transform, false);
                Bounds bounds = default;
                bool found = false;
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    if (!found) { bounds = renderer.bounds; found = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (found && bounds.size.y > .01f)
                {
                    float scale = 1.8f / bounds.size.y;
                    instance.transform.localScale *= scale;
                    instance.transform.localPosition = Vector3.up * (-bounds.min.y * scale);
                }
                var animator = instance.GetComponent<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.runtimeAnimatorController = animation;
                animator.applyRootMotion = false;
                var controller = root.AddComponent<CharacterController>();
                controller.height = 1.8f;
                controller.radius = .22f;
                controller.center = new Vector3(0f, .9f, 0f);
                controller.skinWidth = .02f;
                controller.stepOffset = .15f;
                root.AddComponent<SlowWalkingCharacter>();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        private static void ValidateImportedMotion(GameObject model, AnimationClip clip, AnimatorController controller)
        {
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                int missing = 0;
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    if (!string.IsNullOrEmpty(binding.path) && instance.transform.Find(binding.path) == null) missing++;
                var bones = instance.GetComponentsInChildren<Transform>(true);
                clip.SampleAnimation(instance, 0f);
                var rotations = new Quaternion[bones.Length];
                for (int i = 0; i < bones.Length; i++) rotations[i] = bones[i].localRotation;
                clip.SampleAnimation(instance, clip.length * .25f);
                int moving = 0;
                for (int i = 0; i < bones.Length; i++)
                    if (Quaternion.Angle(rotations[i], bones[i].localRotation) > .1f) moving++;
                var animator = instance.GetComponent<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                // Advance through a whole cycle and verify it continues moving in the second cycle.
                for (int i = 0; i < 105; i++) animator.Update(clip.length / 100f);
                for (int i = 0; i < bones.Length; i++) rotations[i] = bones[i].localRotation;
                for (int i = 0; i < 20; i++) animator.Update(clip.length / 100f);
                int movingAfterLoop = 0;
                for (int i = 0; i < bones.Length; i++)
                    if (Quaternion.Angle(rotations[i], bones[i].localRotation) > .1f) movingAfterLoop++;
                var state = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"[SlowWalk] Clip='{clip.name}', looping={clip.isLooping}, duration={clip.length:F2}s, animated bones={moving}, missing binding paths={missing}, state looping={state.loop}, cycles={state.normalizedTime:F2}, moving bones after first loop={movingAfterLoop}.");
                if (moving == 0 || missing > 0 || !clip.isLooping || !state.loop || movingAfterLoop == 0)
                    Debug.LogError("[SlowWalk] Imported animation loop validation failed.");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
    }
}
