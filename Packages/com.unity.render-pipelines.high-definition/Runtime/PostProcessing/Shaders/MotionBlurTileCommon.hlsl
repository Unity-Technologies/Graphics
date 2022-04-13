#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/MotionBlurCommon.hlsl"

#define USE_WAVE_INTRINSICS         defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS)

#ifdef SCATTERING
#define TILE_SIZE                   16u
#else
#define TILE_SIZE                   32u
#endif

uint PackMotionVec(float2 packedMotionVec)
{
    // Most relevant bits contain the length of the motion vector, so that we can sort directly on uint value.
    return f32tof16(packedMotionVec.y) | f32tof16(packedMotionVec.x) << 16;
}

float2 UnpackMotionVec(uint packedMotionVec)
{
    float2 outMotionVec;
    outMotionVec.x = f16tof32(packedMotionVec >> 16);
    outMotionVec.y = f16tof32(packedMotionVec);
    return outMotionVec;
}
