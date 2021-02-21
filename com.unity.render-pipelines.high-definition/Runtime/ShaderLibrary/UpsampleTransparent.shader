Shader "Hidden/HDRP/UpsampleTransparent"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma multi_compile_local BILINEAR NEAREST_DEPTH
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

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
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        TEXTURE2D_X(_LowResTransparent);
#ifdef NEAREST_DEPTH
        TEXTURE2D_X_FLOAT(_LowResDepthTexture);

#define NEIGHBOUR_SEARCH 4
#define DEBUG_EDGE 0
#endif

        float4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;

            float2 fullResTexelSize = _ScreenSize.zw;
            float2 halfResTexelSize = 2.0f * fullResTexelSize;

        #ifdef NEAREST_DEPTH

            // The following is an implementation of NVIDIA's http://developer.download.nvidia.com/assets/gamedev/files/sdk/11/OpacityMappingSDKWhitePaper.pdf

            float4 lowResDepths = GATHER_RED_TEXTURE2D_X(_LowResDepthTexture, s_linear_clamp_sampler, ClampAndScaleUVForBilinear(uv, halfResTexelSize));

            // Gather UVs
            float2 topLeftUV = uv - 0.5f * halfResTexelSize;
            float2 UVs[NEIGHBOUR_SEARCH] = {
              topLeftUV + float2(0.0f,             halfResTexelSize.y),
              topLeftUV + float2(halfResTexelSize.x, halfResTexelSize.y),
              topLeftUV + float2(halfResTexelSize.x, 0.0f),
              topLeftUV,
            };

            float fullResDepth = LoadCameraDepth(input.positionCS.xy);
            float linearFullResDepth = LinearEyeDepth(fullResDepth, _ZBufferParams);

            float minDiff = 1e12f;
            float relativeDepthThresh = 0.1f * linearFullResDepth;

            float2 nearestUV;
            int countBelowThresh = 0;

            [unroll]
            for (int i = 0; i < NEIGHBOUR_SEARCH; ++i)
            {
                float depthDiff = abs(linearFullResDepth - LinearEyeDepth(lowResDepths[i], _ZBufferParams));
                if (depthDiff < minDiff)
                {
                    minDiff = depthDiff;
                    nearestUV = UVs[i];
                }
                countBelowThresh += (depthDiff < relativeDepthThresh);
            }

            if (countBelowThresh == NEIGHBOUR_SEARCH)
            {
                // Bilinear.
                return SAMPLE_TEXTURE2D_X_LOD(_LowResTransparent, s_linear_clamp_sampler, ClampAndScaleUVForBilinear(uv, halfResTexelSize), 0);
            }
            else
            {
                // Edge with nearest UV
#if DEBUG_EDGE
                return float4(0.0, 10.0, 0.0, 1.0);
#else
                return SAMPLE_TEXTURE2D_X_LOD(_LowResTransparent, s_point_clamp_sampler, ClampAndScaleUVForPoint(nearestUV), 0);
#endif
            }
        #else // BILINEAR

            return SAMPLE_TEXTURE2D_X_LOD(_LowResTransparent, s_linear_clamp_sampler, ClampAndScaleUVForBilinear(uv, halfResTexelSize), 0.0);

        #endif


        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off ZTest Off Blend Off Cull Off
            Blend One SrcAlpha, Zero One
            BlendOp Add

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
