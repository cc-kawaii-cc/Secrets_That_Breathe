#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    // Plain constants: this type deliberately has no asset import callbacks.
    public static class SuitWalkAssets
    {
        public const string Folder = "Assets/SecretsThatBreathe/Characters/NavySuitVillain/Walk";
        public const string ModelPath = Folder + "/SuitWalk.fbx";
        public const string ControllerPath = Folder + "/SuitWalk.controller";
        public const string PrefabPath = Folder + "/NavySuitVillain_Walk.prefab";
        public const string ClipName = "SuitWalk";
        public const string ResultFile = "Tools/PoliceStationOpening/suitwalk.result.txt";
    }

    // Configures the new walking FBX (Generic rig, looping clip), then builds its controller and prefab once.
    // The original NavySuitVillain prefab and the Conversation model are not touched.
    [InitializeOnLoad]
    public static class SuitWalkSetup
    {
        private static bool queued;
        static SuitWalkSetup() => Queue();
        internal static void Queue()
        {
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += () => { queued = false; Install(); };
        }

        [MenuItem("Tools/Secrets That Breathe/Set Up Suit Walk Character")]
        private static void Install()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var importer = AssetImporter.GetAtPath(SuitWalkAssets.ModelPath) as ModelImporter;
            if (importer == null) return;
            if (!importer.userData.Contains("suit-walk-v1"))
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.optimizeGameObjects = false;
                importer.importCameras = false;
                importer.importLights = false;
                var clips = importer.defaultClipAnimations;
                if (clips.Length != 1) { Report("FAIL: expected exactly one take in SuitWalk.fbx, found " + clips.Length); return; }
                foreach (var clip in clips)
                {
                    clip.name = SuitWalkAssets.ClipName;
                    clip.loopTime = true;
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionY = true;
                    clip.lockRootRotation = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootPositionXZ = true;
                }
                importer.clipAnimations = clips;
                importer.userData += "suit-walk-v1";
                importer.SaveAndReimport();
                Queue();
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SuitWalkAssets.PrefabPath) != null) return;
            try { Build(); }
            catch (Exception error) { Report("FAIL\n" + error); Debug.LogException(error); }
        }

        private static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SuitWalkAssets.ModelPath);
            if (model == null) throw new InvalidOperationException("SuitWalk.fbx is not imported yet.");
            AnimationClip walk = null;
            Avatar avatar = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(SuitWalkAssets.ModelPath))
            {
                // FBX imports also expose __preview__ clips; those do not inherit Loop Time.
                if (asset is AnimationClip clip && clip.name == SuitWalkAssets.ClipName) walk = clip;
                else if (asset is Avatar a) avatar = a;
            }
            Require(walk != null, "Clip '" + SuitWalkAssets.ClipName + "' was not imported.");
            Require(avatar != null && avatar.isValid, "The Generic avatar is invalid.");
            Require(walk.isLooping, "The walk clip is not set to loop.");
            Require(Mathf.Abs(walk.length - 1.1f) < .05f, "Unexpected walk duration " + walk.length);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(SuitWalkAssets.ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(SuitWalkAssets.ControllerPath);
                var machine = controller.layers[0].stateMachine;
                var state = machine.AddState("Walk");
                state.motion = walk;
                machine.defaultState = state;
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssetIfDirty(controller);
            }
            var suitMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SecretsThatBreathe/Characters/NavySuitVillain/NavySuitVillain.mat");
            var glassesMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SecretsThatBreathe/Characters/NavySuitVillain/SunglassesBlack.mat");
            Require(suitMaterial != null && glassesMaterial != null, "The NavySuitVillain materials are missing.");

            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("NavySuitVillain_Walk");
                SceneManager.MoveGameObjectToScene(root, preview);
                var wrapper = new GameObject("Model");
                wrapper.transform.SetParent(root.transform, false);
                var actor = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                actor.name = "SuitWalkModel";
                actor.transform.SetParent(wrapper.transform, false);
                var animator = actor.GetComponent<Animator>();
                if (animator == null) animator = actor.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.avatar = avatar;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.sharedMaterials = new[] { renderer.name.Contains("Sunglasses") ? glassesMaterial : suitMaterial };
                    renderer.updateWhenOffscreen = true;
                }

                // Which way does the imported character face? Toes must be in front of the ankle along root +Z.
                walk.SampleAnimation(actor, 0f);
                Transform ankle = Find(actor.transform, "Foot.L"), toes = Find(actor.transform, "Toes.L");
                Require(ankle != null && toes != null, "Foot.L / Toes.L bones were not found.");
                Vector3 forward = root.transform.InverseTransformVector(toes.position - ankle.position);
                forward.y = 0f;
                bool facesBackwards = forward.z < 0f;
                if (facesBackwards) wrapper.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                var points = Points(actor, false);
                float min = points.Min(p => p.y), max = points.Max(p => p.y);
                // The Blender model is 4.255 units tall standing (= 1.85 m). The first frame is a slightly lowered walking pose,
                // so use the known standing height instead of this pose's height, and check it is in the expected range.
                Require(max - min > 3.9f && max - min < 4.3f, "Unexpected imported size of the walking model: " + (max - min));
                float factor = 1.85f / 4.255f;
                wrapper.transform.localScale = Vector3.one * factor;
                wrapper.transform.localPosition = Vector3.up * (-min * factor);

                // Real Unity skinning checks: height, floor, loop seam and visible leg movement.
                float lowest = float.MaxValue, highest = float.MinValue, travel = 0f;
                walk.SampleAnimation(actor, 0f);
                var first = Points(actor, false);
                foreach (float fraction in new[] { 0f, .125f, .25f, .375f, .5f, .625f, .75f, .875f })
                {
                    walk.SampleAnimation(actor, walk.length * fraction);
                    var sample = Points(actor, false);
                    lowest = Mathf.Min(lowest, sample.Min(p => p.y));
                    highest = Mathf.Max(highest, sample.Max(p => p.y));
                    for (int i = 0; i < sample.Length; i++) travel = Mathf.Max(travel, Vector3.Distance(first[i], sample[i]));
                }
                walk.SampleAnimation(actor, walk.length);
                var last = Points(actor, false);
                float seam = 0f;
                for (int i = 0; i < last.Length; i++) seam = Mathf.Max(seam, Vector3.Distance(first[i], last[i]));
                var renderers = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                bool glassesOnHead = renderers.Any(r => r.name.Contains("Sunglasses") && r.bones.Any(b => b != null && b.name == "Head"));
                int triangles = renderers.Sum(r => r.sharedMesh.triangles.Length / 3);
                Require(Mathf.Abs((highest - lowest) - 1.85f) < .12f, "Height changed while walking: " + (highest - lowest));
                Require(lowest > -.03f && lowest < .03f, "Feet are not on the floor: " + lowest);
                Require(travel > .3f, "The walk barely moves: " + travel);
                Require(seam < .01f, "Visible loop seam: " + seam);
                Require(glassesOnHead, "Sunglasses are not bound to the head.");
                Require(triangles == 1072, "Unexpected triangle count " + triangles);
                walk.SampleAnimation(actor, 0f);

                PrefabUtility.SaveAsPrefabAsset(root, SuitWalkAssets.PrefabPath);
                Report("PASS\nclip=" + walk.name + " length=" + walk.length.ToString("F3") + " looping=" + walk.isLooping +
                       "\nscale factor=" + factor.ToString("F4") + " facedBackwards=" + facesBackwards + " toesForward=" + forward.ToString("F3") +
                       "\nheight range=" + (highest - lowest).ToString("F3") + " lowest=" + lowest.ToString("F4") +
                       " maximum vertex travel=" + travel.ToString("F3") + " loop seam=" + seam.ToString("F5") +
                       "\ntriangles=" + triangles + " skinned renderers=" + renderers.Length + " glasses on Head=" + glassesOnHead +
                       "\nprefab=" + SuitWalkAssets.PrefabPath + "\ncontroller=" + SuitWalkAssets.ControllerPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            AssetDatabase.SaveAssets();
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }

        // CPU skinning of the current pose, in world space.
        private static Vector3[] Points(GameObject model, bool includeGlasses)
        {
            return model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => includeGlasses || !r.name.Contains("Sunglasses")).SelectMany(renderer =>
                {
                    var mesh = renderer.sharedMesh; var vertices = mesh.vertices; var weights = mesh.boneWeights;
                    var matrices = renderer.bones.Select((bone, index) => bone.localToWorldMatrix * mesh.bindposes[index]).ToArray();
                    return vertices.Select((vertex, index) =>
                    {
                        var weight = weights[index];
                        return matrices[weight.boneIndex0].MultiplyPoint3x4(vertex) * weight.weight0
                             + matrices[weight.boneIndex1].MultiplyPoint3x4(vertex) * weight.weight1
                             + matrices[weight.boneIndex2].MultiplyPoint3x4(vertex) * weight.weight2
                             + matrices[weight.boneIndex3].MultiplyPoint3x4(vertex) * weight.weight3;
                    }).ToArray();
                }).ToArray();
        }

        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        private static void Report(string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SuitWalkAssets.ResultFile));
            File.WriteAllText(SuitWalkAssets.ResultFile, text);
            Debug.Log("[SuitWalk] " + text);
        }
    }
}
#endif
