using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecretsThatBreathe.Phone.Editor
{
    public static class BlankPhoneBuilder
    {
        public const string Root = "Assets/SecretsThatBreathe/Props/Phone";
        public const float ScreenWidth = .072f;
        public const float ScreenHeight = ScreenWidth * 1844f / 853f;

        [MenuItem("Tools/Phone/Build Blank Phone")]
        public static void Build()
        {
            foreach (var folder in new[] { "Models", "Materials", "Prefabs", "Preview", "Textures" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var frame = Material("Frame_Graphite", new Color(.24f,.26f,.28f), .75f, .65f);
            var back = Material("Back_Plain", new Color(.12f,.13f,.14f), .15f, .45f);
            var bezel = Material("Bezel_Black", new Color(.018f,.022f,.028f), .1f, .6f);
            var screen = ScreenMaterial("Screen_Flashlight_Off", "Phone_Flashlight_Off.png");
            var screenOn = ScreenMaterial("Screen_Flashlight_On", "Phone_Flashlight_On.png");
            var lens = Material("Lens_Dark", new Color(.025f,.04f,.05f), .3f, .85f);
            var led = Material("Flash_LED", new Color(.85f,.85f,.79f), 0, .5f);
            var phone = new GameObject("Phone_Blank");
            try
            {
                // Metres, portrait +Y, screen faces +Z. Origin is the centre of the device.
                Part(phone, "Body_Frame", RoundedSolid(.078f,.1668f,.009f,.010f,.0012f), frame);
                Part(phone, "Back", RoundedFace(.075f,.1638f,.0085f,-.0048f,false), back);
                Part(phone, "Front_Bezel", RoundedFace(.0758f,.1646f,.009f,.0048f,true), bezel);
                Part(phone, "Screen", RoundedFace(ScreenWidth,ScreenHeight,.006f,.0051f,true), screen);
                Detail(phone,"Power_Button", new Vector3(.0394f,.025f,0), new Vector3(.0012f,.018f,.003f), frame);
                Detail(phone,"Volume_Button", new Vector3(-.0394f,.028f,0), new Vector3(.0012f,.025f,.003f), frame);
                Detail(phone,"Earpiece",new Vector3(0,.0801f,.0050f),new Vector3(.014f,.0008f,.0003f),lens);
                // The rear camera is omitted. A small box is enough for the flash LED.
                Detail(phone,"Rear_Flash",new Vector3(-.012f,.066f,-.0048f),new Vector3(.004f,.004f,.0006f),led);
                var collider = phone.AddComponent<BoxCollider>();
                collider.size = new Vector3(.080f,.1668f,.011f);
                Anchor(phone,"GripAnchor",Vector3.zero,Quaternion.identity);
                var lightAnchor = Anchor(phone,"FlashlightAnchor",new Vector3(-.012f,.066f,-.0053f),Quaternion.Euler(0,180,0));
                var light = lightAnchor.AddComponent<Light>();
                light.type=LightType.Spot; light.range=12; light.intensity=3;
                light.spotAngle=55; light.innerSpotAngle=32; light.shadows=LightShadows.Soft;
                light.color=new Color(1,.97f,.9f); light.enabled=false;
                var controller=phone.AddComponent<PhoneFlashlightScreen>();
                controller.Configure(phone.transform.Find("Screen").GetComponent<Renderer>(),light,screen,screenOn);
                var prefab = PrefabUtility.SaveAsPrefabAsset(phone, Root+"/Prefabs/Phone_Blank.prefab");
                AssetDatabase.SaveAssets();
                Validate(prefab);
                ValidateScreenStates(prefab,screen,screenOn);
                ExportObj(phone);
                Preview(phone,"Phone_Flashlight_Off.png");
                controller.TurnOn();
                Preview(phone,"Phone_Flashlight_On.png");
                controller.TurnOff();
                Debug.Log("BLANK_PHONE_OK: prefab, UVs, dimensions, collider and disabled flashlight verified.");
            }
            finally { UnityEngine.Object.DestroyImmediate(phone); }
        }

        static Material ScreenMaterial(string name,string filename)
        {
            string texturePath=Root+"/Textures/"+filename;
            var importer=AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if(importer==null) throw new Exception("Missing phone screen texture: "+texturePath);
            importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=true;importer.mipmapEnabled=true;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            string path=Root+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=Shader.Find("Universal Render Pipeline/Unlit");
            if(!shader) throw new Exception("URP Unlit shader missing");
            if(!material) {material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.shader=shader;material.SetColor("_BaseColor",Color.white);
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetTextureScale("_BaseMap",Vector2.one);material.SetTextureOffset("_BaseMap",Vector2.zero);
            EditorUtility.SetDirty(material);return material;
        }

        static void ValidateScreenStates(GameObject prefab,Material off,Material on)
        {
            var first=UnityEngine.Object.Instantiate(prefab);
            var second=UnityEngine.Object.Instantiate(prefab);
            try
            {
                var controller=first.GetComponent<PhoneFlashlightScreen>();
                var renderer=first.transform.Find("Screen").GetComponent<Renderer>();
                var light=first.GetComponentInChildren<Light>();
                if(controller.IsOn || light.enabled || renderer.sharedMaterial!=off) throw new Exception("Phone must start off");
                controller.ToggleFlashlight();
                if(!controller.IsOn || !light.enabled || renderer.sharedMaterial!=on) throw new Exception("On state failed");
                if(second.transform.Find("Screen").GetComponent<Renderer>().sharedMaterial!=off || second.GetComponentInChildren<Light>().enabled)
                    throw new Exception("State leaked to another phone");
                light.enabled=false;controller.SendMessage("LateUpdate");
                if(controller.IsOn || renderer.sharedMaterial!=off) throw new Exception("External light synchronization failed");
                if(off.GetTexture("_BaseMap")==on.GetTexture("_BaseMap")) throw new Exception("Textures must differ");
                Debug.Log("PHONE_SCREEN_STATES_OK: off, on, external light sync, independent instances.");
            }
            finally {UnityEngine.Object.DestroyImmediate(first);UnityEngine.Object.DestroyImmediate(second);}
        }

        static Material Material(string name, Color color, float metal, float smooth)
        {
            string path=Root+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material,path);
                material.SetColor("_BaseColor",color); material.SetFloat("_Metallic",metal);
                material.SetFloat("_Smoothness",smooth);
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        static GameObject Anchor(GameObject parent,string name,Vector3 position,Quaternion rotation)
        {
            var go=new GameObject(name); go.transform.SetParent(parent.transform,false);
            go.transform.localPosition=position; go.transform.localRotation=rotation; return go;
        }

        static GameObject Part(GameObject parent,string name,Mesh mesh,Material material)
        {
            string path=Root+"/Models/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing)
            {
                // Update through Mesh APIs so changed topology also invalidates GPU buffers.
                existing.Clear();existing.vertices=mesh.vertices;existing.uv=mesh.uv;
                existing.triangles=mesh.triangles;existing.RecalculateNormals();
                existing.RecalculateTangents();existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;
            }
            else AssetDatabase.CreateAsset(mesh,path);
            mesh.name=name;
            var go=Anchor(parent,name,Vector3.zero,Quaternion.identity);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            return go;
        }

        static void Detail(GameObject parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var go=Part(parent,name,BoxMesh(size),material);
            go.transform.localPosition=position;
        }

        static Mesh BoxMesh(Vector3 size)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            Vector3[] corners={new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),
                new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};
            int[][] faces={new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,4,7,3},new[]{1,2,6,5},new[]{0,1,5,4},new[]{3,7,6,2}};
            foreach(var face in faces)
            {
                int start=vertices.Count;
                foreach(int index in face) vertices.Add(Vector3.Scale(corners[index],size)*.5f);
                uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            return Mesh(vertices,uv,triangles);
        }

        static List<Vector2> Outline(float width,float height,float radius)
        {
            var points=new List<Vector2>();
            var centres=new[] {new Vector2(width/2-radius,height/2-radius),new Vector2(-width/2+radius,height/2-radius),new Vector2(-width/2+radius,-height/2+radius),new Vector2(width/2-radius,-height/2+radius)};
            for(int corner=0;corner<4;corner++)
                for(int step=0;step<=4;step++)
                {
                    float angle=(corner*90+step*22.5f)*Mathf.Deg2Rad;
                    points.Add(centres[corner]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);
                }
            return points;
        }

        static Mesh RoundedFace(float width,float height,float radius,float z,bool front)
        {
            var outline=Outline(width,height,radius); var vertices=new List<Vector3> {new Vector3(0,0,z)};
            var uv=new List<Vector2> {new Vector2(.5f,.5f)}; var triangles=new List<int>();
            foreach(var p in outline) { vertices.Add(new Vector3(p.x,p.y,z)); uv.Add(new Vector2(p.x/width+.5f,p.y/height+.5f)); }
            for(int i=0;i<outline.Count;i++)
            { int a=i+1,b=(i+1)%outline.Count+1; triangles.Add(0); triangles.Add(front?a:b); triangles.Add(front?b:a); }
            return Mesh(vertices,uv,triangles);
        }

        static Mesh RoundedSolid(float width,float height,float depth,float radius,float bevel)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            float[] z={-depth/2,-depth/2+bevel,depth/2-bevel,depth/2};
            for(int ring=0;ring<4;ring++)
            {
                float inset=(ring==0||ring==3)?bevel:0;
                foreach(var p in Outline(width-2*inset,height-2*inset,Mathf.Max(.00001f,radius-inset)))
                { vertices.Add(new Vector3(p.x,p.y,z[ring])); uv.Add(new Vector2(p.x/width+.5f,p.y/height+.5f)); }
            }
            int count=vertices.Count/4;
            for(int ring=0;ring<3;ring++) for(int i=0;i<count;i++)
            {
                int a=ring*count+i,b=ring*count+(i+1)%count,c=b+count,d=a+count;
                triangles.AddRange(new[]{a,b,c,a,c,d});
            }
            for(int i=1;i<count-1;i++) {triangles.AddRange(new[]{0,i+1,i,3*count,3*count+i,3*count+i+1});}
            return Mesh(vertices,uv,triangles);
        }

        static Mesh Mesh(List<Vector3> vertices,List<Vector2> uv,List<int> triangles)
        {
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }

        static void Validate(GameObject prefab)
        {
            if(prefab.transform.Find("Rear_Camera")!=null) throw new Exception("Rear camera must be removed");
            var mesh=prefab.transform.Find("Screen").GetComponent<MeshFilter>().sharedMesh;
            if(Mathf.Abs(mesh.bounds.size.x/mesh.bounds.size.y-853f/1844f)>.0001f) throw new Exception("Screen ratio mismatch");
            int triangles=0;
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                triangles+=filter.sharedMesh.triangles.Length/3;
                if(filter.sharedMesh.uv.Length!=filter.sharedMesh.vertexCount) throw new Exception("Missing UVs");
                foreach(var index in filter.sharedMesh.triangles) if(index<0||index>=filter.sharedMesh.vertexCount) throw new Exception("Invalid triangle");
            }
            if(triangles>300) throw new Exception("Phone exceeds the 300 triangle budget");
            Debug.Log("PHONE_LOW_POLY_OK: "+triangles+" triangles, no rear camera.");
            if(prefab.GetComponent<BoxCollider>()==null || prefab.GetComponentInChildren<Light>().enabled) throw new Exception("Invalid pickup/light preparation");
        }

        static void ExportObj(GameObject phone)
        {
            var output=new StringBuilder("# Blank phone in metres. +Y top, +Z screen. Screen UVs fill 0..1.\n"); int offset=1;
            foreach(var filter in phone.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;output.AppendLine("o "+filter.name);
                foreach(var v in mesh.vertices) {var p=filter.transform.TransformPoint(v);output.AppendLine(FormattableString.Invariant($"v {p.x} {p.y} {p.z}"));}
                foreach(var v in mesh.uv) output.AppendLine(FormattableString.Invariant($"vt {v.x} {v.y}"));
                var t=mesh.triangles;for(int i=0;i<t.Length;i+=3) output.AppendLine($"f {t[i]+offset}/{t[i]+offset} {t[i+1]+offset}/{t[i+1]+offset} {t[i+2]+offset}/{t[i+2]+offset}");
                offset+=mesh.vertexCount;
            }
            File.WriteAllText(Root+"/Models/Phone_Blank.obj",output.ToString());
        }

        static void Preview(GameObject phone,string filename)
        {
            var cameraGo=new GameObject("Phone preview camera");var lightGo=new GameObject("Phone preview light");
            var texture=new RenderTexture(1000,1000,24);var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            var previewMaterials=new List<Material>();
            var originalMaterials=new Dictionary<Renderer,Material>();
            try
            {
                foreach(var r in phone.GetComponentsInChildren<Renderer>())
                {
                    r.gameObject.layer=30;
                    if(GraphicsSettings.currentRenderPipeline==null)
                    {
                        originalMaterials[r]=r.sharedMaterial;
                        var material=new Material(Shader.Find(r.name=="Screen"?"Unlit/Texture":"Standard"));
                        if(r.name=="Screen") material.mainTexture=r.sharedMaterial.GetTexture("_BaseMap");
                        else
                        {
                            material.color=r.sharedMaterial.GetColor("_BaseColor");
                            material.SetFloat("_Metallic",r.sharedMaterial.GetFloat("_Metallic"));
                            material.SetFloat("_Glossiness",r.sharedMaterial.GetFloat("_Smoothness"));
                        }
                        r.sharedMaterial=material;previewMaterials.Add(material);
                    }
                }
                var camera=cameraGo.AddComponent<Camera>();camera.cullingMask=1<<30;
                camera.backgroundColor=new Color(.09f,.105f,.13f);camera.clearFlags=CameraClearFlags.SolidColor;
                camera.transform.position=new Vector3(.19f,.12f,.34f);camera.transform.LookAt(Vector3.zero);
                camera.orthographic=true;camera.orthographicSize=.115f;camera.nearClipPlane=.01f;
                camera.farClipPlane=2;
                camera.targetTexture=texture;
                var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.cullingMask=1<<30;
                lightGo.transform.rotation=Quaternion.Euler(30,-145,0);
                camera.Render();RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,1000,1000),0,0);image.Apply();
                File.WriteAllBytes(Root+"/Preview/"+filename,image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous;texture.Release();UnityEngine.Object.DestroyImmediate(texture);
                foreach(var pair in originalMaterials) pair.Key.sharedMaterial=pair.Value;
                foreach(var material in previewMaterials) UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(lightGo);
            }
            AssetDatabase.Refresh();
        }
    }
}
