// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "TENKOKU/fx_Particle" {
Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Particle Texture", 2D) = "white" {}
}

SubShader {
	Tags {
		"RenderPipeline"="UniversalPipeline"
		"Queue"="Transparent+100"
		"RenderType"="Transparent"
	}

	Pass {
		Name "TenkokuParticleURP"
		Tags { "LightMode"="UniversalForward" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Back
		ZWrite Off

		HLSLPROGRAM
		#pragma target 3.0
		#pragma vertex TenkokuParticleVert
		#pragma fragment TenkokuParticleFrag

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		TEXTURE2D(_MainTex);
		SAMPLER(sampler_MainTex);
		float4 _MainTex_ST;
		half4 _TintColor;

		struct TenkokuParticleAttributes {
			float4 positionOS : POSITION;
			half4 color : COLOR;
			float2 uv : TEXCOORD0;
		};

		struct TenkokuParticleVaryings {
			float4 positionCS : SV_POSITION;
			half4 color : COLOR;
			float2 uv : TEXCOORD0;
		};

		TenkokuParticleVaryings TenkokuParticleVert(TenkokuParticleAttributes input) {
			TenkokuParticleVaryings output;
			output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
			output.color = input.color;
			output.uv = TRANSFORM_TEX(input.uv, _MainTex);
			return output;
		}

		half4 TenkokuParticleFrag(TenkokuParticleVaryings input) : SV_Target {
			half4 color = 2.0h * input.color * _TintColor *
				SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
			color.rgb = lerp(_TintColor.rgb, color.rgb, color.a) * 0.5h;
			color.rgb = clamp(color.rgb, 0.15h, 1.0h);
			clip(color.a - 0.1h);
			return color;
		}
		ENDHLSL
	}
}

Category {
	Tags {"Queue"="Overlay+11"}
	Blend SrcAlpha OneMinusSrcAlpha
	//Blend One One
	Cull Back
	Lighting Off
	ZWrite On
	//ZTest Always

	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _TintColor;
			
			struct appdata_t {
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				return o;
			}

			half4 frag (v2f i) : COLOR {
				fixed4 col =  2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
				col.rgb = lerp(_TintColor.rgb,col.rgb,col.a)*0.5;

				//col.rgb = lerp(col.rgb,fixed3(1,1,1),0.25);

				col.rgb = clamp(col.rgb,0.15,1.0);
				//col.rgb = _TintColor.rgb;
				clip (col.a-0.1);
				return col;
			}
			
			ENDCG 
		}
	} 		
}
}
