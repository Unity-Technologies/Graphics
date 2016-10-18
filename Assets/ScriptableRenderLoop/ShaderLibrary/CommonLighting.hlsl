#ifndef UNITY_COMMON_LIGHTING_INCLUDED
#define UNITY_COMMON_LIGHTING_INCLUDED

// Ligthing convention
// Light direction is oriented backward (-Z). i.e in shader code, light direction is - lightData.forward

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
// Helper function for anisotropy
//-----------------------------------------------------------------------------

// Ref: http://blog.selfshadow.com/publications/s2012-shading-course/burley/s2012_pbs_disney_brdf_notes_v3.pdf (in addenda)
// Convert anisotropic ratio (0->no isotropic; 1->full anisotropy in tangent direction) to roughness
void ConvertAnisotropyToRoughness(float roughness, float anisotropy, out float roughnessT, out float roughnessB)
{
    float anisoAspect = sqrt(1.0 - 0.9 * anisotropy);
    roughnessT = roughness * anisoAspect;
    roughnessB = roughness / anisoAspect;
}

// Fake anisotropic by distorting the normal as suggested by:
// Ref: Donald Revie - Implementing Fur Using Deferred Shading (GPU Pro 2)
// anisotropic ratio (0->no isotropic; 1->full anisotropy in tangent direction)
float3 GetAnisotropicModifiedNormal(float3 N, float3 T, float3 V, float anisotropy)
{
    float3 anisoT = cross(-V, T);
    float3 anisoN = cross(anisoT, T);

    return normalize(lerp(N, anisoN, anisotropy));
}

//-----------------------------------------------------------------------------
// Helper function for perceptual roughness
//-----------------------------------------------------------------------------

float PerceptualRoughnessToRoughness(float perceptualRoughness)
{
    return perceptualRoughness * perceptualRoughness;
}

float RoughnessToPerceptualRoughness(float roughness)
{
    return sqrt(roughness);
}

float PerceptualSmoothnessToRoughness(float perceptualSmoothness)
{
    return (1 - perceptualSmoothness) * (1 - perceptualSmoothness);
}

float PerceptualSmoothnessToPerceptualRoughness(float perceptualSmoothness)
{
    return (1 - perceptualSmoothness);
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
