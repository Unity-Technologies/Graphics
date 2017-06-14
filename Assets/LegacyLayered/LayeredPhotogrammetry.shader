Shader "LayeredPhotogrammetry"
{
    Properties
    {
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

        _NormalMap0("NormalMap0", 2D) = "bump" {}
        _NormalMap1("NormalMap1", 2D) = "bump" {}
        _NormalMap2("NormalMap2", 2D) = "bump" {}
        _NormalMap3("NormalMap3", 2D) = "bump" {}

        _NormalScale0("_NormalScale0", Range(0.0, 2.0)) = 1
        _NormalScale1("_NormalScale1", Range(0.0, 2.0)) = 1
        _NormalScale2("_NormalScale2", Range(0.0, 2.0)) = 1
        _NormalScale3("_NormalScale3", Range(0.0, 2.0)) = 1

        _HeightMap0("HeightMap0", 2D) = "black" {}
        _HeightMap1("HeightMap1", 2D) = "black" {}
        _HeightMap2("HeightMap2", 2D) = "black" {}
        _HeightMap3("HeightMap3", 2D) = "black" {}

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

        // Layer blending options
        _LayerMaskMap("LayerMaskMap", 2D) = "white" {}
        [ToggleOff] _UseHeightBasedBlend("UseHeightBasedBlend", Float) = 0.0
        [ToggleOff] _UseHeightBasedBlend("UseHeightBasedBlend", Float) = 0.0
        // Layer blending options V2
        [ToggleOff] _UseDensityMode("Use Density mode", Float) = 0.0
        [ToggleOff] _UseMainLayerInfluence("UseMainLayerInfluence", Float) = 0.0

        // Store result of combination of _HeightFactor and _HeightAmplitude0
        _LayerHeightAmplitude0("_LayerHeightAmplitude0", Float) = 1
        _LayerHeightAmplitude1("_LayerHeightAmplitude1", Float) = 1
        _LayerHeightAmplitude2("_LayerHeightAmplitude2", Float) = 1
        _LayerHeightAmplitude3("_LayerHeightAmplitude3", Float) = 1

        // Store result of combination of _HeightCenterOffset0 and _HeightCenter0
        _LayerHeightCenter0("_LayerOffset0", Float) = 0.0
        _LayerHeightCenter1("_LayerOffset1", Float) = 0.0
        _LayerHeightCenter2("_LayerOffset2", Float) = 0.0
        _LayerHeightCenter3("_LayerOffset3", Float) = 0.0

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
        [Enum(UV0, 0, Planar, 4)] _UVBlendMask("UV Set for blendMask", Float) = 0
        _TexWorldScaleBlendMask("Tiling", Float) = 1.0

        [ToggleOff] _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _TexWorldScale0("Tiling", Float) = 1.0
        _TexWorldScale1("Tiling", Float) = 1.0
        _TexWorldScale2("Tiling", Float) = 1.0
        _TexWorldScale3("Tiling", Float) = 1.0

        [Enum(UV0, 0, Planar, 4)] _UVBase0("UV Set for base0", Float) = 0 // no UV1/2/3 for main layer
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4)] _UVBase1("UV Set for base1", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4)] _UVBase2("UV Set for base2", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4)] _UVBase3("UV Set for base3", Float) = 0

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
    }

    CGINCLUDE
        
    // Define feature
    #pragma shader_feature _ALPHATEST_ON

     
    #pragma shader_feature _LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR_BLENDMASK
        
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR0
        
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR1
  
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR2
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR3
    #pragma shader_feature _ _REQUIRE_UV2 _REQUIRE_UV3
    
    #pragma shader_feature _NORMALMAP0
    #pragma shader_feature _NORMALMAP1
    #pragma shader_feature _NORMALMAP2
    #pragma shader_feature _NORMALMAP3
    
    #pragma shader_feature _MASKMAP0
    #pragma shader_feature _MASKMAP1
    #pragma shader_feature _MASKMAP2
    #pragma shader_feature _MASKMAP3
    
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
        

    #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
    #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED
    #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON

    #define _MAX_LAYER 4

    #if defined(_LAYEREDLIT_4_LAYERS)
    #   define _LAYER_COUNT 4
    #elif defined(_LAYEREDLIT_3_LAYERS)
    #   define _LAYER_COUNT 3
    #else
    #   define _LAYER_COUNT 2
    #endif
	
	#define MERGE_NAME(X, Y) X##Y
	#define PROP_DECL(type, name) type name##0, name##1, name##2, name##3
	// sampler are share by texture type inside a layered material but we need to support that a particualr layer have no texture, so we take the first sampler of available texture as the share one
	// mean we must declare all sampler
    #define PROP_DECL_TEX2D(name)\
    UNITY_DECLARE_TEX2D(MERGE_NAME(name, 0)); \
    UNITY_DECLARE_TEX2D(MERGE_NAME(name, 1)); \
    UNITY_DECLARE_TEX2D(MERGE_NAME(name, 2)); \
    UNITY_DECLARE_TEX2D(MERGE_NAME(name, 3))

    float _AlphaCutoff;

    // Set of users variables
    PROP_DECL(float4, _BaseColor);
    PROP_DECL_TEX2D(_BaseColorMap);
    float4 _BaseColorMap0_ST;
    float4 _BaseColorMap1_ST;
    float4 _BaseColorMap2_ST;
    float4 _BaseColorMap3_ST;

    PROP_DECL(float, _Metallic);
    PROP_DECL(float, _Smoothness);
    PROP_DECL_TEX2D(_MaskMap);

    PROP_DECL_TEX2D(_NormalMap);
    PROP_DECL(float, _NormalScale);
    float4 _NormalMap0_TexelSize; // Unity facility. This will provide the size of the base normal to the shader

    PROP_DECL_TEX2D(_HeightMap);
    float4 _HeightMap0_TexelSize;
    float4 _HeightMap1_TexelSize;
    float4 _HeightMap2_TexelSize;
    float4 _HeightMap3_TexelSize;

    PROP_DECL_TEX2D(_DetailMask);
    PROP_DECL_TEX2D(_DetailMap);
    float4 _DetailMap0_ST;
    float4 _DetailMap1_ST;
    float4 _DetailMap2_ST;
    float4 _DetailMap3_ST;
    PROP_DECL(float, _UVDetail);
    PROP_DECL(float, _DetailAlbedoScale);
    PROP_DECL(float, _DetailNormalScale);
    PROP_DECL(float, _DetailSmoothnessScale);

    PROP_DECL(float, _LayerHeightAmplitude);
    PROP_DECL(float, _LayerHeightCenter);
    PROP_DECL(float, _MinimumOpacity);

    UNITY_DECLARE_TEX2D(_LayerMaskMap);

    float _BlendUsingHeight1;
    float _BlendUsingHeight2;
    float _BlendUsingHeight3;

    PROP_DECL(float, _OpacityAsDensity);

    float _InheritBaseNormal1;
    float _InheritBaseNormal2;
    float _InheritBaseNormal3;

    float _InheritBaseHeight1;
    float _InheritBaseHeight2;
    float _InheritBaseHeight3;

    float _InheritBaseColor1;
    float _InheritBaseColor2;
    float _InheritBaseColor3;

    float _InheritBaseColorThreshold1;
    float _InheritBaseColorThreshold2;
    float _InheritBaseColorThreshold3;

    float _LayerTilingBlendMask;
    PROP_DECL(float, _LayerTiling);

    float _TexWorldScaleBlendMask;
    PROP_DECL(float, _TexWorldScale);
    PROP_DECL(float4, _UVMappingMask);
    PROP_DECL(float4, _UVDetailsMappingMask);

    #if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3) || defined(DYNAMICLIGHTMAP_ON)
    #define ATTRIBUTES_NEED_TEXCOORD2
    #endif
    #if defined(_REQUIRE_UV3)
    #define ATTRIBUTES_NEED_TEXCOORD3
    #endif


    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300


        // ------------------------------------------------------------------
        //  Base forward pass (directional light, emission, lightmaps, ...)
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            Blend One Zero

            ZWrite On

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------

            //#pragma shader_feature _NORMALMAP
            //#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            //#pragma shader_feature _EMISSION
            //#pragma shader_feature _METALLICGLOSSMAP
            //#pragma shader_feature ___ _DETAIL_MULX2
            //#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            //#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            //#pragma shader_feature _ _GLOSSYREFLECTIONS_OFF
            //#pragma shader_feature _PARALLAXMAP

            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertBase
            #pragma fragment fragBase
            #include "UnityLayeredPhotogrammetryCoreForward.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Additive forward pass (one light per pass)
        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            Fog { Color (0,0,0,0) } // in additive pass fog should be black
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------


            //#pragma shader_feature _NORMALMAP
            //#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            //#pragma shader_feature _METALLICGLOSSMAP
            //#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            //#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            //#pragma shader_feature ___ _DETAIL_MULX2
            //#pragma shader_feature _PARALLAXMAP

            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertAdd
            #pragma fragment fragAdd
            #include "UnityLayeredPhotogrammetryCoreForward.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------


            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _PARALLAXMAP
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #include "UnityStandardShadow.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Deferred pass
        Pass
        {
            Name "DEFERRED"
            Tags { "LightMode" = "Deferred" }

            CGPROGRAM
            #pragma target 3.0
            #pragma exclude_renderers nomrt


            // -------------------------------------

            //#pragma shader_feature _NORMALMAP
            //#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            //#pragma shader_feature _EMISSION
            //#pragma shader_feature _METALLICGLOSSMAP
            //#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            //#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            //#pragma shader_feature ___ _DETAIL_MULX2
            //#pragma shader_feature _PARALLAXMAP

            #pragma multi_compile_prepassfinal
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertDeferred
            #pragma fragment fragDeferred

            #include "UnityLayeredPhotogrammetryCore.cginc"

            ENDCG
        }

        // ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags { "LightMode"="Meta" }

            Cull Off

            CGPROGRAM
            #pragma vertex vert_meta
            #pragma fragment frag_meta

            //#pragma shader_feature _EMISSION
            //#pragma shader_feature _METALLICGLOSSMAP
            //#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            //#pragma shader_feature ___ _DETAIL_MULX2
            //#pragma shader_feature EDITOR_VISUALIZATION

            #include "UnityLayeredPhotogrammetryMeta.cginc"
            ENDCG
        }
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 150

        // ------------------------------------------------------------------
        //  Base forward pass (directional light, emission, lightmaps, ...)
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            Blend One Zero
            ZWrite On

            CGPROGRAM
            #pragma target 2.0

            //#pragma shader_feature _NORMALMAP
            //#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            //#pragma shader_feature _EMISSION
            //#pragma shader_feature _METALLICGLOSSMAP
            //#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            //#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            //#pragma shader_feature _ _GLOSSYREFLECTIONS_OFF
            // SM2.0: NOT SUPPORTED shader_feature ___ _DETAIL_MULX2
            // SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP

            #pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED

            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #pragma vertex vertBase
            #pragma fragment fragBase
            #include "UnityLayeredPhotogrammetryCoreForward.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Additive forward pass (one light per pass)
        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            Fog { Color (0,0,0,0) } // in additive pass fog should be black
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma target 2.0

            //#pragma shader_feature _NORMALMAP
            //#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            //#pragma shader_feature _METALLICGLOSSMAP
            //#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            //#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            //#pragma shader_feature ___ _DETAIL_MULX2
            // SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP
            #pragma skip_variants SHADOWS_SOFT

            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog

            #pragma vertex vertAdd
            #pragma fragment fragAdd
            #include "UnityLayeredPhotogrammetryCoreForward.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 2.0

            //#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            //#pragma shader_feature _METALLICGLOSSMAP
            //#pragma skip_variants SHADOWS_SOFT
            #pragma multi_compile_shadowcaster

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #include "UnityStandardShadow.cginc"

            ENDCG
        }

        // ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags { "LightMode"="Meta" }

            Cull Off

            CGPROGRAM
            #pragma vertex vert_meta
            #pragma fragment frag_meta

            //#pragma shader_feature _EMISSION
            //#pragma shader_feature _METALLICGLOSSMAP
            //#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            //#pragma shader_feature ___ _DETAIL_MULX2
            //#pragma shader_feature EDITOR_VISUALIZATION

            #include "UnityLayeredPhotogrammetryMeta.cginc"
            ENDCG
        }
    }


    FallBack "VertexLit"
    CustomEditor "LayeredPhotogrammetryGUI"
}
