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

public static class PoliceRadioBuildAndVerify
{
    const string Folder="Assets/SecretsThatBreathe/Characters/ThaiPolice/Radio";
    const string Prefab="Assets/SecretsThatBreathe/Characters/ThaiPolice/IdleTalk/ThaiPolice_IdleTalk.prefab";
    const string Report="ArtSource/PoliceRadio";
    static List<string> log=new List<string>();
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);log.Add("PASS "+message);}
    static Animator Active(GameObject root)=>root.GetComponentsInChildren<Animator>(true).First(a=>a.enabled && a.gameObject.activeInHierarchy);
    static Transform Bone(Animator a,string name)=>a.GetComponentsInChildren<Transform>(true).First(b=>b.name==name);
    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(Report); PlayerSettings.colorSpace=ColorSpace.Linear;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/RadioPalette.png"));material.SetFloat("_Smoothness",.17f);
            AssetDatabase.CreateAsset(material,Folder+"/PoliceRadio.mat");
            var tex=(TextureImporter)AssetImporter.GetAtPath(Folder+"/RadioPalette.png");tex.filterMode=FilterMode.Point;tex.mipmapEnabled=false;tex.textureCompression=TextureImporterCompression.Uncompressed;tex.SaveAndReimport();
            var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/PoliceRadio.fbx");importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;
            importer.importCameras=false;importer.importLights=false;importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"PoliceRadio"),material);importer.SaveAndReimport();
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var animator=Active(root);animator.Rebind();animator.Update(0);
                var chest=Bone(animator,"Chest");var neck=Bone(animator,"Neck");
                foreach(var old in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="PoliceShoulderRadio").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
                var radio=new GameObject("PoliceShoulderRadio");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(radio,root.scene);
                radio.transform.position=neck.position+root.transform.rotation*new Vector3(-.13f,-.11f,.095f);
                radio.transform.rotation=root.transform.rotation;radio.transform.localScale=root.transform.lossyScale;
                radio.transform.SetParent(chest,true);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PoliceRadio.fbx"),root.scene);
                model.transform.SetParent(radio.transform,false);
                foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=material;
                var grip=new GameObject("PoliceRadioGrip");grip.transform.SetParent(radio.transform,false);grip.transform.localPosition=new Vector3(-.047f,-.045f,.032f);
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
                Check(model.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3)==320,"320-triangle radio attached to the existing Chest bone");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            Validate();AssetDatabase.SaveAssets();File.WriteAllLines(Report+"/Unity-validation.txt",log);
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            var playAnimator=Active(actor);playAnimator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_IdleTalk.controller");
            var host=new GameObject("RealPlayModeRadioCheck");var gesture=host.AddComponent<PoliceEncounterGestures>();gesture.policeRoot=actor.transform;
            var probe=host.AddComponent<PoliceRadioPlaybackProbe>();probe.gesture=gesture;probe.actor=actor;probe.camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;
            EditorApplication.EnterPlaymode();
        }
        catch(Exception e){log.Add("FAIL "+e);File.WriteAllLines(Report+"/Unity-validation.txt",log);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Validate()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var rendererData=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(rendererData,"Assets/RadioVerificationRenderer.asset");
        var pipeline=UniversalRenderPipelineAsset.Create(rendererData);AssetDatabase.CreateAsset(pipeline,"Assets/RadioVerificationPipeline.asset");GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.54f,.56f,.60f);RenderSettings.ambientEquatorColor=new Color(.34f,.34f,.33f);RenderSettings.ambientGroundColor=new Color(.16f,.15f,.14f);
        var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.transform.rotation=Quaternion.Euler(35,-30,0);
        var fill=new GameObject("Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.45f;fill.transform.rotation=Quaternion.Euler(30,155,0);
        var camera=new GameObject("Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.0f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.27f,.29f,.30f);camera.nearClipPlane=.01f;camera.farClipPlane=20;
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>();camera.transform.position=new Vector3(2.2f,1.55f,4);camera.transform.LookAt(new Vector3(0,1.05f,0));
        foreach(string mode in new[]{"Standing","IdleTalk"})
        {
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            var animator=Active(actor);
            if(mode=="IdleTalk")animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_IdleTalk.controller");
            animator.Rebind();animator.Update(0);
            var host=new GameObject("GestureDriver");var gesture=host.AddComponent<PoliceEncounterGestures>();gesture.policeRoot=actor.transform;
            typeof(PoliceEncounterGestures).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(gesture,null);
            var upper=Bone(animator,"UpperArm.L");var forearm=Bone(animator,"Forearm.L");var hand=Bone(animator,"Hand.L");var head=Bone(animator,"Head");
            var bones=new[]{upper,forearm,hand};var curves=new Dictionary<string,AnimationCurve[]>();
            foreach(var bone in bones)curves[AnimationUtility.CalculateTransformPath(bone,animator.transform)]=Enumerable.Range(0,4).Select(_=>new AnimationCurve()).ToArray();
            Vector3 root=actor.transform.position;float maxStep=0;float previousTime=0;Quaternion[] previous=null;float contactError=0,releaseError=0;
            for(int frame=0;frame<=240;frame++)
            {
                float time=frame/60f;float delta=time-previousTime;previousTime=time;
                animator.Update(delta);
                var baseRotations=bones.Select(b=>b.localRotation).ToArray();Quaternion headRotation=head.localRotation;
                if(frame==0)gesture.SetGesture(PoliceEncounterGestures.Gesture.Radio);
                if(frame==156)gesture.SetGesture(PoliceEncounterGestures.Gesture.Talk);
                gesture.AdvanceGesture(delta);
                if(previous!=null)for(int i=0;i<3;i++)maxStep=Mathf.Max(maxStep,Quaternion.Angle(previous[i],bones[i].localRotation));
                previous=bones.Select(b=>b.localRotation).ToArray();
                if(frame==100)
                {
                    Check(gesture.radioGrip!=null,mode+" finds the radio grip automatically");
                    contactError=Vector3.Distance(hand.position,gesture.radioGrip.position);
                    Check(contactError<.025f,mode+" wrist reaches radio: "+contactError.ToString("F4")+" m");
                    Check(Vector3.Angle(hand.rotation*Vector3.up,gesture.radioGrip.up)<5,mode+" fingers orient up along the radio");
                    Check(forearm.position.y<upper.position.y,mode+" elbow remains below shoulder");
                }
                if(frame==240){for(int i=0;i<3;i++)releaseError=Mathf.Max(releaseError,Quaternion.Angle(baseRotations[i],bones[i].localRotation));}
                CheckSilent(Quaternion.Angle(headRotation,head.localRotation)<.001f,"Gesture changes head pose");
                CheckSilent(Vector3.Distance(root,actor.transform.position)<.0001f,"Gesture moves character root");
                if(mode=="IdleTalk")
                {
                    foreach(var bone in bones)
                    {
                        var q=bone.localRotation;var c=curves[AnimationUtility.CalculateTransformPath(bone,animator.transform)];
                        c[0].AddKey(time,q.x);c[1].AddKey(time,q.y);c[2].AddKey(time,q.z);c[3].AddKey(time,q.w);
                    }
                    if(new[]{0,33,90,155,190,240}.Contains(frame))Capture(camera,Report+"/Radio_"+frame.ToString("D3")+".png");
                    if(frame==100){var position=camera.transform.position;var rotation=camera.transform.rotation;camera.transform.position=new Vector3(-3,1.6f,3);camera.transform.LookAt(new Vector3(0,1.12f,0));Capture(camera,Report+"/Radio_Side.png");camera.transform.SetPositionAndRotation(position,rotation);}
                }
            }
            Check(maxStep<12,mode+" arm raises and lowers smoothly: max "+maxStep.ToString("F2")+" degrees/frame");
            Check(releaseError<.05f,mode+" hand fully returns to underlying animation: "+releaseError.ToString("F3")+" degrees");
            if(mode=="IdleTalk")
            {
                var clip=new AnimationClip{name="Police_Radio_GrabAndLower",frameRate=60};
                foreach(var entry in curves)for(int channel=0;channel<4;channel++)clip.SetCurve(entry.Key,typeof(Transform),"m_LocalRotation."+"xyzw"[channel],entry.Value[channel]);
                clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
                AssetDatabase.CreateAsset(clip,Folder+"/Police_Radio_GrabAndLower.anim");
                Check(Mathf.Abs(clip.length-4)<.01f,"Four-second non-looping Generic left-arm clip saved");
            }
            UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(actor);
        }
        Check(true,"Real Animator + runtime IK evaluated at 60 FPS; head and character root remain unchanged");
    }
    static void CheckSilent(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Capture(Camera camera,string path)
    {
        var rt=new RenderTexture(900,1100,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();camera.targetTexture=rt;camera.Render();camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
}

