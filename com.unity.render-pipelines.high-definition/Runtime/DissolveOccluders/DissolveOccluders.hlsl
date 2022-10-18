#ifndef DISSOLVE_OCCLUDERS_H
#define DISSOLVE_OCCLUDERS_H

// custom-begin:
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/DissolveOccluders/DissolveOccludersData.cs.hlsl"

#if defined(HIGH_DEFINITION_EXTENSIONS_ENABLED)
#include "Packages/com.unity.render-pipelines.high-definition-extensions/Runtime/DissolveOccluders/DissolveOccludersCustom.hlsl"
#endif

StructuredBuffer<DissolveOccludersCylinder> _DissolveOccludersCylinders;
int _DissolveOccludersCylindersCount;
float2 _DissolveOccludersFadeHeightScaleBias;

float ComputeAlphaFromDissolveOccluders(const in PositionInputs posInput, const in float4 screenSize)
{
    float alphaMin = 1.0f;
    for (int i = 0; i < _DissolveOccludersCylindersCount; ++i)
    {
        if ((_DissolveOccludersFadeHeightScaleBias.x + _DissolveOccludersFadeHeightScaleBias.y) < 1e-5f)
        {
            alphaMin = 0.0f;
            break;
        }

        float cylinderPositionWSY = _DissolveOccludersCylinders[i].positionWSY;
        float height = GetAbsolutePositionWS(posInput.positionWS).y - cylinderPositionWSY;
        float alpha = sqrt(saturate(height * _DissolveOccludersFadeHeightScaleBias.x + _DissolveOccludersFadeHeightScaleBias.y));
        alphaMin = min(alphaMin, alpha);
        if (alphaMin < 1e-5f) { break; }
    }
    return max(alphaMin, _DissolveOnOcclusionOpacity);
}

void ClipFromDissolveOccludersBase(const in PositionInputs posInput, const in float4 screenSize)
{
    float dither = LoadBlueNoiseRGB((uint2)posInput.positionSS.xy).z;
    float alpha = ComputeAlphaFromDissolveOccluders(posInput, screenSize);
    clip(alpha - max(1e-5f, dither));
}

void ClipFromDissolveOccluders(const in PositionInputs posInput, const in float4 screenSize)
{
#if defined(CLIP_FROM_DISSOLVE_OCCLUDERS_CUSTOM)
    if (!TryClipFromDissolveOccludersCustom(posInput, screenSize))
#endif
    {
        ClipFromDissolveOccludersBase(posInput, screenSize);
    } 
}

#define _EVALUATE_DISSOLVE_ON_OCCLUSION (defined(_ENABLE_DISSOLVE_ON_OCCLUSION) && (SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_DEPTH_ONLY || SHADERPASS == SHADERPASS_MOTION_VECTORS || SHADERPASS == SHADERPASS_FORWARD || SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_DISTORTION))

// custom-end
#endif
