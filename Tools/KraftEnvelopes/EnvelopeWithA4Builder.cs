using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class EnvelopeWithA4Builder
{
    const string Root="Assets/SecretsThatBreathe/Props/KraftEnvelopes";
    const string Name="KraftEnvelope_Open_WithA4";
    public static void Build()
    {
        try {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/Models/"+Name+".fbx");
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard; importer.importAnimation=false; importer.isReadable=true; importer.SaveAndReimport();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+Name+".fbx"); var source=model.GetComponentInChildren<MeshFilter>();
            var mesh=UnityEngine.Object.Instantiate(source.sharedMesh); mesh.name=Name;
            var bake=source.transform.localToWorldMatrix; var verts=mesh.vertices; var normals=mesh.normals;
            for(int i=0;i<verts.Length;i++) verts[i]=bake.MultiplyPoint3x4(verts[i]);
            for(int i=0;i<normals.Length;i++) normals[i]=bake.MultiplyVector(normals[i]).normalized;
            mesh.vertices=verts; mesh.normals=normals; mesh.RecalculateBounds(); mesh.RecalculateTangents();
            UnwrapParam settings; UnwrapParam.SetDefaults(out settings); settings.packMargin=.02f;
            if(!Unwrapping.GenerateSecondaryUVSet(mesh,settings)) throw new Exception("Lightmap UV generation failed");
            if(mesh.triangles.Length/3!=124 || mesh.uv2.Length!=mesh.vertexCount) throw new Exception("Invalid mesh or lightmap UV");
            AssetDatabase.CreateAsset(mesh,Root+"/Models/"+Name+"_Mesh.asset");
            var imported=source.GetComponent<MeshRenderer>().sharedMaterials; var materials=new Material[imported.Length];
            for(int i=0;i<materials.Length;i++) {
                string n=imported[i].name;
                string path=n.Contains("Printed")?"Assets/SecretsThatBreathe/Props/A4Documents/Materials/A4_Printed.mat":n.Contains("Blank")?"Assets/SecretsThatBreathe/Props/A4Documents/Materials/A4_Blank.mat":Root+"/Materials/KraftPaper.mat";
                materials[i]=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!materials[i]) throw new Exception("Material missing: "+path);
            }
            var go=new GameObject(Name); go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterials=materials;
            var collider=go.AddComponent<BoxCollider>(); collider.center=mesh.bounds.center; collider.size=mesh.bounds.size;
            if(Mathf.Abs(mesh.bounds.size.x-.235f)>.0001f || Mathf.Abs(mesh.bounds.min.y)>.0001f) throw new Exception("Incorrect scale or pivot");
            // The outer sheet edge must project 89.1 mm beyond the mouth.
            float actualExposure=Mathf.Max(Mathf.Abs(mesh.bounds.min.z),Mathf.Abs(mesh.bounds.max.z))-.1625f;
            if(Mathf.Abs(actualExposure-.0891f)>.0001f) throw new Exception("Incorrect paper projection: "+actualExposure);
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/"+Name+".prefab");
            File.WriteAllText(Root+"/EnvelopeWithA4Verification.txt","PASS: one mesh, 124 triangles, lightmap UV2, original paper/envelope materials, scale 1, floor pivot, BoxCollider. A4 210 x 297 mm, 89.1 mm (30%) exposed.");
            UnityEngine.Object.DestroyImmediate(go); AssetDatabase.SaveAssets(); Debug.Log("ENVELOPE_WITH_A4_PASS"); EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
