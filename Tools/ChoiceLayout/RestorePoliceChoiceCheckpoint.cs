#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class RestorePoliceChoiceCheckpoint
    {
        const string Folder="Tools/ChoiceLayout";
        const string Request=Folder+"/restore-gun-checkpoint-v2.request";
        static RestorePoliceChoiceCheckpoint(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Move(Request,Folder+"/restore-gun-checkpoint-v2.consumed");
            var report=new List<string>();
            try
            {
                var scene=SceneManager.GetSceneByPath("Assets/MainScenes/From_PM/MainScene_2_Evening.unity");
                if(!scene.IsValid()||!scene.isLoaded)throw new Exception("Open the Evening scene first.");
                EditorSceneManager.SaveScene(scene,Folder+"/Backups/MainScene_2_Evening-before-original-choice-restore.unity",true);
                var seating=scene.GetRootGameObjects().Single(g=>g.name=="SeatingCanvas");
                var parent=seating.transform.Find("PoliceEncounter/Choices");
                if(!parent)throw new Exception("Saved Choices group missing.");
                report.Add("Choices parent was activeSelf="+parent.gameObject.activeSelf);
                Undo.RecordObject(parent.gameObject,"Restore gun/push checkpoint choice visibility");parent.gameObject.SetActive(true);EditorUtility.SetDirty(parent.gameObject);
                if(!parent.gameObject.activeInHierarchy)throw new Exception("Another parent is inactive.");
                var sets=parent.GetComponentsInChildren<AuntMoneyChoiceUI>(true);
                if(sets.Length!=2)throw new Exception("Expected two police choice sets.");
                foreach(var set in sets)if(!set.acceptButton||!set.refuseButton||!set.acceptLabel||!set.refuseLabel)throw new Exception("Incomplete bindings: "+set.name);
                var saved=JsonUtility.FromJson<ChoiceData>(File.ReadAllText(Folder+"/gun-checkpoint-choices.json"));
                foreach(var set in sets)
                {
                    long id=(long)GlobalObjectId.GetGlobalObjectIdSlow(set).targetObjectId;var state=saved.choices.Single(c=>c.id==id);
                    RestoreRect((RectTransform)set.transform,state.panel);RestoreRect((RectTransform)set.acceptButton.transform,state.firstButton);RestoreRect((RectTransform)set.refuseButton.transform,state.secondButton);
                    RestoreRect(set.acceptLabel.rectTransform,state.firstLabel);RestoreRect(set.refuseLabel.rectTransform,state.secondLabel);
                    Undo.RecordObjects(new UnityEngine.Object[]{set.acceptLabel,set.refuseLabel},"Restore pre-layout choice wording and alignment");
                    set.acceptLabel.text=state.accept.text;set.acceptLabel.alignment=(TextAlignmentOptions)state.accept.alignment;set.acceptLabel.margin=state.accept.margin;
                    set.refuseLabel.text=state.refuse.text;set.refuseLabel.alignment=(TextAlignmentOptions)state.refuse.alignment;set.refuseLabel.margin=state.refuse.margin;
                    EditorUtility.SetDirty(set.acceptLabel);EditorUtility.SetDirty(set.refuseLabel);
                    Undo.RecordObject(set.gameObject,"Restore original hidden-until-dialogue menu state");set.gameObject.SetActive(false);EditorUtility.SetDirty(set.gameObject);
                }
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed.");
                report.Add("Saved active Choices parent, matching the post-gun-animation checkpoint.");
                TestBothMenus(parent.gameObject,report);
                File.WriteAllLines(Folder+"/restore-gun-checkpoint-v2.result.txt",new[]{"PASS"}.Concat(report));
            }
            catch(Exception e){File.WriteAllLines(Folder+"/restore-gun-checkpoint-v2.result.txt",new[]{"FAIL",e.ToString()}.Concat(report));Debug.LogException(e);}
        }
        [Serializable] class ChoiceData { public ChoiceState[] choices; }
        [Serializable] class ChoiceState { public long id; public RectState panel,firstButton,secondButton,firstLabel,secondLabel;public TextState accept,refuse; }
        [Serializable] class RectState { public Vector2 anchorMin,anchorMax,position,size,pivot;public Vector3 scale; }
        [Serializable] class TextState { public string text;public int alignment;public Vector4 margin; }
        static void RestoreRect(RectTransform rect,RectState state)
        {
            Undo.RecordObject(rect,"Restore pre-layout choice position");rect.anchorMin=state.anchorMin;rect.anchorMax=state.anchorMax;rect.pivot=state.pivot;rect.anchoredPosition=state.position;rect.sizeDelta=state.size;rect.localScale=state.scale;EditorUtility.SetDirty(rect);
        }
        static void TestBothMenus(GameObject source,List<string> report)
        {
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject("Restored menu test",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(root,preview);
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;((RectTransform)root.transform).sizeDelta=new Vector2(1920,1080);
                var copy=UnityEngine.Object.Instantiate(source,root.transform,false);copy.name="Choices test copy";
                var sets=copy.GetComponentsInChildren<AuntMoneyChoiceUI>(true);
                foreach(var set in sets)set.gameObject.SetActive(false);
                var cameraGo=new GameObject("Menu test camera");SceneManager.MoveGameObjectToScene(cameraGo,preview);var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=540;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=new Color(.06f,.07f,.08f);camera.clearFlags=CameraClearFlags.SolidColor;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(preview);canvas.worldCamera=camera;
                foreach(var set in sets)
                {
                    set.ShowChoices();Canvas.ForceUpdateCanvases();
                    foreach(var label in new[]{set.acceptLabel,set.refuseLabel})
                    {
                        label.ForceMeshUpdate();
                        if(!label.gameObject.activeInHierarchy||!label.enabled||label.textInfo.characterCount==0||label.canvasRenderer.GetAlpha()<.99f)throw new Exception("Choice text not visible: "+set.name);
                    }
                    if(!set.acceptButton.IsInteractable()||!set.refuseButton.IsInteractable())throw new Exception("Buttons not interactable");
                    int clicks=0;set.acceptButton.onClick.AddListener(()=>clicks++);set.refuseButton.onClick.AddListener(()=>clicks++);
                    if(set.acceptButton.onClick.GetPersistentEventCount()!=0||set.refuseButton.onClick.GetPersistentEventCount()!=0)throw new Exception("Unexpected persistent callback on preview");
                    set.acceptButton.onClick.Invoke();set.refuseButton.onClick.Invoke();if(clicks!=2)throw new Exception("Choice click test failed");
                    Capture(camera,Folder+"/restored-"+set.name+".png");
                    set.HideChoices();if(set.gameObject.activeSelf)throw new Exception("Menu did not hide");
                    report.Add(set.name+": ShowChoices displays both labels; both button callbacks fire; HideChoices works. Tested on an isolated clone of the actual saved hierarchy.");
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
        }
        static void Capture(Camera camera,string path)
        {
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();camera.targetTexture=rt;camera.Render();camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
#endif
