Shader "HDRenderPipeline/LitTessellation"
{
    Properties
    {
        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

        // Reminder. Color here are in linear but the UI (color picker) do the conversion sRGB to linear
        _BaseColor("BaseColor", Color) = (1,1,1,1)
        _BaseColorMap("BaseColorMap", 2D) = "white" {}

        _Metallic("_Metallic", Range(0.0, 1.0)) = 0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 1.0
        _MaskMap("MaskMap", 2D) = "white" {}

        _SpecularOcclusionMap("SpecularOcclusion", 2D) = "white" {}

        _NormalMap("NormalMap", 2D) = "bump" {}     // Tangent space normal map
        _NormalMapOS("NormalMapOS", 2D) = "white" {} // Object space normal map - no good default value
        _NormalScale("_NormalScale", Range(0.0, 2.0)) = 1

        _HeightMap("HeightMap", 2D) = "black" {}
        _HeightAmplitude("Height Amplitude", Float) = 0.01 // In world units
        _HeightCenter("Height Center", Float) = 0.5 // In texture space

        _DetailMap("DetailMap", 2D) = "black" {}
        _DetailMask("DetailMask", 2D) = "white" {}
        _DetailAlbedoScale("_DetailAlbedoScale", Range(-2.0, 2.0)) = 1
        _DetailNormalScale("_DetailNormalScale", Range(0.0, 2.0)) = 1
        _DetailSmoothnessScale("_DetailSmoothnessScale", Range(-2.0, 2.0)) = 1

        _TangentMap("TangentMap", 2D) = "bump" {}
        _TangentMapOS("TangentMapOS", 2D) = "white" {}
        _Anisotropy("Anisotropy", Range(0.0, 1.0)) = 0
        _AnisotropyMap("AnisotropyMap", 2D) = "white" {}

        _SubsurfaceProfile("Subsurface Profile", Int) = 0
        _SubsurfaceRadius("Subsurface Radius", Range(0.0, 1.0)) = 1.0
        _SubsurfaceRadiusMap("Subsurface Radius Map", 2D) = "white" {}
        _Thickness("Thickness", Range(0.0, 1.0)) = 1.0
        _ThicknessMap("Thickness Map", 2D) = "white" {}

        _SpecularColor("SpecularColor", Color) = (1, 1, 1, 1)
        _SpecularColorMap("SpecularColorMap", 2D) = "white" {}

        // Wind
        [ToggleOff]  _EnableWind("Enable Wind", Float) = 0.0
        _InitialBend("Initial Bend", float) = 1.0
        _Stiffness("Stiffness", float) = 1.0
        _Drag("Drag", float) = 1.0
        _ShiverDrag("Shiver Drag", float) = 0.2
        _ShiverDirectionality("Shiver Directionality", Range(0.0, 1.0)) = 0.5

        _DistortionVectorMap("DistortionVectorMap", 2D) = "black" {}

        // Following options are for the GUI inspector and different from the input parameters above
        // These option below will cause different compilation flag.

        _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        _EmissiveIntensity("EmissiveIntensity", Float) = 0

        [ToggleOff] _DistortionEnable("Enable Distortion", Float) = 0.0
        [ToggleOff] _DistortionOnly("Distortion Only", Float) = 0.0
        [ToggleOff] _DistortionDepthTest("Distortion Depth Test Enable", Float) = 0.0
        [ToggleOff] _DepthOffsetEnable("Depth Offset View space", Float) = 0.0

        [ToggleOff]  _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _HorizonFade("Horizon fade", Range(0.0, 5.0)) = 1.0

        // Stencil state
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2 // StencilLightingUsage.RegularLighting  (fixed at compile time)

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _ZTestMode("_ZTestMode", Int) = 8

        [ToggleOff] _DoubleSidedEnable("Double sided enable", Float) = 0.0
        [Enum(None, 0, Mirror, 1, Flip, 2)] _DoubleSidedNormalMode("Double sided normal mode", Float) = 1
        [HideInInspector] _DoubleSidedConstants("_DoubleSidedConstants", Vector) = (1, 1, -1, 0)

        [Enum(UV0, 0, Planar, 1, TriPlanar, 2)] _UVBase("UV Set for base", Float) = 0
        _TexWorldScale("Scale to apply on world coordinate", Float) = 1.0
        [HideInInspector] _UVMappingMask("_UVMappingMask", Color) = (1, 0, 0, 0)
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace("NormalMap space", Float) = 0

        [Enum(Subsurface Scattering, 0, Standard, 1, Specular Color, 2)] _MaterialID("MaterialId", Int) = 1 // MaterialId.LitStandard

        [ToggleOff]  _EnablePerPixelDisplacement("Enable per pixel displacement", Float) = 0.0
        _PPDMinSamples("Min sample for POM", Range(1.0, 64.0)) = 5
        _PPDMaxSamples("Max sample for POM", Range(1.0, 64.0)) = 15
        _PPDLodThreshold("Start lod to fade out the POM effect", Range(0.0, 16.0)) = 5
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail("UV Set for detail", Float) = 0
        [HideInInspector] _UVDetailsMappingMask("_UVDetailsMappingMask", Color) = (1, 0, 0, 0)
        [Enum(Use Emissive Color, 0, Use Emissive Mask, 1)] _EmissiveColorMode("Emissive color mode", Float) = 1

        // Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
        // value that exist to identify if the GI emission need to be enabled.
        // In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
        // TODO: Fix the code in legacy unity so we can customize the beahvior for GI
        _EmissionColor("Color", Color) = (1, 1, 1)

        // Tessellation specific
        [Enum(Phong, 0, Displacement, 1, DisplacementPhong, 2)] _TessellationMode("Tessellation mode", Float) = 0
        _TessellationFactor("Tessellation Factor", Range(0.0, 15.0)) = 4.0
        _TessellationFactorMinDistance("Tessellation start fading distance", Float) = 20.0
        _TessellationFactorMaxDistance("Tessellation end fading distance", Float) = 50.0
        _TessellationFactorTriangleSize("Tessellation triangle size", Float) = 100.0
        _TessellationShapeFactor("Tessellation shape factor", Range(0.0, 1.0)) = 0.75 // Only use with Phong
        _TessellationBackFaceCullEpsilon("Tessellation back face epsilon", Range(-1.0, 0.0)) = -0.25
        [ToggleOff] _TessellationObjectScale("Tessellation object scale", Float) = 0.0
        [ToggleOff] _TessellationTilingScale("Tessellation tiling height scale", Float) = 1.0
         // TODO: Handle culling mode for backface culling
    }

    HLSLINCLUDE

    #pragma target 5.0
    #pragma only_renderers d3d11 ps4// TEMP: until we go futher in dev
    // #pragma enable_d3d11_debug_symbols

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _DISTORTION_ON
    #pragma shader_feature _DEPTHOFFSET_ON
    #pragma shader_feature _DOUBLESIDED_ON
    #pragma shader_feature _PER_PIXEL_DISPLACEMENT
    // Default is _TESSELLATION_PHONG
    #pragma shader_feature _ _TESSELLATION_DISPLACEMENT _TESSELLATION_DISPLACEMENT_PHONG
    #pragma shader_feature _TESSELLATION_OBJECT_SCALE
    #pragma shader_feature _TESSELLATION_TILING_SCALE

    #pragma shader_feature _ _MAPPING_PLANAR _MAPPING_TRIPLANAR
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE
    #pragma shader_feature _ _REQUIRE_UV2 _REQUIRE_UV3
    #pragma shader_feature _EMISSIVE_COLOR

    #pragma shader_feature _NORMALMAP
    #pragma shader_feature _MASKMAP
    #pragma shader_feature _SPECULAROCCLUSIONMAP
    #pragma shader_feature _EMISSIVE_COLOR_MAP
    #pragma shader_feature _HEIGHTMAP
    #pragma shader_feature _TANGENTMAP
    #pragma shader_feature _ANISOTROPYMAP
    #pragma shader_feature _DETAIL_MAP
    #pragma shader_feature _SUBSURFACE_RADIUS_MAP
    #pragma shader_feature _THICKNESSMAP
    #pragma shader_feature _SPECULARCOLORMAP
    #pragma shader_feature _VERTEX_WIND

    #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
    #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED
    #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
    // enable dithering LOD crossfade
    #pragma multi_compile _ LOD_FADE_CROSSFADE
    // TODO: We should have this keyword only if VelocityInGBuffer is enable, how to do that ?
    //#pragma multi_compile VELOCITYOUTPUT_OFF VELOCITYOUTPUT_ON

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
    #define TESSELLATION_ON
    // Use surface gradient normal mapping as it handle correctly triplanar normal mapping and multiple UVSet
    #define SURFACE_GRADIENT

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "../../../ShaderLibrary/common.hlsl"
    #include "../../../ShaderLibrary/Wind.hlsl"
    #include "../../../ShaderLibrary/tessellation.hlsl"
    #include "../../ShaderConfig.cs.hlsl"
    #include "../../ShaderVariables.hlsl"
    #include "../../ShaderPass/FragInputs.hlsl"
    #include "../../ShaderPass/ShaderPass.cs.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "../../Material/Lit/LitProperties.hlsl"

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300

        Pass
        {
            Name "GBuffer"  // Name is not used
            Tags { "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

            Cull [_CullMode]

            Stencil
            {
                Ref  [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_GBUFFER
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassGBuffer.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "GBufferDebugDisplay"  // Name is not used
            Tags{ "LightMode" = "GBufferDebugDisplay" } // This will be only for opaque object based on the RenderQueue index

            Cull [_CullMode]

            Stencil
            {
                Ref  [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define DEBUG_DISPLAY
            #define SHADERPASS SHADERPASS_GBUFFER
            #include "../../Debug/DebugDisplay.hlsl"
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassGBuffer.hlsl"

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

            // No tessellation for Meta pass
            #undef TESSELLATION_ON

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitMetaPass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassLightTransport.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }

            Cull[_CullMode]

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitDepthPass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{ "LightMode" = "DepthOnly" }

            Cull[_CullMode]

            ZWrite On

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitDepthPass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Motion Vectors"
            Tags{ "LightMode" = "MotionVectors" } // Caution, this need to be call like this to setup the correct parameters by C++ (legacy Unity)

            Cull[_CullMode]

            ZWrite Off // TODO: Test Z equal here.

            HLSLPROGRAM

            // TODO: Tesselation can't work with velocity for now...
            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_VELOCITY
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitVelocityPass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassVelocity.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Distortion" // Name is not used
            Tags { "LightMode" = "DistortionVectors" } // This will be only for transparent object based on the RenderQueue index

            Blend One One
            ZTest [_ZTestMode]
            ZWrite off
            Cull [_CullMode]

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_DISTORTION
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitDistortionPass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassDistortion.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Forward" // Name is not used
            Tags { "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_CullMode]

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_FORWARD
            #include "../../Lighting/Forward.hlsl"
            // TEMP until pragma work in include
            #pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS

            #include "../../Lighting/Lighting.hlsl"
            #include "ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ForwardDisplayDebug" // Name is not used
            Tags{ "LightMode" = "ForwardDisplayDebug" } // This will be only for transparent object based on the RenderQueue index

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_CullMode]

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define DEBUG_DISPLAY
            #define SHADERPASS SHADERPASS_FORWARD
            #include "../../Debug/DebugDisplay.hlsl"
            #include "../../Lighting/Forward.hlsl"
            // TEMP until pragma work in include
            #pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS

            #include "../../Lighting/Lighting.hlsl"
            #include "ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "Experimental.Rendering.HDPipeline.LitGUI"
}
