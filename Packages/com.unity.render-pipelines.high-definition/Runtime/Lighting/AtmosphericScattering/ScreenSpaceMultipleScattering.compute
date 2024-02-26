#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

#pragma kernel ScreenSpaceMultipleScattering

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

// #pragma enable_d3d11_debug_symbols

TEXTURE2D_X(_OpticalFogTransmittance);
RW_TEXTURE2D_X(float4, _Destination);
float _MultipleScatteringIntensity;
uint _OpticalFogTextureChannel;

[numthreads(8, 8, 1)]
void ScreenSpaceMultipleScattering(uint3 dispatchThreadId : SV_DispatchThreadID)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(dispatchThreadId.z);

    uint2 pixelPosition = dispatchThreadId.xy;
    float2 centerPixel = pixelPosition + 0.5;
    float2 uv = centerPixel * _ScreenSize.zw;

    float2 opticalFogTransmittanceData = SAMPLE_TEXTURE2D_X_LOD(_OpticalFogTransmittance, s_linear_clamp_sampler, uv * _RTHandleScale.xy, 0).xy;
    // When lens flare are enabled, the SSMS opacity is stored in the green channel, because we don't want cloud opacity in this pass.
    float opticalFogTransmittance = 1 - saturate(opticalFogTransmittanceData[_OpticalFogTextureChannel]);
    _Destination[COORD_TEXTURE2D_X(pixelPosition)] = SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_trilinear_clamp_sampler, uv * _RTHandleScaleHistory.xy, opticalFogTransmittance * _MultipleScatteringIntensity);
}
