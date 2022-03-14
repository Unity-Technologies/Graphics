#ifndef UNITY_BUILTIN_DATA_INCLUDED
#define UNITY_BUILTIN_DATA_INCLUDED

//-----------------------------------------------------------------------------
// BuiltinData
// This structure include common data that should be present in all material
// and are independent from the BSDF parametrization.
// Note: These parameters can be store in GBuffer if the writer wants
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl" // Require for GetIndexColor auto generated
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.cs.hlsl"

//-----------------------------------------------------------------------------
// Modification Options
//-----------------------------------------------------------------------------
// Due to various transform and conversions that happen, some precision is lost along the way.
// as a result, motion vectors that are close to 0 due to cancellation of components (camera and object) end up not doing so.
// To workaround the issue, if the computed motion vector is less than MICRO_MOVEMENT_THRESHOLD (now 1% of a pixel)
// if  KILL_MICRO_MOVEMENT is == 1, we set the motion vector to 0 instead.
// An alternative could be rounding the motion vectors (e.g. round(motionVec.xy * 1eX) / 1eX) with X varying on how many digits)
// but that might lead to artifacts with mismatch between actual motion and written motion vectors on non trivial motion vector lengths.
#define KILL_MICRO_MOVEMENT
#define MICRO_MOVEMENT_THRESHOLD (0.01f * _ScreenSize.zw)

//-----------------------------------------------------------------------------
// helper macro
//-----------------------------------------------------------------------------

#define BUILTIN_DATA_SHADOW_MASK                    float4(builtinData.shadowMask0, builtinData.shadowMask1, builtinData.shadowMask2, builtinData.shadowMask3)
#ifdef UNITY_VIRTUAL_TEXTURING
    #define ZERO_BUILTIN_INITIALIZE(builtinData)    ZERO_INITIALIZE(BuiltinData, builtinData); builtinData.vtPackedFeedback = real4(1.0f, 1.0f, 1.0f, 1.0f)
#else
    #define ZERO_BUILTIN_INITIALIZE(builtinData)    ZERO_INITIALIZE(BuiltinData, builtinData)
#endif

//-----------------------------------------------------------------------------
// common Encode/Decode functions
//-----------------------------------------------------------------------------

// Guideline for motion vectors buffer.
// The object motion vectors buffer is potentially fill in several pass.
// - In gbuffer pass with extra RT (Not supported currently)
// - In forward prepass pass
// - In dedicated motion vectors pass
// So same motion vectors buffer is use for all scenario, so if deferred define a motion vectors buffer, the same is reuse for forward case.
// THis is similar to NormalBuffer

// EncodeMotionVector / DecodeMotionVector code for now, i.e it must do nothing like it is doing currently.
// Design note: We assume that motion vector/distortion fit into a single buffer (i.e not spread on several buffer)
void EncodeMotionVector(float2 motionVector, out float4 outBuffer)
{
    // RT - 16:16 float
    outBuffer = float4(motionVector.xy, 0.0, 0.0);
}


// exponent sign bit is useless since we assume magnitude of 1-epsilon max.
// This is what we used as a pseudo stencil for tagging motion vector related bits
// If both exponent sign bits (we have a 16:16 float, so 15th bit of each of the 2 components)
// [0,0] : No tags
// [1,0] : No motion
// [0,1] : Exclude from TAA
// [1,1] : Free --- NOTE: If this is implemented the [1,0] and [0,1] need to be changed as now they only check 1 bit.

#define TEST 1

void SetPixelAsNoMotionVectors(inout float4 inBuffer)
{
#if TEST
    // We need to make sure to operate in uint to do bitfield ops
    uint mvXAsUint = f32tof16(inBuffer.x);
    // Flag the 15th bit as 1
    mvXAsUint = mvXAsUint | (1 << (14));
    inBuffer.x = asfloat(f16tof32(mvXAsUint));
#endif
}

void SetPixelAsAntiGhostTAA(inout float4 inBuffer)
{
#if TEST
    // We need to make sure to operate in uint to do bitfield ops
    uint mvYAsUint = f32tof16(inBuffer.y);
    // Flag the 15th bit as 1
    mvYAsUint = mvYAsUint | (1 << 14);
    inBuffer.y = asfloat(f16tof32(mvYAsUint));
#endif
}


bool PixelSetAsNoMotionVectors(float4 inBuffer)
{
#if TEST
    uint mvXAsUint = f32tof16(inBuffer.x);
    // Check if the bit is set.
    return (mvXAsUint >> 14) & 1;
#else
    return false;
#endif
}

