Shader "Hidden/HDRP/Blit"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 playstation xboxone vulkan metal switch
        #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY
        #pragma multi_compile _ BLIT_SINGLE_SLICE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        TEXTURE2D_X(_BlitTexture);
        SamplerState sampler_PointClamp;
        SamplerState sampler_LinearClamp;
        SamplerState sampler_PointRepeat;
        SamplerState sampler_LinearRepeat;
        uniform float4 _BlitScaleBias;
        uniform float4 _BlitScaleBiasRt;
        uniform float _BlitMipLevel;
        uniform float2 _BlitTextureSize;
        uniform uint _BlitPaddingSize;
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
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
            return output;
        }

        Varyings VertQuad(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetQuadVertexPosition(input.vertexID) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
            output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            output.texcoord = GetQuadTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
            return output;
        }

        Varyings VertQuadPadding(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            float2 scalePadding = ((_BlitTextureSize + float(_BlitPaddingSize)) / _BlitTextureSize);
            float2 offsetPaddding = (float(_BlitPaddingSize) / 2.0) / (_BlitTextureSize + _BlitPaddingSize);

            output.positionCS = GetQuadVertexPosition(input.vertexID) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
            output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            output.texcoord = GetQuadTexCoord(input.vertexID);
            output.texcoord = (output.texcoord - offsetPaddding) * scalePadding;
            output.texcoord = output.texcoord * _BlitScaleBias.xy + _BlitScaleBias.zw;
            return output;
        }

        float4 Frag(Varyings input, SamplerState s)
        {
        #if defined(USE_TEXTURE2D_X_AS_ARRAY) && defined(BLIT_SINGLE_SLICE)
            return SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTexture, s, input.texcoord.xy, _BlitTexArraySlice, _BlitMipLevel);
        #endif

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, s, input.texcoord.xy, _BlitMipLevel);
        }

        float4 FragNearest(Varyings input) : SV_Target
        {
            return Frag(input, sampler_PointClamp);
        }

        float4 FragBilinear(Varyings input) : SV_Target
        {
            return Frag(input, sampler_LinearClamp);
        }

        float4 FragBilinearRepeat(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord.xy;
            return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
        }

        float4 FragNearestRepeat(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord.xy;
            return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointRepeat, uv, _BlitMipLevel);
        }

        float4 FragOctahedralBilinearRepeat(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float u = input.texcoord.x;
            float v = input.texcoord.y;

            float2 uv;
            if (u < 0.0f)
            {
                if (v < 0.0f)
                    uv = float2(1.0f + u, 1.0f + v);
                else if (v < 1.0f)
                    uv = float2(-u, 1.0f - v);
                else
                    uv = float2(1.0f + u, v - 1.0f);
            }
            else if (u < 1.0f)
            {
                if (v < 0.0f)
                    uv = float2(1.0f - u, -v);
                else if (v < 1.0f)
                    uv = float2(u, v);
                else
                    uv = float2(1.0f - u, 2.0f - v);
            }
            else
            {
                if (v < 0.0f)
                    uv = float2(u - 1.0f, 1.0f + v);
                else if (v < 1.0f)
                    uv = float2(2.0f - u, 1.0f - v);
                else
                    uv = float2(u - 1.0f, v - 1.0f);
            }

            return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
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

        // 2: Nearest quad
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragNearest
            ENDHLSL
        }

        // 3: Bilinear quad
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinear
            ENDHLSL
        }

        // 4: Nearest quad with padding
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragNearest
            ENDHLSL
        }

        // 5: Bilinear quad with padding
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragBilinear
            ENDHLSL
        }

        // 6: Nearest quad with padding
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragNearestRepeat
            ENDHLSL
        }

        // 7: Bilinear quad with padding
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragBilinearRepeat
            ENDHLSL
        }

        // 8: Bilinear quad with padding (for OctahedralTexture)
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragOctahedralBilinearRepeat
            ENDHLSL
        }

        /// Version 4, 5, 6, 7 with Alpha Blending 0.5
        // 9: Nearest quad with padding alpha blend (4 with alpha blend)
        Pass
        {
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragNearest
                #define WITH_ALPHA_BLEND
            ENDHLSL
        }

        // 10: Bilinear quad with padding alpha blend (5 with alpha blend)
        Pass
        {
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragBilinear
                #define WITH_ALPHA_BLEND
            ENDHLSL
        }

        // 11: Nearest quad with padding alpha blend (6 with alpha blend)
        Pass
        {
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragNearestRepeat
                #define WITH_ALPHA_BLEND
            ENDHLSL
        }

        // 12: Bilinear quad with padding alpha blend (7 with alpha blend)
        Pass
        {
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragBilinearRepeat
                #define WITH_ALPHA_BLEND
            ENDHLSL
        }

        // 13: Bilinear quad with padding alpha blend (for OctahedralTexture) (8 with alpha blend)
        Pass
        {
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragOctahedralBilinearRepeat
                #define WITH_ALPHA_BLEND
            ENDHLSL
        }

    }

    Fallback Off
}
