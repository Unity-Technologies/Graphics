Shader "Hidden/HDRP/Distributed/BlitYUVToRGB"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY
        #pragma multi_compile _ BLIT_SINGLE_SLICE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        TEXTURE2D_X(_BlitTextureY);
        TEXTURE2D_X(_BlitTextureU);
        TEXTURE2D_X(_BlitTextureV);
        SamplerState sampler_PointClamp;
        SamplerState sampler_LinearClamp;
        SamplerState sampler_PointRepeat;
        SamplerState sampler_LinearRepeat;
        uniform int _BlitTexArraySlice;

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
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        float4 YUVToRGB(float y, float u, float v)
        {
            // BT.2020
            float a = 0.2627f;
            float b = 0.6780f;
            float c = 0.0593f;
            float d = 1.8814f;
            float e = 1.4747f;

            // BT.709
            // float a = 0.2126f;
            // float b = 0.7152f;
            // float c = 0.0722f;
            // float d = 1.8556f;
            // float e = 1.5748f;

            u = u - 0.5f;
            v = v - 0.5f;

            float R = y + e * v;
            float G = y - (a * e / b) * v - (c * d / b) * u;
            float B = y + d * u;
            //float3 rgb = max(float3(0, 0, 0), float3(R, G, B));
            //rgb = pow(rgb, 2.2f);
		    //return float4(rgb, 1);
            return float4(R, G, B, 1);
        }

        float4 FragYUV(Varyings input) : SV_Target
        {
            SamplerState s = sampler_LinearClamp;

            float y, u, v;

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        #if defined(USE_TEXTURE2D_X_AS_ARRAY) && defined(BLIT_SINGLE_SLICE)
            y = SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTextureY, s, input.texcoord.xy, _BlitTexArraySlice, 0).r;
            u = SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTextureU, s, input.texcoord.xy, _BlitTexArraySlice, 0).r;
            v = SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTextureV, s, input.texcoord.xy, _BlitTexArraySlice, 0).r;
        #else
            y = SAMPLE_TEXTURE2D_X_LOD(_BlitTextureY, s, input.texcoord.xy, 0).r;
            u = SAMPLE_TEXTURE2D_X_LOD(_BlitTextureU, s, input.texcoord.xy, 0).r;
            v = SAMPLE_TEXTURE2D_X_LOD(_BlitTextureV, s, input.texcoord.xy, 0).r;
        #endif

            return YUVToRGB(y, u, v);
        }

        float4 FragNV12(Varyings input) : SV_Target
        {
            SamplerState s = sampler_LinearClamp;

            float y;
            float2 uv;

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        #if defined(USE_TEXTURE2D_X_AS_ARRAY) && defined(BLIT_SINGLE_SLICE)
            y = SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTextureY, s, input.texcoord.xy, _BlitTexArraySlice, 0).r;
            uv = SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTextureU, s, input.texcoord.xy, _BlitTexArraySlice, 0).rg;
        #else
            y = SAMPLE_TEXTURE2D_X_LOD(_BlitTextureY, s, input.texcoord.xy, 0).r;
            uv = SAMPLE_TEXTURE2D_X_LOD(_BlitTextureU, s, input.texcoord.xy, 0).rg;
        #endif

            return YUVToRGB(y, uv.r, uv.g);
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragYUV
            ENDHLSL
        }

        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragNV12
            ENDHLSL
        }

    }

    Fallback Off
}
