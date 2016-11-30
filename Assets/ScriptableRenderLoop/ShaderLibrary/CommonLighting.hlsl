#ifndef UNITY_COMMON_LIGHTING_INCLUDED
#define UNITY_COMMON_LIGHTING_INCLUDED

// These clamping function to max of floating point 16 bit are use to prevent INF in code in case of extreme value
float ClampToFloat16Max(float value)
{
    return min(value, 65504.0);
}

float2 ClampToFloat16Max(float2 value)
{
    return min(value, 65504.0);
}

float3 ClampToFloat16Max(float3 value)
{
    return min(value, 65504.0);
}

float4 ClampToFloat16Max(float4 value)
{
    return min(value, 65504.0);
}

// Ligthing convention
// Light direction is oriented backward (-Z). i.e in shader code, light direction is -lightData.forward

//-----------------------------------------------------------------------------
// Various helper
//-----------------------------------------------------------------------------

// Performs the mapping of the vector 'v' centered within the cube of dimensions [-1, 1]^3
// to a vector centered within the sphere of radius 1.
// The function expects 'v' to be within the cube (possibly unexpected results otherwise).
// Ref: http://mathproofs.blogspot.com/2005/07/mapping-cube-to-sphere.html
float3 MapCubeToSphere(float3 v)
{
    float3 v2 = v * v;
    float2 vr3 = v2.xy * rcp(3.0);
    return v * sqrt((float3)1.0 - 0.5 * v2.yzx - 0.5 * v2.zxy + vr3.yxx * v2.zzy);
}

// Computes the squared magnitude of the vector computed by MapCubeToSphere().
float ComputeCubeToSphereMapSqMagnitude(float3 v)
{
    float3 v2 = v * v;
    // Note: dot(v, v) is often computed before this function is called,
    // so the compiler should optimize and use the precomputed result here.
    return dot(v, v) - v2.x * v2.y - v2.y * v2.z - v2.z * v2.x + v2.x * v2.y * v2.z;
}

//-----------------------------------------------------------------------------
// Attenuation functions
//-----------------------------------------------------------------------------

// Ref: Moving Frostbite to PBR
float SmoothDistanceAttenuation(float squaredDistance, float invSqrAttenuationRadius)
{
    float factor = squaredDistance * invSqrAttenuationRadius;
    float smoothFactor = saturate(1.0f - factor * factor);
    return smoothFactor * smoothFactor;
}

#define PUNCTUAL_LIGHT_THRESHOLD 0.01 // 1cm (in Unity 1 is 1m)

float GetDistanceAttenuation(float3 unL, float invSqrAttenuationRadius)
{
    float sqrDist = dot(unL, unL);
    float attenuation = 1.0f / (max(PUNCTUAL_LIGHT_THRESHOLD * PUNCTUAL_LIGHT_THRESHOLD, sqrDist));
    // Non physically based hack to limit light influence to attenuationRadius.
    attenuation *= SmoothDistanceAttenuation(sqrDist, invSqrAttenuationRadius);

    return attenuation;
}

float GetAngleAttenuation(float3 L, float3 lightDir, float lightAngleScale, float lightAngleOffset)
{
    float cd = dot(lightDir, L);
    float attenuation = saturate(cd * lightAngleScale + lightAngleOffset);
    // smooth the transition
    attenuation *= attenuation;

    return attenuation;
}

// Applies SmoothDistanceAttenuation() after stretching/shrinking the attenuation sphere
// of the given radius into an ellipsoid. The process is performed along the specified axis, and
// the magnitude of the transformation is controlled by the aspect ratio (the inverse is given).
// Both the ellipsoid (e.i. 'axis') and 'unL' should be in the same coordinate system.
// 'unL' should be computed from the center of the ellipsoid.
float GetEllipsoidalDistanceAttenuation(float3 unL,  float invSqrAttenuationRadius,
                                        float3 axis, float invAspectRatio)
{
    // Project the unnormalized light vector onto the stretching/shrinking axis.
    float projL = dot(unL, axis);

    // We want 'unL' to stretch/shrink along each axis (by the inverse of the aspect ratio).
    // It is equivalent to stretching/shrinking the sphere (by the aspect ratio) into an ellipsoid.
    float diff = projL - projL * invAspectRatio;
    unL -= diff * axis;

    return SmoothDistanceAttenuation(dot(unL, unL), invSqrAttenuationRadius);
}

