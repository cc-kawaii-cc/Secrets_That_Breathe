using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecretsThatBreathe.RoadsideGrass.Editor
{
    public static class RoadsideGrassAssets
    {
        public const string Root = "Assets/SecretsThatBreathe/Environment/RoadsideGrass";
        public static readonly string[] Names = { "RoadsideGrass_Green", "RoadsideGrass_Mixed", "RoadsideGrass_Lite" };

        [MenuItem("Tools/Roadside Grass/Rebuild Prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Meshes");
            AssetDatabase.Refresh();
            Shader shader = Shader.Find("Secrets That Breathe/Roadside Grass Wind");
            if (!shader) throw new InvalidOperationException("Grass shader is missing.");
            string materialPath = Root + "/Materials/RoadsideGrass_Wind.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            material.shader = shader;
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_WindStrength", .028f);
            material.SetFloat("_WindSpeed", 1.25f);
            material.SetVector("_WindDirection", new Vector4(1,0,.35f,0));
            material.SetFloat("_Smoothness", .18f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            foreach (string name in Names)
            {
                string modelPath = Root + "/Models/" + name + ".fbx";
                var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (!importer) throw new InvalidOperationException("Missing " + modelPath);
                importer.globalScale = 1;
                importer.useFileScale = true;
                importer.bakeAxisConversion = true;
                importer.importAnimation = false;
                importer.importCameras = false; importer.importLights = false;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.None;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.isReadable = true;
                importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                var source = model.GetComponentInChildren<MeshFilter>().sharedMesh;
                string meshPath = Root + "/Meshes/" + name + ".asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (!mesh) { mesh = UnityEngine.Object.Instantiate(source); AssetDatabase.CreateAsset(mesh,meshPath); }
                else EditorUtility.CopySerialized(source,mesh);
                mesh.name = name;
                // Expand culling bounds for shader displacement without altering geometry.
                var bounds = mesh.bounds; bounds.Expand(new Vector3(.25f,.02f,.25f)); mesh.bounds=bounds;
                EditorUtility.SetDirty(mesh);
                var go = new GameObject(name);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                // Terrain detail prototypes use a single root mesh and material.
                PrefabUtility.SaveAsPrefabAsset(go,Root + "/Prefabs/" + name + ".prefab");
                UnityEngine.Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Roadside grass prefabs built: " + Root);
        }

        public static DetailPrototype Prototype(int index)
        {
            var p = new DetailPrototype {
                prototype = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + Names[index] + ".prefab"),
                usePrototypeMesh = true, useInstancing = true,
                renderMode = DetailRenderMode.VertexLit,
                minWidth = .8f, maxWidth = 1.2f,
                minHeight = .8f, maxHeight = 1.2f,
                noiseSpread = .35f, healthyColor = Color.white, dryColor = Color.white,
                alignToGround = .25f, positionJitter = 1f,
                holeEdgePadding = .1f, useDensityScaling = true, density = 1f
            };
            if (!p.Validate(out string error)) throw new InvalidOperationException(error);
            return p;
        }

        [MenuItem("Tools/Roadside Grass/Add to Selected Terrain")]
        public static void AddToSelectedTerrain()
        {
            Terrain terrain = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Terrain>() : null;
            if (!terrain || !terrain.terrainData) { Debug.LogWarning("Select a Terrain first."); return; }
            var prototypes = new List<DetailPrototype>(terrain.terrainData.detailPrototypes);
            int count = 0;
            for (int i=0;i<Names.Length;i++)
            {
                var p=Prototype(i);
                if (prototypes.Exists(existing => existing.prototype == p.prototype)) continue;
                prototypes.Add(p); count++;
            }
            if (count == 0) return;
            Undo.RecordObject(terrain.terrainData,"Add roadside grass details");
            terrain.terrainData.detailPrototypes=prototypes.ToArray();
            EditorUtility.SetDirty(terrain.terrainData);
            Debug.Log("Added " + count + " grass detail brushes. Use Terrain > Paint Details.");
        }
    }
}
