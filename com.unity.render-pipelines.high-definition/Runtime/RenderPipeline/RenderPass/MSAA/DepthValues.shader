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
            output.texcoord   = TexCoordStereoOffset(GetFullScreenTriangleTexCoord(input.vertexID) * _ScreenSize.xy);
            return output;
        }

        FragOut Frag1X(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            FragOut fragO;
            int2 pixelCoords = int2(input.texcoord);
            float depthVal = LOAD_TEXTURE2D_X_MSAA(_DepthTextureMS, pixelCoords, 0).x;
            fragO.depthValues = float4(depthVal, depthVal, depthVal, 0.0f);
            fragO.normal = LOAD_TEXTURE2D_X_MSAA(_NormalTextureMS, pixelCoords, 0);
            fragO.actualDepth = fragO.depthValues.x;
            return fragO;
        }

        FragOut Frag2X(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            FragOut fragO;
            fragO.depthValues = float4(0.0f, 100000.0f, 0.0f, 0.0f);
            int2 pixelCoords = int2(input.texcoord);
            for(int sampleIdx = 0; sampleIdx < 2; ++sampleIdx)
            {
                float depthVal = LOAD_TEXTURE2D_X_MSAA(_DepthTextureMS, pixelCoords, sampleIdx).x;
                fragO.depthValues.x = max(depthVal, fragO.depthValues.x);
                fragO.depthValues.y = min(depthVal, fragO.depthValues.y);
                fragO.depthValues.z += depthVal;
            }
            fragO.depthValues.z *= 0.5;
            fragO.actualDepth = fragO.depthValues.x;
            fragO.normal = LOAD_TEXTURE2D_X_MSAA(_NormalTextureMS, pixelCoords, 0);
            return fragO;
        }

        FragOut Frag4X(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            FragOut fragO;
            fragO.depthValues = float4(0.0f, 100000.0f, 0.0f, 0.0f);
            int2 pixelCoords = int2(input.texcoord);
            for(int sampleIdx = 0; sampleIdx < 4; ++sampleIdx)
            {
                float depthVal = LOAD_TEXTURE2D_X_MSAA(_DepthTextureMS, pixelCoords, sampleIdx).x;
                fragO.depthValues.x = max(depthVal, fragO.depthValues.x);
                fragO.depthValues.y = min(depthVal, fragO.depthValues.y);
                fragO.depthValues.z += depthVal;
            }
            fragO.depthValues.z *= 0.25;
            fragO.actualDepth = fragO.depthValues.x;
            fragO.normal = LOAD_TEXTURE2D_X_MSAA(_NormalTextureMS, pixelCoords, 0);
            return fragO;
        }

        FragOut Frag8X(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            FragOut fragO;
            fragO.depthValues = float4(0.0f, 100000.0f, 0.0f, 0.0f);
            int2 pixelCoords = int2(input.texcoord);
            for(int sampleIdx = 0; sampleIdx < 8; ++sampleIdx)
            {
                float depthVal = LOAD_TEXTURE2D_X_MSAA(_DepthTextureMS, pixelCoords, sampleIdx).x;
                fragO.depthValues.x = max(depthVal, fragO.depthValues.x);
                fragO.depthValues.y = min(depthVal, fragO.depthValues.y);
                fragO.depthValues.z += depthVal;
            }
            fragO.depthValues.z *= 0.125;
            fragO.normal = LOAD_TEXTURE2D_X_MSAA(_NormalTextureMS, pixelCoords, 0);
            fragO.actualDepth = fragO.depthValues.x;
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
