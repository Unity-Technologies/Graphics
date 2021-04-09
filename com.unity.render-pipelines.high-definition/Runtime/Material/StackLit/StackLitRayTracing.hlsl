float3 SampleSpecularBRDF(BSDFData bsdfData, float2 theSample, float3 viewWS)
{
    float roughness;
    float3x3 localToWorld;

    if (IsVLayeredEnabled(bsdfData) && (bsdfData.coatMask > 0))
    {
        roughness = bsdfData.coatRoughness;
        localToWorld = GetLocalFrame(bsdfData.normalWS);
    }
    else
    {
        roughness = PerceptualRoughnessToRoughness(lerp(bsdfData.perceptualRoughnessA, bsdfData.perceptualRoughnessB, bsdfData.lobeMix));

        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
        {
            localToWorld = float3x3(bsdfData.tangentWS, bsdfData.bitangentWS, bsdfData.normalWS);
        }
        else
        {
            localToWorld = GetLocalFrame(bsdfData.normalWS);
        }
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
                                                    float3 reflection,
                                                    inout float reflectionHierarchyWeight,
                                                    inout LightHierarchyData lightHierarchyData)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    float3 reflectanceFactor = (float3)0.0;

    if (IsVLayeredEnabled(bsdfData) && (bsdfData.coatMask > 0))
    {
        reflectanceFactor = preLightData.specularFGD[COAT_LOBE_IDX];
        // TODOENERGY: If vlayered, should be done in ComputeAdding with FGD formulation for non dirac lights.
        // Incorrect, but for now:
        reflectanceFactor *= preLightData.energyCompensationFactor[COAT_LOBE_IDX];
        //float coatFGD = reflectanceFactor.r;
        reflectanceFactor *= preLightData.hemiSpecularOcclusion[COAT_LOBE_IDX];

        lightHierarchyData.lobeReflectionWeight[COAT_LOBE_IDX] = reflectionHierarchyWeight;
        // Instead of reflectionHierarchyWeight *= coatFGD,
        // we return min_of_all(lobeReflectionWeight) == 0, as we didn't provide any light for the bottom layer lobes:
        reflectionHierarchyWeight = 0;
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

        lightHierarchyData.lobeReflectionWeight[BASE_LOBEA_IDX] =
        lightHierarchyData.lobeReflectionWeight[BASE_LOBEB_IDX] = reflectionHierarchyWeight;
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
    // TODO: There's space for doing better here:

    // bool hasCoatNormal = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT)
    //                     && HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT_NORMAL_MAP);
    // outStandardlit.normalWS = hasCoatNormal ? surfaceData.coatNormalWS : surfaceData.normalWS;
    // Using coatnormal not necessarily better here depends on what each are vs geometric normal and the coat strength
    // vs base strength. Could do something with that and specular albedos...
    outStandardlit.normalWS = surfaceData.normalWS;

    // We set metallic to 0 with SSS and specular color mode
    float metallic = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SPECULAR_COLOR | MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;

    // StandardLit expects diffuse color in baseColor:
    outStandardlit.baseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    outStandardlit.fresnel0 = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SPECULAR_COLOR) ? surfaceData.specularColor : ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, IorToFresnel0(surfaceData.dielectricIor));
    outStandardlit.specularOcclusion = 1; // TODO

    outStandardlit.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(lerp(surfaceData.perceptualSmoothnessA, surfaceData.perceptualSmoothnessB, surfaceData.lobeMix));
    outStandardlit.coatMask = surfaceData.coatMask;
    outStandardlit.emissiveAndBaked = builtinData.bakeDiffuseLighting * surfaceData.ambientOcclusion + builtinData.emissiveColor;
    outStandardlit.isUnlit = 0;
}
#endif
