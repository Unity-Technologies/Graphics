Shader "Hidden/HDRP/DebugRenderer"
{
    Properties
    {
    }

    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    //#pragma enable_d3d11_debug_symbols

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugRenderer.cs.hlsl"

    StructuredBuffer<LineData> _LineData;

    CBUFFER_START(DebugRenderer)
    float4 _CameraRelativeOffset;
    CBUFFER_END

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        uint instanceID   : SV_InstanceID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varying
    {
        float4 positionCS : SV_POSITION;
        float4 color : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varying VertLine(Attributes input)
    {
        Varying output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        float3 vertexPos = input.vertexID == 0 ? _LineData[input.instanceID].p0 : _LineData[input.instanceID].p1;

        float3 positionRWS = vertexPos - _CameraRelativeOffset.xyz;
        output.positionCS = TransformWorldToHClip(positionRWS);
        output.color = _LineData[input.instanceID].color;;

        return output;
    }

    float4 FragLine(Varying input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return input.color;
    }
        ENDHLSL

    SubShader
    {
        Pass
        {
            Name "LineNoDepthTest"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex VertLine
            #pragma fragment FragLine
            ENDHLSL
        }

        Pass
        {
            Name "LineDepthTest"

            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex VertLine
            #pragma fragment FragLine
            ENDHLSL
        }
    }
}
