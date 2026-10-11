Shader "TreePackVol1/URP Leaves"
{
    Properties
    {
        _MainTex ("Base Map", 2D) = "white" {}
        _Color ("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 1
        _TranslucencyColor ("Translucency Color", Color) = (0.73, 0.85, 0.41, 1)
        _TranslucencyViewDependency ("Translucency View Dependency", Range(0, 1)) = 0.7
        [HideInInspector] _TreeInstanceColor ("Tree Instance Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _TreeInstanceScale ("Tree Instance Scale", Vector) = (1, 1, 1, 1)
        [HideInInspector] _SquashPlaneNormal ("Squash Plane", Vector) = (0, 1, 0, 0)
        [HideInInspector] _SquashAmount ("Squash Amount", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        Cull Off
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _ShadowStrength;
                half4 _TranslucencyColor;
                half _TranslucencyViewDependency;
                half4 _TreeInstanceColor;
                float4 _TreeInstanceScale;
                float4 _SquashPlaneNormal;
                float _SquashAmount;
            CBUFFER_END

            void RestoreTreeLeaf(inout float3 positionOS, inout half3 normalOS, float4 tangentOS)
            {
                float isBillboard = 1.0 - abs(tangentOS.w);
                float3 billboardNormal = normalize(mul(float4(normalOS, 0.0), UNITY_MATRIX_IT_MV).xyz);
                positionOS += mul(float4(normalOS.xy, 0.0, 0.0), UNITY_MATRIX_IT_MV).xyz * isBillboard;
                normalOS = lerp(normalOS, billboardNormal, isBillboard);

                positionOS *= _TreeInstanceScale.xyz;
                float3 planeNormal = _SquashPlaneNormal.xyz;
                float3 projected = positionOS - (dot(planeNormal, positionOS) + _SquashPlaneNormal.w) * planeNormal;
                positionOS = lerp(projected, positionOS, saturate(_SquashAmount));
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 restoredPositionOS = input.positionOS.xyz;
                half3 restoredNormalOS = input.normalOS;
                RestoreTreeLeaf(restoredPositionOS, restoredNormalOS, input.tangentOS);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(restoredPositionOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = GetVertexNormalInputs(restoredNormalOS).normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color * _TreeInstanceColor;
                clip(albedo.a - _Cutoff);

                half3 normalWS = normalize(input.normalWS);
                normalWS *= IS_FRONT_VFACE(face, 1.0h, -1.0h);
                half3 lighting = SampleSH(normalWS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                lighting += mainLight.color * saturate(dot(normalWS, mainLight.direction)) *
                            mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                #if defined(_ADDITIONAL_LIGHTS)
                    uint lightCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex)
                    {
                        Light light = GetAdditionalLight(lightIndex, input.positionWS);
                        lighting += light.color * saturate(dot(normalWS, light.direction)) *
                                    light.distanceAttenuation * light.shadowAttenuation;
                    }
                #endif

                albedo.rgb = MixFog(albedo.rgb * lighting, input.fogFactor);
                return albedo;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _ShadowStrength;
                half4 _TranslucencyColor;
                half _TranslucencyViewDependency;
                half4 _TreeInstanceColor;
                float4 _TreeInstanceScale;
                float4 _SquashPlaneNormal;
                float _SquashAmount;
            CBUFFER_END

            void RestoreTreeLeafShadow(inout float3 positionOS, inout half3 normalOS, float4 tangentOS)
            {
                float isBillboard = 1.0 - abs(tangentOS.w);
                float3 billboardNormal = normalize(mul(float4(normalOS, 0.0), UNITY_MATRIX_IT_MV).xyz);
                positionOS += mul(float4(normalOS.xy, 0.0, 0.0), UNITY_MATRIX_IT_MV).xyz * isBillboard;
                normalOS = lerp(normalOS, billboardNormal, isBillboard);
                positionOS *= _TreeInstanceScale.xyz;
                float3 planeNormal = _SquashPlaneNormal.xyz;
                float3 projected = positionOS - (dot(planeNormal, positionOS) + _SquashPlaneNormal.w) * planeNormal;
                positionOS = lerp(projected, positionOS, saturate(_SquashAmount));
            }

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 restoredPositionOS = input.positionOS.xyz;
                half3 restoredNormalOS = input.normalOS;
                RestoreTreeLeafShadow(restoredPositionOS, restoredNormalOS, input.tangentOS);
                float3 positionWS = TransformObjectToWorld(restoredPositionOS);
                half3 normalWS = TransformObjectToWorldNormal(restoredNormalOS);
                float3 lightDirectionWS = normalize(_LightPosition.xyz - positionWS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #else
                    output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #endif
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _Color.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _ShadowStrength;
                half4 _TranslucencyColor;
                half _TranslucencyViewDependency;
                half4 _TreeInstanceColor;
                float4 _TreeInstanceScale;
                float4 _SquashPlaneNormal;
                float _SquashAmount;
            CBUFFER_END

            float3 RestoreTreeLeafDepth(float3 positionOS, half3 normalOS, float4 tangentOS)
            {
                float isBillboard = 1.0 - abs(tangentOS.w);
                positionOS += mul(float4(normalOS.xy, 0.0, 0.0), UNITY_MATRIX_IT_MV).xyz * isBillboard;
                positionOS *= _TreeInstanceScale.xyz;
                float3 planeNormal = _SquashPlaneNormal.xyz;
                float3 projected = positionOS - (dot(planeNormal, positionOS) + _SquashPlaneNormal.w) * planeNormal;
                return lerp(projected, positionOS, saturate(_SquashAmount));
            }

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 restoredPositionOS = RestoreTreeLeafDepth(input.positionOS.xyz, input.normalOS, input.tangentOS);
                output.positionCS = TransformObjectToHClip(restoredPositionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _Color.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
