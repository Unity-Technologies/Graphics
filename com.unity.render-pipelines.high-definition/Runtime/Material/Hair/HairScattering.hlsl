#if _USE_DENSITY_VOLUME_SCATTERING
// NOTE: Temporary package dependency. We should move all of this to somewhere in HDRP
// #include "Packages/com.unity.demoteam.hair/Runtime/HairSimData.hlsl"
// #include "Packages/com.unity.demoteam.hair/Runtime/HairSimComputeVolumeUtility.hlsl"
#endif

#define STRAND_DIAMETER_MILLIMETERS 0.02
#define STRAND_DIAMETER_METERS      (STRAND_DIAMETER_MILLIMETERS * METERS_PER_MILLIMETER)

TEXTURE3D(_PreIntegratedHairFiberScattering);

struct HairScatteringData
{
    float  strandCount;

    float3 averageScatteringFront;
    float3 averageScatteringBack;

    // 0: Front, 1: Back
    float3 averageVariance[2];
    float3 averageShift[2];

    float3 globalScattering;
    float3 localScattering;
};

float ScatteringSpreadGaussian(float x, float v)
{
    return rcp(sqrt(TWO_PI * v)) * exp(-Sq(x) / (2 * v));
}

float EvaluateStrandCount(float3 L, float3 P)
{
#if _USE_DENSITY_VOLUME_SCATTERING
    // Trace against the density field in the light ray direction.
    const float3 worldPos = GetAbsolutePositionWS(P);
    const float3 worldDir = L;

    const int numStepsWithinCell = 10;
    const int numSteps = _VolumeCells.x * numStepsWithinCell;

    VolumeTraceState trace = VolumeTraceBegin(worldPos, worldDir, 0.5, numStepsWithinCell);

    float strandCountApprox = 0;

    for (int i = 0; i != numSteps; i++)
    {
        if (VolumeTraceStep(trace))
        {
            float cellDensity = VolumeSampleScalar(_VolumeDensity, trace.uvw);

            // TODO: Strand Count
            strandCountApprox += cellDensity;
        }
    }

    return strandCountApprox * 0.1;
#else
    // TODO
    return 1;
#endif
}

// TODO: Maybe collapse all of this into one scattering data gather.
HairScatteringData SampleAverageScattering(float3 diffuseColor, float perceptualRoughness, float sinThetaI)
{
    HairScatteringData scatteringData;
    ZERO_INITIALIZE(HairScatteringData, scatteringData);

    // Prepare the sampling coordinate.
    float  X = PerceptualRoughnessToPerceptualSmoothness(perceptualRoughness);
    float  Y = abs(sinThetaI);
    float3 Z = clamp((diffuseColor), 0.01, 0.99); // Need to clamp the absorption a bit due to artifacts at these boundaries.

    // Sample the LUT for each color channel (wavelength).
    // Note that we parameterize by diffuse color, not absorption, to fit in [0, 1].
    float2 R = SAMPLE_TEXTURE3D_LOD(_PreIntegratedHairFiberScattering, s_linear_clamp_sampler, float3(X, Y, Z.r), 0).xy;
    float2 G = SAMPLE_TEXTURE3D_LOD(_PreIntegratedHairFiberScattering, s_linear_clamp_sampler, float3(X, Y, Z.g), 0).xy;
    float2 B = SAMPLE_TEXTURE3D_LOD(_PreIntegratedHairFiberScattering, s_linear_clamp_sampler, float3(X, Y, Z.b), 0).xy;

    scatteringData.averageScatteringFront = float3(R.x, G.x, B.x);
    scatteringData.averageScatteringBack  = float3(R.y, G.y, B.y);

    return scatteringData;
}

void ComputeAverageScatteringVariance(BSDFData bsdfData, inout HairScatteringData scatteringData)
{
    const float3 beta = float3(
        bsdfData.roughnessR,
        bsdfData.roughnessTT,
        bsdfData.roughnessTRT
    );

    // Here we should be deriving the average variance with the per-component (R, TT, TRT)
    // BSDF average in the hemisphere, and not the BSDF average per-absorption channel.

    // Front scattering (Disney Eq. 15)
    {
        const float3 af = scatteringData.averageScatteringFront;
        scatteringData.averageVariance[0] = dot(beta, af) / (af.r + af.g + af.b);
    }

    // Back scattering (Disney Eq. 16)
    {
        const float3 ab = scatteringData.averageScatteringBack;
        scatteringData.averageVariance[0] = dot(beta, ab) / (ab.r + ab.g + ab.b);
    }
}

void ComputeAverageScatteringShift(BSDFData bsdfData, inout HairScatteringData scatteringData)
{
    const float3 shift = float3(
        bsdfData.cuticleAngleR,
        bsdfData.cuticleAngleTT,
        bsdfData.cuticleAngleTRT
    );

    // Here we should be deriving the average shift with the per-component (R, TT, TRT)
    // BSDF average in the hemisphere, and not the BSDF average per-absorption channel.

    // Front scattering (Disney Eq. 13)
    {
        const float3 af = scatteringData.averageScatteringFront;
        scatteringData.averageShift[0] = dot(shift, af) / (af.r + af.g + af.b);
    }

    // Back scattering (Disney Eq. 14)
    {
        const float3 ab = scatteringData.averageScatteringBack;
        scatteringData.averageShift[1] = dot(shift, ab) / (ab.r + ab.g + ab.b);
    }
}

