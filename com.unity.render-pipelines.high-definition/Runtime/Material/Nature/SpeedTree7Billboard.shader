Shader "HDRP/Nature/SpeedTree7 Billboard"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _HueVariation("Hue Variation", Color) = (1.0,0.5,0.0,0.1)
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _SpecTex("Intensity (RGB) Smoothness (A)", 2D) = "black" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.333

        [MaterialEnum(None,0,Fastest,1)] _WindQuality("Wind Quality", Range(0,1)) = 0
    }

    SubShader
    {
       // This tags allow to use the shader replacement features
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
            "RenderType" = "TransparentCutout"
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
                
        #pragma shader_feature_local EFFECT_BUMP
        #pragma shader_feature_local EFFECT_HUE_VARIATION
        #define ENABLE_WIND
        #define SPEEDTREE_BILLBOARD
        
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
        // enable dithering LOD crossfade
        #pragma multi_compile _ LOD_FADE_CROSSFADE

        #pragma vertex SpeedTree7Vert
        #pragma fragment Frag
        
        ENDHLSL

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

            #pragma vertex SpeedTree7Vert

            // When we have alpha test, we will force a depth prepass so we always bypass the clip instruction in the GBuffer
            #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST

            #define SHADERPASS SHADERPASS_GBUFFER
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #ifdef DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
            #endif
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7BillboardInput.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7CommonPasses.hlsl"            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7BillboardPasses.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ColorMask 0

            HLSLPROGRAM

            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma multi_compile __ BILLBOARD_FACE_CAMERA_POS

            #define DEPTH_ONLY
            #define SHADOW_CASTER

            #define SHADERPASS SHADERPASS_SHADOWS
            #define USE_LEGACY_UNITY_MATRIX_VARIABLES
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7BillboardInput.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7CommonPasses.hlsl"            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7BillboardPasses.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7LitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
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

            //#pragma vertex SpeedTree7VertDepth
            //#pragma vertex SpeedTree7Vert

            #define WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_MSAA_DEPTH

            #define SHADERPASS SHADERPASS_DEPTH_ONLY

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7BillboardInput.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7CommonPasses.hlsl"            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7BillboardPasses.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7LitData.hlsl"
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

            #pragma vertex SpeedTree7Vert

            #define SHADERPASS SHADERPASS_FORWARD

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

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7BillboardInput.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7CommonPasses.hlsl"            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7BillboardPasses.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7LitData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"
            
            ENDHLSL
        } 
    }
}
