#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Reference/HairReferenceCommon.hlsl"

void ComputeFiberAttenuations(float cosThetaO, float eta, float h, float3 T, inout float3 Ap[PATH_MAX + 1])
{
    // Reconstruct the incident angle.
    float cosGammaO = SafeSqrt(1 - Sq(h));
    float cosTheta  = cosThetaO * cosGammaO;

    float F0 = IorToFresnel0(eta);
    float F  = F_Schlick(F0, cosTheta);

    // Solve for P == 0 (Reflection at the cuticle).
    Ap[0] = F;

    // Solve for P == 1 (Solves two air-hair boundary events and one transmission event).
    Ap[1] = Sq(1 - F) * T;

    // Solve for 2 < P < PMAX
    for (int p = 2; p < PATH_MAX; p++)
        Ap[p] = Ap[p - 1] * T * F;

    // Solve for the residual lobe PMAX < P < INFINITY
    // Ref: A Practical and Controllable Hair and Fur Model for Production Eq. 6
    Ap[PATH_MAX] = Ap[PATH_MAX - 1] * F * T / (1 - T * F);
}

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
        M = (exp(-b) * BesselI(a)) / (sinh(1 / v) * 2 * v);
    }

    return M;
}

float AzimuthalScattering(float phi, int p, float s, float gammaO, float gammaT)
{
    float dphi = phi - AzimuthalDirection(p, gammaO, gammaT);

    // Remap Phi to fit in the -PI..PI domain for the logistic.
    if (dphi > +PI) dphi -= TWO_PI;
    if (dphi < -PI) dphi += TWO_PI;

    return TrimmedLogistic(dphi, s, -PI, PI);
}

CBSDF EvaluateHairReference(float3 V, float3 L, BSDFData bsdfData)
{
    // Initialize the BSDF invocation.
    ReferenceBSDFData data = GetReferenceBSDFData(L, bsdfData);

    // Transform to the local frame for spherical coordinates
    float3x3 frame = GetLocalFrame(bsdfData.hairStrandDirectionWS);
    float3 wi = mul(L, transpose(frame));
    float3 wo = mul(V, transpose(frame));

    ReferenceAngles angles = GetReferenceAngles(wi, wo);

    // Find refracted ray angles.
    float sinThetaT = angles.sinThetaO / data.eta;
    float cosThetaT = SafeSqrt(1 - Sq(sinThetaT));

    // Find the modified index of refraction.
    float etaP = sqrt(Sq(data.eta) - Sq(angles.sinThetaO)) / angles.cosThetaO;

    // Compute refracted angle gamma T (exploiting the Bravais properties of a cylinder).
    float sinGammaT = data.h / etaP;
    float cosGammaT = SafeSqrt(1 - Sq(sinGammaT));
    float gammaT = clamp(FastASin(sinGammaT), -1, 1);

    // Compute transmittance of single path through the fiber using Beer's Law.
    // Ref: The Implementation of a Hair Scattering Model Eq. 1.4
    float3 T = exp(-data.sigmaA * (2 * cosGammaT / cosThetaT));

    // Compute the absorptions that occur in the fiber for every path.
    float3 A[PATH_MAX + 1];
    ComputeFiberAttenuations(angles.cosThetaO, data.eta, data.h, T, A);

    float3 F = 0;

    for (uint p = 0; p < PATH_MAX; p++)
    {
        float sinThetaO, cosThetaO;
        ApplyCuticleTilts(p, angles, data, sinThetaO, cosThetaO);

        F += LongitudinalScattering(angles.cosThetaI, cosThetaO, angles.sinThetaI, sinThetaO, data.v[p]) * A[p] *
             AzimuthalScattering(angles.phi, p, data.s, data.gammaO, gammaT);
    }

    // Compute the residual lobe
    F += LongitudinalScattering(angles.cosThetaI, angles.cosThetaO, angles.sinThetaI, angles.sinThetaO, data.v[PATH_MAX]) * A[PATH_MAX] / TWO_PI;

    if(abs(angles.cosThetaI) > 0)
        F /= abs(angles.cosThetaI);

    return HairFtoCBSDF(max(F, 0));
}
