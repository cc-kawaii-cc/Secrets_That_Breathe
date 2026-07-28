// Copyright (c) 2016 Unity Technologies. MIT license - license_unity.txt
// #NVJOB Dynamic Sky v2.5.1
// Universal Render Pipeline compatibility pass for this project.

Shader "#NVJOB/Dynamic Sky"
{
    Properties
    {
        [HideInInspector][NoScaleOffset] _Texture1("Texture 1", 2D) = "white" {}
        [HideInInspector] _TextureUv1("Texture 1 Tiling", Float) = 1
        [HideInInspector] _IntensityT1("Intensity", Float) = 1.5
        [HideInInspector] _VectorX1("Motion Vector X", Float) = 0.9
        [HideInInspector] _VectorY1("Motion Vector Y", Float) = 1.0

        [HideInInspector][NoScaleOffset] _Texture2("Texture 2", 2D) = "gray" {}
        [HideInInspector] _TextureUv2("Texture 2 Tiling", Float) = 1
        [HideInInspector] _IntensityT2("Intensity", Float) = 1.5
        [HideInInspector] _VectorX2("Motion Vector X", Float) = 1.3
        [HideInInspector] _VectorY2("Motion Vector Y", Float) = 1.2

        [HideInInspector][NoScaleOffset] _Texture3("Texture 3", 2D) = "gray" {}
        [HideInInspector] _TextureUv3("Texture 3 Tiling", Float) = 1
        [HideInInspector] _IntensityT3("Intensity", Float) = -0.5
        [HideInInspector] _VectorX3("Motion Vector X", Float) = -1
        [HideInInspector] _VectorY3("Motion Vector Y", Float) = -1

        [HideInInspector][HDR] _Color("Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _IntensityInput("Intensity Input", Float) = 1.6
        [HideInInspector] _Fluffiness("Fluffiness", Float) = 0.75
        [HideInInspector] _IntensityOutput("Intensity Output", Float) = 1

        [HideInInspector][HDR] _Level1Color("Top Horizon Color", Color) = (0.65, 0.86, 0.63, 1)
        [HideInInspector] _Level1("Top Horizon Level", Float) = 10
        [HideInInspector][HDR] _Level0Color("Bottom Horizon Color", Color) = (0.37, 0.78, 0.92, 1)
        [HideInInspector] _Level0("Bottom Horizon Level", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry+501"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        LOD 400

        Pass
        {
            Name "DynamicSky"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local DSKY_CLOUD_1 DSKY_CLOUD_2 DSKY_HORIZON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Texture1);
            SAMPLER(sampler_Texture1);
            TEXTURE2D(_Texture2);
            SAMPLER(sampler_Texture2);
            TEXTURE2D(_Texture3);
            SAMPLER(sampler_Texture3);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _Level0Color;
                float4 _Level1Color;
                float _TextureUv1;
                float _IntensityT1;
                float _VectorX1;
                float _VectorY1;
                float _TextureUv2;
                float _IntensityT2;
                float _VectorX2;
                float _VectorY2;
                float _TextureUv3;
                float _IntensityT3;
                float _VectorX3;
                float _VectorY3;
                float _IntensityInput;
                float _Fluffiness;
                float _IntensityOutput;
                float _Level0;
                float _Level1;
            CBUFFER_END

            float _SkyShaderUvX;
            float _SkyShaderUvZ;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = input.uv - 1.0;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                #if defined(DSKY_HORIZON)
                    float levelRange = max(abs(_Level1 - _Level0), 0.0001);
                    float levelBlend = saturate((input.positionWS.y - _Level0) / levelRange);
                    return lerp(_Level0Color, _Level1Color, levelBlend);
                #else
                    float2 uv1 = input.uv + float2(_SkyShaderUvX * _VectorX1, _SkyShaderUvZ * _VectorY1);
                    float2 uv2 = input.uv + float2(_SkyShaderUvX * _VectorX2, _SkyShaderUvZ * _VectorY2);
                    float2 uv3 = input.uv + float2(_SkyShaderUvX * _VectorX3, _SkyShaderUvZ * _VectorY3);

                    float4 cloud = _Color;

                    #if defined(DSKY_CLOUD_2)
                        cloud *= SAMPLE_TEXTURE2D(_Texture1, sampler_Texture1, uv1 * _TextureUv1).r * _IntensityT1;
                        cloud *= SAMPLE_TEXTURE2D(_Texture2, sampler_Texture2, uv2 * _TextureUv2).g * _IntensityT2;
                        cloud *= SAMPLE_TEXTURE2D(_Texture3, sampler_Texture3, uv3 * _TextureUv3).b * _IntensityT3;
                    #else
                        cloud *= SAMPLE_TEXTURE2D(_Texture1, sampler_Texture1, uv1 * _TextureUv1) * _IntensityT1;
                        cloud *= SAMPLE_TEXTURE2D(_Texture2, sampler_Texture2, uv2 * _TextureUv2).r * _IntensityT2;
                        cloud *= SAMPLE_TEXTURE2D(_Texture3, sampler_Texture3, uv3 * _TextureUv3).r * _IntensityT3;
                    #endif

                    cloud *= _IntensityInput;
                    float alpha = cloud.a;
                    float4 shapedCloud = normalize((cloud - 0.5) * _Fluffiness + 0.5);
                    return half4(shapedCloud.rgb * _IntensityOutput, alpha);
                #endif
            }
            ENDHLSL
        }
    }

    CustomEditor "NVDSkyMaterials"
}
