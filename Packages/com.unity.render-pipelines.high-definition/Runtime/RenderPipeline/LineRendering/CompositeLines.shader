Shader "Hidden/HDRP/CompositeLines"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        TEXTURE2D_X(_LineColorTexture);
        TEXTURE2D_X(_LineDepthTexture);
        TEXTURE2D_X(_LineMotionTexture);

        float _AlphaDepthWriteThreshold;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            return output;
        }

        void FragCompositeAll(Varyings input, out float  depth  : SV_Depth,
            out float4 color : SV_Target0,
            out float4 motion : SV_Target1)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if UNITY_UV_STARTS_AT_TOP
            input.positionCS.y = _ScreenParams.y - input.positionCS.y;
#endif
            color  = LOAD_TEXTURE2D_X(_LineColorTexture,  (int2)input.positionCS.xy);
            depth  = LOAD_TEXTURE2D_X(_LineDepthTexture,  (int2)input.positionCS.xy).x;
            motion = LOAD_TEXTURE2D_X(_LineMotionTexture, (int2)input.positionCS.xy);
        }

        void FragColorOnly(Varyings input, out float  depth  : SV_Depth,
            out float4 color : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if UNITY_UV_STARTS_AT_TOP
            input.positionCS.y = _ScreenParams.y - input.positionCS.y;
#endif
            color  = LOAD_TEXTURE2D_X(_LineColorTexture,  (int2)input.positionCS.xy);
            depth  = LOAD_TEXTURE2D_X(_LineDepthTexture,  (int2)input.positionCS.xy).x;
        }

        void FragMovecDepthOnly(Varyings input, out float  depth  : SV_Depth,
                                  out float4 motion : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if UNITY_UV_STARTS_AT_TOP
            input.positionCS.y = _ScreenParams.y - input.positionCS.y;
#endif
            float4 color = LOAD_TEXTURE2D_X(_LineColorTexture,  (int2)input.positionCS.xy);
            if(color.a > _AlphaDepthWriteThreshold)
            {
                motion = LOAD_TEXTURE2D_X(_LineMotionTexture, (int2)input.positionCS.xy);
                depth  = LOAD_TEXTURE2D_X(_LineDepthTexture,  (int2)input.positionCS.xy).x;
            }
            else
            {
                motion = 0;
                depth = 0;
                discard;
            }
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        //Writes color, alpha, depth and motion vectors to composite output
        //Buffers written to: color, depth and motion vectors
        Pass
        {
            Name "CompositeAll"
            ZWrite On ZTest Less Cull Off

            Blend 0 One OneMinusSrcAlpha // Color
            Blend 1 One Zero     // Motion

            HLSLPROGRAM
                #pragma vertex   Vert
                #pragma fragment FragCompositeAll
            ENDHLSL
        }

        //Writes color to output. Note that we only do ZTest here, no ZWrite
        //Buffers written to: color
        Pass
        {
            Name "CompositeColorOnly"
            ZWrite Off ZTest Less Cull Off

            Blend 0 One OneMinusSrcAlpha // Color

            HLSLPROGRAM
                #pragma vertex   Vert
                #pragma fragment FragColorOnly
            ENDHLSL
        }

        //Conditionally writes depth and motion vectors to output. If the alpha of the hair source texel is lower than threshold, the depth & motion vectors from hair are rejected for the given texel.
        //Buffers written to: depth and motion vectors
        Pass
        {
            Name "CompositeDepthMovecOnly"
            ZWrite On ZTest Less Cull Off

            Blend 0 One Zero     // Motion

            HLSLPROGRAM
                #pragma vertex   Vert
                #pragma fragment FragMovecDepthOnly
            ENDHLSL
        }
    }
    Fallback Off
}
