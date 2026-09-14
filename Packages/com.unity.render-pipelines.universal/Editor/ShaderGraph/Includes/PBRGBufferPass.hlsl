
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"

void InitializeInputData(Varyings input, SurfaceDescription surfaceDescription, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;

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

    inputData.shadowCoord = MainLightShadowsAvailable() ? TransformWorldToShadowCoord(inputData.positionWS) : float4(0, 0, 0, 0);

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
    #endif

    inputData.preExposureMultiplier = GetPreExposureMultiplier();
}

void InitializeBakedGIData(Varyings input, inout InputData inputData)
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
    giParams.positionSS = inputData.positionCS.xy;

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

GBufferFragOutput frag(PackedVaryings packedInput)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    #if defined(_ALPHATEST_ON)
        half alpha = surfaceDescription.Alpha;
        clip(alpha - surfaceDescription.AlphaClipThreshold);
    #elif _SURFACE_TYPE_TRANSPARENT
        half alpha = surfaceDescription.Alpha;
    #else
        half alpha = 1;
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

#if defined(_DBUFFER)
    ApplyDecal(unpacked.positionCS,
        surfaceDescription.BaseColor,
        specular,
        inputData.normalWS,
        metallic,
        surfaceDescription.Occlusion,
        surfaceDescription.Smoothness);
#endif

    InitializeBakedGIData(unpacked, inputData);

    // in LitForwardPass GlobalIllumination (and temporarily LightingPhysicallyBased) are called inside UniversalFragmentPBR
    // in Deferred rendering we store the sum of these values (and of emission as well) in the GBuffer
    BRDFData brdfData;
    InitializeBRDFData(surfaceDescription.BaseColor, metallic, specular, surfaceDescription.Smoothness, alpha, brdfData);

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
    URP_LIGHT_ACCUM3 color = GlobalIllumination(brdfData, (BRDFData)0, 0,
                                            inputData.bakedGI, surfaceDescription.Occlusion, inputData.positionWS,
                                            inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);

    return PackGBuffersBRDFData(brdfData, inputData, surfaceDescription.Smoothness, ClampExposed(inputData.preExposureMultiplier * (surfaceDescription.Emission + color)), surfaceDescription.Occlusion);
}
