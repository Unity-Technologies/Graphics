// The # of lobes to evaluate explicitly (R, TT, TRT, TRRT+..) before summing up the remainder with a residual lobe approximation.
#define PATH_MAX 3

#define HAIR_TYPE_RIBBON 1 << 0
#define HAIR_TYPE_TUBE   1 << 1

#define HAIR_TYPE HAIR_TYPE_TUBE

#define SQRT_PI_OVER_8 0.62665706865775012560

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
    float  sinAlpha[3];
    float  cosAlpha[3];

    float  s;               // Logistic Scale Factor
    float  v[PATH_MAX + 1]; // Longitudinal Variance
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

// Ref: Light Scattering from Human Hair Fibers Eq. 3
float AzimuthalDirection(uint p, float gammaO, float gammaT)
{
    return (2 * p * gammaT) - (2 * gammaO) + (p * PI);
}

float HyperbolicCosecant(float x)
{
    return rcp(sinh(x));
}

float Logistic(float x, float s)
{
    // Avoids numerical instability for large x / s ratios.
    x = abs(x);

    return exp(-x / s) / (s * Sq(1 + exp(-x / s)));
}

float LogisticCDF(float x, float s)
{
    return 1 / (1 + exp(-x / s));
}

float TrimmedLogistic(float x, float s, float a, float b)
{
    return Logistic(x, s) / ( LogisticCDF(b, s) - LogisticCDF(a, s) );
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

void LongitudinalVarianceFromBeta(float beta, inout float v[PATH_MAX + 1])
{
    // Ref: A Practical and Controllable Hair and Fur Model for Production Path Tracing Eq. 7
    v[0] = Sq((0.726 * beta) + (0.812 * beta * beta) + (3.7 * pow(beta, 20.0)));
    v[1] = 0.25 * v[0];
    v[2] =  4.0 * v[0];

    for (int p = 3; p <= PATH_MAX; ++p)
        v[p] = v[2];
}

// Ref: A Practical and Controllable Hair and Fur Model for Production Path Tracing Eq. 8
float LogisticScaleFromBeta(float beta)
{
    return SQRT_PI_OVER_8 * ((0.265 * beta) + (1.194 * beta * beta) + (5.372 * pow(beta, 22.0)));
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

void GetAlphaScalesFromAlpha(float alpha, inout float sinAlpha[3], inout float cosAlpha[3])
{
    sinAlpha[0] = sin(alpha);
    cosAlpha[0] = CosFromSin(sinAlpha[0]);

    // Get the lobe alpha terms by solving for the trigonometric double angle identities.
    for (int i = 1; i < 3; ++i)
    {
        sinAlpha[i] = 2 * cosAlpha[i - 1] * sinAlpha[i - 1];
        cosAlpha[i] = Sq(cosAlpha[i - 1]) - Sq(sinAlpha[i - 1]);
    }
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
    data.s      = LogisticScaleFromBeta(data.betaN);

    // Fill the list of variances from beta
    LongitudinalVarianceFromBeta(data.betaM, data.v);

    // Fill the alpha terms for each lobe
    GetAlphaScalesFromAlpha(data.alpha, data.sinAlpha, data.cosAlpha);

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
    angles.cosThetaI = CosFromSin(angles.sinThetaI);
    angles.cosThetaO = CosFromSin(angles.sinThetaO);

    angles.phiI = FastAtan2(wi.z, wi.y);
    angles.phiO = FastAtan2(wo.z, wo.y);
    angles.phi  = angles.phiO - angles.phiI;

    return angles;
}

// Quick utility to convert into a structure used in rasterizer.
CBSDF HairFtoCBSDF(float3 F)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    // Transmission is baked into the model.
    cbsdf.specR = F;

    return cbsdf;
}
