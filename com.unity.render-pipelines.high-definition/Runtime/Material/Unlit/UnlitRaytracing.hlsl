#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"

#if (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER)
void FitToStandardLit( BSDFData bsdfData
                        , BuiltinData builtinData
                        , uint2 positionSS
                        , out StandardBSDFData outStandardlit )
{

    ZERO_INITIALIZE(StandardBSDFData, outStandardlit);

    // Output is not to be lit
    // The inverse current exposure multiplier needs to only be applied to the color as it need to be brought to the current exposure value, the emissive
    // color is already in the right exposure space.
    outStandardlit.emissiveAndBaked = (bsdfData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor);
    outStandardlit.isUnlit = 1;

    // Be cause this will not be lit, we need to apply atmospheric scattering right away
    ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), RayTCurrent(), outStandardlit.emissiveAndBaked, true);
}
#endif
