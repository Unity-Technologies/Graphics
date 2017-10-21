Shader "Hidden/HDRenderPipeline/ComputeGgxEnergyCompensationFactors"
{
   SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: until we go further in dev

            #include "../../../../Core/ShaderLibrary/Common.hlsl"
            #include "../../../../Core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "../../../ShaderVariables.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texCoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texCoord   = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // These coordinate sampling must match the decoding in GetPreIntegratedDFG in lit.hlsl, i.e here we use perceptualRoughness, must be the same in shader
                float  NdotV     = input.texCoord.x;
                float  roughness = PerceptualRoughnessToRoughness(input.texCoord.y);
                float3 N         = float3(0, 0, 1);
                float3 V         = float3(sqrt(1 - NdotV * NdotV), 0, NdotV);

                float cbsdfInt = IntegrateGgxWithoutFresnel(V, N, roughness);

                return cbsdfInt.xxxx;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
