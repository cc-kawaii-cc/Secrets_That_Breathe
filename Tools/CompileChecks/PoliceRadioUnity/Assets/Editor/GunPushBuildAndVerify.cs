using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SecretsThatBreathe;

public static class PoliceGunPushBuildAndVerify
{
    const string Folder="Assets/SecretsThatBreathe/Characters/ThaiPolice/GunAndPush";
    const string Prefab="Assets/SecretsThatBreathe/Characters/ThaiPolice/IdleTalk/ThaiPolice_IdleTalk.prefab";
    const string Report="ArtSource/PoliceGunPush";
    static List<string> log=new List<string>();
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);log.Add("PASS "+message);}
    static Animator Active(GameObject root)=>root.GetComponentsInChildren<Animator>(true).First(a=>a.enabled && a.gameObject.activeInHierarchy);
    static Transform Bone(Animator a,string name)=>a.GetComponentsInChildren<Transform>(true).First(b=>b.name==name);
    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(Report);PlayerSettings.colorSpace=ColorSpace.Linear;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/HolsterPalette.png"));material.SetFloat("_Smoothness",.12f);
            AssetDatabase.CreateAsset(material,Folder+"/PoliceHolsteredGun.mat");
            var tex=(TextureImporter)AssetImporter.GetAtPath(Folder+"/HolsterPalette.png");tex.filterMode=FilterMode.Point;tex.mipmapEnabled=false;tex.textureCompression=TextureImporterCompression.Uncompressed;tex.SaveAndReimport();
            var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/PoliceHolsteredGun.fbx");importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;importer.importCameras=false;importer.importLights=false;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"PoliceHolsteredGun"),material);importer.SaveAndReimport();
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var animator=Active(root);animator.Rebind();animator.Update(0);var hips=Bone(animator,"Hips");
                foreach(var old in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="PoliceHolsteredGun").ToArray())
                    if(old.parent && old.parent.name=="Hips")UnityEngine.Object.DestroyImmediate(old.gameObject);
                var holster=new GameObject("PoliceHolsteredGun");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holster,root.scene);
                holster.transform.position=hips.position+root.transform.rotation*new Vector3(.19f,-.045f,.015f);holster.transform.rotation=root.transform.rotation;holster.transform.localScale=root.transform.lossyScale;holster.transform.SetParent(hips,true);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PoliceHolsteredGun.fbx"),root.scene);model.name="HolsterModel";model.transform.SetParent(holster.transform,false);
                foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=material;
                var grip=new GameObject("PoliceGunGrip");grip.transform.SetParent(holster.transform,false);grip.transform.localPosition=new Vector3(.015f,.095f,.012f);grip.transform.localRotation=Quaternion.Euler(0,0,165);
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);Check(model.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3)==268,"268-triangle holstered gun attached to Hips; shoulder radio retained");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            ValidateClip();AssetDatabase.SaveAssets();File.WriteAllLines(Report+"/Unity-validation.txt",log);
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));var playAnimator=Active(actor);playAnimator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_IdleTalk.controller");
            var host=new GameObject("RealPlayModeGunPushCheck");var gesture=host.AddComponent<PoliceEncounterGestures>();gesture.policeRoot=actor.transform;
            var probe=host.AddComponent<PoliceGunPushPlaybackProbe>();probe.gesture=gesture;probe.actor=actor;probe.camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;EditorApplication.EnterPlaymode();
        }
        catch(Exception e){log.Add("FAIL "+e);File.WriteAllLines(Report+"/Unity-validation.txt",log);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void ValidateClip()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var data=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(data,"Assets/GunPushVerificationRenderer.asset");var pipeline=UniversalRenderPipelineAsset.Create(data);AssetDatabase.CreateAsset(pipeline,"Assets/GunPushVerificationPipeline.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.54f,.56f,.60f);RenderSettings.ambientEquatorColor=new Color(.34f,.34f,.33f);RenderSettings.ambientGroundColor=new Color(.16f,.15f,.14f);
        var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.transform.rotation=Quaternion.Euler(35,-30,0);
        var fill=new GameObject("Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.45f;fill.transform.rotation=Quaternion.Euler(30,155,0);
        var camera=new GameObject("Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.02f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.27f,.29f,.30f);camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.gameObject.AddComponent<UniversalAdditionalCameraData>();camera.transform.position=new Vector3(2.8f,1.65f,4);camera.transform.LookAt(new Vector3(0,1.05f,0));
        foreach(string mode in new[]{"Standing","IdleTalk"})
        {
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));var animator=Active(actor);
            if(mode=="IdleTalk")animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_IdleTalk.controller");
            animator.Rebind();animator.Update(0);var host=new GameObject("GestureDriver");var gesture=host.AddComponent<PoliceEncounterGestures>();gesture.policeRoot=actor.transform;typeof(PoliceEncounterGestures).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(gesture,null);
            var bones=new[]{"UpperArm.L","Forearm.L","Hand.L","UpperArm.R","Forearm.R","Hand.R"}.Select(n=>Bone(animator,n)).ToArray();var head=Bone(animator,"Head");var left=bones[2];var right=bones[5];
            var curves=new Dictionary<string,AnimationCurve[]>();foreach(var bone in bones)curves[AnimationUtility.CalculateTransformPath(bone,animator.transform)]=Enumerable.Range(0,4).Select(_=>new AnimationCurve()).ToArray();
            Quaternion[] previous=null;float maxStep=0,release=0;string maxAt="";Vector3 root=actor.transform.position;
            for(int frame=0;frame<=240;frame++)
            {
                animator.Update(frame==0?0:1f/60);var baseline=bones.Select(b=>b.localRotation).ToArray();var baseHead=head.localRotation;
                if(frame==0)gesture.SetGesture(PoliceEncounterGestures.Gesture.GunAndPush);
                if(frame==180)gesture.SetGesture(PoliceEncounterGestures.Gesture.Talk);
                gesture.AdvanceGesture(frame==0?0:1f/60);
                if(previous!=null)for(int i=0;i<6;i++){float step=Quaternion.Angle(previous[i],bones[i].localRotation);if(step>maxStep){maxStep=step;maxAt=" frame "+frame+" bone "+bones[i].name;}}previous=bones.Select(b=>b.localRotation).ToArray();
                if(frame==38){Check(gesture.gunGrip && Vector3.Distance(right.position,gesture.gunGrip.position)<.035f,mode+" right hand contacts holstered grip before shove");Check(actor.transform.InverseTransformPoint(left.position).z<.35f,mode+" left hand prepares before extending");}
                if(frame==84){float reach=actor.transform.InverseTransformPoint(left.position).z;Check(reach>.38f,mode+" one palm push reaches forward: "+reach.ToString("F3")+" m");Check(Vector3.Distance(right.position,gesture.gunGrip.position)<.035f,mode+" right hand stays on the grip during shove");}
                if(frame==125){float error=0;for(int i=0;i<3;i++)error=Mathf.Max(error,Quaternion.Angle(baseline[i],bones[i].localRotation));Check(error<.05f,mode+" pushing arm recovers before dialogue ends");}
                if(frame==240)for(int i=0;i<6;i++)release=Mathf.Max(release,Quaternion.Angle(baseline[i],bones[i].localRotation));
                if(Vector3.Distance(root,actor.transform.position)>.0001f || Quaternion.Angle(baseHead,head.localRotation)>.001f)throw new Exception("Gesture changes actor root/head");
                if(mode=="IdleTalk")foreach(var bone in bones){var q=bone.localRotation;var c=curves[AnimationUtility.CalculateTransformPath(bone,animator.transform)];c[0].AddKey(frame/60f,q.x);c[1].AddKey(frame/60f,q.y);c[2].AddKey(frame/60f,q.z);c[3].AddKey(frame/60f,q.w);}
            }
            Check(maxStep<14,mode+" continuous arm motion: "+maxStep.ToString("F2")+" degrees/frame"+maxAt);Check(release<.05f,mode+" both arms fully return to base animation");
            if(mode=="IdleTalk")
            {
                var clip=new AnimationClip{name="Police_GunGrip_ChestPush_AndLower",frameRate=60};foreach(var entry in curves)for(int i=0;i<4;i++)clip.SetCurve(entry.Key,typeof(Transform),"m_LocalRotation."+"xyzw"[i],entry.Value[i]);clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);AssetDatabase.CreateAsset(clip,Folder+"/Police_GunGrip_ChestPush_AndLower.anim");Check(Mathf.Abs(clip.length-4)<.01f,"Four-second non-looping Generic both-arm clip saved");
            }
            UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(actor);
        }
    }
}




