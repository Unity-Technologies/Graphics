// TODO: It is currently tricky to move this tracing util from the package as we then have a conflicting/redefinition
// issue from the "HairVert" custom node. The solution may need to be a secondary cbuffer for general binding.
// #include "Packages/com.unity.demoteam.hair/Runtime/HairSimData.hlsl"
// #include "Packages/com.unity.demoteam.hair/Runtime/HairSimComputeVolumeUtility.hlsl"

float3 VolumeUVWToWorld(float3 uvw)
{
    float3 positionWS = (uvw * (_VolumeWorldMax - _VolumeWorldMin)) + _VolumeWorldMin;
    return positionWS;
}

// Currently the only routine we support for gathering multiple scattering information is by tracing a density volume.
#define MULTIPLE_SCATTERING_DENSITY_VOLUME 1

// Inform LightLoopDefs.hlsl that we have a multiple scattering struct define.
#define HAVE_MULTIPLE_SCATTERING_DATA

struct MultipleScatteringData
{
    float fiberCount;

#if MULTIPLE_SCATTERING_DENSITY_VOLUME
    float3 shadowProxyPositionRWS;
#endif
};

MultipleScatteringData EvaluateMultipleScattering_Light(PositionInputs posInputs, float3 L)
{
    MultipleScatteringData data;
    ZERO_INITIALIZE(MultipleScatteringData, data);

#if MULTIPLE_SCATTERING_DENSITY_VOLUME
    // Trace against the density field in the shadow ray direction.
    const float3 positionWS  = GetAbsolutePositionWS(posInputs.positionWS);
    const float3 directionWS = L;

    const int numStepsWithinCell = 10;
    const int numSteps = _VolumeCells.x * numStepsWithinCell;

    VolumeTraceState trace = VolumeTraceBegin(positionWS, directionWS, 0.5, numStepsWithinCell);

    // Track the outermost edge coordinate.
    float3 shadowProxyCoord = trace.uvw;

    for (int i = 0; i != numSteps; i++)
    {
        if (VolumeTraceStep(trace))
        {
            float cellDensitySampled = VolumeSampleScalar(_VolumeDensity, trace.uvw);

            // TODO: Better strand count approximation.
            data.fiberCount += cellDensitySampled;

            if (any(cellDensitySampled))
            {
                // While tracing, track the outermost coordinate in the shadow ray direction.
                // This will be used to override the shadow test to handle self-occlusion between strands.
                shadowProxyCoord = trace.uvw;
            }
        }
    }

    // Transform the outermost coordinate into terms of camera relative world space.
    float3 shadowProxyPositionWS = VolumeUVWToWorld(shadowProxyCoord);
    data.shadowProxyPositionRWS = GetCameraRelativePositionWS(shadowProxyPositionWS);
#else
    // TODO: Optimized routine (ie Deep Opacity Maps).
#endif

    return data;
}

void EvaluateMultipleScattering_ShadowProxy(MultipleScatteringData scatteringData, inout float3 positionWS)
{
#if MULTIPLE_SCATTERING_DENSITY_VOLUME
    // Modify the shadow sample position to be the edge of the density volume toward the light.
    positionWS = scatteringData.shadowProxyPositionRWS;
#endif
}
