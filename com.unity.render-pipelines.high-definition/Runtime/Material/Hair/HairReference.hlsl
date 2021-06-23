// #define HAIR_REFERENCE_NEAR_FIELD
// #define HAIR_REFERENCE_LONGITUDINAL_ENERGY_CONSERVING

struct ReferenceInputs
{
    float thetaI;
    float thetaR;
    float thetaD;
    float thetaT;

    float phiI;
    float phiR;
    float phi;

    float LdotV;

    float h;

    float eta;
    float etaP;
    float3 fresnel0;

    float shifts[3];
    float variances[3];
    float logisticScale;

    float3 absorption;
    float3 absorptionP;
};

float ModifiedIOR(float ior, float thetaD)
{
    float sinThetaD = sin(thetaD);
    float num = (ior * ior) - (sinThetaD * sinThetaD);
    return sqrt(num) / cos(thetaD);
}

float HyperbolicCosecant(float x)
{
    return rcp(sinh(x));
}

// Plot: https://www.desmos.com/calculator/4dnfmn9xal
float RoughnessToLongitudinalVariance(float roughness)
{
#ifdef HAIR_REFERENCE_LONGITUDINAL_ENERGY_CONSERVING
    float beta = roughness;
    float v = (0.726 * beta) + (0.812 * beta * beta) + (3.7 * pow(beta, 20.0));
    return v * v;
#else
    return roughness;
#endif
}

float RoughnessToLogisticalScale(float roughness)
{
    float beta = roughness;
    return (0.265 * beta) + (1.194 * beta * beta) + (5.372 * pow(beta, 22.0));
}

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

// Remap the azimuthal direction to the normalized logistic function on -PI to PI.
float RemapLogisticAngle(float a)
{
    if (a < -PI)
        a += TWO_PI;

    if (a > +PI)
        a -= TWO_PI;

    return a;
}

// Ref: Light Scattering from Human Hair Fibers
float AzimuthalDirection(uint p, float etaPrime, float h)
{
    float gammaI = asin(h);
    float gammaT = asin(h / etaPrime);
    float omega = (2 * p * gammaT) - (2 * gammaI) + (p * PI);

    return omega;
}

float3 Attenuation(uint p, float h, float LdotV, float thetaD, float etaPrime, float3 fresnel0, float3 absorption)
{
    float3 A;

    if (p == 0)
    {
        // Attenuation term for R is a special case.s
        A = F_Schlick(fresnel0, sqrt(0.5 + 0.5 * LdotV));
    }
    else
    {
        float3 f = F_Schlick(fresnel0, acos(cos(thetaD) * cos(asin(h))));
        float gammaT = asin(h / etaPrime);
        float3 T = exp(-2 * absorption * (1 + cos(2 * gammaT)));

        // A = pow(1 - f, 2.0) * pow(f, p - 1) * pow(T, p);

        if (p == 1)
            A = pow(1 - f, 2.0) * T;
        else
            A = pow(1 - f, 2.0) * f * (T * T);
    }

    return A;
}

// Ref: [A Practical and Controllable Hair and Fur Model for Production Path Tracing]
// Plot: https://www.desmos.com/calculator/cmy0eig6ln
float LogisticAzimuthalAngularDistribution(float x, float s)
{
    const float a = -PI;
    const float b = +PI;

    const float scalePeakTerm = sqrt(PI / 8.0);
    s *= scalePeakTerm;

    float normalizeTerm = rcp(rcp(1 + exp(a / s)) - rcp(1 + exp(b / s)));

    float distributionN = exp(-x / s);
    float distributionD = s * Sq(1 + distributionN);

    return normalizeTerm * (distributionN / distributionD);
}

float Gaussian(float beta, float phi)
{
    return exp(-0.5 * (phi * phi) / (beta * beta)) * rcp(sqrt(TWO_PI) * beta);
}

// Ref: [An Energy-Conserving Hair Reflectance Model]
float GaussianDetector(float beta, float phi)
{
    float D = 0;

    // Higher order detection is negligible for (beta < 80º).
    int order = 4;

    for (int k = -order; k <= order; k++)
    {
        D += Gaussian(beta, phi - (TWO_PI * k));
    }

    return D;
}

