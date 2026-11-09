// Mango Tree pack - feeds the scene's WindZone into the "MangoTree/Wind URP" shader.
// Works automatically: no component needs to be added. Just put a Wind Zone in the scene
// (GameObject > 3D Object > Wind Zone). Directional zones are recommended.
// Works for GameObjects, Terrain Trees and Terrain Details (Instanced Mesh).
using UnityEngine;

namespace MangoTreePack
{
    [AddComponentMenu("")]
    [ExecuteAlways]
    public class MangoTreeWind : MonoBehaviour
    {
        static readonly int WindDirId = Shader.PropertyToID("_MG_WindDir");
        static readonly int WindZoneId = Shader.PropertyToID("_MG_WindZone");

        static MangoTreeWind s_Instance;
        static WindZone s_Zone;
        static float s_NextSearch;

        /// <summary>Optional: force a specific WindZone instead of auto-detecting one.</summary>
        public static WindZone OverrideZone;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateDriver()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[MangoTree Wind]");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<MangoTreeWind>();
        }

        void Update()
        {
            Push(Time.unscaledTime);
        }

        void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }

        /// <summary>Find a WindZone and upload its settings as global shader values.</summary>
        public static void Push(float now)
        {
            if (now + 2f < s_NextSearch) s_NextSearch = 0f;   // clock switched (edit <-> play)
            if (OverrideZone != null)
            {
                s_Zone = OverrideZone;
            }
            else if (s_Zone == null || !s_Zone.gameObject.activeInHierarchy || now >= s_NextSearch)
            {
                s_NextSearch = now + 1f;
                s_Zone = FindZone();
            }

            if (s_Zone == null || !s_Zone.gameObject.activeInHierarchy)
            {
                Shader.SetGlobalVector(WindDirId, Vector4.zero);
                return;
            }

            Vector3 dir = s_Zone.transform.forward;
            float main = s_Zone.windMain;
            if (s_Zone.mode == WindZoneMode.Spherical)
            {
                // approximate a spherical zone around the main camera position
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 d = cam.transform.position - s_Zone.transform.position;
                    float dist = d.magnitude;
                    main *= Mathf.Clamp01(1f - dist / Mathf.Max(0.01f, s_Zone.radius));
                    if (dist > 0.01f) dir = d / dist;
                }
            }
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            dir.Normalize();

            Shader.SetGlobalVector(WindDirId, new Vector4(dir.x, dir.y, dir.z, 1f));
            Shader.SetGlobalVector(WindZoneId, new Vector4(main, s_Zone.windTurbulence,
                s_Zone.windPulseMagnitude, s_Zone.windPulseFrequency));
        }

        static WindZone FindZone()
        {
            var zones = Object.FindObjectsByType<WindZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            WindZone best = null;
            foreach (var z in zones)
            {
                if (!z.gameObject.activeInHierarchy) continue;
                if (z.mode == WindZoneMode.Directional) return z;
                if (best == null) best = z;
            }
            return best;
        }

#if UNITY_EDITOR
        // keep the wind alive in the Scene view while not playing
        [UnityEditor.InitializeOnLoadMethod]
        static void EditorHook()
        {
            UnityEditor.EditorApplication.update -= EditorTick;
            UnityEditor.EditorApplication.update += EditorTick;
        }

        static double s_LastEditorPush;

        static void EditorTick()
        {
            if (Application.isPlaying) return;
            double t = UnityEditor.EditorApplication.timeSinceStartup;
            if (t - s_LastEditorPush < 0.1) return;
            s_LastEditorPush = t;
            Push((float)t);
        }
#endif
    }
}
