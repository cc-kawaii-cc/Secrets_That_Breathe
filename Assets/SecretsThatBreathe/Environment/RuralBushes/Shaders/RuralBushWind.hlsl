#ifndef RURAL_BUSH_TREE_WIND_INCLUDED
#define RURAL_BUSH_TREE_WIND_INCLUDED
// Per-renderer properties, supplied through RuralBushWind's MaterialPropertyBlock.
float4 _RuralBushWind;
float4 _RuralBushPreviousWind;
float4 _RuralBushWindTime;
float4 _RuralBushRootWS;
float4 _RuralBushUpWS;
float4 _RuralBushShape;
float _RuralBushLeafFlutter;

float3 RuralBushOffsetWS(float3 p, float2 uv, float time, float4 wind)
{
    float height = dot(p - _RuralBushRootWS.xyz, _RuralBushUpWS.xyz);
    float weight = saturate((height - _RuralBushShape.x) / max(.001, _RuralBushShape.y));
    weight *= weight;
    float phase = dot(_RuralBushRootWS.xyz, float3(.73, .19, .41)) + height * .32;
    float sway = .65 + .25 * sin(time * 1.35 + phase) + .1 * sin(time * 2.17 + phase);
    float3 offset = wind.xyz * (_RuralBushShape.z * weight * sway);
    // Each atlas column is a complete leafy shoot; Y is its root-to-tip weight.
    // Flutter is disabled for wood and fruit by their renderer property block.
    float tip = saturate(uv.y); tip *= tip;
    float flutter = sin(time * 7.3 + dot(p, float3(4.1, 2.7, 3.2)))
                  + .35 * sin(time * 12.7 + dot(p, float3(6.3, 3.1, -4.7)));
    float3 flutterDirection = wind.xyz * .22 + _RuralBushUpWS.xyz * min(length(wind.xyz), 3.0);
    offset += flutterDirection * (flutter * _RuralBushShape.w * tip * _RuralBushLeafFlutter * weight * (.35 + wind.w * .3));
    return offset;
}

float3 RuralBushPositionOS(float3 positionOS, float2 uv, float time, float4 wind)
{
    float3 p = TransformObjectToWorld(positionOS);
    return TransformWorldToObject(p + RuralBushOffsetWS(p, uv, time, wind));
}

// Transform the normal and tangent with the local deformation Jacobian.
void RuralBushDeformSurface(inout float3 position, inout float3 normal, inout float4 tangent, float2 uv)
{
    float3 original = position;
    float3 n = normalize(normal);
    float3 t = normalize(tangent.xyz);
    float3 b = normalize(cross(n, t));
    position = RuralBushPositionOS(original, uv, _RuralBushWindTime.x, _RuralBushWind);
    float3 dt = RuralBushPositionOS(original + t * .01, uv, _RuralBushWindTime.x, _RuralBushWind) - position;
    float3 db = RuralBushPositionOS(original + b * .01, uv, _RuralBushWindTime.x, _RuralBushWind) - position;
    normal = normalize(cross(dt, db));
    tangent.xyz = normalize(dt);
}
#endif




