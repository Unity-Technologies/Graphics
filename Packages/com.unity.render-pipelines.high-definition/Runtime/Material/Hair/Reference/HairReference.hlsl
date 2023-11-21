#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Reference/HairReferenceCommon.hlsl"

// Reference implementation of a Marschner-based energy conserving hair reflectance model with concepts from:
// "The Implementation of a Hair Scattering Model" (Pharr 2016)
// "A Practical and Controllable Hair and Fur Model for Production Path Tracing" (Chiang 2016)
// "Importance Sampling for Physically-Based Hair Fiber Models" (D'Eon 2012)
// "An Energy-Conserving Hair Reflectance Model" (d'Eon 2011)
// "Light Scattering from Human Hair Fibers" (Marschner 2003)

void ComputeFiberAttenuations(float cosThetaO, float eta, float h, float3 T, inout float3 A[PATH_MAX + 1])
{
    // Reconstruct the incident angle.
    float cosGammaO = SafeSqrt(1 - Sq(h));
    float cosTheta  = cosThetaO * cosGammaO;

    float F0 = IorToFresnel0(eta);
    float F  = F_Schlick(F0, cosTheta);

    // Solve for P == 0 (Reflection at the cuticle).
    A[0] = F;

    // Solve for P == 1 (Solves two air-hair boundary events and one transmission event).
    A[1] = Sq(1 - F) * T;

    // Solve for 2 < P < PMAX
    for (uint p = 2; p < PATH_MAX; p++)
        A[p] = A[p - 1] * T * F;

    // Solve for the residual lobe PMAX < P < INFINITY
    // Ref: A Practical and Controllable Hair and Fur Model for Production Eq. 6
    A[PATH_MAX] = A[PATH_MAX - 1] * F * T / (1 - T * F);
}

void ComputeFiberAttenuationsPDF(float cosThetaO, float3 sigmaA, float eta, float h, inout float APDF[PATH_MAX + 1])
{
    float sinThetaO = SafeSqrt(1 - Sq(cosThetaO));

    // Compute the refracted ray
    float sinThetaT = sinThetaO / eta;
    float cosThetaT = SafeSqrt(1 - Sq(sinThetaT));

    float etaP = sqrt(Sq(eta) - Sq(sinThetaO)) / cosThetaO;
    float sinGammaT = h / etaP;
    float cosGammaT = SafeSqrt(1 - Sq(sinGammaT));

    // Compute transmittance of single path through the fiber using Beer's Law.
    float3 T = exp(-sigmaA * (2 * cosGammaT / cosThetaT));

    float3 A[PATH_MAX + 1];
    ComputeFiberAttenuations(cosThetaO, eta, h, T, A);

    float sumY = 0;

    int i;

    for (i = 0; i <= PATH_MAX; i++)
        sumY += Luminance(A[i]);

    for (i = 0; i <= PATH_MAX; i++)
        APDF[i] = Luminance(A[i]) / sumY;
}

// Ref: An Energy-Conserving Hair Reflectance Model Eq. 7
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

float AzimuthalScattering(float phi, uint p, float s, float gammaO, float gammaT)
{
    float dphi = phi - AzimuthalDirection(p, gammaO, gammaT);

    // Remap Phi to fit in the -PI..PI domain for the logistic.
    while (dphi > +PI) dphi -= TWO_PI;
    while (dphi < -PI) dphi += TWO_PI;

    return TrimmedLogistic(dphi, s, -PI, PI);
}

CBSDF EvaluateHairReference(float3 wo, float3 wi, BSDFData bsdfData)
{
    // Initialize the BSDF invocation.
    ReferenceBSDFData data = GetReferenceBSDFData(bsdfData);
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
    float3 T = exp(-data.sigmaA * (2 * cosGammaT / cosThetaT));

    // Compute the absorptions that occur in the fiber for every path.
    float3 A[PATH_MAX + 1];
    ComputeFiberAttenuations(angles.cosThetaO, data.eta, data.h, T, A);

    float3 F = 0;

    for (uint p = 0; p < PATH_MAX; ++p)
    {
        float sinThetaO, cosThetaO;
        ApplyCuticleTilts(p, angles, data, sinThetaO, cosThetaO);

        F += LongitudinalScattering(angles.cosThetaI, cosThetaO, angles.sinThetaI, sinThetaO, data.v[p]) * A[p] *
             AzimuthalScattering(angles.phi, p, data.s, data.gammaO, gammaT);
    }

    // Compute the residual lobe
    F += LongitudinalScattering(angles.cosThetaI, angles.cosThetaO, angles.sinThetaI, angles.sinThetaO, data.v[PATH_MAX]) * A[PATH_MAX] * INV_TWO_PI;

    if(abs(wi.z) > 0)
        F /= abs(wi.z + 1e-4);

    return HairFtoCBSDF(max(F, 0));
}

