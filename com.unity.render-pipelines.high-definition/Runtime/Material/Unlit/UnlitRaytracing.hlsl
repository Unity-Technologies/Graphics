
#if (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER)
void FitToStandardLit( SurfaceData surfaceData
                        , BuiltinData builtinData
                        , uint2 positionSS
                        , out StandardBSDFData outStandardlit )
{

    ZERO_INITIALIZE(StandardBSDFData, outStandardlit);
    outStandardlit.emissiveAndBaked = surfaceData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor;
    outStandardlit.isUnlit = 1;
}
#endif
