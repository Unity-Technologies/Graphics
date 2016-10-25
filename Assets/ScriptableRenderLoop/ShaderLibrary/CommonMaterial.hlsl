#ifndef UNITY_COMMON_MATERIAL_INCLUDED
#define UNITY_COMMON_MATERIAL_INCLUDED

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

// ----------------------------------------------------------------------------
// Parallax mapping
// ----------------------------------------------------------------------------

float2 ParallaxOffset(float3 viewDirTS, float height)
{
    // Parallax mapping with offset limiting to reduce weird artifcat (i.e do not divide by z), also save performance
    return viewDirTS.xy * height;
}


#endif // UNITY_COMMON_MATERIAL_INCLUDED