HairScatteringData EvaluateMultipleScattering(BSDFData bsdfData, float3 V, float3 L, float3 P)
{
    float3 T = bsdfData.hairStrandDirectionWS;

    // The dot product of an incident direction with the strand direction gives the sine
    // of the angle between the incident direction and the normal plane.
    float sinThetaI = dot(T, L);

    // TEMP: Extra angle derivation, this is redundant work as we already do this for the same light on the BSDF evaluation.
    float thetaH, thetaD;
    {
        float sinThetaR = dot(T, V);

        float thetaI = FastASin(sinThetaI);
        float thetaR = FastASin(sinThetaR);

        thetaH = (thetaI + thetaR) * 0.5;
        thetaD = (thetaR - thetaI) * 0.5;
    }

    // Sample the average front and back scattering.
    // TODO: Gather everything at once.
    HairScatteringData scatteringData = SampleAverageScattering(bsdfData.diffuseColor, bsdfData.perceptualRoughness, sinThetaI);

    ComputeAverageScatteringVariance(bsdfData, scatteringData);
    ComputeAverageScatteringShift   (bsdfData, scatteringData);

    // Fetch the number of hair fibers between the shading point x and the light.
    scatteringData.strandCount = EvaluateStrandCount(L, P);

    // Solve for multiple scattering in a volume of hair fibers with concepts from:
    // "Dual Scattering Approximation for Fast Multiple Scattering in Hair" (Zinke et. al)
    // "Efficient Implementation of the Dual Scattering Model in RenderMan" (Sadeghi et. al)
    // "A BSSRDF Model for Efficient Rendering of Fur with Global Illumination" (Yan et. al)

    // Global scattering.
    {
        // Following the observation of Zinke et. al., density factor (ratio of occlusion of the shading point by neighboring strands)
        // can be approximated with this constant to match most path traced references.
        const float df = 0.7;

        // Pre-define some shorthand for the symbols. .
        const float  n  = scatteringData.strandCount;
        const float3 Bf = scatteringData.averageVariance[0];

        // Approximate the transmittance by assuming that all hair strands between the shading point and the light are
        // oriented the same. This is suitable for long, straighter hair ( Eq. 6 Disney ).
        float3 Tf = df * pow(scatteringData.averageScatteringFront, n);

        // Approximate the accumulated variance, by assuming strands all have the same average roughness. ( Eq. 7 Disney )
        float3 sigmaF = Sq(Bf) * n;

        // Computes the forward scattering spread ( Eq. 7 ).
        float3 Sf = float3( ScatteringSpreadGaussian(thetaH, sigmaF.r),
                            ScatteringSpreadGaussian(thetaH, sigmaF.g),
                            ScatteringSpreadGaussian(thetaH, sigmaF.b)) * rcp(PI * cos(thetaD));

        // Resolve the final global scattering term ( Eq. 4 ).
        scatteringData.globalScattering = Tf * Sf;
    }

    // Local scattering.
    {
        // Similarly to front scattering, this same density coefficient is suggested for matching most path traced references.
        const float db = 0.7;

        // Pre-define some shorthand for the symbols.
        const float3 af  = scatteringData.averageScatteringFront;
        const float3 ab  = scatteringData.averageScatteringBack;
        const float3 sf  = scatteringData.averageShift[0];
        const float3 sb  = scatteringData.averageShift[1];
        const float3 Bf  = scatteringData.averageVariance[0];
        const float3 Bb  = scatteringData.averageVariance[1];
        const float3 Bf2 = Sq(Bf);
        const float3 Bb2 = Sq(Bb);

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
        float3 A3 = (ab3 * af2) / afI2;
        float3 Ab = A1 + A3;

        // Computes the average longitudinal shift ( Eq. 16 ).
        float3 shiftB = 1 - (2 * ab2 / afI2);
        float3 shiftF = ((2 * afI2) + (4 * af2 * ab2)) / afI3;
        float3 deltaB = (sb * shiftB) + (sf * shiftF);

        // Compute the average back scattering standard deviation ( Eq. 17 ).
        float3 sigmaB = (1 + db * af2);
        sigmaB *= (ab * sqrt((2 * Bf2) + Bb2)) + (ab3 * sqrt((2 * Bf2) + Bb2));
        sigmaB /= ab + (ab3 * ((2 * Bf) + (3 * Bb)));
        sigmaB  = Sq(sigmaB);

        // Computes the average back scattering spread ( Eq. 15 ).
        float3 Sb = float3( ScatteringSpreadGaussian(thetaH - deltaB, sigmaB.r),
                            ScatteringSpreadGaussian(thetaH - deltaB, sigmaB.g),
                            ScatteringSpreadGaussian(thetaH - deltaB, sigmaB.b)) * rcp(PI * cos(thetaD));

        // Resolve the overall local scattering term ( Eq. 9 & 10 ).
        scatteringData.localScattering = db * (2 * Ab * Sb) / cos(thetaD);
    }

    return scatteringData;
}
