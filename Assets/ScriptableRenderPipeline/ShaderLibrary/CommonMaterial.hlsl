#ifndef UNITY_COMMON_MATERIAL_INCLUDED
#define UNITY_COMMON_MATERIAL_INCLUDED

//-----------------------------------------------------------------------------
// Helper function for anisotropy
//-----------------------------------------------------------------------------

// Ref: http://blog.selfshadow.com/publications/s2012-shading-course/burley/s2012_pbs_disney_brdf_notes_v3.pdf (in addenda)
// Convert anisotropic ratio (0->no isotropic; 1->full anisotropy in tangent direction) to roughness
void ConvertAnisotropyToRoughness(float roughness, float anisotropy, out float roughnessT, out float roughnessB)
{
    // (0 <= anisotropy <= 1), therefore (0 <= anisoAspect <= 1)
    // The 0.9 factor limits the aspect ratio to 10:1.
    float anisoAspect = sqrt(1.0 - 0.9 * anisotropy);

    roughnessT = roughness / anisoAspect; // Distort along tangent (rougher)
    roughnessB = roughness * anisoAspect; // Straighten along bitangent (smoother)
}

// Ref: Donald Revie - Implementing Fur Using Deferred Shading (GPU Pro 2)
// The grain direction (e.g. hair or brush direction) is assumed to be orthogonal to the normal.
// The returned normal is NOT normalized.
float3 ComputeGrainNormal(float3 grainDir, float3 V)
{
    float3 B = cross(-V, grainDir);
    return cross(B, grainDir);
}

// Fake anisotropic by distorting the normal.
// The grain direction (e.g. hair or brush direction) is assumed to be orthogonal to N.
// Anisotropic ratio (0->no isotropic; 1->full anisotropy in tangent direction)
float3 GetAnisotropicModifiedNormal(float3 grainDir, float3 N, float3 V, float anisotropy)
{
    float3 grainNormal = ComputeGrainNormal(grainDir, V);
    // TODO: test whether normalizing 'grainNormal' is worth it.
    return normalize(lerp(N, grainNormal, anisotropy));
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

// ref https://www.gamedev.net/topic/678043-how-to-blend-world-space-normals/#entry5287707
// assume compositing in world space
// Note: Using vtxNormal = float3(0, 0, 1) give the BlendNormalRNM formulation.
// TODO: Untested
float3 BlendNormalWorldspaceRNM(float3 n1, float3 n2, float3 vtxNormal)
{
    // Build the shortest-arc quaternion
    float4 q = float4(cross(vtxNormal, n2), dot(vtxNormal, n2) + 1.0) / sqrt(2.0 * (dot(vtxNormal, n2) + 1));

    // Rotate the normal
    return n1 * (q.w * q.w - dot(q.xyz, q.xyz)) + 2 * q.xyz * dot(q.xyz, n1) + 2 * q.w * cross(q.xyz, n1);
}

// ref http://blog.selfshadow.com/publications/blending-in-detail/
// ref https://gist.github.com/selfshadow/8048308
// Reoriented Normal Mapping
// Blending when n1 and n2 are already 'unpacked' and normalised
// assume compositing in tangent space
float3 BlendNormalRNM(float3 n1, float3 n2)
{
    float3 t = n1.xyz + float3(0.0, 0.0, 1.0);
    float3 u = n2.xyz * float3(-1.0, -1.0, 1.0);
    float3 r = (t / t.z) * dot(t, u) - u;
    return r;
}

// assume compositing in tangent space
float3 BlendNormal(float3 n1, float3 n2)
{
    return normalize(float3(n1.xy * n2.z + n2.xy * n1.z, n1.z * n2.z));
}

// Ref: http://http.developer.nvidia.com/GPUGems3/gpugems3_ch01.html / http://www.slideshare.net/icastano/cascades-demo-secrets
float3 ComputeTriplanarWeights(float3 normal)
{
    // Determine the blend weights for the 3 planar projections.
    float3 blendWeights = abs(normal);
    // Tighten up the blending zone
    blendWeights = (blendWeights - 0.2) * 7.0;
    blendWeights = blendWeights * blendWeights * blendWeights; // pow(blendWeights, 3);
    // Force weights to sum to 1.0 (very important!)
    blendWeights = max(blendWeights, float3(0.0, 0.0, 0.0));
    blendWeights /= dot(blendWeights, 1.0);

    return blendWeights;
}

// Planar/Triplanar convention for Unity in world space
void GetTriplanarCoordinate(float3 position, out float2 uvXZ, out float2 uvXY, out float2 uvZY)
{
    // Caution: This must follow the same rule as what is use for SurfaceGradient triplanar
    // TODO: Currently the normal mapping looks wrong without SURFACE_GRADIENT option because we don't handle corretly the tangent space
    uvXZ = float2(position.z, position.x);
    uvXY = float2(position.x, position.y);
    uvZY = float2(position.z, position.y);
}

float LerpWhiteTo(float b, float t)
{
    float oneMinusT = 1.0 - t;
    return oneMinusT + b * t;
}

float3 LerpWhiteTo(float3 b, float t)
{
    float oneMinusT = 1.0 - t;
    return float3(oneMinusT, oneMinusT, oneMinusT) + b * t;
}

// ----------------------------------------------------------------------------
// SSS/Transmittance
// ----------------------------------------------------------------------------

// Computes the fraction of light passing through the object.
// Evaluate Int{0, inf}{2 * Pi * r * R(sqrt(r^2 + d^2))}, where R is the diffusion profile.
// Ref: Approximate Reflectance Profiles for Efficient Subsurface Scattering by Pixar (BSSRDF only).
float3 ComputeTransmittance(float3 S, float3 volumeAlbedo, float thickness, float radiusScale)
{
    // Thickness and SSS radius are decoupled for artists.
    // In theory, we should modify the thickness by the inverse of the radius scale of the profile.
    // thickness /= radiusScale;

    float3 expOneThird = exp(((-1.0 / 3.0) * thickness) * S);

    return 0.25 * (expOneThird + 3 * expOneThird * expOneThird * expOneThird) * volumeAlbedo;
}

// MACRO from Legacy Untiy
// Transforms 2D UV by scale/bias property
#define TRANSFORM_TEX(tex, name) ((tex.xy) * name##_ST.xy + name##_ST.zw)

#define GET_TEXELSIZE_NAME(name) (name##_TexelSize)

#endif // UNITY_COMMON_MATERIAL_INCLUDED
