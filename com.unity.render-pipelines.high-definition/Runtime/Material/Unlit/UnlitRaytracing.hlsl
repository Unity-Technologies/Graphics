#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"

#if (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER)
void FitToStandardLit( SurfaceData surfaceData
                        , BuiltinData builtinData
                        , uint2 positionSS
                        , out StandardBSDFData outStandardlit )
{

    ZERO_INITIALIZE(StandardBSDFData, outStandardlit);
    
    // Output is not to be lit
    //Note: we have to multiply everything with the inverse exposure, since the result buffer expects everything to be 'pre exposed'.
    //Is important to know too that we are applying InverseCurrentExposure twice (since this is just for reflections). Once when generating the material emissive value,
    //and once more for render target storage. 
    outStandardlit.emissiveAndBaked = (surfaceData.color + builtinData.emissiveColor) * GetInverseCurrentExposureMultiplier();
    outStandardlit.isUnlit = 1;

    // Be cause this will not be lit, we need to apply atmospheric scattering right away
    ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), RayTCurrent(), outStandardlit.emissiveAndBaked, true);
}
#endif
