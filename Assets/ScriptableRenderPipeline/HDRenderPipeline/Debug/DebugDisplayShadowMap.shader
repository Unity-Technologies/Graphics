Shader "Hidden/HDRenderPipeline/DebugDisplayShadowMap"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 metal // TEMP: unitl we go futher in dev

        #include "../../ShaderLibrary/Common.hlsl"

        #define SHADOW_TILEPASS // TODO: Not sure it must be define, ask uygar
        #include "../../ShaderLibrary/Shadow/Shadow.hlsl"
        #undef SHADOW_TILEPASS

        float4 _TextureScaleBias;
        SamplerState ltc_linear_clamp_sampler;
        TEXTURE2D_ARRAY(_AtlasTexture);

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
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "RegularShadow"
            ZTest Off
            Blend One Zero
            Cull Off
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragAtlas
            
            float4 FragAtlas(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D_ARRAY(_AtlasTexture, ltc_linear_clamp_sampler, input.texcoord, 0).xxxx;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
