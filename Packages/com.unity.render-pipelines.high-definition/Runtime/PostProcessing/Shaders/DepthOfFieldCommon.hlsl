#ifndef DEPTH_OF_FIELD_COMMON
#define DEPTH_OF_FIELD_COMMON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

struct TileData
{
    uint position;
};

struct CoCTileData
{
    float minFarCoC;
    float maxFarCoC;
    float minNearCoC;
    float maxNearCoC;
};

CoCTileData LoadCoCTileData(TEXTURE2D_X(tileTexture), uint2 coords)
{
    float4 data = tileTexture[COORD_TEXTURE2D_X(coords)];
    CoCTileData tileData = {data.x, data.y, data.z, data.w};
    return tileData;
}

float4 PackCoCTileData(CoCTileData data)
{
    return float4(data.minFarCoC, data.maxFarCoC, data.minNearCoC, data.maxNearCoC);
}

uint PackKernelCoord(float2 coords)
{
    return uint(f32tof16(coords.x) | f32tof16(coords.y) << 16);
}

float2 UnpackKernelCoord(StructuredBuffer<uint> kernel, uint id)
{
    uint coord = kernel[id];
    return float2(f16tof32(coord), f16tof32(coord >> 16));
}

uint PackTileCoord(uint2 coord)
{
    return (coord.x << 16u) | coord.y;
}

uint2 UnpackTileCoord(TileData tile)
{
    uint pos = tile.position;
    return uint2((pos >> 16u) & 0xffff, pos & 0xffff);
}

float CameraDepth(TEXTURE2D_X(depthMinMaxAvg), uint2 pixelCoords)
{
    pixelCoords = FromOutputPosSSToPreupsamplePosSS(pixelCoords);

#ifndef USE_MIN_DEPTH
    return LoadCameraDepth(pixelCoords);
#else
    // When MSAA is enabled, DoF should use the min depth of the MSAA samples to avoid 1-pixel ringing around in-focus objects [case 1347291]
    // Since the transparent depth pre-pass is not using MSAA and it's not included in the _DepthMinMaxAvg texture, we manually compute the min against the standard depth pyramid
    return min(LOAD_TEXTURE2D_X_LOD(depthMinMaxAvg, pixelCoords, 0).g, LoadCameraDepth(pixelCoords));
#endif
}

#endif // DEPTH_OF_FIELD_COMMON
