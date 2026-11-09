#ifndef MANGOTREE_WIND_INCLUDED
#define MANGOTREE_WIND_INCLUDED

// Global wind state, pushed every frame by MangoTreePack.MangoTreeWind (reads the scene's WindZone).
// _MG_WindDir  : xyz = world-space wind direction (normalised), w = 1 when a WindZone is active
// _MG_WindZone : x = Main, y = Turbulence, z = Pulse Magnitude, w = Pulse Frequency
float4 _MG_WindDir;
float4 _MG_WindZone;

// Wind bend + leaf flutter, computed ONLY from the object-space position. Because bark, leaves and fruit use the
// same function (same bend parameters) they move together, and every pass (forward / shadow / depth) agrees.
// Object space is Y-up with the pivot at the base of the trunk.
float3 MG_ApplyWind(float3 positionOS)
{
    if (_MG_WindDir.w < 0.5)
        return positionOS;

    float3 dirWS    = _MG_WindDir.xyz;
    float  windMain = _MG_WindZone.x;
    float  turb     = _MG_WindZone.y;

    float treeH = max(_MGWind.z, 0.5);
    float h0    = _MGWind2.y;
    float hN    = saturate((positionOS.y - h0) / max(treeH - h0, 0.1));
    float radN  = saturate(length(positionOS.xz) / max(_MGWind2.x, 0.1));
    // stiff trunk and inner limbs, flexible top and crown edge
    float w = hN * hN * (0.30 + 0.70 * radN);

    float3 pivotWS = GetObjectToWorldMatrix()._m03_m13_m23;
    float t = _Time.y;
    float phase = dot(pivotWS.xz, float2(0.71, 0.53));

    // gusts travelling through the orchard in the wind direction
    float wave  = sin(dot(pivotWS.xz, dirWS.xz) * _MGWind2.w - t * (0.9 + windMain * 0.8)) * 0.5 + 0.5;
    // WindZone pulse
    float pulse = 1.0 + _MG_WindZone.z * sin(t * 6.2831853 * max(_MG_WindZone.w, 0.01) + phase * 0.3);
    // per-tree sway and slow limb-to-limb variation
    float sway  = sin(t * _MGWind.w + phase) * 0.35 + 0.65;
    float limb  = sin(t * _MGWind.w * 1.37 + dot(positionOS, float3(1.9, 1.3, 2.3)) + phase) * 0.25;

    float bend = windMain * _MGWind.x * w * ((0.4 + 0.6 * wave) * sway * pulse + limb);

    // flutter of the leaves (the bark material sets flutter = 0)
    float3 f = sin(t * float3(6.3, 7.7, 5.1) * (0.6 + turb) * _MGWind2.z
                   + positionOS * float3(11.0, 9.0, 13.0) + phase);
    float3 flutterWS = f * (turb * _MGWind.y * (0.35 + 0.65 * w));

    float3 offsetWS = dirWS * bend + flutterWS;
    offsetWS.y -= abs(bend) * 0.25 * hN;       // lean down a little so branches keep their length

    float3 offsetOS = mul((float3x3)GetWorldToObjectMatrix(), offsetWS);
    return positionOS + offsetOS;
}

#endif
