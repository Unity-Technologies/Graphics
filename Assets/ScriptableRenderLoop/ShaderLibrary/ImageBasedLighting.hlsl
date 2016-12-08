#ifndef UNITY_IMAGE_BASED_LIGHTING_INCLUDED
#define UNITY_IMAGE_BASED_LIGHTING_INCLUDED

#include "CommonLighting.hlsl"
#include "CommonMaterial.hlsl"
#include "BSDF.hlsl"
#include "Sampling.hlsl"

//-----------------------------------------------------------------------------
// Util image based lighting
//-----------------------------------------------------------------------------

// TODO: We need to change this hard limit!
#define UNITY_SPECCUBE_LOD_STEPS (6)

float perceptualRoughnessToMipmapLevel(float perceptualRoughness)
{
    // TODO: Clean a bit this code
    // CAUTION: remap from Morten may work only with offline convolution, see impact with runtime convolution!

    // For now disabled
#if 0
    float m = PerceptualRoughnessToRoughness(perceptualRoughness); // m is the real roughness parameter
    float n = (2.0 / max(FLT_EPSILON, m*m)) - 2.0;		// remap to spec power. See eq. 21 in --> https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf

    n /= 4.0;									    // remap from n_dot_h formulatino to n_dot_r. See section "Pre-convolved Cube Maps vs Path Tracers" --> https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html

    perceptualRoughness = pow(2.0 / (n + 2.0), 0.25);		// remap back to square root of real roughness (0.25 include both the sqrt root of the conversion and sqrt for going from roughness to perceptualRoughness)
#else
    // MM: came up with a surprisingly close approximation to what the #if 0'ed out code above does.
    perceptualRoughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);
#endif

    return perceptualRoughness * UNITY_SPECCUBE_LOD_STEPS;
}

float mipmapLevelToPerceptualRoughness(float mipmapLevel)
{
    return mipmapLevel / UNITY_SPECCUBE_LOD_STEPS;
}

// Ref: See "Moving Frostbite to PBR" Listing 22
// This formulation is for GGX only (with smith joint visibility or regular)
float3 GetSpecularDominantDir(float3 N, float3 R, float roughness)
{
    float a = 1.0 - roughness;
    float lerpFactor = a * (sqrt(a) + roughness);
    // The result is not normalized as we fetch in a cubemap
    return lerp(N, R, lerpFactor);
}

//-----------------------------------------------------------------------------
// Anisotropic image based lighting
//-----------------------------------------------------------------------------
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

void ImportanceSampleCosDir(float2 u,
                            float3 N,
                            float3 tangentX,
                            float3 tangentY,
                            out float3 L)
{
    // Cosine sampling - ref: http://www.rorydriscoll.com/2009/01/07/better-sampling/
    float cosTheta = sqrt(max(0.0, 1.0 - u.x));
    float sinTheta = sqrt(u.x);
    float phi = TWO_PI * u.y;

    // Transform from spherical into cartesian
    L = float3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta);
    // Local to world
    L = tangentX * L.x + tangentY * L.y + N * L.z;
}

void ImportanceSampleGGXDir(float2 u,
                            float3 V,
                            float3 N,
                            float3 tangentX,
                            float3 tangentY,
                            float roughness,
                            out float3 H,
                            out float3 L)
{
    // GGX NDF sampling
    float cosThetaH = sqrt((1.0 - u.x) / (1.0 + (roughness * roughness - 1.0) * u.x));
    float sinThetaH = sqrt(max(0.0, 1.0 - cosThetaH * cosThetaH));
    float phiH      = TWO_PI * u.y;

    // Transform from spherical into cartesian
    H = float3(sinThetaH * cos(phiH), sinThetaH * sin(phiH), cosThetaH);
    // Local to world
    H = tangentX * H.x + tangentY * H.y + N * H.z;

    // Convert sample from half angle to incident angle
    L = 2.0 * dot(V, H) * H - V;
}

