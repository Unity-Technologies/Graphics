Shader "Hidden/HDRP/ProbeVolumeFragmentationDebug"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite On
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/ProbeVolumeDebug.hlsl"

            #pragma enable_d3d11_debug_symbols

            int _ChunkCount;
            StructuredBuffer<int> _DebugFragmentation;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                int lineSize = (int)ceil(sqrt(_ChunkCount));
                int2 coord = (int2)(input.texcoord * lineSize);

                int index = coord.y * lineSize + coord.x;

                float4 color = 0.0;
                if (index < _ChunkCount && _DebugFragmentation[index] != -1)
                    color = float4(0.0, 1.0, 0.0, 1.0);

                return color;
            }

            ENDHLSL
        }

    }
        Fallback Off
}
