#ifndef BLEND_STATES
#define BLEND_STATES

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

APVSample BlendAPVSamples(APVSample state0, APVSample state1, float factor)
{
    APVSample result;
    result.L0   = lerp(state0.L0,   state1.L0,   factor);
    result.L1_R = lerp(state0.L1_R, state1.L1_R, factor);
    result.L1_G = lerp(state0.L1_G, state1.L1_G, factor);
    result.L1_B = lerp(state0.L1_B, state1.L1_B, factor);
#ifdef PROBE_VOLUMES_L2
    result.L2_R = lerp(state0.L2_R, state1.L2_R, factor);
    result.L2_G = lerp(state0.L2_G, state1.L2_G, factor);
    result.L2_B = lerp(state0.L2_B, state1.L2_B, factor);
    result.L2_C = lerp(state0.L2_C, state1.L2_C, factor);
#endif
    result.status = APV_SAMPLE_STATUS_DECODED;
    return result;
}

void EncodeAndStoreAPV(APVResourcesRW apvRes, APVSample apvSample, int3 loc)
{
    apvSample.Encode();

    float4 L0_L1Rx = float4(apvSample.L0, apvSample.L1_R.x);
    float4 L1G_L1Ry = float4(apvSample.L1_G, apvSample.L1_R.y);
    float4 L1B_L1Rz = float4(apvSample.L1_B, apvSample.L1_R.z);

    apvRes.L0_L1Rx [loc].rgba = L0_L1Rx;
    apvRes.L1G_L1Ry[loc].rgba = L1G_L1Ry;
    apvRes.L1B_L1Rz[loc].rgba = L1B_L1Rz;

#ifdef PROBE_VOLUMES_L2
    apvRes.L2_0[loc].rgba = apvSample.L2_R;
    apvRes.L2_1[loc].rgba = apvSample.L2_G;
    apvRes.L2_2[loc].rgba = apvSample.L2_B;
    apvRes.L2_3[loc].rgba = float4(apvSample.L2_C, 0.0f);
#endif
}

#endif
