
float3 ConvertToNormalTS(float3 normalData, float3 tangentWS, float3 bitangentWS)
{
#ifdef _NORMALMAP
    #ifdef SURFACE_GRADIENT
        return SurfaceGradientFromTBN(normalData.xy, tangentWS, bitangentWS);
    #else
        return normalData;
    #endif
#else
    #ifdef SURFACE_GRADIENT
        return float3(0.0, 0.0, 0.0); // No gradient
    #else
        return float3(0.0, 0.0, 1.0);
    #endif
#endif
}

void BuildSurfaceData(inout FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData, out float3 bentNormalWS)
{
    ZERO_INITIALIZE(SurfaceData, surfaceData);

#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    #ifdef TERRAIN_PERPIXEL_NORMAL_OVERRIDE
        float3 normalWS = surfaceDescription.normalWS;
    #else
        float2 texCoord0 = fragInputs.texCoord0.xy / _TerrainHeightmapRecipSize.zw;
        float2 terrainNormalMapUV = (texCoord0 + 0.5f) * _TerrainHeightmapRecipSize.xy;
        float3 normalOS = SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, terrainNormalMapUV).rgb * 2.0 - 1.0;
        float3 normalWS = mul((float3x3)GetObjectToWorldMatrix(), normalOS);
    #endif
    float4 tangentWS = ConstructTerrainTangent(normalWS, GetObjectToWorldMatrix()._13_23_33);
    fragInputs.tangentToWorld = BuildTangentToWorld(tangentWS, normalWS);
    surfaceData.normalWS = normalWS;
#endif

    // The tangent is not normalize in tangentToWorld for mikkt. Tag: SURFACE_GRADIENT
    surfaceData.tangentWS = normalize(fragInputs.tangentToWorld[0].xyz);

    surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];

    $SurfaceDescription.BaseColor:         surfaceData.baseColor =            surfaceDescription.BaseColor;
    $SurfaceDescription.Smoothness:        surfaceData.perceptualSmoothness = surfaceDescription.Smoothness;
    $SurfaceDescription.Occlusion:         surfaceData.ambientOcclusion =     surfaceDescription.Occlusion;
    $SurfaceDescription.SpecularOcclusion: surfaceData.specularOcclusion =    surfaceDescription.SpecularOcclusion;
    $SurfaceDescription.Metallic:          surfaceData.metallic =             surfaceDescription.Metallic;

    // Transparency parameters
    // Use thickness from SSS
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;
    surfaceData.transmittanceMask = 0.0;

    // specularOcclusion need to be init ahead of decal to quiet the compiler that modify the SurfaceData struct
    // however specularOcclusion can come from the graph, so need to be init here so it can be override.
    surfaceData.specularOcclusion = 1.0;

    // These static material feature allow compile time optimization
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

#if defined(DECAL_SURFACE_GRADIENT) && defined(SURFACE_GRADIENT)
    #if !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL) || !defined(TERRAIN_PERPIXEL_NORMAL_OVERRIDE)
    float3 normalTS = float3(0.0, 0.0, 1.0);
    $SurfaceDescription.NormalOS: normalTS = TransformObjectToTangent(surfaceDescription.NormalOS, fragInputs.tangentToWorld);
    $SurfaceDescription.NormalTS: normalTS = surfaceDescription.NormalTS;
    $SurfaceDescription.NormalWS: normalTS = TransformWorldToTangent(surfaceDescription.NormalWS, fragInput.tangentToWorld);
    normalTS = ConvertToNormalTS(normalTS, fragInputs.tangentToWorld[0], fragInputs.tangentToWorld[1]);
        #if HAVE_DECALS
        if (_EnableDecals)
        {
            float alpha = 1.0; // unused

            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, fragInputs, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, fragInputs.tangentToWorld[2], surfaceData, normalTS);
        }
        #endif // HAVE_DECALS
        GetNormalWS(fragInputs, normalTS, surfaceData.normalWS, float3(1.0, 1.0, 1.0));
    #elif HAVE_DECALS
        if (_EnableDecals)
        {
            float3 normalTS = SurfaceGradientFromPerturbedNormal(input.tangentToWorld[2], surfaceData.normalWS);

            float alpha = 1.0; // unused

            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, fragInputs, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, fragInputs.tangentToWorld[2], surfaceData, normalTS);

            GetNormalWS(fragInputs, normalTS, surfaceData.normalWS, float3(1.0, 1.0, 1.0));
        }
    #endif // HAVE_DECALS
#else // DECAL_SURFACE_GRADIENT && SURFACE_GRADIENT
    #if !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL) || !defined(TERRAIN_PERPIXEL_NORMAL_OVERRIDE)
    float3 normalTS = float3(0.0, 0.0, 1.0);
    $SurfaceDescription.NormalOS: normalTS = TransformObjectToTangent(surfaceDescription.NormalOS, fragInputs.tangentToWorld);
    $SurfaceDescription.NormalTS: normalTS = surfaceDescription.NormalTS;
    $SurfaceDescription.NormalWS: normalTS = TransformWorldToTangent(surfaceDescription.NormalWS, fragInput.tangentToWorld);
    normalTS = ConvertToNormalTS(normalTS, fragInputs.tangentToWorld[0], fragInputs.tangentToWorld[1]);
    GetNormalWS(fragInputs, normalTS, surfaceData.normalWS, float3(1.0, 1.0, 1.0));
    #endif

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
