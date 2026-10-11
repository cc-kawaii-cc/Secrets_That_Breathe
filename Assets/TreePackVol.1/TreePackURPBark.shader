Shader "TreePackVol1/URP Bark"
{
    Properties
    {
        _MainTex ("Base Map", 2D) = "white" {}
        _Color ("Base Color", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.2
        [HideInInspector] _TreeInstanceColor ("Tree Instance Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _TreeInstanceScale ("Tree Instance Scale", Vector) = (1, 1, 1, 1)
        [HideInInspector] _SquashPlaneNormal ("Squash Plane", Vector) = (0, 1, 0, 0)
        [HideInInspector] _SquashAmount ("Squash Amount", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        Cull Back
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
                half _Smoothness;
                half4 _TreeInstanceColor;
                float4 _TreeInstanceScale;
                float4 _SquashPlaneNormal;
                float _SquashAmount;
            CBUFFER_END

            float3 RestoreTreeBarkPosition(float3 positionOS)
            {
                float3 scaled = positionOS * _TreeInstanceScale.xyz;
                float3 planeNormal = _SquashPlaneNormal.xyz;
                float3 projected = scaled - (dot(planeNormal, scaled) + _SquashPlaneNormal.w) * planeNormal;
                return lerp(projected, scaled, saturate(_SquashAmount));
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
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
                float3 restoredPositionOS = RestoreTreeBarkPosition(input.positionOS.xyz);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(restoredPositionOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = GetVertexNormalInputs(input.normalOS).normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color * _TreeInstanceColor;
                half3 normalWS = normalize(input.normalWS);
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

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }

    FallBack Off
}
