#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Reference/HairReferenceCommon.hlsl"

// Ref: [An Energy-Conserving Hair Reflectance Model]
// Plot: https://www.desmos.com/calculator/jmf1ofgfdv
float LongitudinalScattering(float cosThetaI, float cosThetaO, float sinThetaI, float sinThetaO, float v)
{
    float M;

    float a = cosThetaI * cosThetaO / v;
    float b = sinThetaI * sinThetaO / v;

    if (v < 0.1)
    {
        // Ref: [https://publons.com/review/414383/]
        // Small variances (< ~0.1) produce numerical issues due to limited floating precision.
        M = exp(LogBesselI(a) - b - rcp(v) + 0.6931 + log(rcp(2 * v)));
    }
    else
    {
        M = exp(-b) * BesselI(a) / HyperbolicCosecant(1 / v) * 2 * v;
    }

    return M;
}

CBSDF EvaluateHairReference(float3 V, float3 L, BSDFData bsdfData)
{
    ReferenceBSDFData data = GetReferenceBSDFData(L, bsdfData);
    ReferenceAngles angles = GetReferenceAngles(L, V, bsdfData);

    float3 F;

    F = float3(1, 0, 0);

    return HairFtoCBSDF(F);
}