bool PixelSetAsNoMotionVectors(uint2 mvAsUint)
{
#if TEST
    uint mvXAsUint = mvAsUint.x;
    // Check if the bit is set.
    return (mvXAsUint >> 14) & 1;
#else
    return false;
#endif
}

bool PixelSetAsExcludedFromTAA(float4 inBuffer)
{
#if TEST
    uint mvYAsUint = f32tof16(inBuffer.y);
    // Check if the bit is set.
    return (mvYAsUint >> 14) & 1;
#else
    return false;
#endif
}

bool PixelSetAsExcludedFromTAA(uint2 mvAsUint)
{
#if TEST
    uint mvYAsUint = mvAsUint.y;
    // Check if the bit is set.
    return (mvYAsUint >> 14) & 1;
#else
    return false;
#endif
}

void DecodeMotionVector(float4 inBuffer, out float2 motionVector)
{
#if TEST

    // Because we might have messed with the 15th bit (see above), we need to set it back to 0.
    uint2 mvAsUint = (f32tof16(inBuffer.xy));

    bool isNoMotion = PixelSetAsNoMotionVectors(mvAsUint);
    // Reset the bits as we now found the decoding.
    mvAsUint.x &= ~(1 << 14);
    mvAsUint.y &= ~(1 << 14);
    inBuffer.x = asfloat(f16tof32(mvAsUint.x));
    inBuffer.y = asfloat(f16tof32(mvAsUint.y));

    motionVector = isNoMotion ? 0 : inBuffer.xy;
    //motionVector = inBuffer.xy;
#else
    motionVector = inBuffer.xy;
#endif

}

void DecodeMotionVector(float4 inBuffer, out float2 motionVector, out bool taaAntiGhost)
{
    // Because we might have messed with the 15th bit (see above), we need to set it back to 0.
    uint2 mvAsUint = asuint(inBuffer.xy);

    bool isNoMotion = PixelSetAsNoMotionVectors(mvAsUint);
    taaAntiGhost = PixelSetAsExcludedFromTAA(mvAsUint);
    // Reset the bits as we now found the decoding.
    mvAsUint.x &= ~(1 << 14);
    mvAsUint.y &= ~(1 << 14);
    inBuffer.x = asfloat(mvAsUint.x);
    inBuffer.y = asfloat(mvAsUint.y);

    motionVector = isNoMotion ? 0 : inBuffer.xy;
}

void EncodeDistortion(float2 distortion, float distortionBlur, bool isValidSource, out float4 outBuffer)
{
    // RT - 16:16:16:16 float
    // distortionBlur in alpha for a different blend mode
    outBuffer = float4(distortion, isValidSource, distortionBlur); // Caution: Blend mode depends on order of attribut here, can't change without updating blend mode.
}

void DecodeDistortion(float4 inBuffer, out float2 distortion, out float distortionBlur, out bool isValidSource)
{
    distortion = inBuffer.xy;
    distortionBlur = inBuffer.a;
    isValidSource = (inBuffer.z != 0.0);
}

void GetBuiltinDataDebug(uint paramId, BuiltinData builtinData, PositionInputs posInput, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBuiltinDataDebug(paramId, builtinData, result, needLinearToSRGB);

    switch (paramId)
    {
    case DEBUGVIEW_BUILTIN_BUILTINDATA_BAKED_DIFFUSE_LIGHTING:
        // TODO: require a remap
        // TODO: we should not gamma correct, but easier to debug for now without correct high range value
        result = builtinData.bakeDiffuseLighting; needLinearToSRGB = true;
        break;
    case DEBUGVIEW_BUILTIN_BUILTINDATA_DEPTH_OFFSET:
        result = builtinData.depthOffset.xxx * 10.0; // * 10 assuming 1 unity is 1m
        break;
    case DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION:
        result = float3((builtinData.distortion / (abs(builtinData.distortion) + 1) + 1) * 0.5, 0.5);
        break;
#ifdef DEBUG_DISPLAY
    case DEBUGVIEW_BUILTIN_BUILTINDATA_RENDERING_LAYERS:
        // Only 8 first rendering layers are currently in use (used by light layers)
        // This mode shows only those layers

        uint stripeSize = 8;

        int lightLayers = builtinData.renderingLayers & _DebugLightLayersMask;
        uint layerId = 0, layerCount = countbits(lightLayers);

        result = float3(0, 0, 0);
        for (uint i = 0; (i < 8) && (layerId < layerCount); i++)
        {
            if (lightLayers & (1 << i))
            {
                if ((posInput.positionSS.y / stripeSize) % layerCount == layerId)
                    result = _DebugRenderingLayersColors[i].xyz;
                layerId++;
            }
        }
        break;
#endif
    }
}

#endif // UNITY_BUILTIN_DATA_INCLUDED