float3 AzimuthalScatteringNearField(uint p, ReferenceInputs inputs)
{
    // Evaluation of near field azimuthal scattering is done with the true offset (h).
    // It leverages the monte carlo integration of the pathtracer to solve the full integral.
    float3 A = Attenuation(p, inputs.h, inputs.LdotV, inputs.thetaD, inputs.etaP, inputs.fresnel0, inputs.absorptionP);

    float azimuth = AzimuthalDirection(p, inputs.etaP, inputs.h);

    // Remap to the logistic function.
    azimuth = RemapLogisticAngle(azimuth);

    float D = LogisticAzimuthalAngularDistribution(inputs.logisticScale, inputs.phi - azimuth);

    return A * D;
}

// Plot: https://www.desmos.com/calculator/i86ekgtzlg
float3 AzimuthalScatteringFarField(uint p, ReferenceInputs inputs)
{
    // Integrate azimuthal scattering over the fiber width using a gaussian quadrature.
    // Np(phi) = 0.5 * Int{-1, 1}{A(p, h) * D(phi - Omega)dh} where h is the fiber axis offset.
    float3 N = 0;

    // Quadrature of order 35 is sufficient for all but very smooth hairs (beta < 2º).
    const uint n = 35;

    for (uint i = 0; i < n; i++)
    {
        // Remap h to -1..1
        float h = 2 * ((float)i / n) - 1;

        float3 A = Attenuation(p, h, inputs.LdotV, inputs.thetaD, inputs.etaP, inputs.fresnel0, inputs.absorptionP);

        float omega = AzimuthalDirection(p, inputs.etaP, h);

        float D = GaussianDetector(inputs.logisticScale, inputs.phi - omega);

        N += A * D;
    }

    N *= 2.0 / n;

    return 0.5 * N;
}

// Ref: [An Energy-Conserving Hair Reflectance Model]
// Plot: https://www.desmos.com/calculator/jmf1ofgfdv
float LongitudinalScattering(uint p, ReferenceInputs inputs)
{
    const float v = max(0.0001, inputs.variances[p]);
    float thetaI  = inputs.thetaI;
    float thetaR  = inputs.thetaR;

    float M;

#ifdef HAIR_REFERENCE_LONGITUDINAL_ENERGY_CONSERVING
    // Apply the cuticle shift.
    thetaR -= inputs.shifts[p];

    if (v < 0.1)
    {
        // Ref: [https://publons.com/review/414383/]
        // Small variances (< ~0.1) produce numerical issues due to limited floating precision.
        float a = (cos(-thetaI) * cos(thetaR)) / v;
        float b = (sin(-thetaI) * sin(thetaR)) / v;

        // The log of the bessel function may also be problematic for larger inputs (> ~12)...
        float lnI0;
        if (a > 12)
        {
            // ...in which case it's approximated.
            lnI0 = a + 0.5 * (-log(TWO_PI) + log(rcp(a)) + rcp(8 * a));
        }
        else
        {
            lnI0 = log(BesselI(a));
        }

        M = exp(lnI0 + b - rcp(v) + 0.6931 + log(rcp(2 * v)));
    }
    else
    {
        M  = HyperbolicCosecant(rcp(v)) / (2 * v);
        M *= exp((sin(-thetaI) * sin(thetaR)) / v);
        M *= BesselI((cos(-thetaI) * cos(thetaR)) / v);
    }
#else
    const float thetaH = 0.5 * (thetaI + thetaR);
    M = D_LongitudinalScatteringGaussian(thetaH - inputs.shifts[p], v);
#endif

    return M;
}

float3 AzimuthalScattering(uint p, ReferenceInputs inputs)
{
    float3 N;

#ifdef HAIR_REFERENCE_NEAR_FIELD
    // Disney Integration of N(phi, h) (Near-Field).
    N = AzimuthalScatteringNearField(p, inputs);
#else
    // D'Eon's integration over fiber width and Gaussian Detector (Far-Field).
    N = AzimuthalScatteringFarField(p, inputs);
#endif

    return N;
}

