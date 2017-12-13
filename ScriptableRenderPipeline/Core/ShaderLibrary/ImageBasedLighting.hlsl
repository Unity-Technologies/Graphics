#ifndef UNITY_IMAGE_BASED_LIGHTING_INCLUDED
#define UNITY_IMAGE_BASED_LIGHTING_INCLUDED

#include "CommonLighting.hlsl"
#include "CommonMaterial.hlsl"
#include "BSDF.hlsl"
#include "Sampling/Sampling.hlsl"

#ifndef UNITY_SPECCUBE_LOD_STEPS
    #define UNITY_SPECCUBE_LOD_STEPS 6
#endif

//-----------------------------------------------------------------------------
// Util image based lighting
//-----------------------------------------------------------------------------

// The *approximated* version of the non-linear remapping. It works by
// approximating the cone of the specular lobe, and then computing the MIP map level
// which (approximately) covers the footprint of the lobe with a single texel.
// Improves the perceptual roughness distribution.
float PerceptualRoughnessToMipmapLevel(float perceptualRoughness, uint mipMapCount)
{
    perceptualRoughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);

    return perceptualRoughness * mipMapCount;
}

float PerceptualRoughnessToMipmapLevel(float perceptualRoughness)
{
    return PerceptualRoughnessToMipmapLevel(perceptualRoughness, UNITY_SPECCUBE_LOD_STEPS);
}

// The *accurate* version of the non-linear remapping. It works by
// approximating the cone of the specular lobe, and then computing the MIP map level
// which (approximately) covers the footprint of the lobe with a single texel.
// Improves the perceptual roughness distribution and adds reflection (contact) hardening.
// TODO: optimize!
float PerceptualRoughnessToMipmapLevel(float perceptualRoughness, float NdotR)
{
    float m = PerceptualRoughnessToRoughness(perceptualRoughness);

    // Remap to spec power. See eq. 21 in --> https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf
    float n = (2.0 / max(FLT_EPS, m * m)) - 2.0;

    // Remap from n_dot_h formulation to n_dot_r. See section "Pre-convolved Cube Maps vs Path Tracers" --> https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html
    n /= (4.0 * max(NdotR, FLT_EPS));

    // remap back to square root of real roughness (0.25 include both the sqrt root of the conversion and sqrt for going from roughness to perceptualRoughness)
    perceptualRoughness = pow(2.0 / (n + 2.0), 0.25);

    return perceptualRoughness * UNITY_SPECCUBE_LOD_STEPS;
}

// The inverse of the *approximated* version of perceptualRoughnessToMipmapLevel().
float MipmapLevelToPerceptualRoughness(float mipmapLevel)
{
    float perceptualRoughness = saturate(mipmapLevel / UNITY_SPECCUBE_LOD_STEPS);

    return saturate(1.7 / 1.4 - sqrt(2.89 / 1.96 - (2.8 / 1.96) * perceptualRoughness));
}

//-----------------------------------------------------------------------------
// Anisotropic image based lighting
//-----------------------------------------------------------------------------

// Ref: Donald Revie - Implementing Fur Using Deferred Shading (GPU Pro 2)
// The grain direction (e.g. hair or brush direction) is assumed to be orthogonal to the normal.
// The returned normal is NOT normalized.
float3 ComputeGrainNormal(float3 grainDir, float3 V)
{
    float3 B = cross(grainDir, V);
    return cross(B, grainDir);
}

// Fake anisotropy by distorting the normal (non-negative anisotropy values only).
// The grain direction (e.g. hair or brush direction) is assumed to be orthogonal to N.
// Anisotropic ratio (0->no isotropic; 1->full anisotropy in tangent direction)
float3 GetAnisotropicModifiedNormal(float3 grainDir, float3 N, float3 V, float anisotropy)
{
    float3 grainNormal = ComputeGrainNormal(grainDir, V);
    return normalize(lerp(N, grainNormal, anisotropy));
}

