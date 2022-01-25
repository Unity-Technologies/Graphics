Shader "Hidden/HDRP/DownsampleDepth"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma multi_compile_local_fragment MIN_DOWNSAMPLE CHECKERBOARD_DOWNSAMPLE
        #pragma multi_compile_local_fragment _ GATHER_DOWNSAMPLE
        #pragma multi_compile_local_fragment _ OUTPUT_FIRST_MIP_OF_MIPCHAIN
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

#ifdef OUTPUT_FIRST_MIP_OF_MIPCHAIN
    #ifdef SHADER_API_PSSL
        RW_TEXTURE2D_X(float, _OutputTexture) : register(u0);
    #else
        RW_TEXTURE2D_X(float, _OutputTexture) : register(u1);
    #endif
        float4 _DstOffset;
#endif

        float4 _ScaleBias; // x: uv offset x, uv offset y, uv x scale, uv y scale,

        void Frag(Varyings input, out float outputDepth : SV_Depth)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#ifdef GATHER_DOWNSAMPLE
            float4 depths = GATHER_RED_TEXTURE2D_X(_CameraDepthTexture, s_linear_clamp_sampler, input.texcoord * _ScaleBias.xy + _ScaleBias.zw);
            outputDepth = MinDepth(depths);
#else
            uint2 fullResUpperCorner = uint2((((float2)input.positionCS.xy - 0.5f) * 2.0) + 0.5f);
            float4 depths;
            depths.x = LoadCameraDepth(fullResUpperCorner);
            depths.y = LoadCameraDepth(fullResUpperCorner + uint2(0, 1));
            depths.z = LoadCameraDepth(fullResUpperCorner + uint2(1, 0));
            depths.w = LoadCameraDepth(fullResUpperCorner + uint2(1, 1));

            float minDepth = MinDepth(depths);
        #if MIN_DOWNSAMPLE
            outputDepth = minDepth;
        #elif CHECKERBOARD_DOWNSAMPLE
            outputDepth = (uint(input.positionCS.x + input.positionCS.y) & 1) > 0 ? minDepth : MaxDepth(depths);
        #endif

        #ifdef OUTPUT_FIRST_MIP_OF_MIPCHAIN
            _OutputTexture[COORD_TEXTURE2D_X(_DstOffset.xy + input.positionCS.xy)] = minDepth;
        #endif
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
