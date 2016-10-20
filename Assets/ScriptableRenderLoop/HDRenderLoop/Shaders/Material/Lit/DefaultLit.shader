Shader "Unity/Lit"
{
    Properties
    {
        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

        // Reminder. Color here are in linear but the UI (color picker) do the conversion sRGB to linear
        _BaseColor("BaseColor", Color) = (1,1,1,1)
        _BaseColorMap("BaseColorMap", 2D) = "white" {}

        _Metalic("_Metalic", Range(0.0, 1.0)) = 0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _MaskMap("MaskMap", 2D) = "white" {}

        _SpecularOcclusionMap("SpecularOcclusion", 2D) = "white" {}

        _NormalMap("NormalMap", 2D) = "bump" {}
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace("NormalMap space", Float) = 0

        _HeightMap("HeightMap", 2D) = "black" {}
        _HeightScale("Height Scale", Float) = 1
        _HeightBias("Height Bias", Float) = 0
        [Enum(Parallax, 0, Displacement, 1)] _HeightMapMode("Heightmap usage", Float) = 0

        _SubSurfaceRadius("SubSurfaceRadius", Range(0.0, 1.0)) = 0
        _SubSurfaceRadiusMap("SubSurfaceRadiusMap", 2D) = "white" {}
        //_Thickness("Thickness", Range(0.0, 1.0)) = 0
        //_ThicknessMap("ThicknessMap", 2D) = "white" {}
        //_SubSurfaceProfile("SubSurfaceProfile", Float) = 0

        //_CoatCoverage("CoatCoverage", Range(0.0, 1.0)) = 0
        //_CoatCoverageMap("CoatCoverageMapMap", 2D) = "white" {}

        //_CoatRoughness("CoatRoughness", Range(0.0, 1.0)) = 0
        //_CoatRoughnessMap("CoatRoughnessMap", 2D) = "white" {}

        // _DistortionVectorMap("DistortionVectorMap", 2D) = "white" {}
        // _DistortionBlur("DistortionBlur", Range(0.0, 1.0)) = 0

        // Following options are for the GUI inspector and different from the input parameters above
        // These option below will cause different compilation flag.

        _DiffuseLightingMap("DiffuseLightingMap", 2D) = "black" {}
        _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        _EmissiveIntensity("EmissiveIntensity", Float) = 0

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

        [Enum(Mask Alpha, 0, BaseColor Alpha, 1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 1
        [Enum(Use Emissive Color, 0, Use Emissive Mask, 1)] _EmissiveColorMode("Emissive color mode", Float) = 1
        [Enum(None, 0, DoubleSided, 1, DoubleSidedLigthingFlip, 2, DoubleSidedLigthingMirror, 3)] _DoubleSidedMode("Double sided mode", Float) = 0
    }

    HLSLINCLUDE

    #pragma target 5.0
    #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------

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

    #pragma multi_compile _ LIGHTMAP_ON
    #pragma multi_compile _ DIRLIGHTMAP_COMBINED

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------
    #include "common.hlsl"
    #include "../../ShaderPass/ShaderPass.cs.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    // Set of users variables
    float4 _BaseColor;
    UNITY_DECLARE_TEX2D(_BaseColorMap);

    float _Metalic;
    float _Smoothness;
    UNITY_DECLARE_TEX2D(_MaskMap);
    UNITY_DECLARE_TEX2D(_SpecularOcclusionMap);

    UNITY_DECLARE_TEX2D(_NormalMap);
    UNITY_DECLARE_TEX2D(_Heightmap);
    float _HeightScale;
    float _HeightBias;

    UNITY_DECLARE_TEX2D(_DiffuseLightingMap);
    float4 _EmissiveColor;
    UNITY_DECLARE_TEX2D(_EmissiveColorMap);
    float _EmissiveIntensity;

    float _SubSurfaceRadius;
    UNITY_DECLARE_TEX2D(_SubSurfaceRadiusMap);
    // float _Thickness;
    // UNITY_DECLARE_TEX2D(_ThicknessMap);

    // float _CoatCoverage;
    // UNITY_DECLARE_TEX2D(_CoatCoverageMap);

    // float _CoatRoughness;
    // UNITY_DECLARE_TEX2D(_CoatRoughnessMap);

    float _AlphaCutoff;

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300

        // ------------------------------------------------------------------
        //  Deferred pass
        Pass
        {
            Name "GBuffer"  // Name is not used
            Tags { "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

            Cull  [_CullMode]

            HLSLPROGRAM

            #pragma vertex VertDefault
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_GBUFFER
            #include "LitCommon.hlsl"

            #include "../../ShaderPass/ShaderPassGBuffer.hlsl"

            ENDHLSL
        }

        // ------------------------------------------------------------------
        //  Debug pass
        Pass
        {
            Name "Debug"
            Tags { "LightMode" = "DebugViewMaterial" }

            Cull[_CullMode]

            HLSLPROGRAM

            #pragma vertex VertDefault
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_DEBUG_VIEW_MATERIAL
            #include "LitCommon.hlsl"

            void GetVaryingsDataDebug(uint paramId, Varyings input, inout float3 result, inout bool needLinearToSRGB)
            {
                switch (paramId)
                {
                case DEBUGVIEW_VARYING_DEPTH:
                    // TODO: provide a customize parameter (like a slider)
                    float linearDepth = frac(LinearEyeDepth(input.positionHS.z, _ZBufferParams) * 0.1);
                    result = linearDepth.xxx;
                    break;
                case DEBUGVIEW_VARYING_TEXCOORD0:
                    result = float3(input.texCoord0 * 0.5 + 0.5, 0.0);
                    break;
                case DEBUGVIEW_VARYING_TEXCOORD1:
                    result = float3(input.texCoord1 * 0.5 + 0.5, 0.0);
                    break;
                case DEBUGVIEW_VARYING_TEXCOORD2:
                    result = float3(input.texCoord2 * 0.5 + 0.5, 0.0);
                    break;
                case DEBUGVIEW_VARYING_VERTEXTANGENTWS:
                    result = input.tangentToWorld[0].xyz * 0.5 + 0.5;
                    break;
                case DEBUGVIEW_VARYING_VERTEXBITANGENTWS:
                    result = input.tangentToWorld[1].xyz * 0.5 + 0.5;
                    break;
                case DEBUGVIEW_VARYING_VERTEXNORMALWS:
                    result = input.tangentToWorld[2].xyz * 0.5 + 0.5;
                    break;
                }
            }
            
            #include "../../ShaderPass/ShaderPassDebugViewMaterial.hlsl"

            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags{ "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM

            #pragma vertex VertLT
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #include "LitCommon.hlsl"

            CBUFFER_START(UnityMetaPass)
            // x = use uv1 as raster position
            // y = use uv2 as raster position
            bool4 unity_MetaVertexControl;

            // x = return albedo
            // y = return normal
            bool4 unity_MetaFragmentControl;

            CBUFFER_END

            // This was not in constant buffer in original unity, so keep outiside. But should be in as ShaderRenderPass frequency
            float unity_OneOverOutputBoost;
            float unity_MaxOutputValue;

            struct VaryingsLT
            {
                float4 positionHS;
                float2 texCoord0;
                float2 texCoord1;
            };

            struct PackedVaryingsLT
            {
                float4 positionHS : SV_Position;
                float4 interpolators[1] : TEXCOORD0;
            };

            // Function to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
            PackedVaryingsLT PackVaryings(VaryingsLT input)
            {
                PackedVaryingsLT output;
                output.positionHS = input.positionHS;
                output.interpolators[0].xy = input.texCoord0;
                output.interpolators[0].zw = input.texCoord1;

                return output;
            }

            VaryingsLT UnpackVaryings(PackedVaryingsLT input)
            {
                VaryingsLT output;
                output.positionHS = input.positionHS;
                output.texCoord0 = input.interpolators[0].xy;
                output.texCoord1 = input.interpolators[0].zw;

                return output;
            }

            PackedVaryingsLT VertLT(Attributes input)
            {
                VaryingsLT output;

                // Output UV coordinate in vertex shader
                if (unity_MetaVertexControl.x)
                {
                    input.positionOS.xy = input.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
                    // OpenGL right now needs to actually use incoming vertex position,
                    // so use it in a very dummy way
                    //v.positionOS.z = vertex.z > 0 ? 1.0e-4f : 0.0f;
                }
                if (unity_MetaVertexControl.y)
                {
                    input.positionOS.xy = input.uv2 * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                    // OpenGL right now needs to actually use incoming vertex position,
                    // so use it in a very dummy way
                    //v.positionOS.z = vertex.z > 0 ? 1.0e-4f : 0.0f;
                }

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionHS = TransformWorldToHClip(positionWS);
                output.texCoord0 = input.uv0;
                output.texCoord1 = input.uv1;
                return PackVaryings(output);
            }

            #define GetSurfaceAndBuiltinData GetSurfaceAndBuiltinDataLT
            #define Varyings VaryingsLT
            #define PackedVaryings PackedVaryingsLT
            #include "../../ShaderPass/ShaderPassLightTransport.hlsl"

            ENDHLSL
        }

        // ------------------------------------------------------------------
        //  forward pass
        Pass
        {
            Name "Forward" // Name is not used
            Tags { "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_CullMode]

            HLSLPROGRAM

            #pragma vertex VertDefault
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_FORWARD
            #include "LitCommon.hlsl"

            #include "../../ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "LitGUI"
}
