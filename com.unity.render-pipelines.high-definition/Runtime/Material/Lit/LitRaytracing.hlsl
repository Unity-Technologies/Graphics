#ifdef HAS_LIGHTLOOP
IndirectLighting EvaluateBSDF_RaytracedRefraction(LightLoopContext lightLoopContext,
                                                    PreLightData preLightData,
                                                    float3 transmittedColor)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

#if HAS_REFRACTION
    float3 preLD = transmittedColor;

    // We use specularFGD as an approximation of the fresnel effect (that also handle smoothness)
    float3 F = preLightData.specularFGD;
    lighting.specularTransmitted = (1.0 - F) * preLD.rgb * preLightData.transparentTransmittance;
#endif

    return lighting;
}
#endif

#if (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER)
void FitToStandardLit( SurfaceData surfaceData
                        , BuiltinData builtinData
                        , uint2 positionSS
                        , out StandardBSDFData outStandardlit)
{    
    outStandardlit.baseColor = surfaceData.baseColor;
    outStandardlit.specularOcclusion = surfaceData.specularOcclusion;
    outStandardlit.normalWS = surfaceData.normalWS;
    outStandardlit.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    outStandardlit.fresnel0 = surfaceData.specularColor;
    outStandardlit.coatMask = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT) ? surfaceData.coatMask : 0.0;
    outStandardlit.emissiveAndBaked = builtinData.bakeDiffuseLighting * surfaceData.ambientOcclusion + builtinData.emissiveColor;
#ifdef LIGHT_LAYERS
    outStandardlit.renderingLayers = builtinData.renderingLayers;
#endif
#ifdef SHADOWS_SHADOWMASK
    outStandardlit.shadowMasks = BUILTIN_DATA_SHADOW_MASK;
#endif
    outStandardlit.isUnlit = 0;
}
#endif