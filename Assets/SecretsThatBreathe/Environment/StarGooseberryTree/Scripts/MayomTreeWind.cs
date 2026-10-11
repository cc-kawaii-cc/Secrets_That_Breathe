using UnityEngine;

namespace SecretsThatBreathe.MayomTree
{
    /// <summary>Feeds WindZone values to the mayom GPU shader; never edits meshes or materials.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    [AddComponentMenu("Environment/Mayom Tree Wind")]
    public sealed class MayomTreeWind : MonoBehaviour
    {
        [Tooltip("Optional. Leave empty to combine active Wind Zones in this scene.")]
        public WindZone windZone;
        [Range(0, 3)] public float windMultiplier = 1;
        [Range(0, 2)] public float leafFlutter = 1;
        public bool previewInEditMode = true;

        static readonly int WindId = Shader.PropertyToID("_MayomWind");
        static readonly int PreviousWindId = Shader.PropertyToID("_MayomPreviousWind");
        static readonly int TimeId = Shader.PropertyToID("_MayomWindTime");
        static readonly int RootId = Shader.PropertyToID("_MayomRootWS");
        static readonly int UpId = Shader.PropertyToID("_MayomUpWS");
        static readonly int ShapeId = Shader.PropertyToID("_MayomShape");
        static readonly int LeafId = Shader.PropertyToID("_MayomLeafFlutter");
        static WindZone[] zones;
        static double nextSearch;
        MeshRenderer[] renderers;
        Bounds[] restBounds;
        MaterialPropertyBlock properties;
        Vector4 previousWind;
        float previousTime;
        bool hasPreviousFrame;
#if UNITY_EDITOR
        double lastEditorTick;
        static double lastRepaint;
#endif

        void OnEnable()
        {
            renderers = GetComponentsInChildren<MeshRenderer>(true);
            restBounds = new Bounds[renderers.Length];
            properties = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                restBounds[i] = renderers[i].localBounds;
                var bounds = restBounds[i];
                bounds.Expand(3f); // Maximum clamped displacement, including enlarged leaves.
                renderers[i].localBounds = bounds;
            }
            nextSearch = 0;
            hasPreviousFrame = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += EditorTick;
#endif
        }

        void LateUpdate()
        {
            if (Application.isPlaying) ApplyWind(Time.time);
        }

#if UNITY_EDITOR
        void EditorTick()
        {
            if (Application.isPlaying || !this) return;
            double now = UnityEditor.EditorApplication.timeSinceStartup;
            if (now - lastEditorTick < 1.0 / 30.0) return;
            lastEditorTick = now;
            ApplyWind((float)now);
            if (previewInEditMode && now - lastRepaint > 1.0 / 30.0)
            {
                lastRepaint = now;
                UnityEditor.SceneView.RepaintAll();
            }
        }
#endif

        /// <summary>Evaluates one zone. W holds turbulence; XYZ holds directional force.</summary>
        public static Vector4 SampleZone(WindZone zone, Vector3 position, float time)
        {
            if (!zone || !zone.gameObject.activeInHierarchy) return Vector4.zero;
            Vector3 direction = zone.transform.forward;
            float falloff = 1;
            if (zone.mode == WindZoneMode.Spherical)
            {
                Vector3 delta = position - zone.transform.position;
                float radius = Mathf.Max(0, zone.radius);
                if (radius <= 0 || delta.magnitude >= radius) return Vector4.zero;
                falloff = 1 - delta.magnitude / radius;
                falloff *= falloff;
                if (delta.sqrMagnitude > .000001f) direction = delta.normalized;
            }
            float pulse = zone.windPulseFrequency > 0
                ? .5f + .5f * Mathf.Sin(time * zone.windPulseFrequency * Mathf.PI * 2) : 0;
            float main = Mathf.Max(0, zone.windMain);
            float force = main * (1 + Mathf.Max(0, zone.windPulseMagnitude) * pulse) * falloff;
            Vector3 wind = direction * force;
            return new Vector4(wind.x, wind.y, wind.z, Mathf.Max(0, zone.windTurbulence) * falloff);
        }

        public void ApplyWind(float time)
        {
            if (renderers == null || properties == null) return;
            Vector4 wind = Vector4.zero;
            if (Application.isPlaying || previewInEditMode)
            {
                if (windZone) wind = SampleZone(windZone, transform.position, time);
                else
                {
                    double now = Time.realtimeSinceStartupAsDouble;
                    if (zones == null || now >= nextSearch)
                    {
                        zones = FindObjectsByType<WindZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        nextSearch = now + 1;
                    }
                    foreach (var zone in zones)
                        if (zone && zone.gameObject.scene == gameObject.scene)
                            wind += SampleZone(zone, transform.position, time);
                }
            }
            float multiplier = Mathf.Clamp(windMultiplier, 0, 3);
            Vector3 force = Vector3.ClampMagnitude(new Vector3(wind.x, wind.y, wind.z), 3) * multiplier;
            wind = new Vector4(force.x, force.y, force.z, Mathf.Clamp(wind.w, 0, 3));
            if (!hasPreviousFrame) { previousWind = wind; previousTime = time; }
            float scale = Mathf.Max(.001f, transform.TransformVector(Vector3.up).magnitude);
            // Metre-scale, tall upright star gooseberry tree.
            var shape = new Vector4(1.0f * scale, 4.0f * scale, .09f * scale, .018f * scale);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer) continue;
                renderer.GetPropertyBlock(properties);
                properties.SetVector(WindId, wind);
                properties.SetVector(PreviousWindId, previousWind);
                properties.SetVector(TimeId, new Vector4(time, previousTime, 0, 0));
                properties.SetVector(RootId, transform.position);
                properties.SetVector(UpId, transform.up);
                properties.SetVector(ShapeId, shape);
                properties.SetFloat(LeafId, renderer.name.StartsWith("Leaves") ? Mathf.Clamp(leafFlutter, 0, 2) : 0);
                renderer.SetPropertyBlock(properties);
            }
            previousWind = wind;
            previousTime = time;
            hasPreviousFrame = true;
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorTick;
#endif
            if (renderers == null || properties == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i]) continue;
                renderers[i].GetPropertyBlock(properties);
                properties.SetVector(WindId, Vector4.zero);
                properties.SetVector(PreviousWindId, Vector4.zero);
                renderers[i].SetPropertyBlock(properties);
                renderers[i].localBounds = restBounds[i];
            }
        }
    }
}

