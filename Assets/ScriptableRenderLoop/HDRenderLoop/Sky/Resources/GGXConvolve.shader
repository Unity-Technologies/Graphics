Shader "Hidden/HDRenderLoop/GGXConvolve"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Common.hlsl"
            #include "ImageBasedLighting.hlsl"

            struct Attributes
            {
                float3 positionCS : POSITION;
                float3 eyeVector : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 eyeVector : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = float4(input.positionCS.xy, UNITY_RAW_FAR_CLIP_VALUE, 1.0);
                output.eyeVector = input.eyeVector;
                return output;
            }

            TEXTURECUBE(_MainTex);
            SAMPLERCUBE(sampler_MainTex);
            float _Level;
            float _MipMapCount;
            float _InvOmegaP;

            half4 Frag(Varyings input) : SV_Target
            {
                float3 N = input.eyeVector;
                float3 V = N;

                float perceptualRoughness = mipmapLevelToPerceptualRoughness(_Level);
                // We approximate the pre-integration with V == N
                float4 val = IntegrateLD(   TEXTURECUBE_PARAM(_MainTex, sampler_MainTex),
                                            V,
                                            N,
                                            PerceptualRoughnessToRoughness(perceptualRoughness),
                                            _MipMapCount,
                                            _InvOmegaP,
                                            1,
                                            false);

                return val;
            }
            ENDHLSL
        }
    }
}
