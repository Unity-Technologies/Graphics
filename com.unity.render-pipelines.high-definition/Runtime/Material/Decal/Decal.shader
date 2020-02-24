Shader "HDRP/Decal"
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
		[HideInInspector] _MaskBlendMode("_MaskBlendMode", Float) = 4.0 // smoothness 3RT default
		[ToggleUI] _MaskmapMetal("_MaskmapMetal", Range(0.0, 1.0)) = 0.0
		[ToggleUI] _MaskmapAO("_MaskmapAO", Range(0.0, 1.0)) = 0.0
		[ToggleUI] _MaskmapSmoothness("_MaskmapSmoothness", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _DecalMeshDepthBias("_DecalMeshDepthBias", Float) = 0.0
		[HideInInspector] _DrawOrder("_DrawOrder", Int) = 0
        [ToggleUI] _Emissive("_Emissive", Range(0.0, 1.0)) = 0.0
        [HDR] _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        // Used only to serialize the LDR and HDR emissive color in the material UI,
        // in the shader only the _EmissiveColor should be used
        [HideInInspector] _EmissiveColorLDR("EmissiveColor LDR", Color) = (0, 0, 0)
        [HDR][HideInInspector] _EmissiveColorHDR("EmissiveColor HDR", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        [HideInInspector] _EmissiveIntensityUnit("Emissive Mode", Int) = 0
        [ToggleUI] _UseEmissiveIntensity("Use Emissive Intensity", Int) = 0
        _EmissiveIntensity("Emissive Intensity", Float) = 1
        _EmissiveExposureWeight("Emissive Pre Exposure", Range(0.0, 1.0)) = 1.0


        // Stencil state
        [HideInInspector] _DecalStencilRef("_DecalStencilRef", Int) = 16
        [HideInInspector] _DecalStencilWriteMask("_DecalStencilWriteMask", Int) = 16

        // Remapping
        [HideInInspector] _SmoothnessRemapMin("SmoothnessRemapMin", Float) = 0.0
        [HideInInspector] _SmoothnessRemapMax("SmoothnessRemapMax", Float) = 1.0
        [HideInInspector] _AORemapMin("AORemapMin", Float) = 0.0
        [HideInInspector] _AORemapMax("AORemapMax", Float) = 1.0

        // scaling
        [HideInInspector] _MetallicScale("_MetallicScale", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _DecalMaskMapBlueScale("_DecalMaskMapBlueScale", Range(0.0, 1.0)) = 1.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    //#pragma enable_d3d11_debug_symbols

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------
    #pragma shader_feature_local _COLORMAP
    #pragma shader_feature_local _NORMALMAP
    #pragma shader_feature_local _MASKMAP
    #pragma shader_feature_local _EMISSIVEMAP
	#pragma shader_feature_local _ALBEDOCONTRIBUTION

    #pragma multi_compile_instancing

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalProperties.hlsl"

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline"}

		// c# code relies on the order in which the passes are declared, any change will need to be reflected in Decalsystem.cs - s_MaterialDecalNames and s_MaterialDecalSGNames array
        // and DecalSet.InitializeMaterialValues()

		// pass 0 is mesh 3RT mode
		Pass
		{
			Name "DBufferMesh_3RT"
			Tags{"LightMode" = "DBufferMesh_3RT"} // Smoothness

            Stencil
            {
                WriteMask [_DecalStencilWriteMask]
                Ref [_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha

			ColorMask BA 2	// smoothness/smoothness alpha

			HLSLPROGRAM

            #define DECALS_3RT
			#define SHADERPASS SHADERPASS_DBUFFER_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

		// enum MaskBlendFlags
		//{
		//	Metal = 1 << 0,
		//	AO = 1 << 1,
		//	Smoothness = 1 << 2,
		//}

		// Projectors
		//
		// 1 - Metal
		// 2 - AO
		// 3 - Metal + AO
		// 4 - Smoothness also 3RT
		// 5 - Metal + Smoothness
		// 6 - AO + Smoothness
		// 7 - Metal + AO + Smoothness
		//

		Pass // 1
		{
			Name "DBufferProjector_M"
			Tags{"LightMode" = "DBufferProjector_M"} // Metalness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

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

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

		Pass // 2
		{
			Name "DBufferProjector_AO"
			Tags{"LightMode" = "DBufferProjector_AO"} // AO only
													  // back faces with zfail, for cases when camera is inside the decal volume
            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

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

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

		Pass // 3
		{
			Name "DBufferProjector_MAO"
			Tags{"LightMode" = "DBufferProjector_MAO"} // AO + Metalness
													   // back faces with zfail, for cases when camera is inside the decal volume
            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask RG 2	// metalness + ao
			ColorMask RG 3	// metalness alpha + ao alpha

			HLSLPROGRAM

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

		Pass // 4
		{
			Name "DBufferProjector_S"
			Tags{"LightMode" = "DBufferProjector_S"} // Smoothness - also use as DBufferProjector_3RT

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			// back faces with zfail, for cases when camera is inside the decal volume
			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha

			ColorMask BA 2	// smoothness/smoothness alpha
            ColorMask 0 3   // Caution: We need to setup the mask to 0 in case perChannelMAsk is enabled as 4 RT are bind

			HLSLPROGRAM

            // We need multicompile here as DBufferProjector_S is also use as DBufferProjector_3RT so for both 3RT and 4RT
            #pragma multi_compile DECALS_3RT DECALS_4RT

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

		Pass // 5
		{
			Name "DBufferProjector_MS"
			Tags{"LightMode" = "DBufferProjector_MS"} // Smoothness and Metalness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

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

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

		Pass // 6
		{
			Name "DBufferProjector_AOS"
			Tags{"LightMode" = "DBufferProjector_AOS"} // AO + Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			// back faces with zfail, for cases when camera is inside the decal volume
			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask GBA 2	// ao, smoothness, smoothness alpha
			ColorMask G 3	// ao alpha

			HLSLPROGRAM

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

        Pass // 7
        {
            Name "DBufferProjector_MAOS"
            Tags { "LightMode" = "DBufferProjector_MAOS" } // Metalness AO and Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

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

            #define DECALS_4RT
            #define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

            ENDHLSL
        }

		// Mesh
		// 8 - Metal
		// 9 - AO
		// 10 - Metal + AO
		// 11 - Smoothness
		// 12 - Metal + Smoothness
		// 13 - AO + Smoothness
		// 14 - Metal + AO + Smoothness

		Pass // 8
		{
			Name "DBufferMesh_M"
			Tags{"LightMode" = "DBufferMesh_M"} // Metalness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

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

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

		Pass // 9
		{
			Name "DBufferMesh_AO"
			Tags{"LightMode" = "DBufferMesh_AO"} // AO only

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

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

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

		Pass // 10
		{
			Name "DBufferMesh_MAO"
			Tags{"LightMode" = "DBufferMesh_MAO"} // AO + Metalness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask RG 2	// metalness + ao
			ColorMask RG 3	// metalness alpha + ao alpha

			HLSLPROGRAM

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

		Pass // 11
		{
			Name "DBufferMesh_S"
			Tags{"LightMode" = "DBufferMesh_S"} // Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha

			ColorMask BA 2	// smoothness/smoothness alpha
            ColorMask 0 3   // Caution: We need to setup the mask to 0 in case perChannelMAsk is enabled as 4 RT are bind

			HLSLPROGRAM

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}


		Pass // 12
		{
			Name "DBufferMesh_MS"
			Tags{"LightMode" = "DBufferMesh_MS"} // Smoothness and Metalness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

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

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

		Pass // 13
		{
			Name "DBufferMesh_AOS"
			Tags{"LightMode" = "DBufferMesh_AOS"} // AO + Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask GBA 2	// ao, smoothness, smoothness alpha
			ColorMask G 3	// ao alpha

			HLSLPROGRAM

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

		Pass // 14
		{
			Name "DBufferMesh_MAOS"
			Tags{"LightMode" = "DBufferMesh_MAOS"} // Metalness AO and Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			HLSLPROGRAM

            #define DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

        Pass // 15
        {
            Name "Projector_Emissive"
            Tags{ "LightMode" = "Projector_Emissive" } // Emissive

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }
            // back faces with zfail, for cases when camera is inside the decal volume
            Cull Front
            ZWrite Off
            ZTest Greater

            // additive
            Blend 0 SrcAlpha One

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_FORWARD_EMISSIVE_PROJECTOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

            ENDHLSL
        }

        Pass // 16
        {
            Name "Mesh_Emissive"
            Tags{ "LightMode" = "Mesh_Emissive" } // Emissive

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }
            // back faces with zfail, for cases when camera is inside the decal volume
            ZWrite Off
            ZTest LEqual

            // additive
            Blend 0 SrcAlpha One

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_FORWARD_EMISSIVE_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

            ENDHLSL
        }

	}
    CustomEditor "Rendering.HighDefinition.DecalUI"
}
