Shader "Hidden/HDRP/PreIntegratedAzimuthalScattering"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM

            #pragma editor_sync_compilation

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #define PREFER_HALF 0

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/PreIntegratedAzimuthalScattering.cs.hlsl"

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
                // We want the LUT to contain the entire [0, 1] range, without losing half a texel at each side.
                float2 coordLUT = RemapHalfTexelCoordTo01(input.texCoord, AZIMUTHALSCATTERINGTEXTURE_RESOLUTION);

                // TODO: Integrate the azimuthal scattering for all beta, theta, and phi. Then, fit a gaussian for each phi.
                return float4(coordLUT, 0, 1);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
