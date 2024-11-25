#ifndef VRS_IMAGE_INCLUDED
#define VRS_IMAGE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

RW_TEXTURE2D(uint, _ShadingRateImage);

uniform float4 _VrsScaleBias;

void ImageStore(uint shadingRateNativeValue, uint2 gid)
{
#if !defined(APPLY_Y_FLIP)
    // compute shader introduce a natural y-flip
    // hence the reverse test
    gid.y = _VrsScaleBias.w - 1 - gid.y;
#endif

    _ShadingRateImage[gid] = shadingRateNativeValue;
}

#endif // VRS_IMAGE_INCLUDED
