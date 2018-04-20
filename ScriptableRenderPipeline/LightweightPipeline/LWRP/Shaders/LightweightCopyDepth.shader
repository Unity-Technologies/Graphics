Shader "Hidden/LightweightPipeline/CopyDepth"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightiweightPipeline"}

        Pass
        {
            ZTest Always ZWrite On ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag

            #include "LWRP/ShaderLibrary/DepthCopy.hlsl"

            float frag(VertexOutput i) : SV_Depth
            {
                return SampleDepth(i.uv);
            }
            ENDHLSL
        }
    }
}
