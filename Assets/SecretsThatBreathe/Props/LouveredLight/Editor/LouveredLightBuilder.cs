using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SecretsThatBreathe.LouveredLight.Editor
{
    public static class LouveredLightBuilder
    {
        public const string Root = "Assets/SecretsThatBreathe/Props/LouveredLight";
        [MenuItem("Tools/Louvered Light/Rebuild Prefab")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new Exception("URP Lit shader missing");
            string[] names = { "Housing_White", "Reflector_Chrome", "Tube_Emission", "EndCaps_Aluminium", "Sockets_Ivory", "Fasteners" };
            Color[] colors = { new Color(.88f,.9f,.92f),new Color(.78f,.82f,.86f),new Color(.88f,.95f,1),new Color(.44f,.48f,.52f),new Color(.8f,.8f,.75f),new Color(.13f,.15f,.17f) };
            float[] metal = { .12f,.88f,0,.8f,0,.7f };
            float[] smooth = { .72f,.8f,.8f,.73f,.5f,.7f };
            var materials = new Material[names.Length];
            for (int i=0; i<names.Length; i++)
            {
                string path = Root + "/Materials/" + names[i] + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat,path); }
                mat.name=names[i]; mat.shader=shader; mat.SetColor("_BaseColor", colors[i]);
                mat.SetFloat("_Metallic", metal[i]); mat.SetFloat("_Smoothness",smooth[i]);
                mat.enableInstancing=true;
                if (i==2)
                {
                    mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor",new Color(3.52f,3.8f,4f,1));
                    mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
                }
                else { mat.DisableKeyword("_EMISSION"); mat.SetColor("_EmissionColor",Color.black); }
                EditorUtility.SetDirty(mat); materials[i]=mat;
            }
            string modelPath=Root + "/Models/LouveredLight_T8_2x36W.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.globalScale=1; importer.useFileScale=true; importer.bakeAxisConversion=true;
            importer.importAnimation=false; importer.importCameras=false; importer.importLights=false;
            importer.importNormals=ModelImporterNormals.Import;
            importer.generateSecondaryUV=true;
            for(int i=0;i<names.Length;i++) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),names[i]),materials[i]);
            importer.SaveAndReimport();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var go=new GameObject("LouveredLight_T8_2x36W");
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(model); visual.name="Fixture";
            visual.transform.SetParent(go.transform,false);
            foreach(var r in visual.GetComponentsInChildren<MeshRenderer>())
            {
                int i=Array.FindIndex(names,n=>r.name.StartsWith(n,StringComparison.Ordinal));
                if(i<0) throw new Exception("Unknown model part " + r.name);
                r.sharedMaterial=materials[i];
            }
            var collider=go.AddComponent<BoxCollider>(); collider.center=new Vector3(0,-.04f,0); collider.size=new Vector3(.31f,.08f,1.24f);
            // One downward light supplies room illumination independently of emission/baking.
            var lightObject=new GameObject("Downward Light (optional)"); lightObject.transform.SetParent(go.transform,false);
            lightObject.transform.localPosition=new Vector3(0,-.09f,0);
            lightObject.transform.localRotation=Quaternion.Euler(90,0,0);
            var light=lightObject.AddComponent<Light>(); light.type=LightType.Spot;
            light.color=new Color(.88f,.95f,1); light.intensity=2; light.range=5;
            light.spotAngle=115; light.innerSpotAngle=80; light.shadows=LightShadows.None;
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/LouveredLight_T8_2x36W.prefab");
            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            Validate(prefab);
        }
        static void Validate(GameObject prefab)
        {
            var renderers=prefab.GetComponentsInChildren<MeshRenderer>();
            if(renderers.Length!=6) throw new Exception("Expected six material groups");
            Bounds b=renderers[0].bounds; long triangles=0;
            foreach(var r in renderers)
            {
                b.Encapsulate(r.bounds);
                var m=r.GetComponent<MeshFilter>().sharedMesh;
                triangles+=m.GetIndexCount(0)/3;
                if(!r.sharedMaterial || ShaderUtil.ShaderHasError(r.sharedMaterial.shader)) throw new Exception("Material/shader failure");
            }
            if(Mathf.Abs(b.size.x-.31f)>.015f || Mathf.Abs(b.size.z-1.24f)>.015f || b.max.y>.015f || b.min.y<-.1f)
                throw new Exception("Incorrect metre scale or ceiling orientation: "+b);
            var emission=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Tube_Emission.mat");
            if(!emission.IsKeywordEnabled("_EMISSION") || emission.GetColor("_EmissionColor").maxColorComponent<3) throw new Exception("Emission missing");
            Debug.Log("LAMP_VALIDATION_PASS: "+triangles+" triangles; bounds "+b+"; 6 renderers; URP emission; collider; downward light.");
        }
        public static void BatchBuild()
        {
            try
            {
                Build();
                Directory.CreateDirectory(Root+"/Demo");
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var fixture=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/LouveredLight_T8_2x36W.prefab"));
                var camera=new GameObject("Preview Camera").AddComponent<Camera>();
                camera.transform.position=new Vector3(1.05f,-1.2f,-1.45f); camera.transform.LookAt(new Vector3(0,-.035f,0));
                camera.orthographic=true; camera.orthographicSize=.78f; camera.nearClipPlane=.01f;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.075f,.085f,.1f); camera.allowHDR=true;
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
                RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.5f,.52f,.55f);
                var key=new GameObject("Studio Key").AddComponent<Light>(); key.type=LightType.Directional; key.intensity=1.5f;
                key.transform.rotation=Quaternion.Euler(-35,-30,0);
                var fill=new GameObject("Studio Fill").AddComponent<Light>(); fill.type=LightType.Directional; fill.intensity=.6f;
                fill.transform.rotation=Quaternion.Euler(-60,140,0);
                var volume=new GameObject("Bloom Preview").AddComponent<Volume>(); volume.isGlobal=true;
                var profile=ScriptableObject.CreateInstance<VolumeProfile>();
                var bloom=profile.Add<Bloom>(true); bloom.intensity.Override(.22f); bloom.threshold.Override(1.2f);
                var tone=profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
                AssetDatabase.CreateAsset(profile,Root+"/Demo/LampPreviewVolume.asset");
                AssetDatabase.AddObjectToAsset(bloom,profile); AssetDatabase.AddObjectToAsset(tone,profile); volume.sharedProfile=profile;
                EditorSceneManager.SaveScene(scene,Root+"/Demo/LouveredLight_Preview.unity");
                // These pipeline assets exist only in the independent verification project.
                var data=ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(data,"Assets/VerificationRenderer.asset");
                var pipeline=UniversalRenderPipelineAsset.Create(data); pipeline.msaaSampleCount=4;
                AssetDatabase.CreateAsset(pipeline,"Assets/VerificationPipeline.asset");
                GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline;
                AssetDatabase.SaveAssets();
                var rt=new RenderTexture(1400,1000,24,RenderTextureFormat.ARGBHalf); rt.Create(); camera.targetTexture=rt;
                camera.Render(); RenderTexture.active=rt;
                var image=new Texture2D(1400,1000,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1400,1000),0,0); image.Apply();
                string report=Environment.GetEnvironmentVariable("LAMP_REPORT_DIR");
                File.WriteAllBytes(Path.Combine(report,"LouveredLight_Unity_URP.png"),image.EncodeToPNG());
                File.WriteAllText(Path.Combine(report,"Unity_validation.txt"),"PASS: Unity "+Application.unityVersion+"; URP Lit materials; 6 renderers; HDR tube emission; metre dimensions 0.31 x 0.08 x 1.24; ceiling pivot; collider; optional downward light.\n");
                RenderTexture.active=null; camera.targetTexture=null; rt.Release();
                UnityEngine.Object.DestroyImmediate(image);
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
