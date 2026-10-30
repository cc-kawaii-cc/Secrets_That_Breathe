using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConversationAssetBuild
{
    const string Root = "Assets/SecretsThatBreathe/Characters/";
    [Serializable] class RoleReport
    {
        public string role, prefab, clip;
        public float seconds, height, maximumVertexMotion, maximumRootMotion, loopError;
        public int triangles, skinnedRenderers;
        public bool controllerAssigned, looped, glassesFollowsHead, animatorPlaybackVerified;
    }
    [Serializable] class Report { public bool success; public string error; public RoleReport[] roles; public string pair; }
    public static void Build()
    {
        var report = new Report();
        try
        {
            var police = Install("Police", "ThaiPolice", 1.8f, Root + "ThaiPolice/IdleTalk/ThaiPolice_IdleTalk.prefab");
            var suit = Install("Suit", "NavySuitVillain", 1.85f, Root + "NavySuitVillain/NavySuitVillain.prefab");
            report.roles = new[] { police, suit };
            string pairFolder = Root + "Conversation";
            Directory.CreateDirectory(pairFolder); AssetDatabase.Refresh();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var pair = new GameObject("PoliceAndSuit_Conversation");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pair, preview);
                foreach (var data in report.roles)
                {
                    var actor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(data.prefab), preview);
                    actor.name = data.role; actor.transform.SetParent(pair.transform, false);
                    actor.transform.localPosition = new Vector3(data.role == "Police" ? -.7f : .7f, 0, 0);
                    actor.transform.localRotation = Quaternion.Euler(0, data.role == "Police" ? 90 : 270, 0);
                    actor.transform.localScale = Vector3.one;
                }
                report.pair = pairFolder + "/PoliceAndSuit_Conversation.prefab";
                PrefabUtility.SaveAsPrefabAsset(pair, report.pair);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            AssetDatabase.SaveAssets(); report.success = true;
        }
        catch (Exception error) { report.error = error.ToString(); Debug.LogException(error); }
        Directory.CreateDirectory("ArtSource/Conversation");
        File.WriteAllText("ArtSource/Conversation/unity-build-report.json", JsonUtility.ToJson(report, true));
        EditorApplication.Exit(report.success ? 0 : 1);
    }

    static RoleReport Install(string role, string folder, float height, string prefabPath)
    {
        string path = Root + folder + "/Conversation/" + role + "Conversation.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        Require(importer != null, "FBX importer missing: " + path);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true; importer.optimizeGameObjects = false;
        importer.importCameras = false; importer.importLights = false;
        var clips = importer.defaultClipAnimations;
        Require(clips.Length == 1, "Expected only the authored conversation take");
        foreach (var clip in clips)
        {
            clip.name = role + "Conversation"; clip.loopTime = true;
            clip.keepOriginalOrientation = true; clip.keepOriginalPositionY = true;
            clip.lockRootRotation = true; clip.lockRootHeightY = true; clip.lockRootPositionXZ = true;
        }
        importer.clipAnimations = clips; importer.SaveAndReimport();
        var motion = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => c.name == role + "Conversation");
        Require(Mathf.Abs(motion.length - 8f) < .04f, "Wrong clip duration");
        string controllerPath = Root + folder + "/Conversation/" + role + "Conversation.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Length > 0 ? machine.states[0].state : machine.AddState("Standing Conversation");
        state.motion = motion; state.speed = 1; machine.defaultState = state; EditorUtility.SetDirty(controller);
        var original = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Require(original != null, "Original prefab missing: " + prefabPath);
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // Preserve original root/components/children/file IDs, including scene references.
            var previous = root.transform.Find("ConversationModel");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var oldAnimator in root.GetComponentsInChildren<Animator>(true)) oldAnimator.enabled = false;
            var model = new GameObject("ConversationModel"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model, root.scene);
            var actor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.scene); actor.transform.SetParent(model.transform, false);
            model.name = "ConversationModel"; model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            var animator = actor.GetComponent<Animator>(); if (animator == null) animator = actor.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.enabled = true;
            Require(animator.avatar != null && animator.avatar.isValid, "Invalid Generic Avatar");
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.enabled = true; renderer.updateWhenOffscreen = true;
                var name = renderer.name.Contains("Sunglasses") ? "SunglassesBlack" : folder;
                var material = AssetDatabase.LoadAssetAtPath<Material>(Root + folder + "/" + name + ".mat");
                Require(material != null, "Missing source material: " + name);
                renderer.sharedMaterials = new[] { material };
            }
            motion.SampleAnimation(actor, 0f);
            var points = Points(model, false); var local = points.Select(p => root.transform.InverseTransformPoint(p)).ToArray();
            float min = local.Min(p => p.y), max = local.Max(p => p.y); float factor = height / (max - min);
            model.transform.localScale *= factor; model.transform.localPosition = Vector3.up * (-min * factor);
            // Real Unity skinning tests, not just clip metadata checks.
            motion.SampleAnimation(actor, 0f); var baseline = Points(model, false); var origin = model.transform.position;
            float actualHeight = baseline.Max(p => p.y) - baseline.Min(p => p.y); Debug.Log("MEASURE " + role + " raw=" + (max-min) + " factor=" + factor + " result=" + actualHeight + " actorScale=" + actor.transform.localScale + " wrapper=" + model.transform.localScale + " renderer=" + model.GetComponentInChildren<SkinnedMeshRenderer>().transform.lossyScale);
            Require(Mathf.Abs(actualHeight - height * root.transform.lossyScale.y) < .01f, "Height changed after skinning: " + actualHeight + " expected " + height * root.transform.lossyScale.y);
            float maxMotion = 0, rootMotion = 0;
            foreach (float time in new[] { .5f, 1.5f, 2f, 3f, 4.5f, 5.5f, 6f, 7f })
            {
                motion.SampleAnimation(actor, time); var sample = Points(model, false);
                for (int i = 0; i < baseline.Length; i++) maxMotion = Mathf.Max(maxMotion, Vector3.Distance(baseline[i], sample[i]));
                rootMotion = Mathf.Max(rootMotion, Vector3.Distance(origin, model.transform.position));
            }
            motion.SampleAnimation(actor, motion.length); var end = Points(model, false);
            float seam = 0; for (int i = 0; i < end.Length; i++) seam = Mathf.Max(seam, Vector3.Distance(end[i], baseline[i]));
            Require(maxMotion > .1f, "No visible skinned hand movement"); Require(rootMotion < .001f, "Character drifts while talking");
            Require(seam < .005f, "Visible loop seam");
            motion.SampleAnimation(actor, 0f);
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            bool glasses = role != "Suit" || renderers.Any(r => r.name.Contains("Sunglasses") && r.bones.Any(b => b != null && b.name == "Head"));
            Require(glasses, "Sunglasses are not bound to the head");
            int triangles = renderers.Sum(r => r.sharedMesh.triangles.Length / 3);
            Require(triangles == (role == "Police" ? 964 : 1072), "Unexpected triangle count");
            animator.Rebind(); animator.Update(0f);
            var playingStart = Points(model, false); animator.Update(role == "Police" ? 2f : 6f);
            var playingEnd = Points(model, false);
            float playbackMotion = playingStart.Select((point, index) => Vector3.Distance(point, playingEnd[index])).Max();
            Require(playbackMotion > .1f, "Animator controller does not play the gesture: " + playbackMotion);
            animator.Rebind(); animator.Update(0f);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return new RoleReport { role = role, prefab = prefabPath, clip = motion.name, seconds = motion.length,
                height = actualHeight / root.transform.lossyScale.y, maximumVertexMotion = maxMotion,
                maximumRootMotion = rootMotion, loopError = seam, triangles = triangles, skinnedRenderers = renderers.Length,
                controllerAssigned = true, looped = motion.isLooping, glassesFollowsHead = glasses, animatorPlaybackVerified = true };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static Vector3[] Points(GameObject model, bool includeGlasses)
    {
        return model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Where(r => includeGlasses || !r.name.Contains("Sunglasses")).SelectMany(renderer =>
            {
                var mesh = renderer.sharedMesh; var vertices = mesh.vertices; var weights = mesh.boneWeights;
                var matrices = renderer.bones.Select((bone, index) => bone.localToWorldMatrix * mesh.bindposes[index]).ToArray();
                return vertices.Select((vertex, index) => {
                    var weight = weights[index];
                    return matrices[weight.boneIndex0].MultiplyPoint3x4(vertex) * weight.weight0
                         + matrices[weight.boneIndex1].MultiplyPoint3x4(vertex) * weight.weight1
                         + matrices[weight.boneIndex2].MultiplyPoint3x4(vertex) * weight.weight2
                         + matrices[weight.boneIndex3].MultiplyPoint3x4(vertex) * weight.weight3;
                }).ToArray();
            }).ToArray();
    }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}




