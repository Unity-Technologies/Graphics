// The # of lobes to evaluate explicitly (R, TT, TRT, TRRT+..) before summing up the remainder with a residual lobe approximation.
#define PATH_MAX 3

#define HAIR_TYPE_RIBBON 1 << 0
#define HAIR_TYPE_TUBE   1 << 1

#define HAIR_TYPE HAIR_TYPE_TUBE

// Precompute the factorials (should really precompute the squared value).
static const float FACTORIAL[11] = { 1.0,
                                     1.0,
                                     2.0,
                                     6.0,
                                     24.0,
                                     120.0,
                                     720.0,
                                     5040.0,
                                     40320.0,
                                     362880.0,
                                     3628800.0 };

struct ReferenceBSDFData
{
    float  h;      // Intersection offset from fiber center.
    float  eta;    // Refraction Index
    float3 sigmaA; // Absorption Coefficient of Cortex
    float  betaM;  // Longitudinal Roughness
    float  betaN;  // Azimuthal Roughness
    float  alpha;  // Cuticle Tilt
};

struct ReferenceAngles
{
    float sinThetaI;
    float sinThetaO;

    float cosThetaI;
    float cosThetaO;

    float phiI;
    float phiO;
    float phi;
};

float HyperbolicCosecant(float x)
{
    return rcp(sinh(x));
}

// Modified Bessel Function of the First Kind
float BesselI(float x)
{
    float b = 0;

    UNITY_UNROLL
    for (int i = 0; i <= 10; ++i)
    {
        const float f = FACTORIAL[i];
        b += pow(x, 2.0 * i) / (pow(4, i) * f * f);
    }

    return b;
}

float LogBesselI(float x)
{
    float lnIO;

    // The log of the bessel function may also be problematic for larger inputs (> ~12)...
    if (x > 12)
    {
        // ...in which case it's approximated.
        lnIO = x + 0.5 * (-log(TWO_PI) + log(rcp(x)) + rcp(8 * x));
    }
    else
    {
        lnIO = log(BesselI(x));
    }

    return lnIO;
}

// TODO: Currently we do not support ribbon in pathtracing.
float GetHFromRibbon(BSDFData bsdfData)
{
    return -1;
}

float GetHFromTube(float3 L, float3 N, float3 T)
{
    // Angle of inclination from normal plane.
    float sinTheta = dot(L, T);

    // Project w to the normal plane.
    float3 LProj = L - sinTheta * T;

    // Find gamma in the normal plane.
    float cosGamma = dot(LProj, N);

    // Length along the fiber width.
    return SinFromCos(cosGamma);
}

ReferenceBSDFData GetReferenceBSDFData(float3 L, BSDFData bsdfData)
{
    ReferenceBSDFData data;
    ZERO_INITIALIZE(ReferenceBSDFData, data);

#if HAIR_TYPE == HAIR_TYPE_TUBE
    data.h = GetHFromTube(L, bsdfData.geomNormalWS, bsdfData.hairStrandDirectionWS);
#elif HAIR_TYPE == HAIR_TYPE_RIBBON
    data.h = GetHFromRibbon(bsdfData);
#endif

    data.eta    = 1.55;
    data.sigmaA = bsdfData.absorption;
    data.betaM  = 0.3;
    data.betaN  = 0.3;
    data.alpha  = 2.0;

    return data;
}

ReferenceAngles GetReferenceAngles(float3 L, float3 V, BSDFData bsdfData)
{
    ReferenceAngles angles;
    ZERO_INITIALIZE(ReferenceAngles, angles);

    // Transform to the local frame for spherical coordinates
    float3x3 frame = GetLocalFrame(bsdfData.hairStrandDirectionWS);
    float3 wi = TransformWorldToTangent(L, frame);
    float3 wo = TransformWorldToTangent(V, frame);

    angles.sinThetaI = wi.x;
    angles.sinThetaO = wo.x;

    // This is technically "CosFromSin", but does the same thing. Worth adding for readability?
    angles.cosThetaI = SinFromCos(angles.sinThetaI);
    angles.cosThetaO = SinFromCos(angles.sinThetaO);

    angles.phiI = FastAtan2(wi.z, wi.y);
    angles.phiO = FastAtan2(wo.z, wo.y);
    angles.phi  = angles.phiO - angles.phiI;

    return angles;
}

CBSDF HairFtoCBSDF(float3 F)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    // Transmission is baked into the model.
    cbsdf.specR = F;

    return cbsdf;
}
