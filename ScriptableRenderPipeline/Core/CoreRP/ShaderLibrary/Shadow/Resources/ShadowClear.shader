Shader "Hidden/ScriptableRenderPipeline/ShadowClear"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal

        #include "CoreRP/ShaderLibrary/Common.hlsl"


        float4 Frag() : SV_Target { return 0.0.xxxx; }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "ClearShadow_0"
            ZTest Always
            Cull Off
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert_0
            #pragma fragment Frag

            float4 Vert_0( uint vertexID : VERTEXID_SEMANTIC ) : SV_POSITION
            {
                return GetFullScreenTriangleVertexPosition( vertexID, 0.0 );
            }

            ENDHLSL
        }

        Pass
        {
            Name "ClearShadow_1"
            ZTest Always
            Cull Off
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert_1
            #pragma fragment Frag
            
            float4 Vert_1( uint vertexID : VERTEXID_SEMANTIC ) : SV_POSITION
            {
                return GetFullScreenTriangleVertexPosition( vertexID, 1.0 );
            }

            ENDHLSL
        }

    }
    Fallback Off
}
