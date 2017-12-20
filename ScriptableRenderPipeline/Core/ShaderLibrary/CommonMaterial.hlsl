#ifndef UNITY_COMMON_MATERIAL_INCLUDED
#define UNITY_COMMON_MATERIAL_INCLUDED

//-----------------------------------------------------------------------------
// Helper functions for roughness
//-----------------------------------------------------------------------------

REAL PerceptualRoughnessToRoughness(REAL perceptualRoughness)
{
    return perceptualRoughness * perceptualRoughness;
}

REAL RoughnessToPerceptualRoughness(REAL roughness)
{
    return sqrt(roughness);
}

REAL PerceptualSmoothnessToRoughness(REAL perceptualSmoothness)
{
    return (1.0 - perceptualSmoothness) * (1.0 - perceptualSmoothness);
}

REAL PerceptualSmoothnessToPerceptualRoughness(REAL perceptualSmoothness)
{
    return (1.0 - perceptualSmoothness);
}

// Using roughness values of 0 leads to INFs and NANs. The only sensible place to use the roughness
// value of 0 is IBL, so we do not modify the perceptual roughness which is used to select the MIP map level.
// Note: making the constant too small results in aliasing.
REAL ClampRoughnessForAnalyticalLights(REAL roughness)
{
    return max(roughness, 1.0/1024.0);
}

// 'bsdfData.roughnessT' and 'bsdfData.roughnessB' are clamped, and are meant to be used with analytical lights.
// 'bsdfData.perceptualRoughness' is not clamped, and is meant to be used for IBL.
// If IBL needs the linear roughness value for some reason, it can be computed as follows:
// REAL roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
void ConvertAnisotropyToRoughness(REAL perceptualRoughness, REAL anisotropy, out REAL roughnessT, out REAL roughnessB)
{
    REAL roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

    // Use the parametrization of Sony Imageworks.
    // Ref: Revisiting Physically Based Shading at Imageworks, p. 15.
    roughnessT = roughness * (1 + anisotropy);
    roughnessB = roughness * (1 - anisotropy);

    roughnessT = ClampRoughnessForAnalyticalLights(roughnessT);
    roughnessB = ClampRoughnessForAnalyticalLights(roughnessB);
}

// ----------------------------------------------------------------------------
// Parallax mapping
// ----------------------------------------------------------------------------

// ref https://www.gamedev.net/topic/678043-how-to-blend-world-space-normals/#entry5287707
// assume compositing in world space
// Note: Using vtxNormal = REAL3(0, 0, 1) give the BlendNormalRNM formulation.
// TODO: Untested
REAL3 BlendNormalWorldspaceRNM(REAL3 n1, REAL3 n2, REAL3 vtxNormal)
{
    // Build the shortest-arc quaternion
    REAL4 q = REAL4(cross(vtxNormal, n2), dot(vtxNormal, n2) + 1.0) / sqrt(2.0 * (dot(vtxNormal, n2) + 1));

    // Rotate the normal
    return n1 * (q.w * q.w - dot(q.xyz, q.xyz)) + 2 * q.xyz * dot(q.xyz, n1) + 2 * q.w * cross(q.xyz, n1);
}

// ref http://blog.selfshadow.com/publications/blending-in-detail/
// ref https://gist.github.com/selfshadow/8048308
// Reoriented Normal Mapping
// Blending when n1 and n2 are already 'unpacked' and normalised
// assume compositing in tangent space
REAL3 BlendNormalRNM(REAL3 n1, REAL3 n2)
{
    REAL3 t = n1.xyz + REAL3(0.0, 0.0, 1.0);
    REAL3 u = n2.xyz * REAL3(-1.0, -1.0, 1.0);
    REAL3 r = (t / t.z) * dot(t, u) - u;
    return r;
}

// assume compositing in tangent space
REAL3 BlendNormal(REAL3 n1, REAL3 n2)
{
    return normalize(REAL3(n1.xy * n2.z + n2.xy * n1.z, n1.z * n2.z));
}

// Ref: http://http.developer.nvidia.com/GPUGems3/gpugems3_ch01.html / http://www.slideshare.net/icastano/cascades-demo-secrets
REAL3 ComputeTriplanarWeights(REAL3 normal)
{
    // Determine the blend weights for the 3 planar projections.
    REAL3 blendWeights = abs(normal);
    // Tighten up the blending zone
    blendWeights = (blendWeights - 0.2) * 7.0;
    blendWeights = blendWeights * blendWeights * blendWeights; // pow(blendWeights, 3);
    // Force weights to sum to 1.0 (very important!)
    blendWeights = max(blendWeights, REAL3(0.0, 0.0, 0.0));
    blendWeights /= dot(blendWeights, 1.0);

    return blendWeights;
}

// Planar/Triplanar convention for Unity in world space
void GetTriplanarCoordinate(REAL3 position, out REAL2 uvXZ, out REAL2 uvXY, out REAL2 uvZY)
{
    // Caution: This must follow the same rule as what is use for SurfaceGradient triplanar
    // TODO: Currently the normal mapping looks wrong without SURFACE_GRADIENT option because we don't handle corretly the tangent space
    uvXZ = REAL2(position.z, position.x);
    uvXY = REAL2(position.x, position.y);
    uvZY = REAL2(position.z, position.y);
}

REAL LerpWhiteTo(REAL b, REAL t)
{
    REAL oneMinusT = 1.0 - t;
    return oneMinusT + b * t;
}

REAL3 LerpWhiteTo(REAL3 b, REAL t)
{
    REAL oneMinusT = 1.0 - t;
    return REAL3(oneMinusT, oneMinusT, oneMinusT) + b * t;
}


#endif // UNITY_COMMON_MATERIAL_INCLUDED
