Shader "Hidden/HDRP/UpsampleTransparent"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma editor_sync_compilation
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


        float4 _Params; //x: targetResolutionMultiplier, y: 1.0/targetResolutionMultiplier, z: unused, w: unused
        TEXTURE2D_X(_LowResTransparent);
#ifdef NEAREST_DEPTH
        TEXTURE2D_X_FLOAT(_LowResDepthTexture);

#define NEIGHBOUR_SEARCH 4
#define DEBUG_EDGE 0
#endif

        TEXTURE2D_X(_BeforeRefraction);
        TEXTURE2D_X(_BeforeRefractionAlpha);

        float4 Upsample(uint2 coord, float2 uv)
        {
            float2 fullResTexelSize = _ScreenSize.zw;
            float2 halfResTexelSize = _Params.y * fullResTexelSize;
            float2 lowResDRSScale = _Params.zw;

        #ifdef NEAREST_DEPTH

            // The following is an implementation of NVIDIA's http://developer.download.nvidia.com/assets/gamedev/files/sdk/11/OpacityMappingSDKWhitePaper.pdf

            float4 lowResDepths = GATHER_RED_TEXTURE2D_X(_LowResDepthTexture, s_linear_clamp_sampler, ClampAndScaleUV(uv, halfResTexelSize, 0.5, lowResDRSScale));

            // Gather UVs
            float2 topLeftUV = uv - 0.5f * halfResTexelSize;
            float2 UVs[NEIGHBOUR_SEARCH] = {
              topLeftUV + float2(0.0f,             halfResTexelSize.y),
              topLeftUV + float2(halfResTexelSize.x, halfResTexelSize.y),
              topLeftUV + float2(halfResTexelSize.x, 0.0f),
              topLeftUV,
            };

            float fullResDepth = LoadCameraDepth(coord);
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
                return SAMPLE_TEXTURE2D_X_LOD(_LowResTransparent, s_linear_clamp_sampler, ClampAndScaleUV(uv, halfResTexelSize, 0.5, lowResDRSScale), 0);
            }
            else
            {
                // Edge with nearest UV
                #if DEBUG_EDGE
                return float4(0.0, 10.0, 0.0, 1.0);
                #else
                // Important note! The reason we need to do ClampAndScaleUVForBilinear is because the candidate for nearestUV are going to be the ones
                // used for bilinear. We are using the same UVs used for bilinear -hence the uv clamp for bilinear- it is just the filtering that is different.
                return SAMPLE_TEXTURE2D_X_LOD(_LowResTransparent, s_point_clamp_sampler, ClampAndScaleUV(nearestUV, halfResTexelSize, 0.5, lowResDRSScale), 0);
                #endif
            }
        #else // BILINEAR

            return SAMPLE_TEXTURE2D_X_LOD(_LowResTransparent, s_linear_clamp_sampler, ClampAndScaleUV(uv, halfResTexelSize, 0.5, lowResDRSScale), 0.0);

        #endif
        }

        float4 Combine(uint2 coord)
        {
            uint width = 0, height = 0, mipCount = 0, elements = 0;
            #if defined(USE_TEXTURE2D_X_AS_ARRAY)
            _BeforeRefractionAlpha.GetDimensions(0, width, height, mipCount, elements);
            #else
            _BeforeRefractionAlpha.GetDimensions(0, width, height, mipCount);
            #endif

            float3 color = LOAD_TEXTURE2D_X(_BeforeRefraction, coord).xyz;
            float alpha  = LOAD_TEXTURE2D_X(_BeforeRefractionAlpha, min(coord, float2(width, height) - 1)).x;
            return float4(color, alpha);
        }

        float4 FragCombine(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return Combine(input.positionCS.xy);
        }

        float4 FragUpsample(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return Upsample(input.positionCS.xy, input.texcoord);
        }

        float4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float4 lowRes = Upsample(input.positionCS.xy, input.texcoord);
            float4 combine = Combine(input.positionCS.xy);

            return float4(combine.xyz * lowRes.a + lowRes.xyz, combine.a * lowRes.a);
        }


    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // Combine
        Pass
        {
            ZWrite Off ZTest Off Blend Off Cull Off
            Blend One SrcAlpha, Zero One
            BlendOp Add

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragCombine
            ENDHLSL
        }

        // Upsample
        Pass
        {
            ZWrite Off ZTest Off Blend Off Cull Off
            Blend One SrcAlpha, Zero One
            BlendOp Add

            HLSLPROGRAM
                #pragma multi_compile_local_fragment BILINEAR NEAREST_DEPTH

                #pragma vertex Vert
                #pragma fragment FragUpsample
            ENDHLSL
        }

        // Upsample + Combine
        Pass
        {
            ZWrite Off ZTest Off Blend Off Cull Off
            Blend One SrcAlpha, Zero One
            BlendOp Add

            HLSLPROGRAM
                #pragma multi_compile_local_fragment BILINEAR NEAREST_DEPTH

                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
