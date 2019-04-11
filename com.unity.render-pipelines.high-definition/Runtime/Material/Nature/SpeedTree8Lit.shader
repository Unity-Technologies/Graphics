Shader "HDRP/Nature/SpeedTree8"
{
    Properties
    {
        _MainTex("Base (RGB) Transparency (A)", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)

        [Toggle(EFFECT_HUE_VARIATION)] _HueVariationKwToggle("Hue Variation", Float) = 0
        _HueVariationColor("Hue Variation Color", Color) = (1.0,0.5,0.0,0.1)

        [Toggle(EFFECT_BUMP)] _NormalMapKwToggle("Normal Mapping", Float) = 0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _ExtraTex("Smoothness (R), Metallic (G), AO (B)", 2D) = "(0.5, 0.0, 1.0)" {}
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

        [Toggle(EFFECT_SUBSURFACE)] _SubsurfaceKwToggle("Subsurface", Float) = 0
        _SubsurfaceTex("Subsurface (RGB)", 2D) = "white" {}
        _SubsurfaceColor("Subsurface Color", Color) = (1,1,1,1)
        _SubsurfaceIndirect("Subsurface Indirect", Range(0.0, 1.0)) = 0.25

        [Toggle(EFFECT_BILLBOARD)] _BillboardKwToggle("Billboard", Float) = 0
        _BillboardShadowFade("Billboard Shadow Fade", Range(0.0, 1.0)) = 0.5

        _Cutoff("Alpha Cutoff", Range(0.0,1.0)) = 0.333
        _ZBias("Depth Bias", Range(0.0, 0.1)) = 0.005

        [Enum(No,2,Yes,0)] _TwoSided("Two Sided", Int) = 2 // enum matches cull mode
        [KeywordEnum(None,Fastest,Fast,Better,Best,Palm)] _WindQuality("Wind Quality", Range(0,5)) = 0
        [HideInInspector] _EmissionColor("Color", Color) = (0, 0, 0)    // Base Lit material UI assumes there is an _EmissionColor, so we have it here as a placeholder.
    }

    SubShader
    {       
        // This tags allow to use the shader replacement features
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
            "RenderType" = "Opaque"
            "DisableBatching" = "LODFading"
        }

        LOD 400
        Cull [_Cull]
        
        HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #pragma enable_d3d11_debug_symbols
        
        #pragma multi_compile_instancing
        #pragma instancing_options renderinglayer assumeuniformscaling maxcount:50
        #pragma shader_feature_local _WINDQUALITY_NONE _WINDQUALITY_FASTEST _WINDQUALITY_FAST _WINDQUALITY_BETTER _WINDQUALITY_BEST _WINDQUALITY_PALM
        #pragma shader_feature_local EFFECT_BILLBOARD
        #pragma shader_feature_local EFFECT_BUMP
        #pragma shader_feature_local EFFECT_HUE_VARIATION
        #pragma shader_feature_local EFFECT_EXTRA_TEX
        #define ENABLE_WIND
        #define EFFECT_BACKSIDE_NORMALS
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_TEXCOORD0         // Use for uvHueVariation
        #define VARYINGS_NEED_TANGENT_TO_WORLD
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0

        #define CUSTOM_UNPACK                   // Needed so we can interpret the packing properly
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
        //-------------------------------------------------------------------------------------
        // Variant
        //-------------------------------------------------------------------------------------

        // enable dithering LOD crossfade
        #pragma multi_compile _ LOD_FADE_CROSSFADE

        #pragma vertex SpeedTree8Vert
        #pragma fragment Frag
        
        ENDHLSL

        Pass
        {
            Name "SceneSelectionPass" // Name is not used
            Tags{ "LightMode" = "SceneSelectionPass" }

            ColorMask 0
            Cull Off

            HLSLPROGRAM

            // We reuse depth prepass for the scene selection, allow to handle alpha correctly as well as tessellation and vertex animation
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENESELECTIONPASS // This will drive the output of the scene selection shader
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Passes.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ColorMask 0

            HLSLPROGRAM

            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE

            #define SHADERPASS SHADERPASS_SHADOWS
            #define USE_LEGACY_UNITY_MATRIX_VARIABLES
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Passes.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "GBuffer"  // Name is not used
            Tags{ "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

            HLSLPROGRAM

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // Setup DECALS_OFF so the shader stripper can remove variants
            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile _ LIGHT_LAYERS

            #ifdef _ALPHATEST_ON
            // When we have alpha test, we will force a depth prepass so we always bypass the clip instruction in the GBuffer
            #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
            #endif

            #define SHADERPASS SHADERPASS_GBUFFER
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #ifdef DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
            #endif
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Passes.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl"

            ENDHLSL
        }

        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags{ "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM

            // Lightmap memo
            // DYNAMICLIGHTMAP_ON is used when we have an "enlighten lightmap" ie a lightmap updated at runtime by enlighten.This lightmap contain indirect lighting from realtime lights and realtime emissive material.Offline baked lighting(from baked material / light,
            // both direct and indirect lighting) will hand up in the "regular" lightmap->LIGHTMAP_ON.
            
            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Passes.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthForwardOnly"}
            
            Cull Off

            ZWrite On

            ColorMask 0

            HLSLPROGRAM
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE

            #define WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_MSAA_DEPTH

            #define SHADERPASS SHADERPASS_DEPTH_ONLY

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Passes.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "ForwardOnly" }
            
            Cull Off

            HLSLPROGRAM
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            
            // Supported shadow modes per light type
            #pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH SHADOW_VERY_HIGH

            #define LIGHTLOOP_TILE_PASS
            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #ifdef GEOM_TYPE_LEAF
            #define _SURFACE_TYPE_TRANSPARENT
            #endif

            #define SHADERPASS SHADERPASS_FORWARD
            // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
            #ifndef _SURFACE_TYPE_TRANSPARENT
            #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
            #endif
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"    

            #ifdef DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
            #endif

            // The light loop (or lighting architecture) is in charge to:
            // - Define light list
            // - Define the light loop
            // - Setup the constant/data
            // - Do the reflection hierarchy
            // - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

            #define HAS_LIGHTLOOP

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8Passes.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree8LitData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"
            
            ENDHLSL
        }
    }

    FallBack "HDRP/Lit"
    CustomEditor "SpeedTree8ShaderGUI"
}
