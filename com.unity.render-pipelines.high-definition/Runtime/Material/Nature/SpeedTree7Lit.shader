Shader "HDRP/Nature/SpeedTree7"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _HueVariation("Hue Variation", Color) = (1.0,0.5,0.0,0.1)
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _DetailTex("Detail", 2D) = "black" {}
        _BumpMap("Normal Map", 2D) = "bump" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.333
        [MaterialEnum(Off,0,Front,1,Back,2)] _Cull("Cull", Int) = 2
        [MaterialEnum(None,0,Fastest,1,Fast,2,Better,3,Best,4,Palm,5)] _WindQuality("Wind Quality", Range(0,5)) = 0
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
        
        #pragma multi_compile_instancing
        #pragma instancing_options renderinglayer assumeuniformscaling maxcount:50
        #pragma shader_feature_local GEOM_TYPE_BRANCH GEOM_TYPE_BRANCH_DETAIL GEOM_TYPE_FROND GEOM_TYPE_LEAF GEOM_TYPE_MESH
        #pragma shader_feature_local EFFECT_BUMP
        #pragma shader_feature_local EFFECT_HUE_VARIATION
        #define ENABLE_WIND
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_TEXCOORD0         // Use for uvHueVariation
        // Because we need more than two components for certain values, we can't just share a single interpolator like we do with TEXCOORD0 and TEXCOORD1
        #ifdef GEOM_BRANCH_DETAIL
            #define VARYINGS_NEED_TEXCOORD3     // Use for detail
        #endif
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        //#include "SpeedTreeCommon.cginc"
        
        //-------------------------------------------------------------------------------------
        // Variant
        //-------------------------------------------------------------------------------------

        // enable dithering LOD crossfade
        #pragma multi_compile _ LOD_FADE_CROSSFADE

        #pragma vertex SpeedTree7Vert
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
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Passes.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            HLSLPROGRAM

            Cull[_CullMode]

            ZClip[_ZClip]
            ZWrite On
            ZTest LEqual

            ColorMask 0

            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_SHADOWS
            #define USE_LEGACY_UNITY_MATRIX_VARIABLES
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Passes.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "GBuffer"  // Name is not used
            Tags{ "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

            Cull[_CullMode]
            ZTest[_ZTestGBuffer]

            Stencil
            {
                WriteMask[_StencilWriteMask]
                Ref[_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // Setup DECALS_OFF so the shader stripper can remove variants
            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile _ LIGHT_LAYERS
            #pragma multi_compile _ VARYINGS_NEED_COLOR

            #define VARYINGS_NEED_TANGENT_TO_WORLD

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
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Passes.hlsl"
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
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Passes.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthForwardOnly"}
            
            Cull Off

            ZWrite On

            Stencil
            {
                WriteMask[_StencilDepthPrepassWriteMask]
                Ref[_StencilDepthPrepassRef]
                Comp Always
                Pass Replace
            }

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

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Passes.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "ForwardOnly" }
            
            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            Blend [_SrcBlend] [_DstBlend]
            // In case of forward we want to have depth equal for opaque mesh
            ZTest [_ZTestDepthEqualForOpaque]
            ZWrite [_ZWrite]
            Cull [_CullModeForward]

            HLSLPROGRAM
            #define VARYINGS_NEED_COLOR
            
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            
            // Supported shadow modes per light type
            #pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH SHADOW_VERY_HIGH

            #define LIGHTLOOP_TILE_PASS
            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #define VARYINGS_NEED_TANGENT_TO_WORLD

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

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Input.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree7Passes.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"
            
            ENDHLSL
        }
    }

    Dependency "BillboardShader" = "HDRP/Nature/SpeedTree7 Billboard"
    FallBack "HDRP/Lit"
    //CustomEditor "SpeedTreeMaterialInspector"
}
