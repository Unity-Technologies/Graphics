Shader "CustomSRP/SRP0802/RenderPass UnlitTransparent"
{
	Properties
	{
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Cull Back Lighting Off ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Tags { "LightMode" = "SRP0802_Pass1" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "../_General/ShaderLibrary/Input/Transformation.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			struct RTstruct
			{
				float4 Albedo : SV_Target0;
				float4 Emission : SV_Target1;
			};

			CBUFFER_START(UnityPerMaterial)
			sampler2D _MainTex;
			float4 _MainTex_ST;
			CBUFFER_END
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			RTstruct frag (v2f i)
			{
				RTstruct o;

				o.Albedo = tex2D(_MainTex, i.uv);
				o.Emission = frac(float4(_Time.x, _Time.y, _Time.z, _Time.w));
				o.Emission.xy *= i.uv;
				o.Emission.zw *= i.uv;

				o.Emission = 1-o.Emission; //just want a different color for transparent objects..

				return o;
			}
			ENDHLSL
		}

		Pass
		{
			Tags { "LightMode" = "SRP0802_Pass2" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "../_General/ShaderLibrary/Input/Transformation.hlsl"
			#include "SRP0802_HLSLSupport.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				return o;
			}
			
			UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(0);
			UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(1);

			float4 frag (v2f i) : SV_Target
			{
				float4 albedo = UNITY_READ_FRAMEBUFFER_INPUT(0, i.vertex.xyz);
				float4 emission = UNITY_READ_FRAMEBUFFER_INPUT(1, i.vertex.xyz);

				return albedo + emission;
			}
			ENDHLSL
		}
	}
}
