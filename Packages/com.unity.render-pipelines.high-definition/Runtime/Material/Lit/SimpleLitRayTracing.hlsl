float3 SampleSpecularBRDF(BSDFData bsdfData, float2 theSample, float3 viewWS)
{
    float roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    float3x3 localToWorld = GetLocalFrame(bsdfData.normalWS);
    float NdotL, NdotH, VdotH;
    float3 sampleDir;
    SampleGGXDir(theSample, viewWS, localToWorld, roughness, sampleDir, NdotL, NdotH, VdotH);
    return sampleDir;
}

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

float RecursiveRenderingReflectionPerceptualSmoothness(BSDFData bsdfData)
{
    return PerceptualRoughnessToPerceptualSmoothness(bsdfData.perceptualRoughness);
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
void FitToStandardLit( BSDFData bsdfData
                        , BuiltinData builtinData
                        , uint2 positionSS
                        , inout StandardBSDFData outStandardlit)
{
    outStandardlit.specularOcclusion = bsdfData.specularOcclusion;
    outStandardlit.normalWS = bsdfData.normalWS;
    outStandardlit.baseColor = bsdfData.diffuseColor;
    outStandardlit.fresnel0 = bsdfData.fresnel0;
    outStandardlit.perceptualRoughness = bsdfData.perceptualRoughness;
    outStandardlit.coatMask = bsdfData.coatMask;
    outStandardlit.emissiveAndBaked = builtinData.bakeDiffuseLighting * bsdfData.ambientOcclusion + builtinData.emissiveColor;
    outStandardlit.isUnlit = 0;
}
#endif
