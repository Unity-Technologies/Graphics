#ifndef REBLUR_UTILITIES_H_
#define REBLUR_UTILITIES_H_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/ReBlurDenoiser.cs.hlsl"

// Accumulation loop is done on 32 frames
#define MAX_ACCUM_FRAME_NUM 32.0
#define MIP_LEVEL_COUNT 4.0
#define MAX_FRAME_NUM_WITH_HISTORY_FIX 4.0

float2 RotateVector(float4 rotator, float2 v)
{
    return v.x * rotator.xz + v.y * rotator.yw;
}

float UVInScreen( float2 uv )
{
    return float( all( saturate( uv ) == uv ) );
}

// IMPORTANT:
// - works for "negative x" only
// - huge error for x < -2, but still applicable for "weight" calculations
// https://www.desmos.com/calculator/cd3mvg1gfo
#define ExpApprox( x ) \
    rcp( ( x ) * ( x ) - ( x ) + 1.0 )

// Must be used for noisy data
// https://www.desmos.com/calculator/9yoyc3is2g
// scale = 3-5 is needed to match energy in "_ComputeNonExponentialWeight" ( especially when used in a recurrent loop )
#define _ComputeExponentialWeight( x, px, py ) \
    ExpApprox( -NRD_EXP_WEIGHT_DEFAULT_SCALE * abs( ( x ) * ( px ) + ( py ) ) )

// A good choice for non noisy data
// IMPORTANT: cutoffs are needed to minimize floating point precision drifting
#define _ComputeNonExponentialWeight( x, px, py ) \
   smoothstep( 0.999, 0.001, abs( ( x ) * ( px ) + ( py ) ) )

float GetSpecularLobeHalfAngle( float linearRoughness, float percentOfVolume = 0.75 )
{
    float m = linearRoughness * linearRoughness;
    // https://seblagarde.files.wordpress.com/2015/07/course_notes_moving_frostbite_to_pbr_v32.pdf (page 72)
    // TODO: % of NDF volume - is it the trimming factor from VNDF sampling?
    return atan( m * percentOfVolume / ( 1.0 - percentOfVolume ) );
}

float GetSpecMagicCurve2( float roughness, float percentOfVolume = 0.987 )
{
    float angle = GetSpecularLobeHalfAngle( roughness, percentOfVolume );
    float almostHalfPi = GetSpecularLobeHalfAngle( 1.0, percentOfVolume );
    return saturate( angle / almostHalfPi );
}

float GetCombinedWeight(float2 geometryWeightParams, float3 Nv, float3 Xvs, float normalWeightParams, float3 N, float4 Ns, float2 roughnessWeightParams = 0)
{
    float3 a = float3( geometryWeightParams.x, normalWeightParams, roughnessWeightParams.x );
    float3 b = float3( geometryWeightParams.y, 0.0, roughnessWeightParams.y );

    float3 t;
    t.x = dot( Nv, Xvs );
    t.y = FastACos( saturate( dot( N, Ns.xyz ) ) );
    t.z = Ns.w;

    float3 w = _ComputeNonExponentialWeight( t, a, b );

    return w.x * w.y * w.z;
}

#define SPECULAR_DOMINANT_DIRECTION_G2 0
#define SPECULAR_DOMINANT_DIRECTION_G1 1
#define SPECULAR_DOMINANT_DIRECTION_DEFAULT 2

float GetSpecularDominantFactor(float NoV, float linearRoughness)
{   
    float a = 0.298475 * log( 39.4115 - 39.0029 * linearRoughness );
    float dominantFactor = pow(saturate(1.0 - NoV), 10.8649 ) * ( 1.0 - a ) + a;
    return saturate(dominantFactor);
}

float3 GetSpecularDominantDirectionWithFactor( float3 N, float3 V, float dominantFactor )
{
    float3 R = reflect( -V, N );
    float3 D = lerp( N, R, dominantFactor );

    return normalize( D );
}

float4 GetSpecularDominantDirection( float3 N, float3 V, float linearRoughness)
{
    float NoV = abs( dot( N, V ) );
    float dominantFactor = GetSpecularDominantFactor( NoV, linearRoughness);

    return float4( GetSpecularDominantDirectionWithFactor( N, V, dominantFactor ), dominantFactor );
}

float2x3 GetKernelBasis( float3 V, float3 N, float linearRoughness)
{
    float3x3 basis = GetLocalFrame(N);
    float3 T = basis[0];
    float3 B = basis[1];
    float NoV = abs(dot( N, V ));
    float f = GetSpecularDominantFactor(NoV, linearRoughness);
    float3 R = reflect( -V, N );
    float3 D = normalize( lerp( N, R, f ) );
    float NoD = abs( dot( N, D ) );
    
    if( NoD < 0.999 && linearRoughness != 1.0 )
    {
        float3 Dreflected = reflect( -D, N );
        T = normalize( cross( N, Dreflected ) );
        B = cross( Dreflected , T );

        float NoV = abs( dot( N, V ) );
        float acos01sq = saturate( 1.0 - NoV );
        float skewFactor = lerp( 1.0, linearRoughness , sqrt( acos01sq ) );
        T *= skewFactor;
    }

    return float2x3( T, B );
}

