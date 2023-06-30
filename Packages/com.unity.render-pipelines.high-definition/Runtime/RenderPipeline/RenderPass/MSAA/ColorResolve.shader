Shader "Hidden/HDRP/ColorResolve"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        //#pragma enable_d3d11_debug_symbols

        TEXTURE2D_X_MSAA(float4, _ColorTextureMS);

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
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID) * _ScreenSize.xy;
            return output;
        }

        float ResolveWeight(float4 color, float totalSampleCount)
        {
            const float boxFilterWeight = rcp(totalSampleCount);
            float toneMapWeight = rcp(1.0f + Luminance(color.xyz));

            return boxFilterWeight * toneMapWeight;
        }

        float InverseToneMapWeight(float4 color)
        {
            return rcp(1.0f - Luminance(color.xyz));
        }

        float4 LoadColorTextureMS(float2 pixelCoords, uint sampleIndex)
        {
            return LOAD_TEXTURE2D_X_MSAA(_ColorTextureMS, pixelCoords, sampleIndex);
        }

        float4 Resolve(Varyings input, int sampleCount)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 pixelCoords = int2(input.texcoord);

            float4 finalVal = 0;
            for (int i = 0; i < sampleCount; ++i)
            {
                float4 currSample = (LoadColorTextureMS(pixelCoords, i));
                finalVal.rgb += currSample.rgb * ResolveWeight(currSample, sampleCount);
                finalVal.a += currSample.a * rcp(sampleCount);
            }

            finalVal.xyz *= InverseToneMapWeight(finalVal);

            return finalVal;
        }

        float4 Frag1X(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            int2 pixelCoords = int2(input.texcoord);
            return LoadColorTextureMS(pixelCoords, 0);
        }

        float4 Frag2X(Varyings input) : SV_Target
        {
            return Resolve(input, 2);
        }

        float4 Frag4X(Varyings input) : SV_Target
        {
            return Resolve(input, 4);
        }

        float4 Frag8X(Varyings input) : SV_Target
        {
            return Resolve(input, 8);
        }

    ENDHLSL
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // 0: MSAA 1x
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off
            Name "MSAA1X"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag1X
            ENDHLSL
        }

        // 1: MSAA 2x
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off
            Name "MSAA2X"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag2X
            ENDHLSL
        }

        // 2: MSAA 4X
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off
            Name "MSAA4X"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag4X
            ENDHLSL
        }

        // 3: MSAA 8X
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off
            Name "MSAA8X"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag8X
            ENDHLSL
        }
    }
    Fallback Off
}
