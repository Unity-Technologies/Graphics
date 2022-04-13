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
    return lighting;
}

float RecursiveRenderingReflectionPerceptualSmoothness(BSDFData bsdfData)
{
    return PerceptualRoughnessToPerceptualSmoothness(bsdfData.perceptualRoughness);
}
#endif

#if (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER)
void FitToStandardLit( BSDFData bsdfData
                        , BuiltinData builtinData
                        , uint2 positionSS
                        , out StandardBSDFData outStandardlit)
{
    outStandardlit.baseColor = bsdfData.diffuseColor;
    outStandardlit.specularOcclusion = bsdfData.specularOcclusion;
    outStandardlit.normalWS = bsdfData.normalWS;
    outStandardlit.perceptualRoughness = bsdfData.perceptualRoughness;
    outStandardlit.fresnel0 = bsdfData.fresnel0;
    outStandardlit.coatMask = 0.0;
    outStandardlit.emissiveAndBaked = builtinData.bakeDiffuseLighting * bsdfData.ambientOcclusion + builtinData.emissiveColor;
    outStandardlit.isUnlit = 0;
}
#endif
