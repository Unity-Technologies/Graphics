// Deprecated. Include GBufferInput.hlsl or GBufferOutput.hlsl instead.
#ifndef UNIVERSAL_GBUFFERUTIL_INCLUDED
#define UNIVERSAL_GBUFFERUTIL_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"

// Deprecated.
#define kLightingInvalid  -1

// Deprecated.
#define kLightingLit       1

// Deprecated.
#define kLightingSimpleLit 2

#define DECL_SV_TARGET(idx) SV_Target##idx
#define DECL_OPT_GBUFFER_TARGET(type, name, idx) type name : DECL_SV_TARGET(GBUFFER_IDX_AFTER(idx))

// Deprecated. Please upgrade your shaders to use GBufferFragOutput instead.
struct FragmentOutput
{
    half4 GBuffer0 : SV_Target0;
    half4 GBuffer1 : SV_Target1;
    half4 GBuffer2 : SV_Target2;
    half4 GBuffer3 : SV_Target3; // Camera color attachment

    #if defined(GBUFFER_FEATURE_DEPTH)
    DECL_OPT_GBUFFER_TARGET(float, GBuffer4, GBUFFER_IDX_R_DEPTH);
    #endif

    #if defined(GBUFFER_FEATURE_SHADOWMASK)
    DECL_OPT_GBUFFER_TARGET(half4, GBuffer5, GBUFFER_IDX_RGBA_SHADOWMASK);
    #endif

    #if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
    DECL_OPT_GBUFFER_TARGET(half4, GBuffer6, GBUFFER_IDX_R_RENDERING_LAYERS);
    #endif
};

#undef DECL_SV_TARGET
#undef DECL_OPT_GBUFFER_TARGET

// Deprecated. Please upgrade your shaders to use PackGBuffersSurfaceData() instead.
GBufferFragOutput SurfaceDataToGbuffer(SurfaceData surfaceData, InputData inputData, half3 globalIllumination, int lightingMode)
{
    return PackGBuffersSurfaceData(surfaceData, inputData, globalIllumination);
}

// Deprecated. Please upgrade your shaders to use PackGBuffersBRDFData() instead.
GBufferFragOutput BRDFDataToGbuffer(BRDFData brdfData, InputData inputData, half smoothness, half3 globalIllumination, half occlusion = 1.0)
{
    return PackGBuffersBRDFData(brdfData, inputData, smoothness, globalIllumination, occlusion);
}

// Deprecated. Please upgrade your shaders to use UnpackGBuffers() & GBufferDataToSurfaceData() instead (include GBufferInput.hlsl).
SurfaceData SurfaceDataFromGbuffer(half4 gbuffer0, half4 gbuffer1, half4 gbuffer2, int lightingMode)
{
    SurfaceData surfaceData;

    surfaceData.albedo = gbuffer0.rgb;
    uint materialFlags = UnpackGBufferMaterialFlags(gbuffer0.a);
    surfaceData.occlusion = 1.0; // Not used by SimpleLit material.
    surfaceData.specular = gbuffer1.rgb;
    half smoothness = gbuffer2.a;

    surfaceData.metallic = 0.0; // Not used by SimpleLit material.
    surfaceData.alpha = 1.0; // gbuffer only contains opaque materials
    surfaceData.smoothness = smoothness;

    surfaceData.emission = (half3)0; // Note: this is not made available at lighting pass in this renderer - emission contribution is included (with GI) in the value GBuffer3.rgb, that is used as a renderTarget during lighting
    surfaceData.normalTS = (half3)0; // Note: does this normalTS member need to be in SurfaceData? It looks like an intermediate value

    return surfaceData;
}

// Deprecated. Please upgrade your shaders to use UnpackGBuffers() & GBufferDataToBRDFData() instead (include GBufferInput.hlsl).
BRDFData BRDFDataFromGbuffer(half4 gbuffer0, half4 gbuffer1, half4 gbuffer2)
{
    half3 albedo = gbuffer0.rgb;
    half3 specular = gbuffer1.rgb;
    uint materialFlags = UnpackGBufferMaterialFlags(gbuffer0.a);
    half smoothness = gbuffer2.a;

    BRDFData brdfData = (BRDFData)0;
    half alpha = half(1.0); // NOTE: alpha can get modfied, forward writes it out (_ALPHAPREMULTIPLY_ON).

    half3 brdfDiffuse;
    half3 brdfSpecular;
    half reflectivity;
    half oneMinusReflectivity;

    if ((materialFlags & kMaterialFlagSpecularSetup) != 0)
    {
        // Specular setup
        reflectivity = ReflectivitySpecular(specular);
        oneMinusReflectivity = half(1.0) - reflectivity;
        brdfDiffuse = albedo * oneMinusReflectivity;
        brdfSpecular = specular;
    }
    else
    {
        // Metallic setup
        reflectivity = specular.r;
        oneMinusReflectivity = 1.0 - reflectivity;
        half metallic = MetallicFromReflectivity(reflectivity);
        brdfDiffuse = albedo * oneMinusReflectivity;
        brdfSpecular = lerp(kDielectricSpec.rgb, albedo, metallic);
    }
    InitializeBRDFDataDirect(albedo, brdfDiffuse, brdfSpecular, reflectivity, oneMinusReflectivity, smoothness, alpha, brdfData);

    return brdfData;
}

// Deprecated.
InputData InputDataFromGbufferAndWorldPosition(half4 gbuffer2, float3 wsPos)
{
    InputData inputData = (InputData)0;

    inputData.positionWS = wsPos;
    inputData.normalWS = normalize(UnpackGBufferNormal(gbuffer2.xyz)); // normalize() is required because terrain shaders use additive blending for normals (not unit-length anymore)

    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(wsPos.xyz);

    // TODO: pass this info?
    inputData.shadowCoord     = (float4)0;
    inputData.fogCoord        = (half  )0;
    inputData.vertexLighting  = (half3 )0;

    inputData.bakedGI = (half3)0; // Note: this is not made available at lighting pass in this renderer - bakedGI contribution is included (with emission) in the value GBuffer3.rgb, that is used as a renderTarget during lighting

    return inputData;
}

// Deprecated. Please upgrade your shaders to use PackGBufferMaterialFlags() instead.
float PackMaterialFlags(uint materialFlags)
{
    return PackGBufferMaterialFlags(materialFlags);
}

// Deprecated. Please upgrade your shaders to use UnpackGBufferMaterialFlags() instead.
uint UnpackMaterialFlags(float packedMaterialFlags)
{
    return UnpackGBufferMaterialFlags(packedMaterialFlags);
}

// Deprecated. Please upgrade your shaders to use PackGBufferNormal() instead.
half3 PackNormal(half3 n)
{
    return PackGBufferNormal(n);
}

// Deprecated. Please upgrade your shaders to use UnpackGBufferNormal() instead.
half3 UnpackNormal(half3 pn)
{
    return UnpackGBufferNormal(pn);
}

#endif
