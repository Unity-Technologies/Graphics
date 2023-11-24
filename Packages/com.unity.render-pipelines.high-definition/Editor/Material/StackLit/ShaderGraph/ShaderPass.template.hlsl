void ApplyDecalToSurfaceDataNoNormal(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
    surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;

    // TODOTODO: _MATERIAL_FEATURE_SPECULAR_COLOR and _MATERIAL_FEATURE_HAZY_GLOSS

#ifdef DECALS_4RT // only smoothness in 3RT mode
    surfaceData.metallic = surfaceData.metallic * decalSurfaceData.MAOSBlend.x + decalSurfaceData.mask.x;
    surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.MAOSBlend.y + decalSurfaceData.mask.y;
#endif

    surfaceData.perceptualSmoothnessA = surfaceData.perceptualSmoothnessA * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
    surfaceData.perceptualSmoothnessB = surfaceData.perceptualSmoothnessB * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
    surfaceData.coatPerceptualSmoothness = surfaceData.coatPerceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
}


void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData, out float3 bentNormalWS)
{
    // setup defaults -- these are used if the graph doesn't output a value
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    $CoatMaskOne: surfaceData.coatMask = 1.0;
    $UseProfileIor: surfaceData.useProfileIor = true;

    // Copy graph values to surfaceData, if defined
    $SurfaceDescription.BaseColor:                 surfaceData.baseColor =                surfaceDescription.BaseColor;
    $SurfaceDescription.SubsurfaceMask:            surfaceData.subsurfaceMask =           surfaceDescription.SubsurfaceMask;
    $SurfaceDescription.TransmissionTint:          surfaceData.transmissionMask =         surfaceDescription.TransmissionTint;
    $SurfaceDescription.Thickness:                 surfaceData.thickness =                surfaceDescription.Thickness;
    $SurfaceDescription.DiffusionProfileHash:      surfaceData.diffusionProfileHash =     asuint(surfaceDescription.DiffusionProfileHash);
    $SurfaceDescription.IridescenceMask:           surfaceData.iridescenceMask =          surfaceDescription.IridescenceMask;
    $SurfaceDescription.IridescenceThickness:      surfaceData.iridescenceThickness =     surfaceDescription.IridescenceThickness;
    $SurfaceDescription.IridescenceCoatFixupTIR:   surfaceData.iridescenceCoatFixupTIR =  surfaceDescription.IridescenceCoatFixupTIR;
    $SurfaceDescription.IridescenceCoatFixupTIRClamp: surfaceData.iridescenceCoatFixupTIRClamp =  surfaceDescription.IridescenceCoatFixupTIRClamp;
    $SurfaceDescription.Specular:                  surfaceData.specularColor =            surfaceDescription.Specular;
    $SurfaceDescription.DielectricIor:             surfaceData.dielectricIor =            surfaceDescription.DielectricIor;
    $SurfaceDescription.Metallic:                  surfaceData.metallic =                 surfaceDescription.Metallic;
    $SurfaceDescription.Smoothness:                surfaceData.perceptualSmoothnessA =    surfaceDescription.Smoothness;
    $SurfaceDescription.SmoothnessB:               surfaceData.perceptualSmoothnessB =    surfaceDescription.SmoothnessB;
    $SurfaceDescription.Occlusion:                 surfaceData.ambientOcclusion =         surfaceDescription.Occlusion;
    //TODO: if custom external values are wanted, we would ideally need one SO value per lobe.
    $SurfaceDescription.SpecularOcclusion:         surfaceData.specularOcclusionCustomInput = surfaceDescription.SpecularOcclusion;
    $SurfaceDescription.SOFixupVisibilityRatioThreshold: surfaceData.soFixupVisibilityRatioThreshold = surfaceDescription.SOFixupVisibilityRatioThreshold;
    $SurfaceDescription.SOFixupStrengthFactor:            surfaceData.soFixupStrengthFactor =           surfaceDescription.SOFixupStrengthFactor;
    $SurfaceDescription.SOFixupMaxAddedRoughness:   surfaceData.soFixupMaxAddedRoughness =        surfaceDescription.SOFixupMaxAddedRoughness;

    $SurfaceDescription.Anisotropy:                surfaceData.anisotropyA =              surfaceDescription.Anisotropy;
    $SurfaceDescription.AnisotropyB:               surfaceData.anisotropyB =              surfaceDescription.AnisotropyB;
    $SurfaceDescription.CoatSmoothness:            surfaceData.coatPerceptualSmoothness = surfaceDescription.CoatSmoothness;
    $SurfaceDescription.CoatMask:                  surfaceData.coatMask =                 surfaceDescription.CoatMask;
    $SurfaceDescription.CoatIor:                   surfaceData.coatIor =                  surfaceDescription.CoatIor;
    $SurfaceDescription.CoatThickness:             surfaceData.coatThickness =            surfaceDescription.CoatThickness;
    $SurfaceDescription.CoatExtinction:            surfaceData.coatExtinction =           surfaceDescription.CoatExtinction;
    $SurfaceDescription.LobeMix:                   surfaceData.lobeMix =                  surfaceDescription.LobeMix;
    $SurfaceDescription.Haziness:                  surfaceData.haziness =                 surfaceDescription.Haziness;
    $SurfaceDescription.HazeExtent:                surfaceData.hazeExtent =               surfaceDescription.HazeExtent;
    // Note: we don't know yet if we put 1.0 in surfaceData.hazyGlossMaxDielectricF0 or the graph provided value:
    //$SurfaceDescription.HazyGlossMaxDielectricF0:  surfaceData.hazyGlossMaxDielectricF0 = surfaceDescription.HazyGlossMaxDielectricF0;

    // These static material feature allow compile time optimization
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_STACK_LIT_STANDARD;
    #ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_SPECULAR_COLOR;
    #endif
    #ifdef _MATERIAL_FEATURE_DUAL_SPECULAR_LOBE
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_DUAL_SPECULAR_LOBE;
    #endif
    #ifdef _MATERIAL_FEATURE_HAZY_GLOSS
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_HAZY_GLOSS;
    #endif
    #ifdef _MATERIAL_FEATURE_ANISOTROPY
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY;
    #endif
    #ifdef _MATERIAL_FEATURE_COAT
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_COAT;
    #endif
    #ifdef _MATERIAL_FEATURE_COAT_NORMALMAP
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_COAT_NORMAL_MAP;
    #endif
    #ifdef _MATERIAL_FEATURE_IRIDESCENCE
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_IRIDESCENCE;
    #endif
    #ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING;
    #endif
    #ifdef _MATERIAL_FEATURE_TRANSMISSION
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION;
    #endif

    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_SSS_DIFFUSE_POWER;
    $UseProfileLobes: surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_SSS_DUAL_LOBE;

    #ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
        // Reproduce the energy conservation done in legacy Unity. Not ideal but better for compatibility and users can unchek it
        $Specular.EnergyConserving: surfaceData.baseColor *= (1.0 - Max3(surfaceData.specularColor.r, surfaceData.specularColor.g, surfaceData.specularColor.b));
    #endif

    #ifdef _MATERIAL_FEATURE_HAZY_GLOSS
        surfaceData.hazyGlossMaxDielectricF0 = 1.0;
        $CapHazinessIfNotMetallic: surfaceData.hazyGlossMaxDielectricF0 = surfaceDescription.HazyGlossMaxDielectricF0;
        // ...no need for the SurfaceDescription.HazyGlossMaxDielectricF0: predicate,
        // the CapHazinessIfNotMetallic: predicate is only active if the pass wants the PixelShaderSlot HazyGlossMaxDielectricF0
        // and if masterNode.capHazinessWrtMetallic.isOn:, the later should ensure we're in hazygloss mode and thus,
        // the masterNode has the HazyGlossMaxDielectricF0 material slot added.
    #endif

    //
    // Setup all surfaceData normals: .normalWS, .bentNormalWS, .tangentWS, .coatNormalWS, .geomNormalWS
    //

    float3 doubleSidedConstants = GetDoubleSidedConstants();

    ApplyDecalAndGetNormal(fragInputs, posInput, surfaceDescription, surfaceData);

    surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];

    surfaceData.coatNormalWS = surfaceData.geomNormalWS;
    $SurfaceDescription.CoatNormalOS: GetNormalWS_SrcOS(fragInputs, surfaceDescription.CoatNormalOS, surfaceData.coatNormalWS, doubleSidedConstants);
    $SurfaceDescription.CoatNormalTS: GetNormalWS(fragInputs, surfaceDescription.CoatNormalTS, surfaceData.coatNormalWS, doubleSidedConstants);
    $SurfaceDescription.CoatNormalWS: GetNormalWS_SrcWS(fragInputs, surfaceDescription.CoatNormalWS, surfaceData.CoatNormalWS, doubleSidedConstants);

    // surfaceData.tangentWS = normalize(fragInputs.tangentToWorld[0].xyz);
    // ...We don't need to normalize if we're going to call Orthonormalize anyways as long as
    // surfaceData.normalWS is normalized:
    surfaceData.tangentWS = (fragInputs.tangentToWorld[0].xyz); // The tangent is not normalize in tangentToWorld for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT

    $SurfaceDescription.TangentOS: surfaceData.tangentWS = TransformObjectToWorldNormal(surfaceDescription.TangentOS);
    $SurfaceDescription.TangentTS: surfaceData.tangentWS = TransformTangentToWorld(surfaceDescription.TangentTS, fragInputs.tangentToWorld);
    $SurfaceDescription.TangentWS: surfaceData.tangentWS = surfaceDescription.TangentWS;

    surfaceData.bentNormalWS = float3(0.0, 0.0, 0.0); // Initialise bentNormalWS before decal to keep compiler quiet, will be override after decal.

    bentNormalWS = surfaceData.normalWS;
    $BentNormal: GetNormalWS(fragInputs, surfaceDescription.BentNormal, bentNormalWS, doubleSidedConstants);
    surfaceData.bentNormalWS = bentNormalWS;

    surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);

    //
    // SpecularAA
    //

    // TODO Note: specular occlusion that uses bent normals should also use filtering, although the visibility model is not a
    // specular lobe with roughness but a cone with solid angle determined by the ambient occlusion so this is an even more
    // empirical hack (with visibility modelled by a single circular region in direction space)
    // Intuitively, an increase of variance should enlarge (possible) visibility and thus diminish the occlusion
    // (enlarge the visibility cone). This goes in hand with the softer BSDF specular lobe.

    // Note that when using the Hazy Gloss parametrization, the user has no direct control on smoothnessB, and
    // surfaceData.perceptualSmoothnessB will be 0 in that case.
    // Conceptually, in that mode smoothnessB now depends on hazeExtent and smoothnessA only, and hazeExtent is
    // a perceptual control in that "smoothnessB from smoothnessA" dependency (it will also influence lobeMix and the new
    // f0 for the base layer precisely via smoothnessB). Finally, in that parametrization, smoothnessB is always <= smoothnessA.
    // It thus makes sense to only modify smoothnessA while doing SpecularAA, the hazemapping takes care of the rest.
    // The compiler should prune out our calculations for surfaceData.perceptualSmoothnessB in that case since it will never
    // be read before being overwritten later.

    float geometricVariance = 0.0;
