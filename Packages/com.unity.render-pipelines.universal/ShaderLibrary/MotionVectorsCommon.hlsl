#ifndef UNIVERSAL_PIPELINE_MOTIONVECTORSCOMMON_INCLUDED
#define UNIVERSAL_PIPELINE_MOTIONVECTORSCOMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

// This is required to avoid artifacts ("gaps" in the _MotionVectorTexture) on some platform
void ApplyMotionVectorZBias(inout float4 positionCS)
{
    #if defined(UNITY_REVERSED_Z)
    positionCS.z -= unity_MotionVectorsParams.z * positionCS.w;
    #else
    positionCS.z += unity_MotionVectorsParams.z * positionCS.w;
    #endif
}

float2 CalcNdcMotionVectorFromCsPositions(float4 posCS, float4 prevPosCS)
{
    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    if (forceNoMotion)
        return float2(0.0, 0.0);

    // Non-uniform raster needs to keep the posNDC values in float to avoid additional conversions
    // since uv remap functions use floats
    float2 posNDC = posCS.xy * rcp(posCS.w);
    float2 prevPosNDC = prevPosCS.xy * rcp(prevPosCS.w);

    float2 velocity;
    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        // Convert velocity from NDC space (-1..1) to screen UV 0..1 space since FoveatedRendering remap needs that range.
        float2 posUV = RemapFoveatedRenderingResolve(posNDC * 0.5 + 0.5);
        float2 prevPosUV = RemapFoveatedRenderingPrevFrameLinearToNonUniform(prevPosNDC * 0.5 + 0.5);

        // Calculate forward velocity
        velocity = (posUV - prevPosUV);
        #if UNITY_UV_STARTS_AT_TOP
        velocity.y = -velocity.y;
        #endif
    }
    else
    #endif
    {
        // Calculate forward velocity
        velocity = (posNDC.xy - prevPosNDC.xy);
        #if UNITY_UV_STARTS_AT_TOP
        velocity.y = -velocity.y;
        #endif

        // Convert velocity from NDC space (-1..1) to UV 0..1 space
        // Note: It doesn't mean we don't have negative values, we store negative or positive offset in UV space.
        // Note: ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)
        velocity.xy *= 0.5;
    }

    return velocity;
}

#endif
