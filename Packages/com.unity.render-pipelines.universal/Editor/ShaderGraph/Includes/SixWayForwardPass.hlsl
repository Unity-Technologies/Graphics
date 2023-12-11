void InitializeInputData(Varyings input, bool frontFace, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

    float signNormal = frontFace ? 1.0f : -1.0f;
    inputData.normalWS = signNormal * input.normalWS;
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, signNormal * input.normalWS);

    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.sh;
    #endif

    inputData.positionCS = input.positionCS;
    #endif
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

void frag(
    PackedVaryings packedInput
    , out half4 outColor : SV_Target0
    , bool frontFace : FRONT_FACE_SEMANTIC
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif

)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

#if defined(_SURFACE_TYPE_TRANSPARENT)
    bool isTransparent = true;
#else
    bool isTransparent = false;
#endif

#if defined(_ALPHATEST_ON)
    half alpha = AlphaDiscard(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
#elif defined(_SURFACE_TYPE_TRANSPARENT)
    half alpha = surfaceDescription.Alpha;
#else
    half alpha = half(1.0);
#endif

    #if defined(LOD_FADE_CROSSFADE) && USE_UNITY_CROSSFADE
        LODFadeCrossFade(unpacked.positionCS);
    #endif

    InputData inputData;
    InitializeInputData(unpacked, frontFace, inputData);

    #ifdef VARYINGS_NEED_TEXCOORD0
        SETUP_DEBUG_TEXTURE_DATA(inputData, unpacked.texCoord0);
    #else
        SETUP_DEBUG_TEXTURE_DATA_NO_UV(inputData);
    #endif
    SixWaySurfaceData surfaceData;
    surfaceData.rightTopBack = surfaceDescription.RightTopBack * INV_PI;
    surfaceData.leftBottomFront = surfaceDescription.LeftBottomFront * INV_PI;
    surfaceData.emission = surfaceDescription.Emission;
    surfaceData.baseColor = surfaceDescription.BaseColor;
    surfaceData.occlusion = surfaceDescription.Occlusion;
    surfaceData.alpha = saturate(alpha);
    surfaceData.diffuseGIData0 = unpacked.diffuseGIData0;
    surfaceData.diffuseGIData1 = unpacked.diffuseGIData1;
    surfaceData.diffuseGIData2 = unpacked.diffuseGIData2;
#if defined(_SIX_WAY_COLOR_ABSORPTION)
    surfaceData.absorptionRange = INV_PI + saturate(surfaceDescription.AbsorptionStrength) * (1 - INV_PI);
#endif


    half4 color = UniversalFragmentSixWay(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    color.a = OutputAlpha(color.a, isTransparent);

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}
