Shader "Hidden/HDRP/DepthValues"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        // #pragma enable_d3d11_debug_symbols

        // Target multisampling textures
        TEXTURE2D_X_MSAA(float, _DepthTextureMS);
        TEXTURE2D_X_MSAA(float4, _NormalTextureMS);

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

        struct FragOut
        {
            float4 depthValues : SV_Target0;
            float4 normal : SV_Target1;
            float actualDepth : SV_Depth;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID) * _ScreenSize.xy;
            return output;
        }

#define UNITY_REVERSED_Z 1
#define UNITY_NEAR_CLIP_VALUE (1.0)
// This value will not go through any matrix projection conversion
#define UNITY_RAW_FAR_CLIP_VALUE (0.0)

        FragOut ComputeDepthValues(Varyings input, int numSamples)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 pixelCoords = int2(input.texcoord);

            FragOut fragO;

            fragO.depthValues = float4(-FLT_INF, FLT_INF, 0, 0);

            UNITY_UNROLL
            for (int sampleIdx = 0; sampleIdx < numSamples; ++sampleIdx)
            {
                float depthVal = LOAD_TEXTURE2D_X_MSAA(_DepthTextureMS, pixelCoords, sampleIdx).x;

                // Separate the far plane from the geometry.
                if (depthVal != UNITY_RAW_FAR_CLIP_VALUE)
                {
                    fragO.depthValues.x  = max(depthVal, fragO.depthValues.x);
                    fragO.depthValues.y  = min(depthVal, fragO.depthValues.y);
                    fragO.depthValues.z += depthVal;
                    fragO.depthValues.w += 1;
                }
            }

            if (fragO.depthValues.w == 0)
            {
                // Only the far plane.
                fragO.depthValues.xyz = UNITY_RAW_FAR_CLIP_VALUE.xxx;
            }
            else
            {
                // Normalize.
                fragO.depthValues.z *= rcp(fragO.depthValues.w);
            }

            fragO.normal = LOAD_TEXTURE2D_X_MSAA(_NormalTextureMS, pixelCoords, 0);
            fragO.actualDepth = fragO.depthValues.x;

            return fragO;
        }

        FragOut Frag1X(Varyings input)
        {
            return ComputeDepthValues(input, 1);
        }

        FragOut Frag2X(Varyings input)
        {
            return ComputeDepthValues(input, 2);
        }

        FragOut Frag4X(Varyings input)
        {
            return ComputeDepthValues(input, 4);
        }

        FragOut Frag8X(Varyings input)
        {
            return ComputeDepthValues(input, 8);
        }
    ENDHLSL
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // 0: MSAA 1x
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag1X
            ENDHLSL
        }

        // 1: MSAA 2x
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag2X
            ENDHLSL
        }

        // 2: MSAA 4X
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag4X
            ENDHLSL
        }

        // 3: MSAA 8X
        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag8X
            ENDHLSL
        }
    }
    Fallback Off
}
