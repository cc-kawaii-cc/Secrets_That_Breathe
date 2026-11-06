using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SecretsThatBreathe.EditorTools
{
    [InitializeOnLoad]
    public static class EveningSecondChoiceDialogueSetup
    {
        const string Folder = "Tools/ChoiceLayout";
        const string Request = Folder + "/second-choice-20261107.request";
        static double next;
        static EveningSecondChoiceDialogueSetup() => EditorApplication.update += Poll;
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene=EditorSceneManager.GetSceneByPath("Assets/MainScenes/From_PM/MainScene_2_Evening.unity");
            if (!scene.IsValid() || !scene.isLoaded) return;
            if (File.ReadAllText(Request).Trim()!="second-choice-dialogue-v1") return;
            File.Move(Request, Request+".consumed");
            try
            {
                var encounter=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<EveningPoliceEncounter>(true)).Single();
                string backup=Folder+"/before-second-choice-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";
                if(!EditorSceneManager.SaveScene(scene,backup,true)) throw new IOException("Could not back up current scene.");
                Undo.RecordObject(encounter,"Update second dialogue choices");
                SetText(encounter.secondChoices.acceptLabel,"แล้วนั่นเจ้าหน้าที่ด้วยไหมครับ");
                SetText(encounter.secondChoices.refuseLabel,"ขอโทษที่รบกวนครับ");
                var first=encounter.sunglassesReply;
                bool underCanvas=false;
                for(var parent=first.transform;parent!=null;parent=parent.parent) if(parent.name=="SeatingCanvas")underCanvas=true;
                if(!underCanvas)throw new InvalidOperationException("Dialogue lines must be under SeatingCanvas.");
                SetText(first,"แล้วคนข้างหลังนั่นเจ้าหน้าที่ด้วยหรอครับ");
                var second=MakeLine(first,"08_sunglassesReply2","มาถึงที่เกิดเหตุก่อนใคร คุยกันเจื้อยแจ้วเชียว");
                var third=MakeLine(first,"08_sunglassesReply3","ตำรวจที่นี่เขาเลือกปฏิบัติกันอย่างงี้หรอครับ");
                var serialized=new SerializedObject(encounter);
                var replies=serialized.FindProperty("sunglassesReplies");
                if(replies==null) throw new InvalidOperationException("Reload updated encounter script first.");
                replies.arraySize=3;
                replies.GetArrayElementAtIndex(0).objectReferenceValue=first;
                replies.GetArrayElementAtIndex(1).objectReferenceValue=second;
                replies.GetArrayElementAtIndex(2).objectReferenceValue=third;
                serialized.ApplyModifiedProperties();
                SetText(encounter.threatNarration,"(ไม่พูดอะไร เอามือนึงจับปืนและอีกมือดันตัวเข้มให้ถอยออกไป)");
                var evidence=encounter.evidenceReplies.Last();
                SetText(evidence,"แต่ผมมีสิทธิ์เก็บหลักฐานในที่เกิดเหตุเพื่อปกป้องสิทธ์ของลูกความผมนะ");
                encounter.evidenceReplies=new[]{evidence};
                var police=encounter.finalPoliceReplies;
                if(police.Length<3)throw new InvalidOperationException("Existing final police lines are missing.");
                SetText(police[0],"แต่ตามกฏหมายคุณไม่มีสิทธิ์บังคับให้ผมเปิดพื้นที่ให้คุณตามอำเภอใจ");
                SetText(police[1],"ถ้าอยากได้สิทธิ์เข้าถึงพื้นที่");
                var station=MakeLine(police[1],"13_finalPoliceStation","ไปคุยกับหัวหน้าผมที่สถานี");
                var last=police.Last();SetText(last,"ระหว่างนี้กรุณาถอยไปครับ");
                encounter.finalPoliceReplies=new[]{police[0],police[1],station,last};
                EditorUtility.SetDirty(encounter);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save dialogue changes.");
                File.WriteAllText(Folder+"/second-choice-20261107.result.txt","APPLIED\nFirst choice: 3 sequential Khem lines -> instant parenthesized narration + existing gun/push gesture.\nSecond choice: 1 Khem line -> 4 police lines.\nAll authored under SeatingCanvas; other current scene changes preserved.\nBackup: "+backup+"\n");
            }
            catch(Exception error)
            {
                File.WriteAllText(Folder+"/second-choice-20261107.result.txt","FAILED\n"+error);
                Debug.LogException(error);
            }
        }
        static void SetText(TMP_Text label,string value)
        {
            if(label==null)throw new InvalidOperationException("Missing saved text label.");
            Undo.RecordObject(label,"Edit dialogue text");label.text=value;EditorUtility.SetDirty(label);
        }
        static TMP_Text MakeLine(TMP_Text template,string name,string text)
        {
            var group=template.transform.parent;
            var existing=group.parent.Find(name);
            GameObject copy;
            if(existing!=null)copy=existing.gameObject;
            else
            {
                copy=UnityEngine.Object.Instantiate(group.gameObject,group.parent,false);
                copy.name=name;Undo.RegisterCreatedObjectUndo(copy,"Add saved dialogue sentence");
            }
            copy.SetActive(false);
            var label=copy.GetComponentInChildren<TMP_Text>(true);SetText(label,text);return label;
        }
    }
}
