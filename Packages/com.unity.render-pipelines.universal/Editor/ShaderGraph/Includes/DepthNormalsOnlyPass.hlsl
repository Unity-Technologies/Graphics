#ifndef SG_DEPTH_NORMALS_PASS_INCLUDED
#define SG_DEPTH_NORMALS_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/PackNormalsTexture.hlsl"

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
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    #if defined(_ALPHATEST_ON)
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    #if defined(LOD_FADE_CROSSFADE) && USE_UNITY_CROSSFADE
        LODFadeCrossFade(unpacked.positionCS);
    #endif

    // Retrieve the normal from the bump map or mesh normal
    #if defined(_NORMALMAP)
        #if _NORMAL_DROPOFF_TS
            // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
            float crossSign = (unpacked.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
            float3 bitangent = crossSign * cross(unpacked.normalWS.xyz, unpacked.tangentWS.xyz);
            float3 normalWS = TransformTangentToWorld(surfaceDescription.NormalTS, half3x3(unpacked.tangentWS.xyz, bitangent, unpacked.normalWS.xyz));
        #elif _NORMAL_DROPOFF_OS
            float3 normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
        #elif _NORMAL_DROPOFF_WS
            float3 normalWS = surfaceDescription.NormalWS;
        #endif
    #else
        float3 normalWS = unpacked.normalWS;
    #endif

    outNormalWS = half4(PackNormalWSToTexture(NormalizeNormalPerPixel(normalWS)), 0.0);

    #if defined(_WRITE_SMOOTHNESS) && !defined(_SCREENSPACEREFLECTIONS_OFF)
        outNormalWS.a = surfaceDescription.Smoothness;
    #endif

    #ifdef _WRITE_RENDERING_LAYERS
        outRenderingLayers = EncodeMeshRenderingLayer();
    #endif
}

#endif
