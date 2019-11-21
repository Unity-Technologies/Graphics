Shader "Hidden/HDRP/AccumulationMotionBlur"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/MotionBlurCommon.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/TemporalAntialiasing.hlsl"

        TEXTURE2D_X(_InputTexture);
        TEXTURE2D_X(_InputHistoryTexture);
        RW_TEXTURE2D_X(float3, _OutputHistoryTexture);

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
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        void FragAccumulation(Varyings input, out float3 outColor : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            
            float2 uv = input.texcoord;
            
            //TODO: Remove Fetch
            float3 color   = Fetch(_InputTexture,        uv, 0.0, _RTHandleScale.xy);
            float3 history = Fetch(_InputHistoryTexture, uv, 0.0, _RTHandleScale.xy); //TODO: History Scale
            color = (color * rcp(asfloat(_AccumulationSampleCount))) + (history * step(1, _AccumulationSampleIndex));

            _OutputHistoryTexture[COORD_TEXTURE2D_X(input.positionCS.xy)] = color;
            outColor = color;
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex   Vert
                #pragma fragment FragAccumulation
            ENDHLSL
        }
    }
    Fallback Off
}
