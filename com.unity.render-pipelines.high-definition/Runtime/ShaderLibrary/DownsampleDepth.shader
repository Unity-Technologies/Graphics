Shader "Hidden/HDRP/DownsampleDepth"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma multi_compile_local MIN_DOWNSAMPLE CHECKERBOARD_DOWNSAMPLE
        #pragma only_renderers d3d11 playstation xboxone vulkan metal switch
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
            output.texcoord = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        float MinDepth(float4 depths)
        {
#if UNITY_REVERSED_Z
            return Max3(depths.x, depths.y, max(depths.z, depths.w));
#else
            return Min3(depths.x, depths.y, min(depths.z, depths.w));
#endif
        }

        float MaxDepth(float4 depths)
        {
#if UNITY_REVERSED_Z
            return Min3(depths.x, depths.y, min(depths.z, depths.w));
#else
            return Max3(depths.x, depths.y, max(depths.z, depths.w));
#endif
        }

        void Frag(Varyings input, out float outputDepth : SV_Depth)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            uint2 fullResUpperCorner = uint2(input.positionCS.xy * 2.0);
            float4 depths;
            depths.x = LoadCameraDepth(fullResUpperCorner);
            depths.y = LoadCameraDepth(fullResUpperCorner + uint2(0, 1));
            depths.z = LoadCameraDepth(fullResUpperCorner + uint2(1, 0));
            depths.w = LoadCameraDepth(fullResUpperCorner + uint2(1, 1));

        #if MIN_DOWNSAMPLE
            outputDepth = MinDepth(depths);
        #elif CHECKERBOARD_DOWNSAMPLE
            outputDepth = (uint(input.positionCS.x + input.positionCS.y) & 1) > 0 ? MinDepth(depths) : MaxDepth(depths);
        #endif
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite On Blend Off Cull Off ZTest Always

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
