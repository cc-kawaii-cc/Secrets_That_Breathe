using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // Repairs only TestBook. No global model import callbacks or project-wide reimport.
    [InitializeOnLoad]
    public static class TestBookAnimationSetup
    {
        private const string ModelPath = "Assets/Tonpalm/TestBook.fbx";
        private const string ControllerPath = "Assets/Tonpalm/TestBook.controller";
        static TestBookAnimationSetup()
        {
            EditorApplication.delayCall += Install;
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        }

        [MenuItem("Tools/Secrets That Breathe/Repair TestBook Animation")]
        private static void Install()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            AnimationClip open = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                if (!(asset is AnimationClip clip) || clip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
                if (clip.name == "OpenBook" || clip.name.EndsWith("|OpenBook", StringComparison.Ordinal))
                { open = clip; break; }
            }
            if (open == null)
            {
                Debug.LogWarning("[TestBook] Real OpenBook clip was not imported. Check the Animation tab of TestBook.fbx.");
                return;
            }
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var machine = controller.layers[0].stateMachine;
            AnimatorState state = null;
            foreach (var child in machine.states)
                if (child.state.name == "OpenBook") { state = child.state; break; }
            if (state == null) state = machine.AddState("OpenBook");
            if (state.motion != open)
            {
                state.motion = open;
                EditorUtility.SetDirty(state);
                AssetDatabase.SaveAssetIfDirty(state);
            }
            if (machine.defaultState != state)
            {
                machine.defaultState = state;
                EditorUtility.SetDirty(machine);
                AssetDatabase.SaveAssetIfDirty(machine);
            }
            AssetDatabase.SaveAssetIfDirty(controller);
            var scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = child.gameObject;
                    if (PrefabUtility.GetNearestPrefabInstanceRoot(go) != go ||
                        PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) != ModelPath) continue;
                    var animator = go.GetComponent<Animator>();
                    if (animator == null) animator = Undo.AddComponent<Animator>(go);
                    if (animator.runtimeAnimatorController == controller && animator.enabled && !animator.applyRootMotion) continue;
                    Undo.RecordObject(animator, "Connect TestBook OpenBook animation");
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.enabled = true;
                    if (animator.speed <= 0f) animator.speed = 1f;
                    EditorUtility.SetDirty(animator);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                    count++;
                }
            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                ValidateClip(open);
                Debug.Log($"[TestBook] Connected OpenBook ({open.length:F3}s) on {count} scene instance(s). Play starts the animation; position and scale are preserved.");
            }
        }

        private static void ValidateClip(AnimationClip clip)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null || clip.length <= 0f) return;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                var transforms = instance.GetComponentsInChildren<Transform>(true);
                var positions = new Vector3[transforms.Length];
                var rotations = new Quaternion[transforms.Length];
                clip.SampleAnimation(instance, 0f);
                for (int i = 0; i < transforms.Length; i++)
                { positions[i] = transforms[i].localPosition; rotations[i] = transforms[i].localRotation; }
                clip.SampleAnimation(instance, clip.length * .75f);
                int moving = 0;
                for (int i = 0; i < transforms.Length; i++)
                    if (Vector3.Distance(positions[i], transforms[i].localPosition) > .00001f ||
                        Quaternion.Angle(rotations[i], transforms[i].localRotation) > .01f) moving++;
                if (moving == 0) Debug.LogError("[TestBook] OpenBook does not move any imported transform.");
                else Debug.Log($"[TestBook] Validated real imported clip: {moving} moving transform(s).");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
    }
}