// Ref: "Moving Frostbite to PBR", p. 69.
float3 GetSpecularDominantDir(float3 N, float3 R, float roughness, float NdotV)
{
    float a = 1.0 - roughness;
    float s = sqrt(a);

#ifdef USE_FB_DSD
    // This is the original formulation.
    float lerpFactor = (s + roughness) * a;
#else
    // TODO: tweak this further to achieve a closer match to the reference.
    float lerpFactor = (s + roughness) * saturate(a * a + lerp(0.0, a, NdotV * NdotV));
#endif

    // The result is not normalized as we fetch in a cubemap
    return lerp(N, R, lerpFactor);
}

// To simulate the streching of highlight at grazing angle for IBL we shrink the roughness
// which allow to fake an anisotropic specular lobe.
// Ref: http://www.frostbite.com/2015/08/stochastic-screen-space-reflections/ - slide 84
float AnisotropicStrechAtGrazingAngle(float roughness, float perceptualRoughness, float NdotV)
{
    return roughness * lerp(saturate(NdotV * 2.0), 1.0, perceptualRoughness);
}

// ----------------------------------------------------------------------------
// Importance sampling BSDF functions
// ----------------------------------------------------------------------------

void SampleGGXDir(float2   u,
                  float3   V,
                  float3x3 localToWorld,
                  float    roughness,
              out float3   L,
              out float    NdotL,
              out float    NdotH,
              out float    VdotH,
                  bool     VeqN = false)
{
    // GGX NDF sampling
    float cosTheta = sqrt((1.0 - u.x) / (1.0 + (roughness * roughness - 1.0) * u.x));
    float phi      = TWO_PI * u.y;

    float3 localH = SphericalToCartesian(phi, cosTheta);

    NdotH = cosTheta;

    float3 localV;

    if (VeqN)
    {
        // localV == localN
        localV = float3(0.0, 0.0, 1.0);
        VdotH  = NdotH;
    }
    else
    {
        localV = mul(V, transpose(localToWorld));
        VdotH  = saturate(dot(localV, localH));
    }

    // Compute { localL = reflect(-localV, localH) }
    float3 localL = -localV + 2.0 * VdotH * localH;
    NdotL = localL.z;

    L = mul(localL, localToWorld);
}

// Ref: "A Simpler and Exact Sampling Routine for the GGX Distribution of Visible Normals".
void SampleVisibleAnisoGGXDir(float2 u, float3 V, float3x3 localToWorld,
                              float roughnessT, float roughnessB,
                              out float3 L,
                              out float  NdotL,
                              out float  NdotH,
                              out float  VdotH,
                                  bool   VeqN = false)
{
    float3 localV = mul(V, transpose(localToWorld));

    // Construct an orthonormal basis around the stretched view direction.
    float3x3 viewToLocal;
    if (VeqN)
    {
        viewToLocal = k_identity3x3;
    }
    else
    {
        viewToLocal[2] = normalize(float3(roughnessT * localV.x, roughnessB * localV.y, localV.z));
        viewToLocal[0] = (viewToLocal[2].z < 0.9999) ? normalize(cross(viewToLocal[2], float3(0, 0, 1))) : float3(1, 0, 0);
        viewToLocal[1] = cross(viewToLocal[0], viewToLocal[2]);
    }

    // Compute a sample point with polar coordinates (r, phi).
    float r   = sqrt(u.x);
    float b   = viewToLocal[2].z + 1;
    float a   = rcp(b);
    float c   = (u.y < a) ? u.y * b : 1 + (u.y * b - 1) / viewToLocal[2].z;
    float phi = PI * c;
    float p1  = r * cos(phi);
    float p2  = r * sin(phi) * ((u.y < a) ? 1 : viewToLocal[2].z);

    // Unstretch.
    float3 viewH = normalize(float3(roughnessT * p1, roughnessB * p2, sqrt(1 - p1 * p1 - p2 * p2)));
    VdotH = viewH.z;

    float3 localH = mul(viewH, viewToLocal);
    NdotH = localH.z;

    // Compute { localL = reflect(-localV, localH) }
    float3 localL = -localV + 2 * VdotH * localH;
    NdotL = localL.z;

    L = mul(localL, localToWorld);
}

