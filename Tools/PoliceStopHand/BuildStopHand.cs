using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Rendering;

// Asset authoring only. This file is not part of the game's runtime assembly.
public static class BuildStopHand
{
    const string Folder = "Assets/SecretsThatBreathe/Characters/ThaiPolice/StopHand";
    public static void Run()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    static void Build()
    {
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var actor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/SecretsThatBreathe/Characters/ThaiPolice/IdleTalk/ThaiPolice_IdleTalk.prefab"));
        var animator = actor.GetComponentsInChildren<Animator>().First(a => a.enabled && a.gameObject.activeInHierarchy);
        var idleController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/SecretsThatBreathe/Characters/LowPolyMaleSlowWalk/LowPolyMale_IdleTalk.controller");
        var idle = idleController.animationClips.First(c => !c.name.StartsWith("__preview__"));
        animator.runtimeAnimatorController = idleController; animator.Rebind(); animator.Update(0f);
        var bones = new[] { "UpperArm.L", "Forearm.L", "Hand.L" }
            .Select(n => animator.GetComponentsInChildren<Transform>(true).First(t => t.name == n)).ToArray();
        var clip = UnityEngine.Object.Instantiate(idle); clip.name = "Police_StopHand"; clip.frameRate = 60;
        string[] paths = bones.Select(b => AnimationUtility.CalculateTransformPath(b, animator.transform)).ToArray();
        var curves = paths.Select(_ => Enumerable.Range(0, 4).Select(x => new AnimationCurve()).ToArray()).ToArray();
        Quaternion[] last = null; float maxStep = 0f, palmDot = 0f;
        // Palm inward at rest, fingers in local +Y, as on the existing police rig.
        Vector3 inward = actor.transform.right * (Vector3.Dot(bones[2].position - actor.transform.position, actor.transform.right) >= 0 ? -1 : 1);
        Vector3 palmLocal = Vector3.ProjectOnPlane(Quaternion.Inverse(bones[2].rotation) * inward, Vector3.up).normalized;
        for (int frame = 0; frame <= 480; frame++)
        {
            float time = frame / 60f;
            idle.SampleAnimation(animator.gameObject, time % idle.length);
            var baseline = bones.Select(b => b.localRotation).ToArray();
            Transform upper = bones[0], lower = bones[1], hand = bones[2];
            float a = Vector3.Distance(upper.position, lower.position), bLen = Vector3.Distance(lower.position, hand.position);
            Vector3 wrist = upper.position + actor.transform.rotation * new Vector3(-.08f, -.04f, .40f);
            Vector3 direction = (wrist - upper.position).normalized;
            float distance = Mathf.Clamp(Vector3.Distance(wrist, upper.position), Mathf.Abs(a-bLen)+.001f, a+bLen-.005f);
            wrist = upper.position + direction * distance;
            Vector3 bend = Vector3.ProjectOnPlane(actor.transform.rotation * new Vector3(-.35f, -1f, -.08f), direction).normalized;
            float along = (a*a + distance*distance - bLen*bLen) / (2f*distance);
            Vector3 elbow = upper.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f,a*a-along*along));
            upper.rotation = Quaternion.FromToRotation(lower.position-upper.position, elbow-upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position-lower.position, wrist-lower.position) * lower.rotation;
            hand.rotation = Quaternion.FromToRotation(hand.up, actor.transform.up) * hand.rotation;
            Vector3 palm = Vector3.ProjectOnPlane(hand.rotation*palmLocal, actor.transform.up);
            hand.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(palm, actor.transform.forward, actor.transform.up), actor.transform.up) * hand.rotation;
            float amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time/.7f));
            for(int j=0;j<3;j++) bones[j].localRotation = Quaternion.Slerp(baseline[j],bones[j].localRotation,amount);
            if(last!=null) for(int j=0;j<3;j++) maxStep=Mathf.Max(maxStep,Quaternion.Angle(last[j],bones[j].localRotation));
            last=bones.Select(t=>t.localRotation).ToArray();
            if(frame==90)
            {
                palmDot=Vector3.Dot((hand.rotation*palmLocal).normalized, actor.transform.forward);
                if(palmDot<.95f || Vector3.Dot(hand.up,actor.transform.up)<.95f) throw new Exception("Stop palm must face player with fingers upward.");
                Capture(actor, "front"); Capture(actor,"side");
            }
            for(int j=0;j<3;j++) for(int c=0;c<4;c++) curves[j][c].AddKey(time,last[j][c]);
        }
        if(maxStep>12f) throw new Exception("Discontinuous stop motion: "+maxStep);
        for(int j=0;j<3;j++) for(int c=0;c<4;c++) clip.SetCurve(paths[j],typeof(Transform),"m_LocalRotation."+"xyzw"[c],curves[j][c]);
        clip.EnsureQuaternionContinuity(); var settings=AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime=false; AnimationUtility.SetAnimationClipSettings(clip,settings);
        AssetDatabase.CreateAsset(clip,Folder+"/Police_StopHand.anim");
        var controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/Police_PlayerTalk.controller");
        var machine=controller.layers[0].stateMachine;
        var normal=machine.AddState("Idle Talk"); normal.motion=idle; machine.defaultState=normal;
        var stop=machine.AddState("Stop Hand"); stop.motion=clip;
        AssetDatabase.SaveAssets();
        // Validate saved animation/controller binding against the actual active rig.
        animator.runtimeAnimatorController=controller; animator.Rebind(); animator.Update(0);
        animator.Play("Stop Hand",0,1.5f/clip.length); animator.Update(0);
        foreach(var path in paths) if(animator.transform.Find(path)==null) throw new Exception("Missing animation binding "+path);
        Capture(actor,"front"); Capture(actor,"side");
        var held=bones.Select(t=>t.localRotation).ToArray();
        animator.CrossFadeInFixedTime("Idle Talk",.65f,0);
        for(int frame=0;frame<45;frame++) animator.Update(1f/60f);
        if(animator.GetCurrentAnimatorStateInfo(0).shortNameHash!=Animator.StringToHash("Idle Talk")) throw new Exception("Stop hand did not return to Idle Talk.");
        Directory.CreateDirectory("ArtSource/PoliceStopHand");
        File.WriteAllText("ArtSource/PoliceStopHand/validation.txt", "PASS native AnimationClip + dedicated AnimatorController\nPASS 0.7s smooth raise, held palm, no root scale/position changes added\nPASS palm faces player dot="+palmDot+"\nPASS maximum arm change per 60Hz frame="+maxStep+" degrees\nPASS animation bindings exist on active PoliceConversation rig\n");
        UnityEngine.Object.DestroyImmediate(actor);
    }
    static void Capture(GameObject actor,string angle)
    {
        var cameraObject=new GameObject("StopPreviewCamera"); var camera=cameraObject.AddComponent<Camera>();
        camera.backgroundColor=new Color(.22f,.24f,.26f); camera.clearFlags=CameraClearFlags.SolidColor;
        camera.nearClipPlane=.01f; camera.farClipPlane=20; camera.fieldOfView=38;
        camera.transform.position=angle=="front" ? new Vector3(0,1.5f,3.4f) : new Vector3(-2.7f,1.5f,2.4f);
        camera.transform.LookAt(new Vector3(0,1.05f,0));
        var lightObject=new GameObject("PreviewLight"); var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.2f; light.transform.rotation=Quaternion.Euler(35,-150,0);
        RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=Color.gray;
        var target=new RenderTexture(800,900,24); target.Create(); camera.targetTexture=target; camera.Render();
        var previous=RenderTexture.active; RenderTexture.active=target;
        var image=new Texture2D(800,900,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,800,900),0,0); image.Apply();
        Directory.CreateDirectory("ArtSource/PoliceStopHand"); File.WriteAllBytes("ArtSource/PoliceStopHand/"+angle+".png",image.EncodeToPNG());
        RenderTexture.active=previous; camera.targetTexture=null; target.Release();
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(lightObject);
    }
}
