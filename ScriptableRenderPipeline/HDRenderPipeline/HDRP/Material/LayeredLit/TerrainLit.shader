Shader "HDRenderPipeline/TerrainLit"
{
    Properties
    {
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}

        [HideInInspector] _Splat0("Layer 0", 2D) = "white" {}
        [HideInInspector] _Splat1("Layer 1", 2D) = "white" {}
        [HideInInspector] _Splat2("Layer 2", 2D) = "white" {}
        [HideInInspector] _Splat3("Layer 3", 2D) = "white" {}

        [HideInInspector] _Normal0("Normal 0", 2D) = "bump" {}
        [HideInInspector] _Normal1("Normal 1", 2D) = "bump" {}
        [HideInInspector] _Normal2("Normal 2", 2D) = "bump" {}
        [HideInInspector] _Normal3("Normal 3", 2D) = "bump" {}

        // Since we don't use a mask texture for getting the mask, we'll need the metallic property to be serialized as in sRGB space.
        [HideInInspector] [Gamma] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0
        [HideInInspector] [Gamma] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0
        [HideInInspector] [Gamma] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0
        [HideInInspector] [Gamma] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0
        [HideInInspector] _Smoothness0("Smoothness0", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness1("Smoothness1", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness2("Smoothness2", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness3("Smoothness3", Range(0.0, 1.0)) = 1.0

        // TODO: route values from terrain layers. enable _DENSITY_MODE if any of these enabled.
        [HideInInspector] [ToggleUI] _OpacityAsDensity0("_OpacityAsDensity0", Float) = 0.0
        [HideInInspector] [ToggleUI] _OpacityAsDensity1("_OpacityAsDensity1", Float) = 0.0
        [HideInInspector] [ToggleUI] _OpacityAsDensity2("_OpacityAsDensity2", Float) = 0.0
        [HideInInspector] [ToggleUI] _OpacityAsDensity3("_OpacityAsDensity3", Float) = 0.0

        // TODO: Allow heightmap?
        [HideInInspector] _HeightMap0("HeightMap0", 2D) = "black" {}
        [HideInInspector] _HeightMap1("HeightMap1", 2D) = "black" {}
        [HideInInspector] _HeightMap2("HeightMap2", 2D) = "black" {}
        [HideInInspector] _HeightMap3("HeightMap3", 2D) = "black" {}
        // Caution: Default value of _HeightAmplitude must be (_HeightMax - _HeightMin) * 0.01
        // Those two properties are computed from the ones exposed in the UI and depends on the displaement mode so they are separate because we don't want to lose information upon displacement mode change.
        [HideInInspector] _HeightAmplitude0("Height Scale0", Float) = 0.02
        [HideInInspector] _HeightAmplitude1("Height Scale1", Float) = 0.02
        [HideInInspector] _HeightAmplitude2("Height Scale2", Float) = 0.02
        [HideInInspector] _HeightAmplitude3("Height Scale3", Float) = 0.02
        [HideInInspector] _HeightCenter0("Height Bias0", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter1("Height Bias1", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter2("Height Bias2", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter3("Height Bias3", Range(0.0, 1.0)) = 0.5

        // TODO: support tri-planar?
        [HideInInspector] _TexWorldScale0("Tiling", Float) = 1.0
        [HideInInspector] _TexWorldScale1("Tiling", Float) = 1.0
        [HideInInspector] _TexWorldScale2("Tiling", Float) = 1.0
        [HideInInspector] _TexWorldScale3("Tiling", Float) = 1.0

        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

        // All the following properties are filled by the referenced lit shader.

        _SmoothnessRemapMin0("SmoothnessRemapMin0", Range(0.0, 1.0)) = 0.0
        _SmoothnessRemapMin1("SmoothnessRemapMin1", Range(0.0, 1.0)) = 0.0
        _SmoothnessRemapMin2("SmoothnessRemapMin2", Range(0.0, 1.0)) = 0.0
        _SmoothnessRemapMin3("SmoothnessRemapMin3", Range(0.0, 1.0)) = 0.0

        _SmoothnessRemapMax0("SmoothnessRemapMax0", Range(0.0, 1.0)) = 1.0
        _SmoothnessRemapMax1("SmoothnessRemapMax1", Range(0.0, 1.0)) = 1.0
        _SmoothnessRemapMax2("SmoothnessRemapMax2", Range(0.0, 1.0)) = 1.0
        _SmoothnessRemapMax3("SmoothnessRemapMax3", Range(0.0, 1.0)) = 1.0

        _AORemapMin0("AORemapMin0", Range(0.0, 1.0)) = 0.0
        _AORemapMin1("AORemapMin1", Range(0.0, 1.0)) = 0.0
        _AORemapMin2("AORemapMin2", Range(0.0, 1.0)) = 0.0
        _AORemapMin3("AORemapMin3", Range(0.0, 1.0)) = 0.0

        _AORemapMax0("AORemapMax0", Range(0.0, 1.0)) = 1.0
        _AORemapMax1("AORemapMax1", Range(0.0, 1.0)) = 1.0
        _AORemapMax2("AORemapMax2", Range(0.0, 1.0)) = 1.0
        _AORemapMax3("AORemapMax3", Range(0.0, 1.0)) = 1.0

        _MaskMap0("MaskMap0", 2D) = "white" {}
        _MaskMap1("MaskMap1", 2D) = "white" {}
        _MaskMap2("MaskMap2", 2D) = "white" {}
        _MaskMap3("MaskMap3", 2D) = "white" {}

        // All the following properties exist only in layered lit material

        // Layer blending options
        [ToggleUI] _UseHeightBasedBlend("UseHeightBasedBlend", Float) = 0.0
        _HeightTransition("Height Transition", Range(0, 1.0)) = 0.0

        // Following are builtin properties

        // Stencil state
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2 // StencilLightingUsage.RegularLighting
        [HideInInspector] _StencilWriteMask("_StencilWriteMask", Int) = 7 // StencilMask.Lighting  (fixed at compile time)
        [HideInInspector] _StencilRefMV("_StencilRefMV", Int) = 128 // StencilLightingUsage.RegularLighting  (fixed at compile time)
        [HideInInspector] _StencilWriteMaskMV("_StencilWriteMaskMV", Int) = 128 // StencilMask.ObjectsVelocity  (fixed at compile time)

        // Blending state
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _ZTestDepthEqualForOpaque("_ZTestDepthEqualForOpaque", Int) = 4 // Less equal
        [HideInInspector] _ZTestGBuffer("_ZTestGBuffer", Int) = 4

        [ToggleUI] _DoubleSidedEnable("Double sided enable", Float) = 0.0
        [Enum(Flip, 0, Mirror, 1, None, 2)] _DoubleSidedNormalMode("Double sided normal mode", Float) = 1
        [HideInInspector] _DoubleSidedConstants("_DoubleSidedConstants", Vector) = (1, 1, -1, 0)

        // Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
        // value that exist to identify if the GI emission need to be enabled.
        // In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
        // TODO: Fix the code in legacy unity so we can customize the behavior for GI
        _EmissionColor("Color", Color) = (1, 1, 1)

        // HACK: GI Baking system relies on some properties existing in the shader ("_MainTex", "_Cutoff" and "_Color") for opacity handling, so we need to store our version of those parameters in the hard-coded name the GI baking system recognizes.
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)

        [ToggleUI] _SupportDBuffer("Support DBuffer", Float) = 1.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal

    #pragma shader_feature _DOUBLESIDED_ON

    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR0 _LAYER_MAPPING_TRIPLANAR0
    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR1 _LAYER_MAPPING_TRIPLANAR1
    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR2 _LAYER_MAPPING_TRIPLANAR2
    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR3 _LAYER_MAPPING_TRIPLANAR3

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

    #pragma shader_feature _DENSITY_MODE
    #pragma shader_feature _HEIGHT_BASED_BLEND

    // TODO: define _LAYER_COUNT directly and support 2-4
    #pragma shader_feature _ _LAYEREDLIT_3_LAYERS _LAYEREDLIT_4_LAYERS

    #pragma shader_feature _DISABLE_DBUFFER

    //enable GPU instancing support
    #pragma multi_compile_instancing

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
    // Use surface gradient normal mapping as it handle correctly triplanar normal mapping and multiple UVSet
    #define SURFACE_GRADIENT
    // This shader support vertex modification
    #define HAVE_VERTEX_MODIFICATION // TODO: Implement ApplyVertexModification for terrain heightmap sampling

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "CoreRP/ShaderLibrary/Common.hlsl"
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
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDLitShader" }

        // Caution: The outline selection in the editor use the vertex shader/hull/domain shader of the first pass declare. So it should not bethe  meta pass.
        Pass
        {
            Name "GBuffer"  // Name is not used
            Tags { "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

            Cull [_CullMode]
            ZTest[_ZTestGBuffer]

            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            #define SHADERPASS SHADERPASS_GBUFFER
            #include "../../ShaderVariables.hlsl"
            #ifdef DEBUG_DISPLAY
            #include "../../Debug/DebugDisplay.hlsl"
            #endif
            #include "../../Material/Material.hlsl"
            #include "../Lit/ShaderPass/LitSharePass.hlsl"
            #include "TerrainLitData.hlsl"
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
            #include "../Lit/ShaderPass/LitSharePass.hlsl"
            #include "TerrainLitData.hlsl"
            #include "../../ShaderPass/ShaderPassLightTransport.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Motion Vectors"
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

            ZWrite On

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_VELOCITY
            #include "../../ShaderVariables.hlsl"
            #include "../../Material/Material.hlsl"
            #include "../Lit/ShaderPass/LitVelocityPass.hlsl"
            #include "TerrainLitData.hlsl"
            #include "../../ShaderPass/ShaderPassVelocity.hlsl"

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
            #include "../Lit/ShaderPass/LitDepthPass.hlsl"
            #include "TerrainLitData.hlsl"
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

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "../../ShaderVariables.hlsl"
            #include "../../Material/Material.hlsl"
            #include "../Lit/ShaderPass/LitDepthPass.hlsl"
            #include "TerrainLitData.hlsl"
            #include "../../ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Forward" // Name is not used
            Tags{ "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            // In case of forward we want to have depth equal for opaque mesh
            ZTest [_ZTestDepthEqualForOpaque]
            ZWrite [_ZWrite]
            Cull [_CullMode]

            HLSLPROGRAM

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // #include "../../Lighting/Forward.hlsl"
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
            #include "../Lit/ShaderPass/LitSharePass.hlsl"
            #include "TerrainLitData.hlsl"
            #include "../../ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "UnityEditor.Experimental.Rendering.HDPipeline.TerrainLitGUI"
}
