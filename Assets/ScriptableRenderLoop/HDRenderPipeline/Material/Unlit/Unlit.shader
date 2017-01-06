Shader "HDRenderPipeline/Unlit"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _ColorMap("ColorMap", 2D) = "white" {}

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

        [Enum(None, 0, DoubleSided, 1)] _DoubleSidedMode("Double sided mode", Float) = 0
    }

    HLSLINCLUDE

    #pragma target 5.0
    #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _EMISSIVE_COLOR_MAP

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    #define UNITY_MATERIAL_UNLIT // Need to be define before including Material.hlsl

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "common.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderConfig.cs.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderVariables.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderPipeline/Material/Attributes.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderPass/ShaderPass.cs.hlsl"    

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    float4  _Color;
	TEXTURE2D(_ColorMap);
	SAMPLER2D(sampler_ColorMap);

    float3 _EmissiveColor;
	TEXTURE2D(_EmissiveColorMap);
	SAMPLER2D(sampler_EmissiveColorMap);

    float _EmissiveIntensity;

    float _AlphaCutoff;

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300

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
            #include "../../Material/Material.hlsl"
            #include "UnlitData.hlsl"
            #include "UnlitSharePass.hlsl"
            
            #include "../../ShaderPass/ShaderPassDebugViewMaterial.hlsl"

            ENDHLSL
        }

        // ------------------------------------------------------------------
        //  forward pass
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "ForwardUnlit" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_CullMode]

            HLSLPROGRAM

            #pragma vertex VertDefault
            #pragma fragment Frag

            #define SHADERPASS SHADERPASS_FORWARD_UNLIT
            #include "../../Material/Material.hlsl"
            #include "UnlitData.hlsl"
            #include "UnlitSharePass.hlsl"
            
            #include "../../ShaderPass/ShaderPassForwardUnlit.hlsl"

            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        // ------------------------------------------------------------------
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
            #include "../../Material/Material.hlsl"
            #include "UnlitData.hlsl"

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

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS;
                float2 texCoord0;
                float2 texCoord1;
            };

            struct PackedVaryings
            {
                float4 positionCS : SV_Position;
                float4 interpolators[1] : TEXCOORD0;
            };

            // Function to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
            PackedVaryings PackVaryings(Varyings input)
            {
                PackedVaryings output;
                output.positionCS = input.positionCS;
                output.interpolators[0].xy = input.texCoord0;
                output.interpolators[0].zw = input.texCoord1;

                return output;
            }

            FragInputs UnpackVaryings(PackedVaryings input)
            {
                FragInputs output;
                ZERO_INITIALIZE(FragInputs, output);

                output.unPositionSS = input.positionCS;
                output.texCoord0 = input.interpolators[0].xy;
                output.texCoord1 = input.interpolators[0].zw;

                return output;
            }

            PackedVaryings Vert(Attributes input)
            {
                Varyings output;

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
                output.positionCS = TransformWorldToHClip(positionWS);
                output.texCoord0 = input.uv0;
                output.texCoord1 = input.uv1;

                return PackVaryings(output);
            }


            #include "../../ShaderPass/ShaderPassLightTransport.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "Experimental.ScriptableRenderLoop.UnlitGUI"
}
