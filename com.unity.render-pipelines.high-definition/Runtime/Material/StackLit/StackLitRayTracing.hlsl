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
void FitToStandardLit( BSDFData bsdfData
                        , BuiltinData builtinData
                        , uint2 positionSS
                        , out StandardBSDFData outStandardlit)
{
    // TODO: There's space for doing better here:

    // bool hasCoatNormal = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT)
    //                     && HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT_NORMAL_MAP);
    // outStandardlit.normalWS = hasCoatNormal ? surfaceData.coatNormalWS : surfaceData.normalWS;
    // Using coatnormal not necessarily better here depends on what each are vs geometric normal and the coat strength
    // vs base strength. Could do something with that and specular albedos...
    outStandardlit.normalWS = bsdfData.normalWS;

    // StandardLit expects diffuse color in baseColor:
    outStandardlit.baseColor = bsdfData.diffuseColor;
    outStandardlit.fresnel0 = bsdfData.fresnel0;
    outStandardlit.specularOcclusion = 1; // TODO

    // We didn't run GetPreLightData, we cheaply cap base roughness up to coat roughness at least:
    outStandardlit.perceptualRoughness = max(bsdfData.coatPerceptualRoughness, lerp(bsdfData.perceptualRoughnessA, bsdfData.perceptualRoughnessB, bsdfData.lobeMix));
    // We make the coat mask go to 0 as the stacklit coat gets rougher (works ok and better than just feeding coatmask directly)
    outStandardlit.coatMask = lerp(bsdfData.coatMask, 0, saturate((bsdfData.coatPerceptualRoughness - CLEAR_COAT_PERCEPTUAL_ROUGHNESS)/0.2) );
    outStandardlit.emissiveAndBaked = builtinData.bakeDiffuseLighting * bsdfData.ambientOcclusion + builtinData.emissiveColor;
    outStandardlit.isUnlit = 0;
}
#endif
