#ifndef UNITY_DECLARE_EXPOSURE_TEXTURE_INCLUDED
#define UNITY_DECLARE_EXPOSURE_TEXTURE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ExposureFunctions.hlsl"

#ifdef _EXPOSURE
TYPED_TEXTURE2D(float2, _ExposureMultiplierTexture);
#endif
float GetPreExposureMultiplier()
{
#ifdef _EXPOSURE
    return GetPreExposureMultiplier(_ExposureMultiplierTexture);
#else
    return 1.0;
#endif
}

float GetInvPreExposureMultiplier()
{
#ifdef _EXPOSURE
    return GetInvPreExposureMultiplier(_ExposureMultiplierTexture);
#else
    return 1.0;
#endif
}
#endif
