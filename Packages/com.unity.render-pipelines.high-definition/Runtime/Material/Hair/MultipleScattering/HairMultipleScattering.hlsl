TEXTURE3D(_HairAttenuation);

#define FRONT 0
#define BACK  1

struct HairScatteringData
{
    half3 averageScattering [2];
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
    return bsdfData.visibility > -1 ? bsdfData.visibility : 1 - saturate(strandCount);
}

float3 ComputeDualScattering(BSDFData bsdfData, HairAngle angles, int strandCount, inout float3 Fs)
{
    // Fetch pre-computed data
    // ---------------------------------------------------------

    HairScatteringData scatteringData;
    ZERO_INITIALIZE(HairScatteringData, scatteringData);
    {
        // Prepare the sampling coordinate.
        // (Note, currently clamping the smoothness due to noise in the LUT, this can be fixed by preintegrating with importance sampling).
        const half  X = min(PerceptualRoughnessToPerceptualSmoothness(bsdfData.perceptualRoughness), 0.6);
        const half  Y = abs(angles.sinThetaI);
        const half3 Z = bsdfData.diffuseColor;

        // Sample the LUT for each wavelength.
        // Note that we parameterize by diffuse color, not absorption, to fit in [0, 1].
        // It might be possible to fully support azimuthal roughness by separating the integral and using extra 2D lut.
        // However the effect of azimuthal is subtle for the scattering term, mostly producing a much more saturated result for low absorptions.
        // Because of this, it might be much simpler and easier to approximate the a. roughness by modulating the attenuation below.
        const half2 R = SAMPLE_TEXTURE3D_LOD(_HairAttenuation, s_linear_clamp_sampler, float3(X, Y, Z.r), 0).xy;
        const half2 G = SAMPLE_TEXTURE3D_LOD(_HairAttenuation, s_linear_clamp_sampler, float3(X, Y, Z.g), 0).xy;
        const half2 B = SAMPLE_TEXTURE3D_LOD(_HairAttenuation, s_linear_clamp_sampler, float3(X, Y, Z.b), 0).xy;

        scatteringData.averageScattering[FRONT] = float3(R.x, G.x, B.x);
        scatteringData.averageScattering[BACK]  = float3(R.y, G.y, B.y);
    }

    const half3 alpha = half3(
        bsdfData.cuticleAngleR,
        bsdfData.cuticleAngleTT,
        bsdfData.cuticleAngleTRT
    );

    const half3 beta = sqrt(half3(
        bsdfData.roughnessR,
        bsdfData.roughnessTT,
        bsdfData.roughnessTRT
    ));

    // Declare shorthand symbols / variables from the paper
    // ---------------------------------------------------------
    const half  n    = max(strandCount - 1, 0);
    const half3 af   = min(scatteringData.averageScattering[FRONT], 0.99); // Need to clamp in case of NaNs.
    const half3 ab   = min(scatteringData.averageScattering[BACK],  0.99); // Need to clamp in case of NaNs.

    const half3 fw  = af / (af.r + af.g + af.b);
    const half3 bw  = ab / (ab.r + ab.g + ab.b);
    const half3 sf  = dot(alpha, fw);
    const half3 sb  = dot(alpha, bw);
    const half3 Bf  = dot(beta,  fw);
    const half3 Bb  = dot(beta,  bw);
    const half3 Bf2 = Sq(Bf);
    const half3 Bb2 = Sq(Bb);

    // Compute global scattering
    // ---------------------------------------------------------
#if _MATERIAL_FEATURE_HAIR_MARSCHNER_CINEMATIC
    // Predicate term for switching the model between direct / scatter evaluation.
    const half directFraction = GetDirectFraction(bsdfData, n);

    // Following the observation of Zinke et. al., density factor (ratio of occlusion of the shading point by neighboring strands)
    // can be approximated with this constant to match most path traced references.
    const half df = 0.7;

    // Approximate the transmittance by assuming that all hair strands between the shading point and the light are
    // oriented the same. This is suitable for long, straighter hair ( Eq. 6 Disney ).
    half3 Tf = df * pow(max(af, 0), n);

    // Approximate the accumulated variance, by assuming strands all have the same average roughness and inclination. ( Eq. 7 Disney )
    half3 sigmaF = Bf2 * max(1, n);
#else
    half3 sigmaF = Bf2;
#endif

    // Compute local scattering
    // ---------------------------------------------------------

    // Similarly to front scattering, this same density coefficient is suggested for matching most path traced references.
    const half db = 0.7;

    // Compute the average backscattering attenuation, the attenuation in the neighborhood of x.
    // Here we only model the first and third backscattering event, as the following are negligible.

    // Ex. of a single backward scattering event. Where L is the incident light, V is the camera, (F) is a fiber cross-section
    // with a forward scattering event, and (B) is a fiber cross section with a backward scattering event.
    //
    // V <---------------
    //                  |
    //                 (F) <--- ... ---> (B)
    // L -------------->|

    half3 af1 = af;
    half3 af2 = Sq(af1);

    half3 afI1 = 1 - af2;
    half3 afI2 = Sq(afI1);
    half3 afI3 = afI1 * afI1 * afI1;

    half3 ab1 = ab;
    half3 ab2 = Sq(ab1);
    half3 ab3 = ab2 * ab1;

    // Analytic solutions to the potential infinite permutations of backward scattering
    // in a volume of fibers for one and three backward scatters ( Eq. 11, 13, & 14 ).
    half3 A1 = ab1 * af2 / afI1;
    half3 A3 = ab3 * af2 / afI3;
    half3 Ab = A1 + A3;

    // Computes the average longitudinal shift ( Eq. 16  ).
    half3 shiftB = 1 - ((2 * ab2) / afI2);
    half3 shiftF = ((2 * afI2) + (4 * af2 * ab2)) / afI3;
    half3 deltaB = (sb * shiftB) + (sf * shiftF);

    // Compute the average back scattering standard deviation ( Eq. 17 ).
    half3 sigmaB = (1 + db * af2);
    sigmaB *= (ab * sqrt((2 * Bf2) + Bb2)) + (ab3 * sqrt((2 * Bf2) + (3 * Bb2)));
    sigmaB *= rcp(ab + (ab3 * ((2 * Bf)) + (3 * Bb)));
    sigmaB  = Sq(sigmaB);

    half3 psiL = 2 * Ab * db * Gaussian(angles.thetaH - deltaB, sigmaB + sigmaF) * INV_PI * rcp(Sq(angles.cosThetaD));

#if _MATERIAL_FEATURE_HAIR_MARSCHNER_CINEMATIC
    const half3 Fdirect  = directFraction * (Fs + psiL);
    const half3 Fscatter = (Tf - directFraction) * df * (Fs + PI * psiL);
#else
    const half3 Fdirect  = Fs + psiL;
    const half3 Fscatter = 0; // No global scattering contribution.
#endif

    // Ref: Section 3.3 & 4 of Zinke et. al.
    return angles.cosThetaI * (saturate(Fdirect) + saturate(Fscatter));
}
