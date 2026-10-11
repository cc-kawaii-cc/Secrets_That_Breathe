#ifndef MAYOM_TREE_WIND_INCLUDED
#define MAYOM_TREE_WIND_INCLUDED
// Per-renderer properties, supplied through MayomTreeWind's MaterialPropertyBlock.
float4 _MayomWind;
float4 _MayomPreviousWind;
float4 _MayomWindTime;
float4 _MayomRootWS;
float4 _MayomUpWS;
float4 _MayomShape;
float _MayomLeafFlutter;

float3 MayomOffsetWS(float3 p, float2 uv, float time, float4 wind)
{
    float height = dot(p - _MayomRootWS.xyz, _MayomUpWS.xyz);
    float weight = saturate((height - _MayomShape.x) / max(.001, _MayomShape.y));
    weight *= weight;
    float phase = dot(_MayomRootWS.xyz, float3(.73, .19, .41)) + height * .32;
    float sway = .65 + .25 * sin(time * 1.35 + phase) + .1 * sin(time * 2.17 + phase);
    float3 offset = wind.xyz * (_MayomShape.z * weight * sway);
    // Each atlas column is a complete leafy shoot; Y is its root-to-tip weight.
    // Flutter is disabled for wood and fruit by their renderer property block.
    float tip = saturate(uv.y); tip *= tip;
    float flutter = sin(time * 7.3 + dot(p, float3(4.1, 2.7, 3.2)))
                  + .35 * sin(time * 12.7 + dot(p, float3(6.3, 3.1, -4.7)));
    float3 flutterDirection = wind.xyz * .22 + _MayomUpWS.xyz * min(length(wind.xyz), 3.0);
    offset += flutterDirection * (flutter * _MayomShape.w * tip * _MayomLeafFlutter * weight * (.35 + wind.w * .3));
    return offset;
}

float3 MayomPositionOS(float3 positionOS, float2 uv, float time, float4 wind)
{
    float3 p = TransformObjectToWorld(positionOS);
    return TransformWorldToObject(p + MayomOffsetWS(p, uv, time, wind));
}

// Transform the normal and tangent with the local deformation Jacobian.
void MayomDeformSurface(inout float3 position, inout float3 normal, inout float4 tangent, float2 uv)
{
    float3 original = position;
    float3 n = normalize(normal);
    float3 t = normalize(tangent.xyz);
    float3 b = normalize(cross(n, t));
    position = MayomPositionOS(original, uv, _MayomWindTime.x, _MayomWind);
    float3 dt = MayomPositionOS(original + t * .01, uv, _MayomWindTime.x, _MayomWind) - position;
    float3 db = MayomPositionOS(original + b * .01, uv, _MayomWindTime.x, _MayomWind) - position;
    normal = normalize(cross(dt, db));
    tangent.xyz = normalize(dt);
}
#endif


