#ifndef __SKYUTILS_H__
#define __SKYUTILS_H__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

float4x4 _PixelCoordToViewDirWS;

#if defined(USING_STEREO_MATRICES)
    #define _PixelCoordToViewDirWS   _XRViewConstants[unity_StereoEyeIndex].pixelCoordToViewDirWS
#endif

// Generates a world-space view direction for sky and atmospheric effects
float3 GetSkyViewDirWS(float2 positionCS)
{
    // XRTODO: double-wide cleanup
#if defined(UNITY_SINGLE_PASS_STEREO)
    PositionInputs posInput = GetPositionInput_Stereo(positionCS.xy + _TaaJitterStrength.xy, _ScreenSize.zw, UNITY_RAW_FAR_CLIP_VALUE, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, unity_StereoEyeIndex);
    return normalize(GetCurrentViewPosition() - posInput.positionWS);
#endif

    float4 viewDirWS = mul(float4(positionCS.xy + _TaaJitterStrength.xy, 1.0f, 1.0f), _PixelCoordToViewDirWS);
    return normalize(viewDirWS.xyz);
}

#endif // __SKYUTILS_H__
