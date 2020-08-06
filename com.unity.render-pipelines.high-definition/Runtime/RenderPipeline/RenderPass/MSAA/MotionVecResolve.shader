Shader "Hidden/HDRP/MotionVecResolve"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        // #pragma enable_d3d11_debug_symbols

        // Target multisampling textures
        TEXTURE2D_X_MSAA(float2, _MotionVectorTextureMS);

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
            float2 motionVectors : SV_Target0;
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

        FragOut Frag1X(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            FragOut fragO;
            int2 pixelCoords = int2(input.texcoord);
            fragO.motionVectors = LOAD_TEXTURE2D_X_MSAA(_MotionVectorTextureMS, pixelCoords, 0);
            return fragO;
        }

        FragOut Frag2X(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            FragOut fragO;
            int2 pixelCoords = int2(input.texcoord);
            float2 outMotionVec = 0;
            for(int sampleIdx = 0; sampleIdx < 2; ++sampleIdx)
            {
                outMotionVec += LOAD_TEXTURE2D_X_MSAA(_MotionVectorTextureMS, pixelCoords, sampleIdx);
            }
            fragO.motionVectors = outMotionVec * 0.5f;
            return fragO;
        }

        FragOut Frag4X(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            FragOut fragO;
            int2 pixelCoords = int2(input.texcoord);
            float2 outMotionVec = 0;
            for(int sampleIdx = 0; sampleIdx < 4; ++sampleIdx)
            {
                outMotionVec += LOAD_TEXTURE2D_X_MSAA(_MotionVectorTextureMS, pixelCoords, sampleIdx);
            }
            fragO.motionVectors = outMotionVec * 0.25f;

            return fragO;
        }

        FragOut Frag8X(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            FragOut fragO;
            int2 pixelCoords = int2(input.texcoord);
            float2 outMotionVec = 0;
            for(int sampleIdx = 0; sampleIdx < 8; ++sampleIdx)
            {
                outMotionVec += LOAD_TEXTURE2D_X_MSAA(_MotionVectorTextureMS, pixelCoords, sampleIdx);
            }
            fragO.motionVectors = outMotionVec * 0.125f;
            return fragO;
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
