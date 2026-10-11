using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.MangoTreeUpdate.Editor
{
    public static class MangoTreeTerrainAssetBuilder
    {
        public const string Root=MangoTreeUpdateAssetBuilder.Root;
        public const string ShaderName="Secrets That Breathe/Mango Tree Terrain Wind Lit";

        public static void Build()
        {
            Directory.CreateDirectory(Root+"/Models/Terrain");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Shader shader=Shader.Find(ShaderName);
            if (!shader) throw new InvalidOperationException("Missing Terrain mango shader.");
            var materials=new Material[3];
            string[] suffixes={"Bark","Leaves","Fruit_Green"};
            for (int i=0;i<3;i++)
            {
                var source=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/MangoUpdate_"+suffixes[i]+".mat");
                string path=Root+"/Materials/MangoTerrain_"+suffixes[i]+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat) { mat=new Material(source); AssetDatabase.CreateAsset(mat,path); }
                else mat.CopyPropertiesFromMaterial(source);
                mat.name="MangoTerrain_"+suffixes[i]; mat.shader=shader;
                mat.SetFloat("_MangoTerrainLeafFlutter",i==1 ? 1 : 0);
                mat.enableInstancing=true;
                EditorUtility.SetDirty(mat); materials[i]=mat;
            }
            var meshes=new Mesh[2,3];
            for (int lod=0;lod<3;lod++)
            {
                string path=Root+"/Models/MangoTree_Update_LOD"+lod+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                bool wasReadable=importer.isReadable;
                try
                {
                    if (!wasReadable) { importer.isReadable=true; importer.SaveAndReimport(); }
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    meshes[0,lod]=Combine(model,lod,true);
                    meshes[1,lod]=Combine(model,lod,false);
                }
                finally
                {
                    if (!wasReadable) { importer.isReadable=false; importer.SaveAndReimport(); }
                }
            }
            BuildPrefab("MangoTree_Update_Terrain",meshes,0,materials);
            BuildPrefab("MangoTree_Update_Terrain_NoFruit",meshes,1,new[] {materials[0],materials[1]});
            BuildDemo();
            AssetDatabase.SaveAssets();
        }

        static Mesh Combine(GameObject model,int lod,bool fruit)
        {
            Scene preview=EditorSceneManager.NewPreviewScene();
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,preview);
            try
            {
                var list=new List<CombineInstance>();
                // Fixed submesh order matches the three Terrain materials.
                foreach (string prefix in new[] {"Bark","Leaves","Fruit_Green"})
                    foreach (var filter in instance.GetComponentsInChildren<MeshFilter>())
                    {
                        if (!filter.name.StartsWith(prefix) || (!fruit && prefix=="Fruit_Green")) continue;
                        for (int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)
                            list.Add(new CombineInstance {mesh=filter.sharedMesh,subMeshIndex=sub,
                                transform=instance.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});
                    }
                string name="MangoTerrain"+(fruit ? "" : "_NoFruit")+"_LOD"+lod;
                var mesh=new Mesh {name=name,indexFormat=IndexFormat.UInt16};
                mesh.CombineMeshes(list.ToArray(),false,true);
                mesh.RecalculateBounds();
                var bounds=mesh.bounds; bounds.Expand(2.4f); mesh.bounds=bounds;
                string path=Root+"/Models/Terrain/"+name+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing)
                {
                    EditorUtility.CopySerialized(mesh,existing);
                    UnityEngine.Object.DestroyImmediate(mesh); mesh=existing;
                    EditorUtility.SetDirty(mesh);
                }
                else AssetDatabase.CreateAsset(mesh,path);
                return mesh;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static void BuildPrefab(string name,Mesh[,] meshes,int type,Material[] materials)
        {
            Scene preview=EditorSceneManager.NewPreviewScene();
            var root=new GameObject(name); SceneManager.MoveGameObjectToScene(root,preview);
            try
            {
                var group=root.AddComponent<LODGroup>(); var levels=new LOD[3];
                for (int lod=0;lod<3;lod++)
                {
                    // A root MeshRenderer also supports Terrain's prototype validation.
                    var obj=lod==0 ? root : new GameObject("LOD"+lod);
                    if (lod>0) obj.transform.SetParent(root.transform,false);
                    obj.AddComponent<MeshFilter>().sharedMesh=meshes[type,lod];
                    var renderer=obj.AddComponent<MeshRenderer>(); renderer.sharedMaterials=materials;
                    renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
                    renderer.receiveShadows=true;
                    renderer.lightProbeUsage=LightProbeUsage.BlendProbes;
                    renderer.motionVectorGenerationMode=MotionVectorGenerationMode.Object;
                    levels[lod]=new LOD(new[] {.50f,.20f,.035f}[lod],new Renderer[] {renderer});
                }
                group.SetLODs(levels); group.fadeMode=LODFadeMode.CrossFade; group.animateCrossFading=true;
                group.localReferencePoint=new Vector3(0,3.65f,0); group.size=7.5f;
                var collider=root.AddComponent<CapsuleCollider>();
                collider.center=new Vector3(.01f,1.16f,0); collider.radius=.245f; collider.height=2.42f;
                // No per-tree wind component: Terrain does not execute it.
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+name+".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        [MenuItem("Tools/Mango Tree Update/Add Mango to Selected Terrain")]
        public static void AddToSelectedTerrain()
        {
            var terrain=Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Terrain>() : null;
            if (!terrain || !terrain.terrainData) return;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/MangoTree_Update_Terrain.prefab");
            if (!prefab) throw new InvalidOperationException("Rebuild the Mango Tree Update assets first.");
            AddPrototype(terrain,prefab,true);
            bool hasController=false;
            foreach (var controller in UnityEngine.Object.FindObjectsByType<MangoTreeTerrainWind>(FindObjectsSortMode.None))
                if (controller.gameObject.scene==terrain.gameObject.scene) hasController=true;
            if (!hasController) Undo.AddComponent<MangoTreeTerrainWind>(terrain.gameObject);
            EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
            Debug.Log("Mango terrain prototype added. Select Paint Trees to paint; the scene has shared mango wind.",terrain);
        }

        [MenuItem("Tools/Mango Tree Update/Add Mango to Selected Terrain",true)]
        static bool CanAdd() { return Selection.activeGameObject && Selection.activeGameObject.GetComponent<Terrain>(); }

        public static int AddPrototype(Terrain terrain,GameObject prefab,bool undo=false)
        {
            var data=terrain.terrainData; var prototypes=new List<TreePrototype>(data.treePrototypes);
            for(int i=0;i<prototypes.Count;i++) if(prototypes[i].prefab==prefab) return i;
            if(undo) Undo.RecordObject(data,"Add mango tree prototype");
            prototypes.Add(new TreePrototype {prefab=prefab,bendFactor=0});
            data.treePrototypes=prototypes.ToArray(); data.RefreshPrototypes();
            EditorUtility.SetDirty(data); terrain.Flush(); return prototypes.Count-1;
        }

        static void BuildDemo()
        {
            Scene previous=SceneManager.GetActiveScene();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                string path=Root+"/Demo/MangoTerrainData.asset";
                var data=AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (!data) { data=new TerrainData(); AssetDatabase.CreateAsset(data,path); }
                data.heightmapResolution=33; data.size=new Vector3(52,4,52);
                var heights=new float[33,33];
                for(int y=0;y<33;y++) for(int x=0;x<33;x++) heights[y,x]=.05f+.035f*Mathf.Sin(x*.13f)*Mathf.Sin(y*.18f);
                data.SetHeights(0,0,heights);
                string groundPath=Root+"/Demo/MangoTerrainGround.asset";
                var ground=AssetDatabase.LoadAssetAtPath<Texture2D>(groundPath);
                if(!ground)
                {
                    ground=new Texture2D(2,2,TextureFormat.RGBA32,false) {name="Mango Terrain Ground"};
                    ground.SetPixels(new[] {new Color(.24f,.30f,.18f),new Color(.25f,.31f,.19f),
                        new Color(.25f,.31f,.19f),new Color(.24f,.30f,.18f)}); ground.Apply();
                    AssetDatabase.CreateAsset(ground,groundPath);
                }
                string layerPath=Root+"/Demo/MangoTerrainLayer.terrainlayer";
                var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
                if(!layer) {layer=new TerrainLayer {diffuseTexture=ground,tileSize=new Vector2(8,8),smoothness=.05f}; AssetDatabase.CreateAsset(layer,layerPath);}
                data.terrainLayers=new[] {layer}; data.alphamapResolution=32;
                var splat=new float[32,32,1];
                for(int y=0;y<32;y++) for(int x=0;x<32;x++) splat[y,x,0]=1;
                data.SetAlphamaps(0,0,splat);
                data.treePrototypes=new[] {
                    new TreePrototype {prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/MangoTree_Update_Terrain.prefab")},
                    new TreePrototype {prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/MangoTree_Update_Terrain_NoFruit.prefab")}};
                var instances=new List<TreeInstance>();
                for(int i=0;i<9;i++) instances.Add(new TreeInstance {prototypeIndex=i==8 ? 1 : 0,
                    position=new Vector3(.27f+(i%3)*.23f,0,.27f+(i/3)*.23f),
                    rotation=i*2.39996f,widthScale=.9f+(i%3)*.12f,heightScale=.9f+(i%3)*.12f,
                    color=Color.white,lightmapColor=Color.white});
                data.SetTreeInstances(instances.ToArray(),true);
                var obj=Terrain.CreateTerrainGameObject(data); obj.name="Mango Terrain (Paint Trees)";
                obj.transform.position=new Vector3(-26,-.2f,-26);
                var terrain=obj.GetComponent<Terrain>(); terrain.treeDistance=500; terrain.treeBillboardDistance=250;
                terrain.treeMaximumFullLODCount=5000; terrain.drawInstanced=true;
                obj.AddComponent<MangoTreeTerrainWind>();
                var zone=new GameObject("Wind Zone").AddComponent<WindZone>();
                zone.windMain=1; zone.windTurbulence=.5f; zone.windPulseMagnitude=.25f; zone.windPulseFrequency=.2f;
                zone.transform.rotation=Quaternion.Euler(0,55,0);
                var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional;
                sun.intensity=1.2f; sun.shadows=LightShadows.Soft; sun.transform.rotation=Quaternion.Euler(43,-35,0);
                RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.42f,.48f,.52f); RenderSettings.skybox=null;
                var camera=new GameObject("Preview Camera").AddComponent<Camera>(); camera.tag="MainCamera";
                camera.transform.position=new Vector3(28,17,-34); camera.transform.LookAt(new Vector3(0,3,0));
                camera.orthographic=true; camera.orthographicSize=22; camera.nearClipPlane=.1f; camera.farClipPlane=500;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.14f,.18f,.21f);
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                EditorUtility.SetDirty(data);
                EditorSceneManager.SaveScene(scene,Root+"/Demo/MangoTree_Update_Terrain_Preview.unity");
            }
            finally
            {
                if (!Application.isBatchMode)
                {
                    EditorSceneManager.CloseScene(scene,true); if(previous.IsValid()) SceneManager.SetActiveScene(previous);
                }
            }
        }
    }
}
