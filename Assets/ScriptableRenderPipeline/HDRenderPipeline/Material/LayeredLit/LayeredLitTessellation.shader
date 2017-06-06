Shader "HDRenderPipeline/LayeredLitTessellation"
{
    Properties
    {
        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

        // All the following properties are filled by the referenced lit shader.

        // Reminder. Color here are in linear but the UI (color picker) do the conversion sRGB to linear
        _BaseColor0("BaseColor0", Color) = (1, 1, 1, 1)
        _BaseColor1("BaseColor1", Color) = (1, 1, 1, 1)
        _BaseColor2("BaseColor2", Color) = (1, 1, 1, 1)
        _BaseColor3("BaseColor3", Color) = (1, 1, 1, 1)

        _BaseColorMap0("BaseColorMap0", 2D) = "white" {}
        _BaseColorMap1("BaseColorMap1", 2D) = "white" {}
        _BaseColorMap2("BaseColorMap2", 2D) = "white" {}
        _BaseColorMap3("BaseColorMap3", 2D) = "white" {}

        _Metallic0("Metallic0", Range(0.0, 1.0)) = 0
        _Metallic1("Metallic1", Range(0.0, 1.0)) = 0
        _Metallic2("Metallic2", Range(0.0, 1.0)) = 0
        _Metallic3("Metallic3", Range(0.0, 1.0)) = 0

        _Smoothness0("Smoothness0", Range(0.0, 1.0)) = 1.0
        _Smoothness1("Smoothness1", Range(0.0, 1.0)) = 1.0
        _Smoothness2("Smoothness2", Range(0.0, 1.0)) = 1.0
        _Smoothness3("Smoothness3", Range(0.0, 1.0)) = 1.0

        _MaskMap0("MaskMap0", 2D) = "white" {}
        _MaskMap1("MaskMap1", 2D) = "white" {}
        _MaskMap2("MaskMap2", 2D) = "white" {}
        _MaskMap3("MaskMap3", 2D) = "white" {}

        _SpecularOcclusionMap0("SpecularOcclusion0", 2D) = "white" {}
        _SpecularOcclusionMap1("SpecularOcclusion1", 2D) = "white" {}
        _SpecularOcclusionMap2("SpecularOcclusion2", 2D) = "white" {}
        _SpecularOcclusionMap3("SpecularOcclusion3", 2D) = "white" {}

        _NormalMap0("NormalMap0", 2D) = "bump" {}
        _NormalMap1("NormalMap1", 2D) = "bump" {}
        _NormalMap2("NormalMap2", 2D) = "bump" {}
        _NormalMap3("NormalMap3", 2D) = "bump" {}

        _NormalMapOS0("NormalMapOS0", 2D) = "white" {}
        _NormalMapOS1("NormalMapOS1", 2D) = "white" {}
        _NormalMapOS2("NormalMapOS2", 2D) = "white" {}
        _NormalMapOS3("NormalMapOS3", 2D) = "white" {}

        _NormalScale0("_NormalScale0", Range(0.0, 2.0)) = 1
        _NormalScale1("_NormalScale1", Range(0.0, 2.0)) = 1
        _NormalScale2("_NormalScale2", Range(0.0, 2.0)) = 1
        _NormalScale3("_NormalScale3", Range(0.0, 2.0)) = 1

        _HeightMap0("HeightMap0", 2D) = "black" {}
        _HeightMap1("HeightMap1", 2D) = "black" {}
        _HeightMap2("HeightMap2", 2D) = "black" {}
        _HeightMap3("HeightMap3", 2D) = "black" {}

        _HeightAmplitude0("Height Scale0", Float) = 1
        _HeightAmplitude1("Height Scale1", Float) = 1
        _HeightAmplitude2("Height Scale2", Float) = 1
        _HeightAmplitude3("Height Scale3", Float) = 1

        _HeightCenter0("Height Bias0", Float) = 0
        _HeightCenter1("Height Bias1", Float) = 0
        _HeightCenter2("Height Bias2", Float) = 0
        _HeightCenter3("Height Bias3", Float) = 0

        _DetailMap0("DetailMap0", 2D) = "black" {}
        _DetailMap1("DetailMap1", 2D) = "black" {}
        _DetailMap2("DetailMap2", 2D) = "black" {}
        _DetailMap3("DetailMap3", 2D) = "black" {}

        _DetailMask0("DetailMask0", 2D) = "white" {}
        _DetailMask1("DetailMask1", 2D) = "white" {}
        _DetailMask2("DetailMask2", 2D) = "white" {}
        _DetailMask3("DetailMask3", 2D) = "white" {}

        _DetailAlbedoScale0("_DetailAlbedoScale0", Range(-2.0, 2.0)) = 1
        _DetailAlbedoScale1("_DetailAlbedoScale1", Range(-2.0, 2.0)) = 1
        _DetailAlbedoScale2("_DetailAlbedoScale2", Range(-2.0, 2.0)) = 1
        _DetailAlbedoScale3("_DetailAlbedoScale3", Range(-2.0, 2.0)) = 1

        _DetailNormalScale0("_DetailNormalScale0", Range(0.0, 2.0)) = 1
        _DetailNormalScale1("_DetailNormalScale1", Range(0.0, 2.0)) = 1
        _DetailNormalScale2("_DetailNormalScale2", Range(0.0, 2.0)) = 1
        _DetailNormalScale3("_DetailNormalScale3", Range(0.0, 2.0)) = 1

        _DetailSmoothnessScale0("_DetailSmoothnessScale0", Range(-2.0, 2.0)) = 1
        _DetailSmoothnessScale1("_DetailSmoothnessScale1", Range(-2.0, 2.0)) = 1
        _DetailSmoothnessScale2("_DetailSmoothnessScale2", Range(-2.0, 2.0)) = 1
        _DetailSmoothnessScale3("_DetailSmoothnessScale3", Range(-2.0, 2.0)) = 1

        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace0("NormalMap space", Float) = 0
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace1("NormalMap space", Float) = 0
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace2("NormalMap space", Float) = 0
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace3("NormalMap space", Float) = 0

        // All the following properties exist only in layered lit material

        // Layer blending options
        _LayerMaskMap("LayerMaskMap", 2D) = "white" {}
        [ToggleOff] _UseHeightBasedBlend("UseHeightBasedBlend", Float) = 0.0
        // Layer blending options V2
        [ToggleOff] _UseDensityMode("Use Density mode", Float) = 0.0
        [ToggleOff] _UseMainLayerInfluence("UseMainLayerInfluence", Float) = 0.0

        _HeightFactor0("_HeightFactor0", Float) = 1
        _HeightFactor1("_HeightFactor1", Float) = 1
        _HeightFactor2("_HeightFactor2", Float) = 1
        _HeightFactor3("_HeightFactor3", Float) = 1

        // Store result of combination of _HeightFactor and _HeightAmplitude0
        [HideInInspector] _LayerHeightAmplitude0("_LayerHeightAmplitude0", Float) = 1
        [HideInInspector] _LayerHeightAmplitude1("_LayerHeightAmplitude1", Float) = 1
        [HideInInspector] _LayerHeightAmplitude2("_LayerHeightAmplitude2", Float) = 1
        [HideInInspector] _LayerHeightAmplitude3("_LayerHeightAmplitude3", Float) = 1

        _HeightCenterOffset0("_HeightCenterOffset0", Float) = 0.0
        _HeightCenterOffset1("_HeightCenterOffset1", Float) = 0.0
        _HeightCenterOffset2("_HeightCenterOffset2", Float) = 0.0
        _HeightCenterOffset3("_HeightCenterOffset3", Float) = 0.0

        // Store result of combination of _HeightCenterOffset0 and _HeightCenter0
        [HideInInspector] _LayerCenterOffset0("_LayerCenterOffset0", Float) = 0.0
        [HideInInspector] _LayerCenterOffset1("_LayerCenterOffset1", Float) = 0.0
        [HideInInspector] _LayerCenterOffset2("_LayerCenterOffset2", Float) = 0.0
        [HideInInspector] _LayerCenterOffset3("_LayerCenterOffset3", Float) = 0.0

        _BlendUsingHeight1("_BlendUsingHeight1", Float) = 0.0
        _BlendUsingHeight2("_BlendUsingHeight2", Float) = 0.0
        _BlendUsingHeight3("_BlendUsingHeight3", Float) = 0.0

        _InheritBaseNormal1("_InheritBaseNormal1", Range(0, 1.0)) = 0.0
        _InheritBaseNormal2("_InheritBaseNormal2", Range(0, 1.0)) = 0.0
        _InheritBaseNormal3("_InheritBaseNormal3", Range(0, 1.0)) = 0.0

        _InheritBaseHeight1("_InheritBaseHeight1", Range(0, 1.0)) = 0.0
        _InheritBaseHeight2("_InheritBaseHeight2", Range(0, 1.0)) = 0.0
        _InheritBaseHeight3("_InheritBaseHeight3", Range(0, 1.0)) = 0.0

        _InheritBaseColor1("_InheritBaseColor1", Range(0, 1.0)) = 0.0
        _InheritBaseColor2("_InheritBaseColor2", Range(0, 1.0)) = 0.0
        _InheritBaseColor3("_InheritBaseColor3", Range(0, 1.0)) = 0.0

        _InheritBaseColorThreshold1("_InheritBaseColorThreshold1", Range(0, 1.0)) = 1.0
        _InheritBaseColorThreshold2("_InheritBaseColorThreshold2", Range(0, 1.0)) = 1.0
        _InheritBaseColorThreshold3("_InheritBaseColorThreshold3", Range(0, 1.0)) = 1.0

        _MinimumOpacity0("_MinimumOpacity0", Range(0, 1.0)) = 1.0
        _MinimumOpacity1("_MinimumOpacity1", Range(0, 1.0)) = 1.0
        _MinimumOpacity2("_MinimumOpacity2", Range(0, 1.0)) = 1.0
        _MinimumOpacity3("_MinimumOpacity3", Range(0, 1.0)) = 1.0

        _OpacityAsDensity0("_OpacityAsDensity0", Range(0, 1.0)) = 0.0
        _OpacityAsDensity1("_OpacityAsDensity1", Range(0, 1.0)) = 0.0
        _OpacityAsDensity2("_OpacityAsDensity2", Range(0, 1.0)) = 0.0
        _OpacityAsDensity3("_OpacityAsDensity3", Range(0, 1.0)) = 0.0

        _LayerTilingBlendMask("_LayerTilingBlendMask", Float) = 1
        _LayerTiling0("_LayerTiling0", Float) = 1
        _LayerTiling1("_LayerTiling1", Float) = 1
        _LayerTiling2("_LayerTiling2", Float) = 1
        _LayerTiling3("_LayerTiling3", Float) = 1

        [HideInInspector] _LayerCount("_LayerCount", Float) = 2.0

        [Enum(None, 0, Multiply, 1, Add, 2)] _VertexColorMode("Vertex color mode", Float) = 0

        [ToggleOff]  _ObjectScaleAffectTile("_ObjectScaleAffectTile", Float) = 0.0
        [Enum(UV0, 0, Planar, 4, Triplanar, 5)] _UVBlendMask("UV Set for blendMask", Float) = 0
        _TexWorldScaleBlendMask("Tiling", Float) = 1.0

        // Following are builtin properties

        _DistortionVectorMap("DistortionVectorMap", 2D) = "black" {}

        // Wind
        [ToggleOff]  _EnableWind("Enable Wind", Float) = 0.0
        _InitialBend("Initial Bend", float) = 1.0
        _Stiffness("Stiffness", float) = 1.0
        _Drag("Drag", float) = 1.0
        _ShiverDrag("Shiver Drag", float) = 0.2
        _ShiverDirectionality("Shiver Directionality", Range(0.0, 1.0)) = 0.5

        _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        _EmissiveIntensity("EmissiveIntensity", Float) = 0

        [ToggleOff] _DistortionEnable("Enable Distortion", Float) = 0.0
        [ToggleOff] _DistortionOnly("Distortion Only", Float) = 0.0
        [ToggleOff] _DistortionDepthTest("Distortion Depth Test Enable", Float) = 0.0
        [ToggleOff] _DepthOffsetEnable("Depth Offset View space", Float) = 0.0

        [ToggleOff] _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0

        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _HorizonFade("Horizon fade", Range(0.0, 5.0)) = 1.0

        // Stencil state
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2 // StencilLightingUsage.RegularLighting (fixed at compile time)

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode ("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _ZTestMode("_ZTestMode", Int) = 8

        [ToggleOff] _DoubleSidedEnable("Double sided enable", Float) = 0.0
        [Enum(None, 0, Mirror, 1, Flip, 2)] _DoubleSidedNormalMode("Double sided normal mode", Float) = 1
        [HideInInspector] _DoubleSidedConstants("_DoubleSidedConstants", Vector) = (1, 1, -1, 0)

        [ToggleOff]  _EnablePerPixelDisplacement("Enable per pixel displacement", Float) = 0.0
        _PPDMinSamples("Min sample for POM", Range(1.0, 64.0)) = 5
        _PPDMaxSamples("Max sample for POM", Range(1.0, 64.0)) = 15
        _PPDLodThreshold("Start lod to fade out the POM effect", Range(0.0, 16.0)) = 5
        [Enum(Use Emissive Color, 0, Use Emissive Mask, 1)] _EmissiveColorMode("Emissive color mode", Float) = 1

        // Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
        // value that exist to identify if the GI emission need to be enabled.
        // In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
        // TODO: Fix the code in legacy unity so we can customize the beahvior for GI
        _EmissionColor("Color", Color) = (1, 1, 1)

        // WARNING
        // All the following properties that concern the UV mapping are the same as in the Lit shader.
        // This means that they will get overridden when synchronizing the various layers.
        // To avoid this, make sure that all properties here are in the exclusion list in LayeredLitUI.SynchronizeLayerProperties
        _TexWorldScale0("Tiling", Float) = 1.0
        _TexWorldScale1("Tiling", Float) = 1.0
        _TexWorldScale2("Tiling", Float) = 1.0
        _TexWorldScale3("Tiling", Float) = 1.0

        [Enum(UV0, 0, Planar, 4, Triplanar, 5)] _UVBase0("UV Set for base0", Float) = 0 // no UV1/2/3 for main layer (matching Lit.shader and for PPDisplacement restriction)
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase1("UV Set for base1", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase2("UV Set for base2", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase3("UV Set for base3", Float) = 0

        [HideInInspector] _UVMappingMask0("_UVMappingMask0", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVMappingMask1("_UVMappingMask1", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVMappingMask2("_UVMappingMask2", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVMappingMask3("_UVMappingMask3", Color) = (1, 0, 0, 0)

        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail0("UV Set for detail0", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail1("UV Set for detail1", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail2("UV Set for detail2", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail3("UV Set for detail3", Float) = 0

        [HideInInspector] _UVDetailsMappingMask0("_UVDetailsMappingMask0", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVDetailsMappingMask1("_UVDetailsMappingMask1", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVDetailsMappingMask2("_UVDetailsMappingMask2", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVDetailsMappingMask3("_UVDetailsMappingMask3", Color) = (1, 0, 0, 0)

        // Tesselation specific
        [Enum(Phong, 0, Displacement, 1, DisplacementPhong, 2)] _TessellationMode("Tessellation mode", Float) = 1
        _TessellationFactor("Tessellation Factor", Range(0.0, 15.0)) = 4.0
        _TessellationFactorMinDistance("Tessellation start fading distance", Float) = 20.0
        _TessellationFactorMaxDistance("Tessellation end fading distance", Float) = 50.0
        _TessellationFactorTriangleSize("Tessellation triangle size", Float) = 100.0
        _TessellationShapeFactor("Tessellation shape factor", Range(0.0, 1.0)) = 0.75 // Only use with Phong
        _TessellationBackFaceCullEpsilon("Tessellation back face epsilon", Range(-1.0, 0.0)) = -0.25
        [ToggleOff] _TessellationObjectScale("Tessellation object scale", Float) = 0.0
        [ToggleOff] _TessellationTilingScale("Tessellation tiling scale", Float) = 1.0
        // TODO: Handle culling mode for backface culling
    }

    HLSLINCLUDE

    #pragma target 5.0
    #pragma only_renderers d3d11 ps4 // TEMP: until we go further in dev

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _DISTORTION_ON
    #pragma shader_feature _DEPTHOFFSET_ON
    #pragma shader_feature _DOUBLESIDED_ON
    #pragma shader_feature _PER_PIXEL_DISPLACEMENT
    // Default is _TESSELLATION_PHONG
    #pragma shader_feature _ _TESSELLATION_DISPLACEMENT _TESSELLATION_DISPLACEMENT_PHONG
    #pragma shader_feature _TESSELLATION_OBJECT_SCALE
    #pragma shader_feature _TESSELLATION_TILING_SCALE

    #pragma shader_feature _LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR_BLENDMASK _LAYER_MAPPING_TRIPLANAR_BLENDMASK
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR0 _LAYER_MAPPING_TRIPLANAR0
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR1 _LAYER_MAPPING_TRIPLANAR1
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR2 _LAYER_MAPPING_TRIPLANAR2
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR3 _LAYER_MAPPING_TRIPLANAR3
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE0
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE1
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE2
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE3
    #pragma shader_feature _ _REQUIRE_UV2 _REQUIRE_UV3
    #pragma shader_feature _EMISSIVE_COLOR

    #pragma shader_feature _NORMALMAP0
    #pragma shader_feature _NORMALMAP1
    #pragma shader_feature _NORMALMAP2
    #pragma shader_feature _NORMALMAP3
    #pragma shader_feature _MASKMAP0
    #pragma shader_feature _MASKMAP1
    #pragma shader_feature _MASKMAP2
    #pragma shader_feature _MASKMAP3
    #pragma shader_feature _SPECULAROCCLUSIONMAP0
    #pragma shader_feature _SPECULAROCCLUSIONMAP1
    #pragma shader_feature _SPECULAROCCLUSIONMAP2
    #pragma shader_feature _SPECULAROCCLUSIONMAP3
    #pragma shader_feature _EMISSIVE_COLOR_MAP
    #pragma shader_feature _HEIGHTMAP0
    #pragma shader_feature _HEIGHTMAP1
    #pragma shader_feature _HEIGHTMAP2
    #pragma shader_feature _HEIGHTMAP3
    #pragma shader_feature _DETAIL_MAP0
    #pragma shader_feature _DETAIL_MAP1
    #pragma shader_feature _DETAIL_MAP2
    #pragma shader_feature _DETAIL_MAP3
    #pragma shader_feature _ _LAYER_MASK_VERTEX_COLOR_MUL _LAYER_MASK_VERTEX_COLOR_ADD
    #pragma shader_feature _MAIN_LAYER_INFLUENCE_MODE
    #pragma shader_feature _DENSITY_MODE
    #pragma shader_feature _HEIGHT_BASED_BLEND
    #pragma shader_feature _ _LAYEREDLIT_3_LAYERS _LAYEREDLIT_4_LAYERS
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

    #define _MAX_LAYER 4

    #if defined(_LAYEREDLIT_4_LAYERS)
    #   define _LAYER_COUNT 4
    #elif defined(_LAYEREDLIT_3_LAYERS)
    #   define _LAYER_COUNT 3
    #else
    #   define _LAYER_COUNT 2
    #endif

    // Explicitely said that we are a layered shader as we share code between lit and layered lit
    #define LAYERED_LIT_SHADER

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
            #include "../Lit/ShaderPass/LitSharePass.hlsl"
            #include "../Lit/LitData.hlsl"
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
            #include "../Lit/ShaderPass/LitSharePass.hlsl"
            #include "../Lit/LitData.hlsl"
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
            #include "../Lit/ShaderPass/LitMetaPass.hlsl"
            #include "../Lit/LitData.hlsl"
            #include "../../ShaderPass/ShaderPassLightTransport.hlsl"

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
            #include "../Lit/ShaderPass/LitVelocityPass.hlsl"
            #include "../Lit/LitData.hlsl"
            #include "../../ShaderPass/ShaderPassVelocity.hlsl"

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
            #include "../Lit/ShaderPass/LitDepthPass.hlsl"
            #include "../Lit/LitData.hlsl"
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
            #include "../Lit/ShaderPass/LitDepthPass.hlsl"
            #include "../Lit/LitData.hlsl"
            #include "../../ShaderPass/ShaderPassDepthOnly.hlsl"

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
            #include "../Lit/ShaderPass/LitDistortionPass.hlsl"
            #include "../Lit/LitData.hlsl"
            #include "../../ShaderPass/ShaderPassDistortion.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Forward" // Name is not used
            Tags{ "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_CullMode]

            HLSLPROGRAM

            #pragma hull Hull
            #pragma domain Domain

            #define SHADERPASS SHADERPASS_FORWARD
            #include "../../Lighting/Forward.hlsl"
            // TEMP until pragma work in include
            #pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS

            #include "../../Lighting/Lighting.hlsl"
            #include "../Lit/ShaderPass/LitSharePass.hlsl"
            #include "../Lit/LitData.hlsl"
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
            #include "../Lit/ShaderPass/LitSharePass.hlsl"
            #include "../Lit/LitData.hlsl"
            #include "../../ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "Experimental.Rendering.HDPipeline.LayeredLitGUI"
}
