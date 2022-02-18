#ifndef BUILTIN_GBUFFERUTIL_INCLUDED
#define BUILTIN_GBUFFERUTIL_INCLUDED

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"

// inspired from [builtin_shaders]/CGIncludes/UnityGBuffer.cginc

// Non-static meshes with real-time lighting need to write shadow mask, which in that case stores per-object occlusion probe values.
#if !defined(LIGHTMAP_ON) && defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK)
#define OUTPUT_SHADOWMASK 1 // subtractive
#elif defined(SHADOWS_SHADOWMASK)
#define OUTPUT_SHADOWMASK 2 // shadow mask
#else
#define OUTPUT_SHADOWMASK 0
#endif

#define kLightingInvalid  -1  // No dynamic lighting: can aliase any other material type as they are skipped using stencil
#define kLightingLit       1  // lit shader
#define kLightingSimpleLit 2  // Simple lit shader
// clearcoat 3
// backscatter 4
// skin 5

// Material flags
#define kMaterialFlagReceiveShadowsOff        1 // Does not receive dynamic shadows
#define kMaterialFlagSpecularHighlightsOff    2 // Does not receivce specular
#define kMaterialFlagSubtractiveMixedLighting 4 // The geometry uses subtractive mixed lighting
#define kMaterialFlagSpecularSetup            8 // Lit material use specular setup instead of metallic setup

// Light flags.
#define kLightFlagSubtractiveMixedLighting    4 // The light uses subtractive mixed lighting.

struct FragmentOutput
{
    half4 GBuffer0 : SV_Target0;
    half4 GBuffer1 : SV_Target1;
    half4 GBuffer2 : SV_Target2;
    half4 GBuffer3 : SV_Target3; // Camera color attachment
    #if OUTPUT_SHADOWMASK
    half4 GBuffer4 : SV_Target4;
    #endif
};

float PackMaterialFlags(uint materialFlags)
{
    return materialFlags * (1.0h / 255.0h);
}

uint UnpackMaterialFlags(float packedMaterialFlags)
{
    return uint((packedMaterialFlags * 255.0h) + 0.5h);
}

#ifdef _GBUFFER_NORMALS_OCT
half3 PackNormal(half3 n)
{
    float2 octNormalWS = PackNormalOctQuadEncode(n);                  // values between [-1, +1], must use fp32 on some platforms.
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0, +1]
    return PackFloat2To888(remappedOctNormalWS);                      // values between [ 0, +1]
}

half3 UnpackNormal(half3 pn)
{
    half2 remappedOctNormalWS = Unpack888ToFloat2(pn);                // values between [ 0, +1]
    half2 octNormalWS = remappedOctNormalWS.xy * 2.0h - 1.0h;         // values between [-1, +1]
    return UnpackNormalOctQuadEncode(octNormalWS);                    // values between [-1, +1]
}

half PackSmoothness(half s, int lightingMode)
{
    if (lightingMode == kLightingSimpleLit)                           // See SimpleLitInput.hlsl, SampleSpecularSmoothness().
        return 0.1h * log2(s) - 0.1h;                                 // values between [ 0, +1]
    else
        return s;                                                     // values between [ 0, +1]
}

half UnpackSmoothness(half ps, int lightingMode)
{
    if (lightingMode == kLightingSimpleLit)                           // See SimpleLitInput.hlsl, SampleSpecularSmoothness().
        return exp2(10.0h * ps + 1.0h);
    else
        return ps;                                                    // values between [ 0, +1]
}

#else
half3 PackNormal(half3 n)
{ return n; }                                                         // values between [-1, +1]

half3 UnpackNormal(half3 pn)
{ return pn; }                                                        // values between [-1, +1]

half PackSmoothness(half s, int lightingMode)
{
    if (lightingMode == kLightingSimpleLit)                           // See SimpleLitInput.hlsl, SampleSpecularSmoothness().
        return 0.1h * log2(s) - 0.1h;                                 // Normally values between [-1, +1] but need [0; +1] to make terrain blending works
    else
        return s;                                                     // Normally values between [-1, +1] but need [0; +1] to make terrain blending works
}

