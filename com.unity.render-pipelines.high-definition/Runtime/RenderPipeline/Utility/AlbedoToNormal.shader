Shader "Hidden/HDRP/AlbedoToNormal"
{
    HLSLINCLUDE



#pragma target 4.5
#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    TEXTURE2D_X(_AlbedoTexture);

    CBUFFER_START(cb)
        float4 _Size;
    CBUFFER_END

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    float4 Frag(Varyings input) : SV_Target
    {

    }
        ENDHLSL

        SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

            Pass
        {
            Stencil
            {
                WriteMask 64
                ReadMask 64 // StencilBitMask.DistortionVectors
                Ref  64     // StencilBitMask.DistortionVectors
                Comp Equal
                Pass Zero   // We can clear the bit since we won't need anymore.
            }

            ZWrite Off ZTest Off Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
