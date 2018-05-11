Shader "HDRenderPipeline/StackLit"
{
    Properties
    {
        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

        // Reminder. Color here are in linear but the UI (color picker) do the conversion sRGB to linear
        // Be careful, do not change the name here to _Color. It will conflict with the "fake" parameters (see end of properties) required for GI.
        _BaseColor("BaseColor", Color) = (1,1,1,1)
        _BaseColorMap("BaseColor Map", 2D) = "white" {}
        [HideInInspector] _BaseColorMapShow("BaseColor Map Show", Float) = 0
        _BaseColorMapUV("BaseColor Map UV", Float) = 0.0
        _BaseColorMapUVLocal("BaseColorMap UV Local", Float) = 0.0

        [HideInInspector] _MetallicMapShow("Metallic Map Show", Float) = 0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0
        _MetallicMap("Metallic Map", 2D) = "black" {}
        _MetallicUseMap("Metallic Use Map", Float) = 0
        _MetallicMapUV("Metallic Map UV", Float) = 0.0
        _MetallicMapUVLocal("Metallic Map UV Local", Float) = 0.0
        _MetallicMapChannel("Metallic Map Channel", Float) = 0.0
        _MetallicMapChannelMask("Metallic Map Channel Mask", Vector) = (1, 0, 0, 0)
        _MetallicRemap("Metallic Remap", Vector) = (0, 1, 0, 0)
        [HideInInspector] _MetallicRange("Metallic Range", Vector) = (0, 1, 0, 0)

        _DielectricIor("DielectricIor IOR", Range(1.0, 2.5)) = 1.5

        [HideInInspector] _SmoothnessAMapShow("SmoothnessA Map Show", Float) = 0
        _SmoothnessA("SmoothnessA", Range(0.0, 1.0)) = 1.0
        _SmoothnessAMap("SmoothnessA Map", 2D) = "white" {}
        _SmoothnessAUseMap("SmoothnessA Use Map", Float) = 0
        _SmoothnessAMapUV("SmoothnessA Map UV", Float) = 0.0
        _SmoothnessAMapUVLocal("_SmoothnessA Map UV Local", Float) = 0.0
        _SmoothnessAMapChannel("SmoothnessA Map Channel", Float) = 0.0
        _SmoothnessAMapChannelMask("SmoothnessA Map Channel Mask", Vector) = (1, 0, 0, 0)
        _SmoothnessARemap("SmoothnessA Remap", Vector)  = (0, 1, 0, 0)
        [ToggleUI] _SmoothnessARemapInverted("Invert SmoothnessA Remap", Float) = 0.0
        [HideInInspector] _SmoothnessARange("SmoothnessA Range", Vector) = (0, 1, 0, 0)

        [ToggleUI] _EnableDualSpecularLobe("Enable Dual Specular Lobe", Float) = 0.0 // UI only
        [HideInInspector] _SmoothnessBMapShow("SmoothnessB Map Show", Float) = 0
        _SmoothnessB("SmoothnessB", Range(0.0, 1.0)) = 1.0
        _SmoothnessBMap("SmoothnessB Map", 2D) = "white" {}
        _SmoothnessBUseMap("SmoothnessB Use Map", Float) = 0
        _SmoothnessBMapUV("SmoothnessB Map UV", Float) = 0.0
        _SmoothnessAMapUVLocal("_SmoothnessB Map UV Local", Float) = 0.0
        _SmoothnessBMapChannel("SmoothnessB Map Channel", Float) = 0.0
        _SmoothnessBMapChannelMask("SmoothnessB Map Channel Mask", Vector) = (1, 0, 0, 0)
        _SmoothnessBRemap("SmoothnessB Remap", Vector) = (0, 1, 0, 0)
        [ToggleUI] _SmoothnessBRemapInverted("Invert SmoothnessB Remap", Float) = 0.0
        [HideInInspector] _SmoothnessBRange("SmoothnessB Range", Vector) = (0, 1, 0, 0)
        _LobeMix("Lobe Mix", Range(0.0, 1.0)) = 0

        [ToggleUI] _VlayerRecomputePerLight("Vlayer Recompute Per Light", Float) = 0.0 // UI only
        [ToggleUI] _VlayerUseRefractedAnglesForBase("Vlayer Use Refracted Angles For Base", Float) = 0.0 // UI only
        [ToggleUI] _DebugEnable("Debug Enable", Float) = 0.0 // UI only
        _DebugEnvLobeMask("DebugEnvLobeMask", Vector) = (1, 1, 1, 1)
        _DebugLobeMask("DebugLobeMask", Vector) = (1, 1, 1, 1)
        _DebugAniso("DebugAniso", Vector) = (1, 0, 0, 1000.0)

        // TODO: TangentMap, AnisotropyMap and CoatIorMap (SmoothnessMap ?)

        [ToggleUI] _EnableAnisotropy("Enable Anisotropy", Float) = 0.0 // UI only
        _Anisotropy("Anisotropy", Range(-1.0, 1.0)) = 0.0
        _AnisotropyMap("Anisotropy Map", 2D) = "white" {}
        _AnisotropyUseMap("Anisotropy Use Map", Float) = 0
        _AnisotropyMapUV("Anisotropy Map UV", Float) = 0.0
        _AnisotropyMapUVLocal("Anisotropy Map UV Local", Float) = 0.0
        _AnisotropyMapChannel("Anisotropy Map Channel", Float) = 0.0
        _AnisotropyMapChannelMask("Anisotropy Map Channel Mask", Vector) = (1, 0, 0, 0)
        _AnisotropyRemap("Anisotropy Remap", Vector) = (0, 1, 0, 0)
        [HideInInspector] _AnisotropyRange("Anisotropy Range", Vector) = (0, 1, 0, 0)

        [ToggleUI] _EnableCoat("Enable Coat", Float) = 0.0 // UI only
        [HideInInspector] _CoatSmoothnessMapShow("CoatSmoothness Show", Float) = 0
        _CoatSmoothness("CoatSmoothness", Range(0.0, 1.0)) = 1.0
        _CoatSmoothnessMap("CoatSmoothness Map", 2D) = "white" {}
        _CoatSmoothnessUseMap("CoatSmoothness Use Map", Float) = 0
        _CoatSmoothnessMapUV("CoatSmoothness Map UV", Float) = 0.0
        _CoatSmoothnessMapUVLocal("CoatSmoothness Map UV Local", Float) = 0.0
        _CoatSmoothnessMapChannel("CoatSmoothness Map Channel", Float) = 0.0
        _CoatSmoothnessMapChannelMask("CoatSmoothness Map Channel Mask", Vector) = (1, 0, 0, 0)
        _CoatSmoothnessRemap("CoatSmoothness Remap", Vector) = (0, 1, 0, 0)
        [ToggleUI] _CoatSmoothnessRemapInverted("Invert CoatSmoothness Remap", Float) = 0.0
        [HideInInspector] _CoatSmoothnessRange("CoatSmoothness Range", Vector) = (0, 1, 0, 0)

        _CoatIor("Coat IOR", Range(1.0001, 2.0)) = 1.5
        _CoatThickness("Coat Thickness", Range(0.0, 0.99)) = 0.0
        _CoatExtinction("Coat Extinction Coefficient", Color) = (1,1,1) // in thickness^-1 units

        [ToggleUI] _EnableCoatNormalMap("Enable Coat Normal Map", Float) = 0.0 // UI only
        [HideInInspector] _CoatNormalMapShow("Coat NormalMap Show", Float) = 0.0
        _CoatNormalMap("Coat NormalMap", 2D) = "bump" {}     // Tangent space normal map
        _CoatNormalMapUV("Coat NormalMapUV", Float) = 0.0
        _CoatNormalMapUVLocal("Coat NormalMapUV Local", Float) = 0.0
        _CoatNormalMapObjSpace("Coat NormalMap ObjSpace", Float) = 0.0
        _CoatNormalScale("Coat NormalMap Scale", Range(0.0, 2.0)) = 1

        [HideInInspector] _NormalMapShow("NormalMap Show", Float) = 0.0
        _NormalMap("NormalMap", 2D) = "bump" {}     // Tangent space normal map
        _NormalMapUV("NormalMapUV", Float) = 0.0
        _NormalMapUVLocal("NormalMapUV Local", Float) = 0.0
        _NormalMapObjSpace("NormalMapUV Local", Float) = 0.0
        _NormalScale("Normal Scale", Range(0.0, 2.0)) = 1

        [HideInInspector] _AmbientOcclusionMapShow("AmbientOcclusion Map Show", Float) = 0
        _AmbientOcclusion("AmbientOcclusion", Range(0.0, 1.0)) = 1
        _AmbientOcclusionMap("AmbientOcclusion Map", 2D) = "white" {}
        _AmbientOcclusionUseMap("AmbientOcclusion Use Map", Float) = 0
        _AmbientOcclusionMapUV("AmbientOcclusion Map UV", Float) = 0.0
        _AmbientOcclusionMapUVLocal("AmbientOcclusion Map UV Local", Float) = 0.0
        _AmbientOcclusionMapChannel("AmbientOcclusion Map Channel", Float) = 0.0
        _AmbientOcclusionMapChannelMask("AmbientOcclusion Map Channel Mask", Vector) = (1, 0, 0, 0)
        _AmbientOcclusionRemap("AmbientOcclusion Remap", Vector) = (0, 1, 0, 0)
        [HideInInspector] _AmbientOcclusionRange("AmbientOcclusion Range", Vector) = (0, 1, 0, 0)

        [HideInInspector] _EmissiveColorMapShow("Emissive Color Map Show", Float) = 0.0
        _EmissiveColor("Emissive Color", Color) = (1, 1, 1)
        _EmissiveColorMap("Emissive Color Map", 2D) = "white" {}
        _EmissiveColorMapUV("Emissive Color Map UV", Range(0.0, 1.0)) = 0
        _EmissiveColorMapUVLocal("Emissive Color Map UV Local", Float) = 0.0
        _EmissiveIntensity("Emissive Intensity", Float) = 0
        [ToggleUI] _AlbedoAffectEmissive("Albedo Affect Emissive", Float) = 0.0

        [ToggleUI] _EnableSubsurfaceScattering("Enable Subsurface Scattering", Float) = 0.0
        _DiffusionProfile("Diffusion Profile", Int) = 0
        [HideInInspector] _SubsurfaceMaskMapShow("Subsurface Mask Map Show", Float) = 0
        _SubsurfaceMask("Subsurface Mask", Range(0.0, 1.0)) = 1.0
        _SubsurfaceMaskMap("Subsurface Mask Map", 2D) = "black" {}
        _SubsurfaceMaskUseMap("Subsurface Mask Use Map", Float) = 0
        _SubsurfaceMaskMapUV("Subsurface Mask Map UV", Float) = 0.0
        _SubsurfaceMaskMapUVLocal("Subsurface Mask UV Local", Float) = 0.0
        _SubsurfaceMaskMapChannel("Subsurface Mask Map Channel", Float) = 0.0
        _SubsurfaceMaskMapChannelMask("Subsurface Mask Map Channel Mask", Vector) = (1, 0, 0, 0)
        _SubsurfaceMaskRemap("Subsurface Mask Remap", Vector) = (0, 1, 0, 0)
        [HideInInspector] _SubsurfaceMaskRange("Subsurface Mask Range", Vector) = (0, 1, 0, 0)

        [ToggleUI] _EnableTransmission("Enable Transmission", Float) = 0.0
        [HideInInspector] _ThicknessMapShow("Thickness Show", Float) = 0
        _Thickness("Thickness", Range(0.0, 1.0)) = 1.0
        _ThicknessMap("Thickness Map", 2D) = "black" {}
        _ThicknessUseMap("Thickness Use Map", Float) = 0
        _ThicknessMapUV("Thickness Map UV", Float) = 0.0
        _ThicknessMapUVLocal("Thickness Map UV Local", Float) = 0.0
        _ThicknessMapChannel("Thickness Map Channel", Float) = 0.0
        _ThicknessMapChannelMask("Thickness Map Channel Mask", Vector) = (1, 0, 0, 0)
        _ThicknessRemap("Thickness Remap", Vector) = (0, 1, 0, 0)
        [ToggleUI] _ThicknessRemapInverted("Invert Thickness Remap", Float) = 0.0
        [HideInInspector] _ThicknessRange("Thickness Range", Vector) = (0, 1, 0, 0)

        [ToggleUI] _EnableIridescence("Enable Iridescence", Float) = 0.0 // UI only
        _IridescenceIor("Coat IOR", Range(1.0, 2.0)) = 1.5
        _IridescenceThickness("_IridescenceThickness", Range(0.0, 1.0)) = 0.0
        _IridescenceThicknessMap("IridescenceThickness Color Map", 2D) = "black" {}
        _IridescenceThicknessUseMap("IridescenceThickness Use Map", Float) = 0
        _IridescenceThicknessMapUV("IridescenceThickness Map UV", Float) = 0.0
        _IridescenceThicknessMapLocal("IridescenceThickness Map UV Local", Float) = 0.0
        _IridescenceThicknessMapChannel("IridescenceThickness Mask Map Channel", Float) = 0.0
        _IridescenceThicknessMapChannelMask("IridescenceThickness Mask Map Channel Mask", Vector) = (1, 0, 0, 0)
        [HideInInspector] _IridescenceThicknessRange("IridescenceThickness Range", Vector) = (0, 1, 0, 0)

        _DistortionVectorMap("DistortionVectorMap", 2D) = "black" {}
        [ToggleUI] _DistortionEnable("Enable Distortion", Float) = 0.0
        [ToggleUI] _DistortionOnly("Distortion Only", Float) = 0.0
        [ToggleUI] _DistortionDepthTest("Distortion Depth Test Enable", Float) = 1.0
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

        // Transparency
        [ToggleUI] _PreRefractionPass("PreRefractionPass", Float) = 0.0

        [ToggleUI]  _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _TransparentSortPriority("_TransparentSortPriority", Float) = 0

        // Stencil state
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2 // StencilLightingUsage.RegularLighting  (fixed at compile time)
        [HideInInspector] _StencilWriteMask("_StencilWriteMask", Int) = 7 // StencilMask.Lighting  (fixed at compile time)
        [HideInInspector] _StencilRefMV("_StencilRefMV", Int) = 128 // StencilLightingUsage.RegularLighting  (fixed at compile time)
        [HideInInspector] _StencilWriteMaskMV("_StencilWriteMaskMV", Int) = 128 // StencilMask.ObjectsVelocity  (fixed at compile time)

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _CullModeForward("__cullmodeForward", Float) = 2.0 // This mode is dedicated to Forward to correctly handle backface then front face rendering thin transparent
        [HideInInspector] _ZTestDepthEqualForOpaque("_ZTestDepthEqualForOpaque", Int) = 4 // Less equal
        [HideInInspector] _ZTestModeDistortion("_ZTestModeDistortion", Int) = 8

        [ToggleUI] _SpecularAntiAliasingEnabled("Specular Anti Aliasing Enabled", Float) = 0.0
        _SpecularAntiAliasingScreenSpaceVariance("SpecularAntiAliasingScreenSpaceVariance", Range(0.0, 1.0)) = 1.0
        _SpecularAntiAliasingThreshold("SpecularAntiAliasingThreshold", Range(0.0, 1.0)) = 0.18

        [ToggleUI] _NormalCurvatureToRoughnessEnabled("_NormalCurvatureToRoughnessEnabled", Float) = 0.0
        _NormalCurvatureToRoughnessScale("_NormalCurvatureToRoughnessScale", Range(0.0, 2.0)) = 1.0
        _NormalCurvatureToRoughnessBias("_NormalCurvatureToRoughnessBias", Range(-1.0, 1.0)) = 0.0
        _NormalCurvatureToRoughnessExponent("_NormalCurvatureToRoughnessExponent", Range(0.05, 20.0)) = 0.333

        [ToggleUI] _EnableFogOnTransparent("Enable Fog", Float) = 1.0
        [ToggleUI] _EnableBlendModePreserveSpecularLighting("Enable Blend Mode Preserve Specular Lighting", Float) = 1.0

        [ToggleUI] _DoubleSidedEnable("Double sided enable", Float) = 0.0
        [Enum(Flip, 0, Mirror, 1, None, 2)] _DoubleSidedNormalMode("Double sided normal mode", Float) = 1 // This is for the editor only, see BaseLitUI.cs: _DoubleSidedConstants will be set based on the mode.
        [HideInInspector] _DoubleSidedConstants("_DoubleSidedConstants", Vector) = (1, 1, -1, 0)


        // Sections show values.
        [HideInInspector] _MaterialFeaturesShow("_MaterialFeaturesShow", Float) = 1.0
        [HideInInspector] _StandardShow("_StandardShow", Float) = 0.0
        [HideInInspector] _EmissiveShow("_EmissiveShow", Float) = 0.0
        [HideInInspector] _CoatShow("_CoatShow", Float) = 0.0
        [HideInInspector] _DebugShow("_DebugShow", Float) = 0.0
        [HideInInspector] _SSSShow("_SSSShow", Float) = 0.0
        [HideInInspector] _DualSpecularLobeShow("_DualSpecularLobeShow", Float) = 0.0
        [HideInInspector] _AnisotropyShow("_AnisotropyShow", Float) = 0.0
        [HideInInspector] _TransmissionShow("_TransmissionShow", Float) = 0.0
        [HideInInspector] _IridescenceShow("_IridescenceShow", Float) = 0.0

        // Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
        // value that exist to identify if the GI emission need to be enabled.
        // In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
        // TODO: Fix the code in legacy unity so we can customize the beahvior for GI
        _EmissionColor("Color", Color) = (1, 1, 1)

        // HACK: GI Baking system relies on some properties existing in the shader ("_MainTex", "_Cutoff" and "_Color") for opacity handling, so we need to store our version of those parameters in the hard-coded name the GI baking system recognizes.
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _DOUBLESIDED_ON

    #pragma shader_feature _NORMALMAP_TANGENT_SPACE
    #pragma shader_feature _USE_UV2
    #pragma shader_feature _USE_UV3
    #pragma shader_feature _USE_TRIPLANAR
    // ...TODO: for surface gradient framework eg see litdata.hlsl,
    // but we need it right away for toggle with LayerTexCoord mapping so we might need them
    // from the Frag input right away. See also ShaderPass/StackLitSharePass.hlsl.

    // Keyword for transparent
    #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
    #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
    #pragma shader_feature _BLENDMODE_PRESERVE_SPECULAR_LIGHTING // easily handled in material.hlsl, so adding this already.
    #pragma shader_feature _ENABLE_FOG_ON_TRANSPARENT

    // MaterialFeature are used as shader feature to allow compiler to optimize properly
    #pragma shader_feature _MATERIAL_FEATURE_DUAL_SPECULAR_LOBE
    #pragma shader_feature _MATERIAL_FEATURE_ANISOTROPY
    #pragma shader_feature _MATERIAL_FEATURE_COAT
    #pragma shader_feature _MATERIAL_FEATURE_COAT_NORMALMAP
    #pragma shader_feature _MATERIAL_FEATURE_IRIDESCENCE
    #pragma shader_feature _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    #pragma shader_feature _MATERIAL_FEATURE_TRANSMISSION

    #pragma shader_feature _VLAYERED_RECOMPUTE_PERLIGHT
    #pragma shader_feature _VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE


    #pragma shader_feature _STACKLIT_DEBUG
    //enable GPU instancing support
    #pragma multi_compile_instancing

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    #define UNITY_MATERIAL_STACKLIT // Need to be define before including Material.hlsl

    // If we use subsurface scattering, enable output split lighting (for forward pass)
    #if defined(_MATERIAL_FEATURE_SUBSURFACE_SCATTERING) && !defined(_SURFACE_TYPE_TRANSPARENT)
    #define OUTPUT_SPLIT_LIGHTING
    #endif

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "CoreRP/ShaderLibrary/Common.hlsl"
    #include "../../ShaderPass/FragInputs.hlsl"
    #include "../../ShaderPass/ShaderPass.cs.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "../../Material/StackLit/StackLitProperties.hlsl"

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDStackLitShader" }

        // Caution: The outline selection in the editor use the vertex shader/hull/domain shader of the first pass declare. So it should not be the meta pass.

        Pass
        {
            Name "Depth prepass"
            Tags{ "LightMode" = "DepthForwardOnly" }

            Cull[_CullMode]

            ZWrite On

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "../../ShaderVariables.hlsl"
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/StackLitDepthPass.hlsl"
            #include "StackLitData.hlsl"
            #include "../../ShaderPass/ShaderPassDepthOnly.hlsl"

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
            #include "ShaderPass/StackLitSharePass.hlsl"
            #include "StackLitData.hlsl"
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

            #define SHADERPASS SHADERPASS_SHADOWS
            #define USE_LEGACY_UNITY_MATRIX_VARIABLES
            #include "../../ShaderVariables.hlsl"
            #include "../../Material/Material.hlsl"

            #include "ShaderPass/StackLitDepthPass.hlsl"
            #include "StackLitData.hlsl"
            #include "../../ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Distortion" // Name is not used
            Tags { "LightMode" = "DistortionVectors" } // This will be only for transparent object based on the RenderQueue index

            Blend [_DistortionSrcBlend] [_DistortionDstBlend], [_DistortionBlurSrcBlend] [_DistortionBlurDstBlend]
            BlendOp Add, [_DistortionBlurBlendOp]
            ZTest [_ZTestModeDistortion]
            ZWrite off
            Cull [_CullMode]

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_DISTORTION
            #include "../../ShaderVariables.hlsl"
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/StackLitDistortionPass.hlsl"
            #include "StackLitData.hlsl"
            #include "../../ShaderPass/ShaderPassDistortion.hlsl"

            ENDHLSL
        }

        // StackLit shader always render in forward
        Pass
        {
            Name "Forward" // Name is not used
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
            //
            // NOTE: For _CullModeForward, see BaseLitUI and the handling of TransparentBackfaceEnable:
            // Basically, we need to use it to support a TransparentBackface pass before this pass
            // (and it should be placed just before this one) for separate backface and frontface rendering,
            // eg for "hair shader style" approximate sorting, see eg Thorsten Scheuermann writeups on this:
            // http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.607.1272&rep=rep1&type=pdf
            // http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2012/10/Scheuermann_HairSketchSlides.pdf
            // http://web.engr.oregonstate.edu/~mjb/cs519/Projects/Papers/HairRendering.pdf
            //
            // See Lit.shader and the order of the passes after a DistortionVectors, we have:
            // TransparentDepthPrepass, TransparentBackface, Forward, TransparentDepthPostpass

            HLSLPROGRAM

            #pragma multi_compile _ DEBUG_DISPLAY
            //NEWLITTODO
            //#pragma multi_compile _ LIGHTMAP_ON
            //#pragma multi_compile _ DIRLIGHTMAP_COMBINED
            //#pragma multi_compile _ DYNAMICLIGHTMAP_ON
            //#pragma multi_compile _ SHADOWS_SHADOWMASK

            // #include "../../Lighting/Forward.hlsl" : nothing left in there.
            //#pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS
            #define LIGHTLOOP_TILE_PASS
            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #define SHADERPASS SHADERPASS_FORWARD
            // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
            #ifndef _SURFACE_TYPE_TRANSPARENT
                #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
            #endif
            #include "../../ShaderVariables.hlsl"
            #ifdef DEBUG_DISPLAY
            #include "../../Debug/DebugDisplay.hlsl"
            #endif
            #include "../../Lighting/Lighting.hlsl"
            //...this will include #include "../../Material/Material.hlsl" but also LightLoop which the forward pass directly uses.

            #include "ShaderPass/StackLitSharePass.hlsl"
            #include "StackLitData.hlsl"
            #include "../../ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }

    }

    CustomEditor "Experimental.Rendering.HDPipeline.StackLitGUI"
}
