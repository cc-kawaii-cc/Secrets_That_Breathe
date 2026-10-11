#ifndef MANGO_TREE_TERRAIN_WIND_INCLUDED
#define MANGO_TREE_TERRAIN_WIND_INCLUDED
// Terrain tree instances do not run prefab MonoBehaviours. One scene controller
// uploads zones; each native tree draw derives its own root and scale from its matrix.
#define MANGO_TERRAIN_MAX_ZONES 8
float4 _MangoTerrainTime;
float4 _MangoTerrainSettings;
float4 _MangoTerrainPreviousSettings;
int _MangoTerrainZoneCount;
int _MangoTerrainPreviousZoneCount;
float4 _MangoTerrainZoneShape[MANGO_TERRAIN_MAX_ZONES];
float4 _MangoTerrainPreviousZoneShape[MANGO_TERRAIN_MAX_ZONES];
float4 _MangoTerrainZoneWind[MANGO_TERRAIN_MAX_ZONES];
float4 _MangoTerrainPreviousZoneWind[MANGO_TERRAIN_MAX_ZONES];
float _MangoTerrainLeafFlutter;

float4 MangoTerrainSample(bool previous)
{
    float3 root = TransformObjectToWorld(float3(0,0,0));
    float4 wind = 0;
    int count = previous ? _MangoTerrainPreviousZoneCount : _MangoTerrainZoneCount;
    [loop] for (int i=0; i<count && i<MANGO_TERRAIN_MAX_ZONES; i++)
    {
        float4 shape = previous ? _MangoTerrainPreviousZoneShape[i] : _MangoTerrainZoneShape[i];
        float4 force = previous ? _MangoTerrainPreviousZoneWind[i] : _MangoTerrainZoneWind[i];
        if (shape.w >= 0)
        {
            float3 delta = root-shape.xyz;
            float distance = length(delta);
            float falloff = saturate(1-distance/max(.0001,shape.w));
            falloff *= falloff;
            float3 direction = distance>.0001 ? delta/distance : normalize(force.xyz+float3(0,0,.000001));
            wind.xyz += direction * length(force.xyz) * falloff;
            wind.w += force.w * falloff;
        }
        else wind += force;
    }
    float multiplier = previous ? _MangoTerrainPreviousSettings.x : _MangoTerrainSettings.x;
    wind.xyz *= min(1.0,3.0/max(.0001,length(wind.xyz))) * multiplier;
    wind.w = clamp(wind.w,0,3);
    return wind;
}

// Preserve the shared URP vertex and motion-vector wrapper interface.
#define _MangoWindTime _MangoTerrainTime
#define _MangoWind MangoTerrainSample(false)
#define _MangoPreviousWind MangoTerrainSample(true)

float3 MangoOffsetWS(float3 p, float2 uv, float time, float4 wind)
{
    float3 root = TransformObjectToWorld(float3(0,0,0));
    float3 up = TransformObjectToWorldDir(float3(0,1,0),false);
    float scale = max(.001,length(up));
    up /= scale;
    float height = dot(p-root,up);
    float weight = saturate((height-1.4*scale)/(5.8*scale));
    weight *= weight;
    float phase = dot(root,float3(.73,.19,.41))+height*.32;
    float sway = .65+.25*sin(time*1.35+phase)+.1*sin(time*2.17+phase);
    float3 offset = wind.xyz*(.10*scale*weight*sway);
    float tip = saturate((frac(uv.y*2.0)-.005)/.99); tip *= tip;
    float flutter = sin(time*7.3+dot(p,float3(4.1,2.7,3.2)))
                  + .35*sin(time*12.7+dot(p,float3(6.3,3.1,-4.7)));
    float3 flutterDirection = wind.xyz*.22+up*min(length(wind.xyz),3.0);
    offset += flutterDirection*(flutter*.025*scale*tip*_MangoTerrainLeafFlutter
        *_MangoTerrainSettings.y*weight*(.35+wind.w*.3));
    return offset;
}

float3 MangoPositionOS(float3 positionOS, float2 uv, float time, float4 wind)
{
    float3 p = TransformObjectToWorld(positionOS);
    return TransformWorldToObject(p+MangoOffsetWS(p,uv,time,wind));
}

void MangoDeformSurface(inout float3 position, inout float3 normal, inout float4 tangent, float2 uv)
{
    float3 original = position;
    float3 n = normalize(normal);
    float3 t = normalize(tangent.xyz);
    float3 b = normalize(cross(n,t));
    float4 wind = _MangoWind;
    position = MangoPositionOS(original,uv,_MangoWindTime.x,wind);
    float3 dt = MangoPositionOS(original+t*.01,uv,_MangoWindTime.x,wind)-position;
    float3 db = MangoPositionOS(original+b*.01,uv,_MangoWindTime.x,wind)-position;
    normal = normalize(cross(dt,db));
    tangent.xyz = normalize(dt);
}
#endif
