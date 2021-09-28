Shader "Hidden/HDRP/CompositeUI"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma editor_sync_compilation
        #pragma multi_compile_local _ HDR_OUTPUT_REC2020 HDR_OUTPUT_SCRGB

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/HDROutput.hlsl"

        TEXTURE2D_X(_InputTexture);
        TEXTURE2D_X(_UITexture);

        CBUFFER_START(cb)
            float4 _HDROutputParams;
        CBUFFER_END

        #define _MinNits    _HDROutputParams.x
        #define _MaxNits    _HDROutputParams.y
        #define _PaperWhite _HDROutputParams.z
        #define _IsRec2020  (int)(_HDROutputParams.w) == 0
        #define _IsRec709   (int)(_HDROutputParams.w) == 1
        #define _IsP3       (int)(_HDROutputParams.w) == 2

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

        float4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = input.texcoord;
            // We need to flip y
            uv.y = 1.0f - uv.y;

            float4 sceneColor = SAMPLE_TEXTURE2D_X(_InputTexture, s_point_clamp_sampler, uv);
            sceneColor.rgb = OETF(sceneColor.rgb);

            float4 uiValue = SAMPLE_TEXTURE2D_X(_UITexture, s_point_clamp_sampler, uv);

            float uiBoost = 1.0f; // TODO_FCC: Add from editor UI
            sceneColor.rgb = SceneUIComposition(uiValue, sceneColor.rgb, _PaperWhite * uiBoost);

            return sceneColor;
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
