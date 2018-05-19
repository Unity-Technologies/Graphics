Shader "Hidden/LightweightPipeline/CopyDepth"
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
            Name "Default"
            ZTest Always ZWrite On ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile __ _MSAA_DEPTH

            #include "LWRP/ShaderLibrary/DepthCopy.hlsl"

            ENDHLSL
        }
    }
}
