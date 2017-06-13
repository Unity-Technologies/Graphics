Shader "Hidden/ScriptableRenderPipeline/DebugDisplayShadowMap"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 metal // TEMP: unitl we go futher in dev

        #include "../../../ShaderLibrary/Common.hlsl"

        float4 _TextureScaleBias;
        float _TextureSlice;
        float2 _ValidRange;
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
            #pragma fragment FragRegular
            
            float4 FragRegular(Varyings input) : SV_Target
            {
                return saturate( (SAMPLE_TEXTURE2D_ARRAY(_AtlasTexture, ltc_linear_clamp_sampler, input.texcoord, _TextureSlice).x - _ValidRange.x) * _ValidRange.y ).xxxx;
            }

            ENDHLSL
        }

        Pass
        {
            Name "VarianceShadow"
            ZTest Off
            Blend One Zero
            Cull Off
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragVariance
            
            float4 FragVariance(Varyings input) : SV_Target
            {
                return saturate((SAMPLE_TEXTURE2D_ARRAY(_AtlasTexture, ltc_linear_clamp_sampler, input.texcoord, _TextureSlice).x - _ValidRange.x) * _ValidRange.y).xxxx;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
