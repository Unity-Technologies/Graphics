#include "Common.hlsl"

namespace GlobalProbe
{
    float3 DirectionIndexToDirection(uint directionIndex, uint angularResolution)
    {
        uint2 pixelPos = uint2(directionIndex % angularResolution, directionIndex / angularResolution);
        float3 direction = OctahedralSquareToSphere((float2(pixelPos) + 0.5f) * rcp(angularResolution));
        return direction;
    }

    uint DirectionToDirectionIndex(float3 direction, uint angularResolution)
    {
        const uint2 angularSquarePos = min(uint2(angularResolution - 1, angularResolution - 1), OctahedralSphereToSquare(direction) * angularResolution);
        return angularSquarePos.y * angularResolution + angularSquarePos.x;
    }
}
