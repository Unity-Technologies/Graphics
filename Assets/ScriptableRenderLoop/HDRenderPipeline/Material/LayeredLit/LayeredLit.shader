Shader "HDRenderPipeline/LayeredLit"
{
    Properties
    {
        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

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

        _Smoothness0("Smoothness0", Range(0.0, 1.0)) = 0.5
        _Smoothness1("Smoothness1", Range(0.0, 1.0)) = 0.5
        _Smoothness2("Smoothness2", Range(0.0, 1.0)) = 0.5
        _Smoothness3("Smoothness3", Range(0.0, 1.0)) = 0.5

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

        _DetailHeightScale0("_DetailHeightScale0", Range(-2.0, 2.0)) = 1
        _DetailHeightScale1("_DetailHeightScale1", Range(-2.0, 2.0)) = 1
        _DetailHeightScale2("_DetailHeightScale2", Range(-2.0, 2.0)) = 1
        _DetailHeightScale3("_DetailHeightScale3", Range(-2.0, 2.0)) = 1

        _DetailAOScale0("_DetailAOScale0", Range(-2.0, 2.0)) = 1
        _DetailAOScale1("_DetailAOScale1", Range(-2.0, 2.0)) = 1
        _DetailAOScale2("_DetailAOScale2", Range(-2.0, 2.0)) = 1
        _DetailAOScale3("_DetailAOScale3", Range(-2.0, 2.0)) = 1

        // Layer blending options
        _LayerMaskMap("LayerMaskMap", 2D) = "white" {}
        [ToggleOff] _UseHeightBasedBlend("UseHeightBasedBlend", Float) = 0.0
        // Layer blending options V2
        [ToggleOff] _UseHeightBasedBlendV2("Use Height Blend V2", Float) = 0.0
        [ToggleOff] _UseMainLayerInfluence("UseMainLayerInfluence", Float) = 0.0

        _HeightOffset1("_HeightOffset1", Range(-0.3, 0.3)) = 0.0
        _HeightOffset2("_HeightOffset2", Range(-0.3, 0.3)) = 0.0
        _HeightOffset3("_HeightOffset3", Range(-0.3, 0.3)) = 0.0

        _HeightFactor1("_HeightFactor1", Float) = 1
        _HeightFactor2("_HeightFactor2", Float) = 1
        _HeightFactor3("_HeightFactor3", Float) = 1

        _BlendSize1("_BlendSize1", Range(0, 0.30)) = 0.0
        _BlendSize2("_BlendSize2", Range(0, 0.30)) = 0.0
        _BlendSize3("_BlendSize3", Range(0, 0.30)) = 0.0

        _VertexColorHeightFactor("_VertexColorHeightFactor", Float) = 0.3

        _HeightCenterOffset1("_HeightCenterOffset1", Float) = 0.0
        _HeightCenterOffset2("_HeightCenterOffset2", Float) = 0.0
        _HeightCenterOffset3("_HeightCenterOffset3", Float) = 0.0

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

        _MinimumOpacity1("_MinimumOpacity1", Range(0, 1.0)) = 1.0
        _MinimumOpacity2("_MinimumOpacity2", Range(0, 1.0)) = 1.0
        _MinimumOpacity3("_MinimumOpacity3", Range(0, 1.0)) = 1.0

        _OpacityAsDensity1("_OpacityAsDensity1", Range(0, 1.0)) = 0.0
        _OpacityAsDensity2("_OpacityAsDensity2", Range(0, 1.0)) = 0.0
        _OpacityAsDensity3("_OpacityAsDensity3", Range(0, 1.0)) = 0.0

        _LayerTiling0("LayerTiling0", Float) = 1
        _LayerTiling1("LayerTiling1", Float) = 1
        _LayerTiling2("LayerTiling2", Float) = 1
        _LayerTiling3("LayerTiling3", Float) = 1

        _DistortionVectorMap("DistortionVectorMap", 2D) = "black" {}

        _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        _EmissiveIntensity("EmissiveIntensity", Float) = 0

        [ToggleOff] _DistortionEnable("Enable Distortion", Float) = 0.0
        [ToggleOff] _DistortionOnly("Distortion Only", Float) = 0.0
        [ToggleOff] _DistortionDepthTest("Distortion Depth Test Enable", Float) = 0.0
        [ToggleOff] _DepthOffsetEnable("Depth Offset View space", Float) = 0.0

        [ToggleOff] _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0

        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode ("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _ZTestMode("_ZTestMode", Int) = 8

        [Enum(None, 0, DoubleSided, 1, DoubleSidedLigthingFlip, 2, DoubleSidedLigthingMirror, 3)] _DoubleSidedMode("Double sided mode", Float) = 0

        [Enum(Mask Alpha, 0, BaseColor Alpha, 1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 1
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace("NormalMap space", Float) = 0
        [ToggleOff]  _EnablePerPixelDisplacement("Enable per pixel displacement", Float) = 0.0
        _PPDMinSamples("Min sample for POM", Range(1.0, 64.0)) = 5
        _PPDMaxSamples("Max sample for POM", Range(1.0, 64.0)) = 15
        [Enum(DetailMapNormal, 0, DetailMapAOHeight, 1)] _DetailMapMode("DetailMap mode", Float) = 0
        [Enum(Use Emissive Color, 0, Use Emissive Mask, 1)] _EmissiveColorMode("Emissive color mode", Float) = 1

        [HideInInspector] _LayerCount("_LayerCount", Float) = 2.0

        [Enum(None, 0, Multiply, 1, Add, 2)] _VertexColorMode("Vertex color mode", Float) = 0

        // WARNING
        // All the following properties that concern the UV mapping are the same as in the Lit shader.
        // This means that they will get overridden when synchronizing the various layers.
        // To avoid this, make sure that all properties here are in the exclusion list in LayeredLitUI.SynchronizeLayerProperties
        _TexWorldScale0("Tiling", Float) = 1.0
        _TexWorldScale1("Tiling", Float) = 1.0
        _TexWorldScale2("Tiling", Float) = 1.0
        _TexWorldScale3("Tiling", Float) = 1.0

        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase0("UV Set for base0", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase1("UV Set for base1", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase2("UV Set for base2", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase3("UV Set for base3", Float) = 0

        [HideInInspector] _UVMappingMask0("_UVMappingMask0", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVMappingMask1("_UVMappingMask1", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVMappingMask2("_UVMappingMask2", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVMappingMask3("_UVMappingMask3", Color) = (1, 0, 0, 0)

        [HideInInspector] _UVMappingPlanar0("_UVMappingPlanar0", Float) = 0.0
        [HideInInspector] _UVMappingPlanar1("_UVMappingPlanar1", Float) = 0.0
        [HideInInspector] _UVMappingPlanar2("_UVMappingPlanar2", Float) = 0.0
        [HideInInspector] _UVMappingPlanar3("_UVMappingPlanar3", Float) = 0.0        

        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail0("UV Set for detail0", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail1("UV Set for detail1", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail2("UV Set for detail2", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail3("UV Set for detail3", Float) = 0

        [HideInInspector] _UVDetailsMappingMask0("_UVDetailsMappingMask0", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVDetailsMappingMask1("_UVDetailsMappingMask1", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVDetailsMappingMask2("_UVDetailsMappingMask2", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVDetailsMappingMask3("_UVDetailsMappingMask3", Color) = (1, 0, 0, 0)
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 metal // TEMP: unitl we go futher in dev

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _DISTORTION_ON
    #pragma shader_feature _DEPTHOFFSET_ON
    #pragma shader_feature _ _DOUBLESIDED _DOUBLESIDED_LIGHTING_FLIP _DOUBLESIDED_LIGHTING_MIRROR

    #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    #pragma shader_feature _LAYER_MAPPING_TRIPLANAR_0
    #pragma shader_feature _LAYER_MAPPING_TRIPLANAR_1
    #pragma shader_feature _LAYER_MAPPING_TRIPLANAR_2
    #pragma shader_feature _LAYER_MAPPING_TRIPLANAR_3
    #pragma shader_feature _DETAIL_MAP_WITH_NORMAL
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE   
    #pragma shader_feature _PER_PIXEL_DISPLACEMENT
    #pragma shader_feature _ _REQUIRE_UV2 _REQUIRE_UV3
    #pragma shader_feature _EMISSIVE_COLOR

    #pragma shader_feature _NORMALMAP
    #pragma shader_feature _MASKMAP
    #pragma shader_feature _SPECULAROCCLUSIONMAP
    #pragma shader_feature _EMISSIVE_COLOR_MAP
    #pragma shader_feature _HEIGHTMAP
    #pragma shader_feature _DETAIL_MAP
    #pragma shader_feature _ _LAYER_MASK_VERTEX_COLOR_MUL _LAYER_MASK_VERTEX_COLOR_ADD
    #pragma shader_feature _MAIN_LAYER_INFLUENCE_MODE
    #pragma shader_feature _HEIGHT_BASED_BLEND
    #pragma shader_feature _HEIGHT_BASED_BLEND_V2
    #pragma shader_feature _ _LAYEREDLIT_3_LAYERS _LAYEREDLIT_4_LAYERS

    #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
    #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED
    #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
    // TODO: We should have this keyword only if VelocityInGBuffer is enable, how to do that ?
    //#pragma multi_compile VELOCITYOUTPUT_OFF VELOCITYOUTPUT_ON 

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "common.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderConfig.cs.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderVariables.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderPass/ShaderPass.cs.hlsl"    

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

    #include "Assets/ScriptableRenderLoop/HDRenderPipeline/Material/Lit/LitProperties.hlsl"

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

            Cull  [_CullMode]

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_GBUFFER

            #include "../../Material/Material.hlsl"            
            #include "../Lit/ShaderPass/LitSharePass.hlsl"    
            #include "../Lit/LitData.hlsl"
            #include "../../ShaderPass/ShaderPassGBuffer.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Debug"
            Tags{ "LightMode" = "DebugViewMaterial" }

            Cull[_CullMode]

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_DEBUG_VIEW_MATERIAL

            #include "../../Material/Material.hlsl"            
            #include "../Lit/ShaderPass/LitSharePass.hlsl"
            #include "../Lit/LitData.hlsl"
            #include "../../ShaderPass/ShaderPassDebugViewMaterial.hlsl"

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
    }

	CustomEditor "Experimental.Rendering.HDPipeline.LayeredLitGUI"
}
