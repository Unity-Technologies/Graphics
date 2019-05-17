#ifndef DISSOLVE_OCCLUDERS_H
#define DISSOLVE_OCCLUDERS_H

// custom-begin:
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/DissolveOccluders/DissolveOccludersData.cs.hlsl"

StructuredBuffer<DissolveOccludersCylinder> _DissolveOccludersCylinders;
int _DissolveOccludersCylindersCount;
float2 _DissolveOccludersAspectScale;

void ClipFromDissolveOccluders(const in PositionInputs posInput, const in float4 screenSize)
{
    float dither = LoadBlueNoiseRGB((uint2)posInput.positionSS.xy).z;
    float alphaMin = 1.0f;
    for (int i = 0; i < _DissolveOccludersCylindersCount; ++i)
    {
        float3 cylinderPositionNDC = _DissolveOccludersCylinders[i].positionNDC;
        float2 cylinderRadiusScaleBiasNDC = _DissolveOccludersCylinders[i].radiusScaleBiasNDC;

        if (posInput.deviceDepth < cylinderPositionNDC.z) { continue; }

        float2 offsetNDC = (posInput.positionNDC.xy - cylinderPositionNDC.xy) * _DissolveOccludersAspectScale;
        float distanceNDC = length(offsetNDC);

        float alpha = distanceNDC * cylinderRadiusScaleBiasNDC.x + cylinderRadiusScaleBiasNDC.y;
        alpha = saturate(alpha);
        alphaMin = min(alphaMin, alpha);
        if (alphaMin < 1e-5f) { break; }
    }
    clip(alphaMin - max(1e-5f, dither));
}

// custom-end
#endif