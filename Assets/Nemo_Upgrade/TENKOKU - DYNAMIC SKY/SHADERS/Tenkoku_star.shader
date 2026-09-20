Shader "TENKOKU/star_shader"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _MainTex ("Particle Texture", 2D) = "white" {}
        _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-398" "RenderType"="Transparent" }
        Blend One One
        Cull Off
        ZWrite Off
        ColorMask RGB
        Pass
        {
            Name "Tenkoku Stars URP"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _TintColor;
                float _InvFade;
            CBUFFER_END
            float _tenkokuIsLinear;
            float _Tenkoku_Ambient;
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 star = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 color = saturate(input.color * _TintColor * star * 3.0h);
                color.rgb *= color.a * lerp(2.2h, 1.0h, saturate(_tenkokuIsLinear));
                color.rgb *= 1.0h - saturate(_Tenkoku_Ambient * 4.0h);
                return half4(color.rgb, 1.0h);
            }
            ENDHLSL
        }
    }
}
