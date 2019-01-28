Shader "Lightweight Render Pipeline/2D/Sprite-Lit-Default"
{
	Properties
	{
		_MainTex ("Diffuse", 2D) = "white" {}
		_MaskTex("Mask", 2D) = "white" {}
		_NormalMap("Normal Map", 2D) = "bump" {}
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
	ENDHLSL

	SubShader
	{
		Tags { "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off

		Pass
		{
			Tags { "LightMode" = "CombinedShapeLight" }
			HLSLPROGRAM
			struct Attributes
			{
				float4 positionOS   : POSITION;
				float4 color		: COLOR;
				half2  uv			: TEXCOORD0;
			};

			struct Varyings
			{
				float4  positionCS		: SV_POSITION;
				float4  color			: COLOR;
				half2	uv				: TEXCOORD0;
				half2	lightingUV		: TEXCOORD1;
				float4  vertexWorldPos	: TEXCOORD3;
				half2	pixelScreenPos	: TEXCOORD4;
			};

			#pragma prefer_hlslcc gles

			#pragma vertex CombinedShapeLightVertex
			#pragma fragment CombinedShapeLightFragment
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __

			#include "Include/CombinedShapeLightPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Tags { "LightMode" = "NormalsRendering"}
			HLSLPROGRAM
			struct Attributes
			{
				float4 positionOS   : POSITION;
				float4 color		: COLOR;
				half2  uv			: TEXCOORD0;
			};

			struct Varyings
			{
				float4  positionCS		: SV_POSITION;
				float4  color			: COLOR;
				half2	uv				: TEXCOORD0;
				float3  normal			: TEXCOORD1;
				float3  tangent			: TEXCOORD2;
				float3  bitangent		: TEXCOORD3;
			};

			#pragma prefer_hlslcc gles
			#pragma vertex NormalsRenderingVertex
			#pragma fragment NormalsRenderingFragment

			#include "Include/NormalsRenderingPass.hlsl"
			ENDHLSL
		}
	}
}

