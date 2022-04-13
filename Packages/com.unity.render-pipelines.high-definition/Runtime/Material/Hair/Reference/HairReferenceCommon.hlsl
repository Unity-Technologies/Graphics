// The # of lobes to evaluate explicitly (R, TT, TRT, TRRT+..) before summing up the remainder with a residual lobe approximation.
#define PATH_MAX 3

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
    float  gammaO;

    float  eta;    // Refraction Index
    float3 sigmaA; // Absorption Coefficient of Cortex
    float  betaM;  // Longitudinal Roughness
    float  betaN;  // Azimuthal Roughness

    float  alpha;  // Cuticle Tilt (Radians)
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

float TrimmedLogisticSampled(float u, float s, float a, float b)
{
    float k = LogisticCDF(b, s) - LogisticCDF(a, s);
    float x = -s * log(1 / (u * k + LogisticCDF(a, s)) - 1);

    return clamp(x, a, b);
}

// Modified Bessel Function of the First Kind
float BesselI(float x)
{
    float b = 0;

    UNITY_UNROLL
    for (int i = 0; i < 10; ++i)
    {
        const float f = FACTORIAL[i];
        b += pow(abs(x), 2.0 * i) / (pow(4, i) * f * f);
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
    v[0] = Sq(0.726 * beta + 0.812 * Sq(beta) + 3.7 * pow(abs(beta), 20.0));
    v[1] = 0.25 * v[0];
    v[2] =  4.0 * v[0];

    for (int p = 3; p <= PATH_MAX; p++)
        v[p] = v[2];
}

// Ref: A Practical and Controllable Hair and Fur Model for Production Path Tracing Eq. 8
float LogisticScaleFromBeta(float beta)
{
    return SQRT_PI_OVER_8 * ((0.265 * beta) + (1.194 * beta * beta) + (5.372 * pow(abs(beta), 22.0)));
}

void ApplyCuticleTilts(uint p, ReferenceAngles angles, ReferenceBSDFData data, out float sinThetaO, out float cosThetaO)
{
    if (p == 0)
    {
        sinThetaO = angles.sinThetaO * data.cosAlpha[1] - angles.cosThetaO * data.sinAlpha[1];
        cosThetaO = angles.cosThetaO * data.cosAlpha[1] + angles.sinThetaO * data.sinAlpha[1];
    }
    else if (p == 1)
    {
        sinThetaO = angles.sinThetaO * data.cosAlpha[0] + angles.cosThetaO * data.sinAlpha[0];
        cosThetaO = angles.cosThetaO * data.cosAlpha[0] - angles.sinThetaO * data.sinAlpha[0];
    }
    else if (p == 2)
    {
        sinThetaO = angles.sinThetaO * data.cosAlpha[2] + angles.cosThetaO * data.sinAlpha[2];
        cosThetaO = angles.cosThetaO * data.cosAlpha[2] - angles.sinThetaO * data.sinAlpha[2];
    }
    else
    {
        sinThetaO = angles.sinThetaO;
        cosThetaO = angles.cosThetaO;
    }

    // Need to clamp for possible out of range after cuticle tilt application.
    cosThetaO = abs(cosThetaO);
}

void GetAlphaScalesFromAlpha(float alpha, inout float sinAlpha[3], inout float cosAlpha[3])
{
    sinAlpha[0] = sin(alpha);
    cosAlpha[0] = SafeSqrt(1 - Sq(sinAlpha[0]));

    // Get the lobe alpha terms by solving for the trigonometric double angle identities.
    for (int i = 1; i < 3; ++i)
    {
        sinAlpha[i] = 2 * cosAlpha[i - 1] * sinAlpha[i - 1];
        cosAlpha[i] = Sq(cosAlpha[i - 1]) - Sq(sinAlpha[i - 1]);
    }
}

ReferenceBSDFData GetReferenceBSDFData(BSDFData bsdfData)
{
    ReferenceBSDFData data;
    ZERO_INITIALIZE(ReferenceBSDFData, data);

    data.h      = bsdfData.h;
    data.gammaO = FastASin(data.h);
    data.eta    = 1.55;
    data.sigmaA = bsdfData.absorption;
    data.betaM  = bsdfData.perceptualRoughness;
    data.betaN  = bsdfData.perceptualRoughnessRadial;
    data.alpha  = bsdfData.cuticleAngle;
    data.s      = LogisticScaleFromBeta(data.betaN);

    // Fill the list of variances from beta
    LongitudinalVarianceFromBeta(data.betaM, data.v);

    // Fill the alpha terms for each lobe
    GetAlphaScalesFromAlpha(data.alpha, data.sinAlpha, data.cosAlpha);

    return data;
}

ReferenceAngles GetReferenceAngles(float3 wi, float3 wo)
{
    ReferenceAngles angles;
    ZERO_INITIALIZE(ReferenceAngles, angles);

    angles.sinThetaI = wi.x;
    angles.sinThetaO = wo.x;

    // Small epsilon to suppress various compiler warnings + div-zero guard
    const float epsilon = 1e-5;

    angles.cosThetaI = SafeSqrt(1 - (Sq(angles.sinThetaI) + epsilon));
    angles.cosThetaO = SafeSqrt(1 - (Sq(angles.sinThetaO) + epsilon));

    angles.phiI = atan2(wi.z, wi.y + epsilon);
    angles.phiO = atan2(wo.z, wo.y + epsilon);
    angles.phi  = angles.phiI - angles.phiO;

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
