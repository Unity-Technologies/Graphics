Shader "HDRenderLoop/Unlit"
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
    #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/ShaderConfig.cs"
    #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/ShaderVariables.hlsl"
    #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/ShaderPass/ShaderPass.cs.hlsl"    
    #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Debug/DebugViewMaterial.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    float4  _Color;
    UNITY_DECLARE_TEX2D(_ColorMap);
    float3 _EmissiveColor;
    UNITY_DECLARE_TEX2D(_EmissiveColorMap);
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
            #include "UnlitShare.hlsl"

            void GetVaryingsDataDebug(uint paramId, FragInput input, inout float3 result, inout bool needLinearToSRGB)
            {
                switch (paramId)
                {
                case DEBUGVIEW_VARYING_DEPTH:
                    // TODO: provide a customize parameter (like a slider)
                    float linearDepth = frac(LinearEyeDepth(input.positionCS.z, _ZBufferParams) * 0.1);
                    result = linearDepth.xxx;
                    break;
                case DEBUGVIEW_VARYING_TEXCOORD0:
                    result = float3(input.texCoord0 * 0.5 + 0.5, 0.0);
                    break;
                }
            }
            
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
            #include "UnlitShare.hlsl"
            
            #include "../../ShaderPass/ShaderPassForwardUnlit.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "UnlitGUI"
}
