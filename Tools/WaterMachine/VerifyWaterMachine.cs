using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using SecretsThatBreathe;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public sealed class VerifyWaterMachine : MonoBehaviour
{
    public TMP_FontAsset font;
    IEnumerator Start()
    {
        var camera = new GameObject("MainCamera").AddComponent<Camera>();
        camera.tag="MainCamera"; camera.transform.position=new Vector3(0,0,-2);
        var canvas = new GameObject("Canvas",typeof(RectTransform),typeof(Canvas));
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var template=new GameObject("ScenePrompt",typeof(RectTransform)); template.transform.SetParent(canvas.transform,false);
        var text=new GameObject("PromptText",typeof(RectTransform),typeof(TextMeshProUGUI)); text.transform.SetParent(template.transform,false);
        var label=text.GetComponent<TextMeshProUGUI>(); label.font=font; label.fontSize=30; label.text="[E] Sit";
        template.SetActive(false);
        var machine=GameObject.CreatePrimitive(PrimitiveType.Cube); machine.name="Water vending machine";
        machine.transform.localScale=new Vector3(1,2,.6f);
        var interaction=machine.AddComponent<WaterVendingMachineInteraction>();
        var hint=UnityEngine.Object.Instantiate(template,canvas.transform); hint.name="WaterMachinePrompt";
        var messagePanel=UnityEngine.Object.Instantiate(template,canvas.transform); messagePanel.name="WaterMachineMessage";
        var messageLabel=messagePanel.GetComponentInChildren<TMP_Text>(true);
        messageLabel.text="ตู้นี้เสียมานานแล้วนะ ทำไมไม่มีคนมาซ่อมสักที"; messageLabel.fontSize=52;
        var zone=new GameObject("InteractionZone").AddComponent<BoxCollider>();
        zone.transform.SetParent(machine.transform,false); zone.transform.localScale=new Vector3(1,.5f,1/.6f);
        zone.isTrigger=true; zone.size=new Vector3(5,3,5);
        interaction.hintPanel=hint; interaction.messagePanel=messagePanel;
        interaction.interactionZone=zone; interaction.playerBody=camera.transform;
        interaction.playerCamera=camera; interaction.messageDuration=.35f;
        InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        Application.runInBackground=true;
        var keyboard=InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
        InputSystem.EnableDevice(keyboard);
        yield return null;
        try
        {
            camera.transform.position=new Vector3(0,0,-5);
            Require(!interaction.TryInteract(),"out-of-range E was accepted");
            camera.transform.position=new Vector3(0,0,-2); camera.transform.rotation=Quaternion.Euler(0,180,0);
            Require(!interaction.TryInteract(),"looking away was accepted"); camera.transform.rotation=Quaternion.identity;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position=new Vector3(0,0,-1); wall.transform.localScale=new Vector3(2,2,.1f);
            Physics.SyncTransforms(); Require(!interaction.TryInteract(),"interaction through a wall was accepted");
            wall.SetActive(false); Physics.SyncTransforms();
            Require(interaction.CanInteract(),"nearby visible machine unavailable");
            zone.size=new Vector3(.5f,3,.5f); Require(!interaction.CanInteract(),"shrinking the zone did not change interaction reach");
            zone.size=new Vector3(5,3,5); Require(interaction.CanInteract(),"expanding the zone did not restore interaction reach");
            zone.center=new Vector3(4,0,0); Require(!interaction.CanInteract(),"moving the zone centre was ignored"); zone.center=Vector3.zero;
            zone.size=new Vector3(1,3,5); zone.transform.rotation=Quaternion.Euler(0,45,0);
            camera.transform.position=new Vector3(1.5f,0,-1.5f); Require(!interaction.IsPlayerInZone(),"rotated zone used an axis-aligned approximation");
            zone.transform.rotation=Quaternion.identity; zone.size=new Vector3(5,3,5); camera.transform.position=new Vector3(0,0,-2);
            Time.timeScale=0; Require(!interaction.TryInteract(),"interaction while paused was accepted"); Time.timeScale=1;
        }
        catch(Exception e) { Fail(e); yield break; }
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));
        InputSystem.Update(); Debug.Log("KEY_TEST: pressed="+keyboard.eKey.wasPressedThisFrame+" current="+(Keyboard.current==keyboard)+" ready="+interaction.CanInteract()); interaction.SendMessage("Update");
        yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        InputSystem.Update();
        yield return null;
        try
        {
            Require(interaction.HasInteracted,"E key did not activate the machine");
            var message=canvas.transform.Find("WaterMachineMessage").GetComponentInChildren<TMP_Text>(true);
            Require(message.text=="ตู้นี้เสียมานานแล้วนะ ทำไมไม่มีคนมาซ่อมสักที","incorrect Thai sentence");
            Require(message.font==font,"scene font was not preserved");
            Require(message.fontSize==52,"runtime overwrote the Canvas font size");
            foreach(char c in message.text) Require(c==' ' || font.HasCharacter(c,true,true),"missing Thai glyph: "+c);
            Require(!interaction.TryInteract(),"second interaction was accepted");
        }
        catch(Exception e) { Fail(e); yield break; }
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));
        InputSystem.Update(); interaction.SendMessage("Update");
        yield return new WaitForSeconds(.5f);
        try
        {
            Require(!canvas.transform.Find("WaterMachineMessage").gameObject.activeSelf,"message did not hide or repeat E restarted it");
            Require(!canvas.transform.Find("WaterMachinePrompt").gameObject.activeSelf,"prompt remained after use");
            interaction.enabled=false; interaction.enabled=true;
            Require(interaction.HasInteracted && !interaction.TryInteract(),"re-enabling reset the one-shot state");
            File.WriteAllText(Environment.GetEnvironmentVariable("WATER_REPORT_PATH"),"PASS: editable Canvas text/font/font-size preserved; box size, centre and rotation determine interaction eligibility; E activates once; subsequent E ignored; message hides; disable/re-enable stays consumed; out-of-zone, looking away, wall occlusion and paused input rejected; exact Thai sentence and Kanit Medium font preserved.\n");
            Debug.Log("WATER_MACHINE_PLAYMODE_PASS");
#if UNITY_EDITOR
            EditorApplication.Exit(0);
#endif
        }
        catch(Exception e) { Fail(e); }
    }
    static void Require(bool value,string reason) { if(!value) throw new Exception(reason); }
    static void Fail(Exception e)
    {
        Debug.LogException(e);
#if UNITY_EDITOR
        EditorApplication.Exit(1);
#endif
    }
#if UNITY_EDITOR
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var runner=new GameObject("Verification").AddComponent<VerifyWaterMachine>();
        runner.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Nemo_Upgrade/Font/Kanit/Kanit-Medium SDF.asset");
        EditorApplication.EnterPlaymode();
    }
#endif
}
