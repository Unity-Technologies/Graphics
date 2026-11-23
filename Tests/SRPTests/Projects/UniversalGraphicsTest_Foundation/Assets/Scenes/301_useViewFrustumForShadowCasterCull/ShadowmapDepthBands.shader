// Shows a shadow map as grey depth bands, brighter where a caster is closer to the light.
// It reads the map only through depth comparisons, as URP does when it samples shadows, so the
// output does not depend on how a graphics API exposes depth texels as colour.
Shader "Hidden/Test/ShadowmapDepthBands"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "ShadowmapDepthBands"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #define DEPTH_BAND_COUNT 16

            TEXTURE2D_SHADOW(_BlitTexture);
            SAMPLER_CMP(sampler_LinearClampCompare);
            float4 _BlitScaleBias;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float occludedBands = 0.0;

                [unroll]
                for (int band = 0; band < DEPTH_BAND_COUNT; ++band)
                {
                    // Distance from the light as a fraction of the shadow depth range, at the far edge of each band.
                    // The last one stops just short of the far plane, where empty texels are stored.
                    float distanceFromLight = min((band + 1.0) / DEPTH_BAND_COUNT, 0.9999);
                    #if UNITY_REVERSED_Z
                    float referenceDepth = 1.0 - distanceFromLight;
                    #else
                    float referenceDepth = distanceFromLight;
                    #endif

                    // The comparison returns 1 when nothing in the shadow map is closer to the light than the reference
                    float lit = SAMPLE_TEXTURE2D_SHADOW(_BlitTexture, sampler_LinearClampCompare, float3(input.texcoord, referenceDepth));
                    occludedBands += 1.0 - lit;
                }

                float brightness = occludedBands / DEPTH_BAND_COUNT;
                return float4(brightness, brightness, brightness, 1.0);
            }
            ENDHLSL
        }
    }
}