// ref: http://blog.selfshadow.com/publications/s2012-shading-course/burley/s2012_pbs_disney_brdf_notes_v3.pdf p26
void SampleAnisoGGXDir(float2 u,
                       float3 V,
                       float3 N,
                       float3 tangentX,
                       float3 tangentY,
                       float  roughnessT,
                       float  roughnessB,
                   out float3 H,
                   out float3 L)
{
    // AnisoGGX NDF sampling
    H = sqrt(u.x / (1.0 - u.x)) * (roughnessT * cos(TWO_PI * u.y) * tangentX + roughnessB * sin(TWO_PI * u.y) * tangentY) + N;
    H = normalize(H);

    // Convert sample from half angle to incident angle
    L = 2.0 * saturate(dot(V, H)) * H - V;
}

// weightOverPdf return the weight (without the diffuseAlbedo term) over pdf. diffuseAlbedo term must be apply by the caller.
void ImportanceSampleLambert(float2   u,
                             float3x3 localToWorld,
                         out float3   L,
                         out float    NdotL,
                         out float    weightOverPdf)
{
    float3 localL = SampleHemisphereCosine(u.x, u.y);

    NdotL = localL.z;

    L = mul(localL, localToWorld);

    // Importance sampling weight for each sample
    // pdf = N.L / PI
    // weight = fr * (N.L) with fr = diffuseAlbedo / PI
    // weight over pdf is:
    // weightOverPdf = (diffuseAlbedo / PI) * (N.L) / (N.L / PI)
    // weightOverPdf = diffuseAlbedo
    // diffuseAlbedo is apply outside the function

    weightOverPdf = 1.0;
}

// weightOverPdf return the weight (without the Fresnel term) over pdf. Fresnel term must be apply by the caller.
void ImportanceSampleGGX(float2   u,
                         float3   V,
                         float3x3 localToWorld,
                         float    roughness,
                         float    NdotV,
                     out float3   L,
                     out float    VdotH,
                     out float    NdotL,
                     out float    weightOverPdf)
{
    float NdotH;
    SampleGGXDir(u, V, localToWorld, roughness, L, NdotL, NdotH, VdotH);

    // Importance sampling weight for each sample
    // pdf = D(H) * (N.H) / (4 * (L.H))
    // weight = fr * (N.L) with fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
    // weight over pdf is:
    // weightOverPdf = F(H) * G(V, L) * (L.H) / ((N.H) * (N.V))
    // weightOverPdf = F(H) * 4 * (N.L) * V(V, L) * (L.H) / (N.H) with V(V, L) = G(V, L) / (4 * (N.L) * (N.V))
    // Remind (L.H) == (V.H)
    // F is apply outside the function

    float Vis = V_SmithJointGGX(NdotL, NdotV, roughness);
    weightOverPdf = 4.0 * Vis * NdotL * VdotH / NdotH;
}

// weightOverPdf return the weight (without the Fresnel term) over pdf. Fresnel term must be apply by the caller.
void ImportanceSampleAnisoGGX(float2   u,
                              float3   V,
                              float3x3 localToWorld,
                              float    roughnessT,
                              float    roughnessB,
                              float    NdotV,
                          out float3   L,
                          out float    VdotH,
                          out float    NdotL,
                          out float    weightOverPdf)
{
    float3 tangentX = localToWorld[0];
    float3 tangentY = localToWorld[1];
    float3 N        = localToWorld[2];

    float3 H;
    SampleAnisoGGXDir(u, V, N, tangentX, tangentY, roughnessT, roughnessB, H, L);

    float NdotH = saturate(dot(N, H));
    // Note: since L and V are symmetric around H, LdotH == VdotH
    VdotH = saturate(dot(V, H));
    NdotL = saturate(dot(N, L));

    // Importance sampling weight for each sample
    // pdf = D(H) * (N.H) / (4 * (L.H))
    // weight = fr * (N.L) with fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
    // weight over pdf is:
    // weightOverPdf = F(H) * G(V, L) * (L.H) / ((N.H) * (N.V))
    // weightOverPdf = F(H) * 4 * (N.L) * V(V, L) * (L.H) / (N.H) with V(V, L) = G(V, L) / (4 * (N.L) * (N.V))
    // Remind (L.H) == (V.H)
    // F is apply outside the function

    // For anisotropy we must not saturate these values
    float TdotV = dot(tangentX, V);
    float BdotV = dot(tangentY, V);
    float TdotL = dot(tangentX, L);
    float BdotL = dot(tangentY, L);

    float Vis = V_SmithJointGGXAniso(TdotV, BdotV, NdotV, TdotL, BdotL, NdotL, roughnessT, roughnessB);
    weightOverPdf = 4.0 * Vis * NdotL * VdotH / NdotH;
}

