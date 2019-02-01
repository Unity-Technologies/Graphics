Shader "Hidden/Light2D-Sprite-Additive"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

	struct Attributes
	{
		float4 positionOS : POSITION;
		half4  color	  : COLOR;
		half2  uv		  : TEXCOORD0;
	};

	struct Varyings
	{
		float4 positionCS : SV_POSITION;
		half4  color	  : COLOR;
		half2  uv		  : TEXCOORD0;
	};
	ENDHLSL

	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		Blend One One
		BlendOp Add
		ZWrite Off
		Cull Off


		Pass
		{
			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma vertex vert
			#pragma fragment frag

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			
			uniform float	_InverseLightIntensityScale;
			
			Varyings vert (Attributes attributes)
			{
				Varyings o;
				o.positionCS.xyz = TransformObjectToHClip(attributes.positionOS.xyz);
				o.positionCS.w = 1;
				o.color = attributes.color;
				o.uv = attributes.uv;
				return o;
			}
			
			half4 frag (Varyings i) : SV_Target
			{
				half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
				col = i.color * i.color.a * col * col.a * _InverseLightIntensityScale;
				col.a = 1;
				return col;
			}
			ENDHLSL
		}
	}
}
