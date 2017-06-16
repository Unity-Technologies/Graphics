Shader "Hidden/HDPipeline/Lighting/ScreenSpaceAmbientOcclusion"
{
    HLSLINCLUDE

    #include "../../../ShaderLibrary/Common.hlsl"
    #include "../../ShaderConfig.cs.hlsl"
    #include "../../ShaderVariables.hlsl"

    #define UNITY_MATERIAL_LIT // Needs to be defined before including Material.hlsl
    #include "../../Material/Material.hlsl"

    DECLARE_GBUFFER_TEXTURE(_GBufferTexture);

    // The constant below determines the contrast of occlusion. This allows
    // users to control over/under occlusion. At the moment, this is not exposed
    // to the editor because it's rarely useful.
    static const float kContrast = 0.6;

    // The constant below controls the geometry-awareness of the bilateral
    // filter. The higher value, the more sensitive it is.
    static const float kGeometryCoeff = 0.8;

    // The constants below are used in the AO estimator. Beta is mainly used
    // for suppressing self-shadowing noise, and Epsilon is used to prevent
    // calculation underflow. See the paper (Morgan 2011 http://goo.gl/2iz3P)
    // for further details of these constants.
    static const float kBeta = 0.002;

    // A small value used for avoiding self-occlusion.
    static const float kEpsilon = 1e-6;

    // Interleaved gradient function from Jimenez 2014 http://goo.gl/eomGso
    float GradientNoise(float2 uv)
    {
        uv = floor(uv * _ScreenParams.xy);
        float f = dot(float2(0.06711056, 0.00583715), uv);
        return frac(52.9829189 * frac(f));
    }

    // Check if the depth value is valid.
    bool CheckDepth(float rawDepth)
    {
    #if defined(UNITY_REVERSED_Z)
        return rawDepth > 0.00001;
    #else
        return rawDepth < 0.99999;
    #endif
    }

    // AO/normal packed format conversion
    half4 PackAONormal(half ao, half3 n)
    {
        return half4(ao, n * 0.5 + 0.5);
    }

    half GetPackedAO(half4 p)
    {
        return p.x;
    }

    half3 GetPackedNormal(half4 p)
    {
        return p.yzw * 2.0 - 1.0;
    }

    half3 SampleNormal(uint2 unPositionSS)
    {
        float3 unused;
        BSDFData bsdfData;
        FETCH_GBUFFER(gbuffer, _GBufferTexture, unPositionSS);
        DECODE_FROM_GBUFFER(gbuffer, 0xFFFFFFFF, bsdfData, unused);
        return mul((float3x3)unity_WorldToCamera, bsdfData.normalWS);
    }

    // Normal vector comparer (for geometry-aware weighting)
    half CompareNormal(half3 d1, half3 d2)
    {
        return smoothstep(kGeometryCoeff, 1.0, dot(d1, d2));
    }

    // Default vertex shader
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

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: Ambient occlusion estimation
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragAO
            #include "AOEstimator.hlsl"
            ENDHLSL
        }

        // 1: Denoising (horizontal pass)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragSeparableFilter
            #define SSAO_NOISEFILTER_HORIZONTAL
            #define SSAO_NOISEFILTER_CENTERNORMAL
            #include "NoiseFilter.hlsl"
            ENDHLSL
        }

        // 2: Denoising (vertical pass)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragSeparableFilter
            #define SSAO_NOISEFILTER_VERTICAL
            #include "NoiseFilter.hlsl"
            ENDHLSL
        }

        // 3: Final filtering
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragFinalFilter
            #include "NoiseFilter.hlsl"
            ENDHLSL
        }
    }
}
