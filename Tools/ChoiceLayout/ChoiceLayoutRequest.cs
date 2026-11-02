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
    // Explicit one-shot request only. Merely importing this script never edits a scene.
    [InitializeOnLoad]
    public static class ChoiceLayoutRequest
    {
        const string Folder="Tools/ChoiceLayout";
        const string Request=Folder+"/apply.request";
        static bool running;
        static ChoiceLayoutRequest(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(running || !File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)return;
            string id=File.ReadAllText(Request).Trim();File.Move(Request,Folder+"/"+id+".consumed");running=true;
            var report=new List<string>();Scene original=SceneManager.GetActiveScene();
            try
            {
                Directory.CreateDirectory(Folder+"/Backups");
                foreach(string path in new[]{"Assets/MainScenes/From_PM/MainScene_2_Evening.unity","Assets/MainScenes/From_PM/Lawyer Room.unity"})
                {
                    Scene scene=SceneManager.GetSceneByPath(path);bool wasLoaded=scene.IsValid() && scene.isLoaded;
                    if(!wasLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                    try
                    {
                        // Includes unsaved Inspector edits in an already open scene.
                        string backup=Folder+"/Backups/"+Path.GetFileNameWithoutExtension(path)+"-"+id+".unity";
                        if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Could not back up "+path);
                        var choices=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AuntMoneyChoiceUI>(true)).ToArray();
                        foreach(var choice in choices)
                        {
                            if(id.StartsWith("restore-",StringComparison.Ordinal))
                            {
                                Restore(choice,path);
                                report.Add(path+" / "+Hierarchy(choice.transform)+": original choice layout restored; current text/font/color preserved");
                            }
                            else
                            {
                                Apply(choice);
                                report.Add(path+" / "+Hierarchy(choice.transform)+": left-aligned, common X, lower-screen rows; fonts/text/colors preserved");
                                Preview(choice,id+"-"+choice.name);
                            }
                        }
                        EditorSceneManager.MarkSceneDirty(scene);
                        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save "+path);
                        report.Add("Saved "+choices.Length+" choice sets; backup "+backup);
                    }
                    finally{if(!wasLoaded)EditorSceneManager.CloseScene(scene,true);}
                }
                File.WriteAllLines(Folder+"/"+id+".result.txt",new[]{"PASS"}.Concat(report));
            }
            catch(Exception error){File.WriteAllLines(Folder+"/"+id+".result.txt",new[]{"FAIL",error.ToString()}.Concat(report));Debug.LogException(error);}
            finally{if(original.IsValid() && original.isLoaded)SceneManager.SetActiveScene(original);running=false;}
        }
        static string Hierarchy(Transform t)=>t.parent?Hierarchy(t.parent)+"/"+t.name:t.name;
        [Serializable] class RestoreData { public RestoreScene[] scenes; }
        [Serializable] class RestoreScene { public string path; public RestoreChoice[] choices; }
        [Serializable] class RestoreChoice
        {
            public long id;
            public RestoreRect panel,firstButton,secondButton,firstLabel,secondLabel;
            public RestoreText accept,refuse;
        }
        [Serializable] class RestoreRect { public Vector2 anchorMin,anchorMax,position,size,pivot; public Vector3 scale; }
        [Serializable] class RestoreText { public int alignment;public Vector4 margin; }
        static void Restore(AuntMoneyChoiceUI choice,string path)
        {
            var data=JsonUtility.FromJson<RestoreData>(File.ReadAllText(Folder+"/restore-layout.json"));
            long id=(long)GlobalObjectId.GetGlobalObjectIdSlow(choice).targetObjectId;
            var saved=data.scenes.Single(s=>s.path==path).choices.Single(c=>c.id==id);
            RestoreTransform((RectTransform)choice.transform,saved.panel);
            RestoreTransform((RectTransform)choice.acceptButton.transform,saved.firstButton);
            RestoreTransform((RectTransform)choice.refuseButton.transform,saved.secondButton);
            RestoreTransform(choice.acceptLabel.rectTransform,saved.firstLabel);
            RestoreTransform(choice.refuseLabel.rectTransform,saved.secondLabel);
            Undo.RecordObjects(new UnityEngine.Object[]{choice.acceptLabel,choice.refuseLabel},"Restore original choice alignment");
            choice.acceptLabel.alignment=(TextAlignmentOptions)saved.accept.alignment;choice.acceptLabel.margin=saved.accept.margin;
            choice.refuseLabel.alignment=(TextAlignmentOptions)saved.refuse.alignment;choice.refuseLabel.margin=saved.refuse.margin;
            EditorUtility.SetDirty(choice.acceptLabel);EditorUtility.SetDirty(choice.refuseLabel);
        }
        static void RestoreTransform(RectTransform rect,RestoreRect saved)
        {
            Undo.RecordObject(rect,"Restore original choice layout");
            rect.anchorMin=saved.anchorMin;rect.anchorMax=saved.anchorMax;rect.pivot=saved.pivot;
            rect.anchoredPosition=saved.position;rect.sizeDelta=saved.size;rect.localScale=saved.scale;
            EditorUtility.SetDirty(rect);
        }
        static void Apply(AuntMoneyChoiceUI choice)
        {
            if(!choice.acceptLabel || !choice.refuseLabel || !choice.acceptButton || !choice.refuseButton)throw new Exception("Incomplete choice bindings");
            var canvas=choice.GetComponentsInParent<Canvas>(true).LastOrDefault(c=>c.name=="SeatingCanvas");
            if(!canvas)throw new Exception("Choice must remain below SeatingCanvas: "+choice.name);
            RectTransform root=(RectTransform)canvas.transform,panel=(RectTransform)choice.transform,parent=(RectTransform)panel.parent;
            Undo.RecordObjects(new UnityEngine.Object[]{panel,choice.acceptButton.transform,choice.refuseButton.transform,choice.acceptLabel,choice.refuseLabel,choice.acceptLabel.rectTransform,choice.refuseLabel.rectTransform},"Align dialogue choices left");
            Vector3 left=parent.InverseTransformPoint(root.TransformPoint(new Vector3(root.rect.xMin+root.rect.width*.14f,root.rect.yMin+root.rect.height*.265f,0)));
            Vector3 right=parent.InverseTransformPoint(root.TransformPoint(new Vector3(root.rect.xMin+root.rect.width*.89f,root.rect.yMin+root.rect.height*.265f,0)));
            panel.anchorMin=new Vector2((left.x-parent.rect.xMin)/parent.rect.width,(left.y-parent.rect.yMin)/parent.rect.height);
            panel.anchorMax=new Vector2((right.x-parent.rect.xMin)/parent.rect.width,panel.anchorMin.y);
            panel.pivot=new Vector2(0,.5f);panel.anchoredPosition=Vector2.zero;panel.sizeDelta=new Vector2(0,150);
            Canvas.ForceUpdateCanvases();
            float width=panel.rect.width;
            float h1=Configure(choice.acceptLabel,(RectTransform)choice.acceptButton.transform,width);
            float h2=Configure(choice.refuseLabel,(RectTransform)choice.refuseButton.transform,width);
            var first=(RectTransform)choice.acceptButton.transform;var second=(RectTransform)choice.refuseButton.transform;
            first.anchoredPosition=new Vector2(0,56);second.anchoredPosition=new Vector2(0,56-h1-4);
            first.sizeDelta=new Vector2(0,h1);second.sizeDelta=new Vector2(0,h2);
            foreach(var obj in new UnityEngine.Object[]{panel,first,second,choice.acceptLabel,choice.refuseLabel,choice.acceptLabel.rectTransform,choice.refuseLabel.rectTransform})EditorUtility.SetDirty(obj);
            if(choice.acceptLabel.alignment!=TextAlignmentOptions.Left || choice.refuseLabel.alignment!=TextAlignmentOptions.Left)throw new Exception("Alignment check failed");
            if(choice.acceptLabel.rectTransform.pivot.x!=0 || choice.refuseLabel.rectTransform.pivot.x!=0)throw new Exception("Hover origin check failed");
        }
        static float Configure(TMP_Text label,RectTransform button,float width)
        {
            button.anchorMin=new Vector2(0,.5f);button.anchorMax=new Vector2(1,.5f);button.pivot=new Vector2(0,1);button.localScale=Vector3.one;
            var rect=label.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(0,.5f);rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;rect.localScale=Vector3.one;
            label.alignment=TextAlignmentOptions.Left;label.margin=Vector4.zero;
            // Keep the reference spacing for single lines; expand only if a saved
            // option wraps, so long choices never overlap the next click target.
            float height=label.GetPreferredValues(label.text,width,float.PositiveInfinity).y;
            return Mathf.Max(54,height);
        }
        static void Preview(AuntMoneyChoiceUI choice,string name)
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                var go=new GameObject("Choice layout preview",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(go,scene);
                var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(1920,1080);
                var cameraGo=new GameObject("Preview camera");SceneManager.MoveGameObjectToScene(cameraGo,scene);var camera=cameraGo.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=540;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.065f,.075f,.085f);camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
                canvas.worldCamera=camera;
                var labels=new[]{choice.acceptLabel,choice.refuseLabel};
                for(int i=0;i<2;i++)
                {
                    var source=labels[i];var textGo=new GameObject("Choice "+(i+1),typeof(RectTransform),typeof(TextMeshProUGUI));textGo.transform.SetParent(go.transform,false);var label=textGo.GetComponent<TextMeshProUGUI>();
                    label.font=source.font;label.fontSharedMaterial=source.fontSharedMaterial;label.fontSize=source.fontSize;label.fontStyle=source.fontStyle;label.color=source.color;label.richText=source.richText;label.text=source.text;label.alignment=TextAlignmentOptions.Left;
                    var r=label.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.14f,.265f);r.pivot=new Vector2(0,1);r.sizeDelta=((RectTransform)source.transform.parent).rect.size;r.anchoredPosition=((RectTransform)source.transform.parent).anchoredPosition;
                    label.ForceMeshUpdate();
                }
                Canvas.ForceUpdateCanvases();var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();camera.targetTexture=rt;camera.Render();camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
                var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
#endif

