using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SecretsThatBreathe.MangoTreeUpdate.Editor
{
    // Opt-in batch verification only; importing this file never starts Play Mode.
    public static class VerifyMangoTerrainPlayMode
    {
        const string Key="SecretsThatBreathe.VerifyMangoTerrainPlayMode";
        static double startedPlaying;

        [InitializeOnLoadMethod]
        static void Resume()
        {
            if (SessionState.GetBool(Key,false))
            {
                EditorApplication.update-=Tick;
                EditorApplication.update+=Tick;
            }
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene(MangoTreeTerrainAssetBuilder.Root+"/Demo/MangoTree_Update_Terrain_Preview.unity");
            SessionState.SetBool(Key,true);
            SessionState.SetFloat(Key+"Start",(float)EditorApplication.timeSinceStartup);
            Resume();
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            try
            {
                if (!SessionState.GetBool(Key,false)) {EditorApplication.update-=Tick; return;}
                if (EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"Start",0)>90)
                    throw new Exception("Terrain Play Mode verification timed out.");
                if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
                if (startedPlaying==0) startedPlaying=EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup-startedPlaying<2) return;
                var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
                var controller=UnityEngine.Object.FindFirstObjectByType<MangoTreeTerrainWind>();
                if(!terrain || !controller) throw new Exception("Runtime Terrain setup is missing.");
                Physics.SyncTransforms();
                int hits=0;
                foreach(var tree in terrain.terrainData.treeInstances)
                {
                    Vector3 root=terrain.transform.position+Vector3.Scale(tree.position,terrain.terrainData.size);
                    if(Physics.Raycast(root+new Vector3(-2,1.2f*tree.heightScale,0),Vector3.right,out RaycastHit hit,4)) hits++;
                }
                if(hits!=terrain.terrainData.treeInstanceCount) throw new Exception("Runtime tree collider raycasts: "+hits+"/"+terrain.terrainData.treeInstanceCount);
                if(Shader.GetGlobalInt("_MangoTerrainZoneCount")!=1) throw new Exception("Automatic Runtime Terrain wind did not update.");
                if(Math.Abs(Shader.GetGlobalVector("_MangoTerrainTime").x-Time.time)>.2f)
                    throw new Exception("Shared wind clock did not follow Runtime LateUpdate.");
                string line="PASS: Play Mode native tree trunk collision "+hits+"/"+terrain.terrainData.treeInstanceCount+" raycasts, and shared wind updates automatically in Runtime LateUpdate.\n";
                string report=Environment.GetEnvironmentVariable("MANGO_UPDATE_REPORT_DIR");
                File.AppendAllText(Path.Combine(report,"Terrain_Verification.txt"),line);
                Debug.Log("MANGO_TERRAIN_PLAY_PASS "+line);
                SessionState.SetBool(Key,false); EditorApplication.update-=Tick; EditorApplication.Exit(0);
            }
            catch(Exception e)
            {
                SessionState.SetBool(Key,false); EditorApplication.update-=Tick;
                Debug.LogException(e); EditorApplication.Exit(1);
            }
        }
    }
}
