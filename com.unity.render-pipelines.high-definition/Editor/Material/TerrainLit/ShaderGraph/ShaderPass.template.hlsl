void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData, out float3 bentNormalWS)
{
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    // specularOcclusion need to be init ahead of decal to quiet the compiler that modify the SurfaceData struct
    // however specularOcclusion can come from the graph, so need to be init here so it can be override.
    surfaceData.specularOcclusion = 1.0;

    $SurfaceDescription.BaseColor:                  surfaceData.baseColor =                 surfaceDescription.BaseColor;
    $SurfaceDescription.Smoothness:                 surfaceData.perceptualSmoothness =      surfaceDescription.Smoothness;
    $SurfaceDescription.Occlusion:                  surfaceData.ambientOcclusion =          surfaceDescription.Occlusion;
    $SurfaceDescription.SpecularOcclusion:          surfaceData.specularOcclusion =         surfaceDescription.SpecularOcclusion;
    $SurfaceDescription.Metallic:                   surfaceData.metallic =                  surfaceDescription.Metallic;

    surfaceData.tangentWS = normalize(fragInputs.tangentToWorld[0].xyz);    // The tangent is not normalize in tangentToWorld for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT

    surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];

    // Transparency parameters
    // Use thickness from SSS
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;
    surfaceData.transmittanceMask = 0.0;

    // These static material feature allow compile time optimization
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);

    // normal delivered to master node
    $SurfaceDescription.NormalOS: GetNormalWS_SrcOS(fragInputs, surfaceDescription.NormalOS, surfaceData.normalWS, doubleSidedConstants);
    $SurfaceDescription.NormalTS: GetNormalWS(fragInputs, surfaceDescription.NormalTS, surfaceData.normalWS, doubleSidedConstants);
    $SurfaceDescription.NormalWS: GetNormalWS_SrcWS(fragInputs, surfaceDescription.NormalWS, surfaceData.normalWS, doubleSidedConstants);

    $SurfaceDescription.TangentOS: surfaceData.tangentWS = TransformObjectToWorldNormal(surfaceDescription.TangentOS);
    $SurfaceDescription.TangentTS: surfaceData.tangentWS = TransformTangentToWorld(surfaceDescription.TangentTS, fragInputs.tangentToWorld);
    $SurfaceDescription.TangentWS: surfaceData.tangentWS = surfaceDescription.TangentWS;

#if defined(DECAL_SURFACE_GRADIENT) && defined(SURFACE_GRADIENT)
    $SurfaceDescription.NormalOS: float3 normalTS = TransformObjectToTangent(surfaceDescription.TangentOS, fragInputs.tangentToWorld);
    $SurfaceDescription.NormalTS: float3 normalTS = surfaceDescription.TangentTS;
    $SurfaceDescription.NormalWS: float3 normalTS = TransformWorldToTangent(surfaceDescription.TangentWS, fragInput.tangentToWorld);

    #if !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL) || !defined(TERRAIN_PERPIXEL_NORMAL_OVERRIDE)
        #if HAVE_DECALS
        if (_EnableDecals)
        {
            float alpha = 1.0; // unused

            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, fragInputs, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, fragInputs.tangentToWorld[2], surfaceData, normalTS);
        }
        #endif // HAVE_DECALS
    #elif HAVE_DECALS
        if (_EnableDecals)
        {
            float alpha = 1.0; // unused

            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, fragInputs, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, fragInputs.tangentToWorld[2], surfaceData, normalTS);
        }
    #endif // HAVE_DECALS
#else // DECAL_SURFACE_GRADIENT && SURFACE_GRADIENT
    #if HAVE_DECALS
        if (_EnableDecals)
        {
            float alpha = 1.0; // unused

            // Both uses and modifies 'surfaceData.normalWS'.
            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, fragInputs, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, fragInputs.tangentToWorld[2], surfaceData);
        }
    #endif // HAVE_DECALS
#endif // DECAL_SURFACE_GRADIENT && SURFACE_GRADIENT

    bentNormalWS = surfaceData.normalWS;
    $BentNormal: GetNormalWS(fragInputs, surfaceDescription.BentNormal, bentNormalWS, doubleSidedConstants);

    surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);

#ifdef DEBUG_DISPLAY
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        // TODO: need to update mip info
        TerrainLitDebug(fragInputs.texCoord0.xy, surfaceData.baseColor);
        surfaceData.metallic = 0;
    }

    // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
    // as it can modify attribute use for static lighting
    ApplyDebugToSurfaceData(fragInputs.tangentToWorld, surfaceData);
#endif

// By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion.
// If user provide bent normal then we process a better term
#if defined(_MASKMAP) && !defined(_SPECULAR_OCCLUSION_NONE)
    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
#endif

#if defined(_ENABLE_GEOMETRIC_SPECULAR_AA) && !defined(SHADER_STAGE_RAY_TRACING)
    surfaceData.perceptualSmoothness = GeometricNormalFiltering(surfaceData.perceptualSmoothness, fragInputs.tangentToWorld[2], surfaceDescription.SpecularAAScreenSpaceVariance, surfaceDescription.SpecularAAThreshold);
#endif
}
