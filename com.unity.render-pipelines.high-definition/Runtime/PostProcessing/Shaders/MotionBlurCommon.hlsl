#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"

#define TILE_SIZE                   32u
#define WAVE_SIZE					64u

#ifdef VELOCITY_PREPPING 
RWTexture2D<float3> _VelocityAndDepth;
#else
Texture2D<float3> _VelocityAndDepth;
#endif

#ifdef GEN_PASS
RWTexture2D<float3> _TileMinMaxVel;
#else
Texture2D<float3> _TileMinMaxVel;
#endif

#if NEIGHBOURHOOD_PASS
RWTexture2D<float3> _TileMaxNeighbourhood;
#else
Texture2D<float3> _TileMaxNeighbourhood;
#endif


CBUFFER_START(MotionBlurUniformBuffer)
float4x4 _PrevVPMatrixNoTranslation;
float4 _TileTargetSize;     // .xy size, .zw 1/size
float4 _MotionBlurParams0;  // Unpacked below.
float _MotionBlurIntensity;
int    _SampleCount;
CBUFFER_END

#define _ScreenMagnitude _MotionBlurParams0.x
#define _MotionBlurMaxVelocity _MotionBlurParams0.y
#define _MinVelThreshold  _MotionBlurParams0.z
#define _MinMaxVelRatioForSlowPath _MotionBlurParams0.w


// --------------------------------------
// Encoding/Decoding
// --------------------------------------
#define PACKING 1

// We use polar coordinates. This has the advantage of storing the length separately and we'll need the length several times.
// This returns a couple { Length, Angle }
// TODO_FCC: Profile! We should be fine since this is going to be in a bw bound pass, but worth checking as atan2 costs a lot. 
float2 EncodeVelocity(float2 velocity)
{

#if PACKING
    float velLength = length(velocity);
    if (velLength < 0.0001f)
    {
        return 0.0f;
    }
    else
    {
        float theta = atan2(velocity.y, velocity.x)  * (0.5 / PI) + 0.5;
        return float2(velLength, theta);
    }
#else

    float len = length(velocity);

    if(len > 0)
    {
        return min(len, _MotionBlurMaxVelocity / _ScreenMagnitude) * normalize(velocity);
    }
    else return 0;
#endif
}

float2 ClampVelocity(float2 velocity)
{

    float len = length(velocity);
    if (len > 0)
    {
        return min(len, _MotionBlurMaxVelocity / _ScreenMagnitude) * (velocity * rcp(len));
    }
    else
    {
        return 0;
    }
}

float VelocityLengthFromEncoded(float2 velocity)
{
#if PACKING
    return  velocity.x;
#else
    return length(velocity);
#endif
}

float VelocityLengthInPixelsFromEncoded(float2 velocity)
{
#if PACKING
    return  velocity.x * _ScreenMagnitude;
#else
    return length(velocity * _ScreenSize.xy);
#endif
}

float2 DecodeVelocityFromPacked(float2 velocity)
{
#if PACKING
    float theta = velocity.y * (2 * PI) - PI;
    return  (float2(sin(theta), cos(theta)) * velocity.x).yx;
#else
    return velocity;
#endif
}


// Prep velocity so that:
//  - Compute velocity due to camera rotation
//  - Compute velocity due to camera translation
//  - Remove camera rotation velocity out of object velocity
//  - Clamp (ObjectVelocity - CameraRotation) or (CameraTranslation) then add Camera rotation vel. 
float2 ComputeVelocity(PositionInputs posInput, float2 sampledVelocity)
{
    // Once we have the velocity without camera translation -> velCameraRot = currNDC - prevNDCNoTrans;
    // Velocity buffer will now contain fullVel = (Object + Camera Rotation + Camera Translation) with Object that might be 0. 
    // We now do velToClamp = Clamp((fullVel - velCameraRot)) +  velCameraRot;

    float4 worldPos = float4(posInput.positionWS, 1.0);
    float4 prevPos = worldPos;

    float4 prevClipPos = mul(_PrevVPMatrixNoTranslation, prevPos);
    float4 curClipPos = mul(UNITY_MATRIX_UNJITTERED_VP, worldPos);

    float2 previousPositionCS = prevClipPos.xy / prevClipPos.w;
    float2 positionCS = curClipPos.xy / curClipPos.w;

    float2 velCameraRot = (positionCS - previousPositionCS);
#if UNITY_UV_STARTS_AT_TOP
    velCameraRot.y = -velCameraRot.y;
#endif

    velCameraRot.x = velCameraRot.x * _TextureWidthScaling.y;

    // Encode should be clamp here.

    float2 clampVelRot = float2(clamp(velCameraRot.x, -0.15f, 0.15), clamp(velCameraRot.y, -0.15f, 0.15f));

    return ClampVelocity((sampledVelocity - velCameraRot) * _MotionBlurIntensity) + clampVelRot;
}

// --------------------------------------
// Misc functions that work on encoded representation
// --------------------------------------

float2 MinVel(float2 v, float2 w)
{
    return VelocityLengthFromEncoded(v) < VelocityLengthFromEncoded(w) ? v : w;
}

float2 MaxVel(float2 v, float2 w)
{
    return (VelocityLengthFromEncoded(v) < VelocityLengthFromEncoded(w)) ? w : v;
}