// Applies SmoothDistanceAttenuation() after stretching/shrinking the attenuation sphere
// of the given radius into an ellipsoid. The process is performed along the 2 specified axes, and
// the magnitude of the transformation is controlled by the aspect ratios (the inverses are given).
// Both the ellipsoid (e.i. 'axis1', 'axis2') and 'unL' should be in the same coordinate system.
// 'unL' should be computed from the center of the ellipsoid.
float GetEllipsoidalDistanceAttenuation(float3 unL,   float invSqrAttenuationRadius,
                                        float3 axis1, float invAspectRatio1,
                                        float3 axis2, float invAspectRatio2)
{
    // Project the unnormalized light vector onto the stretching/shrinking axes.
    float projL1 = dot(unL, axis1);
    float projL2 = dot(unL, axis2);

    // We want 'unL' to stretch/shrink along each axis (by the inverse of the aspect ratio).
    // It is equivalent to stretching/shrinking the sphere (by the aspect ratio) into an ellipsoid.
    float diff1 = projL1 - projL1 * invAspectRatio1;
    float diff2 = projL2 - projL2 * invAspectRatio2;
    unL -= diff1 * axis1;
    unL -= diff2 * axis2;

    return SmoothDistanceAttenuation(dot(unL, unL), invSqrAttenuationRadius);
}

// Applies SmoothDistanceAttenuation() after performing the box to sphere mapping.
// If the length of the diagonal of the box is 'd', invHalfDiag = rcp(0.5 * d).
// Both the box (e.i. 'invHalfDiag') and 'unL' should be in the same coordinate system.
// 'unL' should be computed from the center of the box.
float GetBoxToSphereMapDistanceAttenuation(float3 unL, float3 invHalfDiag)
{
    // As our algorithm only works with the [-1, 1]^2 cube,
    // we rescale the light vector to compensate.
    unL *= invHalfDiag;

    // Our algorithm expects the input vector to be within the cube.
    if (Max3(abs(unL.x), abs(unL.y), abs(unL.z)) > 1.0) return 0.0;

    // Compute the light attenuation.
    float sqDist = ComputeCubeToSphereMapSqMagnitude(unL);
    return SmoothDistanceAttenuation(sqDist, 1.0);
}

//-----------------------------------------------------------------------------
// IES Helper
//-----------------------------------------------------------------------------

float2 GetIESTextureCoordinate(float3x3 lightToWord, float3 L)
{
    // IES need to be sample in light space
    float3 dir = mul(lightToWord, -L); // Using matrix on left side do a transpose

    // convert to spherical coordinate
    float2 sphericalCoord; // .x is theta, .y is phi
    // Texture is encoded with cos(phi), scale from -1..1 to 0..1
    sphericalCoord.y = (dir.z * 0.5) + 0.5;
    float theta = atan2(dir.y, dir.x);
    sphericalCoord.x = theta * INV_TWO_PI;

    return sphericalCoord;
}

//-----------------------------------------------------------------------------
// Get local frame
//-----------------------------------------------------------------------------

// generate an orthonormalBasis from 3d unit vector.
void GetLocalFrame(float3 N, out float3 tangentX, out float3 tangentY)
{
    float3 upVector     = abs(N.z) < 0.999 ? float3(0.0, 0.0, 1.0) : float3(1.0, 0.0, 0.0);
    tangentX            = normalize(cross(upVector, N));
    tangentY            = cross(N, tangentX);
}

// TODO: test
/*
// http://orbit.dtu.dk/files/57573287/onb_frisvad_jgt2012.pdf
void GetLocalFrame(float3 N, out float3 tangentX, out float3 tangentY)
{
    if (N.z < -0.999) // Handle the singularity
    {
        tangentX = float3(0.0, -1.0, 0.0);
        tangentY = float3(-1.0, 0.0, 0.0);
        return ;
    }

    float a     = 1.0 / (1.0 + N.z);
    float b     = -N.x * N.y * a;
    tangentX    = float3(1.0f - N.x * N.x * a , b, -N.x);
    tangentY    = float3(b, 1.0f - N.y * N.y * a, -N.y);
}
*/

#endif // UNITY_COMMON_LIGHTING_INCLUDED
