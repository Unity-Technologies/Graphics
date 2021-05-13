#if _USE_DENSITY_VOLUME_SCATTERING
// NOTE: Temporary package dependency. We should move all of this to somewhere in HDRP
// #include "Packages/com.unity.demoteam.hair/Runtime/HairSimData.hlsl"
// #include "Packages/com.unity.demoteam.hair/Runtime/HairSimComputeVolumeUtility.hlsl"
#endif

float EvaluateStrandCount(float3 L, float3 P)
{
#if _USE_DENSITY_VOLUME_SCATTERING
    // Trace against the density field in the light ray direction.
//    const float3 worldPos = GetAbsolutePositionWS(P);
//    const float3 worldDir = L;
//
//    const int numStepsWithinCell = 10;
//    const int numSteps = _VolumeCells.x * numStepsWithinCell;
//
//    VolumeTraceState trace = VolumeTraceBegin(worldPos, worldDir, 0.5, numStepsWithinCell);
//
//    float density = 0;
//
//    for (int i = 0; i != numSteps; i++)
//    {
//        if (VolumeTraceStep(trace))
//        {
//            density += VolumeSampleScalar(_VolumeDensity, trace.uvw);
//        }
//    }
//
//    return saturate(density);
#else
    // TODO
    return 1;
#endif
}
