Shader "Hidden/HDRP/DebugBlitQuad"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        TEXTURE2D(_InputTexture);
        TEXTURE2D_ARRAY(_InputTextureArray);
        SAMPLER(sampler_InputTexture);
        SAMPLER(sampler_InputTextureArray);
        float _Mipmap;
        float _ApplyExposure;
        int   _ArrayIndex;

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
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);

            return output;
        }

        float4 Frag(Varyings input) : SV_Target
        {
            float3 color = color = SAMPLE_TEXTURE2D_LOD(_InputTexture, sampler_InputTexture, input.texcoord.xy, _Mipmap).rgb;
            return float4(color * (_ApplyExposure > 0.0 ? GetCurrentExposureMultiplier() : 1.0), 1.0);
        }

        float4 FragArray(Varyings input) : SV_Target
        {
            float3 color = SAMPLE_TEXTURE2D_ARRAY_LOD(_InputTextureArray, sampler_InputTextureArray, input.texcoord.xy, _ArrayIndex, _Mipmap).rgb;
            return float4(color * (_ApplyExposure > 0.0 ? GetCurrentExposureMultiplier() : 1.0), 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // 0: Texture
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }

        // 0: TextureArray
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragArray
            ENDHLSL
        }
    }
    Fallback Off
}
