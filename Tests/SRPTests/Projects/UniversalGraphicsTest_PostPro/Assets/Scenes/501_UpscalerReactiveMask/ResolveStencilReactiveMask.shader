Shader "Custom/ResolveStencilReactiveMask"
{
    SubShader
    {
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            Stencil
            {
                ReadMask 1
                Ref 1
                Comp Equal
                Pass Keep
            }

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                OUT.texcoord = GetFullScreenTriangleTexCoord(IN.vertexID);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return float4(1, 1, 1, 1);
            }
            ENDHLSL
        }
    }
}
