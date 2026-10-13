using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class DocumentBinderBuilder
{
    const string Root="Assets/SecretsThatBreathe/Props/DocumentBinder";
    public static void Build()
    {
        try {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/Models/DocumentBinder.fbx");
            importer.globalScale=1; importer.useFileScale=true; importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.isReadable=true; importer.importAnimation=false; importer.SaveAndReimport();
            var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/Binder_Leather.png");
            ti.maxTextureSize=2048; ti.wrapMode=TextureWrapMode.Clamp; ti.SaveAndReimport();
            Directory.CreateDirectory(Root+"/Materials");
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new Exception("URP Lit missing");
            var leather=new Material(shader){name="Binder_Leather"}; leather.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Binder_Leather.png")); leather.SetFloat("_Smoothness",.28f);
            var edge=new Material(shader){name="Binder_Edge"}; edge.SetColor("_BaseColor",new Color(.025f,.025f,.028f)); edge.SetFloat("_Smoothness",.22f);
            foreach(var m in new[]{leather,edge}) AssetDatabase.CreateAsset(m,Root+"/Materials/"+m.name+".mat");
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/DocumentBinder.fbx");
            var source=model.GetComponentInChildren<MeshFilter>(); var mesh=UnityEngine.Object.Instantiate(source.sharedMesh);
            mesh.name="DocumentBinder_SingleMesh";
            var bake=Matrix4x4.TRS(Vector3.zero,source.transform.localRotation,source.transform.localScale);
            var vertices=mesh.vertices; var normals=mesh.normals; var tangents=mesh.tangents;
            for(int i=0;i<vertices.Length;i++) vertices[i]=bake.MultiplyPoint3x4(vertices[i]);
            for(int i=0;i<normals.Length;i++) normals[i]=bake.MultiplyVector(normals[i]).normalized;
            for(int i=0;i<tangents.Length;i++) { var t=bake.MultiplyVector(new Vector3(tangents[i].x,tangents[i].y,tangents[i].z)).normalized; tangents[i]=new Vector4(t.x,t.y,t.z,tangents[i].w); }
            mesh.vertices=vertices; mesh.normals=normals; mesh.tangents=tangents; mesh.RecalculateBounds();
            var lightmapSettings=new UnwrapParam(); UnwrapParam.SetDefaults(out lightmapSettings); lightmapSettings.packMargin=.02f;
                if(!Unwrapping.GenerateSecondaryUVSet(mesh,lightmapSettings)) throw new Exception("Lightmap UV generation failed");
                AssetDatabase.CreateAsset(mesh,Root+"/Models/DocumentBinder_Mesh.asset");
            var go=new GameObject("DocumentBinder"); go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();
            // Resolve slots by name rather than assuming FBX keeps slot order.
            var sourceMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;
            var mapped=new Material[sourceMaterials.Length];
            for(int i=0;i<mapped.Length;i++) { string name=sourceMaterials[i].name; if(name.Contains("Paper")) throw new Exception("Paper must be absent"); mapped[i]=name.Contains("Edge")?edge:leather; }
            renderer.sharedMaterials=mapped;
            var collider=go.AddComponent<BoxCollider>(); collider.center=mesh.bounds.center; collider.size=mesh.bounds.size;
            if(go.GetComponentsInChildren<MeshRenderer>().Length!=1 || mesh.subMeshCount!=2) throw new Exception("Expected one renderer with two materials");
            if(Mathf.Abs(renderer.bounds.size.y-.32f)>.01f) throw new Exception("Unexpected model height: "+renderer.bounds.size);
            if(mesh.triangles.Length/3>32) throw new Exception("Triangle budget exceeded");
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/DocumentBinder.prefab");
            File.WriteAllText(Root+"/UnityVerification.txt","PASS: One GameObject, one MeshRenderer, two materials, no paper, BoxCollider. World dimensions: "+renderer.bounds.size+". Triangles: "+mesh.triangles.Length/3);
            UnityEngine.Object.DestroyImmediate(go); AssetDatabase.SaveAssets();
            Debug.Log("DOCUMENT_BINDER_BUILD_PASS"); EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}

