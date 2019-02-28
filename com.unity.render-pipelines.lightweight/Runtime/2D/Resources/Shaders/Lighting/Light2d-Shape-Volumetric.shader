Shader "Hidden/Light2d-Shape-Volumetric"
{
    Properties
    {
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

	struct Attributes
	{
		float4 positionOS   : POSITION;
		float4 color : COLOR;
		float4 volumeColor : TANGENT;
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
		Tags { "RenderType" = "Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha One
			BlendOp Add
			ZWrite Off
			ZTest Off 
			Cull Off  // Shape lights have their interiors with the wrong winding order


			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma vertex vert
			#pragma fragment frag

			TEXTURE2D(_FalloffLookup);
			SAMPLER(sampler_FalloffLookup);
			uniform float _FalloffCurve;


			Varyings vert (Attributes attributes)
            {
				Varyings o;
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
				o.color = attributes.color * attributes.volumeColor;
				o.lookupUV = float2(o.color.a, _FalloffCurve);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
				half4 color = i.color;
				color.a = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.lookupUV).r;
                return color;
            }
            ENDHLSL
        }
    }
}