// ----------------------------------------------------------------------------
// Pre-integration
// ----------------------------------------------------------------------------

// Ref: Listing 18 in "Moving Frostbite to PBR" + https://knarkowicz.wordpress.com/2014/12/27/analytical-dfg-term-for-ibl/
float4 IntegrateGGXAndDisneyFGD(float3 V, float3 N, float roughness, uint sampleCount = 8192)
{
    float NdotV     = saturate(dot(N, V));
    float4 acc      = float4(0.0, 0.0, 0.0, 0.0);
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(V.xy * 0.5 + 0.5);

    float3x3 localToWorld = GetLocalFrame(N);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u = frac(randNum + Hammersley2d(i, sampleCount));

        float VdotH;
        float NdotL;
        float weightOverPdf;

        float3 L; // Unused
        ImportanceSampleGGX(u, V, localToWorld, roughness, NdotV,
                            L, VdotH, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            // Integral{BSDF * <N,L> dw} =
            // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
            // F0 * Integral{(BSDF / F) * <N,L> dw} +
            // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} =
            // (1 - F0) * x + F0 * y = lerp(x, y, F0)

            acc.x += weightOverPdf * pow(1 - VdotH, 5);
            acc.y += weightOverPdf;
        }

        // for Disney we still use a Cosine importance sampling, true Disney importance sampling imply a look up table
        ImportanceSampleLambert(u, localToWorld, L, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            float LdotV = dot(L, V);
            float disneyDiffuse = DisneyDiffuseNoPI(NdotV, NdotL, LdotV, RoughnessToPerceptualRoughness(roughness));

            acc.z += disneyDiffuse * weightOverPdf;
        }
    }

    acc /= sampleCount;

    // Remap from the [0.5, 1.5] to the [0, 1] range.
    acc.z -= 0.5;

    return acc;
}

uint GetIBLRuntimeFilterSampleCount(uint mipLevel)
{
    uint sampleCount = 0;

    switch (mipLevel)
    {
        case 1: sampleCount = 21; break;
        case 2: sampleCount = 34; break;
#ifdef SHADER_API_MOBILE
        case 3: sampleCount = 34; break;
        case 4: sampleCount = 34; break;
        case 5: sampleCount = 34; break;
        case 6: sampleCount = 34; break; // UNITY_SPECCUBE_LOD_STEPS
#else
        case 3: sampleCount = 55; break;
        case 4: sampleCount = 89; break;
        case 5: sampleCount = 89; break;
        case 6: sampleCount = 89; break; // UNITY_SPECCUBE_LOD_STEPS
#endif
    }

    return sampleCount;
}

