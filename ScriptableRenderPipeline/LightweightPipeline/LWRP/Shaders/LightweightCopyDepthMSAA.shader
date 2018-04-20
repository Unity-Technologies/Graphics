Shader "Hidden/LightweightPipeline/CopyDepthMSAA"
{
    Properties
    {
        [HideInInspector] _SampleCount("MSAA sample count", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}

        Pass
        {
            ZTest Always ZWrite On ColorMask 0

            HLSLPROGRAM
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag

            #pragma require msaatex

            #define MSAA_DEPTH 1

            #include "LWRP/ShaderLibrary/DepthCopy.hlsl"

            ENDHLSL
        }
    }
}
