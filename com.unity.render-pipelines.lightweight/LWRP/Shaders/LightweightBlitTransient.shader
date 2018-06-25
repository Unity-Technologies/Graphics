Shader "Hidden/LightweightPipeline/BlitTransient"
{
    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
        LOD 100

        Pass
        {
            Name "Default"
            Tags{"LightMode" = "LightweightForward"}

            ZTest Always ZWrite Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "LWRP/ShaderLibrary/Core.hlsl"

            UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(0);

            float4 Vertex(float4 vertexPosition : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(vertexPosition.xyz);
            }

            half4 Fragment(float4 pos : SV_POSITION) : COLOR0
            {
                return UNITY_READ_FRAMEBUFFER_INPUT(0, pos);
            }
            ENDHLSL
        }
    }
}