CBSDF EvaluateMarschnerReference(float3 V, float3 L, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    // Transform to the local frame for spherical coordinates
    float3x3 frame = GetLocalFrame(bsdfData.hairStrandDirectionWS);
    float3 I = TransformWorldToTangent(L, frame);
    float3 R = TransformWorldToTangent(V, frame);

    ReferenceInputs inputs;
    ZERO_INITIALIZE(ReferenceInputs, inputs);

    // Model Reference Inputs.
    // Notation Ref: Light Scattering from Human Hair Fibers
    {
        // Longitudinal
        inputs.thetaI = HALF_PI - acos(I.z);
        inputs.thetaR = HALF_PI - acos(R.z);
        inputs.thetaD = (inputs.thetaR - inputs.thetaI) * 0.5;

        // Azimuthal
        float phiI = atan2(I.y, I.x);
        float phiR = atan2(R.y, R.x);
        inputs.phi = phiR - phiI;

        inputs.variances[0] = RoughnessToLongitudinalVariance(bsdfData.roughnessR);
        inputs.variances[1] = RoughnessToLongitudinalVariance(bsdfData.roughnessTT);
        inputs.variances[2] = RoughnessToLongitudinalVariance(bsdfData.roughnessTRT);

        inputs.shifts[0] = bsdfData.cuticleAngleR;
        inputs.shifts[1] = bsdfData.cuticleAngleTT;
        inputs.shifts[2] = bsdfData.cuticleAngleTRT;

        inputs.eta = 1.55;
        inputs.fresnel0 = bsdfData.fresnel0;

        // The analysis of azimuthal scattering can be restricted to the normal plane by exploiting
        // the Bravais properties of a smooth cylinder fiber and using the modified index of refraction.
        inputs.etaP = ModifiedIOR(inputs.eta, inputs.thetaD);

        inputs.LdotV = dot(L, V);

#ifdef HAIR_REFERENCE_NEAR_FIELD
        // Evaluation of h in the normal plane, given by gammaI = asin(h), where gammaI is the incident angle.
        // Since we are using a near-field method, we can use the true h value (rather than integrating over the whole fiber width).
        inputs.h = sin(acos(dot(bsdfData.normalWS, L)));

        inputs.logisticScale = RoughnessToLogisticalScale(bsdfData.roughnessRadial);
#else
        // TODO: Maintain the Disney parameterization for the far field model.
        inputs.logisticScale = bsdfData.roughnessRadial;
#endif

        float thetaT = asin(sin(inputs.thetaR / inputs.eta));
        inputs.absorptionP = bsdfData.absorption / cos(thetaT);
    }

    float3 S = 0;

    // Factored lobe representation. Sigma Sp(thetai, thetao, phi) = Mp(thetai, thetao) * Np(phi).
    for (uint p = 0; p < 3; p++)
    {
        // TEMP: Lobe (R, TT, TRT, TRRT) selection
        // if (p == 0) continue;
        // if (p == 1) continue;
        // if (p == 2) continue;

        S += LongitudinalScattering(p, inputs) * AzimuthalScattering(p, inputs);
    }

    // Suppress NaNs.
    S = saturate(S);

    // Transmission is currently built in to the model. Should the TT lobe be separated?
    cbsdf.specR = S;

    return cbsdf;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env - Reference
// ----------------------------------------------------------------------------

float3 IntegrateMarschnerIBLRef(LightLoopContext lightLoopContext,
                                float3 V, PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                uint sampleCount = 16)
{
    float3 acc = float3(0.0, 0.0, 0.0);

    // Add some jittering on Hammersley2d
    float2 randNum = InitRandom(V.xy * 0.5 + 0.5);

    // Integrate over the sphere due to reflective and transmissive events in the BSDF.
    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u = frac(u + randNum);

        float3 L = SampleSphereUniform(u.x, u.y);

        // Incident Light intensity
        float4 val = SampleEnv(lightLoopContext, lightData.envIndex, L, 0, lightData.rangeCompressionFactorCompensation, 0.5);

        // BRDF Data
        CBSDF cbsdf = EvaluateMarschnerReference(V, L, bsdfData);

        float weight = rcp(INV_FOUR_PI * sampleCount);
        acc += val.rgb * cbsdf.specR * weight;
    }

    return acc;
}
