#ifndef UNIVERSAL_GBUFFERUTIL_INCLUDED
#define UNIVERSAL_GBUFFERUTIL_INCLUDED

// inspired from [builtin_shaders]/CGIncludes/UnityGBuffer.cginc

#define kLightingInvalid     -1  // No dynamic lighting: can aliase any other material type as they are skipped using stencil
#define kLightingLitMetallic 0  // Standard lit with metallic workflow => !defined(_SPECULAR_SETUP)
#define kLightingLitSpecular 1  // Standard lit with specular workflow =>  defined(_SPECULAR_SETUP)
#define kLightingSimpleLit 2    // Simple lit shader
// clearcoat 3
// backscatter 4 
// skin 5

struct FragmentOutput
{
    half4 GBuffer0 : SV_Target0; // maps to GBufferPass.m_GBufferAttachments[0] on C# side
    half4 GBuffer1 : SV_Target1; // maps to GBufferPass.m_GBufferAttachments[1] on C# side
    half4 GBuffer2 : SV_Target2; // maps to GBufferPass.m_GBufferAttachments[2] on C# side
    half4 GBuffer3 : SV_Target3; // maps to DeferredPass.m_CameraColorAttachment on C# side
};

#define PACK_NORMALS_OCT 0  // TODO Debug OCT packing

// This will encode SurfaceData into GBuffer
FragmentOutput SurfaceDataAndMainLightingToGbuffer(SurfaceData surfaceData, InputData inputData, half3 globalIllumination, int lightingMode)
{
#if PACK_NORMALS_OCT
    half2 octNormalWS = PackNormalOctQuadEncode(inputData.normalWS); // values between [-1, +1]
    half2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
#else
    half3 packedNormalWS = inputData.normalWS * 0.5 + 0.5;   // values between [ 0,  1]
#endif

    half metallic = surfaceData.metallic;
    half packedSmoothness = surfaceData.smoothness;
    if (lightingMode == kLightingLitMetallic)
        metallic *= 14.0 / 16.0;
    if (lightingMode == kLightingLitSpecular)
        metallic = 15.0 / 16.0;
    else if (lightingMode == kLightingSimpleLit)
    {
        metallic = 16.0 / 16.0;
        packedSmoothness = (0.1 * log2(packedSmoothness) - 1); // See SimpleLitInput.hlsl, SampleSpecularSmoothness(): TODO pass in the original smoothness value
    }

    half2 metallicAlpha = half2(metallic, surfaceData.alpha);
    half packedMetallicAlpha = PackFloat2To8(metallicAlpha);

    FragmentOutput output;
    output.GBuffer0 = half4(surfaceData.albedo.rgb, surfaceData.occlusion);     // albedo          albedo          albedo          occlusion    (sRGB rendertarget)
    output.GBuffer1 = half4(surfaceData.specular.rgb, packedMetallicAlpha);     // specular        specular        specular        encoded-metallic+alpha (sRGB rendertarget)
    output.GBuffer2 = half4(packedNormalWS, packedSmoothness);                  // encoded-normal  encoded-normal  encoded-normal  packed-smoothness
    output.GBuffer3 = half4(surfaceData.emission.rgb + globalIllumination, 0);  // emission+GI     emission+GI     emission+GI     [unused]     (lighting buffer)

    return output;
}

// This decodes the Gbuffer into a SurfaceData struct
SurfaceData SurfaceDataFromGbuffer(half4 gbuffer0, half4 gbuffer1, half4 gbuffer2, out int lightingMode)
{
    SurfaceData surfaceData;

    surfaceData.albedo = gbuffer0.rgb;
    surfaceData.occlusion = gbuffer0.a;

    surfaceData.specular = gbuffer1.rgb;

    half2 metallicAlpha = Unpack8ToFloat2(gbuffer1.a);
    half smoothness = gbuffer2.a;

    if (metallicAlpha.x == 16.0 / 16.0)
    {
        lightingMode = kLightingSimpleLit;
        metallicAlpha.x = 0.0; // unused
        smoothness = exp2(10.0 * smoothness + 1);
    }
    else if (metallicAlpha.x == 15.0 / 16.0)
    {
        lightingMode = kLightingLitSpecular;
        metallicAlpha.x = 0.0; // unused
    }
    else
    {
        lightingMode = kLightingLitMetallic;
        metallicAlpha.x *= 16.0 / 14.0;
    }

    surfaceData.metallic = metallicAlpha.x;
    surfaceData.alpha = metallicAlpha.y;
    surfaceData.smoothness = smoothness;

    surfaceData.emission = (half3)0; // Note: this is not made available at lighting pass in this renderer - emission contribution is included (with GI) in the value GBuffer3.rgb, that is used as a renderTarget during lighting
    surfaceData.normalTS = (half3)0; // Note: does this normalTS member need to be in SurfaceData? It looks like an intermediate value

    return surfaceData;
}

InputData InputDataFromGbufferAndWorldPosition(half4 gbuffer2, float3 wsPos)
{
    InputData inputData;

    inputData.positionWS = wsPos;

    half3 packedNormalWS = gbuffer2.xyz;
#if PACK_NORMALS_OCT
    half2 remappedOctNormalWS = Unpack888ToFloat2(packedNormalWS);  // values between [ 0,  1]
    half2 octNormalWS = normalize(remappedOctNormalWS.xy * 2 - 1);  // values between [-1, +1]
    inputData.normalWS = UnpackNormalOctQuadEncode(octNormalWS);
#else
    inputData.normalWS = packedNormalWS * 2 - 1;  // values between [-1, +1]
#endif

    inputData.viewDirectionWS = normalize(GetCameraPositionWS() - wsPos.xyz);

    // TODO: pass this info?
    inputData.shadowCoord     = (float4)0;
    inputData.fogCoord        = (half  )0;
    inputData.vertexLighting  = (half3 )0;

    inputData.bakedGI = (half3)0; // Note: this is not made available at lighting pass in this renderer - bakedGI contribution is included (with emission) in the value GBuffer3.rgb, that is used as a renderTarget during lighting

    return inputData;
}

#endif // UNIVERSAL_GBUFFERUTIL_INCLUDED
