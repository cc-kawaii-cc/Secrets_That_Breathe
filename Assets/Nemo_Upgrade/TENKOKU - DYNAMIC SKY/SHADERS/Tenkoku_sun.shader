Shader "TENKOKU/sun_shader" {

Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_CoronaColor ("Corona Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("BRDF", 2D) = "white" {}
	_overBright ("OverBright", float) = 1.0
	_dispStrength ("Displace Amount", Range(0.0,10.0)) = 1.0
	}

	// URP compatibility pass. The original surface-shader SubShader below is
	// retained for projects that still use the Built-in Render Pipeline.
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }

		Pass {
			Name "TenkokuSunURP"
			Tags { "LightMode"="SRPDefaultUnlit" }
			Blend One One
			Cull Front
			ZWrite Off

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex TenkokuSunVert
			#pragma fragment TenkokuSunFrag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			float4 _MainTex_ST;
			half4 _TintColor;
			half4 _CoronaColor;
			half4 _TenkokuSunColor;
			half4 _Tenkoku_overcastColor;
			half _overBright;
			half _Tenkoku_Ambient;
			half _Tenkoku_EclipseFactor;

			struct TenkokuSunAttributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct TenkokuSunVaryings {
				float4 positionCS : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float3 viewDirWS : TEXCOORD1;
				float2 uv : TEXCOORD2;
			};

			TenkokuSunVaryings TenkokuSunVert(TenkokuSunAttributes input) {
				TenkokuSunVaryings output;
				VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = positionInputs.positionCS;
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return output;
			}

			half4 TenkokuSunFrag(TenkokuSunVaryings input) : SV_Target {
				half3 normalWS = normalize(input.normalWS);
				half3 viewDirWS = normalize(input.viewDirWS);
				half facing = saturate(dot(normalWS, viewDirWS));
				half rim = pow(saturate(1.0h - facing), 2.0h);
				half textureShape = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).b;
				half disc = saturate(textureShape + facing);
				half visibility = saturate(1.0h - (_Tenkoku_overcastColor.a * 3.0h));
				visibility *= saturate(max(_Tenkoku_Ambient * 4.0h, 0.05h));
				visibility *= saturate(_Tenkoku_EclipseFactor);
				half3 sunColor = lerp(_CoronaColor.rgb, _TintColor.rgb * max(_TenkokuSunColor.rgb, half3(1, 1, 1)), disc);
				half strength = (disc + rim * 0.35h) * visibility * max(_overBright, 1.0h);
				return half4(sunColor * strength, strength);
			}
			ENDHLSL
		}
	}

	
	SubShader {
		

		
Tags { "Queue"="Background+1604"}
		Blend One One
		Cull Front
		ZWrite Off
		Offset 1,995000


		CGPROGRAM
		#pragma surface surf Ramp vertex:vert alpha nofog
		#pragma target 3.0
		#pragma glsl
		
		

		sampler2D _MainTex;
		float4 _TintColor;
		float4 _CoronaColor;
		float _dispStrength;
		float _overBright;
		float4 _Tenkoku_overcastColor;
		float4 _TenkokuSunColor;
		float _Tenkoku_AmbientGI;
		float _Tenkoku_Ambient;
		float _Tenkoku_EclipseFactor;

		struct Input {
			float2 uv_MainTex;
			float4 color;
			float4 screenPos;
			float3 viewDir;
			float3 pos;
		};


		half4 LightingRamp (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten){
			
			//lighting dot products
			s.Normal = normalize(s.Normal);
			float NdotL = dot(s.Normal, lightDir);
			float NdotE = dot(s.Normal, viewDir);
			
			//do diffuse wrap
			float diff = (NdotL * 0.5) + 0.5;
			float2 brdfUV = float2(NdotE * 1.0, diff);
			float3 BRDF = tex2D(_MainTex, brdfUV.xy).rgb;

			float4 c;
			c.rgb = s.Albedo;

			c.a = saturate((BRDF.b) * 1.0 * s.Alpha);
			



			c = saturate(c);
			c.a *= _overBright;
			c.a = lerp(1.0,c.a*dot(-viewDir,s.Normal),_CoronaColor.a);

			//lerp(fixed3(1.0,0.75,0.5),c.rgb,saturate(c.a));

			c.rgb *= c.a;

			c.a = s.Alpha;

//c.rgb = half3(1,1,1);//saturate(c.rgb);

c.rgb = _TintColor;//lerp(_TintColor.rgb,_CoronaColor.rgb, 0.0 );

half sSize = saturate(c.a - (saturate(_Tenkoku_overcastColor.a*3)));
sSize = sSize * saturate(lerp(-1.0,1.0,_Tenkoku_Ambient));

c.a = 0;

c.a += saturate(lerp(-0.5,1,dot(viewDir,-s.Normal))) * 0.05 * sSize * _Tenkoku_EclipseFactor;
c.a += saturate(lerp(-1,1,dot(viewDir,-s.Normal))) * 0.05 * sSize * _Tenkoku_EclipseFactor;
c.a += saturate(lerp(-2,1,dot(viewDir,-s.Normal))) * 0.1 * sSize * _Tenkoku_EclipseFactor;
c.a += saturate(lerp(-3,1,dot(viewDir,-s.Normal))) * 0.1 * sSize * _Tenkoku_EclipseFactor;
c.a += saturate(lerp(-6,1,dot(viewDir,-s.Normal))) * 0.1 * sSize * _Tenkoku_EclipseFactor;
c.a += saturate(lerp(-2,1,dot(viewDir,-s.Normal))) * sSize;



c.rgb = lerp(_CoronaColor.rgb, c.rgb * _TenkokuSunColor.rgb, c.a);
c.rgb = lerp(c.rgb,c.rgb * _TintColor.rgb,_TintColor.a);
c.rgb = (c.rgb + (_overBright * (lerp(0,1,dot(viewDir,-s.Normal))) * saturate(lerp(0.0,4.0,_Tenkoku_AmbientGI)) ));


c.a = saturate(c.a - (saturate(_Tenkoku_overcastColor.a*3)));
c.a = c.a * saturate(lerp(0.0,4.0,_Tenkoku_Ambient));


			return c;
			
		}
		
		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
			float disp = 1.0;
			//v.vertex.xyz += (v.normal * (disp * (_dispStrength * 0.5)));
			v.vertex.xyz += (v.normal * (disp * (0.75)));
			o.color = v.color;
		}
		
		void surf (Input IN, inout SurfaceOutput o) {

			o.Albedo = _TenkokuSunColor.rgb;

			o.Alpha = 1.0;//saturate(lerp(1,-3,_Tenkoku_overcastColor.a));

			
			o.Gloss = 0.0;
			o.Specular = 0.0;
			o.Emission = o.Albedo*4;

			//Overbright
			o.Albedo = o.Albedo * 2;
		}
		ENDCG






	} 
}