#if !defined(SHADER_STAGE_RAY_TRACING)
    // Note Specular.AA: or Specular.GeometricAA: guarantees surfaceDescription has SpecularAAScreenSpaceVariance and SpecularAAThreshold.
    $Specular.GeometricAA: geometricVariance = GeometricNormalVariance(fragInputs.tangentToWorld[2], surfaceDescription.SpecularAAScreenSpaceVariance);
#endif
    // TODO: Handle normal map filtering
    // Also handle texture normal filtering when we have the proper operator nodes and can thus have the variance port
    // in our master node:
    float textureFilteringVariance = 0.0;
    //$NormalTexturtextureFiltering: textureFilteringVariance = DecodeVariance(surfaceDescription.CodedNormalVarianceMeasure);
    float coatTextureFilteringVariance = 0.0;
    //$NormalTexturtextureFiltering: coatTextureFilteringVariance = DecodeVariance(surfaceDescription.CodedCoatNormalVarianceMeasure);

    #if defined(DEBUG_DISPLAY)
    #if !defined(SHADER_STAGE_RAY_TRACING)
        // Mipmap mode debugging isn't supported with ray tracing as it relies on derivatives
        if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
        {
            #ifdef FRAG_INPUTS_USE_TEXCOORD0
                surfaceData.baseColor = GET_TEXTURE_STREAMING_DEBUG(posInput.positionSS, fragInputs.texCoord0);
            #else
                surfaceData.baseColor = GET_TEXTURE_STREAMING_DEBUG_NO_UV(posInput.positionSS);
            #endif
            surfaceData.metallic = 0;
        }
    #endif

        // We need to call ApplyDebugToSurfaceData after filling the surfaceData and before filling builtinData
        // as it can modify attributes used for static lighting
        ApplyDebugToSurfaceData(fragInputs.tangentToWorld, surfaceData);
    #endif

    $Specular.AA: surfaceData.perceptualSmoothnessA = NormalFiltering(surfaceData.perceptualSmoothnessA, geometricVariance + textureFilteringVariance, surfaceDescription.SpecularAAThreshold);
    $Specular.AA: surfaceData.perceptualSmoothnessB = NormalFiltering(surfaceData.perceptualSmoothnessB, geometricVariance + textureFilteringVariance, surfaceDescription.SpecularAAThreshold);
    $Specular.AA: surfaceData.coatPerceptualSmoothness = NormalFiltering(surfaceData.coatPerceptualSmoothness, geometricVariance + coatTextureFilteringVariance, surfaceDescription.SpecularAAThreshold);
}
