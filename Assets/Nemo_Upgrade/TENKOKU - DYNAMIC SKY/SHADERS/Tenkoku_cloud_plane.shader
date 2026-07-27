Shader "TENKOKU/cloud_plane"
{
    Properties
    {
        _dist ("Distance", Float) = 500.0
        _brightMult ("Brightness", Float) = 1.0
        _cloudHeight ("Cloud Height", Float) = 1.0
        _sizeCloud ("Cloud Size", Range(0.0, 1.0)) = 1.0
        _amtCloudS ("Cloud Stratus", Range(0.0, 1.0)) = 1.0
        _amtCloudC ("Cloud Cirrus", Range(0.0, 1.0)) = 1.0
        _amtCloudM ("Cloud Cumulus", Range(0.0, 1.0)) = 1.0
        _amtCloudO ("Cloud Overcast", Range(0.0, 1.0)) = 1.0
        _clpCloud ("Cloud Clip", Range(0.0, 1.0)) = 0.0
        _colTint ("Cloud Tint", Color) = (1,1,1,1)
        _colCloudS ("Cloud Stratus Color", Color) = (1,1,1,1)
        _colCloudC ("Cloud Cirrus Color", Color) = (1,1,1,1)
        _colCloud ("Cloud Cumulus Color", Color) = (1,1,1,1)
        _colCloudO ("Cloud Overcast Color", Color) = (1,1,1,1)
        _MainTex ("Clouds A", 2D) = "white" {}
        _CloudTexB ("Clouds B", 2D) = "white" {}
        _BlendTex ("Blend", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_CloudTexB);
            SAMPLER(sampler_CloudTexB);
            TEXTURE2D(_BlendTex);
            SAMPLER(sampler_BlendTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _CloudTexB_ST;
                float4 _BlendTex_ST;
                float4 _colTint;
                float4 _colCloudS;
                float4 _colCloudC;
                float4 _colCloud;
                float4 _colCloudO;
                float _brightMult;
                float _sizeCloud;
                float _amtCloudS;
                float _amtCloudC;
                float _amtCloudM;
                float _amtCloudO;
                float _clpCloud;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uvA = TRANSFORM_TEX(input.uv, _MainTex);
                float2 uvB = TRANSFORM_TEX(input.uv, _CloudTexB);
                float2 uvBlend = TRANSFORM_TEX(input.uv, _BlendTex);

                half4 cloudsA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvA);
                half4 cloudsB = SAMPLE_TEXTURE2D(_CloudTexB, sampler_CloudTexB, uvB * lerp(1.5, 0.75, _sizeCloud));
                half blendMask = SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, uvBlend).r;

                half3 tint = (_colCloudS.rgb * _amtCloudS)
                    + (_colCloudC.rgb * _amtCloudC)
                    + (_colCloud.rgb * _amtCloudM)
                    + (_colCloudO.rgb * _amtCloudO);
                tint = max(tint, _colTint.rgb * 0.35);

                half alpha = cloudsA.b * _amtCloudS;
                alpha += cloudsA.g * _amtCloudC;
                alpha += saturate(dot(cloudsB.rgb, half3(0.35, 0.4, 0.25))) * _amtCloudM;
                alpha += cloudsA.r * _amtCloudO;
                alpha *= blendMask;
                alpha = saturate(alpha - _clpCloud);

                half3 color = saturate(tint * _brightMult);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
