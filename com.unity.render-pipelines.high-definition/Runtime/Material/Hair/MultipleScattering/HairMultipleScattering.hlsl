TEXTURE3D(_PreIntegratedHairFiberScattering);

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

    return max(dot(strandCountProbe, Ylm), 0);
}

float GetDirectFraction(BSDFData bsdfData, float strandCount)
{
    // Defer to the higher quality spline visibility for this light, if any.
    // Otherwise fall back to the coarse approximation from the spherical harmonic.
    return bsdfData.splineVisibility > -1 ? bsdfData.splineVisibility : 1 - saturate(strandCount);
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
        // (Note, currently clamping the smoothness due to noise in the LUT, this can be fixed by preintegrating with importance sampling).
        float  X = clamp(PerceptualRoughnessToPerceptualSmoothness(bsdfData.perceptualRoughness), 0.0, 0.6);
        float  Y = abs(sinThetaI);
        float3 Z = bsdfData.diffuseColor;

        // Sample the LUT for each wavelength.
        // Note that we parameterize by diffuse color, not absorption, to fit in [0, 1].
        // It might be possible to fully support azimuthal roughness by separating the integral and using extra 2D lut.
        // However the effect of azimuthal is subtle for the scattering term, mostly producing a much more saturated result for low absorptions.
        // Because of this, it might be much simpler and easier to approximate the a. roughness by modulating the attenuation below.
        float2 R = SAMPLE_TEXTURE3D_LOD(_PreIntegratedHairFiberScattering, s_linear_clamp_sampler, float3(X, Y, Z.r), 0).xy;
        float2 G = SAMPLE_TEXTURE3D_LOD(_PreIntegratedHairFiberScattering, s_linear_clamp_sampler, float3(X, Y, Z.g), 0).xy;
        float2 B = SAMPLE_TEXTURE3D_LOD(_PreIntegratedHairFiberScattering, s_linear_clamp_sampler, float3(X, Y, Z.b), 0).xy;

        scatteringData.averageScattering[FRONT] = float3(R.x, G.x, B.x);
        scatteringData.averageScattering[BACK]  = float3(R.y, G.y, B.y);
    }

    // 2) Compute the average scattering variance & shift
    {
        // Note: Disney suggest this weighted average but in testing against the reference it seems like the TT terms for
        // the forward lobe and the TRT terms for the backward lobe are sufficient.
        scatteringData.averageVariance[FRONT] = beta.y; // dot(af, beta) / af;
        scatteringData.averageVariance[BACK]  = beta.z; // dot(ab, beta) / ab;

        scatteringData.averageShift[FRONT] = alpha.y; // dot(af, alpha) / af;
        scatteringData.averageShift[BACK]  = alpha.z; // dot(ab, alpha) / ab;
    }

    return scatteringData;
}

float3 EvaluateMultipleScattering(float3 L, float3 Fs, BSDFData bsdfData, float3 alpha, float3 beta, float thetaH, float sinThetaI, float3 D, float3 A[3])
{
    // Fetch the various preintegrated data.
    HairScatteringData hairScatteringData = GetHairScatteringData(bsdfData, alpha, beta, sinThetaI);

    // Solve for multiple scattering in a volume of hair fibers with concepts from:
    // "Dual Scattering Approximation for Fast Multiple Scattering in Hair" (Zinke et. al)
    // "Efficient Implementation of the Dual Scattering Model in RenderMan" (Sadeghi et. al)
    // "A BSSRDF Model for Efficient Rendering of Fur with Global Illumination" (Yan et. al)

    // Pre-define some shorthand for the symbols.
    const float  n    = DecodeHairStrandCount(L, bsdfData.strandCountProbe);
    const float3 af   = hairScatteringData.averageScattering[FRONT];
    const float3 ab   = hairScatteringData.averageScattering[BACK];
    const float3 sf   = hairScatteringData.averageShift[FRONT];
    const float3 sb   = hairScatteringData.averageShift[BACK];
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
    float3 deltaB = (sb * shiftB) + (sf * shiftF);

    // Compute the average back scattering standard deviation ( Eq. 17 ).
    float3 sigmaB = (1 + db * af2);
    sigmaB *= (ab * sqrt((2 * Bf2) + Bb2)) + (ab3 * sqrt((2 * Bf2) + (3 * Bb2)));
    sigmaB /= ab + (ab3 * ((2 * Bf) + (3 * Bb)));
    sigmaB  = Sq(sigmaB);

    // Resolve the overall local scattering term ( Eq. 19 & 20 Disney ).
    // Note, for now remove the square, there seems to be a discrepancy in the gaussian used in the paper (ours takes std. dev while paper uses variance)
    // Additionally, we should be dividing by PI here, but doing this causes another discrepancy with the multiple scattering that is observed in the
    // the path traced reference. It is possible again, that this is due to the gaussian distribution that we use.
    float3 fsBack = db * 2 * Ab * D_LongitudinalScatteringGaussian(thetaH - deltaB, sqrt(sigmaB + sigmaF));

    // Resolve the approximated multiple scattering. (Approximate Eq. 22)
    // ------------------------------------------------------------------------------------
    const float3 MG = D_LongitudinalScatteringGaussian(thetaH - alpha, beta + sqrt(sigmaF));

    // Reuse the azimuthal component, it seems sufficient instead of a whole extra LUT for the average forward scattering on the hemisphere.
    // TODO: This computation is redundant, already done in direct BSDF!
    const float3x3 NG = float3x3( D.x * A[0],
                                  D.y * A[1],
                                  D.z * A[2] );

    const float3 fsScatter = mul(MG, NG);

    const float3 Fdirect   = directFraction * (Fs + fsBack);
    const float3 Fscatter  = (Tf - directFraction) * df * (fsScatter + PI * fsBack);
    const float3 F         = (Fdirect + Fscatter) * sqrt(1 - Sq(sinThetaI));

    return max(F, 0);
}
