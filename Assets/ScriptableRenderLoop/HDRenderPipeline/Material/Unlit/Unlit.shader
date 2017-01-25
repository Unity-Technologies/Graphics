Shader "HDRenderPipeline/Unlit"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _ColorMap("ColorMap", 2D) = "white" {}

        _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        _EmissiveIntensity("EmissiveIntensity", Float) = 0

        _DistortionVectorMap("DistortionVectorMap", 2D) = "black" {}

        [ToggleOff] _DistortionEnable("Enable Distortion", Float) = 0.0
        [ToggleOff] _DistortionOnly("Distortion Only", Float) = 0.0
        [ToggleOff] _DistortionDepthTest("Distortion Depth Test Enable", Float) = 0.0

        [ToggleOff]  _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _ZTestMode("_ZTestMode", Int) = 8

        [Enum(None, 0, DoubleSided, 1)] _DoubleSidedMode("Double sided mode", Float) = 0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 metal  // TEMP: unitl we go futher in dev

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _DISTORTION_ON
    #pragma shader_feature _ _DOUBLESIDED

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
    #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderPass/FragInputs.hlsl"
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

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

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

            #define SHADERPASS SHADERPASS_DEBUG_VIEW_MATERIAL
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/UnlitSharePass.hlsl"            
            #include "UnlitData.hlsl"
            #include "../../ShaderPass/ShaderPassDebugViewMaterial.hlsl"

            ENDHLSL
        }

        // ------------------------------------------------------------------
        //  forward pass
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "Forward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_CullMode]

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_FORWARD_UNLIT
            #include "../../Material/Material.hlsl"
            #include "ShaderPass/UnlitSharePass.hlsl"
            #include "UnlitData.hlsl"
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

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #include "../../Material/Material.hlsl"   
            #include "ShaderPass/UnlitMetaPass.hlsl"
            #include "UnlitData.hlsl"                     
            #include "../../ShaderPass/ShaderPassLightTransport.hlsl"

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
            #include "ShaderPass/UnlitSharePass.hlsl"
            #include "UnlitData.hlsl"
            #include "../../ShaderPass/ShaderPassDistortion.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "Experimental.Rendering.HDPipeline.UnlitGUI"
}
