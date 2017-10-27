Shader "HDRenderPipeline/ExperimentalSkin"
{
	Properties
	{
		[Enum(Skin, 0, Hair, 1, Eye, 2)] _CharacterMaterialID("CharacterMaterialID", Int) = 0

		_DiffuseColor("DiffuseColor", Color) = (1, 1, 1, 1)
		_DiffuseColorMap("DiffuseColorMap", 2D) = "white" {}
		
		_NormalMap("NormalMap", 2D) = "bump" {}
		
		[HideInInspector] _BlendMode("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _ZTestMode("_ZTestMode", Int) = 8

		//Skin
		
		//Hair
		_HairSpecularShift("SpecularShift", Range(0, 1)) = 0.5
		_HairSpecularHighlight("SpecularHighlight", Color) = (1, 1, 1, 1)
		_HairShadowStrength("ShadowStrength", Range(0, 1)) = 0.5

		[ToggleOff] _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 1.0
		_AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_AlphaCutoffShadow("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[ToggleOff] _TransparentDepthWritePrepassEnable("Alpha Cutoff Enable", Float) = 1.0
		_AlphaCutoffPrepass("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_AlphaCutoffOpacityThreshold("Alpha Cutoff", Range(0.0, 1.0)) = 0.99

		//Eye
	}

	HLSLINCLUDE

	#pragma target 4.5
	#pragma only_renderers d3d11 ps4 metal

	//---------------------------------
	//Variant
	//---------------------------------
	#pragma shader_feature _ALPHATEST_ON
	#pragma shader_feature _DOUBLESIDED_ON


	//---------------------------------
	//Define
	//---------------------------------

	#define UNITY_MATERIAL_CHARACTER
	#define SURFACE_GRADIENT

	//---------------------------------
	//Include
	//---------------------------------
	#include "../../../ShaderLibrary/Common.hlsl"
	#include "../../../ShaderLibrary/Wind.hlsl"
	#include "../../ShaderConfig.cs.hlsl"
	#include "../../ShaderVariables.hlsl"
	#include "../../ShaderPass/FragInputs.hlsl"
	#include "../../ShaderPass/ShaderPass.cs.hlsl"

	//---------------------------------
	//Variable Decl
	//---------------------------------
	#include "../../Material/Character/CharacterProperties.hlsl"


	//NOTE: All shaders use same entry point names
	#pragma vertex   Vert
	#pragma fragment Frag

	ENDHLSL

	SubShader{
		
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300
		
		Pass{
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }
		}

		Pass{
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			Cull [_CullMode]
			ZWrite On

			HLSLPROGRAM
			
			#define SHADERPASS SHADERPASS_DEPTH_ONLY
			#include "../../Material/Material.hlsl"
			#include "ShaderPass/CharacterDepthPass.hlsl"
			#include "CharacterData.hlsl"
			#include "../../ShaderPass/ShaderPassDepthOnly.hlsl"

			ENDHLSL
		}

		Pass{
			Name "Forward"
			Tags { "LightMode"="Forward" }

			Blend  [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			Cull   [_CullMode]

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_FORWARD
			#include "../../Lighting/Forward.hlsl"
			#pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS

			#include "../../Lighting/Lighting.hlsl"
			#include "ShaderPass/CharacterSharePass.hlsl"
			#include "CharacterData.hlsl"
			#include "../../ShaderPass/ShaderPassForward.hlsl"

			ENDHLSL
		}

		Pass{
			Name "TransparentDepthWrite"
			Tags { "LightMode"="TransparentDepthWrite" }

			Cull [_CullMode]
			ZWrite On

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DEPTH_ONLY
			#define HAIR_TRANSPARENT_DEPTH_WRITE //TODO
			#include "../../Material/Material.hlsl"
			#include "ShaderPass/CharacterDepthPass.hlsl"
			#include "CharacterData.hlsl"
			#include "../../ShaderPass/ShaderPassDepthOnly.hlsl"

			ENDHLSL

		}
	
	}

	CustomEditor "Experimental.Rendering.HDPipeline.CharacterGUI"
}