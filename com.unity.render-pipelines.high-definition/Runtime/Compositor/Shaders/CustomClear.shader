Shader "Hidden/HDRP/CustomClear"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        TEXTURE2D(_BlitTexture);
        SamplerState sampler_PointClamp;
        SamplerState sampler_LinearClamp;
        float4 _BlitScaleBias;
        float4 _BlitScaleBiasRt;
        float _BlitMipLevel;
        int _ClearAlpha;

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
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
            return output;
        }

        Varyings VertQuad(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetQuadVertexPosition(input.vertexID) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
            output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            output.texcoord = GetQuadTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
            return output;
        }

        float4 ClearColorAndAlphaToZero(Varyings input) : SV_Target
        {
            return float4(0.0f, 0.0f, 0.0f, 0.0f);
        }

        float4 ClearUsingTexture(Varyings input) : SV_Target
        {
            float4 color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord.xy, _BlitMipLevel);
            return float4(color.xyz, _ClearAlpha == 0 ? color.w : 0.0f);
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // 0: Clear color, alpha and stencil to zero
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off
            Stencil
            {
                WriteMask 255
                Ref 0
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment ClearColorAndAlphaToZero
            ENDHLSL
        }

        // 1: Clears the color using the input texture and clears stencil to zero
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off
            Stencil
            {
                WriteMask 255
                Ref 0
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment ClearUsingTexture
            ENDHLSL
        }

    }

    Fallback Off
}
