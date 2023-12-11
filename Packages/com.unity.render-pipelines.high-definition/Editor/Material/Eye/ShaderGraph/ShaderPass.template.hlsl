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
    $SurfaceDescription.Occlusion:                  surfaceData.ambientOcclusion            = surfaceDescription.Occlusion;
    $SurfaceDescription.IOR:                        surfaceData.IOR                         = surfaceDescription.IOR;
    $SurfaceDescription.Mask:                       surfaceData.mask =                      surfaceDescription.Mask;
    $SurfaceDescription.DiffusionProfileHash:       surfaceData.diffusionProfileHash =      asuint(surfaceDescription.DiffusionProfileHash);
    $SurfaceDescription.SubsurfaceMask:             surfaceData.subsurfaceMask = surfaceDescription.SubsurfaceMask;
    $SurfaceDescription.IrisPlaneOffset:             surfaceData.irisPlaneOffset =           surfaceDescription.IrisPlaneOffset;
    $SurfaceDescription.IrisRadius:                 surfaceData.irisRadius =                surfaceDescription.IrisRadius;
    $SurfaceDescription.CausticIntensity:           surfaceData.causticIntensity =          surfaceDescription.CausticIntensity;
    // Input by graph needs to be saturated to avoid unwated behaviors
    $SurfaceDescription.CausticBlend:               surfaceData.causticBlend =              saturate(surfaceDescription.CausticBlend);

    // These static material feature allow compile time optimization
    surfaceData.materialFeatures = 0;

    #ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_EYE_SUBSURFACE_SCATTERING;
    #endif

    #ifdef _MATERIAL_FEATURE_EYE_CINEMATIC
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_EYE_CINEMATIC;
    #endif

    #ifdef _MATERIAL_FEATURE_EYE_CAUSTIC_LUT
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_EYE_CAUSTIC_FROM_LUT | MATERIALFEATUREFLAGS_EYE_CINEMATIC; //force light refraction with caustic
    #endif

    float3 doubleSidedConstants = GetDoubleSidedConstants();

    ApplyDecalAndGetNormal(fragInputs, posInput, surfaceDescription, surfaceData);

    // Note: It is assume that user in the shader graph provide a normal map with flat normal at the Cornea location
    // and an iris normal map. Same for smoothness, IOR and for subsurface mask. So we don't do any operation here.

    surfaceData.irisNormalWS = surfaceData.normalWS;
    $SurfaceDescription.IrisNormalOS: GetNormalWS_SrcOS(fragInputs, surfaceDescription.IrisNormalOS, surfaceData.irisNormalWS, doubleSidedConstants);
    $SurfaceDescription.IrisNormalTS: GetNormalWS(fragInputs, surfaceDescription.IrisNormalTS, surfaceData.irisNormalWS, doubleSidedConstants);
    $SurfaceDescription.IrisNormalWS: GetNormalWS_SrcWS(fragInputs, surfaceDescription.IrisNormalWS, surfaceData.irisNormalWS, doubleSidedConstants);

    surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];

    bentNormalWS = surfaceData.irisNormalWS; // Use diffuse normal (iris) to fetch GI, unless users provide explicit bent normal (not affected by decals)
    $BentNormal: GetNormalWS(fragInputs, surfaceDescription.BentNormal, bentNormalWS, doubleSidedConstants);

    #ifdef DEBUG_DISPLAY
    #if !defined(SHADER_STAGE_RAY_TRACING)
        // Mipmap mode debugging isn't supported with ray tracing as it relies on derivatives
        if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
        {
            #ifdef FRAG_INPUTS_USE_TEXCOORD0
                surfaceData.baseColor = GET_TEXTURE_STREAMING_DEBUG(posInput.positionSS, fragInputs.texCoord0);
            #else
                surfaceData.baseColor = GET_TEXTURE_STREAMING_DEBUG_NO_UV(posInput.positionSS);
            #endif
        }
    #endif

        // We need to call ApplyDebugToSurfaceData after filling the surfaceData and before filling builtinData
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
