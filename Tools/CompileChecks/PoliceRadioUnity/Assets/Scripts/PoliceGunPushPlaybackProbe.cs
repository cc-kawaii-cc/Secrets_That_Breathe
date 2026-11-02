#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SecretsThatBreathe;
[DefaultExecutionOrder(32000)]
public sealed class PoliceGunPushPlaybackProbe : MonoBehaviour
{
    public PoliceEncounterGestures gesture;
    public GameObject actor;
    public Camera camera;
    Transform left,right;
    float time;
    bool started,returning,done,radioStarted,radioReleased,radioChecked;
    bool[] captured=new bool[6];
    void Start(){var a=actor.GetComponentsInChildren<Animator>().First(x=>x.enabled);left=a.GetComponentsInChildren<Transform>().First(x=>x.name=="Hand.L");right=a.GetComponentsInChildren<Transform>().First(x=>x.name=="Hand.R");}
    void LateUpdate()
    {
        if(done)return;
        try
        {
            time+=Time.deltaTime;
            if(!captured[0]){Capture("GunPush_000.png");captured[0]=true;}
            if(time>=.1f && !started){gesture.SetGesture(PoliceEncounterGestures.Gesture.GunAndPush);started=true;}
            if(time>=.75f && !captured[1]){if(!gesture.gunGrip || Vector3.Distance(right.position,gesture.gunGrip.position)>.04f)throw new Exception("Real Play Mode: right wrist misses gun");Capture("GunPush_Grip.png");captured[1]=true;}
            if(time>=1.25f && !captured[2]){Capture("GunPush_Ready.png");captured[2]=true;}
            if(time>=1.48f && !captured[3])
            {
                if(actor.transform.InverseTransformPoint(left.position).z<.38f)throw new Exception("Real Play Mode: left palm does not shove forward");
                if(Vector3.Distance(right.position,gesture.gunGrip.position)>.04f)throw new Exception("Real Play Mode: grip lost during shove");
                Capture("GunPush_Contact.png");captured[3]=true;
                var p=camera.transform.position;var q=camera.transform.rotation;camera.transform.position=new Vector3(-3,1.55f,2.2f);camera.transform.LookAt(new Vector3(0,1.1f,.2f));Capture("GunPush_Side.png");camera.transform.SetPositionAndRotation(p,q);
            }
            if(time>=2 && !captured[4]){if(actor.transform.InverseTransformPoint(left.position).z>.3f)throw new Exception("Real Play Mode: push arm remains extended");Capture("GunPush_Recover.png");captured[4]=true;}
            if(time>=3.1f && !returning){gesture.SetGesture(PoliceEncounterGestures.Gesture.Talk);returning=true;}
            if(time>=4 && !captured[5])
            {
                if(Vector3.Distance(right.position,gesture.gunGrip.position)<.12f)throw new Exception("Real Play Mode: hand remains on gun");
                Capture("GunPush_240.png");captured[5]=true;
                File.AppendAllText("ArtSource/PoliceGunPush/Unity-validation.txt","\nPASS actual Play Mode: right-hand gun grip, left-hand preparation, one shove, recovery, both arms release\n");
            }
            if(time>=4.2f && !radioStarted){gesture.SetGesture(PoliceEncounterGestures.Gesture.Radio);radioStarted=true;}
            if(time>=5.7f && !radioChecked)
            {
                if(!gesture.radioGrip || Vector3.Distance(left.position,gesture.radioGrip.position)>.03f)throw new Exception("Radio regression: wrist misses grip after GunAndPush");
                radioChecked=true;
            }
            if(time>=6 && !radioReleased){gesture.SetGesture(PoliceEncounterGestures.Gesture.Talk);radioReleased=true;}
            if(time>=7.2f)
            {
                if(Vector3.Distance(left.position,gesture.radioGrip.position)<.3f)throw new Exception("Radio regression: arm remains held up");
                File.AppendAllText("ArtSource/PoliceGunPush/Unity-validation.txt","PASS actual Play Mode radio regression: grab and release still work after gun/push gesture\n");done=true;EditorApplication.Exit(0);
            }
        }
        catch(Exception error){done=true;File.AppendAllText("ArtSource/PoliceGunPush/Unity-validation.txt","\nFAIL actual Play Mode: "+error);Debug.LogException(error);EditorApplication.Exit(1);}
    }
    void Capture(string file)
    {
        var rt=new RenderTexture(1000,1100,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes("ArtSource/PoliceGunPush/"+file,image.EncodeToPNG());Destroy(image);RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Destroy(rt);
    }
}
#endif



