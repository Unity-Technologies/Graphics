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
// Helper functions
//-----------------------------------------------------------------------------

// Performs the mapping of the vector 'v' centered within the axis-aligned cube
// of dimensions [-1, 1]^3 to a vector centered within the unit sphere.
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

// texelArea = 4.0 / (resolution * resolution).
// Ref: http://bpeers.com/blog/?itemid=1017
float ComputeCubemapTexelSolidAngle(float3 L, float texelArea)
{
    // Stretch 'L' by (1/d) so that it points at a side of a [-1, 1]^2 cube.
    float d = Max3(abs(L.x), abs(L.y), abs(L.z));
    // Since 'L' is a unit vector, we can directly compute its
    // new (inverse) length without dividing 'L' by 'd' first.
    float invDist = d;

    // dw = dA * cosTheta / (dist * dist), cosTheta = 1.0 / dist,
    // where 'dA' is the area of the cube map texel.
    return texelArea * invDist * invDist * invDist;
}

//-----------------------------------------------------------------------------
// Attenuation functions
//-----------------------------------------------------------------------------

// Ref: Moving Frostbite to PBR
float SmoothDistanceAttenuation(float squaredDistance, float invSqrAttenuationRadius)
{
    float factor = squaredDistance * invSqrAttenuationRadius;
    float smoothFactor = saturate(1.0 - factor * factor);
    return smoothFactor * smoothFactor;
}

#define PUNCTUAL_LIGHT_THRESHOLD 0.01 // 1cm (in Unity 1 is 1m)

float GetDistanceAttenuation(float3 unL, float invSqrAttenuationRadius)
{
    float sqrDist = dot(unL, unL);
    float attenuation = 1.0 / (max(PUNCTUAL_LIGHT_THRESHOLD * PUNCTUAL_LIGHT_THRESHOLD, sqrDist));
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

// Applies SmoothDistanceAttenuation() after transforming the attenuation ellipsoid into a sphere.
// If r = rsqrt(invSqRadius), then the ellipsoid is defined s.t. r1 = r / invAspectRatio, r2 = r3 = r.
// The transformation is performed along the major axis of the ellipsoid (corresponding to 'r1').
// Both the ellipsoid (e.i. 'axis') and 'unL' should be in the same coordinate system.
// 'unL' should be computed from the center of the ellipsoid.
float GetEllipsoidalDistanceAttenuation(float3 unL,  float invSqRadius,
                                        float3 axis, float invAspectRatio)
{
    // Project the unnormalized light vector onto the axis.
    float projL = dot(unL, axis);

    // Transform the light vector instead of transforming the ellipsoid.
    float diff = projL - projL * invAspectRatio;
    unL -= diff * axis;

    float sqDist = dot(unL, unL);
    return SmoothDistanceAttenuation(sqDist, invSqRadius);
}

// Applies SmoothDistanceAttenuation() using the axis-aligned ellipsoid of the given dimensions.
// Both the ellipsoid and 'unL' should be in the same coordinate system.
// 'unL' should be computed from the center of the ellipsoid.
float GetEllipsoidalDistanceAttenuation(float3 unL, float3 invHalfDim)
{
    // Transform the light vector so that we can work with
    // with the ellipsoid as if it was a unit sphere.
    unL *= invHalfDim;

    float sqDist = dot(unL, unL);
    return SmoothDistanceAttenuation(sqDist, 1.0);
}

// Applies SmoothDistanceAttenuation() after mapping the axis-aligned box to a sphere.
// If the diagonal of the box is 'd', invHalfDim = rcp(0.5 * d).
// Both the box and 'unL' should be in the same coordinate system.
// 'unL' should be computed from the center of the box.
float GetBoxDistanceAttenuation(float3 unL, float3 invHalfDim)
{
    // Transform the light vector so that we can work with
    // with the box as if it was a [-1, 1]^2 cube.
    unL *= invHalfDim;

    // Our algorithm expects the input vector to be within the cube.
    if (Max3(abs(unL.x), abs(unL.y), abs(unL.z)) > 1.0) return 0.0;

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
// Lighting functions
//-----------------------------------------------------------------------------

// Ref: Horizon Occlusion for Normal Mapped Reflections: http://marmosetco.tumblr.com/post/81245981087
float GetHorizonOcclusion(float3 V, float3 normalWS, float3 vertexNormal, float horizonFade)
{
    float3 R = reflect(-V, normalWS);
    float specularOcclusion = saturate(1.0 + horizonFade * dot(R, vertexNormal));
    // smooth it
    return specularOcclusion * specularOcclusion;
}

// Ref: Moving Frostbite to PBR - Gotanda siggraph 2011
float GetSpecularOcclusion(float NdotV, float ambientOcclusion, float roughness)
{
	return saturate(PositivePow(NdotV + ambientOcclusion, exp2(-16.0 * roughness - 1.0)) - 1.0 + ambientOcclusion);
}

//-----------------------------------------------------------------------------
// Helper functions
//-----------------------------------------------------------------------------

// 'NdotV' can become negative for visible pixels due to the perspective projection, normal mapping and decals.
// This can produce visible artifacts under specular lighting, both direct (overly dark/bright pixels) and indirect (incorrect cubemap direction).
// One way of avoiding these artifacts is to limit the value of 'NdotV' to a small positive number,
// and calculate the reflection vector for the cubemap fetch using a normal shifted into view.
float3 GetViewShiftedNormal(float3 N, float3 V, float NdotV, float minNdotV)
{
    if (NdotV < minNdotV)
    {
        // We do not renormalize the normal to save a few clock cycles.
        // The magnitude difference is typically negligible, and the normal is only used to compute
        // the reflection vector for the IBL cube map fetch (which does not depend on the magnitude).
        N += (-NdotV + minNdotV) * V;
    }

    return N;
}

// Generates an orthonormal basis from a unit vector.
float3x3 GetLocalFrame(float3 localZ)
{
    float3 upVector = abs(localZ.z) < 0.999 ? float3(0.0, 0.0, 1.0) : float3(1.0, 0.0, 0.0);
    float3 localX   = normalize(cross(upVector, localZ));
    float3 localY   = cross(localZ, localX);

    return float3x3(localX, localY, localZ);
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
    tangentX    = float3(1.0 - N.x * N.x * a , b, -N.x);
    tangentY    = float3(b, 1.0 - N.y * N.y * a, -N.y);
}
*/

#endif // UNITY_COMMON_LIGHTING_INCLUDED
