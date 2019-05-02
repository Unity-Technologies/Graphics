Shader "Hidden/Forest/Standard_Terrain_BaseMap_DeferredShading_HDRenderLoop"
{
    Properties
    {
		// Unity Terrain expects these properties:
		_MainTex ("Base (RGB) Smoothness (A)", 2D) = "white" {}
		_MetallicTex ("Metallic (R)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)

		// Deps
        _HorizonFade("Horizon fade", Range(0.0, 5.0)) = 1.0
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2 // StencilBits.Standard
	}

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone metal vulkan // TEMP: until we go futher in dev
    // #pragma enable_d3d11_debug_symbols

	#pragma fragment TerrainSharedFrag

	// Need to be define before including Material.hlsl
    #define UNITY_MATERIAL_LIT

	//#pragma multi_compile _ OVERRIDE_TERRAIN_PROPERTIES

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Wind.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitProperties.hlsl"
    ENDHLSL

    SubShader
    {
		Tags{
			"Queue" = "Geometry-100"
			"RenderType" = "Opaque"
			"PerformanceChecks" = "False"
		}

        Pass
        {s
            Name "GBuffer"  // Name is not used
            Tags { "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

            //Cull  [_CullMode]
			Stencil{ Ref[_StencilRef] Comp Always Pass Replace }

            HLSLPROGRAM
			#pragma vertex TerrainSharedVert

            #define SHADERPASS SHADERPASS_GBUFFER

			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			//#pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl"

			#define TERRAIN_BASEPASS 1
			#include "Forest_Terrain_Shared.hlsl"

            ENDHLSL
        }

		Pass
        {
            Name "GBufferDebugDisplay"  // Name is not used
            Tags { "LightMode" = "GBufferDebugDisplay" } // This will be only for opaque object based on the RenderQueue index

            //Cull  [_CullMode]
			Stencil{ Ref[_StencilRef] Comp Always Pass Replace }

            HLSLPROGRAM
			#pragma vertex TerrainSharedVert

            #define DEBUG_DISPLAY
            #define SHADERPASS SHADERPASS_GBUFFER

			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			//#pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/Debug/DebugDisplay.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl"

			#define TERRAIN_BASEPASS 1
			#include "Forest_Terrain_Shared.hlsl"

            ENDHLSL
        }

		// Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM

            // Lightmap memo
            // DYNAMICLIGHTMAP_ON is used when we have an "enlighten lightmap" ie a lightmap updated at runtime by enlighten.This lightmap contain indirect lighting from realtime lights and realtime emissive material.Offline baked lighting(from baked material / light, 
            // both direct and indirect lighting) will hand up in the "regular" lightmap->LIGHTMAP_ON.

			// Use specialized vertex function
			#pragma vertex Vert
			//TerrainMetaVert

			// Need UV2 for enlighten UVs
			#define _REQUIRE_UV2 

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"

			#define TERRAIN_BASEPASS 1
			#include "Forest_Terrain_Shared.hlsl"

            ENDHLSL
        }
    }

	//Fallback "Hidden/TerrainEngine/Splatmap/Standard-Base"
}
