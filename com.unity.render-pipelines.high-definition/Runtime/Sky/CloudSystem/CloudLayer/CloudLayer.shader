Shader "Hidden/HDRP/Sky/CloudLayer"
{
    HLSLINCLUDE

    #pragma vertex Vert

    //#pragma enable_d3d11_debug_symbols
    #pragma editor_sync_compilation
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #pragma multi_compile_local _ USE_CLOUD_MOTION
    #pragma multi_compile_local _ USE_FLOWMAP
    #pragma multi_compile_local _ USE_SECOND_CLOUD_LAYER
    #pragma multi_compile_local _ USE_SECOND_CLOUD_MOTION
    #pragma multi_compile_local _ USE_SECOND_FLOWMAP

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/CloudSystem/CloudLayer/CloudLayerCommon.hlsl"

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
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }

    float4 FragBaking(Varyings input) : SV_Target
    {
        return RenderClouds(input.positionCS.xy);
    }

    float4 FragRender(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float4 color = RenderClouds(input.positionCS.xy);
        color.rgb *= GetCurrentExposureMultiplier();
        return color;
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha // Premultiplied alpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragBaking
            ENDHLSL

        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcAlpha // Premultiplied alpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragRender
            ENDHLSL
        }

    }
    Fallback Off
}
