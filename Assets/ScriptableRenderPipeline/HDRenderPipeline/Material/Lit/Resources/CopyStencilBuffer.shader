Shader "Hidden/HDRenderPipeline/CopyStencilBuffer"
{
    SubShader
    {
        Pass
        {
            Stencil
            {
                Ref  1 // StencilLightingUsage.SplitLighting
                Comp Equal
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: until we go further in dev
            // #pragma enable_d3d11_debug_symbols

            #pragma vertex Vert
            #pragma fragment Frag

            #include "../../../../ShaderLibrary/Common.hlsl"
            #include "../../../ShaderVariables.hlsl"
            #include "../../../Lighting/LightDefinition.cs.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            // Should use HiS and therefore be faster than a GPU memcpy().
            float4 Frag(Varyings input) : SV_Target // use SV_StencilRef in D3D 11.3+
            {
                return float4(STENCILLIGHTINGUSAGE_SPLIT_LIGHTING, 0, 0, 0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
