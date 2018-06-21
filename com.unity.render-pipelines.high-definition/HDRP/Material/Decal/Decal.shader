Shader "HDRenderPipeline/Decal"
{
    Properties
    {
		_BaseColor("_BaseColor", Color) = (1,1,1,1)
        _BaseColorMap("BaseColorMap", 2D) = "white" {}
        _NormalMap("NormalMap", 2D) = "bump" {}     // Tangent space normal map
        _MaskMap("MaskMap", 2D) = "white" {}
        _DecalBlend("_DecalBlend", Range(0.0, 1.0)) = 0.5
		[ToggleUI] _AlbedoMode("_AlbedoMode", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _NormalBlendSrc("_NormalBlendSrc", Float) = 0.0
		[HideInInspector] _MaskBlendSrc("_MaskBlendSrc", Float) = 1.0
		[HideInInspector] _MaskBlendMode("_MaskBlendMode", Float) = 0.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    //#pragma enable_d3d11_debug_symbols

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------
    #pragma shader_feature _COLORMAP
    #pragma shader_feature _NORMALMAP
    #pragma shader_feature _MASKMAP
	#pragma shader_feature _ALBEDOCONTRIBUTION

	#pragma shader_feature _NORMAL_BLEND_MASK_B
	#pragma shader_feature _MAOS_BLEND_MASK_B

    #pragma multi_compile_instancing
    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------
    #define UNITY_MATERIAL_DECAL

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "CoreRP/ShaderLibrary/Common.hlsl"
    #include "CoreRP/ShaderLibrary/Wind.hlsl"
    #include "../../ShaderPass/FragInputs.hlsl"
    #include "../../ShaderPass/ShaderPass.cs.hlsl"


    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "DecalProperties.hlsl"

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline"}

        Pass
        {
            Name "DBufferProjector_MAOS"  // Name is not used
            Tags { "LightMode" = "DBufferProjector_MAOS" } // Metalness AO and Smoothness
            // back faces with zfail, for cases when camera is inside the decal volume
            Cull Front
            ZWrite Off
            ZTest Greater
            // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
            #include "../../ShaderVariables.hlsl"
            #include "Decal.hlsl"
            #include "ShaderPass/DecalSharePass.hlsl"
            #include "DecalData.hlsl"
            #include "../../ShaderPass/ShaderPassDBuffer.hlsl"

            ENDHLSL
        }

		Pass
		{
			Name "DBufferProjector_MS"  // Name is not used
			Tags{"LightMode" = "DBufferProjector_MS"} // Metalness and Smoothness
			// back faces with zfail, for cases when camera is inside the decal volume
			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask RBA 2	// metal/smoothness/smoothness alpha 
			ColorMask R 3	// metal alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
			#include "../../ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "../../ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferProjector_M"  // Name is not used
			Tags{"LightMode" = "DBufferProjector_M"} // Metalness 
			// back faces with zfail, for cases when camera is inside the decal volume
			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask R 2	// metal 
			ColorMask R 3	// metal alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
			#include "../../ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "../../ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferProjector_S"  // Name is not used
			Tags{"LightMode" = "DBufferProjector_S"} // Smoothness 
			// back faces with zfail, for cases when camera is inside the decal volume
			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask BA 2	// smoothness/smoothness alpha
			ColorMask 0 3

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
			#include "../../ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "../../ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferProjector_AO"  // Name is not used
			Tags{"LightMode" = "DBufferProjector_AO"} // AO only
			// back faces with zfail, for cases when camera is inside the decal volume
			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask G 2	// ao
			ColorMask G 3	// ao alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
			#include "../../ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "../../ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferMesh_MAOS"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_MAOS"} // Metalness AO and Smoothness
										 
			Cull Back
			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "../../ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "../../ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferMesh_MS"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_MS"} // Metalness and smoothness

			Cull Back
			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask RBA 2	// metal/smoothness/smoothness alpha 
			ColorMask R 3	// metal alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "../../ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "../../ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferMesh_M"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_M"} // Metalness only

			Cull Back
			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask R 2	// metal 
			ColorMask R 3	// metal alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "../../ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "../../ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferMesh_S"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_S"} // Metalness only

			Cull Back
			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask BA 2	// smoothness/smoothness alpha
			ColorMask 0 3	

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "../../ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "../../ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferMesh_AO"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_AO"} // AO only

			Cull Back
			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask G 2	// ao
			ColorMask G 3	// ao alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "../../ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "../../ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}
    }
    CustomEditor "Experimental.Rendering.HDPipeline.DecalUI"
}
