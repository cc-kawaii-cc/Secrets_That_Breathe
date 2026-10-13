using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class A4DocumentsBuilder
{
    const string Root="Assets/SecretsThatBreathe/Props/A4Documents";
    public static void Build()
    {
        try {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(Root+"/Materials");
            var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/A4Document_Reference.png");
            ti.maxTextureSize=1024; ti.alphaSource=TextureImporterAlphaSource.None; ti.wrapMode=TextureWrapMode.Clamp; ti.SaveAndReimport();
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            var printed=new Material(shader){name="A4_Printed"}; printed.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/A4Document_Reference.png")); printed.SetFloat("_Smoothness",.04f);
            var blank=new Material(shader){name="A4_Blank"}; blank.SetColor("_BaseColor",new Color(.96f,.96f,.95f)); blank.SetFloat("_Smoothness",.04f);
            AssetDatabase.CreateAsset(printed,Root+"/Materials/A4_Printed.mat"); AssetDatabase.CreateAsset(blank,Root+"/Materials/A4_Blank.mat");
            string report="";
            foreach(int count in new[]{1,10,20}) {
                string name=count==1?"A4Document_Single":"A4Documents_Stack"+count;
                var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/Models/"+name+".fbx");
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard; importer.importAnimation=false; importer.isReadable=true; importer.SaveAndReimport();
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+name+".fbx"); var source=model.GetComponentInChildren<MeshFilter>();
                var mesh=UnityEngine.Object.Instantiate(source.sharedMesh); mesh.name=name;
                var bake=source.transform.localToWorldMatrix; var verts=mesh.vertices; var norms=mesh.normals;
                for(int i=0;i<verts.Length;i++) verts[i]=bake.MultiplyPoint3x4(verts[i]);
                for(int i=0;i<norms.Length;i++) norms[i]=bake.MultiplyVector(norms[i]).normalized;
                mesh.vertices=verts; mesh.normals=norms; mesh.RecalculateBounds(); mesh.RecalculateTangents();
                var lightmapSettings=new UnwrapParam(); UnwrapParam.SetDefaults(out lightmapSettings); lightmapSettings.packMargin=.02f;
                if(!Unwrapping.GenerateSecondaryUVSet(mesh,lightmapSettings)) throw new Exception("Lightmap UV generation failed");
                AssetDatabase.CreateAsset(mesh,Root+"/Models/"+name+"_Mesh.asset");
                var go=new GameObject(name); go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>(); var imported=source.GetComponent<MeshRenderer>().sharedMaterials; var mapped=new Material[imported.Length];
                for(int i=0;i<mapped.Length;i++) mapped[i]=imported[i].name.Contains("Printed")?printed:blank;
                renderer.sharedMaterials=mapped;
                var collider=go.AddComponent<BoxCollider>(); collider.center=mesh.bounds.center; collider.size=mesh.bounds.size;
                if(mesh.subMeshCount!=2 || mesh.triangles.Length/3!=count*12) throw new Exception("Unexpected polygon/material count");
                if(Mathf.Abs(mesh.bounds.min.y)>.0001f || Mathf.Abs(mesh.bounds.size.y-count*.00018f)>.0001f) throw new Exception("Incorrect paper thickness or pivot");
                if(count==1 && (Mathf.Abs(mesh.bounds.size.x-.210f)>.0001f || Mathf.Abs(mesh.bounds.size.z-.297f)>.0001f)) throw new Exception("Incorrect A4 dimensions");
                PrefabUtility.SaveAsPrefabAsset(go,Root+"/"+name+".prefab");
                report+="PASS: "+name+", "+count+" sheets, "+mesh.triangles.Length/3+" triangles, one renderer, two materials, scale 1, floor pivot, BoxCollider.\n";
                UnityEngine.Object.DestroyImmediate(go);
            }
            File.WriteAllText(Root+"/UnityVerification.txt",report); AssetDatabase.SaveAssets(); Debug.Log("A4_DOCUMENTS_BUILD_PASS\n"+report); EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}

