#ifndef DISSOLVE_OCCLUDERS_H
#define DISSOLVE_OCCLUDERS_H

// custom-begin:
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/DissolveOccluders/DissolveOccludersData.cs.hlsl"

StructuredBuffer<DissolveOccludersCylinder> _DissolveOccludersCylinders;
int _DissolveOccludersCylindersCount;

float ComputeAlphaFromDissolveOccluders(const in PositionInputs posInput, const in float4 screenSize)
{
    float alphaMin = 1.0f;
    for (int i = 0; i < _DissolveOccludersCylindersCount; ++i)
    {
        float cylinderPositionNDCZ = _DissolveOccludersCylinders[i].positionNDCZ;
        float4 ellipseFromNDCScaleBias = _DissolveOccludersCylinders[i].ellipseFromNDCScaleBias;
        float2 alphaFromEllipseScaleBias = _DissolveOccludersCylinders[i].alphaFromEllipseScaleBias;

        if (posInput.deviceDepth < cylinderPositionNDCZ) { continue; }

        float2 offsetEllipse = posInput.positionNDC.xy * ellipseFromNDCScaleBias.xy + ellipseFromNDCScaleBias.zw;
        float lengthEllipseSquared = dot(offsetEllipse, offsetEllipse);
        if (lengthEllipseSquared > 1.0f) { continue; }
        float lengthEllipse = sqrt(lengthEllipseSquared);
        float alpha = saturate(lengthEllipse * alphaFromEllipseScaleBias.x + alphaFromEllipseScaleBias.y);
        alphaMin = min(alphaMin, alpha);

        if (alphaMin < 1e-5f) { break; }
    }
    return alphaMin;
}

void ClipFromDissolveOccluders(const in PositionInputs posInput, const in float4 screenSize)
{
    float dither = LoadBlueNoiseRGB((uint2)posInput.positionSS.xy).z;
    float alpha = ComputeAlphaFromDissolveOccluders(posInput, screenSize);
    clip(alpha - max(1e-5f, dither));
}

// custom-end
#endif