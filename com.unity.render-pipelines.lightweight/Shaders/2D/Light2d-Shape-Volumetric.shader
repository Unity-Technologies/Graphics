Shader "Hidden/Light2D-Shape-Volumetric"
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

		#ifdef SPRITE_LIGHT
			half2  uv		  : TEXCOORD0;
		#endif
	};

	struct Varyings
	{
		float4  positionCS	: SV_POSITION;
		float4  color		: COLOR;
		float2  uv			: TEXCOORD0;
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
			#pragma multi_compile SPRITE_LIGHT __

			#ifdef SPRITE_LIGHT
				TEXTURE2D(_CookieTex);			// This can either be a sprite texture uv or a falloff texture
				SAMPLER(sampler_CookieTex);
			#else
				uniform float  _FalloffCurve;
				TEXTURE2D(_FalloffLookup);
				SAMPLER(sampler_FalloffLookup);
			#endif


			Varyings vert (Attributes attributes)
            {
				Varyings o;
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
				o.color = attributes.color;
				o.color.a = attributes.volumeColor.a;

				#ifdef SPRITE_LIGHT
					o.uv = attributes.uv;
				#else
					o.uv = float2(attributes.color.a, _FalloffCurve);
				#endif

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
				half4 color = i.color;
				#if SPRITE_LIGHT
					color = color * SAMPLE_TEXTURE2D(_CookieTex, sampler_CookieTex, i.uv);
				#else
					color.a = i.color.a * SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
				#endif
                return color;
            }
            ENDHLSL
        }
    }
}
