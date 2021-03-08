Shader "HDRP/AxF"
{
    Properties
    {
        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

        /////////////////////////////////////////////////////////////////////////////
        // General Parameters
        // UI Only: transfered to _MappingMask
        // BUG! 6 values work, not 7 -_-
        //[Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, PlanarXY, 4, PlanarYZ, 5, PlanarZX, 6, Triplanar, 7)] _MappingMode("Mapping Mode", Float) = 0
        [HideInInspector] _MappingMode("Mapping Mode", Float) = 0
        [HideInInspector] _MappingMask("MappingMask", Vector) = (1, 0, 0, 0)
        // UI Only:
        [Enum(World, 0, Local, 1)] _PlanarSpace("Planar/Triplanar space", Float) = 0

        // Tilings and offsets
        _Material_SO( "Main Material Tiling & Offset", Vector) = (1, 1, 0, 0)
        _SVBRDF_DiffuseColorMap_SO( "_SVBRDF_DiffuseColorMap Tiling & Offset", Vector) = (1, 1, 0, 0)
        _SVBRDF_SpecularColorMap_SO( "_SVBRDF_SpecularColorMap Tiling & Offset", Vector) = (1, 1, 0, 0)
        _SVBRDF_NormalMap_SO( "_SVBRDF_NormalMap Tiling & Offset", Vector) = (1, 1, 0, 0)
        _SVBRDF_SpecularLobeMap_SO( "_SVBRDF_SpecularLobeMap Tiling & Offset", Vector) = (1, 1, 0, 0)
        _SVBRDF_AlphaMap_SO( "_SVBRDF_AlphaMap Tiling & Offset", Vector) = (1, 1, 0, 0)
        _SVBRDF_FresnelMap_SO( "_SVBRDF_FresnelMap Tiling & Offset", Vector) = (1, 1, 0, 0)
        _SVBRDF_AnisoRotationMap_SO( "_SVBRDF_AnisoRotationMap Tiling & Offset", Vector) = (1, 1, 0, 0)
        _SVBRDF_HeightMap_SO( "_SVBRDF_HeightMap Tiling & Offset", Vector) = (1, 1, 0, 0)
        _SVBRDF_ClearcoatColorMap_SO( "_SVBRDF_ClearcoatColorMap Tiling & Offset", Vector) = (1, 1, 0, 0)
        _ClearcoatNormalMap_SO( "_ClearcoatNormalMap Tiling & Offset", Vector) = (1, 1, 0, 0)
        _SVBRDF_ClearcoatIORMap_SO( "_SVBRDF_ClearcoatIORMap Tiling & Offset", Vector) = (1, 1, 0, 0)
        _CarPaint2_BTFFlakeMap_SO( "_CarPaint2_BTFFlakeMap Tiling & Offset", Vector) = (1, 1, 0, 0)

        [Enum(SVBRDF, 0, CarPaint, 1, BTF, 2)] _AxF_BRDFType("_AxF_BRDFType", Float) = 0

        [HideInInspector] _Flags( "_Flags", Int ) = 0

        /////////////////////////////////////////////////////////////////////////////
        // SVBRDF Parameters

        // SVBRDF maps
        _SVBRDF_DiffuseColorMap("_SVBRDF_DiffuseColorMap", 2D) = "white" {}
        _SVBRDF_SpecularColorMap("_SVBRDF_SpecularColorMap", 2D) = "white" {}
        _SVBRDF_NormalMap("_SVBRDF_NormalMap", 2D) = "bump" {}
        _SVBRDF_SpecularLobeMap("_SVBRDF_SpecularLobeMap", 2D) = "white" {}
        _SVBRDF_SpecularLobeMapScale("_SVBRDF_SpecularLobeMapScale", Float) = 1         // Scale is useless if we're directly provided a RG16F format
        _SVBRDF_AlphaMap("_SVBRDF_AlphaMap", 2D) = "white" {}
        _SVBRDF_FresnelMap("_SVBRDF_FresnelMap", 2D) = "white" {}
        _SVBRDF_AnisoRotationMap("_SVBRDF_AnisoRotationMap", 2D) = "black" {}
        _SVBRDF_HeightMap("_SVBRDF_HeightMap", 2D) = "black" {}
        _SVBRDF_ClearcoatColorMap("_SVBRDF_ClearcoatColorMap", 2D) = "white" {}
        _ClearcoatNormalMap("_ClearcoatNormal", 2D) = "bump" {}
        _SVBRDF_ClearcoatIORMap("_SVBRDF_ClearcoatIORMap", 2D) = "black" {}

        // SVBRDF Constants
        [HideInInspector] _SVBRDF_BRDFType( "_SVBRDF_BRDFType", Int ) = 0
        [HideInInspector] _SVBRDF_BRDFVariants( "_SVBRDF_BRDFVariants", Int ) = 0
        [HideInInspector] _SVBRDF_HeightMapMaxMM( "_SVBRDF_HeightMapMax", Float ) = 0


        /////////////////////////////////////////////////////////////////////////////
        // Car Paint Parameters
        _CarPaint2_CTDiffuse("_CarPaint2_CTDiffuse", Float) = 0
        _CarPaint2_ClearcoatIOR("_CarPaint2_ClearcoatIOR", Float) = 1

        // BRDF
        _CarPaint2_BRDFColorMapScale("_CarPaint2_BRDFColorMapScale", Float) = 1        // Scale is useless if we're directly provided a RGBA16F format
        _CarPaint2_BRDFColorMap("_CarPaint2_BRDFColorMap", 2D) = "white" {}
        _CarPaint2_BRDFColorMapUVScale("_CarPaint2_BRDFColorMapUVScale", Vector) = (1,1,0,0)  // To be used when we have the bit BRDFColorUseDiagonalClamp set in _Flags

        // Flakes
        _CarPaint2_BTFFlakeMapScale("_CarPaint2_BTFFlakeMapScale", Float) = 1         // Scale is useless if we're directly provided a RGBA16F format
        _CarPaint2_BTFFlakeMap("_CarPaint2_BTFFlakeMap", 2DArray) = "black" {}
        _CarPaint2_FlakeThetaFISliceLUTMap( "_CarPaint2_FlakeThetaFISliceLUTMap", 2D ) = "black" {}

        _CarPaint2_FlakeMaxThetaI("_CarPaint2_FlakeMaxThetaI", Int) = 0
        _CarPaint2_FlakeNumThetaF("_CarPaint2_FlakeNumThetaF", Int) = 0
        _CarPaint2_FlakeNumThetaI("_CarPaint2_FlakeNumThetaI", Int) = 0

        // Cook-Torrance Lobes Descriptors
        _CarPaint2_LobeCount("_CarPaint2_LobeCount", Int) = 0
        _CarPaint2_CTF0s("_CarPaint2_CTF0s", Vector) = (1,1,1,1)
        _CarPaint2_CTCoeffs("_CarPaint2_CTCoeffs", Vector) = (1,1,1,1)
        _CarPaint2_CTSpreads("_CarPaint2_CTSpreads", Vector) = (1,1,1,1)

        // GUI inspector only - saves state in material meta, read back from SetupMaterialKeywordsAndPass
        //[Enum(Off, 0, From Ambient Occlusion, 1, From Bent Normals, 2)]  _SpecularOcclusionMode("Specular Occlusion Mode", Int) = 1
        [Enum(Off, 0, From Ambient Occlusion, 1)]  _SpecularOcclusionMode("Specular Occlusion Mode", Int) = 1

        [ToggleUI]  _UseShadowThreshold("_UseShadowThreshold", Float) = 0.0
        [ToggleUI]  _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _AlphaCutoffShadow("_AlphaCutoffShadow", Range(0.0, 1.0)) = 0.5

        _TransparentSortPriority("_TransparentSortPriority", Float) = 0

        // Stencil state
        // Forward
        [HideInInspector] _StencilRef("_StencilRef", Int) = 0   // StencilUsage.Clear
        [HideInInspector] _StencilWriteMask("_StencilWriteMask", Int) = 6 // StencilUsage.RequiresDeferredLighting | StencilUsage.SubsurfaceScattering
        // Depth prepass
        [HideInInspector] _StencilRefDepth("_StencilRefDepth", Int) = 0 // StencilUsage.Clear
        [HideInInspector] _StencilWriteMaskDepth("_StencilWriteMaskDepth", Int) = 8 // StencilUsage.TraceReflectionRay (8)
        // Motion vector pass
        [HideInInspector] _StencilRefMV("_StencilRefMV", Int) = 32 // StencilUsage.ObjectMotionVector (32)
        [HideInInspector] _StencilWriteMaskMV("_StencilWriteMaskMV", Int) = 40 // StencilUsage.ObjectMotionVector (32) | StencilUsage.TraceReflectionRay (8) as it can be a prepass

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _AlphaSrcBlend("__alphaSrc", Float) = 1.0
        [HideInInspector] _AlphaDstBlend("__alphaDst", Float) = 0.0
        [HideInInspector][ToggleUI]_AlphaToMaskInspectorValue("_AlphaToMaskInspectorValue", Float) = 0 // Property used to save the alpha to mask state in the inspector
        [HideInInspector][ToggleUI]_AlphaToMask("__alphaToMask", Float) = 0
        [HideInInspector][ToggleUI] _ZWrite("__zw", Float) = 1.0
        [HideInInspector][ToggleUI] _TransparentZWrite("_TransparentZWrite", Float) = 0.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _CullModeForward("__cullmodeForward", Float) = 2.0 // This mode is dedicated to Forward to correctly handle backface then front face rendering thin transparent
        [HideInInspector] _ZTestDepthEqualForOpaque("_ZTestDepthEqualForOpaque", Int) = 4 // Less equal


//      [ToggleUI] _EnableFogOnTransparent("Enable Fog", Float) = 1.0
        [HideInInspector][ToggleUI] _EnableBlendModePreserveSpecularLighting("Enable Blend Mode Preserve Specular Lighting", Float) = 1.0

        [ToggleUI] _DoubleSidedEnable("Double sided enable", Float) = 0.0
        [Enum(Flip, 0, Mirror, 1, None, 2)] _DoubleSidedNormalMode("Double sided normal mode", Float) = 1 // This is for the editor only, see BaseLitUI.cs: _DoubleSidedConstants will be set based on the mode.
        [HideInInspector] _DoubleSidedConstants("_DoubleSidedConstants", Vector) = (1, 1, -1, 0)

        [ToggleUI] _EnableGeometricSpecularAA("EnableGeometricSpecularAA", Float) = 0.0
        _SpecularAAScreenSpaceVariance("SpecularAAScreenSpaceVariance", Range(0.0, 1.0)) = 0.1
        _SpecularAAThreshold("SpecularAAThreshold", Range(0.0, 1.0)) = 0.2

        // Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
        // value that exist to identify if the GI emission need to be enabled.
        // In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
        // TODO: Fix the code in legacy unity so we can customize the beahvior for GI
        _EmissionColor("Color", Color) = (1, 1, 1)

        // HACK: GI Baking system relies on some properties existing in the shader ("_MainTex", "_Cutoff" and "_Color") for opacity handling, so we need to store our version of those parameters in the hard-coded name the GI baking system recognizes.
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [ToggleUI] _SupportDecals("Support Decals", Float) = 1.0
        [ToggleUI] _ReceivesSSR("Receives SSR", Float) = 1.0
        [ToggleUI] _ReceivesSSRTransparent("Receives SSR Transparent", Float) = 0.0
        [ToggleUI] _AddPrecomputedVelocity("AddPrecomputedVelocity", Float) = 0.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------
    #pragma shader_feature_local _AXF_BRDF_TYPE_SVBRDF _AXF_BRDF_TYPE_CAR_PAINT _AXF_BRDF_TYPE_BTF

    #pragma shader_feature_local _ _SPECULAR_OCCLUSION_NONE //_SPECULAR_OCCLUSION_FROM_BENT_NORMAL_MAP

    #pragma shader_feature_local _ _MAPPING_PLANAR _MAPPING_TRIPLANAR
    #pragma shader_feature_local _ _REQUIRE_UV1 _REQUIRE_UV2 _REQUIRE_UV3
    #pragma shader_feature_local _ _PLANAR_LOCAL

    #pragma shader_feature_local _ALPHATEST_ON
    #pragma shader_feature_local _ALPHATOMASK_ON
    #pragma shader_feature_local _DOUBLESIDED_ON

    #pragma shader_feature_local _DISABLE_DECALS
    #pragma shader_feature_local _DISABLE_SSR
    #pragma shader_feature_local _DISABLE_SSR_TRANSPARENT
    #pragma shader_feature_local _ENABLE_GEOMETRIC_SPECULAR_AA

    #pragma shader_feature_local _ADD_PRECOMPUTED_VELOCITY

    // Keyword for transparent
    #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
    #pragma shader_feature_local _ENABLE_FOG_ON_TRANSPARENT

    // enable dithering LOD crossfade
    #pragma multi_compile _ LOD_FADE_CROSSFADE

    //enable GPU instancing support
    #pragma multi_compile_instancing
    #pragma instancing_options renderinglayer

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    #define SUPPORT_BLENDMODE_PRESERVE_SPECULAR_LIGHTING

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFProperties.hlsl"

    ENDHLSL

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDLitShader" }

        Pass
        {
            Name "ScenePickingPass"
            Tags { "LightMode" = "Picking" }

            Cull [_CullMode]

            HLSLPROGRAM

            // Note: Require _SelectionID variable

            // We reuse depth prepass for the scene selection, allow to handle alpha correctly as well as tessellation and vertex animation
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENEPICKINGPASS
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFDepthPass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma editor_sync_compilation

            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }

            Cull Off

            HLSLPROGRAM

            // Note: Require _ObjectId and _PassValue variables

            // We reuse depth prepass for the scene selection, allow to handle alpha correctly as well as tessellation and vertex animation
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENESELECTIONPASS // This will drive the output of the scene selection shader
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFDepthPass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma editor_sync_compilation

            ENDHLSL
        }

        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags{ "LightMode" = "META" }


            HLSLPROGRAM

            // Lightmap memo
            // DYNAMICLIGHTMAP_ON is used when we have an "enlighten lightmap" ie a lightmap updated at runtime by enlighten.This lightmap contain indirect lighting from realtime lights and realtime emissive material.Offline baked lighting(from baked material / light,
            // both direct and indirect lighting) will hand up in the "regular" lightmap->LIGHTMAP_ON.

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }

            Cull[_CullMode]

            ZClip [_ZClip]
            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_SHADOWS
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFDepthPass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            Name "DepthForwardOnly"
            Tags{ "LightMode" = "DepthForwardOnly" }

            Cull[_CullMode]
            AlphaToMask [_AlphaToMask]

            ZWrite On

            Stencil
            {
                WriteMask[_StencilWriteMaskDepth]
                Ref[_StencilRefDepth]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM

            #define WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_MSAA_DEPTH
            #pragma multi_compile _ WRITE_DECAL_BUFFER

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            Name "MotionVectors"
            Tags{ "LightMode" = "MotionVectors" } // Caution, this need to be call like this to setup the correct parameters by C++ (legacy Unity)

            // If velocity pass (motion vectors) is enabled we tag the stencil so it don't perform CameraMotionVelocity
            Stencil
            {
                WriteMask [_StencilWriteMaskMV]
                Ref [_StencilRefMV]
                Comp Always
                Pass Replace
            }

            Cull[_CullMode]
            AlphaToMask [_AlphaToMask]

            ZWrite On

            HLSLPROGRAM

            #define WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_DECAL_BUFFER
            #pragma multi_compile _ WRITE_MSAA_DEPTH

            #define SHADERPASS SHADERPASS_MOTION_VECTORS
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        // AxF shader always render in forward
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]
            // In case of forward we want to have depth equal for opaque mesh
            ZTest [_ZTestDepthEqualForOpaque]
            ZWrite [_ZWrite]
            Cull [_CullModeForward]
            ColorMask [_ColorMaskTransparentVel] 1

            HLSLPROGRAM

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON
            // Setup DECALS_OFF so the shader stripper can remove variants
            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT

            // Supported shadow modes per light type
            #pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH

            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #define SHADERPASS SHADERPASS_FORWARD
            // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
            // Don't do it with debug display mode as it is possible there is no depth prepass in this case
            #if !defined(_SURFACE_TYPE_TRANSPARENT) && !defined(DEBUG_DISPLAY)
                #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
            #endif
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
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

        Pass
        {
            Name "FullScreenDebug"
            Tags{ "LightMode" = "FullScreenDebug" }

            Cull[_CullMode]

            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_FULL_SCREEN_DEBUG
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassFullScreenDebug.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }

    CustomEditor "Rendering.HighDefinition.AxFGUI"
}
