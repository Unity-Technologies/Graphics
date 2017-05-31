#ifndef UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_ESTIMATION
#define UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_ESTIMATION

#include "CommonAmbientOcclusion.hlsl"

half _Intensity;
float _Radius;
half _Downsample;
int _SampleCount;

// Sample point picker
float3 PickSamplePoint(float2 uv, float index)
{
    // Uniformaly distributed points on a unit sphere http://goo.gl/X2F1Ho
    float gn = GradientNoise(uv * _Downsample);
    // FIXEME: This was added to avoid a NVIDIA driver issue.
    //                                   vvvvvvvvvvvv
    float u = frac(UVRandom(0.0, index + uv.x * 1e-10) + gn) * 2.0 - 1.0;
    float theta = (UVRandom(1.0, index + uv.x * 1e-10) + gn) * TWO_PI;
    float3 v = float3(SinCos(theta).yx * sqrt(1.0 - u * u), u);
    // Make them distributed between [0, _Radius]
    float l = sqrt((index + 1.0) / _SampleCount) * _Radius;
    return v * l;
}

// Distance-based AO estimator based on Morgan 2011 http://goo.gl/2iz3P
half4 Frag(Varyings input) : SV_Target
{
    // input.positionCS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw / _Downsample);
    float2 uv = posInput.positionSS;

    half3 unused;
    BSDFData bsdfData;
    FETCH_GBUFFER(gbuffer, _GBufferTexture, posInput.unPositionSS / _Downsample);
    DECODE_FROM_GBUFFER(gbuffer, 0xFFFFFFFF, bsdfData, unused);

    // Parameters used in coordinate conversion
    float3x3 proj = (float3x3)unity_CameraProjection;
    float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
    float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);

    // View space normal and depth
    half3 norm_o = SampleNormal(bsdfData);
    float depth_o = SampleDepth(posInput.unPositionSS / _Downsample);

    // Reconstruct the view-space position.
    float3 vpos_o = ReconstructViewPos(uv, depth_o, p11_22, p13_31);

    float ao = 0.0;

    // TODO: Setup several variant based on number of sample count to avoid dynamic loop here
    for (int s = 0; s < _SampleCount; s++)
    {
        // Sample point
        float3 v_s1 = PickSamplePoint(uv, s);
        v_s1 = faceforward(v_s1, -norm_o, v_s1);
        float3 vpos_s1 = vpos_o + v_s1;

        // Reproject the sample point
        float3 spos_s1 = mul(proj, vpos_s1);
        float2 uv_s1_01 = (spos_s1.xy / vpos_s1.z + 1.0) * 0.5;

        // Depth at the sample point
        float depth_s1 = SampleDepth(uint2(uv_s1_01 * _ScreenSize.xy));

        // Relative position of the sample point
        float3 vpos_s2 = ReconstructViewPos(uv_s1_01, depth_s1, p11_22, p13_31);
        float3 v_s2 = vpos_s2 - vpos_o;

        // Estimate the obscurance value
        float a1 = max(dot(v_s2, norm_o) - kBeta * depth_o, 0.0);
        float a2 = dot(v_s2, v_s2) + kEpsilon;
        ao += a1 / a2;
    }

    // Apply intensity normalization/amplifier/contrast.
    ao = pow(max(0, ao * _Radius * _Intensity / _SampleCount), kContrast);

    return PackAONormal(ao, norm_o);
}

#endif // UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_ESTIMATION
