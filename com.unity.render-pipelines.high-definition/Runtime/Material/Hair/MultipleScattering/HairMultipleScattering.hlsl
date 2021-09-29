TEXTURE3D(_PreIntegratedHairFiberScattering);
TEXTURE2D(_PreIntegratedAverageHairFiberScattering);

#define FRONT 0
#define BACK  1

struct HairScatteringData
{
    // 0: Front Hemisphere, 1: Back Hemisphere
    float3 averageScattering [2];
    float3 averageVariance   [2];
    float3 averageShift      [2];

    // Average azimuthal scattering.
    float3 NG;
};

#define HALF_SQRT_INV_PI    0.28209479177387814347
#define HALF_SQRT_3_DIV_PI  0.48860251190291992158

// Returns the approximate strand count in direction L from an L1 band spherical harmonic.
float DecodeHairStrandCount(float3 L, float4 strandCountProbe)
{
    float4 Ylm = float4(
        HALF_SQRT_INV_PI,
        HALF_SQRT_3_DIV_PI * L.y,
        HALF_SQRT_3_DIV_PI * L.z,
        HALF_SQRT_3_DIV_PI * L.x
    );

    return abs(dot(strandCountProbe, Ylm));
}

float GetDirectFraction(BSDFData bsdfData, float strandCount)
{
    // Defer to the higher quality spline visibility for this light, if any.
    // Otherwise fall back to the coarse approximation from the spherical harmonic.
    return bsdfData.splineVisibility > -1 ? bsdfData.splineVisibility : 1 - saturate(strandCount);
}

// TODO: Currently the dual scattering approximation is assuming to be used for hair fibers, but it can be used for other
// fiber materials (ie Fabric). It would be good to eventually generalize this and move it out of hair material evaluation.
HairScatteringData GetHairScatteringData(BSDFData bsdfData, float3 alpha, float3 beta, float sinThetaI, float cosThetaD, float cosPhi, float3 A[3])
{
    HairScatteringData scatteringData;
    ZERO_INITIALIZE(HairScatteringData, scatteringData);

    // 1) Sample the average scattering.
    {
        // Prepare the sampling coordinate.
        float  X = PerceptualRoughnessToPerceptualSmoothness(bsdfData.perceptualRoughness);
        float  Y = abs(sinThetaI);
        float3 Z = clamp(bsdfData.diffuseColor, 0.01, 0.99); // Need to clamp the absorption a bit due to artifacts at these boundaries.

        // Sample the LUT for each color channel (wavelength).
        // Note that we parameterize by diffuse color, not absorption, to fit in [0, 1].
        float2 R = SAMPLE_TEXTURE3D_LOD(_PreIntegratedHairFiberScattering, s_linear_clamp_sampler, float3(X, Y, Z.r), 0).xy;
        float2 G = SAMPLE_TEXTURE3D_LOD(_PreIntegratedHairFiberScattering, s_linear_clamp_sampler, float3(X, Y, Z.g), 0).xy;
        float2 B = SAMPLE_TEXTURE3D_LOD(_PreIntegratedHairFiberScattering, s_linear_clamp_sampler, float3(X, Y, Z.b), 0).xy;

        scatteringData.averageScattering[FRONT] = float3(R.x, G.x, B.x);
        scatteringData.averageScattering[BACK]  = float3(R.y, G.y, B.y);
    }

    // 2) Sample the average azimuthal scattering
    {
        // Prepare the sampling coordiante
        float X = (FastACos(cosPhi) + TWO_PI) / FOUR_PI;
        float Y = cosThetaD;

        // Sample the LUT for each color channel (wavelength).
        // Note that we parameterize by diffuse color, not absorption, to fit in [0, 1].
        scatteringData.NG = SAMPLE_TEXTURE2D_LOD(_PreIntegratedAverageHairFiberScattering, s_linear_clamp_sampler, float2(X, Y), 0).xyz;
    }

    // 3) Compute the average scattering variance & shift
    {
        const float3 af = scatteringData.averageScattering[FRONT];
        const float3 ab = scatteringData.averageScattering[BACK];

        // Here we should be deriving the average variance with the per-component (R, TT, TRT)
        // BSDF average in the hemisphere, and not the BSDF average per-absorption channel.
        float3 afW = af / dot(1, af);
        float3 abW = ab / dot(1, ab);

        scatteringData.averageVariance[FRONT] = dot(beta, afW);  // Front scattering (Disney Eq. 15)
        scatteringData.averageVariance[BACK]  = dot(beta, abW);  // Back scattering  (Disney Eq. 16)

        // scatteringData.averageShift[FRONT] = dot(alpha, afW); // Front scattering (Disney Eq. 13)
        // scatteringData.averageShift[BACK]  = dot(alpha, abW); // Back scattering (Disney Eq. 14)
    }

    return scatteringData;
}