CBSDF SampleHairReference(float3 wo, out float3 wi, out float pdf, float4 u, BSDFData bsdfData)
{
    // Initialize the BSDF invocation.
    ReferenceBSDFData data = GetReferenceBSDFData(bsdfData);

    // Compute angles only for the currently known outgoing direction.
    ReferenceAngles angles;
    ZERO_INITIALIZE(ReferenceAngles, angles);

    angles.sinThetaO = wo.x;
    angles.cosThetaO = SafeSqrt(1 - Sq(angles.sinThetaO));
    angles.phiO = FastAtan2(wo.z, wo.y);

    // Determine the path to sample.
    float APDF[PATH_MAX + 1];
    ComputeFiberAttenuationsPDF(angles.cosThetaO, data.sigmaA, data.eta, data.h, APDF);

    int p;

    for (p = 0; p < PATH_MAX; p++)
    {
        if (u.x < APDF[p])
            break;

        u.x -= APDF[p];
    }

    float sinThetaO, cosThetaO;
    ApplyCuticleTilts(p, angles, data, sinThetaO, cosThetaO);

    // Note, clamping this sample seems required to prevent NaNs for very low (< ~0.1) variances.
    u.y = max(u.y, 1e-4);

    // Importance sample the longitudinal scattering function using an exponential function identity to handle low variance.
    // Ref: "Importance Sampling for Physically-Based Hair Fiber Models" Eq. 6 & 7
    // Ref: "Numerically stable sampling of the von Mises Fisher distribution on S2"
    float sampleMP  = 1 + data.v[p] * log(u.y + (1 - u.y) * exp(-2 / data.v[p]));
    float sinThetaI = -sampleMP * sinThetaO + SafeSqrt(1 - Sq(sampleMP)) * cos(TWO_PI * u.z) * cosThetaO;
    float cosThetaI = SafeSqrt(1 - Sq(sinThetaI));

    // Importance sample the azimuthal scattering function

    // Find the modified index of refraction.
    float etaP = sqrt(Sq(data.eta) - Sq(angles.sinThetaO)) / angles.cosThetaO;

    // Compute refracted angle gamma T (exploiting the Bravais properties of a cylinder).
    float sinGammaT = data.h / etaP;
    float gammaT = clamp(FastASin(sinGammaT), -1, 1);

    float phi;

    if (p < PATH_MAX)
        phi = AzimuthalDirection(p, data.gammaO, gammaT) + TrimmedLogisticSampled(u.w, data.s, -PI, PI);
    else
        phi = TWO_PI * u.w;

    // Construct the sampled direction wi.
    float phiI = angles.phiO + phi;
    wi = float3(sinThetaI, cosThetaI * cos(phiI), cosThetaI * sin(phiI));

    // Solve the overall PDF
    pdf = 0;

    for (p = 0; p < PATH_MAX; p++)
    {
        float sinThetaOp, cosThetaOp;
        ApplyCuticleTilts(p, angles, data, sinThetaOp, cosThetaOp);

        pdf += LongitudinalScattering(cosThetaI, cosThetaOp, sinThetaI, sinThetaOp, data.v[p]) * APDF[p] *
               AzimuthalScattering(phi, p, data.s, data.gammaO, gammaT);
    }

    // Don't forget the residual lobe
    pdf += LongitudinalScattering(cosThetaI, angles.cosThetaO, sinThetaI, angles.sinThetaO, data.v[PATH_MAX]) * APDF[PATH_MAX] * INV_TWO_PI;

    // Enforce a maximum pdf to prevent divide-by-zeros and NaN propagation in path tracer.
    pdf = max(pdf, 1e-3);

    return EvaluateHairReference(wo, wi, bsdfData);
}
