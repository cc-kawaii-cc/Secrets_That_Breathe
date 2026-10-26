#ifndef BANANA_WIND_INCLUDED
#define BANANA_WIND_INCLUDED

// ---------------------------------------------------------------------------------------------
// Banana tree wind. Driven by a WindZone through BananaWindController (global shader values).
//
// Vertex colour layout written by the FBX (linear values, 0..1):
//   R = trunk bend weight   (0 at the ground -> 1 at the crown, already squared)
//   G = leaf / bunch sway weight (0 at the leaf base -> 1 at the tip)
//   B = blade flutter weight  (0 at the midrib -> 1 at the leaf edge / tip)
//   A = random phase per leaf / bunch (0..1)
//
// Globals (NOT part of UnityPerMaterial, so the SRP Batcher keeps working):
//   _BananaWindDir    xyz = wind direction (world), w = WindZone "Main" * strength
//   _BananaWindParams x = turbulence, y = pulse magnitude, z = pulse speed (Hz), w = unused
// With no controller in the scene all values are 0 and the tree stays still.
// ---------------------------------------------------------------------------------------------

float4 _BananaWindDir;
float4 _BananaWindParams;

float3 BananaApplyWind(float3 positionWS, float3 normalWS, float3 originWS, float4 windData,
                       float windOn, float bendMul, float swayMul, float flutterMul)
{
    float windMain = _BananaWindDir.w * windOn;
    if (windMain <= 0.0001)
        return positionWS;

    float turbulence = _BananaWindParams.x;
    float pulseMag   = _BananaWindParams.y;
    float pulseSpeed = _BananaWindParams.z;

    float3 dir = float3(_BananaWindDir.x, 0.0, _BananaWindDir.z);
    dir = dir / max(length(dir), 1e-4);
    float3 side = float3(-dir.z, 0.0, dir.x);

    float t = _Time.y;
    // every tree gets its own phase, derived from its pivot position
    float treePhase = dot(originWS.xz, float2(0.113, 0.157));

    // slow gust travelling along the wind direction
    float gust = sin(t * pulseSpeed * 6.2831853 + dot(originWS.xz, dir.xz) * 0.35);
    float amount = windMain * max(1.0 + pulseMag * gust, 0.0);

    float bend    = windData.r;
    float sway    = windData.g;
    float flutter = windData.b;
    float phase   = windData.a * 6.2831853 + treePhase;

    // 1) trunk lean + a little side-to-side wobble
    float3 disp = dir * (amount * bend * bendMul * 0.18);
    disp += side * (sin(t * 1.3 + treePhase) * bend * turbulence * 0.03 * windMain * bendMul);
    disp.y -= dot(disp.xz, disp.xz) * 0.9 * bend;        // keep the trunk length roughly constant

    // 2) leaves / bunch swing around their attachment point
    float swayW = sway * sway;
    disp += dir * (amount * swayW * swayMul * 0.30 * (0.75 + 0.25 * sin(t * 2.1 + phase)));
    disp += side * (turbulence * swayW * swayMul * 0.06 * sin(t * 1.6 + phase * 1.3));
    disp.y += swayW * swayMul * (amount * 0.10 * sin(t * 1.9 + phase) + turbulence * 0.04 * sin(t * 3.3 + phase * 1.7));

    // 3) fast flutter of the blade edges / tips, along the surface normal
    float fl = flutter * flutterMul * turbulence * windMain * 0.05;
    disp += normalWS * (fl * sin(t * 7.5 + positionWS.x * 3.1 + positionWS.z * 2.3 + phase * 2.0));

    return positionWS + disp;
}

#endif
