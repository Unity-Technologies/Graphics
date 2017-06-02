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

            #include "../../ShaderLibrary/Common.hlsl"

            #define SHADOW_TILEPASS // TODO: Not sure it must be define, ask uygar
            #include "../../ShaderLibrary/Shadow/Shadow.hlsl"
            #undef SHADOW_TILEPASS

            SamplerState ltc_linear_clamp_sampler;

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
                ShadowContext shadowContext = InitShadowContext();

                // Caution: ShadowContext is define in Shadowcontext.hlsl for current render pipeline. This shader must be in sync with its content else it doesn't work.
                return SAMPLE_TEXTURE2D_ARRAY(_ShadowmapExp_PCF, ltc_linear_clamp_sampler, input.texcoord, 0).xxxx;
            }

            ENDHLSL
        }

    }
    Fallback Off
}