// ref: http://blog.selfshadow.com/publications/s2012-shading-course/burley/s2012_pbs_disney_brdf_notes_v3.pdf p26
void ImportanceSampleAnisoGGXDir(   float2 u,
                                    float3 V,
                                    float3 N,
                                    float3 tangentX,
                                    float3 tangentY,
                                    float roughnessT,
                                    float roughnessB,
                                    out float3 H,
                                    out float3 L)
{
    // AnisoGGX NDF sampling
    H = sqrt(u.x / (1.0 - u.x)) * (roughnessT * cos(TWO_PI * u.y) * tangentX + roughnessB * sin(TWO_PI * u.y) * tangentY) + N;
    H = normalize(H);

    // Local to world
  //  H = tangentX * H.x + tangentY * H.y + N * H.z;

    // Convert sample from half angle to incident angle
    L = 2.0 * dot(V, H) * H - V;
}

// weightOverPdf return the weight (without the diffuseAlbedo term) over pdf. diffuseAlbedo term must be apply by the caller.
void ImportanceSampleLambert(
    float2 u,
    float3 N,
    float3 tangentX,
    float3 tangentY,
    out float3 L,
    out float NdotL,
    out float weightOverPdf)
{
    ImportanceSampleCosDir(u, N, tangentX, tangentY, L);

    NdotL = saturate(dot(N, L));

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
void ImportanceSampleGGX(
    float2 u,
    float3 V,
    float3 N,
    float3 tangentX,
    float3 tangentY,
    float roughness,
    float NdotV,
    out float3 L,
    out float VdotH,
    out float NdotL,
    out float weightOverPdf)
{
    float3 H;
    ImportanceSampleGGXDir(u, V, N, tangentX, tangentY, roughness, H, L);

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

    float Vis = V_SmithJointGGX(NdotL, NdotV, roughness);
    weightOverPdf = 4.0 * Vis * NdotL * VdotH / NdotH;
}

// weightOverPdf return the weight (without the Fresnel term) over pdf. Fresnel term must be apply by the caller.
void ImportanceSampleAnisoGGX(
    float2 u,
    float3 V,
    float3 N,
    float3 tangentX,
    float3 tangentY,
    float roughnessT,
    float roughnessB,
    float NdotV,
    out float3 L,
    out float VdotH,
    out float NdotL,
    out float weightOverPdf)
{
    float3 H;
    ImportanceSampleAnisoGGXDir(u, V, N, tangentX, tangentY, roughnessT, roughnessB, H, L);

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
    float TdotV = dot(tangentX, V);
    float BdotV = dot(tangentY, V);
    float TdotL = saturate(dot(tangentX, L));
    float BdotL = saturate(dot(tangentY, L));

    float Vis = V_SmithJointGGXAniso(TdotV, BdotV, NdotV, TdotL, BdotL, NdotL, roughnessT, roughnessB);
    weightOverPdf = 4.0 * Vis * NdotL * VdotH / NdotH;
}

// ----------------------------------------------------------------------------
// Pre-integration
// ----------------------------------------------------------------------------

// Ref: Listing 18 in "Moving Frostbite to PBR" + https://knarkowicz.wordpress.com/2014/12/27/analytical-dfg-term-for-ibl/
float4 IntegrateGGXAndDisneyFGD(float3 V, float3 N, float roughness, uint sampleCount)
{
    float NdotV     = saturate(dot(N, V));
    float4 acc      = float4(0.0, 0.0, 0.0, 0.0);
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(V.xy * 0.5 + 0.5);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5);

        float VdotH;
        float NdotL;
        float weightOverPdf;

        float3 L; // Unused
        ImportanceSampleGGX(u, V, N, tangentX, tangentY, roughness, NdotV,
                            L, VdotH, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            // Integral is
            //   1 / NumSample * \int[  L * fr * (N.L) / pdf ]  with pdf =  D(H) * (N.H) / (4 * (L.H)) and fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
            // This is split  in two part:
            //   A) \int[ L * (N.L) ]
            //   B) \int[ F(H) * 4 * (N.L) * V(V, L) * (L.H) / (N.H) ] with V(V, L) = G(V, L) / (4 * (N.L) * (N.V))
            //      = \int[ F(H) * weightOverPdf ]

            // Recombine at runtime with: ( f0 * weightOverPdf * (1 - Fc) + f90 * weightOverPdf * Fc ) with Fc =(1 - V.H)^5
            float Fc            = pow(1.0 - VdotH, 5.0);
            acc.x               += (1.0 - Fc) * weightOverPdf;
            acc.y               += Fc * weightOverPdf;
        }

        // for Disney we still use a Cosine importance sampling, true Disney importance sampling imply a look up table
        ImportanceSampleLambert(u, N, tangentX, tangentY, L, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            float3 H = normalize(L + V);
            float LdotH = dot(L, H);
            float disneyDiffuse = DisneyDiffuse(NdotV, NdotL, LdotH, RoughnessToPerceptualRoughness(roughness));

            acc.z += disneyDiffuse * weightOverPdf;
        }
    }

    return acc / sampleCount;
}

