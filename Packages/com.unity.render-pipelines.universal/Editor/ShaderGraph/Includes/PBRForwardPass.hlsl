
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DistanceFog.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/VolumetricFogBlendMode.hlsl"

void InitializeInputData(Varyings input, SurfaceDescription surfaceDescription, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

    #ifdef _NORMALMAP
        // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
        float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
        float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

        inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
        #if _NORMAL_DROPOFF_TS
            inputData.normalWS = TransformTangentToWorld(surfaceDescription.NormalTS, inputData.tangentToWorld);
        #elif _NORMAL_DROPOFF_OS
            inputData.normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
        #elif _NORMAL_DROPOFF_WS
            inputData.normalWS = surfaceDescription.NormalWS;
        #endif
    #else
        inputData.normalWS = input.normalWS;
    #endif
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    #if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
        inputData.shadowCoord = ShadowCoordInterpolatorAvailable() ? input.shadowCoord : TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = MainLightShadowsAvailable() ? TransformWorldToShadowCoord(inputData.positionWS) : float4(0, 0, 0, 0);
    #endif

    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    #if defined(DEBUG_DISPLAY)
    #if USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR
    inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
    #endif
    #if USE_LIGHTMAP_UV_INTERPOLATOR
    inputData.staticLightmapUV = input.staticLightmapUV;
    #endif
    #if USE_VERTEX_SH_INTERPOLATOR
    inputData.vertexSH = input.sh;
    #endif

    #if defined(USE_APV_PROBE_OCCLUSION)
    inputData.probeOcclusion = input.probeOcclusion;
    #endif

    inputData.positionCS = input.positionCS;
    #endif

    inputData.preExposureMultiplier = GetPreExposureMultiplier();
}

void InitializeBakedGIData(Varyings input, inout InputData inputData, bool isSurfaceTypeTransparent)
{
    GIParams giParams = (GIParams)0;

    #if USE_LIGHTMAP_UV_INTERPOLATOR
    giParams.staticLightmapUV = input.staticLightmapUV;
    #endif
    #if USE_VERTEX_SH_INTERPOLATOR
    giParams.vertexSH = input.sh;
    #endif
    #if USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR
    giParams.dynamicLightmapUV = input.dynamicLightmapUV.xy;
    #endif
    #ifdef USE_APV_PROBE_OCCLUSION
    giParams.vertexProbeOcclusion = input.probeOcclusion;
    #endif

    giParams.positionWS = inputData.positionWS;
    giParams.normalWS = inputData.normalWS;
    giParams.viewDirWS = inputData.viewDirectionWS;
    giParams.positionSS = input.positionCS.xy;
    giParams.isSurfaceTypeTransparent = isSurfaceTypeTransparent;

    InitializeBakedGI(giParams, inputData.bakedGI, inputData.shadowMask);
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
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

#if defined(_SURFACE_TYPE_TRANSPARENT)
    bool isSurfaceTypeTransparent = true;
#else
    bool isSurfaceTypeTransparent = false;
#endif

#if defined(_ALPHATEST_ON)
    half alpha = AlphaDiscard(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
#elif defined(_SURFACE_TYPE_TRANSPARENT)
    half alpha = surfaceDescription.Alpha;
#else
    half alpha = half(1.0);
#endif

    LODFadeCrossFade(unpacked.positionCS);

    InputData inputData;
    InitializeInputData(unpacked, surfaceDescription, inputData);
    #ifdef VARYINGS_NEED_TEXCOORD0
        SETUP_DEBUG_TEXTURE_DATA(inputData, unpacked.texCoord0);
    #else
        SETUP_DEBUG_TEXTURE_DATA_NO_UV(inputData);
    #endif

    #ifdef _SPECULAR_SETUP
        float3 specular = surfaceDescription.Specular;
        float metallic = 1;
    #else
        float3 specular = 0;
        float metallic = surfaceDescription.Metallic;
    #endif

    half3 normalTS = half3(0, 0, 0);
    #if defined(_NORMALMAP) && defined(_NORMAL_DROPOFF_TS)
        normalTS = surfaceDescription.NormalTS;
    #endif

    SurfaceData surface;
    surface.albedo              = surfaceDescription.BaseColor;
    surface.metallic            = saturate(metallic);
    surface.specular            = specular;
    surface.smoothness          = saturate(surfaceDescription.Smoothness),
    surface.occlusion           = surfaceDescription.Occlusion,
    surface.emission            = surfaceDescription.Emission,
    surface.alpha               = saturate(alpha);
    surface.normalTS            = normalTS;
    surface.clearCoatMask       = 0;
    surface.clearCoatSmoothness = 1;

    #ifdef _CLEARCOAT
        surface.clearCoatMask       = saturate(surfaceDescription.CoatMask);
        surface.clearCoatSmoothness = saturate(surfaceDescription.CoatSmoothness);
    #endif

    surface.albedo = AlphaModulate(surface.albedo, surface.alpha);

#if defined(_DBUFFER) && !defined(_SURFACE_TYPE_TRANSPARENT)
    ApplyDecalToSurfaceData(unpacked.positionCS, surface, inputData);
#endif

    InitializeBakedGIData(unpacked, inputData, isSurfaceTypeTransparent);

    URP_LIGHT_ACCUM4 color = UniversalFragmentPBR(inputData, surface);
    color.rgb = ClampExposed(inputData.preExposureMultiplier * BlendDistanceFog(color.rgb, unpacked.positionCS));
#if defined(_SURFACE_TYPE_TRANSPARENT) && defined(_TRANSPARENT_RECEIVE_FOG)
    color.rgb = MixVolumetricFog(color.rgb, color.a, VolumetricFogBlendModeFromDefines(), false, unpacked.positionCS);
#endif

    color.a = OutputAlpha(color.a, isSurfaceTypeTransparent);

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}
