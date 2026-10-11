using UnityEngine;

namespace SecretsThatBreathe.MangoTreeUpdate
{
    /// <summary>Shared wind for native Terrain.treeInstances, without spawning GameObjects.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    [AddComponentMenu("Environment/Mango Tree Terrain Wind")]
    public sealed class MangoTreeTerrainWind : MonoBehaviour
    {
        public const int MaxZones = 8;
        [Tooltip("Leave empty to use the active Wind Zones in this scene, up to eight.")]
        public WindZone windZone;
        [Range(0,3)] public float windMultiplier = 1;
        [Range(0,2)] public float leafFlutter = 1;
        public bool previewInEditMode = true;

        static MangoTreeTerrainWind owner;
        static readonly int CountId = Shader.PropertyToID("_MangoTerrainZoneCount");
        static readonly int PreviousCountId = Shader.PropertyToID("_MangoTerrainPreviousZoneCount");
        static readonly int ShapeId = Shader.PropertyToID("_MangoTerrainZoneShape");
        static readonly int PreviousShapeId = Shader.PropertyToID("_MangoTerrainPreviousZoneShape");
        static readonly int WindId = Shader.PropertyToID("_MangoTerrainZoneWind");
        static readonly int PreviousWindId = Shader.PropertyToID("_MangoTerrainPreviousZoneWind");
        static readonly int TimeId = Shader.PropertyToID("_MangoTerrainTime");
        static readonly int SettingsId = Shader.PropertyToID("_MangoTerrainSettings");
        static readonly int PreviousSettingsId = Shader.PropertyToID("_MangoTerrainPreviousSettings");
        readonly Vector4[] shapes = new Vector4[MaxZones];
        readonly Vector4[] forces = new Vector4[MaxZones];
        readonly Vector4[] previousShapes = new Vector4[MaxZones];
        readonly Vector4[] previousForces = new Vector4[MaxZones];
        WindZone[] zones;
        double nextSearch;
        float previousTime;
        Vector4 previousSettings;
        int previousCount;
        bool hasPrevious;
#if UNITY_EDITOR
        double lastEditorTick;
#endif

        void OnEnable()
        {
            if (!owner) owner=this;
            hasPrevious=false; nextSearch=0;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += EditorTick;
#endif
        }

        void LateUpdate() { if (Application.isPlaying) ApplyWind(Time.time); }

#if UNITY_EDITOR
        void EditorTick()
        {
            if (Application.isPlaying || !this) return;
            double now=UnityEditor.EditorApplication.timeSinceStartup;
            if (now-lastEditorTick<1.0/30.0) return;
            lastEditorTick=now;
            ApplyWind((float)now);
            if (previewInEditMode && owner==this) UnityEditor.SceneView.RepaintAll();
        }
#endif

        public void ApplyWind(float time)
        {
            if (!isActiveAndEnabled) return;
            if (!owner || !owner.isActiveAndEnabled) owner=this;
            if (owner!=this) return;
            int count=0;
            System.Array.Clear(shapes,0,MaxZones);
            System.Array.Clear(forces,0,MaxZones);
            if (Application.isPlaying || previewInEditMode)
            {
                if (windZone) AddZone(windZone,time,ref count);
                else
                {
                    double now=Time.realtimeSinceStartupAsDouble;
                    if (zones==null || now>=nextSearch)
                    {
                        zones=FindObjectsByType<WindZone>(FindObjectsInactive.Exclude,FindObjectsSortMode.InstanceID);
                        nextSearch=now+1;
                    }
                    foreach (var zone in zones)
                        if (zone && zone.gameObject.scene==gameObject.scene) AddZone(zone,time,ref count);
                }
            }
            Vector4 settings=new Vector4(Mathf.Clamp(windMultiplier,0,3),Mathf.Clamp(leafFlutter,0,2),0,0);
            if (!hasPrevious)
            {
                shapes.CopyTo(previousShapes,0); forces.CopyTo(previousForces,0);
                previousCount=count; previousTime=time; previousSettings=settings;
            }
            Shader.SetGlobalInt(CountId,count);
            Shader.SetGlobalInt(PreviousCountId,previousCount);
            Shader.SetGlobalVectorArray(ShapeId,shapes);
            Shader.SetGlobalVectorArray(PreviousShapeId,previousShapes);
            Shader.SetGlobalVectorArray(WindId,forces);
            Shader.SetGlobalVectorArray(PreviousWindId,previousForces);
            Shader.SetGlobalVector(TimeId,new Vector4(time,previousTime,0,0));
            Shader.SetGlobalVector(SettingsId,settings);
            Shader.SetGlobalVector(PreviousSettingsId,previousSettings);
            shapes.CopyTo(previousShapes,0); forces.CopyTo(previousForces,0);
            previousCount=count; previousTime=time; previousSettings=settings; hasPrevious=true;
        }

        void AddZone(WindZone zone,float time,ref int count)
        {
            if (count>=MaxZones || !zone.gameObject.activeInHierarchy) return;
            bool spherical=zone.mode==WindZoneMode.Spherical;
            if (spherical && zone.radius<=0) return;
            Vector3 p=zone.transform.position;
            shapes[count]=new Vector4(p.x,p.y,p.z,spherical ? zone.radius : -1);
            forces[count]=MangoTreeUpdateWind.SampleZone(zone,p,time);
            count++;
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorTick;
#endif
            if (owner!=this) return;
            Shader.SetGlobalInt(CountId,0);
            Shader.SetGlobalInt(PreviousCountId,0);
            owner=null;
        }
    }
}
