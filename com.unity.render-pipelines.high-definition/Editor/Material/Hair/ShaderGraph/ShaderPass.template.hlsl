void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout SurfaceData surfaceData)
{
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
    surfaceData.diffuseColor.xyz = surfaceData.diffuseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;

    // Always test the normal as we can have decompression artifact
    if (decalSurfaceData.normalWS.w < 1.0)
    {
        surfaceData.normalWS.xyz = SafeNormalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
    }

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
    $SurfaceDescription.BaseColor:                      surfaceData.diffuseColor =                  surfaceDescription.BaseColor;
    $SurfaceDescription.SpecularOcclusion:              surfaceData.specularOcclusion =             surfaceDescription.SpecularOcclusion;
    $SurfaceDescription.Smoothness:                     surfaceData.perceptualSmoothness =          surfaceDescription.Smoothness;
    $SurfaceDescription.Occlusion:                      surfaceData.ambientOcclusion =              surfaceDescription.Occlusion;
    $SurfaceDescription.Transmittance:                  surfaceData.transmittance =                 surfaceDescription.Transmittance;
    $SurfaceDescription.RimTransmissionIntensity:       surfaceData.rimTransmissionIntensity =      surfaceDescription.RimTransmissionIntensity;

    $SurfaceDescription.SpecularTint:                   surfaceData.specularTint =                  surfaceDescription.SpecularTint;
    $SurfaceDescription.SpecularShift:                  surfaceData.specularShift =                 surfaceDescription.SpecularShift;

    $SurfaceDescription.SecondarySmoothness:            surfaceData.secondaryPerceptualSmoothness = surfaceDescription.SecondarySmoothness;
    $SurfaceDescription.SecondarySpecularTint:          surfaceData.secondarySpecularTint =         surfaceDescription.SecondarySpecularTint;
    $SurfaceDescription.SecondarySpecularShift:         surfaceData.secondarySpecularShift =        surfaceDescription.SecondarySpecularShift;

    // These static material feature allow compile time optimization
    surfaceData.materialFeatures = 0;

    // Transform the preprocess macro into a material feature
    #ifdef _MATERIAL_FEATURE_HAIR_KAJIYA_KAY
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY;
    #endif

    #ifdef _DOUBLESIDED_ON
        float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
    #else
        float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
    #endif

    // normal delivered to master node
    $SurfaceDescription.NormalOS: surfaceData.normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
    $SurfaceDescription.NormalTS: GetNormalWS(fragInputs, surfaceDescription.NormalTS, surfaceData.normalWS, doubleSidedConstants);
    $SurfaceDescription.NormalWS: surfaceData.normalWS = surfaceDescription.NormalWS;

    surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];

    // For a typical Unity quad, you have tangent vectors pointing to the right (X axis),
    // and bitangent vectors pointing up (Y axis).
    // The current hair setup uses mesh cards (e.g. quads).
    // Hair is usually painted top-down, from the root to the tip.
    // Therefore, DefaultHairStrandTangent = -MeshCardBitangent.
    // Both the SurfaceData and the BSDFData store the hair tangent
    // (which represents the hair strand direction, root to tip).
    surfaceData.hairStrandDirectionWS = -fragInputs.tangentToWorld[1].xyz;
    // The hair strand direction texture contains tangent-space vectors.
    // We use the same convention for the texture, which means that
    // to get the default behavior (DefaultHairStrandTangent = -MeshCardBitangent),
    // the artist has to paint (0, -1, 0).
    // TODO: pending artist feedback...
    $HairStrandDirection: surfaceData.hairStrandDirectionWS = TransformTangentToWorld(surfaceDescription.HairStrandDirection, fragInputs.tangentToWorld);
    // The original Kajiya-Kay BRDF model expects an orthonormal TN frame.
    // Since we use the tangent shift hack (http://web.engr.oregonstate.edu/~mjb/cs519/Projects/Papers/HairRendering.pdf),
    // we may as well not bother to orthonormalize anymore.
    // The tangent should still be a unit vector, though.
    surfaceData.hairStrandDirectionWS = normalize(surfaceData.hairStrandDirectionWS);

    // Small digression about hair and normals [NOTE-HAIR-NORMALS].
    // Since a hair strand is (approximately) a cylinder,
    // there is a whole "circle" of normals corresponding to any given tangent vector.
    // Since we use the Kajiya-Kay shading model,
    // the way we compute and use normals is a bit complicated.
    // We need 4 separate sets of normals.
    // For shadow bias, we use the geometric normal.
    // For direct lighting, we either (conceptually) use the "light-facing" normal
    // or the user-provided normal.
    // For reflected GI (light probes and reflection probes), we use the normal most aligned
    // with the view vector (the "view-facing" normal), or the user-provided normal.
    // We reflect this normal for transmitted GI.
    // For the highlight shift hack (along the tangent), we use the user-provided normal.

    #if HAVE_DECALS
        if (_EnableDecals)
        {
            float alpha = 1.0;
            $SurfaceDescription.Alpha: alpha = surfaceDescription.Alpha;

            // Both uses and modifies 'surfaceData.normalWS'.
            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, fragInputs, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, fragInputs.tangentToWorld[2], surfaceData);
        }
    #endif

    #if (_USE_LIGHT_FACING_NORMAL)
        float3 viewFacingNormalWS = ComputeViewFacingNormal(V, surfaceData.hairStrandDirectionWS);
        float3 N = viewFacingNormalWS; // Not affected by decals
    #else
        float3 N = surfaceData.normalWS;
    #endif

    bentNormalWS = N;

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
        surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAO(V, bentNormalWS, N, surfaceData.ambientOcclusion, PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness));
    #elif defined(_AMBIENT_OCCLUSION) && defined(_SPECULAR_OCCLUSION_FROM_AO)
        surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(N, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
    #endif

    #if defined(_ENABLE_GEOMETRIC_SPECULAR_AA) && !defined(SHADER_STAGE_RAY_TRACING)
        surfaceData.perceptualSmoothness = GeometricNormalFiltering(surfaceData.perceptualSmoothness, fragInputs.tangentToWorld[2], surfaceDescription.SpecularAAScreenSpaceVariance, surfaceDescription.SpecularAAThreshold);
    #endif
}
