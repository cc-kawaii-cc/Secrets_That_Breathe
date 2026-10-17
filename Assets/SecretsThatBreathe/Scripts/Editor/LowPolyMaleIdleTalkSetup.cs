using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // Configure only this explicitly named new FBX. No AssetPostprocessor or project-wide scan.
    [InitializeOnLoad]
    public static class LowPolyMaleIdleTalkSetup
    {
        private const string Folder = "Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk";
        private const string ModelPath = Folder + "/LowPolyMale_IdleTalk.fbx";
        private const string ImportTag = "idle-talk-rig-v1";
        static LowPolyMaleIdleTalkSetup() => EditorApplication.delayCall += Install;

        [MenuItem("Tools/Secrets That Breathe/Set Up Idle Talking Character")]
        private static void Install()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) return;
            if (!importer.userData.Contains(ImportTag))
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.importCameras = importer.importLights = false;
                var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
                foreach (var clip in clips)
                    if (clip.name.Contains("IdleTalk") || clip.takeName.Contains("IdleTalk"))
                    {
                        clip.name = "IdleTalk";
                        clip.loopTime = true;
                        clip.keepOriginalOrientation = true;
                        clip.keepOriginalPositionY = true;
                        clip.lockRootRotation = clip.lockRootHeightY = clip.lockRootPositionXZ = true;
                    }
                importer.clipAnimations = clips;
                importer.userData += ImportTag;
                importer.SaveAndReimport(); // Only LowPolyMale_IdleTalk.fbx.
                EditorApplication.delayCall += Install;
                return;
            }
            AnimationClip idle = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
                if (asset is AnimationClip clip && clip.name == "IdleTalk") { idle = clip; break; }
            if (idle == null) return; // Never bind Unity's non-looping __preview__ clip.
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) return;
            string controllerPath = Folder + "/LowPolyMale_IdleTalk.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var state = controller.layers[0].stateMachine.AddState("Idle Talk");
                state.motion = idle;
                controller.layers[0].stateMachine.defaultState = state;
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssetIfDirty(controller);
            }
            else
                foreach (var layer in controller.layers)
                    foreach (var child in layer.stateMachine.states)
                        if (child.state.name == "Idle Talk" && child.state.motion != idle)
                        {
                            child.state.motion = idle;
                            EditorUtility.SetDirty(child.state);
                            AssetDatabase.SaveAssetIfDirty(child.state);
                        }
            string prefabPath = Folder + "/LowPolyMale_IdleTalk.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("LowPolyMale_IdleTalk");
                SceneManager.MoveGameObjectToScene(root, preview);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                instance.name = "Model";
                instance.transform.SetParent(root.transform, false);
                // Reuse the walking prefab's authored scale. Baking skinned vertices here
                // can include FBX bone unit conversion and shrink the model a second time.
                var walkingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/LowPolyMale_SlowWalk.prefab");
                var walkingModel = walkingPrefab != null ? walkingPrefab.transform.Find("Model") : null;
                root.transform.localScale = walkingPrefab != null
                    ? walkingPrefab.transform.localScale : Vector3.one * 1.05f;
                instance.transform.localScale = walkingModel != null
                    ? walkingModel.localScale : Vector3.one * .40176782f;
                var bones = instance.GetComponentsInChildren<Transform>(true);
                var rotations = new Quaternion[bones.Length];
                var animator = instance.GetComponent<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                // Sample the real Animator past its first cycle to catch one-shot import mistakes.
                animator.Rebind();
                animator.Update(0f);
                for (int i = 0; i < 105; i++) animator.Update(idle.length / 100f);
                for (int i = 0; i < bones.Length; i++) rotations[i] = bones[i].localRotation;
                for (int i = 0; i < 20; i++) animator.Update(idle.length / 100f);
                int moving = 0;
                for (int i = 0; i < bones.Length; i++)
                    if (Quaternion.Angle(rotations[i], bones[i].localRotation) > .01f) moving++;
                var info = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"[IdleTalk] Clip={idle.name}, loop={idle.isLooping}, stateLoop={info.loop}, cycles={info.normalizedTime:F2}, movingBonesAfterLoop={moving}.");
                if (!idle.isLooping || !info.loop || moving == 0)
                    Debug.LogError("[IdleTalk] Loop validation failed.");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
    }
}
