Shader "HDRP/PostProcess"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PostProcessing/ShaderVariablesPostProcessing.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PostProcessing/PostProcessingProperties.hlsl"

    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "PostProcessShader"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #define SHADERPASS SHADERPASS_POSTPROCESS
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PostProcessing/ShaderPass/PostProcessSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PostProcessing/PostProcessData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPostProcess.hlsl"

            ENDHLSL
        }
    }
    Fallback Off
}
