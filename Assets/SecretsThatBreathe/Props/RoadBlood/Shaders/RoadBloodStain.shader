Shader "Secrets That Breathe/Road Blood Stain"
{
    Properties
    {
        _BloodColor("Thin Blood Tint", Color) = (0.42,0.016,0.025,1)
        _DenseColor("Pooled Blood Tint", Color) = (0.28,0.005,0.013,1)
        _Opacity("Stain Strength", Range(0,1)) = 0.94
        _Patchiness("Surface Variation", Range(0,1)) = 0.25
        _NoiseScale("Surface Detail Scale", Float) = 48
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-20" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "RoadStain"
            Tags { "LightMode"="SRPDefaultUnlit" }
            // Premultiplied alpha keeps the dark-red hue on dim asphalt instead
            // of multiplying the stain down to black. Thin edges show the road.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BloodColor;
                half4 _DenseColor;
                half _Opacity;
                half _Patchiness;
                float _NoiseScale;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 surface : TEXCOORD0;
                half4 color : COLOR;
                half fog : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 cell=floor(p), f=frac(p);
                f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),
                    lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            Varyings Vert(Attributes v)
            {
                Varyings o=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS=p.positionCS;
                o.surface=v.positionOS.xz;
                o.color=v.color;
                o.fog=ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half detail=Noise(i.surface*_NoiseScale)*0.65+Noise(i.surface*_NoiseScale*2.17)*0.35;
                half coverage=saturate(i.color.a*_Opacity)*lerp(1,0.55+detail*0.45,_Patchiness);
                // A stain must disappear into fog rather than darkening the fog itself.
                coverage=MixFogColor(half3(coverage,coverage,coverage),half3(0,0,0),i.fog).r;
                half density=saturate(i.color.r*0.75+detail*0.25);
                half3 tint=lerp(_BloodColor.rgb,_DenseColor.rgb,density);
                return half4(tint*coverage,coverage);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
