$Material.EyeCinematic:             #define _MATERIAL_FEATURE_EYE_CINEMATIC 1
$Material.SubsurfaceScattering:     #define _MATERIAL_FEATURE_SUBSURFACE_SCATTERING 1
$AmbientOcclusion:                  #define _AMBIENT_OCCLUSION 1
$SpecularOcclusionFromAO:           #define _SPECULAR_OCCLUSION_FROM_AO 1
$SpecularOcclusionFromAOBentNormal: #define _SPECULAR_OCCLUSION_FROM_AO_BENT_NORMAL 1
$SpecularOcclusionCustom:           #define _SPECULAR_OCCLUSION_CUSTOM 1
$Specular.AA:                       #define _ENABLE_GEOMETRIC_SPECULAR_AA 1

// If we use subsurface scattering, enable output split lighting (for forward pass)
#if defined(_MATERIAL_FEATURE_SUBSURFACE_SCATTERING) && !defined(_SURFACE_TYPE_TRANSPARENT)
    #define OUTPUT_SPLIT_LIGHTING
#endif

void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_DIFFUSE)
    {
        surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
    }

    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_NORMAL)
    {
        surfaceData.normalWS.xyz = normalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
    }

    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_MASK)
    {
        #ifdef DECALS_4RT // only smoothness in 3RT mode
            // Don't apply any metallic modification
            surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.MAOSBlend.y + decalSurfaceData.mask.y;
        #endif

        surfaceData.perceptualSmoothness = surfaceData.perceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
    }
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

    // These static material feature allow compile time optimization
    surfaceData.materialFeatures = 0;

    #ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_EYE_SUBSURFACE_SCATTERING;
    #endif

    #ifdef _MATERIAL_FEATURE_EYE_CINEMATIC
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_EYE_CINEMATIC;
    #endif

    #ifdef _DOUBLESIDED_ON
        float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
    #else
        float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
    #endif

    // Note: It is assume that user in the shader graph provide a normal map with flat normal at the Cornea location
    // and an iris normal map. Same for smoothness, IOR and for subsurface mask. So we don't do any operation here.

    // normal delivered to master node
    $SurfaceDescription.NormalOS: surfaceData.normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
    $SurfaceDescription.NormalTS: GetNormalWS(fragInputs, surfaceDescription.NormalTS, surfaceData.normalWS, doubleSidedConstants);
    $SurfaceDescription.NormalWS: surfaceData.normalWS = surfaceDescription.NormalWS;

    surfaceData.irisNormalWS = surfaceData.normalWS;
    $SurfaceDescription.IrisNormalOS: surfaceData.irisNormalWS = TransformObjectToWorldNormal(surfaceDescription.IrisNormalOS);
    $SurfaceDescription.IrisNormalTS: GetNormalWS(fragInputs, surfaceDescription.IrisNormalTS, surfaceData.irisNormalWS, doubleSidedConstants);
    $SurfaceDescription.IrisNormalWS: surfaceData.irisNormalWS = surfaceDescription.IrisNormalWS;

    surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];

    #if HAVE_DECALS
        if (_EnableDecals)
        {
            float alpha = 1.0;
            $SurfaceDescription.Alpha: alpha = surfaceDescription.Alpha;

            // Both uses and modifies 'surfaceData.normalWS'.
            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
        }
    #endif

    bentNormalWS = surfaceData.irisNormalWS; // Use diffuse normal (iris) to fetch GI, unless users provide explicit bent normal (not affected by decals)
    $BentNormal: GetNormalWS(fragInputs, surfaceDescription.BentNormal, bentNormalWS, doubleSidedConstants);
       
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
