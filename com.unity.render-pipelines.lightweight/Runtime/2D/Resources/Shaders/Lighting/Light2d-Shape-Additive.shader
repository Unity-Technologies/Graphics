Shader "Hidden/Light2D-Shape-Additive"
{
	Properties
	{
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

	struct Attributes
	{
		float4 positionOS   : POSITION;
		float4 color		: COLOR;
	};

	struct Varyings
	{
		float4  positionCS	: SV_POSITION;
		float4  color		: COLOR;
		float2  lookupUV	: TEXCOORD0;
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

			uniform float  _InverseLightIntensityScale;

			TEXTURE2D(_FalloffLookup);
			SAMPLER(sampler_FalloffLookup);
			uniform float _FalloffCurve;
			
			Varyings vert (Attributes attributes)
			{
				Varyings o;
				o.positionCS = TransformObjectToHClip(attributes.positionOS);
				o.color = attributes.color * _InverseLightIntensityScale;
				o.lookupUV = float2(o.color.a, _FalloffCurve);
				return o;
			}
			
			half4 frag (Varyings i) : SV_Target
			{
				half4 col = i.color;
				float adjAttenuation = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.lookupUV).r;
				col = col * adjAttenuation;
				col.a = 1;
				return col;
			}
			ENDHLSL
		}
	}
}
