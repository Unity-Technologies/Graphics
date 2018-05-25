Shader "Hidden/LightweightPipeline/CopyDepth"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}

        Pass
        {
            Name "Default"
            ZTest Always ZWrite On ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _DEPTH_NO_MSAA _DEPTH_MSAA_2 _DEPTH_MSAA_4

            #include "LWRP/ShaderLibrary/DepthCopy.hlsl"

            ENDHLSL
        }
    }
}
