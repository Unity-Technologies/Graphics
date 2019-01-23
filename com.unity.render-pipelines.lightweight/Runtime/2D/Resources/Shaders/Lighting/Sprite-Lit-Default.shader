Shader "Lightweight Render Pipeline/2D/Sprite-Lit-Default"
{
	Properties
	{
		_MainTex ("Diffuse", 2D) = "white" {}
		_MaskTex("Mask", 2D) = "black" {}
		_NormalMap("Normal Map", 2D) = "bump" {}
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

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
			#pragma prefer_hlslcc gles

			#pragma vertex CombinedShapeLightVertex
			#pragma fragment CombinedShapeLightFragment
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __

			#pragma multi_compile USE_POINT_LIGHTS __
			#pragma multi_compile USE_POINT_LIGHT_COOKIES __ 

			#include "Include/CombinedShapeLightPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Tags { "LightMode" = "NormalsRendering"}
			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma vertex NormalsRenderingVertex
			#pragma fragment NormalsRenderingFragment

			#include "Include/NormalsRenderingPass.hlsl"
			ENDHLSL
		}
	}
}

