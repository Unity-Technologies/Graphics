Shader "Hidden/Light2D-Shape-Superimpose"
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
	};
	ENDHLSL

	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		BlendOp Add
		ZWrite Off
		Cull Off

		Pass
		{
			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma vertex vert
			#pragma fragment frag

			half4 _Color;
			uniform float _InverseLightIntensityScale;
			
			Varyings vert (Attributes attributes)
			{
				Varyings o;
				o.positionCS.xyz = TransformObjectToHClip(attributes.positionOS.xyz);
				o.positionCS.w = 1;
				o.color = attributes.color;
				return o;
			}
			
			half4 frag (Varyings i) : SV_Target
			{
				half4 col = i.color * _InverseLightIntensityScale;
				col = col * i.color.a;
				return col;
			}
			ENDHLSL
		}
	}
}
