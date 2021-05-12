Shader "Hidden/Core/LensFlareDataDrivenPreview2"
{
    SubShader
    {
        // Additive
        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }

            Blend One One
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #define HDRP_FLARE
            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/LensFlareCommon2.hlsl"

            ENDHLSL
        }
    }
}