half UnpackSmoothness(half ps, int lightingMode)
{
    if (lightingMode == kLightingSimpleLit)                           // See SimpleLitInput.hlsl, SampleSpecularSmoothness().
        return exp2(10.0h * ps + 1.0h);                               // values between [ 0, +1]
    else
        return ps;                                                    // values between [ 0, +1]
}
#endif

// This will encode SurfaceData into GBuffer
FragmentOutput SurfaceDataToGbuffer(SurfaceData surfaceData, InputData inputData, half3 globalIllumination, int lightingMode)
{
    half3 packedNormalWS = PackNormal(inputData.normalWS);
    half packedSmoothness = PackSmoothness(surfaceData.smoothness, lightingMode);

    uint materialFlags = 0;

    // SimpleLit does not use _SPECULARHIGHLIGHTS_OFF to disable specular highlights.

    #ifdef _RECEIVE_SHADOWS_OFF
    materialFlags |= kMaterialFlagReceiveShadowsOff;
    #endif

    #if defined(LIGHTMAP_ON) && defined(_MIXED_LIGHTING_SUBTRACTIVE)
    materialFlags |= kMaterialFlagSubtractiveMixedLighting;
    #endif

    FragmentOutput output;
    output.GBuffer0 = half4(surfaceData.albedo.rgb, PackMaterialFlags(materialFlags));   // albedo          albedo          albedo          materialFlags   (sRGB rendertarget)
    output.GBuffer1 = half4(surfaceData.specular.rgb, surfaceData.occlusion);            // specular        specular        specular        occlusion
    output.GBuffer2 = half4(packedNormalWS, packedSmoothness);                           // encoded-normal  encoded-normal  encoded-normal  packed-smoothness
    output.GBuffer3 = half4(globalIllumination, 1);                                      // GI              GI              GI              [optional: see OutputAlpha()] (lighting buffer)
    #if OUTPUT_SHADOWMASK
    output.GBuffer4 = inputData.shadowMask; // will have unity_ProbesOcclusion value if subtractive lighting is used (baked)
    #endif

    return output;
}

// This decodes the Gbuffer into a SurfaceData struct
SurfaceData SurfaceDataFromGbuffer(half4 gbuffer0, half4 gbuffer1, half4 gbuffer2, int lightingMode)
{
    SurfaceData surfaceData;

    surfaceData.albedo = gbuffer0.rgb;
    uint materialFlags = UnpackMaterialFlags(gbuffer0.a);
    surfaceData.occlusion = 1.0; // Not used by SimpleLit material.
    surfaceData.specular = gbuffer1.rgb;
    half smoothness = UnpackSmoothness(gbuffer2.a, lightingMode);

    surfaceData.metallic = 0.0; // Not used by SimpleLit material.
    surfaceData.alpha = 1.0; // gbuffer only contains opaque materials
    surfaceData.smoothness = smoothness;

    surfaceData.emission = (half3)0; // Note: this is not made available at lighting pass in this renderer - emission contribution is included (with GI) in the value GBuffer3.rgb, that is used as a renderTarget during lighting
    surfaceData.normalTS = (half3)0; // Note: does this normalTS member need to be in SurfaceData? It looks like an intermediate value

    return surfaceData;
}

