Shader "Hidden/Core/DebugOccluder"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch webgpu
        //#pragma enable_d3d11_debug_symbols

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

        float2 _ValidRange;
        SamplerState ltc_linear_clamp_sampler;

        struct Attributes
        {
            uint vertexID : VERTEXID_SEMANTIC;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        float GetOutputColor(float occluderValue)
        {
            float value = saturate((occluderValue - _ValidRange.x) * _ValidRange.y);
            return float4(value.xxx, 1);
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline"}
        Pass
        {
            Name "DebugOccluder"
            ZTest Off
            Blend One Zero
            Cull Off
            ZWrite On

            HLSLPROGRAM

            TEXTURE2D(_OccluderTexture);

            #pragma vertex Vert
            #pragma fragment Fragment

            float4 Fragment(Varyings input) : SV_Target
            {
                return GetOutputColor(SAMPLE_TEXTURE2D(_OccluderTexture, ltc_linear_clamp_sampler, input.texcoord).x);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
