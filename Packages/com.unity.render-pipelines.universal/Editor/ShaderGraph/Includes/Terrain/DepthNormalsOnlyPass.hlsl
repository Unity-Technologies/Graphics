#ifndef SG_TERRAIN_DEPTH_NORMALS_PASS_INCLUDED
#define SG_TERRAIN_DEPTH_NORMALS_PASS_INCLUDED

#include "TerrainVert.hlsl"

void frag(PackedVaryings packedInput,
    out half4 color : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    float2 sampleCoords = (unpacked.texCoord0.xy / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
    float3 normalOS = SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb;
    normalOS = normalize(normalOS * 2.0 - 1.0);
    unpacked.normalWS = TransformObjectToWorldNormal(normalOS);
#endif

    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

#ifdef _TERRAIN_SG_ALPHA_CLIP
    half alpha = AlphaDiscard(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
#endif

    half3 normalWS = GetTerrainNormalWS(unpacked, surfaceDescription);
#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
    color = half4(NormalizeNormalPerPixel(normalWS), 0.0);
}

#endif
