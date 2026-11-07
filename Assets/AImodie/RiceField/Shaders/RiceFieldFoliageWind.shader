// Rice Field pack - URP foliage shader with WindZone support (Unity 6000.3 / URP 17.3).
// Based on URP Lit: same lighting, shadows, Forward+, deferred, LOD cross-fade and GPU instancing,
// with a vertex wind bend driven by the scene's WindZone (see Scripts/RiceFieldWind.cs).
// Alpha clipping, double-sided rendering, normal map and the packed mask are always on.
Shader "RiceField/Foliage Wind URP"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo (A = cutout)", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [Normal][NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Float) = 0.6

        [NoScaleOffset] _MetallicGlossMap("Mask (R Metal, G AO, A Smooth)", 2D) = "black" {}
        [NoScaleOffset] _OcclusionMap("Occlusion (G)", 2D) = "white" {}
        _Smoothness("Smoothness Scale", Range(0.0, 1.0)) = 0.75
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        [Header(Wind driven by the scene Wind Zone)]
        _RFWind("Bend, Flutter, Plant Height, Sway Speed", Vector) = (0.12, 0.012, 1.0, 1.6)
        _RFWindWave("Gust Wave Scale", Range(0.0, 2.0)) = 0.25

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0

        // kept for SRP-Batcher layout compatibility with URP Lit (not used)
        [HideInInspector] _WorkflowMode("WorkflowMode", Float) = 1.0
        [HideInInspector] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        [HideInInspector] _SpecGlossMap("Specular", 2D) = "white" {}
        [HideInInspector] _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        [HideInInspector] _ParallaxMap("Height Map", 2D) = "black" {}
        [HideInInspector] _EmissionColor("Color", Color) = (0,0,0)
        [HideInInspector] _EmissionMap("Emission", 2D) = "white" {}
        [HideInInspector] _DetailMask("Detail Mask", 2D) = "white" {}
        [HideInInspector] _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [HideInInspector] _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        [HideInInspector] _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [HideInInspector] _DetailNormalMap("Normal Map", 2D) = "bump" {}
        [HideInInspector] _ClearCoatMask("_ClearCoatMask", Float) = 0.0
        [HideInInspector] _ClearCoatSmoothness("_ClearCoatSmoothness", Float) = 0.0
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 0.0
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        HLSLINCLUDE
            // always-on material features for the foliage cards
            #define _ALPHATEST_ON 1
            #define _NORMALMAP 1
            #define _METALLICSPECGLOSSMAP 1
            #define _OCCLUSIONMAP 1
        ENDHLSL

        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex RF_LitPassVertex
            #pragma fragment LitPassFragment

            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "RiceFieldLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #include "RiceFieldWind.hlsl"

            Varyings RF_LitPassVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                input.positionOS.xyz = RF_ApplyWind(input.positionOS.xyz);
                return LitPassVertex(input);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex RF_ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "RiceFieldLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            #include "RiceFieldWind.hlsl"

            Varyings RF_ShadowPassVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                input.positionOS.xyz = RF_ApplyWind(input.positionOS.xyz);
                return ShadowPassVertex(input);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "GBuffer"
            Tags { "LightMode" = "UniversalGBuffer" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles3 glcore
            #pragma vertex RF_LitGBufferPassVertex
            #pragma fragment LitGBufferPassFragment

            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "RiceFieldLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitGBufferPass.hlsl"
            #include "RiceFieldWind.hlsl"

            Varyings RF_LitGBufferPassVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                input.positionOS.xyz = RF_ApplyWind(input.positionOS.xyz);
                return LitGBufferPassVertex(input);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex RF_DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "RiceFieldLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            #include "RiceFieldWind.hlsl"

            Varyings RF_DepthOnlyVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                input.position.xyz = RF_ApplyWind(input.position.xyz);
                return DepthOnlyVertex(input);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex RF_DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "RiceFieldLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            #include "RiceFieldWind.hlsl"

            Varyings RF_DepthNormalsVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                input.positionOS.xyz = RF_ApplyWind(input.positionOS.xyz);
                return DepthNormalsVertex(input);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Lightmap baking only
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "RiceFieldLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
