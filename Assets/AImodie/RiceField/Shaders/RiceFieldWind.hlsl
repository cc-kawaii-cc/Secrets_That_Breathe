#ifndef RICEFIELD_WIND_INCLUDED
#define RICEFIELD_WIND_INCLUDED

// Global wind state, pushed every frame by RiceField.RiceFieldWind (reads the scene's WindZone).
// _RF_WindDir  : xyz = world-space wind direction (normalised), w = 1 when a WindZone is active
// _RF_WindZone : x = Main, y = Turbulence, z = Pulse Magnitude, w = Pulse Frequency
float4 _RF_WindDir;
float4 _RF_WindZone;

// Bends the plant away from the wind. Object space is Y-up with the pivot at the root,
// so the bend weight comes from the vertex height (stiff base, flexible top).
float3 RF_ApplyWind(float3 positionOS)
{
    if (_RF_WindDir.w < 0.5)
        return positionOS;

    float3 dirWS   = _RF_WindDir.xyz;
    float  windMain = _RF_WindZone.x;
    float  turb     = _RF_WindZone.y;

    float plantH = max(_RFWind.z, 0.05);
    float h = saturate(positionOS.y / plantH);
    float bendW = h * h;

    float3 pivotWS = GetObjectToWorldMatrix()._m03_m13_m23;
    float t = _Time.y;
    float phase = dot(pivotWS.xz, float2(0.71, 0.53));

    // gusts travelling across the field in the wind direction
    float wave = sin(dot(pivotWS.xz, dirWS.xz) * _RFWindWave - t * (1.1 + windMain * 0.9)) * 0.5 + 0.5;
    // WindZone pulse
    float pulse = 1.0 + _RF_WindZone.z * sin(t * 6.2831853 * max(_RF_WindZone.w, 0.01) + phase * 0.3);
    // per-plant sway
    float sway = sin(t * _RFWind.w + phase) * 0.35 + 0.65;

    float bend = windMain * _RFWind.x * bendW * (0.4 + 0.6 * wave) * sway * pulse;

    // turbulence: small, faster flutter of leaves / panicles
    float3 f = sin(t * float3(6.3, 7.7, 5.1) * (0.6 + turb) + positionOS * 13.0 + phase);
    float3 flutterWS = f * (turb * _RFWind.y * h);

    float3 offsetWS = dirWS * bend + flutterWS;
    offsetWS.y -= abs(bend) * 0.35 * h;      // lean down a little so the blade keeps its length

    float3 offsetOS = mul((float3x3)GetWorldToObjectMatrix(), offsetWS);
    return positionOS + offsetOS;
}

#endif
