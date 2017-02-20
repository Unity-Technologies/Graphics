// Shader targeted for LowEnd mobile devices. Single Pass Forward Rendering. Shader Model 2
//
// The parameters and inspector of the shader are the same as Standard shader,
// for easier experimentation.
Shader "LDRenderPipeline/LowEndSpecular"
{
	// Properties is just a copy of Standard (Specular Setup).shader. Our example shader does not use all of them,
	// but the inspector UI expects all these to exist.
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_GlossMapScale("Smoothness Factor", Range(0.0, 1.0)) = 1.0
		[Enum(Specular Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

		_SpecColor("Specular", Color) = (0.2,0.2,0.2)
		_SpecGlossMap("Specular", 2D) = "white" {}
		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

		_BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}

		_Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
		_ParallaxMap("Height Map", 2D) = "black" {}

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}

		_DetailMask("Detail Mask", 2D) = "white" {}

		_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}

		[Enum(UV0,0,UV1,1)] _UVSec("UV Set for secondary textures", Float) = 0

		// Blending state
		[HideInInspector] _Mode("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "PerformanceChecks" = "False" "RenderPipeline" = "LDRenderPipeline"}
		LOD 300

		Pass
		{
			Name "SINGLE_PASS_FORWARD"
			Tags { "LightMode" = "LowEndForwardBase" }

			// Use same blending / depth states as Standard shader
			Blend[_SrcBlend][_DstBlend]
			ZWrite[_ZWrite]

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature _NORMALMAP
			
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ SHADOWS_DEPTH
			#pragma multi_compile _ SHADOWS_FILTERING_PCF
			#pragma multi_compile_fog
			#pragma only_renderers d3d9 d3d11 d3d11_9x glcore gles gles3
			#pragma enable_d3d11_debug_symbols
			
			#define DIFFUSE_AND_SPECULAR_INPUT SpecularInput

			#include "LDRenderPipeline-Core.cginc"
			ENDCG
		}

		Pass
		{
			Name "SHADOW_CASTER"
			Tags { "Lightmode" = "ShadowCaster" }

			ZWrite On ZTest LEqual Cull Front

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ SHADOWS_FILTERING_VSM
			float4 _ShadowBias;

			#include "UnityCG.cginc"
			
			inline void ApplyLinearBias(half4 clipPos)
			{
#if defined(UNITY_REVERSED_Z)
				clipPos.z -= _ShadowBias.x;
#else
				clipPos.z += _ShadowBias.x;
#endif
			}

			float4 vert(float4 position : POSITION) : SV_POSITION
			{
				float4 clipPos = UnityObjectToClipPos(position);
				ApplyLinearBias(clipPos);
				return clipPos;
			}
				
			half4 frag() : SV_TARGET
			{
				return 0;
			}
			ENDCG
		}
	}
	Fallback "Standard (Specular setup)"
	CustomEditor "StandardShaderGUI"
}