// Ref: Listing 19 in "Moving Frostbite to PBR"
float4 IntegrateLD(TEXTURECUBE_ARGS(tex, sampl),
                   TEXTURE2D(ggxIblSamples),
                   float3 V,
                   float3 N,
                   float roughness,
                   float index,      // Current MIP level minus one
                   float invOmegaP,
                   uint sampleCount, // Must be a Fibonacci number
                   bool prefilter,
                   bool usePrecomputedSamples)
{
    float3x3 localToWorld = GetLocalFrame(N);

#ifndef USE_KARIS_APPROXIMATION
    float NdotV       = 1; // N == V
    float partLambdaV = GetSmithJointGGXPartLambdaV(NdotV, roughness);
#endif

    float3 lightInt = float3(0.0, 0.0, 0.0);
    float  cbsdfInt = 0.0;

    for (uint i = 0; i < sampleCount; ++i)
    {
        float3 L;
        float  NdotL, NdotH, LdotH;

        if (usePrecomputedSamples)
        {
            float3 localL = LOAD_TEXTURE2D(ggxIblSamples, uint2(i, index)).xyz;

            L     = mul(localL, localToWorld);
            NdotL = localL.z;
            LdotH = sqrt(0.5 + 0.5 * NdotL);
        }
        else
        {
            float2 u = Fibonacci2d(i, sampleCount);

            // Note: if (N == V), all of the microsurface normals are visible.
            SampleGGXDir(u, V, localToWorld, roughness, L, NdotL, NdotH, LdotH, true);

            if (NdotL <= 0) continue; // Note that some samples will have 0 contribution
        }

        float mipLevel;

        if (!prefilter) // BRDF importance sampling
        {
            mipLevel = 0;
        }
        else // Prefiltered BRDF importance sampling
        {
            // Use lower MIP-map levels for fetching samples with low probabilities
            // in order to reduce the variance.
            // Ref: http://http.developer.nvidia.com/GPUGems3/gpugems3_ch20.html
            //
            // - OmegaS: Solid angle associated with the sample
            // - OmegaP: Solid angle associated with the texel of the cubemap

            float omegaS;

            if (usePrecomputedSamples)
            {
                omegaS = LOAD_TEXTURE2D(ggxIblSamples, uint2(i, index)).w;
            }
            else
            {
                // float PDF = D * NdotH * Jacobian, where Jacobian = 1 / (4 * LdotH).
                // Since (N == V), NdotH == LdotH.
                float pdf = 0.25 * D_GGX(NdotH, roughness);
                // TODO: improve the accuracy of the sample's solid angle fit for GGX.
                omegaS    = rcp(sampleCount) * rcp(pdf);
            }

            // 'invOmegaP' is precomputed on CPU and provided as a parameter to the function.
            // float omegaP = FOUR_PI / (6.0 * cubemapWidth * cubemapWidth);
            const float mipBias = roughness;
            mipLevel = 0.5 * log2(omegaS * invOmegaP) + mipBias;
        }

        // TODO: use a Gaussian-like filter to generate the MIP pyramid.
        float3 val = SAMPLE_TEXTURECUBE_LOD(tex, sampl, L, mipLevel).rgb;

        // The goal of this function is to use Monte-Carlo integration to find
        // X = Integral{Radiance(L) * CBSDF(L, N, V) dL} / Integral{CBSDF(L, N, V) dL}.
        // Note: Integral{CBSDF(L, N, V) dL} is given by the FDG texture.
        // CBSDF  = F * D * G * NdotL / (4 * NdotL * NdotV) = F * D * G / (4 * NdotV).
        // PDF    = D * NdotH / (4 * LdotH).
        // Weight = CBSDF / PDF = F * G * LdotH / (NdotV * NdotH).
        // Since we perform filtering with the assumption that (V == N),
        // (LdotH == NdotH) && (NdotV == 1) && (Weight == F * G).
        // Therefore, after the Monte Carlo expansion of the integrals,
        // X = Sum(Radiance(L) * Weight) / Sum(Weight) = Sum(Radiance(L) * F * G) / Sum(F * G).

    #ifndef USE_KARIS_APPROXIMATION
        // The choice of the Fresnel factor does not appear to affect the result.
        float F = 1; // F_Schlick(F0, LdotH);
        float V = V_SmithJointGGX(NdotL, NdotV, roughness, partLambdaV);
        float G = V * NdotL * NdotV; // 4 cancels out

        lightInt += F * G * val;
        cbsdfInt += F * G;
    #else
        // Use the approximation from "Real Shading in Unreal Engine 4": Weight ≈ NdotL.
        lightInt += NdotL * val;
        cbsdfInt += NdotL;
    #endif
    }

    return float4(lightInt / cbsdfInt, 1.0);
}

