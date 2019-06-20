#ifndef __SKYUTILS_H__
#define __SKYUTILS_H__

// Generates a world-space view direction for sky and atmospheric effects
// XRTODO-optimization: rework _PixelCoordToViewDirWS to be compatible with multiple eyes
float3 GetSkyViewDirWS(float2 positionCS, float3x3 pixelCoordToViewDirWS)
{
#if defined(USING_STEREO_MATRICES)
    // pixelCoordToViewDirWS doesn't capture stereo eye offset: translation is wiped in ComputePixelCoordToWorldSpaceViewDirectionMatrix(...)
    // For VR, we compute the view direction using stereo matrices instead
    PositionInputs posInput = GetPositionInput_Stereo(positionCS.xy + _TaaJitterStrength.xy, _ScreenSize.zw, UNITY_RAW_FAR_CLIP_VALUE, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, unity_StereoEyeIndex);
    float3 viewDirWS = GetCurrentViewPosition() - posInput.positionWS;
#else
    // Points towards the camera
    float3 viewDirWS = mul(float3(positionCS.xy + _TaaJitterStrength.xy, 1.0), pixelCoordToViewDirWS);
#endif

    return normalize(viewDirWS);
}

#endif // __SKYUTILS_H__
