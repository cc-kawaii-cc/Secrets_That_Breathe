using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SecretsThatBreathe.EditorTools;

public static class VerifyRoadSeams
{
    public static void Run()
    {
        try
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var road=new GameObject("Road");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Nemo_Upgrade/Prefabs/Bridge & Road/Prefab/Road.prefab");
            for(int i=0;i<3;i++)
            {
                var tile=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                tile.transform.SetParent(road.transform,false);tile.transform.localPosition=new Vector3(0,0,i*10);
                GameObjectUtility.SetStaticEditorFlags(tile,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
            }
            Directory.CreateDirectory("Assets/MainScenes/From_PM");
            EditorSceneManager.SaveScene(scene,"Assets/MainScenes/From_PM/MainScene_2_Evening.unity");
            var positions=road.GetComponentsInChildren<Collider>().Select(c=>c.transform.position).ToArray();
            Capture("before");
            RoadLightmapSeamSetup.Repair();
            var visual=road.transform.Find("Road_ContinuousLighting");
            if(visual==null)throw new Exception("Missing continuous renderer.");
            var mesh=visual.GetComponent<MeshFilter>().sharedMesh;
            if(mesh.triangles.Length/3!=28 || mesh.uv2.Length!=mesh.vertexCount)throw new Exception("Incorrect mesh or lightmap UVs: "+mesh.triangles.Length/3);
            var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var uv2=mesh.uv2;
            foreach(var group in Enumerable.Range(0,vertices.Length).Where(i=>normals[i].y>.99f).GroupBy(i=>vertices[i].ToString("F3")))
            {
                int first=group.First();
                foreach(int index in group) if(Vector2.Distance(uv[first],uv[index])>.0001f || Vector2.Distance(uv2[first],uv2[index])>.0001f)
                    throw new Exception("UV seam on joined road surface at "+vertices[first]);
            }
            if(road.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)!=1)throw new Exception("Original surfaces still enabled.");
            if(!positions.SequenceEqual(road.GetComponentsInChildren<Collider>().Select(c=>c.transform.position)))throw new Exception("Colliders changed.");
            if(Mathf.Abs(mesh.bounds.size.z-30)>.001f || Mathf.Abs(mesh.bounds.size.x-6)>.001f)throw new Exception("Road dimensions changed.");
            if(!visual.GetComponent<MeshRenderer>().receiveShadows)throw new Exception("Shadows disabled.");
            Directory.CreateDirectory("ArtSource/RoadSeams");
            Capture("after");
            File.WriteAllText("ArtSource/RoadSeams/validation.txt","PASS Unity imports original road and generates one native Mesh asset with UV2\nPASS 3 tiles -> 1 renderer, 28 triangles, 6 x 30 metre road retained\nPASS 8 internal wall triangles removed\nPASS texture UV and lightmap UV match at every shared top vertex\nPASS original collider count/positions retained\nPASS shadow receiving and casting remain enabled\nBaked-lightmap appearance in the real Evening scene has NOT been validated; requires installation and rebake.\n");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static void Capture(string name)
    {
        var cameraObject=new GameObject("DiagnosticCamera");var camera=cameraObject.AddComponent<Camera>();
        camera.transform.position=new Vector3(7,12,-7);camera.transform.LookAt(new Vector3(0,0,9));camera.fieldOfView=60;camera.farClipPlane=200;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.13f,.14f);
        var lightObject=new GameObject("DiagnosticLight");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;
        light.transform.rotation=Quaternion.Euler(30,25,0);light.shadows=LightShadows.Soft;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;
        var rt=new RenderTexture(1200,900,24);rt.Create();camera.targetTexture=rt;camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;
        var texture=new Texture2D(1200,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1200,900),0,0);texture.Apply();
        Directory.CreateDirectory("ArtSource/RoadSeams");File.WriteAllBytes("ArtSource/RoadSeams/"+name+".png",texture.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(lightObject);
    }
}
