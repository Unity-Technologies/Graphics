Shader "HDRenderLoop/LayeredLit"
{
    Properties
    {
        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

        // Reminder. Color here are in linear but the UI (color picker) do the conversion sRGB to linear
        _BaseColor0("BaseColor0", Color) = (1,1,1,1)
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

        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace("NormalMap space", Float) = 0

        _HeightMap0("HeightMap0", 2D) = "black" {}
        _HeightMap1("HeightMap1", 2D) = "black" {}
        _HeightMap2("HeightMap2", 2D) = "black" {}
        _HeightMap3("HeightMap3", 2D) = "black" {}

        _HeightScale0("Height Scale0", Float) = 1
        _HeightScale1("Height Scale1", Float) = 1
        _HeightScale2("Height Scale2", Float) = 1
        _HeightScale3("Height Scale3", Float) = 1

        _HeightBias0("Height Bias0", Float) = 0
        _HeightBias1("Height Bias1", Float) = 0
        _HeightBias2("Height Bias2", Float) = 0
        _HeightBias3("Height Bias3", Float) = 0

        [Enum(Parallax, 0, Displacement, 1)] _HeightMapMode("Heightmap usage", Float) = 0

        _EmissiveColor0("EmissiveColor0", Color) = (0, 0, 0)
        _EmissiveColor1("EmissiveColor1", Color) = (0, 0, 0)
        _EmissiveColor2("EmissiveColor2", Color) = (0, 0, 0)
        _EmissiveColor3("EmissiveColor3", Color) = (0, 0, 0)

        _EmissiveColorMap0("EmissiveColorMap0", 2D) = "white" {}
        _EmissiveColorMap1("EmissiveColorMap1", 2D) = "white" {}
        _EmissiveColorMap2("EmissiveColorMap2", 2D) = "white" {}
        _EmissiveColorMap3("EmissiveColorMap3", 2D) = "white" {}

        _EmissiveIntensity0("EmissiveIntensity0", Float) = 0
        _EmissiveIntensity1("EmissiveIntensity1", Float) = 0
        _EmissiveIntensity2("EmissiveIntensity2", Float) = 0
        _EmissiveIntensity3("EmissiveIntensity3", Float) = 0

        _LayerMaskMap("LayerMaskMap", 2D) = "white" {}

        [ToggleOff]     _DistortionOnly("Distortion Only", Float) = 0.0
        [ToggleOff]     _DistortionDepthTest("Distortion Only", Float) = 0.0

        [ToggleOff]  _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0

        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode ("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        // Material Id
        [HideInInspector] _MaterialId("_MaterialId", FLoat) = 0

        [HideInInspector] _LayerCount("__layerCount", Float) = 2.0

        [Enum(Mask Alpha, 0, BaseColor Alpha, 1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 1
        [Enum(Use Emissive Color, 0, Use Emissive Mask, 1)] _EmissiveColorMode("Emissive color mode", Float) = 1
        [Enum(None, 0, DoubleSided, 1, DoubleSidedLigthingFlip, 2, DoubleSidedLigthingMirror, 3)] _DoubleSidedMode("Double sided mode", Float) = 0
    }

    HLSLINCLUDE

    #pragma target 5.0
    #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _ _DOUBLESIDED_LIGHTING_FLIP _DOUBLESIDED_LIGHTING_MIRROR
    #pragma shader_feature _NORMALMAP
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE
    #pragma shader_feature _MASKMAP
    #pragma shader_feature _SPECULAROCCLUSIONMAP
    #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    #pragma shader_feature _EMISSIVE_COLOR
    #pragma shader_feature _EMISSIVE_COLOR_MAP
    #pragma shader_feature _HEIGHTMAP
    #pragma shader_feature _HEIGHTMAP_AS_DISPLACEMENT
    #pragma shader_feature _LAYERMASKMAP
    #pragma shader_feature _ _LAYEREDLIT_3_LAYERS _LAYEREDLIT_4_LAYERS

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "common.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/ShaderConfig.cs.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/ShaderVariables.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/ShaderPass/ShaderPass.cs.hlsl"    
    #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Debug/DebugViewMaterial.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    // Set of users variables
    #define PROP_DECL(type, name) type name, name##0, name##1, name##2, name##3;
    #define PROP_DECL_TEX2D(name)\
        TEXTURE2D(name##0); \
        SAMPLER2D(sampler##name##0); \
        TEXTURE2D(name##1); \
        TEXTURE2D(name##2); \
        TEXTURE2D(name##3);
    #define PROP_SAMPLE(name, textureName, texcoord, swizzle)\
        name##0 = SAMPLE_TEXTURE2D(textureName##0, sampler##textureName##0, texcoord).##swizzle; \
        name##1 = SAMPLE_TEXTURE2D(textureName##1, sampler##textureName##0, texcoord).##swizzle; \
        name##2 = SAMPLE_TEXTURE2D(textureName##2, sampler##textureName##0, texcoord).##swizzle; \
        name##3 = SAMPLE_TEXTURE2D(textureName##3, sampler##textureName##0, texcoord).##swizzle;
    #define PROP_MUL(name, multiplier, swizzle)\
        name##0 *= multiplier##0.##swizzle; \
        name##1 *= multiplier##1.##swizzle; \
        name##2 *= multiplier##2.##swizzle; \
        name##3 *= multiplier##3.##swizzle;
    #define PROP_ASSIGN(name, input, swizzle)\
        name##0 = input##0.##swizzle; \
        name##1 = input##1.##swizzle; \
        name##2 = input##2.##swizzle; \
        name##3 = input##3.##swizzle;
    #define PROP_ASSIGN_VALUE(name, input)\
        name##0 = input; \
        name##1 = input; \
        name##2 = input; \
        name##3 = input;
    #define PROP_BLEND_COLOR(name, mask) name = BlendLayeredColor(name##0, name##1, name##2, name##3, mask);
    #define PROP_BLEND_SCALAR(name, mask) name = BlendLayeredScalar(name##0, name##1, name##2, name##3, mask);

    #define _MAX_LAYER 4

    #if defined(_LAYEREDLIT_4_LAYERS)
    #   define _LAYER_COUNT 4
    #elif defined(_LAYEREDLIT_3_LAYERS)
    #   define _LAYER_COUNT 3
    #else
    #   define _LAYER_COUNT 2
    #endif

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    // Set of users variables
    PROP_DECL(float4, _BaseColor);
    PROP_DECL_TEX2D(_BaseColorMap);
    PROP_DECL(float, _Metallic);
    PROP_DECL(float, _Smoothness);
    PROP_DECL_TEX2D(_MaskMap);
    PROP_DECL_TEX2D(_SpecularOcclusionMap);
    PROP_DECL_TEX2D(_NormalMap);
    PROP_DECL_TEX2D(_Heightmap);
    PROP_DECL(float, _HeightScale);
    PROP_DECL(float, _HeightBias);
    PROP_DECL(float4, _EmissiveColor);
    PROP_DECL(float, _EmissiveIntensity);

    float _AlphaCutoff;
    TEXTURE2D(_LayerMaskMap);
    SAMPLER2D(sampler_LayerMaskMap);

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

            #pragma vertex VertDefault
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_GBUFFER
            #define LAYERED_LIT_SHADER

            #include "../../Material/Material.hlsl"
            #include "../Lit/LitData.hlsl"
            #include "../Lit/LitSharePass.hlsl"    

            #include "../../ShaderPass/ShaderPassGBuffer.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Debug"
            Tags{ "LightMode" = "DebugViewMaterial" }

            Cull[_CullMode]

            HLSLPROGRAM

            #pragma vertex VertDefault
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_DEBUG_VIEW_MATERIAL
            #define LAYERED_LIT_SHADER

            #include "../../Material/Material.hlsl"
            #include "../Lit/LitData.hlsl"
            #include "../Lit/LitSharePass.hlsl"
            #include "../Lit/LitDebugPass.hlsl"

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

            #pragma vertex Vert
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #define LAYERED_LIT_SHADER
            #include "../../Material/Material.hlsl"
            #include "../Lit/LitData.hlsl"
            #include "../Lit/LitMetaPass.hlsl"

            #include "../../ShaderPass/ShaderPassLightTransport.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Motion Vectors"
            Tags{ "LightMode" = "MotionVectors" } // Caution, this need to be call like this to setup the correct parameters by C++ (legacy Unity)

            Cull[_CullMode]

            ZTest LEqual
            ZWrite Off // TODO: Test Z equal here.

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_VELOCITY
            #define LAYERED_LIT_SHADER
            #include "../../Material/Material.hlsl"         
            #include "../Lit/LitData.hlsl"
            #include "../Lit/LitVelocityPass.hlsl"

            #include "../../ShaderPass/ShaderPassVelocity.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }

            Cull[_CullMode]

            ZWrite On ZTest LEqual

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define LAYERED_LIT_SHADER
            #include "../../Material/Material.hlsl"
            #include "../Lit/LitData.hlsl"
            #include "../Lit/LitDepthPass.hlsl"

            #include "../../ShaderPass/ShaderPassDepthOnly.hlsl"

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

            #pragma vertex VertDefault
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_FORWARD
            // TEMP until pragma work in include
            // #include "../../Lighting/Forward.hlsl"
            #pragma multi_compile LIGHTLOOP_SINGLE_PASS
            #define LAYERED_LIT_SHADER
            //#pragma multi_compile SHADOWFILTERING_FIXED_SIZE_PCF

            #include "../../Lighting/Lighting.hlsl"
            #include "../Lit/LitData.hlsl"
            #include "../Lit/LitSharePass.hlsl"

            #include "../../ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "LayeredLitGUI"
}
