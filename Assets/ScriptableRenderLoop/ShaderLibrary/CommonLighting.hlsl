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

//-----------------------------------------------------------------------------
// various helper
//-----------------------------------------------------------------------------

// Performs the mapping of the vector 'v' located within the cube of dimensions [-r, r]^3
// to a vector within the sphere of radius 'r', where r = sqrt(r2).
// Modified version of http://mathproofs.blogspot.com/2005/07/mapping-cube-to-sphere.html
float3 MapCubeToSphere(float3 v, float r2)
{
    float3 v2 = v * v;
    float2 vr3 = v2.xy * rcp(3.0 * r2);
    return v * sqrt((float3)r2 - 0.5 * v2.yzx - 0.5 * v2.zxy + vr3.yxx * v2.zzy);
}

// Computes the squared magnitude of the vector computed by MapCubeToSphere().
float ComputeCubeToSphereMapSqMagnitude(float3 v, float r2)
{
    float3 v2 = v * v;
    // Note: dot(v, v) is often computed before this function is called,
    // so the compiler should optimize and use the precomputed result here.
    return r2 * dot(v, v) - v2.x * v2.y - v2.y * v2.z - v2.z * v2.x + v2.x * v2.y * v2.z * rcp(r2);
}

#endif // UNITY_COMMON_LIGHTING_INCLUDED