// This will encode SurfaceData into GBuffer
FragmentOutput BRDFDataToGbuffer(BRDFData brdfData, InputData inputData, half smoothness, half3 globalIllumination, half occlusion = 1.0)
{
    half3 packedNormalWS = PackNormal(inputData.normalWS);
    half packedSmoothness = PackSmoothness(smoothness, kLightingLit);

    uint materialFlags = 0;

    #ifdef _RECEIVE_SHADOWS_OFF
    materialFlags |= kMaterialFlagReceiveShadowsOff;
    #endif

    half3 packedSpecular;

    #ifdef _SPECULAR_SETUP
    materialFlags |= kMaterialFlagSpecularSetup;
    packedSpecular = brdfData.specular.rgb;
    #else
    packedSpecular.r = brdfData.reflectivity;
    packedSpecular.gb = 0.0;
    #endif

    #ifdef _SPECULARHIGHLIGHTS_OFF
    // During the next deferred shading pass, we don't use a shader variant to disable specular calculations.
    // Instead, we can either silence specular contribution when writing the gbuffer, and/or reserve a bit in the gbuffer
    // and use this during shading to skip computations via dynamic branching. Fastest option depends on platforms.
    materialFlags |= kMaterialFlagSpecularHighlightsOff;
    packedSpecular = 0.0.xxx;
    #endif

    #if defined(LIGHTMAP_ON) && defined(_MIXED_LIGHTING_SUBTRACTIVE)
    materialFlags |= kMaterialFlagSubtractiveMixedLighting;
    #endif

    FragmentOutput output;
    output.GBuffer0 = half4(brdfData.albedo.rgb, PackMaterialFlags(materialFlags));  // diffuse           diffuse         diffuse         materialFlags   (sRGB rendertarget)
    output.GBuffer1 = half4(packedSpecular, occlusion);                              // metallic/specular specular        specular        occlusion
    output.GBuffer2 = half4(packedNormalWS, packedSmoothness);                       // encoded-normal    encoded-normal  encoded-normal  smoothness
    output.GBuffer3 = half4(globalIllumination, 1);                                  // GI                GI              GI              [optional: see OutputAlpha()] (lighting buffer)
    #if OUTPUT_SHADOWMASK
    output.GBuffer4 = inputData.shadowMask; // will have unity_ProbesOcclusion value if subtractive lighting is used (baked)
    #endif

    return output;
}

// This decodes the Gbuffer into a SurfaceData struct
BRDFData BRDFDataFromGbuffer(half4 gbuffer0, half4 gbuffer1, half4 gbuffer2)
{
    half3 albedo = gbuffer0.rgb;
    half3 specular = gbuffer1.rgb;
    uint materialFlags = UnpackMaterialFlags(gbuffer0.a);

    half smoothness = UnpackSmoothness(gbuffer2.a, kLightingLit);

    BRDFData brdfData = (BRDFData)0;
    half alpha = 1.0; // NOTE: alpha can get modfied, forward writes it out (_ALPHAPREMULTIPLY_ON).

    half3 brdfDiffuse;
    half3 brdfSpecular;
    half reflectivity;
    half oneMinusReflectivity;

    if ((materialFlags & kMaterialFlagSpecularSetup) != 0)
    {
        // Specular setup
        reflectivity = ReflectivitySpecular(specular);
        oneMinusReflectivity = 1.0h - reflectivity;
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
        brdfSpecular = lerp(kDieletricSpec.rgb, albedo, metallic);
    }
    InitializeBRDFDataDirect(albedo, brdfDiffuse, brdfSpecular, reflectivity, oneMinusReflectivity, smoothness, alpha, brdfData);

    return brdfData;
}

InputData InputDataFromGbufferAndWorldPosition(half4 gbuffer2, float3 wsPos)
{
    InputData inputData;

    inputData.positionWS = wsPos;
    inputData.normalWS = normalize(UnpackNormal(gbuffer2.xyz)); // normalize() is required because terrain shaders use additive blending for normals (not unit-length anymore)

    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(wsPos.xyz);

    // TODO: pass this info?
    inputData.shadowCoord     = (float4)0;
    inputData.fogCoord        = (half  )0;
    inputData.vertexLighting  = (half3 )0;

    inputData.bakedGI = (half3)0; // Note: this is not made available at lighting pass in this renderer - bakedGI contribution is included (with emission) in the value GBuffer3.rgb, that is used as a renderTarget during lighting

    return inputData;
}

#endif // BUILTIN_GBUFFERUTIL_INCLUDED
