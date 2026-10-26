// Banana tree shader for the Universal Render Pipeline (Unity 6000.x).
// - Albedo + alpha clip (leaf), tangent-space normal map (trunk), two-sided lighting for leaves
// - Vertex-animated wind driven by a WindZone (see BananaWindController.cs / BananaWind.hlsl)
// - Passes: UniversalForward, ShadowCaster, DepthOnly, DepthNormals (so SSAO / soft shadows work)
Shader "Custom/Banana Tree (URP Wind)"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        [MainColor]   _BaseColor("Tint", Color) = (1, 1, 1, 1)
        [Normal]      _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0, 2)) = 1
        _Smoothness("Smoothness", Range(0, 1)) = 0.25
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip (leaf)", Float) = 0
        [Toggle(_NORMALMAP)] _UseNormalMap("Use Normal Map", Float) = 0
        _YoungTint("Young Leaf Tint (suckers)", Color) = (1.3, 1.2, 0.65, 1)
        _Translucency("Leaf Translucency", Range(0, 1)) = 0.12

        [Header(Wind)]
        [Toggle] _WindOn("Wind", Float) = 1
        _BendMul("Trunk Bend", Range(0, 3)) = 1
        _SwayMul("Leaf / Bunch Sway", Range(0, 3)) = 1
        _FlutterMul("Leaf Flutter", Range(0, 3)) = 1

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull (Off = two sided)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "BananaWind.hlsl"

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half4  _YoungTint;
            half   _BumpScale;
            half   _Smoothness;
            half   _Cutoff;
            half   _Translucency;
            half   _WindOn;
            half   _BendMul;
            half   _SwayMul;
            half   _FlutterMul;
        CBUFFER_END
        ENDHLSL

        // ---------------------------------------------------------------- forward lit
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   vert
            #pragma fragment frag

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 uv1        : TEXCOORD1;      // x = young-leaf tint
                float4 color      : COLOR;          // wind data
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3  normalWS   : TEXCOORD2;
                half4  tangentWS  : TEXCOORD3;      // w = bitangent sign
                half   young      : TEXCOORD4;
                half   fogFactor  : TEXCOORD5;
                half3  vertexLight: TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                float3 originWS   = TransformObjectToWorld(float3(0, 0, 0));

                positionWS = BananaApplyWind(positionWS, normalWS, originWS, IN.color,
                                             _WindOn, _BendMul, _SwayMul, _FlutterMul);

                half sign = IN.tangentOS.w * GetOddNegativeScale();
                OUT.positionWS = positionWS;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS   = half3(normalWS);
                OUT.tangentWS  = half4(TransformObjectToWorldDir(IN.tangentOS.xyz), sign);
                OUT.young      = IN.uv1.x;
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    OUT.vertexLight = VertexLighting(positionWS, normalWS);
                #endif
                return OUT;
            }

            half4 frag(Varyings IN, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 albedo = tex.rgb * _BaseColor.rgb;
                albedo = lerp(albedo, albedo * _YoungTint.rgb, saturate(IN.young));
                half alpha = tex.a * _BaseColor.a;
                #if defined(_ALPHATEST_ON)
                    clip(alpha - _Cutoff);
                #endif

                // two-sided: flip the normal on back faces
                half faceSign = IS_FRONT_VFACE(isFrontFace, 1.0h, -1.0h);
                half3 normalWS = normalize(IN.normalWS) * faceSign;
                #if defined(_NORMALMAP)
                    half3 tangentWS   = normalize(IN.tangentWS.xyz);
                    half3 bitangentWS = cross(normalWS, tangentWS) * IN.tangentWS.w * faceSign;
                    half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                    normalWS = normalize(TransformTangentToWorld(nTS, half3x3(tangentWS, bitangentWS, normalWS)));
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.positionCS = IN.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif
                inputData.fogCoord = InitializeInputDataFog(float4(IN.positionWS, 1.0), IN.fogFactor);
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    inputData.vertexLighting = IN.vertexLight;
                #else
                    inputData.vertexLighting = half3(0, 0, 0);
                #endif
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0.0h;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1.0h;
                surfaceData.alpha = 1.0h;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = half3(0, 0, 0);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // cheap leaf translucency: light shining through the blade towards the camera
                if (_Translucency > 0.001h)
                {
                    Light mainLight = GetMainLight(inputData.shadowCoord, IN.positionWS, inputData.shadowMask);
                    half back = saturate(dot(inputData.viewDirectionWS, -mainLight.direction));
                    half3 trans = albedo * mainLight.color * (back * back * back) * _Translucency
                                  * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                    color.rgb += trans;
                }

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1.0h;
                return color;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                float3 originWS   = TransformObjectToWorld(float3(0, 0, 0));
                positionWS = BananaApplyWind(positionWS, normalWS, originWS, IN.color,
                                             _WindOn, _BendMul, _SwayMul, _FlutterMul);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                #if defined(_ALPHATEST_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- depth only
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                float3 originWS   = TransformObjectToWorld(float3(0, 0, 0));
                positionWS = BananaApplyWind(positionWS, normalWS, originWS, IN.color,
                                             _WindOn, _BendMul, _SwayMul, _FlutterMul);
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                #if defined(_ALPHATEST_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a - _Cutoff);
                #endif
                return IN.positionCS.z;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- depth + normals (SSAO etc.)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   vert
            #pragma fragment frag
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                half4  tangentWS  : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                float3 originWS   = TransformObjectToWorld(float3(0, 0, 0));
                positionWS = BananaApplyWind(positionWS, normalWS, originWS, IN.color,
                                             _WindOn, _BendMul, _SwayMul, _FlutterMul);
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = half3(normalWS);
                half sign = IN.tangentOS.w * GetOddNegativeScale();
                OUT.tangentWS = half4(TransformObjectToWorldDir(IN.tangentOS.xyz), sign);
                return OUT;
            }

            half4 frag(Varyings IN, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                #if defined(_ALPHATEST_ON)
                    clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a - _Cutoff);
                #endif
                half faceSign = IS_FRONT_VFACE(isFrontFace, 1.0h, -1.0h);
                half3 normalWS = normalize(IN.normalWS) * faceSign;
                #if defined(_NORMALMAP)
                    half3 tangentWS   = normalize(IN.tangentWS.xyz);
                    half3 bitangentWS = cross(normalWS, tangentWS) * IN.tangentWS.w * faceSign;
                    half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                    normalWS = normalize(TransformTangentToWorld(nTS, half3x3(tangentWS, bitangentWS, normalWS)));
                #endif

                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
                    return half4(packedNormalWS, 0.0);
                #else
                    return half4(NormalizeNormalPerPixel(normalWS), 0.0);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
