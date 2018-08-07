Shader "Hidden/HDRenderPipeline/Material/Decal/DecalNormalBuffer"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../../ShaderVariables.hlsl"
        #include "Decal.hlsl"
        #include "../NormalBuffer.hlsl"

        TEXTURE2D(_DBufferTexture1);
        RW_TEXTURE2D(float4, _NormalBuffer);
        

        SamplerState sampler_PointClamp;

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
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);// *_BlitScaleBias.xy + _BlitScaleBias.zw;
            return output;
        }

        float4 FragNearest(Varyings input) : SV_Target
        {
            float4 DBufferNormal = SAMPLE_TEXTURE2D(_DBufferTexture1, sampler_PointClamp, input.texcoord) * float4(2.0f, 2.0f, 2.0f, 1.0f) - float4(1.0f, 1.0f, 1.0f, 0.0f);
            float4 GBufferNormal = _NormalBuffer[input.texcoord * _ScreenSize.xy];
            NormalData normalData;
            DecodeFromNormalBuffer(GBufferNormal, uint2(0, 0), normalData);
            normalData.normalWS.xyz = normalize(normalData.normalWS.xyz * DBufferNormal.w + DBufferNormal.xyz);
            EncodeIntoNormalBuffer(normalData, uint2(0, 0), GBufferNormal);
            _NormalBuffer[input.texcoord * _ScreenSize.xy] = GBufferNormal;
            return GBufferNormal;
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off
            Stencil
            {
                WriteMask 255
                Ref 2
                Comp Equal
                Pass Zero
            }

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragNearest
            ENDHLSL
        }
    }

    Fallback Off
}
