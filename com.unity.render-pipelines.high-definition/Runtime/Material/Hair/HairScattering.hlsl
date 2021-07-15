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

    return strandCountApprox;
#else
    // TODO
    return 1;
#endif
}

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

HairScatteringData EvaluateMultipleScattering(BSDFData bsdfData, float3 V, float3 L, float3 P)
{
    float3 T = bsdfData.hairStrandDirectionWS;

    // The dot product of an incident direction with the strand direction gives the sine
    // of the angle between the incident direction and the normal plane.
    float sinThetaI = dot(T, L);

    // TEMP: Extra angle derivation.
    float thetaH, cosThetaD;
    {
        float sinThetaR = dot(T, V);

        float thetaI = FastASin(sinThetaI);
        float thetaR = FastASin(sinThetaR);
        thetaH = (thetaI + thetaR) * 0.5;

        cosThetaD = cos((thetaR - thetaI) * 0.5);
    }

    // Sample the average front and back scattering.
    HairScatteringData scatteringData = SampleAverageScattering(bsdfData.diffuseColor, bsdfData.perceptualRoughness, sinThetaI);

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
        const float densityFactorFront = 0.7;

        // By assuming that all hair strands between the shading point and the light are oriented the same, we can avoid evaluating the average
        // scattering at every intersection, and instead approximate the transmittance like so. This is suitable for long, straighter hair, but
        // have a greater error for curlier hair styles.
        float3 forwardTransmittance = densityFactorFront * pow(scatteringData.averageScatteringFront, scatteringData.strandCount);

        // We make a similar approximation for computation of the accumulated variance, by assuming strands all have the same roughness.
        // Since we only evaluate front scattering for global scattering, where TT is the dominant lobe, we accumulate that roughness.
        float sigmaF = bsdfData.roughnessTT * scatteringData.strandCount;

        float forwardSpread = ScatteringSpreadGaussian(thetaH, sigmaF) * rcp(PI * cosThetaD);

        scatteringData.globalScattering = forwardTransmittance * forwardSpread;
    }

    // Local scattering.
    {
        // Similarly to front scattering, this same density coefficient is suggested for matching most path traced references.
        const float densityFactorBack = 0.7;

        // Compute the average backscattering attenuation, the attenuation in the neighborhood of x.
        // Here we only model the first and third backscattering event, as the following are negligible.

        // Ex. of a single backward scattering event. Where L is the incident light, V is the camera, (F) is a fiber cross-section
        // with a forward scattering event, and (B) is a fiber cross section with a backward scattering event.
        //
        // V <---------------
        //                  |
        //                 (F) <--- ... ---> (B)
        // L -------------->|

        float3 af1 = scatteringData.averageScatteringFront;
        float3 af2 = Sq(af1);
        float3 afI = 1 - af2;

        float3 ab1 = scatteringData.averageScatteringBack;
        float3 ab2 = Sq(ab1);
        float3 ab3 = ab2 * ab1

        // Solve eq. 11, 13, & 14, analytic solutions to the potential infinite permutations of backward scattering
        // in a volume of fibers (for one and three backward scatters).
        float3 A1 = (ab1 * af2) / afI;
        float3 A3 = (ab3 * af2) / pow(afI, 3);
        float3 AB = A1 + A3;

        // TODO: Average backscattering spread

        scatteringData.localScattering = AB;
    }

    return scatteringData;
}
