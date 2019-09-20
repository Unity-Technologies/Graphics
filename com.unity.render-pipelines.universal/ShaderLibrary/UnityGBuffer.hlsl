#ifndef UNIVERSAL_GBUFFERUTIL_INCLUDED
#define UNIVERSAL_GBUFFERUTIL_INCLUDED

// inspired from [builtin_shaders]/CGIncludes/UnityGBuffer.cginc

struct FragmentOutput
{
    half4 GBuffer0 : SV_Target0;
    half4 GBuffer1 : SV_Target1;
    half4 GBuffer2 : SV_Target2;
    half4 GBuffer3 : SV_Target3;
    half4 GBuffer4 : SV_Target4;
};

// This will encode SurfaceData into GBuffer
FragmentOutput SurfaceDataToGbuffer(SurfaceData surfaceData, InputData inputData)
{
    half3 remappedNormalWS = inputData.normalWS.xyz * 0.5f + 0.5f;
    FragmentOutput output;
    output.GBuffer0 = half4(surfaceData.albedo.rgb, surfaceData.occlusion);     // albedo    albedo    albedo    occlusion    (sRGB rendertarget)
    output.GBuffer1 = half4(surfaceData.specular.rgb, surfaceData.smoothness);  // specular  specular  specular  smoothness   (sRGB rendertarget)
    //output.GBuffer2 = half4(inputData.normalWS.xyz, surfaceData.alpha);       // normal    normal    normal    alpha
    output.GBuffer2 = half4(remappedNormalWS, surfaceData.alpha);               // normal    normal    normal    alpha
    output.GBuffer3 = half4(surfaceData.emission.rgb, surfaceData.metallic);    // emission  emission  emission  metallic
    output.GBuffer4 = half4(inputData.bakedGI, 1.0);                            // bakedGI   bakedGI   bakedGI   [unused]

    return output;
}

// This decodes the Gbuffer into a SurfaceData struct
SurfaceData SurfaceDataFromGbuffer(half4 gbuffer0, half4 gbuffer1, half4 gbuffer2, half4 gbuffer3 /*, DeferredData deferredData*/)
{
    SurfaceData surfaceData;

    surfaceData.albedo = gbuffer0.rgb;
    surfaceData.occlusion = gbuffer0.a;

    surfaceData.specular = gbuffer1.rgb;
    surfaceData.smoothness = gbuffer1.a;

    surfaceData.normalTS = (half3)0; // Note: does this normalTS member need to be in SurfaceData? It looks like an intermediate value

    surfaceData.alpha = gbuffer2.a;

    surfaceData.emission = gbuffer3.rgb;
    surfaceData.metallic = gbuffer3.a;

    return surfaceData;
}

InputData InputDataFromGbufferAndWorldPosition(half4 gbuffer2, half4 gbuffer4, float3 wsPos)
{
    InputData inputData;

    inputData.positionWS = wsPos;

    half3 remappedNormal = normalize((float3)gbuffer2.rgb * 2 - 1);
    inputData.normalWS = remappedNormal;

    inputData.viewDirectionWS = GetCameraPositionWS() - wsPos.xyz;

    // TODO: find how to pass this info?
    inputData.shadowCoord     = (float4)0;
    inputData.fogCoord        = (half  )0;
    inputData.vertexLighting  = (half3 )0;

    // TODO: pass bakedGI info without using GBuffer slot?
    inputData.bakedGI = gbuffer4.rgb;

    return inputData;
}

#endif // UNIVERSAL_GBUFFERUTIL_INCLUDED
