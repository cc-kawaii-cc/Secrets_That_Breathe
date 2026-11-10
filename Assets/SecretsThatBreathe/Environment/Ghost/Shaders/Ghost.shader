// See-through "shadow" look for ghosts (URP): dark, desaturated, a cold rim around the outline, wisps of noise drifting up,
// occasional flicker and a small glitch jitter. Works on skinned meshes. _Fade (0..1) fades the whole ghost in / out.
// The shape is never inflated: the glitch only slides thin horizontal slices sideways, in world metres (so a model's own
// import scale does not change how far it moves). _Jitter 0 keeps the exact original shape.
Shader "SecretsThatBreathe/Ghost"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Tint ("Body Colour", Color) = (0.30, 0.33, 0.38, 1)
        _Saturation ("Texture Colour Amount", Range(0, 1)) = 0.25
        _Alpha ("Body Opacity", Range(0, 1)) = 0.32
        _RimColor ("Rim Colour", Color) = (0.62, 0.74, 0.86, 1)
        _RimPower ("Rim Sharpness", Range(0.5, 8)) = 2.2
        _RimAlpha ("Rim Opacity", Range(0, 1)) = 0.75
        _WispScale ("Wisp Size", Float) = 3.0
        _WispSpeed ("Wisp Speed", Float) = 0.6
        _WispAmount ("Wisp Amount", Range(0, 1)) = 0.55
        _Flicker ("Flicker", Range(0, 1)) = 0.35
        _Jitter ("Glitch Slide (m)", Range(0, 0.1)) = 0.012
        _Fade ("Fade", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _Tint;
            half _Saturation;
            half _Alpha;
            half4 _RimColor;
            half _RimPower;
            half _RimAlpha;
            float _WispScale;
            float _WispSpeed;
            half _WispAmount;
            half _Flicker;
            float _Jitter;
            half _Fade;
        CBUFFER_END

        float Hash(float n) { return frac(sin(n) * 43758.5453); }
        float Noise3(float3 p)
        {
            float3 i = floor(p); float3 f = frac(p); f = f * f * (3.0 - 2.0 * f);
            float n = i.x + i.y * 57.0 + i.z * 113.0;
            return lerp(lerp(lerp(Hash(n), Hash(n + 1.0), f.x), lerp(Hash(n + 57.0), Hash(n + 58.0), f.x), f.y),
                        lerp(lerp(Hash(n + 113.0), Hash(n + 114.0), f.x), lerp(Hash(n + 170.0), Hash(n + 171.0), f.x), f.y), f.z);
        }
        float3 GhostPositionWS(float3 positionOS)
        {
            float3 positionWS = TransformObjectToWorld(positionOS);
            float slot = floor(_Time.y * 14.0);
            float on = step(0.9, Hash(slot * 1.37));                         // a glitch now and then
            float band = step(0.5, Hash(floor(positionWS.y * 9.0) + slot));  // thin horizontal slices shift sideways
            positionWS.x += (Hash(slot + positionWS.y * 3.1) - 0.5) * 2.0 * _Jitter * on * band;
            return positionWS;
        }
        ENDHLSL

        // depth first, so only the front-most surface of the ghost is drawn (no arms showing through the body)
        Pass
        {
            Name "GhostDepth"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformWorldToHClip(GhostPositionWS(input.positionOS.xyz));
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "GhostColour"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = GhostPositionWS(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 frag(Varyings input, bool front : SV_IsFrontFace) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half grey = dot(texel.rgb, half3(0.299, 0.587, 0.114));
                half3 body = lerp(grey.xxx, texel.rgb, _Saturation) * _Tint.rgb;
                float3 n = normalize(front ? input.normalWS : -input.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(input.positionWS));
                half rim = pow(1.0 - saturate(abs(dot(n, v))), _RimPower);
                float3 p = input.positionWS * _WispScale + float3(0, -_Time.y * _WispSpeed * _WispScale, 0);
                half wisp = Noise3(p) * 0.65 + Noise3(p * 2.3) * 0.35;
                half flick = 1.0 - _Flicker * step(0.82, Hash(floor(_Time.y * 18.0))) * 0.8 - _Flicker * 0.15 * sin(_Time.y * 37.0);
                half alpha = (_Alpha * lerp(1.0, wisp * 1.6, _WispAmount) + rim * _RimAlpha) * flick * _Fade;
                half3 colour = body + _RimColor.rgb * rim;
                colour = MixFog(colour, input.fogFactor);
                return half4(colour, saturate(alpha));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