// Ref: Listing 19 in "Moving Frostbite to PBR"
float4 IntegrateLD(TEXTURECUBE_ARGS(tex, sampl),
                    float3 V,
                    float3 N,
                    float roughness,
                    float mipmapcount,
                    float invOmegaP,
                    uint sampleCount,
                    bool prefilter = true) // static bool
{
    float3 acc          = float3(0.0, 0.0, 0.0);
    float  accWeight    = 0;

    float2 randNum  = InitRandom(V.xy * 0.5 + 0.5);

    float3 tangentX, tangentY;
    GetLocalFrame(N, tangentX, tangentY);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum + 0.5);

        float3 H;
        float3 L;
        ImportanceSampleGGXDir(u, V, N, tangentX, tangentY, roughness, H, L);

        float NdotL = saturate(dot(N,L));

        float mipLevel;

        if (!prefilter) // BRDF importance sampling
        {
            mipLevel = 0.0;
        }
        else // Prefiltered BRDF importance sampling
        {
            // Use lower MIP-map levels for fetching samples with low probabilities
            // in order to reduce the variance.
            // Ref: http://http.developer.nvidia.com/GPUGems3/gpugems3_ch20.html
            //
            // pdf = D * NdotH * jacobian, where jacobian = 1.0 / (4* LdotH).
            //
            // Since L and V are symmetric around H, LdotH == VdotH.
            // Since we pre-integrate the result for the normal direction,
            // N == V and then NdotH == LdotH. Therefore, the BRDF's pdf
            // can be simplified:
            // pdf = D * NdotH / (4 * LdotH) = D * 0.25;
            //
            // - OmegaS : Solid angle associated to a sample
            // - OmegaP : Solid angle associated to a pixel of the cubemap

            float pdf       = D_GGX(NdotH, roughness) * 0.25;
            float omegaS    = 1.0 / (sampleCount * pdf);                      // Solid angle associated with the sample
            // invOmegaP is precomputed on CPU and provide as a parameter of the function
            // float omegaP = FOUR_PI / (6.0f * cubemapWidth * cubemapWidth); // Solid angle associated with the pixel of the cubemap
            mipLevel        = 0.5 * log2(omegaS * invOmegaP) + 1.0;           // Clamp is not necessary as the hardware will do it
        }

        if (NdotL > 0.0f)
        {
            float3 val = SAMPLE_TEXTURECUBE_LOD(tex, sampl, L, mipLevel).rgb;

            // See p63 equation (53) of moving Frostbite to PBR v2 for the extra NdotL here (both in weight and value)
            acc             += val * NdotL;
            accWeight       += NdotL;
        }
    }

    return float4(acc * (1.0 / accWeight), 1.0);
}

#endif // UNITY_IMAGE_BASED_LIGHTING_INCLUDED
