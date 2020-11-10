Shader "Hidden/HDRP/DebugDensityVolumeAtlas"
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
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            // TODO: texture 3D debug code
            TEXTURE3D(_InputTexture);
            SAMPLER(sampler_InputTexture);
            float _Slice;

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
                return SAMPLE_TEXTURE3D_LOD(_InputTexture, sampler_InputTexture, float3(input.texcoord.xy, _Slice), 0).rgba;
            }

            ENDHLSL
        }

    }
    Fallback Off
}
