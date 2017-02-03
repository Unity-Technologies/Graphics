Shader "Hidden/HDRenderPipeline/DebugDisplayShadowMap"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Off
            Blend One Zero
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: unitl we go futher in dev

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Common.hlsl"

            TEXTURE2D_FLOAT(g_tShadowBuffer);

            TEXTURE2D(_DummyTexture);
            SAMPLER2D(sampler_DummyTexture);

            float4 _TextureScaleBias;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
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
                output.texcoord   = GetFullScreenTriangleTexcoord(input.vertexID) * _TextureScaleBias.xy + _TextureScaleBias.zw;

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // We need the dummy texture for the sampler, but we also need to sample it in order not to get a compile error.
                float4 dummy = SAMPLE_TEXTURE2D(_DummyTexture, sampler_DummyTexture, input.texcoord) * 0.00001;
                return SAMPLE_TEXTURE2D(g_tShadowBuffer, sampler_DummyTexture, input.texcoord).xxxx + dummy;
            }

            ENDHLSL
        }

    }
    Fallback Off
}
