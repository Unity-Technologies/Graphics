void ApplyDecalToSurfaceDataNoNormal(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
    surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;

#ifdef DECALS_4RT // only smoothness in 3RT mode
    // Don't apply any metallic modification
    surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.MAOSBlend.y + decalSurfaceData.mask.y;
#endif

    surfaceData.perceptualSmoothness = surfaceData.perceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
}

void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData, out float3 bentNormalWS)
{
    // setup defaults -- these are used if the graph doesn't output a value
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    // specularOcclusion need to be init ahead of decal to quiet the compiler that modify the SurfaceData struct
    // however specularOcclusion can come from the graph, so need to be init here so it can be override.
    surfaceData.specularOcclusion = 1.0;

    // copy across graph values, if defined
    $SurfaceDescription.BaseColor:                  surfaceData.baseColor =                 surfaceDescription.BaseColor;
    $SurfaceDescription.SpecularOcclusion:          surfaceData.specularOcclusion =         surfaceDescription.SpecularOcclusion;
    $SurfaceDescription.Smoothness:                 surfaceData.perceptualSmoothness =      surfaceDescription.Smoothness;
    $SurfaceDescription.Occlusion:                  surfaceData.ambientOcclusion =          surfaceDescription.Occlusion;
    $SurfaceDescription.Specular:                   surfaceData.specularColor =             surfaceDescription.Specular;
    $SurfaceDescription.DiffusionProfileHash:       surfaceData.diffusionProfileHash =      asuint(surfaceDescription.DiffusionProfileHash);
    $SurfaceDescription.SubsurfaceMask:             surfaceData.subsurfaceMask =            surfaceDescription.SubsurfaceMask;
    $SurfaceDescription.TransmissionMask:           surfaceData.transmissionMask =          surfaceDescription.TransmissionMask;
    $SurfaceDescription.Thickness:                  surfaceData.thickness =                 surfaceDescription.Thickness;
    $SurfaceDescription.Anisotropy:                 surfaceData.anisotropy =                surfaceDescription.Anisotropy;

    // These static material feature allow compile time optimization
    surfaceData.materialFeatures = 0;

    // Transform the preprocess macro into a material feature (note that silk flag is deduced from the abscence of this one)
    #ifdef _MATERIAL_FEATURE_COTTON_WOOL
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL;
        $SurfaceDescription.Smoothness:                 surfaceData.perceptualSmoothness =      lerp(0.0, 0.6, surfaceDescription.Smoothness);
    #else
        // Initialize the normal to something non-zero to avoid div-zero warnings for anisotropy.
        surfaceData.normalWS = float3(0, 1, 0);
    #endif

    #ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING;
    #endif

    #ifdef _MATERIAL_FEATURE_TRANSMISSION
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_FABRIC_TRANSMISSION;
    #endif

    #if defined (_ENERGY_CONSERVING_SPECULAR)
        // Require to have setup baseColor
        // Reproduce the energy conservation done in legacy Unity. Not ideal but better for compatibility and users can unchek it
        surfaceData.baseColor *= (1.0 - Max3(surfaceData.specularColor.r, surfaceData.specularColor.g, surfaceData.specularColor.b));
    #endif

    float3 doubleSidedConstants = GetDoubleSidedConstants();

    ApplyDecalAndGetNormal(fragInputs, posInput, surfaceDescription, surfaceData);

    surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];

    surfaceData.tangentWS = normalize(fragInputs.tangentToWorld[0].xyz);    // The tangent is not normalize in tangentToWorld for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT
    $SurfaceDescription.TangentOS: surfaceData.tangentWS = TransformObjectToWorldNormal(surfaceDescription.TangentOS);
    $SurfaceDescription.TangentTS: surfaceData.tangentWS = TransformTangentToWorld(surfaceDescription.TangentTS, fragInputs.tangentToWorld);
    $SurfaceDescription.TangentWS: surfaceData.tangentWS = surfaceDescription.TangentWS;

    bentNormalWS = surfaceData.normalWS;
    $BentNormal: GetNormalWS(fragInputs, surfaceDescription.BentNormal, bentNormalWS, doubleSidedConstants);

    surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);

    #ifdef DEBUG_DISPLAY
        if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
        {
            // TODO: need to update mip info
        }

        // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
        // as it can modify attribute use for static lighting
        ApplyDebugToSurfaceData(fragInputs.tangentToWorld, surfaceData);
    #endif

    #if defined(_SPECULAR_OCCLUSION_CUSTOM)
        // Just use the value passed through via the slot (not active otherwise)
    #elif defined(_SPECULAR_OCCLUSION_FROM_AO_BENT_NORMAL)
        // If we have bent normal and ambient occlusion, process a specular occlusion
        surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAO(V, bentNormalWS, surfaceData.normalWS, surfaceData.ambientOcclusion, PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness));
    #elif defined(_AMBIENT_OCCLUSION) && defined(_SPECULAR_OCCLUSION_FROM_AO)
        surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
    #endif

    #if defined(_ENABLE_GEOMETRIC_SPECULAR_AA) && !defined(SHADER_STAGE_RAY_TRACING)
        surfaceData.perceptualSmoothness = GeometricNormalFiltering(surfaceData.perceptualSmoothness, fragInputs.tangentToWorld[2], surfaceDescription.SpecularAAScreenSpaceVariance, surfaceDescription.SpecularAAThreshold);
    #endif
}
