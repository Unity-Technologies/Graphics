Shader "HDRenderPipeline/LayeredLit"
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

        _SmoothnessRemapMin0("SmoothnessRemapMin0", Range(0.0, 1.0)) = 0.0
        _SmoothnessRemapMin1("SmoothnessRemapMin1", Range(0.0, 1.0)) = 0.0
        _SmoothnessRemapMin2("SmoothnessRemapMin2", Range(0.0, 1.0)) = 0.0
        _SmoothnessRemapMin3("SmoothnessRemapMin3", Range(0.0, 1.0)) = 0.0

        _SmoothnessRemapMax0("SmoothnessRemapMax0", Range(0.0, 1.0)) = 1.0
        _SmoothnessRemapMax1("SmoothnessRemapMax1", Range(0.0, 1.0)) = 1.0
        _SmoothnessRemapMax2("SmoothnessRemapMax2", Range(0.0, 1.0)) = 1.0
        _SmoothnessRemapMax3("SmoothnessRemapMax3", Range(0.0, 1.0)) = 1.0

        _MaskMap0("MaskMap0", 2D) = "white" {}
        _MaskMap1("MaskMap1", 2D) = "white" {}
        _MaskMap2("MaskMap2", 2D) = "white" {}
        _MaskMap3("MaskMap3", 2D) = "white" {}

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

        _BentNormalMap0("BentNormalMap0", 2D) = "bump" {}
        _BentNormalMap1("BentNormalMap1", 2D) = "bump" {}
        _BentNormalMap2("BentNormalMap2", 2D) = "bump" {}
        _BentNormalMap3("BentNormalMap3", 2D) = "bump" {}

        _BentNormalMapOS0("BentNormalMapOS0", 2D) = "white" {}
        _BentNormalMapOS1("BentNormalMapOS1", 2D) = "white" {}
        _BentNormalMapOS2("BentNormalMapOS2", 2D) = "white" {}
        _BentNormalMapOS3("BentNormalMapOS3", 2D) = "white" {}

        _HeightMap0("HeightMap0", 2D) = "black" {}
        _HeightMap1("HeightMap1", 2D) = "black" {}
        _HeightMap2("HeightMap2", 2D) = "black" {}
        _HeightMap3("HeightMap3", 2D) = "black" {}

        [HideInInspector] _HeightAmplitude0("Height Scale0", Float) = 1
        [HideInInspector] _HeightAmplitude1("Height Scale1", Float) = 1
        [HideInInspector] _HeightAmplitude2("Height Scale2", Float) = 1
        [HideInInspector] _HeightAmplitude3("Height Scale3", Float) = 1

        _HeightCenter0("Height Bias0", Range(0.0, 1.0)) = 0.5
        _HeightCenter1("Height Bias1", Range(0.0, 1.0)) = 0.5
        _HeightCenter2("Height Bias2", Range(0.0, 1.0)) = 0.5
        _HeightCenter3("Height Bias3", Range(0.0, 1.0)) = 0.5

        _HeightMin0("Height Min0", Float) = -1
        _HeightMin1("Height Min1", Float) = -1
        _HeightMin2("Height Min2", Float) = -1
        _HeightMin3("Height Min3", Float) = -1

        _HeightMax0("Height Max0", Float) = 1
        _HeightMax1("Height Max1", Float) = 1
        _HeightMax2("Height Max2", Float) = 1
        _HeightMax3("Height Max3", Float) = 1

        _DetailMap0("DetailMap0", 2D) = "black" {}
        _DetailMap1("DetailMap1", 2D) = "black" {}
        _DetailMap2("DetailMap2", 2D) = "black" {}
        _DetailMap3("DetailMap3", 2D) = "black" {}

        _DetailAlbedoScale0("_DetailAlbedoScale0", Range(0.0, 2.0)) = 1
        _DetailAlbedoScale1("_DetailAlbedoScale1", Range(0.0, 2.0)) = 1
        _DetailAlbedoScale2("_DetailAlbedoScale2", Range(0.0, 2.0)) = 1
        _DetailAlbedoScale3("_DetailAlbedoScale3", Range(0.0, 2.0)) = 1

        _DetailNormalScale0("_DetailNormalScale0", Range(0.0, 2.0)) = 1
        _DetailNormalScale1("_DetailNormalScale1", Range(0.0, 2.0)) = 1
        _DetailNormalScale2("_DetailNormalScale2", Range(0.0, 2.0)) = 1
        _DetailNormalScale3("_DetailNormalScale3", Range(0.0, 2.0)) = 1

        _DetailSmoothnessScale0("_DetailSmoothnessScale0", Range(0.0, 2.0)) = 1
        _DetailSmoothnessScale1("_DetailSmoothnessScale1", Range(0.0, 2.0)) = 1
        _DetailSmoothnessScale2("_DetailSmoothnessScale2", Range(0.0, 2.0)) = 1
        _DetailSmoothnessScale3("_DetailSmoothnessScale3", Range(0.0, 2.0)) = 1

        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace0("NormalMap space", Float) = 0
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace1("NormalMap space", Float) = 0
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace2("NormalMap space", Float) = 0
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace3("NormalMap space", Float) = 0

        // All the following properties exist only in layered lit material

        // Layer blending options
        _LayerMaskMap("LayerMaskMap", 2D) = "white" {}
        _LayerInfluenceMaskMap("LayerInfluenceMaskMap", 2D) = "white" {}
        [ToggleOff] _UseHeightBasedBlend("UseHeightBasedBlend", Float) = 0.0

        _HeightOffset0("Height Offset0", Float) = 0
        _HeightOffset1("Height Offset1", Float) = 0
        _HeightOffset2("Height Offset2", Float) = 0
        _HeightOffset3("Height Offset3", Float) = 0

        _HeightTransition("Height Transition", Range(0, 1.0)) = 0.0

        [ToggleOff] _UseDensityMode("Use Density mode", Float) = 0.0
        [ToggleOff] _UseMainLayerInfluence("UseMainLayerInfluence", Float) = 0.0

        _InheritBaseNormal1("_InheritBaseNormal1", Range(0, 1.0)) = 0.0
        _InheritBaseNormal2("_InheritBaseNormal2", Range(0, 1.0)) = 0.0
        _InheritBaseNormal3("_InheritBaseNormal3", Range(0, 1.0)) = 0.0

        _InheritBaseHeight1("_InheritBaseHeight1", Range(0, 1.0)) = 0.0
        _InheritBaseHeight2("_InheritBaseHeight2", Range(0, 1.0)) = 0.0
        _InheritBaseHeight3("_InheritBaseHeight3", Range(0, 1.0)) = 0.0

        _InheritBaseColor1("_InheritBaseColor1", Range(0, 1.0)) = 0.0
        _InheritBaseColor2("_InheritBaseColor2", Range(0, 1.0)) = 0.0
        _InheritBaseColor3("_InheritBaseColor3", Range(0, 1.0)) = 0.0

        [ToggleOff] _OpacityAsDensity0("_OpacityAsDensity0", Float) = 0.0
        [ToggleOff] _OpacityAsDensity1("_OpacityAsDensity1", Float) = 0.0
        [ToggleOff] _OpacityAsDensity2("_OpacityAsDensity2", Float) = 0.0
        [ToggleOff] _OpacityAsDensity3("_OpacityAsDensity3", Float) = 0.0

        [HideInInspector] _LayerCount("_LayerCount", Float) = 2.0

        [Enum(None, 0, Multiply, 1, Add, 2)] _VertexColorMode("Vertex color mode", Float) = 0

        [ToggleOff]  _ObjectScaleAffectTile("_ObjectScaleAffectTile", Float) = 0.0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBlendMask("UV Set for blendMask", Float) = 0
        [HideInInspector] _UVMappingMaskBlendMask("_UVMappingMaskBlendMask", Color) = (1, 0, 0, 0)
        _TexWorldScaleBlendMask("Tiling", Float) = 1.0

        // Following are builtin properties

        _DistortionVectorMap("DistortionVectorMap", 2D) = "black" {}

        [ToggleOff]  _EnableSpecularOcclusion("Enable specular occlusion", Float) = 0.0

        _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        _EmissiveIntensity("EmissiveIntensity", Float) = 0
        [ToggleOff] _AlbedoAffectEmissive("Albedo Affect Emissive", Float) = 0.0

        [ToggleOff] _DistortionEnable("Enable Distortion", Float) = 0.0
        [ToggleOff] _DistortionOnly("Distortion Only", Float) = 0.0
        [ToggleOff] _DistortionDepthTest("Distortion Depth Test Enable", Float) = 0.0
        [ToggleOff] _DepthOffsetEnable("Depth Offset View space", Float) = 0.0

        [ToggleOff] _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0

        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // Stencil state
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2 // StencilLightingUsage.RegularLighting

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

        [Enum(None, 0, Vertex displacement, 1, Pixel displacement, 2)] _DisplacementMode("DisplacementMode", Int) = 0
        [ToggleOff] _DisplacementLockObjectScale("displacement lock object scale", Float) = 1.0
        [ToggleOff] _DisplacementLockTilingScale("displacement lock tiling scale", Float) = 1.0

        _PPDMinSamples("Min sample for POM", Range(1.0, 64.0)) = 5
        _PPDMaxSamples("Max sample for POM", Range(1.0, 64.0)) = 15
        _PPDLodThreshold("Start lod to fade out the POM effect", Range(0.0, 16.0)) = 5
        _PPDPrimitiveLength("Primitive length for POM", Float) = 1
        _PPDPrimitiveWidth("Primitive width for POM", Float) = 1
        [HideInInspector] _InvPrimScale("Inverse primitive scale for non-planar POM", Vector) = (1, 1, 0, 0)

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

        _TexWorldScale0("Tiling", Float) = 1.0
        _TexWorldScale1("Tiling", Float) = 1.0
        _TexWorldScale2("Tiling", Float) = 1.0
        _TexWorldScale3("Tiling", Float) = 1.0

        [HideInInspector] _InvTilingScale0("Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y))", Float) = 1
        [HideInInspector] _InvTilingScale1("Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y))", Float) = 1
        [HideInInspector] _InvTilingScale2("Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y))", Float) = 1
        [HideInInspector] _InvTilingScale3("Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y))", Float) = 1

        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase0("UV Set for base0", Float) = 0 // no UV1/2/3 for main layer (matching Lit.shader and for PPDisplacement restriction)
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

        [HideInInspector] _ShowMaterialReferences("_ShowMaterialReferences", Float) = 0
        [HideInInspector] _ShowLayer0("_ShowLayer0", Float) = 0
        [HideInInspector] _ShowLayer1("_ShowLayer1", Float) = 0
        [HideInInspector] _ShowLayer2("_ShowLayer2", Float) = 0
        [HideInInspector] _ShowLayer3("_ShowLayer3", Float) = 0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 metal // TEMP: until we go further in dev
    // #pragma enable_d3d11_debug_symbols

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _DEPTHOFFSET_ON
    #pragma shader_feature _DOUBLESIDED_ON
    #pragma shader_feature _ _VERTEX_DISPLACEMENT _PIXEL_DISPLACEMENT
    #pragma shader_feature _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
    #pragma shader_feature _DISPLACEMENT_LOCK_TILING_SCALE
    #pragma shader_feature _PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE
    #pragma shader_feature _VERTEX_WIND

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

    #pragma shader_feature _NORMALMAP0
    #pragma shader_feature _NORMALMAP1
    #pragma shader_feature _NORMALMAP2
    #pragma shader_feature _NORMALMAP3
    #pragma shader_feature _MASKMAP0
    #pragma shader_feature _MASKMAP1
    #pragma shader_feature _MASKMAP2
    #pragma shader_feature _MASKMAP3
    #pragma shader_feature _BENTNORMALMAP0
    #pragma shader_feature _BENTNORMALMAP1
    #pragma shader_feature _BENTNORMALMAP2
    #pragma shader_feature _BENTNORMALMAP3
    #pragma shader_feature _EMISSIVE_COLOR_MAP
    #pragma shader_feature _ENABLESPECULAROCCLUSION
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
    #pragma shader_feature _INFLUENCEMASK_MAP
    #pragma shader_feature _DENSITY_MODE
    #pragma shader_feature _HEIGHT_BASED_BLEND
    #pragma shader_feature _ _LAYEREDLIT_3_LAYERS _LAYEREDLIT_4_LAYERS

    #pragma shader_feature _ _BLENDMODE_LERP _BLENDMODE_ADD _BLENDMODE_SOFT_ADD _BLENDMODE_MULTIPLY _BLENDMODE_PRE_MULTIPLY

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
    // Use surface gradient normal mapping as it handle correctly triplanar normal mapping and multiple UVSet
    #define SURFACE_GRADIENT
    // This shader support vertex modification
    #define HAVE_VERTEX_MODIFICATION

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "../../../Core/ShaderLibrary/common.hlsl"
    #include "../../../Core/ShaderLibrary/Wind.hlsl"
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

            #define SHADERPASS SHADERPASS_GBUFFER
            #include "../../ShaderVariables.hlsl"
            #include "../../Material/Material.hlsl"
            #include "../Lit/ShaderPass/LitSharePass.hlsl"
            #include "../Lit/LitData.hlsl"
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

            #define SHADERPASS SHADERPASS_GBUFFER
            #define _BYPASS_ALPHA_TEST
            #include "../../ShaderVariables.hlsl"
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

            #define DEBUG_DISPLAY
            #define SHADERPASS SHADERPASS_GBUFFER
            #include "../../ShaderVariables.hlsl"
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

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #include "../../ShaderVariables.hlsl"
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

            #define SHADERPASS SHADERPASS_VELOCITY
            #include "../../ShaderVariables.hlsl"
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

            ZClip Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_SHADOWS
            #define USE_LEGACY_UNITY_MATRIX_VARIABLES
            #include "../../ShaderVariables.hlsl"
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

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "../../ShaderVariables.hlsl"
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

            #define SHADERPASS SHADERPASS_DISTORTION
            #include "../../ShaderVariables.hlsl"
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

            #define SHADERPASS SHADERPASS_FORWARD
            #include "../../ShaderVariables.hlsl"
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
            Name "ForwardDebugDisplay" // Name is not used
            Tags{ "LightMode" = "ForwardDebugDisplay" } // This will be only for transparent object based on the RenderQueue index

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_CullMode]

            HLSLPROGRAM

            #define DEBUG_DISPLAY
            #define SHADERPASS SHADERPASS_FORWARD
            #include "../../ShaderVariables.hlsl"
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
