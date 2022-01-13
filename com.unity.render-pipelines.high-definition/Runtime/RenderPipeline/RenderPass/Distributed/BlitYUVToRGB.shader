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
            u = u - 0.5f;
			v = v - 0.5f;

			half r = y + 1.28033f * v;
			half g = y - 0.21482f * v - 0.38059f * u;
			half b = y + 2.12798f * u;

			return float4(r, g, b, 1);
        }

        float4 Frag(Varyings input, SamplerState s)
        {
            float y, u, v;
        #if defined(USE_TEXTURE2D_X_AS_ARRAY) && defined(BLIT_SINGLE_SLICE)
            y = SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTextureY, s, input.texcoord.xy, _BlitTexArraySlice, 0).r;
            u = SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTextureU, s, input.texcoord.xy, _BlitTexArraySlice, 0).r;
            v = SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTextureV, s, input.texcoord.xy, _BlitTexArraySlice, 0).r;
        #endif

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            y = SAMPLE_TEXTURE2D_X_LOD(_BlitTextureY, s, input.texcoord.xy, 0).r;
            u = SAMPLE_TEXTURE2D_X_LOD(_BlitTextureU, s, input.texcoord.xy, 0).r;
            v = SAMPLE_TEXTURE2D_X_LOD(_BlitTextureV, s, input.texcoord.xy, 0).r;

            return YUVToRGB(y, u, v);
        }

        float4 FragRGB(Varyings input) : SV_Target
        {
            return Frag(input, sampler_LinearClamp);
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
                #pragma fragment FragRGB
            ENDHLSL
        }

    }

    Fallback Off
}
