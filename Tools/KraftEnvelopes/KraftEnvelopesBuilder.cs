using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class KraftEnvelopesBuilder
{
    const string Root="Assets/SecretsThatBreathe/Props/KraftEnvelopes";
    public static void Build()
    {
        try {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(Root+"/Materials");
            var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/KraftEnvelope_Reference.png");
            ti.maxTextureSize=1024; ti.alphaSource=TextureImporterAlphaSource.None; ti.wrapMode=TextureWrapMode.Clamp; ti.SaveAndReimport();
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="KraftPaper"};
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/KraftEnvelope_Reference.png")); mat.SetFloat("_Smoothness",.06f);
            AssetDatabase.CreateAsset(mat,Root+"/Materials/KraftPaper.mat");
            string report="";
            foreach(string name in new[]{"KraftEnvelope_Closed","KraftEnvelope_Open","KraftEnvelopes_MessyStack"}) {
                var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/Models/"+name+".fbx");
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard; importer.importAnimation=false; importer.isReadable=true; importer.SaveAndReimport();
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+name+".fbx");
                var source=model.GetComponentInChildren<MeshFilter>(); var mesh=UnityEngine.Object.Instantiate(source.sharedMesh); mesh.name=name;
                var bake=source.transform.localToWorldMatrix;
                var verts=mesh.vertices; var norms=mesh.normals;
                for(int i=0;i<verts.Length;i++) verts[i]=bake.MultiplyPoint3x4(verts[i]);
                for(int i=0;i<norms.Length;i++) norms[i]=bake.MultiplyVector(norms[i]).normalized;
                mesh.vertices=verts; mesh.normals=norms; mesh.RecalculateBounds(); mesh.RecalculateTangents();
                var lightmapSettings=new UnwrapParam(); UnwrapParam.SetDefaults(out lightmapSettings); lightmapSettings.packMargin=.02f;
                if(!Unwrapping.GenerateSecondaryUVSet(mesh,lightmapSettings)) throw new Exception("Lightmap UV generation failed");
                AssetDatabase.CreateAsset(mesh,Root+"/Models/"+name+"_Mesh.asset");
                var go=new GameObject(name); go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=mat;
                var collider=go.AddComponent<BoxCollider>(); collider.center=mesh.bounds.center; collider.size=mesh.bounds.size;
                if(mesh.subMeshCount!=1 || mesh.triangles.Length/3>300) throw new Exception("Expected single-material low polygon mesh");
                if(mesh.bounds.size.x<.2f || mesh.bounds.size.x>.55f || Mathf.Abs(mesh.bounds.min.y)>.001f) throw new Exception("Incorrect scale or floor pivot: "+mesh.bounds);
                PrefabUtility.SaveAsPrefabAsset(go,Root+"/"+name+".prefab");
                report+="PASS: "+name+", "+mesh.triangles.Length/3+" triangles, one renderer/material, scale 1, floor pivot, BoxCollider.\n";
                UnityEngine.Object.DestroyImmediate(go);
            }
            File.WriteAllText(Root+"/UnityVerification.txt",report); AssetDatabase.SaveAssets();
            Debug.Log("KRAFT_ENVELOPES_BUILD_PASS\n"+report); EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}

