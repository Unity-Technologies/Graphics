// Inform the lightloop and evaluation to perform the necesarry tracing work and invoke the hair dual scattering implementation.
#define LIGHT_EVALUATES_MULTIPLE_SCATTERING

#define STRAND_DIAMETER_MILLIMETERS 0.5
#define STRAND_DIAMETER_METERS      (STRAND_DIAMETER_MILLIMETERS * METERS_PER_MILLIMETER)

#define DIRECT_ILLUMINATION_THRESHOLD 1

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
    float3x3 NG;
};

float3 ScatteringSpreadGaussian(float3 x, float3 v)
{
    return rsqrt(TWO_PI * v) * exp(-Sq(x) / (2 * v));
}

// TODO: Currently the dual scattering approximation is assuming to be used for hair fibers, but it can be used for other
// fiber materials (ie Fabric). It would be good to eventually generalize this and move it out of hair material evaluation.
HairScatteringData GetHairScatteringData(BSDFData bsdfData, float3 alpha, float3 beta, float sinThetaI)
{
    HairScatteringData scatteringData;
    ZERO_INITIALIZE(HairScatteringData, scatteringData);

    // 1) Sample the average scattering.
    {
        // Prepare the sampling coordinate.
        float  X = PerceptualRoughnessToPerceptualSmoothness(bsdfData.perceptualRoughness);
        float  Y = abs(sinThetaI);
        float3 Z = clamp((bsdfData.diffuseColor), 0.01, 0.99); // Need to clamp the absorption a bit due to artifacts at these boundaries.

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
        float  X = abs(sinThetaI);
        float3 Y = clamp((bsdfData.diffuseColor), 0.01, 0.99); // Need to clamp the absorption a bit due to artifacts at these boundaries.

        // Sample the LUT for each color channel (wavelength).
        // Note that we parameterize by diffuse color, not absorption, to fit in [0, 1].
        float3 R = SAMPLE_TEXTURE2D_LOD(_PreIntegratedAverageHairFiberScattering, s_linear_clamp_sampler, float2(X, Y.r), 0).xyz;
        float3 G = SAMPLE_TEXTURE2D_LOD(_PreIntegratedAverageHairFiberScattering, s_linear_clamp_sampler, float2(X, Y.g), 0).xyz;
        float3 B = SAMPLE_TEXTURE2D_LOD(_PreIntegratedAverageHairFiberScattering, s_linear_clamp_sampler, float2(X, Y.b), 0).xyz;

        scatteringData.NG = float3x3( float3(R.x, G.x, B.x),
                                      float3(R.y, G.y, B.y),
                                      float3(R.z, G.z, B.z) );
    }

    // 2) Compute the average scattering variance
    {
        // Here we should be deriving the average variance with the per-component (R, TT, TRT)
        // BSDF average in the hemisphere, and not the BSDF average per-absorption channel.

        // Front scattering (Disney Eq. 15)
        {
            const float3 af = scatteringData.averageScattering[FRONT];
            scatteringData.averageVariance[FRONT] = dot(beta, af) / (af.r + af.g + af.b);
        }

        // Back scattering (Disney Eq. 16)
        {
            const float3 ab = scatteringData.averageScattering[BACK];
            scatteringData.averageVariance[BACK] = dot(beta, ab) / (ab.r + ab.g + ab.b);
        }
    }

    // 3) Compute the average scattering shift
    {
        // Here we should be deriving the average shift with the per-component (R, TT, TRT)
        // BSDF average in the hemisphere, and not the BSDF average per-absorption channel.

        // Front scattering (Disney Eq. 13)
        {
            const float3 af = scatteringData.averageScattering[FRONT];
            scatteringData.averageShift[FRONT] = dot(alpha, af) / (af.r + af.g + af.b);
        }

        // Back scattering (Disney Eq. 14)
        {
            const float3 ab = scatteringData.averageScattering[BACK];
            scatteringData.averageShift[BACK] = dot(alpha, ab) / (ab.r + ab.g + ab.b);
        }
    }

    return scatteringData;
}

// void EvaluateMultipleScattering_Material(float3 V, float3 L, MultipleScatteringData lightScatteringData, inout BSDFData bsdfData)
float3 EvaluateMultipleScattering(float3 Fs, BSDFData bsdfData, float3 alpha, float3 beta, float thetaH, float sinThetaI)
{
    // Fetch the various preintegrated data.
    HairScatteringData hairScatteringData = GetHairScatteringData(bsdfData, alpha, beta, sinThetaI);

    // Solve for multiple scattering in a volume of hair fibers with concepts from:
    // "Dual Scattering Approximation for Fast Multiple Scattering in Hair" (Zinke et. al)
    // "Efficient Implementation of the Dual Scattering Model in RenderMan" (Sadeghi et. al)
    // "A BSSRDF Model for Efficient Rendering of Fur with Global Illumination" (Yan et. al)

    // Pre-define some shorthand for the symbols.
    const float  n    = bsdfData.fiberCount;
    const float3 af   = hairScatteringData.averageScattering[FRONT];
    const float3 ab   = hairScatteringData.averageScattering[BACK];
    const float3 sf   = hairScatteringData.averageShift[FRONT];
    const float3 sb   = hairScatteringData.averageShift[BACK];
    const float3 Bf   = hairScatteringData.averageVariance[FRONT];
    const float3 Bb   = hairScatteringData.averageVariance[BACK];
    const float3 Bf2  = Sq(Bf);
    const float3 Bb2  = Sq(Bb);
    const float3x3 NG = hairScatteringData.NG;

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

    // Blend the forward transmittance and variance toward their directly lit values. (Better than binary test)
    const float directFraction = 1 - saturate(n);
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
    float3 deltaB = (sb * shiftB) + (sf * shiftF);

    // Compute the average back scattering standard deviation ( Eq. 17 ).
    float3 sigmaB = (1 + db * af2);
    sigmaB *= (ab * sqrt((2 * Bf2) + Bb2)) + (ab3 * sqrt((2 * Bf2) + Bb2));
    sigmaB /= ab + (ab3 * ((2 * Bf) + (3 * Bb)));
    sigmaB  = Sq(sigmaB);

    // Computes the average back scattering spread ( Eq. 15 ).
    float3 Sb = ScatteringSpreadGaussian(thetaH - deltaB, sigmaB + sigmaF);

    // Resolve the overall local scattering term ( Eq. 9 & 10 ).
    float3 fsBack  = db * 2 * Ab * Sb;

    // Resolve the approximated multiple scattering.
    // ------------------------------------------------------------------------------------
    const float3 MG = D_LongitudinalScatteringGaussian(thetaH - alpha, beta + sigmaF);
    const float3 fsScatter = mul(MG, NG);

    const float3 Fdirect   = directFraction * (Fs + fsBack);
    const float3 Fscatter  = (Tf - directFraction) * df * (fsScatter + PI * fsBack);

    return max(Fdirect + Fscatter, 0);
}
