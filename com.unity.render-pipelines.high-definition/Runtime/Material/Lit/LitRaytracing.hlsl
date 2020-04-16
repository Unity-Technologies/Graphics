#ifdef HAS_LIGHTLOOP
IndirectLighting EvaluateBSDF_RaytracedReflection(LightLoopContext lightLoopContext,
                                                    BSDFData bsdfData,
                                                    PreLightData preLightData,
                                                    float3 reflection)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);
    lighting.specularReflected = reflection.rgb * preLightData.specularFGD;
    return lighting;
}

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
#if HAS_REFRACTION
void OverrideRefractionData(SurfaceData surfaceData, float refractionDistance, float3 refractionPositionWS, inout BSDFData bsdfData, inout PreLightData preLightData)
{
    // This variable is only used for SSRefraction, we intentionally put an invalid value in it.
    bsdfData.absorptionCoefficient = TransmittanceColorAtDistanceToAbsorption(surfaceData.transmittanceColor, refractionDistance);
    preLightData.transparentRefractV = 0.0;
    preLightData.transparentPositionWS = refractionPositionWS;
    preLightData.transparentTransmittance = exp(-bsdfData.absorptionCoefficient * refractionDistance);
}
#endif

#endif

#if (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER)
void FitToStandardLit( SurfaceData surfaceData
                        , BuiltinData builtinData
                        , uint2 positionSS
                        , out StandardBSDFData outStandardlit)
{
    outStandardlit.specularOcclusion = surfaceData.specularOcclusion;
    outStandardlit.normalWS = surfaceData.normalWS;

    float metallic = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;

    outStandardlit.baseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    outStandardlit.fresnel0     = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR) ? surfaceData.specularColor : ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, DEFAULT_SPECULAR_VALUE);

    outStandardlit.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
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
