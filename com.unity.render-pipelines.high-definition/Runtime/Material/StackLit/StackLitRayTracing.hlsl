float3 SampleSpecularBRDF(BSDFData bsdfData, float2 theSample, float3 viewWS)
{
    float roughness = bsdfData.roughnessAT;
    float3x3 localToWorld;
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
    {
        localToWorld = float3x3(bsdfData.tangentWS, bsdfData.bitangentWS, bsdfData.normalWS);
    }
    else
    {
        localToWorld = GetLocalFrame(bsdfData.normalWS);
    }
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

    float3 reflectanceFactor = (float3)0.0;

    if (IsVLayeredEnabled(bsdfData))
    {
        reflectanceFactor = preLightData.specularFGD[COAT_LOBE_IDX];
        reflectanceFactor *= preLightData.hemiSpecularOcclusion[COAT_LOBE_IDX];
        // TODOENERGY: If vlayered, should be done in ComputeAdding with FGD formulation for non dirac lights.
        // Incorrect, but for now:
        reflectanceFactor *= preLightData.energyCompensationFactor[COAT_LOBE_IDX];
    }
    else
    {
        for(int i = 0; i < TOTAL_NB_LOBES; i++)
        {
            float3 lobeFactor = preLightData.specularFGD[i]; // note: includes the lobeMix factor, see PreLightData.
            lobeFactor *= preLightData.hemiSpecularOcclusion[i];
            // TODOENERGY: If vlayered, should be done in ComputeAdding with FGD formulation for non dirac lights.
            // Incorrect, but for now:
            lobeFactor *= preLightData.energyCompensationFactor[i];
            reflectanceFactor += lobeFactor;
        }
    }

    lighting.specularReflected = reflection.rgb * reflectanceFactor;
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
    return PerceptualRoughnessToPerceptualSmoothness(bsdfData.perceptualRoughnessB);
}
#endif

#if (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER)
void FitToStandardLit( SurfaceData surfaceData
                        , BuiltinData builtinData
                        , uint2 positionSS
                        , out StandardBSDFData outStandardlit)
{
    outStandardlit.baseColor = surfaceData.baseColor;
    outStandardlit.specularOcclusion = surfaceData.specularOcclusionCustomInput;
    outStandardlit.normalWS = surfaceData.normalWS;
    outStandardlit.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothnessA);
    outStandardlit.fresnel0 = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SPECULAR_COLOR) ? surfaceData.specularColor : ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, IorToFresnel0(surfaceData.dielectricIor));
    outStandardlit.coatMask = surfaceData.coatMask;
    outStandardlit.emissiveAndBaked = builtinData.bakeDiffuseLighting * surfaceData.ambientOcclusion + builtinData.emissiveColor;
    outStandardlit.isUnlit = 0;
}
#endif
