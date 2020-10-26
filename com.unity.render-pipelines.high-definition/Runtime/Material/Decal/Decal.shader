Shader "HDRP/Decal"
{
    Properties
    {
		_BaseColor("_BaseColor", Color) = (1,1,1,1)
        _BaseColorMap("BaseColorMap", 2D) = "white" {}
        _NormalMap("NormalMap", 2D) = "bump" {}     // Tangent space normal map
        _MaskMap("MaskMap", 2D) = "white" {}
        _DecalBlend("_DecalBlend", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _NormalBlendSrc("_NormalBlendSrc", Float) = 0.0
		[HideInInspector] _MaskBlendSrc("_MaskBlendSrc", Float) = 1.0
		[HideInInspector] _DecalMeshDepthBias("_DecalMeshDepthBias", Float) = 0.0
		[HideInInspector] _DrawOrder("_DrawOrder", Int) = 0
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

        // Remapping
        [HideInInspector] _MetallicRemapMin("_MetallicRemapMin", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _MetallicRemapMax("_MetallicRemapMax", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _SmoothnessRemapMin("SmoothnessRemapMin", Float) = 0.0
        [HideInInspector] _SmoothnessRemapMax("SmoothnessRemapMax", Float) = 1.0
        [HideInInspector] _AORemapMin("AORemapMin", Float) = 0.0
        [HideInInspector] _AORemapMax("AORemapMax", Float) = 1.0

        // scaling
        [HideInInspector] _DecalMaskMapBlueScale("_DecalMaskMapBlueScale", Range(0.0, 1.0)) = 1.0

        // Alternative when no mask map is provided
        [HideInInspector] _Smoothness("_Smoothness",  Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Metallic("_Metallic",  Range(0.0, 1.0)) = 0.0
        [HideInInspector] _AO("_AO",  Range(0.0, 1.0)) = 1.0

        [HideInInspector][ToggleUI]_AffectAlbedo("Boolean", Float) = 1
        [HideInInspector][ToggleUI]_AffectNormal("Boolean", Float) = 1
        [HideInInspector][ToggleUI]_AffectAO("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_AffectMetal("Boolean", Float) = 1
        [HideInInspector][ToggleUI]_AffectSmoothness("Boolean", Float) = 1
        [HideInInspector][ToggleUI]_AffectEmission("Boolean", Float) = 0

        // Stencil state
        [HideInInspector] _DecalStencilRef("_DecalStencilRef", Int) = 16
        [HideInInspector] _DecalStencilWriteMask("_DecalStencilWriteMask", Int) = 16

		// Decal color masks
        [HideInInspector]_DecalColorMask0("_DecalColorMask0", Int) = 0
        [HideInInspector]_DecalColorMask1("_DecalColorMask1", Int) = 0
		[HideInInspector]_DecalColorMask2("_DecalColorMask2", Int) = 0
		[HideInInspector]_DecalColorMask3("_DecalColorMask3", Int) = 0

        // TODO: Remove when name garbage is solve (see IsHDRenderPipelineDecal)
        // This marker allow to identify that a Material is a HDRP/Decal
        [HideInInspector]_Unity_Identify_HDRP_Decal("_Unity_Identify_HDRP_Decal", Float) = 1.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch
    //#pragma enable_d3d11_debug_symbols

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------
    #pragma shader_feature_local _COLORMAP
    #pragma shader_feature_local _MASKMAP
    #pragma shader_feature_local _NORMALMAP
    #pragma shader_feature_local _EMISSIVEMAP

	#pragma shader_feature_local _MATERIAL_AFFECTS_ALBEDO
    #pragma shader_feature_local _MATERIAL_AFFECTS_NORMAL
    #pragma shader_feature_local _MATERIAL_AFFECTS_MASKMAP

    #pragma multi_compile_instancing

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline"}

        // c# code relies on the order in which the passes are declared, any change will need to be reflected in
        // DecalSystem.cs - enum MaterialDecalPass
        // DecalSubTarget.cs  - class SubShaders
        // Caution: passes stripped in builds (like the scene picking pass) need to be put last to have consistent indices

		Pass // 0
		{
			Name "DBufferProjector"
			Tags{"LightMode" = "DBufferProjector"} // Metalness

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

            ColorMask [_DecalColorMask0]
            ColorMask [_DecalColorMask1] 1
            ColorMask [_DecalColorMask2] 2
            ColorMask [_DecalColorMask3] 3

			HLSLPROGRAM

            #pragma multi_compile DECALS_3RT DECALS_4RT
			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalProperties.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

        Pass // 1
        {
            Name "DecalProjectorForwardEmissive"
            Tags{ "LightMode" = "DecalProjectorForwardEmissive" }

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

            #define _MATERIAL_AFFECTS_EMISSION
            #define SHADERPASS SHADERPASS_FORWARD_EMISSIVE_PROJECTOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalProperties.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

            ENDHLSL
        }

		Pass // 2
		{
			Name "DBufferMesh"
			Tags{"LightMode" = "DBufferMesh"}

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
            Blend 3 Zero OneMinusSrcColor

            ColorMask [_DecalColorMask0]
            ColorMask [_DecalColorMask1] 1
            ColorMask [_DecalColorMask2] 2
            ColorMask [_DecalColorMask3] 3

			HLSLPROGRAM

            #pragma multi_compile DECALS_3RT DECALS_4RT
            // enable dithering LOD crossfade
            #pragma multi_compile _ LOD_FADE_CROSSFADE

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalProperties.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

			ENDHLSL
		}

        Pass // 3
        {
            Name "DecalMeshForwardEmissive"
            Tags{ "LightMode" = "DecalMeshForwardEmissive" }

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
            // enable dithering LOD crossfade
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #define _MATERIAL_AFFECTS_EMISSION
            #define SHADERPASS SHADERPASS_FORWARD_EMISSIVE_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalProperties.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

            ENDHLSL
        }

        Pass // 4
        {
            Name "ScenePickingPass"
            Tags { "LightMode" = "Picking" }

            Cull Back

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

            //enable GPU instancing support
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            // enable dithering LOD crossfade
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            // Note: Require _SelectionID variable

            // We reuse depth prepass for the scene selection, allow to handle alpha correctly as well as tessellation and vertex animation
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENEPICKINGPASS
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalProperties.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"
            
            #pragma editor_sync_compilation

            ENDHLSL
        }

	}
    CustomEditor "Rendering.HighDefinition.DecalUI"
}
