Shader "Lightweight Render Pipeline/2D/Sprite-Lit-Ambient"
{
	Properties
	{
		_MainTex ("Diffuse", 2D) = "white" {}
		_MaskTex("Mask", 2D) = "black" {}
		_NormalMap("Normal Map", 2D) = "black" {}
		_SpecularMultiplier("Specular Multiplier", Float) = 1
		_RimMultiplier("Rim Multiplier", Float) = 1
		_AmbientMultiplier("Ambient Multiplier", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off

		Pass
		{
			Tags { "LightMode" = "CombinedShapeLight" }
			CGPROGRAM
			#pragma vertex CombinedShapeLightVertex
			#pragma fragment CombinedShapeLightFragment
			#pragma multi_compile USE_SPECULAR_TEXTURE __
			#pragma multi_compile USE_AMBIENT_TEXTURE __
			#pragma multi_compile USE_RIM_TEXTURE  __

			#pragma multi_compile USE_POINT_LIGHTS __
			#pragma multi_compile USE_POINT_LIGHT_COOKIES __ 

			#undef USE_SPECULAR_TEXTURE
			#undef USE_RIM_TEXTURE
			#define USE_SPECULAR_TEXTURE 0
			#define USE_RIM_TEXTURE 0

			#include "Include/CombinedShapeLightPass.cginc"
			ENDCG
		}
	}
}
