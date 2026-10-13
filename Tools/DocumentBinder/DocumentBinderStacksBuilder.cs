using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class DocumentBinderStacksBuilder
{
    const string Root="Assets/SecretsThatBreathe/Props/DocumentBinder";
    public static void Build()
    {
        try {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var single=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/DocumentBinder.prefab");
            if(!single) throw new Exception("Single binder prefab missing");
            string report="";
            foreach(int count in new[]{2,3}) {
                var stack=new GameObject("DocumentBinder_Stack"+count);
                for(int i=0;i<count;i++) {
                    var binder=(GameObject)PrefabUtility.InstantiatePrefab(single,stack.transform);
                    binder.name="Binder_"+(i+1);
                    binder.transform.localRotation=Quaternion.Euler(90,0,0);
                    binder.transform.localPosition=new Vector3(0,.0375f+i*.075f,-.16f);
                }
                var renderers=stack.GetComponentsInChildren<MeshRenderer>();
                var bounds=renderers[0].bounds;
                foreach(var r in renderers) bounds.Encapsulate(r.bounds);
                if(renderers.Length!=count || stack.GetComponentsInChildren<BoxCollider>().Length!=count) throw new Exception("Unexpected layer count");
                if(Mathf.Abs(bounds.min.y)>.0001f || Mathf.Abs(bounds.size.y-count*.075f)>.0001f) throw new Exception("Incorrect stack height or floor pivot: "+bounds);
                foreach(var filter in stack.GetComponentsInChildren<MeshFilter>()) if(filter.sharedMesh.triangles.Length/3!=28) throw new Exception("Unexpected mesh");
                var path=Root+"/DocumentBinder_Stack"+count+".prefab";
                PrefabUtility.SaveAsPrefabAsset(stack,path);
                var saved=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(saved.transform.childCount!=count) throw new Exception("Saved prefab layer count incorrect");
                report+="PASS: Stack"+count+", "+count+" nested binder prefabs, "+(count*28)+" triangles, floor pivot, height "+bounds.size.y+" m.\n";
                UnityEngine.Object.DestroyImmediate(stack);
            }
            File.WriteAllText(Root+"/StackVerification.txt",report);
            AssetDatabase.SaveAssets(); Debug.Log("DOCUMENT_BINDER_STACKS_PASS\n"+report); EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
