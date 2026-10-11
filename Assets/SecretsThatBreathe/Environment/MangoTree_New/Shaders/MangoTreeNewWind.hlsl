#ifndef MANGO_TREE_NEW_WIND_INCLUDED
#define MANGO_TREE_NEW_WIND_INCLUDED
// Per-renderer properties, supplied through MangoTreeWind's MaterialPropertyBlock.
float4 _MangoWind;
float4 _MangoPreviousWind;
float4 _MangoWindTime;
float4 _MangoRootWS;
float4 _MangoUpWS;
float4 _MangoShape;
float _MangoLeafFlutter;

float3 MangoOffsetWS(float3 p, float2 uv, float time, float4 wind)
{
    float height = dot(p - _MangoRootWS.xyz, _MangoUpWS.xyz);
    float weight = saturate((height - _MangoShape.x) / max(.001, _MangoShape.y));
    weight *= weight;
    float phase = dot(_MangoRootWS.xyz, float3(.73, .19, .41)) + height * .32;
    float sway = .65 + .25 * sin(time * 1.35 + phase) + .1 * sin(time * 2.17 + phase);
    float3 offset = wind.xyz * (_MangoShape.z * weight * sway);
    // The new leaf atlas has two rows. frac(uv.y * 2) restores local petiole-to-tip weight.
    // Flutter is disabled for wood and fruit by their renderer property block.
    float tip = saturate(frac(uv.y * 2.0)); tip *= tip;
    float flutter = sin(time * 7.3 + dot(p, float3(4.1, 2.7, 3.2)))
                  + .35 * sin(time * 12.7 + dot(p, float3(6.3, 3.1, -4.7)));
    float3 flutterDirection = wind.xyz * .22 + _MangoUpWS.xyz * min(length(wind.xyz), 3.0);
    offset += flutterDirection * (flutter * _MangoShape.w * tip * _MangoLeafFlutter * weight * (.35 + wind.w * .3));
    return offset;
}

float3 MangoPositionOS(float3 positionOS, float2 uv, float time, float4 wind)
{
    float3 p = TransformObjectToWorld(positionOS);
    return TransformWorldToObject(p + MangoOffsetWS(p, uv, time, wind));
}

// Transform the normal and tangent with the local deformation Jacobian.
void MangoDeformSurface(inout float3 position, inout float3 normal, inout float4 tangent, float2 uv)
{
    float3 original = position;
    float3 n = normalize(normal);
    float3 t = normalize(tangent.xyz);
    float3 b = normalize(cross(n, t));
    position = MangoPositionOS(original, uv, _MangoWindTime.x, _MangoWind);
    float3 dt = MangoPositionOS(original + t * .01, uv, _MangoWindTime.x, _MangoWind) - position;
    float3 db = MangoPositionOS(original + b * .01, uv, _MangoWindTime.x, _MangoWind) - position;
    normal = normalize(cross(dt, db));
    tangent.xyz = normalize(dt);
}
#endif
