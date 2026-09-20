Shader "TENKOKU/moonsphere_shader"
{
    Properties
    {
        _PrimaryTint ("Primary Tint", Color) = (1,1,1,1)
        _Color ("Main Color", Color) = (1,1,1,1)
        _AmbientTint ("Ambient Tint", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BRDFTex ("BRDF", 2D) = "white" {}
        _overBright ("OverBright", Float) = 1.0
        _dispStrength ("Displace Amount", Range(0.0,3.0)) = 1.0
        _GlowColor ("Glow Color", Color) = (0.5,0.5,0.5,0.5)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-395" "RenderType"="Transparent" }
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Name "Tenkoku Moon URP"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _PrimaryTint;
                half4 _Color;
                half4 _AmbientTint;
                half4 _GlowColor;
                float _overBright;
                float _dispStrength;
            CBUFFER_END
            float4 Tenkoku_Vec_SunFwd;
            float4 Tenkoku_MoonLightColor;
            float4 Tenkoku_MoonHorizColor;
            float4 _Tenkoku_overcastColor;
            float _tenkokuIsLinear;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 sunDirection = normalize(Tenkoku_Vec_SunFwd.xyz + half3(0.0001h,0.0001h,0.0001h));
                half phase = saturate(dot(normalize(input.normalWS), sunDirection) * 2.0h);
                half3 horizonTint = lerp(half3(1,1,1), Tenkoku_MoonHorizColor.rgb * 2.0h, saturate(Tenkoku_MoonHorizColor.a));
                half3 lightColor = max(half3(0.5h,0.5h,0.5h), Tenkoku_MoonLightColor.rgb);
                half3 color = tex.rgb * _PrimaryTint.rgb * _Color.rgb * horizonTint * lightColor;
                color *= lerp(0.4646h, 1.0h, saturate(_tenkokuIsLinear));
                color = lerp(color * 0.3h, color * 2.25h, phase);
                half alpha = tex.a * saturate(1.0h - _Tenkoku_overcastColor.a * 3.0h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
