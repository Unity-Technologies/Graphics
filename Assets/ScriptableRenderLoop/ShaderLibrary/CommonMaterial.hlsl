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

float2 ParallaxOffset(float3 viewDirTS, float height)
{
    // Parallax mapping with offset limiting to reduce weird artifcat (i.e do not divide by z), also save performance
    return viewDirTS.xy * height;
}

// ref http://blog.selfshadow.com/publications/blending-in-detail/
// ref https://gist.github.com/selfshadow/8048308
// Reoriented Normal Mapping
// Blending when n1 and n2 are already 'unpacked' and normalised
float3 BlendNormalRNM(float3 n1, float3 n2)
{
    float3 t = n1.xyz + float3(0.0, 0.0, 1.0);
    float3 u = n2.xyz * float3(-1.0, -1.0, 1.0);
    float3 r = (t / t.z) * dot(t, u) - u;
    return r;
}

float3 BlendNormal(float3 n1, float3 n2)
{
    return normalize(float3(n1.xy * n2.z + n2.xy * n1.z, n1.z * n2.z));
}

// Ref: http://http.developer.nvidia.com/GPUGems3/gpugems3_ch01.html
float3 ComputeTriplanarWeights(float3 normal)
{ 
    // Determine the blend weights for the 3 planar projections.  
    // N_orig is the vertex-interpolated normal vector.  
    float3 blendWeights = abs(normal);
    // Tighten up the blending zone
    blendWeights = (blendWeights - 0.2) * 7.0;
    // Force weights to sum to 1.0 (very important!)  
    blendWeights = max(blendWeights, float3(0.0, 0.0, 0.0));
    blendWeights /= dot(blendWeights, 1.0);

    return blendWeights;
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

#endif // UNITY_COMMON_MATERIAL_INCLUDED
