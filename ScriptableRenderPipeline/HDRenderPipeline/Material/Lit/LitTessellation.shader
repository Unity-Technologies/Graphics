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
        _SmoothnessRemapMin("SmoothnessRemapMin", Float) = 0.0
        _SmoothnessRemapMax("SmoothnessRemapMax", Float) = 1.0
        _AORemapMin("AORemapMin", Float) = 0.0
        _AORemapMax("AORemapMax", Float) = 1.0

        _NormalMap("NormalMap", 2D) = "bump" {}     // Tangent space normal map
        _NormalMapOS("NormalMapOS", 2D) = "white" {} // Object space normal map - no good default value
        _NormalScale("_NormalScale", Range(0.0, 2.0)) = 1

        _BentNormalMap("_BentNormalMap", 2D) = "bump" {}
        _BentNormalMapOS("_BentNormalMapOS", 2D) = "white" {}

        _HeightMap("HeightMap", 2D) = "black" {}
        // Caution: Default value of _HeightAmplitude must be (_HeightMax - _HeightMin) * 0.01
        [HideInInspector] _HeightAmplitude("Height Amplitude", Float) = 0.02 // In world units
        _HeightCenter("Height Center", Range(0.0, 1.0)) = 0.5 // In texture space
        _HeightMin("Heightmap Min", Float) = -1
        _HeightMax("Heightmap Max", Float) = 1

        _DetailMap("DetailMap", 2D) = "black" {}
        _DetailAlbedoScale("_DetailAlbedoScale", Range(0.0, 2.0)) = 1
        _DetailNormalScale("_DetailNormalScale", Range(0.0, 2.0)) = 1
        _DetailSmoothnessScale("_DetailSmoothnessScale", Range(0.0, 2.0)) = 1

        _TangentMap("TangentMap", 2D) = "bump" {}
        _TangentMapOS("TangentMapOS", 2D) = "white" {}
        _Anisotropy("Anisotropy", Range(-1.0, 1.0)) = 0
        _AnisotropyMap("AnisotropyMap", 2D) = "white" {}

        _SubsurfaceProfile("Subsurface Profile", Int) = 0
        _SubsurfaceRadius("Subsurface Radius", Range(0.0, 1.0)) = 1.0
        _SubsurfaceRadiusMap("Subsurface Radius Map", 2D) = "white" {}
        _Thickness("Thickness", Range(0.0, 1.0)) = 1.0
        _ThicknessMap("Thickness Map", 2D) = "white" {}
        _ThicknessRemap("Thickness Remap", Vector) = (0, 1, 0, 0)

        _CoatCoverage("Coat Coverage", Range(0.0, 1.0)) = 1.0
        _CoatIOR("Coat IOR", Range(0.0, 1.0)) = 0.5

        _SpecularColor("SpecularColor", Color) = (1, 1, 1, 1)
        _SpecularColorMap("SpecularColorMap", 2D) = "white" {}

        // Following options are for the GUI inspector and different from the input parameters above
        // These option below will cause different compilation flag.
        [ToggleOff]  _EnableSpecularOcclusion("Enable specular occlusion", Float) = 0.0

        _EmissiveColor("EmissiveColor", Color) = (1, 1, 1)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        _EmissiveIntensity("EmissiveIntensity", Float) = 0
        [ToggleOff] _AlbedoAffectEmissive("Albedo Affect Emissive", Float) = 0.0

        _DistortionVectorMap("DistortionVectorMap", 2D) = "black" {}
        [ToggleOff] _DistortionEnable("Enable Distortion", Float) = 0.0
        [ToggleOff] _DistortionOnly("Distortion Only", Float) = 0.0
        [ToggleOff] _DistortionDepthTest("Distortion Depth Test Enable", Float) = 1.0
        [Enum(Add, 0, Multiply, 1)] _DistortionBlendMode("Distortion Blend Mode", Int) = 0
        [HideInInspector] _DistortionSrcBlend("Distortion Blend Src", Int) = 0
        [HideInInspector] _DistortionDstBlend("Distortion Blend Dst", Int) = 0
        [HideInInspector] _DistortionBlurSrcBlend("Distortion Blur Blend Src", Int) = 0
        [HideInInspector] _DistortionBlurDstBlend("Distortion Blur Blend Dst", Int) = 0
        [HideInInspector] _DistortionBlurBlendMode("Distortion Blur Blend Mode", Int) = 0
        _DistortionScale("Distortion Scale", Float) = 1
        _DistortionVectorScale("Distortion Vector Scale", Float) = 2
        _DistortionVectorBias("Distortion Vector Bias", Float) = -1
        _DistortionBlurScale("Distortion Blur Scale", Float) = 1
        _DistortionBlurRemapMin("DistortionBlurRemapMin", Float) = 0.0
        _DistortionBlurRemapMax("DistortionBlurRemapMax", Float) = 1.0

        [ToggleOff]  _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _AlphaCutoffPrepass("_AlphaCutoffPrepass", Range(0.0, 1.0)) = 0.5
        _AlphaCutoffPostpass("_AlphaCutoffPostpass", Range(0.0, 1.0)) = 0.5
        [ToggleOff] _TransparentDepthPrepassEnable("_TransparentDepthPrepassEnable", Float) = 0.0
        [ToggleOff] _TransparentBackfaceEnable("_TransparentBackfaceEnable", Float) = 0.0
        [ToggleOff] _TransparentDepthPostpassEnable("_TransparentDepthPostpassEnable", Float) = 0.0

        // Transparency
        [Enum(None, 0, Plane, 1, Sphere, 2)]_RefractionMode("Refraction Mode", Int) = 0
        _IOR("Indice Of Refraction", Range(1.0, 2.5)) = 1.0
        _ThicknessMultiplier("Thickness Multiplier", Float) = 1.0
        _TransmittanceColor("Transmittance Color", Color) = (1.0, 1.0, 1.0)
        _TransmittanceColorMap("TransmittanceColorMap", 2D) = "white" {}
        _ATDistance("Transmittance Absorption Distance", Float) = 1.0
        [ToggleOff] _PreRefractionPass("PreRefractionPass", Float) = 0.0

        // Stencil state
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2 // StencilLightingUsage.RegularLighting  (fixed at compile time)

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _CullModeForward("__cullmodeForward", Float) = 2.0 // This mode is dedicated to Forward to correctly handle backface then front face rendering thin transparent
        [HideInInspector] _ZTestMode("_ZTestMode", Int) = 8

        [ToggleOff] _EnableFogOnTransparent("Enable Fog", Float) = 1.0
        [ToggleOff] _EnableBlendModePreserveSpecularLighting("Enable Blend Mode Preserve Specular Lighting", Float) = 1.0

        [ToggleOff] _DoubleSidedEnable("Double sided enable", Float) = 0.0
        [Enum(None, 0, Mirror, 1, Flip, 2)] _DoubleSidedNormalMode("Double sided normal mode", Float) = 1
        [HideInInspector] _DoubleSidedConstants("_DoubleSidedConstants", Vector) = (1, 1, -1, 0)

        [Enum(UV0, 0, Planar, 4, TriPlanar, 5)] _UVBase("UV Set for base", Float) = 0
        _TexWorldScale("Scale to apply on world coordinate", Float) = 1.0
        [HideInInspector] _InvTilingScale("Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y))", Float) = 1
        [HideInInspector] _UVMappingMask("_UVMappingMask", Color) = (1, 0, 0, 0)
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace("NormalMap space", Float) = 0

        [Enum(Subsurface Scattering, 0, Standard, 1, Anisotropy, 2, ClearCoat, 3, Specular Color, 4)] _MaterialID("MaterialId", Int) = 1 // MaterialId.RegularLighting

        [Enum(None, 0, Vertex displacement, 1, Pixel displacement, 2, Tessellation displacement, 3)] _DisplacementMode("DisplacementMode", Int) = 0
        [ToggleOff] _DisplacementLockObjectScale("displacement lock object scale", Float) = 1.0
        [ToggleOff] _DisplacementLockTilingScale("displacement lock tiling scale", Float) = 1.0
        [ToggleOff] _DepthOffsetEnable("Depth Offset View space", Float) = 0.0

        _PPDMinSamples("Min sample for POM", Range(1.0, 64.0)) = 5
        _PPDMaxSamples("Max sample for POM", Range(1.0, 64.0)) = 15
        _PPDLodThreshold("Start lod to fade out the POM effect", Range(0.0, 16.0)) = 5
        _PPDPrimitiveLength("Primitive length for POM", Float) = 1
        _PPDPrimitiveWidth("Primitive width for POM", Float) = 1
        [HideInInspector] _InvPrimScale("Inverse primitive scale for non-planar POM", Vector) = (1, 1, 0, 0)

        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail("UV Set for detail", Float) = 0
        [HideInInspector] _UVDetailsMappingMask("_UVDetailsMappingMask", Color) = (1, 0, 0, 0)
        [ToggleOff] _LinkDetailsWithBase("LinkDetailsWithBase", Float) = 1.0
        [Enum(Use Emissive Color, 0, Use Emissive Mask, 1)] _EmissiveColorMode("Emissive color mode", Float) = 1

        // Wind
        [ToggleOff]  _EnableWind("Enable Wind", Float) = 0.0
        _InitialBend("Initial Bend", float) = 1.0
        _Stiffness("Stiffness", float) = 1.0
        _Drag("Drag", float) = 1.0
        _ShiverDrag("Shiver Drag", float) = 0.2
        _ShiverDirectionality("Shiver Directionality", Range(0.0, 1.0)) = 0.5

        // Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
        // value that exist to identify if the GI emission need to be enabled.
        // In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
        // TODO: Fix the code in legacy unity so we can customize the beahvior for GI
        _EmissionColor("Color", Color) = (1, 1, 1)

        // Tessellation specific
        [Enum(None, 0, Phong, 1)] _TessellationMode("Tessellation mode", Float) = 0
        _TessellationFactor("Tessellation Factor", Range(0.0, 15.0)) = 4.0
        _TessellationFactorMinDistance("Tessellation start fading distance", Float) = 20.0
        _TessellationFactorMaxDistance("Tessellation end fading distance", Float) = 50.0
        _TessellationFactorTriangleSize("Tessellation triangle size", Float) = 100.0
        _TessellationShapeFactor("Tessellation shape factor", Range(0.0, 1.0)) = 0.75 // Only use with Phong
        _TessellationBackFaceCullEpsilon("Tessellation back face epsilon", Range(-1.0, 0.0)) = -0.25
         // TODO: Handle culling mode for backface culling

        // HACK: GI Baking system relies on some properties existing in the shader ("_MainTex", "_Cutoff" and "_Color") for opacity handling, so we need to store our version of those parameters in the hard-coded name the GI baking system recognizes.
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    HLSLINCLUDE

    #pragma target 5.0
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _DEPTHOFFSET_ON
    #pragma shader_feature _DOUBLESIDED_ON
    #pragma shader_feature _ _VERTEX_DISPLACEMENT _PIXEL_DISPLACEMENT _TESSELLATION_DISPLACEMENT
    #pragma shader_feature _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
    #pragma shader_feature _DISPLACEMENT_LOCK_TILING_SCALE
    #pragma shader_feature _PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE
    #pragma shader_feature _VERTEX_WIND
    #pragma shader_feature _ _TESSELLATION_PHONG
    #pragma shader_feature _ _REFRACTION_PLANE _REFRACTION_SPHERE

    #pragma shader_feature _ _MAPPING_PLANAR _MAPPING_TRIPLANAR
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE
    #pragma shader_feature _ _REQUIRE_UV2 _REQUIRE_UV3

    #pragma shader_feature _NORMALMAP
    #pragma shader_feature _MASKMAP
    #pragma shader_feature _SPECULAROCCLUSIONMAP
    #pragma shader_feature _EMISSIVE_COLOR_MAP
    #pragma shader_feature _ENABLESPECULAROCCLUSION
    #pragma shader_feature _HEIGHTMAP
    #pragma shader_feature _TANGENTMAP
    #pragma shader_feature _ANISOTROPYMAP
    #pragma shader_feature _DETAIL_MAP
    #pragma shader_feature _SUBSURFACE_RADIUS_MAP
    #pragma shader_feature _THICKNESSMAP
    #pragma shader_feature _SPECULARCOLORMAP
    #pragma shader_feature _TRANSMITTANCECOLORMAP

    // Keyword for transparent
    #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
    #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
    #pragma shader_feature _BLENDMODE_PRESERVE_SPECULAR_LIGHTING
    #pragma shader_feature _ENABLE_FOG_ON_TRANSPARENT

    // MaterialId are used as shader feature to allow compiler to optimize properly
    // Note _MATID_STANDARD is not define as there is always the default case "_". We assign default as _MATID_STANDARD, so we never test _MATID_STANDARD
    #pragma shader_feature _ _MATID_SSS _MATID_ANISO _MATID_SPECULAR _MATID_CLEARCOAT

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
    // This shader support vertex modification
    #define HAVE_VERTEX_MODIFICATION
    #define HAVE_TESSELLATION_MODIFICATION

    // If we use subsurface scattering, enable output split lighting (for forward pass)
    #if defined(_MATID_SSS) && !defined(_SURFACE_TYPE_TRANSPARENT)
    #define OUTPUT_SPLIT_LIGHTING
    #endif

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "ShaderLibrary/common.hlsl"
    #include "ShaderLibrary/Wind.hlsl"
    #include "ShaderLibrary/GeometricTools.hlsl"
    #include "ShaderLibrary/tessellation.hlsl"
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

            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            #define SHADERPASS SHADERPASS_GBUFFER
            #include "../../ShaderVariables.hlsl"
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassGBuffer.hlsl"

            ENDHLSL
        }

        // This pass is the same as GBuffer only it does not do alpha test (the clip instruction is removed)
        // This is due to the fact that on GCN, any shader with a clip instruction cannot benefit from HiZ so when we do a prepass, in order to get the most performance, we need to make a special case in the subsequent GBuffer pass.
        Pass
        {
            Name "GBufferWithPrepass"  // Name is not used
            Tags { "LightMode" = "GBufferWithPrepass" } // This will be only for opaque object based on the RenderQueue index

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

            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            #define SHADERPASS SHADERPASS_GBUFFER
            #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
            #include "../../ShaderVariables.hlsl"
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

            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            #define DEBUG_DISPLAY
            #define SHADERPASS SHADERPASS_GBUFFER
            #include "../../ShaderVariables.hlsl"
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
            #include "../../ShaderVariables.hlsl"
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassLightTransport.hlsl"

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

            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_SHADOWS
            #define USE_LEGACY_UNITY_MATRIX_VARIABLES
            #include "../../ShaderVariables.hlsl"
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

            ColorMask 0

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "../../ShaderVariables.hlsl"
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
            #include "../../ShaderVariables.hlsl"
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

            Blend [_DistortionSrcBlend] [_DistortionDstBlend], [_DistortionBlurSrcBlend] [_DistortionBlurDstBlend]
            BlendOp Add, [_DistortionBlurBlendOp]
            ZTest [_ZTestMode]
            ZWrite off
            Cull [_CullMode]

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_DISTORTION
            #include "../../ShaderVariables.hlsl"
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitDistortionPass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassDistortion.hlsl"

            ENDHLSL
        }

        // Caution: Order of pass mater. It should be:
        // TransparentDepthPrepass, TransparentBackface, Forward/ForwardOnly, TransparentDepthPostpass
        Pass
        {
            Name "TransparentDepthPrepass"
            Tags{ "LightMode" = "TransparentDepthPrepass" }

            Cull[_CullMode]
            ZWrite On
            ColorMask 0

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define CUTOFF_TRANSPARENT_DEPTH_PREPASS
            #include "../../ShaderVariables.hlsl"
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitDepthPass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "TransparentBackface"
            Tags { "LightMode" = "TransparentBackface" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Front

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // #include "../../Lighting/Forward.hlsl"
            #pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS
            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #define SHADERPASS SHADERPASS_FORWARD
            #include "../../ShaderVariables.hlsl"
            #include "../../Lighting/Lighting.hlsl"
            #include "ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Forward" // Name is not used
            Tags { "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

            Stencil
            {
                Ref[_StencilRef]
                Comp Always
                Pass Replace
            }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull[_CullModeForward]

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // #include "../../Lighting/Forward.hlsl"
            #pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS
            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #define SHADERPASS SHADERPASS_FORWARD
            #include "../../ShaderVariables.hlsl"
            #include "../../Lighting/Lighting.hlsl"
            #include "ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ForwardDebugDisplay" // Name is not used
            Tags{ "LightMode" = "ForwardDebugDisplay" } // This will be only for transparent object based on the RenderQueue index

            Stencil
            {
                Ref[_StencilRef]
                Comp Always
                Pass Replace
            }

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_CullModeForward]

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // #include "../../Lighting/Forward.hlsl"
            #pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS
            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #define DEBUG_DISPLAY
            #define SHADERPASS SHADERPASS_FORWARD
            #include "../../ShaderVariables.hlsl"
            #include "../../Debug/DebugDisplay.hlsl"
            #include "../../Lighting/Lighting.hlsl"
            #include "ShaderPass/LitSharePass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "TransparentDepthPostPass"
            Tags { "LightMode" = "TransparentDepthPostPass" }

            Cull[_CullMode]
            ZWrite On
            ColorMask 0

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define CUTOFF_TRANSPARENT_DEPTH_POSTPASS
            #include "../../ShaderVariables.hlsl"
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/LitDepthPass.hlsl"
            #include "LitData.hlsl"
            #include "../../ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "Experimental.Rendering.HDPipeline.LitGUI"
}
