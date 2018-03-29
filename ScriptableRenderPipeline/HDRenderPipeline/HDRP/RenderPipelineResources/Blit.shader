Shader "Hidden/HDRenderPipeline/Blit"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../ShaderVariables.hlsl"

        TEXTURE2D(_BlitTexture);
        SamplerState sampler_PointClamp;
        SamplerState sampler_LinearClamp;
        uniform float4 _BlitScaleBiasSrc;
		uniform float4 _BlitScaleBiasDst;
        uniform float _BlitMipLevel;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID) * float4(_BlitScaleBiasDst.xy, 1, 1) + float4(_BlitScaleBiasDst.zw, 0, 0);
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID) * float4(_BlitScaleBiasSrc.xy, 1, 1) + float4(_BlitScaleBiasSrc.zw, 0, 0);
            return output;
        }

        float4 FragNearest(Varyings input) : SV_Target
        {
            return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_PointClamp, input.texcoord, _BlitMipLevel);
        }

        float4 FragBilinear(Varyings input) : SV_Target
        {
            return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord, _BlitMipLevel);
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        
        // 0: Nearest
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragNearest
            ENDHLSL
        }

        // 1: Bilinear
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBilinear
            ENDHLSL
        }		
    }

    Fallback Off
}
