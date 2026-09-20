Shader "TENKOKU/sun_shader"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _CoronaColor ("Corona Color", Color) = (1,0.3,0,1)
        _MainTex ("BRDF", 2D) = "white" {}
        _overBright ("OverBright", Float) = 1.0
        _dispStrength ("Displace Amount", Range(0.0,10.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-396" "RenderType"="Transparent" }
        Blend One One
        Cull Front
        ZWrite Off
        Pass
        {
            Name "Tenkoku Sun URP"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
                half4 _CoronaColor;
                float4 _MainTex_ST;
                float _overBright;
                float _dispStrength;
            CBUFFER_END
            float4 _TenkokuSunColor;
            float4 _Tenkoku_overcastColor;
            float _Tenkoku_Ambient;
            float _Tenkoku_EclipseFactor;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 expanded = input.positionOS.xyz + input.normalOS * 0.75;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(expanded);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half facing = saturate(abs(dot(viewDirection, normalize(input.normalWS))));
                half disk = smoothstep(0.0h, 0.65h, facing);
                half corona = pow(saturate(facing), 3.0h);
                half visibility = saturate(1.0h - _Tenkoku_overcastColor.a * 3.0h);
                visibility *= saturate(_Tenkoku_Ambient * 4.0h) * saturate(_Tenkoku_EclipseFactor);
                half3 sunColor = max(_TenkokuSunColor.rgb, half3(0.9h,0.45h,0.08h));
                half3 color = lerp(_CoronaColor.rgb, sunColor, disk);
                color *= (disk * 2.0h + corona * 0.5h) * max(1.0h, (half)_overBright) * visibility;
                return half4(color, visibility);
            }
            ENDHLSL
        }
    }
}
