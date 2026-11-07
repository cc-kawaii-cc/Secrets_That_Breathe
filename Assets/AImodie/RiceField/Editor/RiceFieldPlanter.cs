// Rice Field Planter - plants rice hills in straight rows (or scatters grass) on any Terrain.
// Menu: Tools > Rice Field > 3) Open Planter Window
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RiceField.EditorTools
{
    public class RiceFieldPlanter : EditorWindow
    {
        public enum Mode { Rows, Scatter }

        public Terrain terrain;
        public List<GameObject> prefabs = new List<GameObject>();
        public Mode mode = Mode.Rows;
        [Tooltip("Area in terrain-local metres (x, z, width, length)")]
        public Rect area = new Rect(1, 1, 10, 10);
        [Tooltip("Distance between rows (m). Thai transplanted rice: ~0.25-0.30 m")]
        public float rowSpacing = 0.30f;
        [Tooltip("Distance between hills in a row (m)")]
        public float hillSpacing = 0.25f;
        public float jitter = 0.03f;
        public int scatterCount = 200;
        public Vector2 scaleRange = new Vector2(0.9f, 1.1f);
        public int seed = 1;

        SerializedObject so;
        Vector2 scroll;

        public static void Open()
        {
            var w = GetWindow<RiceFieldPlanter>("Rice Field Planter");
            w.minSize = new Vector2(340, 420);
            if (w.terrain == null) w.terrain = Terrain.activeTerrain;
            if (w.prefabs.Count == 0)
            {
                string root = RiceFieldSetup.Root;
                if (root != null)
                    foreach (var n in new[] { "Rice_Ripe_A", "Rice_Ripe_B", "Rice_Ripe_C" })
                    {
                        var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{root}/Prefabs/{n}.prefab");
                        if (p) w.prefabs.Add(p);
                    }
            }
        }

        void OnEnable() { so = new SerializedObject(this); }

        void OnGUI()
        {
            if (so == null) so = new SerializedObject(this);
            so.Update();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox("Plants prefabs as Terrain Trees (supports LOD Group). " +
                                    "Rows mode = rice paddy rows. Scatter mode = random weeds / grass.", MessageType.Info);
            EditorGUILayout.PropertyField(so.FindProperty("terrain"));
            EditorGUILayout.PropertyField(so.FindProperty("prefabs"), true);
            EditorGUILayout.PropertyField(so.FindProperty("mode"));
            EditorGUILayout.PropertyField(so.FindProperty("area"));
            if (terrain != null && GUILayout.Button("Area = whole terrain"))
            {
                var s = terrain.terrainData.size;
                area = new Rect(0, 0, s.x, s.z);
                so.Update();
            }
            if (mode == Mode.Rows)
            {
                EditorGUILayout.PropertyField(so.FindProperty("rowSpacing"));
                EditorGUILayout.PropertyField(so.FindProperty("hillSpacing"));
                EditorGUILayout.PropertyField(so.FindProperty("jitter"));
            }
            else
            {
                EditorGUILayout.PropertyField(so.FindProperty("scatterCount"));
            }
            EditorGUILayout.PropertyField(so.FindProperty("scaleRange"));
            EditorGUILayout.PropertyField(so.FindProperty("seed"));
            so.ApplyModifiedProperties();

            EditorGUILayout.Space();
            int estimate = mode == Mode.Rows
                ? Mathf.FloorToInt(area.width / Mathf.Max(0.05f, hillSpacing)) * Mathf.FloorToInt(area.height / Mathf.Max(0.05f, rowSpacing))
                : scatterCount;
            EditorGUILayout.LabelField("Instances to add", estimate.ToString("N0"));

            using (new EditorGUI.DisabledScope(terrain == null || prefabs.All(p => p == null)))
            {
                if (GUILayout.Button("Plant", GUILayout.Height(28)))
                {
                    Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Rice Field Plant");
                    var rnd = new System.Random(seed);
                    var list = prefabs.Where(p => p != null).ToList();
                    int n = mode == Mode.Rows
                        ? RiceFieldPlanting.PlantRows(terrain, list, area, rowSpacing, hillSpacing, jitter, scaleRange.x, scaleRange.y, rnd)
                        : RiceFieldPlanting.Scatter(terrain, list, area, scatterCount, scaleRange.x, scaleRange.y, rnd);
                    terrain.Flush();
                    Debug.Log($"[RiceField] Planted {n} instances.");
                }
            }
            using (new EditorGUI.DisabledScope(terrain == null))
            {
                if (GUILayout.Button("Clear trees inside area"))
                {
                    Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Rice Field Clear");
                    int n = RiceFieldPlanting.ClearArea(terrain, area);
                    terrain.Flush();
                    Debug.Log($"[RiceField] Removed {n} instances.");
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
