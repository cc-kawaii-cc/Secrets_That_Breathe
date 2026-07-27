Shader "TENKOKU/moonsphere_shader"
{
    Properties
    {
        _PrimaryTint("Primary Tint", Color) = (1,1,1,1)
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
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _PrimaryTint;
                float4 _Color;
                float4 _AmbientTint;
                float4 _GlowColor;
                float _overBright;
                float _dispStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 displaced = input.positionOS.xyz + input.normalOS * (_dispStrength * 0.005);
                VertexPositionInputs posInputs = GetVertexPositionInputs(displaced);
                output.positionHCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half ndv = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                half rim = pow(saturate(1.0 - ndv), 2.5);

                half3 baseColor = tex.rgb * _PrimaryTint.rgb * _Color.rgb;
                baseColor *= lerp(0.65, 1.25, _AmbientTint.r);
                baseColor += _GlowColor.rgb * rim * _GlowColor.a;
                baseColor *= max(_overBright, 0.1);

                half alpha = saturate(tex.a + _Color.a + rim * 0.2);
                return half4(baseColor, alpha);
            }
            ENDHLSL
        }
    }
}