float3 EvaluateMultipleScattering(float3 L, float3 Fs, BSDFData bsdfData, float3 alpha, float3 beta, float thetaH, float sinThetaI, float cosThetaD, float cosPhi, float3 A[3])
{
    // Clamp the roughnesses
    beta = max(0.01, beta);

    // Fetch the various preintegrated data.
    HairScatteringData hairScatteringData = GetHairScatteringData(bsdfData, alpha, beta, sinThetaI, cosThetaD, cosPhi, A);

    // Solve for multiple scattering in a volume of hair fibers with concepts from:
    // "Dual Scattering Approximation for Fast Multiple Scattering in Hair" (Zinke et. al)
    // "Efficient Implementation of the Dual Scattering Model in RenderMan" (Sadeghi et. al)
    // "A BSSRDF Model for Efficient Rendering of Fur with Global Illumination" (Yan et. al)

    // Pre-define some shorthand for the symbols.
    const float  n    = DecodeHairStrandCount(L, bsdfData.strandCountProbe);
    const float3 af   = hairScatteringData.averageScattering[FRONT];
    const float3 ab   = hairScatteringData.averageScattering[BACK];
    const float3 sf   = dot(alpha, 1) / 0.5; // Hack: Right now this is necesarry to shift delta B down to match the reference
    const float3 sb   = sf;
    const float3 Bf   = hairScatteringData.averageVariance[FRONT];
    const float3 Bb   = hairScatteringData.averageVariance[BACK];
    const float3 Bf2  = Sq(Bf);
    const float3 Bb2  = Sq(Bb);

    // Global scattering.
    // -----------------------------------------------------------------------------------
    // Following the observation of Zinke et. al., density factor (ratio of occlusion of the shading point by neighboring strands)
    // can be approximated with this constant to match most path traced references.
    const float df = 0.7;

    // Approximate the transmittance by assuming that all hair strands between the shading point and the light are
    // oriented the same. This is suitable for long, straighter hair ( Eq. 6 Disney ).
    float3 Tf = df * pow(af, n);

    // Approximate the accumulated variance, by assuming strands all have the same average roughness and inclination. ( Eq. 7 Disney )
    float3 sigmaF = Bf2 * n;

    const float directFraction = GetDirectFraction(bsdfData, n);
    Tf     = lerp(Tf,     1, directFraction);
    sigmaF = lerp(sigmaF, 0, directFraction);

    // Local scattering.
    // ------------------------------------------------------------------------------------

    // Similarly to front scattering, this same density coefficient is suggested for matching most path traced references.
    const float db = 0.7;

    // Compute the average backscattering attenuation, the attenuation in the neighborhood of x.
    // Here we only model the first and third backscattering event, as the following are negligible.

    // Ex. of a single backward scattering event. Where L is the incident light, V is the camera, (F) is a fiber cross-section
    // with a forward scattering event, and (B) is a fiber cross section with a backward scattering event.
    //
    // V <---------------
    //                  |
    //                 (F) <--- ... ---> (B)
    // L -------------->|

    float3 af1 = af;
    float3 af2 = Sq(af1);

    float3 afI1 = 1 - af2;
    float3 afI2 = Sq(afI1);
    float3 afI3 = afI2 * afI1;

    float3 ab1 = ab;
    float3 ab2 = Sq(ab1);
    float3 ab3 = ab2 * ab1;

    // Analytic solutions to the potential infinite permutations of backward scattering
    // in a volume of fibers for one and three backward scatters ( Eq. 11, 13, & 14 ).
    float3 A1 = (ab1 * af2) / afI1;
    float3 A3 = (ab3 * af2) / afI3;
    float3 Ab = A1 + A3;

    // Computes the average longitudinal shift ( Eq. 16  ).
    float3 shiftB = 1 - ((2 * ab2) / afI2);
    float3 shiftF = ((2 * afI2) + (4 * af2 * ab2)) / afI3;
    float3 deltaB = (sf * shiftB) + (sb * shiftF);

    // Compute the average back scattering standard deviation ( Eq. 17 ).
    float3 sigmaB = (1 + db * af2);
    sigmaB *= (ab * sqrt((2 * Bf2) + Bb2)) + (ab3 * sqrt((2 * Bf2) + Bb2));
    sigmaB /= ab + (ab3 * ((2 * Bf) + (3 * Bb)));
    sigmaB  = Sq(sigmaB);

    // Resolve the overall local scattering term ( Eq. 19 & 20 Disney ).
    // Note, for now remove the square, there seems to be a discrepancy in the gaussian used in the paper (ours takes std. dev)
    float3 fsBack = db * 2 * Ab * D_LongitudinalScatteringGaussian(thetaH - deltaB, sqrt(sigmaB + sigmaF)) / PI;

    // Resolve the approximated multiple scattering. (Approximate Eq. 22)
    // ------------------------------------------------------------------------------------
    const float3 MG = D_LongitudinalScatteringGaussian(thetaH - alpha, beta + sqrt(sigmaF));

    const float3x3 NG = float3x3( hairScatteringData.NG.x * A[0],
                                  hairScatteringData.NG.y * A[1],
                                  hairScatteringData.NG.z * A[2] );

    const float3 fsScatter = mul(MG, NG);

    // TODO: It's possible a divide by PI is missing in the BSDF approximation, because this is required to match the reference.
    Fs /= PI;

    const float3 Fdirect   = directFraction * (Fs + fsBack);
    const float3 Fscatter  = (Tf - directFraction) * df * (fsScatter + PI * fsBack);
    const float3 F         = (Fdirect + Fscatter) * sqrt(1 - Sq(sinThetaI));

    return max(F, 0);
}
