Shader "Hidden/HDRP/DebugRenderer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
        SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varying
            {
                float4 positionCS : SV_POSITION;
                float4 color : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varying Vert (Attributes input)
            {
                Varying output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                uint index = input.vertexID / 2;
                uint vertexIndex = input.vertexID % 2;

                float3 vertexPos = vertexIndex == 0 ? _LineData[index].p0 : _LineData[index].p1;
                float4 vertexColor; _LineData[index].color;

                float3 positionRWS = vertexPos - _CameraRelativeOffset.xyz;
                output.positionCS = TransformWorldToHClip(positionRWS);
                output.color = vertexColor;

                return output;
            }

            float4 Frag(Varying input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return input.color;
            }
            ENDHLSL
        }
    }
}