uint ReverseBits4( uint x )
{
    x = ( ( x & 0x5 ) << 1 ) | ( ( x & 0xA ) >> 1 );
    x = ( ( x & 0x3 ) << 2 ) | ( ( x & 0xC ) >> 2 );
    return x;
}

uint Bayer4x4ui( uint2 samplePos, uint frameIndex)
{
    uint2 samplePosWrap = samplePos & 3;
    uint a = 2068378560 * ( 1 - ( samplePosWrap.x >> 1 ) ) + 1500172770 * ( samplePosWrap.x >> 1 );
    uint b = ( samplePosWrap.y + ( ( samplePosWrap.x & 1 ) << 2 ) ) << 2;
    return ( ( a >> b ) + frameIndex ) & 0xF;
}

// RESULT: [0; 1)
float Bayer4x4(uint2 samplePos, uint frameIndex)
{
    uint bayer = Bayer4x4ui(samplePos, frameIndex);
    return float(bayer) / 16.0;
}

float2 GetKernelSampleCoordinates(float3 offset, float3 X, float3 T, float3 B, float4 rotator)
{
    // We can't rotate T and B instead, because T is skewed
    offset.xy = RotateVector(rotator, offset.xy);

    // Compute the world space position
    float3 wsPos = X + T * offset.x + B * offset.y;

    // Evaluate the NDC position
    float4 hClip = TransformWorldToHClip(wsPos);
    hClip.xyz /= hClip.w;

    // Convert it to screen sample space
    float2 nDC = hClip.xy * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    nDC.y = 1.0 - nDC.y;
#endif
    return nDC;
}

float GetModifiedRoughnessFromNormalVariance(float linearRoughness, float3 nonNormalizedAverageNormal)
{
    // https://blog.selfshadow.com/publications/s2013-shading-course/rad/s2013_pbs_rad_notes.pdf (page 20)
    float l = length( nonNormalizedAverageNormal );
    float kappa = saturate( 1.0 - l * l ) * rcp( l * ( 3.0 - l * l ) );
    return sqrt(max(0, linearRoughness * linearRoughness + kappa));
}

float ComputeParallax(float3 currentViewWS, float3 previousPositionWS)
{
    // Compute the previous view vector
    float3 previousViewWS = normalize(_PrevCamPosRWS - previousPositionWS);

    // Compute the cosine between both angles
    float cosa = saturate(dot(currentViewWS, previousViewWS));

    // Evaluate the tangent of the angle
    return sqrt( 1.0 - cosa * cosa ) / max(cosa, 1e-6);
}

// SPEC_ACCUM_CURVE = 1.0 (aggressiveness of history rejection depending on viewing angle: 1 = low, 0.66 = medium, 0.5 = high)
#define SPEC_ACCUM_CURVE 0.5
// SPEC_ACCUM_BASE_POWER = 0.5-1.0 (greater values lead to less aggressive accumulation)
#define SPEC_ACCUM_BASE_POWER 1.0

float GetSpecAccumSpeed(float linearRoughness, float NoV, float parallax)
{
    float acos01sq = 1.0 - NoV; // Approximation of acos^2 in normalized
    float a = pow(saturate(acos01sq), SPEC_ACCUM_CURVE);
    float b = 1.1 + linearRoughness * linearRoughness;
    float parallaxSensitivity = (b + a) / (b - a);
    float powerScale = 1.0 + parallax * parallaxSensitivity;
    float f = 1.0 - exp2(-200.0 * linearRoughness * linearRoughness);
    f *= pow(saturate(linearRoughness), SPEC_ACCUM_BASE_POWER * powerScale);
    return MAX_ACCUM_FRAME_NUM * f;
}

#define K 0.5

float HitDistanceAttenuation(float linearRoughness, float cameraDistance, float hitDistance)
{
    float f = hitDistance / (hitDistance + cameraDistance);
    return lerp(K * linearRoughness, 1.0, f);
}

void EvaluateSurfaceMotionUV(uint2 currentCoord, PositionInputs posInputs,
    out float2 historyUVUnscaled,
    out float2 historySurfaceMotionCoord,
    out float2 historySurfaceMotionUV,
    out float2 velocity)
{
    // Decode the velocity of the pixel
    velocity = float2(0.0, 0.0);
    DecodeMotionVector(LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, (float2)currentCoord.xy), velocity);

    // Compute the pixel coordinate for the history tapping
    historyUVUnscaled = posInputs.positionNDC - velocity;
    historySurfaceMotionCoord = (float2)(historyUVUnscaled * _HistorySizeAndScale.xy);
    historySurfaceMotionUV = historyUVUnscaled * _HistorySizeAndScale.zw;
}

#endif // REBLUR_UTILITIES_H_
