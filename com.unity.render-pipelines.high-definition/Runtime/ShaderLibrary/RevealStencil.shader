Shader "Hidden/HDRP/RevealStencil"
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

        TEXTURE2D_X(_ColorTexture);

        float4 FragCopy(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            return SAMPLE_TEXTURE2D_X_LOD(_ColorTexture, s_linear_clamp_sampler, ClampAndScaleUVForPoint(input.texcoord), 0.0);
        }

        float4 FragHighlight(Varyings input) : SV_Target
        {
            return float4(0, 0.5, 1, 1);
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off ZTest Off Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragCopy
            ENDHLSL
        }

        Pass
        {
            ZWrite Off ZTest Off Blend Off Cull Off
            Stencil
            {
                Ref [_StencilMask]
                ReadMask [_StencilMask]
                Comp Equal
            }

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragHighlight
            ENDHLSL
        }
    }
    Fallback Off
}