// Searches the row 'j' containing 'n' elements of 'haystack' and
// returns the index of the first element greater or equal to 'needle'.
uint BinarySearchRow(uint j, float needle, TEXTURE2D(haystack), uint n)
{
    uint  i = n - 1;
    float v = LOAD_TEXTURE2D(haystack, uint2(i, j)).r;

    if (needle < v)
    {
        i = 0;

        for (uint b = 1 << firstbithigh(n - 1); b != 0; b >>= 1)
        {
            uint p = i | b;
            v = LOAD_TEXTURE2D(haystack, uint2(p, j)).r;
            if (v <= needle) { i = p; } // Move to the right.
        }
    }

    return i;
}

float4 IntegrateLD_MIS(TEXTURECUBE_ARGS(envMap, sampler_envMap),
                       TEXTURE2D(marginalRowDensities),
                       TEXTURE2D(conditionalDensities),
                       float3 V,
                       float3 N,
                       float roughness,
                       float invOmegaP,
                       uint width,
                       uint height,
                       uint sampleCount,
                       bool prefilter)
{
    float3x3 localToWorld = GetLocalFrame(N);

    float2 randNum  = InitRandom(V.xy * 0.5 + 0.5);

    float3 lightInt = float3(0.0, 0.0, 0.0);
    float  cbsdfInt = 0.0;

/*
    // Dedicate 50% of samples to light sampling at 1.0 roughness.
    // Only perform BSDF sampling when roughness is below 0.5.
    const int lightSampleCount = lerp(0, sampleCount / 2, saturate(2.0 * roughness - 1.0));
    const int bsdfSampleCount  = sampleCount - lightSampleCount;
*/

    // The value of the integral of intensity values of the environment map (as a 2D step function).
    float envMapInt2dStep = LOAD_TEXTURE2D(marginalRowDensities, uint2(height, 0)).r;
    // Since we are using equiareal mapping, we need to divide by the area of the sphere.
    float envMapIntSphere = envMapInt2dStep * INV_FOUR_PI;

    // Perform light importance sampling.
    for (uint i = 0; i < sampleCount; i++)
    {
        float2 s = frac(randNum + Hammersley2d(i, sampleCount));

        // Sample a row from the marginal distribution.
        uint y = BinarySearchRow(0, s.x, marginalRowDensities, height - 1);

        // Sample a column from the conditional distribution.
        uint x = BinarySearchRow(y, s.y, conditionalDensities, width - 1);

        // Compute the coordinates of the sample.
        // Note: we take the sample in between two texels, and also apply the half-texel offset.
        // We could compute fractional coordinates at the cost of 4 extra texel samples.
        float  u = saturate((float)x / width  + 1.0 / width);
        float  v = saturate((float)y / height + 1.0 / height);
        float3 L = ConvertEquiarealToCubemap(u, v);

        float NdotL = saturate(dot(N, L));

        if (NdotL > 0.0)
        {
            float3 val = SAMPLE_TEXTURECUBE_LOD(envMap, sampler_envMap, L, 0).rgb;
            float  pdf = (val.r + val.g + val.b) / envMapIntSphere;

            if (pdf > 0.0)
            {
                // (N == V) && (acos(VdotL) == 2 * acos(NdotH)).
                float NdotH = sqrt(NdotL * 0.5 + 0.5);

                // *********************************************************************************
                // Our goal is to use Monte-Carlo integration with importance sampling to evaluate
                // X(V)   = Integral{Radiance(L) * CBSDF(L, N, V) dL} / Integral{CBSDF(L, N, V) dL}.
                // CBSDF  = F * D * G * NdotL / (4 * NdotL * NdotV) = F * D * G / (4 * NdotV).
                // Weight = CBSDF / PDF.
                // We use two approximations of Brian Karis from "Real Shading in Unreal Engine 4":
                // (F * G ≈ NdotL) && (NdotV == 1).
                // Weight = D * NdotL / (4 * PDF).
                // *********************************************************************************

                float weight = D_GGX(NdotH, roughness) * NdotL / (4.0 * pdf);

                lightInt += weight * val;
                cbsdfInt += weight;
            }
        }
    }

    // Prevent NaNs arising from the division of 0 by 0.
    cbsdfInt = max(cbsdfInt, FLT_EPS);

    return float4(lightInt / cbsdfInt, 1.0);
}

#endif // UNITY_IMAGE_BASED_LIGHTING_INCLUDED
