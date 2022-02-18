Shader "Hidden/HDRP/CharlieConvolve"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"

            SAMPLER(s_trilinear_clamp_sampler);

            TEXTURECUBE(_MainTex);
            float _InvOmegaP;
            float _InvFaceCenterTexelSolidAngle;
            float _Level;

            float4x4 _PixelCoordToViewDirWS; // Actually just 3x3, but Unity can only set 4x4

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Points towards the camera
                float3 viewDirWS = normalize(mul(float3(input.positionCS.xy, 1.0), (float3x3)_PixelCoordToViewDirWS));
                // Reverse it to point into the scene
                float3 N = -viewDirWS;

                float perceptualRoughness = MipmapLevelToPerceptualRoughness(_Level);
                float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
                uint  sampleCount = GetIBLRuntimeFilterSampleCount(_Level) / 10; // 10% of the "IBL filter sample count" seems to result in sufficient quality for Charlie convolution

                float4 val = IntegrateLDCharlie(TEXTURECUBE_ARGS(_MainTex, s_trilinear_clamp_sampler),
                             N,
                             roughness,
                             sampleCount,
                             _InvFaceCenterTexelSolidAngle);
                return val;
            }
            ENDHLSL
        }
    }
}
