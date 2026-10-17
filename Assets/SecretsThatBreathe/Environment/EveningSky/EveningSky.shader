Shader "Secrets That Breathe/Skybox/Evening Clouds"
{
    Properties
    {
        [Header(Sky)]
        _ZenithColor ("Upper Sky", Color) = (0.1, 0.16, 0.23, 1)
        _HorizonColor ("Warm Horizon", Color) = (0.38, 0.27, 0.105, 1)
        _GroundColor ("Below Horizon", Color) = (0.025, 0.035, 0.055, 1)
        _Exposure ("Sky Brightness", Range(0, 3)) = 0.8
        _Rotation ("Rotation", Range(0, 360)) = 0
        [Header(Clouds)]
        _CloudColor ("Sunset Cloud Color", Color) = (0.9, 0.32, 0.2, 1)
        _CloudShadow ("Cloud Shadow", Color) = (0.12, 0.13, 0.16, 1)
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.36
        _CloudScale ("Cloud Scale", Range(0.3, 4)) = 1.3
        _CloudBrightness ("Cloud Brightness", Range(0, 3)) = 1.15
        [Header(Sun)]
        _SunColor ("Sun Color", Color) = (1, 0.9, 0.68, 1)
        _SunAzimuth ("Sun Direction", Range(0, 360)) = 245
        _SunElevation ("Sun Height", Range(-5, 45)) = 6
        _SunSize ("Sun Radius In Degrees", Range(0.05, 3)) = 0.6
        _SunBrightness ("Sun Brightness", Range(0, 8)) = 2
        [Header(Fog Blending)]
        [Toggle] _UseSceneFogColor ("Use Scene Fog Color", Float) = 1
        _FogColor ("Manual Fog Color", Color) = (0, 0, 0, 1)
        _HorizonFog ("Horizon Fade To Fog", Range(0, 1)) = 1
        _FogHeight ("Horizon Fade Height", Range(0.01, 0.6)) = 0.14
        _FogBlend ("Whole Sky Fade To Fog", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            float4 _ZenithColor, _HorizonColor, _GroundColor;
            float4 _CloudColor, _CloudShadow, _SunColor, _FogColor;
            float _Exposure, _Rotation, _CloudCoverage, _CloudScale, _CloudBrightness;
            float _SunAzimuth, _SunElevation, _SunSize, _SunBrightness;
            float _UseSceneFogColor, _HorizonFog, _FogHeight, _FogBlend;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.direction = v.vertex.xyz;
                return o;
            }

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            float noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(cell), hash(cell + float2(1, 0)), f.x),
                            lerp(hash(cell + float2(0, 1)), hash(cell + 1.0), f.x), f.y);
            }
            float fbm(float2 p)
            {
                float result = 0, weight = 0.5;
                [unroll] for (int j = 0; j < 5; j++)
                {
                    result += noise(p) * weight;
                    p = float2(p.x * 1.6 - p.y * 1.2, p.x * 1.2 + p.y * 1.6) + 19.1;
                    weight *= 0.5;
                }
                return result;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 d = normalize(i.direction);
                float angle = radians(_Rotation);
                d.xz = float2(d.x * cos(angle) - d.z * sin(angle),
                              d.x * sin(angle) + d.z * cos(angle));
                float height = saturate(d.y);
                float3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(height, 0.42));
                float az = radians(_SunAzimuth), el = radians(_SunElevation);
                float3 sun = float3(sin(az) * cos(el), sin(el), cos(az) * cos(el));
                float sunDot = dot(d, sun);
                sky += _SunColor.rgb * pow(saturate(sunDot), 18.0) * 0.08;

                // Project onto a horizontal cloud layer: continuous in every direction,
                // with naturally compressed cloud detail toward the horizon.
                float2 p = d.xz / max(d.y + 0.12, 0.045) * 2.4 * _CloudScale;
                float warp = fbm(p * 0.35);
                float broad = fbm(p + warp * 2.3);
                float detail = fbm(p * 3.6 + float2(31.7, 8.4));
                float density = broad * 0.8 + detail * 0.2;
                float threshold = lerp(0.76, 0.28, _CloudCoverage);
                float cloud = smoothstep(threshold, threshold + 0.085, density);
                cloud *= smoothstep(-0.015, 0.04, d.y);
                float lit = saturate((detail - 0.25) * 1.8 + broad * 0.5);
                float3 cloudColor = lerp(_CloudShadow.rgb, _CloudColor.rgb, lit) * _CloudBrightness;
                sky = lerp(sky, cloudColor, cloud * 0.92);

                float radius = radians(_SunSize);
                float disk = smoothstep(cos(radius * 1.1), cos(radius * 0.9), sunDot);
                sky += _SunColor.rgb * disk * _SunBrightness * (1.0 - cloud * 0.75);
                sky = lerp(_GroundColor.rgb, sky, smoothstep(-0.025, 0.015, d.y));
                sky *= _Exposure;

                // Skyboxes have no useful world distance. Blend explicitly into the
                // scene's fog color so the far scenery and sky meet without a seam.
                float3 fog = lerp(_FogColor.rgb, unity_FogColor.rgb, saturate(_UseSceneFogColor));
                float horizonFade = (1.0 - smoothstep(0, _FogHeight, abs(d.y))) * _HorizonFog;
                sky = lerp(sky, fog, saturate(horizonFade));
                sky = lerp(sky, fog, _FogBlend);
                return float4(sky, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
