Shader "Hidden/Light2d-Sprite-Volumetric"
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
		float4 volumeColor : TANGENT;
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

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			Varyings vert(Attributes attributes)
            {
				Varyings o;
				o.positionCS = TransformObjectToHClip(attributes.positionOS);
				o.color = attributes.color * attributes.volumeColor;
				o.uv = attributes.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
				half4 color = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return color;
            }
            ENDHLSL
        }
    }
}
