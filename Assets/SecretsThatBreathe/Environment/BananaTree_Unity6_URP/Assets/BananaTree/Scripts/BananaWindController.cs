using UnityEngine;

namespace BananaTree
{
    /// <summary>
    /// Feeds a WindZone into the banana-tree shader.
    /// Put ONE of these in the scene (the menu GameObject > Banana Tree > Wind Zone + Controller does it for you).
    /// Every material using "Custom/Banana Tree (URP Wind)" reads the values through global shader properties,
    /// so the SRP Batcher / GPU instancing keep working and no per-tree script is needed.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Banana Tree/Banana Wind Controller")]
    public class BananaWindController : MonoBehaviour
    {
        [Header("Wind source")]
        [Tooltip("Directional WindZone to follow. Empty = the first active WindZone found in the scene.")]
        public WindZone windZone;

        [Tooltip("When no WindZone exists, the values below are used instead.")]
        public bool useFallbackWhenNoZone = true;

        [Header("Fallback values (no WindZone)")]
        public Vector3 fallbackDirection = new Vector3(1f, 0f, 0.3f);
        [Range(0f, 5f)] public float fallbackMain = 1f;
        [Range(0f, 5f)] public float fallbackTurbulence = 1f;
        [Range(0f, 5f)] public float fallbackPulseMagnitude = 0.5f;
        [Range(0f, 1f)] public float fallbackPulseFrequency = 0.01f;

        [Header("Tuning")]
        [Tooltip("Overall multiplier for the whole effect (0 = trees stand still).")]
        [Range(0f, 3f)] public float strength = 1f;

        [Tooltip("WindZone 'Pulse Frequency' is a very small number (default 0.01). It is multiplied by this to get the gust speed in Hz.")]
        public float pulseFrequencyScale = 25f;

        static readonly int ID_Dir = Shader.PropertyToID("_BananaWindDir");
        static readonly int ID_Params = Shader.PropertyToID("_BananaWindParams");

        float _nextSearch;

        void OnEnable() => Apply();
        void Update() => Apply();
        void OnValidate() => Apply();

        void OnDisable()
        {
            // no controller = no wind
            Shader.SetGlobalVector(ID_Dir, Vector4.zero);
            Shader.SetGlobalVector(ID_Params, Vector4.zero);
        }

        void Apply()
        {
            WindZone zone = windZone;
            if (zone == null && Time.realtimeSinceStartup >= _nextSearch)
            {
                _nextSearch = Time.realtimeSinceStartup + 1f;   // do not search every frame
                _cached = FindFirstObjectByType<WindZone>();
            }
            if (zone == null) zone = _cached;

            Vector3 dir;
            float main, turbulence, pulseMag, pulseFreq;

            if (zone != null && zone.gameObject.activeInHierarchy)
            {
                dir = zone.mode == WindZoneMode.Spherical
                    ? (transform.position - zone.transform.position)   // blows away from the zone centre
                    : zone.transform.forward;
                main = zone.windMain;
                turbulence = zone.windTurbulence;
                pulseMag = zone.windPulseMagnitude;
                pulseFreq = zone.windPulseFrequency;
            }
            else if (useFallbackWhenNoZone)
            {
                dir = fallbackDirection;
                main = fallbackMain;
                turbulence = fallbackTurbulence;
                pulseMag = fallbackPulseMagnitude;
                pulseFreq = fallbackPulseFrequency;
            }
            else
            {
                Shader.SetGlobalVector(ID_Dir, Vector4.zero);
                Shader.SetGlobalVector(ID_Params, Vector4.zero);
                return;
            }

            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.right;
            dir.Normalize();

            Shader.SetGlobalVector(ID_Dir, new Vector4(dir.x, dir.y, dir.z, main * strength));
            Shader.SetGlobalVector(ID_Params, new Vector4(turbulence, pulseMag, Mathf.Max(pulseFreq, 0.0005f) * pulseFrequencyScale, 0f));
        }

        WindZone _cached;
    }
}
