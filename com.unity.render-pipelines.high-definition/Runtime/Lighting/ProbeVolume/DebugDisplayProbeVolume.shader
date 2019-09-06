Shader "Hidden/ScriptableRenderPipeline/DebugDisplayProbeVolume"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

        float4  _TextureScaleBias;
        float2  _ValidRange;
        // float   _RcpGlobalScaleFactor;
        SamplerState ltc_linear_clamp_sampler;
        TEXTURE2D(_AtlasTextureShAr);
        TEXTURE2D(_AtlasTextureShAg);
        TEXTURE2D(_AtlasTextureShAb);

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
            output.texcoord = output.texcoord * _TextureScaleBias.xy + _TextureScaleBias.zw;
            return output;
        }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "ProbeVolume"
            ZTest Off
            Blend One Zero
            Cull Off
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings input) : SV_Target
            {
                float4 valueShAr = saturate((SAMPLE_TEXTURE2D(_AtlasTextureShAr, ltc_linear_clamp_sampler, input.texcoord) - _ValidRange.x) * _ValidRange.y);
                float4 valueShAg = saturate((SAMPLE_TEXTURE2D(_AtlasTextureShAg, ltc_linear_clamp_sampler, input.texcoord) - _ValidRange.x) * _ValidRange.y);
                float4 valueShAb = saturate((SAMPLE_TEXTURE2D(_AtlasTextureShAb, ltc_linear_clamp_sampler, input.texcoord) - _ValidRange.x) * _ValidRange.y);

                return float4(valueShAr.x, valueShAg.x, valueShAb.x, 1);
            }

            ENDHLSL
        }

    }
    Fallback Off
}
