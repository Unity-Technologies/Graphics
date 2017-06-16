#ifndef UNITY_HDRENDERPIPELINE_SSAO_AOESTIMATOR
#define UNITY_HDRENDERPIPELINE_SSAO_AOESTIMATOR

half _Intensity;
float _Radius;
half _Downsample;
int _SampleCount;

float3 SampleInsideHemisphere(float2 uv, half3 norm, int index)
{
    float gn = GradientNoise(uv * _Downsample);
    float2 u = frac(Hammersley2d(index, _SampleCount) + gn);
    float3 v = SampleSphereUniform(u.x, u.y);
    v *= sqrt((index + 1.0) / _SampleCount) * _Radius;
    return faceforward(v, -norm, v);
}

// Distance-based AO estimator based on Morgan 2011 http://goo.gl/2iz3P
half4 FragAO(Varyings input) : SV_Target
{
    PositionInputs posInput = GetPositionInput(input.positionCS.xy / _Downsample, _ScreenSize.zw);

    // Get normal, depth and view-space position of the center point.
    half3 norm_o = SampleNormal(posInput.unPositionSS);

    float depth_o_raw = LOAD_TEXTURE2D(_MainDepthTexture, posInput.unPositionSS).x;
    float depth_o = LinearEyeDepth(depth_o_raw, _ZBufferParams);

    if (!CheckDepth(depth_o_raw)) return PackAONormal(0, norm_o); // TODO: We should use the stencil to not affect the sky

    float3 vpos_o = ComputeViewSpacePosition(posInput.positionSS, depth_o_raw, _InvProjMatrix);

    float ao = 0.0;

    // TODO: Setup several variant based on number of sample count to avoid dynamic loop here
    for (int s = 0; s < _SampleCount; s++)
    {
        // Sample inside the hemisphere defined by the normal.
        float3 vpos_s1 = vpos_o + SampleInsideHemisphere(posInput.positionSS, norm_o, s);

        // Project the sample point and get the view-space position.
        float2 spos_s1 = float2(dot(unity_CameraProjection[0].xyz, vpos_s1),
                                dot(unity_CameraProjection[1].xyz, vpos_s1));
        float2 uv_s1_01 = saturate((spos_s1 / vpos_s1.z + 1.0) * 0.5);
        float depth_s1_raw = LOAD_TEXTURE2D(_MainDepthTexture, uint2(uv_s1_01 * _ScreenSize.xy)).x;
        float3 vpos_s2 = ComputeViewSpacePosition(uv_s1_01, depth_s1_raw, _InvProjMatrix);

        // Estimate the obscurance value
        float3 v_s2 = vpos_s2 - vpos_o;
        float a1 = max(dot(v_s2, norm_o) - kBeta * depth_o, 0.0);
        float a2 = dot(v_s2, v_s2) + kEpsilon;
        ao += CheckDepth(depth_s1_raw) ? a1 / a2 : 0;
    }

    // Apply intensity normalization/amplifier/contrast.
    ao = pow(max(0, ao * _Radius * _Intensity / _SampleCount), kContrast);

    return PackAONormal(ao, norm_o);
}

#endif // UNITY_HDRENDERPIPELINE_SSAO_AOESTIMATOR
