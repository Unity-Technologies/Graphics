#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"

#if (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER)
void FitToStandardLit( SurfaceData surfaceData
                        , BuiltinData builtinData
                        , uint2 positionSS
                        , out StandardBSDFData outStandardlit )
{

    ZERO_INITIALIZE(StandardBSDFData, outStandardlit);

    // Output is not to be lit
    outStandardlit.emissiveAndBaked = surfaceData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor;
    outStandardlit.isUnlit = 1;

    // Be cause this will not be lit, we need to apply atmospheric scattering right away
    ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), RayTCurrent(), outStandardlit.emissiveAndBaked, true);
}
#endif
