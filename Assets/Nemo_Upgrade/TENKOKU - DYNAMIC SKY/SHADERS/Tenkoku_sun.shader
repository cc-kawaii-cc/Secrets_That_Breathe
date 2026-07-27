Shader "TENKOKU/sun_shader"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _CoronaColor ("Corona Color", Color) = (1,0.5,0,1)
        _MainTex ("BRDF", 2D) = "white" {}
        _overBright ("OverBright", Float) = 1.0
        _dispStrength ("Displace Amount", Range(0.0,10.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Blend One One
        Cull Off
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
                float3 viewDirWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float4 _CoronaColor;
                float _overBright;
                float _dispStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 displaced = input.positionOS.xyz + input.normalOS * (_dispStrength * 0.02);
                VertexPositionInputs posInputs = GetVertexPositionInputs(displaced);
                output.positionHCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half2 centered = input.uv * 2.0 - 1.0;
                half radial = saturate(1.0 - dot(centered, centered));
                half corona = pow(radial, 0.5);
                half core = pow(radial, 4.0);
                half3 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                half fresnel = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));

                half3 color = _CoronaColor.rgb * corona;
                color += (_TintColor.rgb + tex) * core * _overBright;
                color += _TintColor.rgb * pow(saturate(1.0 - fresnel), 2.0) * 0.25;
                return half4(max(color, 0), saturate(core + corona));
            }
            ENDHLSL
        }
    }
}
