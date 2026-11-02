#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SecretsThatBreathe;

// Validation project only; never installed into the game.
[DefaultExecutionOrder(32000)]
public sealed class PoliceRadioPlaybackProbe : MonoBehaviour
{
    public PoliceEncounterGestures gesture;
    public GameObject actor;
    public Camera camera;
    float elapsed;
    bool raised,lowered,done;
    bool[] captured=new bool[5];
    Transform hand;
    void Start()
    {
        var animator=actor.GetComponentsInChildren<Animator>().First(a=>a.enabled);
        hand=animator.GetComponentsInChildren<Transform>().First(t=>t.name=="Hand.L");
    }
    void LateUpdate()
    {
        if(done)return;
        try
        {
            elapsed+=Time.deltaTime;
            if(!captured[0]){Capture("Radio_000.png");captured[0]=true;}
            if(elapsed>=.1f && !raised){gesture.SetGesture(PoliceEncounterGestures.Gesture.Radio);raised=true;}
            if(elapsed>=.65f && !captured[1]){Capture("Radio_033.png");captured[1]=true;}
            if(elapsed>=1.6f && !captured[2])
            {
                if(!gesture.radioGrip || Vector3.Distance(hand.position,gesture.radioGrip.position)>.025f)throw new Exception("Play Mode wrist misses radio");
                Capture("Radio_090.png");captured[2]=true;
                var position=camera.transform.position;var rotation=camera.transform.rotation;
                camera.transform.position=new Vector3(-3,1.6f,3);camera.transform.LookAt(new Vector3(0,1.12f,0));Capture("Radio_Side.png");camera.transform.SetPositionAndRotation(position,rotation);
            }
            if(elapsed>=2.6f && !lowered){gesture.SetGesture(PoliceEncounterGestures.Gesture.Talk);lowered=true;Capture("Radio_155.png");}
            if(elapsed>=3.05f && !captured[3]){Capture("Radio_190.png");captured[3]=true;}
            if(elapsed>=3.9f && !captured[4])
            {
                if(Vector3.Distance(hand.position,gesture.radioGrip.position)<.3f)throw new Exception("Play Mode arm did not lower");
                Capture("Radio_240.png");captured[4]=true;
                File.AppendAllText("ArtSource/PoliceRadio/Unity-validation.txt","\nPASS actual Play Mode: raises, contacts the radio, holds, lowers; captures taken after LateUpdate\n");
                done=true;EditorApplication.Exit(0);
            }
        }
        catch(Exception error){done=true;File.AppendAllText("ArtSource/PoliceRadio/Unity-validation.txt","\nFAIL actual Play Mode: "+error);Debug.LogException(error);EditorApplication.Exit(1);}
    }
    void Capture(string name)
    {
        var rt=new RenderTexture(900,1100,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();camera.targetTexture=rt;camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes("ArtSource/PoliceRadio/"+name,image.EncodeToPNG());
        Destroy(image);RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Destroy(rt);
    }
}
#endif
