#ifndef  CAPSULE_OCCLUSION_MONTECARLO
#define CAPSULE_OCCLUSION_MONTECARLO

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/Shaders/CapsuleOcclusionData.hlsl"

// TODO: This should be in some utility hlsl
// source: Building an Orthonormal Basis, Revisited
// http://jcgt.org/published/0006/01/01/
// Same as reference implementation, except transposed.
float3x3 ComputeTangentToWorldMatrix(float3 n)
{
    float3x3 res;
    res[0][2] = n.x;
    res[1][2] = n.y;
    res[2][2] = n.z;

    float s = (n.z >= 0.0f) ? 1.0f : -1.0f;
    float a = -1.0f / (s + n.z);
    float b = n.x * n.y * a;

    res[0][0] = 1.0f + s * n.x * n.x * a;
    res[1][0] = s * b;
    res[2][0] = -s * n.x;

    res[0][1] = b;
    res[1][1] = s + n.y * n.y * a;
    res[2][1] = -n.y;

    return res;
}

float EvaluateCapsuleRaytraceOcclusion(EllipsoidOccluderData data, float3 positionWS, float3 directionWS, float3 N, float3 V)
{
    float3 occluderPositionWS = GetOccluderPositionRWS(data);
    float3 occluderDirectionWS = GetOccluderDirectionWS(data);
    float occluderScaleWS = GetOccluderScaling(data);
    float occluderRadiusWS = GetOccluderRadius(data);

    // float3x3 sphereFromWorldSpace = ComputeTangentToWorldMatrix(occluderDirectionWS);
    // sphereFromWorldSpace = transpose(sphereFromWorldSpace);
    // sphereFromWorldSpace[0] /= occluderRadiusWS;
    // sphereFromWorldSpace[1] /= occluderRadiusWS;
    // sphereFromWorldSpace[2] /= occluderScaleWS * 0.5f;

    float3x3 sphereFromWorldSpace = float3x3(
        data.sphereFromWorldTangent,
        data.sphereFromWorldBitangent,
        data.sphereFromWorldNormal
        );



    float3 samplePositionWS = positionWS + N * 1e-3f + V * 1e-3f;
    float3 samplePositionSphereSpace = mul(sphereFromWorldSpace, samplePositionWS - occluderPositionWS);

    float3 sampleDirectionWS = directionWS;
    float3 sampleDirectionSphereSpace = normalize(mul(sphereFromWorldSpace, sampleDirectionWS));

    // Assume Sphere is at the origin (i.e start = position - spherePosition)
    float2 intersections;
    bool intersects = IntersectRaySphere(samplePositionSphereSpace, sampleDirectionSphereSpace, 1.0f, intersections);

    // TODO: Could wire up occluder opacity here.
    bool isSelfShadowing = min(intersections.x, intersections.y) < 1e-4f;
    return (intersects && !isSelfShadowing) ? 0.0f : 1.0f;
}

float AccumulateCapsuleRaytraceOcclusion(float prevAO, float capsuleAO)
{
    return min(prevAO, capsuleAO);
}

float ComputePDFInverseGGXDistributionOfVisibleNormals(float NdotH, float NdotV, float roughness)
{
    // PDF = D_GGX(NdotH, roughness) * G_MaskingSmithGGX(NdotV, roughness) * VdotH / (4.0f * NdotV * VdotH)
    // PDFInverse = (4.0f * NdotV * VdotH) / (D_GGX(NdotH, roughness) * G_MaskingSmithGGX(NdotV, roughness) * VdotH)
    // PDFInverse = (4.0f * NdotV) / (D_GGX(NdotH, roughness) * G_MaskingSmithGGX(NdotV, roughness))
    return 4.0f * NdotV * D_GGXInverse(NdotH, roughness) / G_MaskingSmithGGX(NdotV, roughness);
    // return 4.0f * VdotH * D_GGXInverse(NdotH, roughness);
}

void EvaluateCapsuleOcclusionMonteCarlo(
    uint evaluationFlags,
    PositionInputs posInput,
    float3 N,
    float3 V,
    float roughness,
    inout float ambientOcclusion,
    inout float specularOcclusion,
    inout float shadow)
{
    // ambientOcclusion = 1.0f - ambientOcclusion;
    // specularOcclusion = 1.0f - specularOcclusion;
    ambientOcclusion = 0.0f;
    specularOcclusion = 0.0f;

    const int SAMPLE_COUNT = 8;
    const float SAMPLE_COUNT_INVERSE = 1.0f / (float)SAMPLE_COUNT;

    for (int s = 0; s < SAMPLE_COUNT; ++s)
    {
        float ambientOcclusionCurrent = 1.0f;
        float specularOcclusionCurrent = 1.0f;
        // Generete a new direction to follow
        float2 newSample;
        newSample.x = GetBNDSequenceSample(posInput.positionSS.xy, _FrameCount * SAMPLE_COUNT + s, 0);
        newSample.y = GetBNDSequenceSample(posInput.positionSS.xy, _FrameCount * SAMPLE_COUNT + s, 1);

        // Importance sample with a cosine lobe (direction that will be used for ray casting)
        float3 sampleDirectionDiffuseWS = SampleHemisphereCosine(newSample.x, newSample.y, N);

        float3x3 tangentToWorld = ComputeTangentToWorldMatrix(N);
        float3 viewDirectionTS = mul(V, tangentToWorld);
        float3 sampleDirectionSpecularWS = SampleDirectionGGXDistributionOfVisibleNormals(
            newSample,
            tangentToWorld,
            viewDirectionTS,
            roughness
        );


        uint sphereCount, sphereStart;

#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        GetCountAndStart(posInput, LIGHTCATEGORY_CAPSULE_OCCLUDER, sphereStart, sphereCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        sphereCount = /* TO ADD FIXED COUNT */;
        sphereStart = 0;
#endif

        bool fastPath = false;
#if SCALARIZE_LIGHT_LOOP
        uint sphereStartLane0;
        fastPath = IsFastPath(sphereStart, sphereStartLane0);

        if (fastPath)
        {
            sphereStart = sphereStartLane0;
        }
#endif

        // Scalarized loop. All spheres that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the one relevant to current thread/pixel are processed.
        // For clarity, the following code will follow the convention: variables starting with s_ are meant to be wave uniform (meant for scalar register),
        // v_ are variables that might have different value for each thread in the wave (meant for vector registers).
        // This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that light data accessed should be largely coherent.
        // Note that the above is valid only if wave intriniscs are supported.
        uint v_sphereListOffset = 0;
        uint v_sphereIdx = sphereStart;

        while (v_sphereListOffset < sphereCount)
        {
            v_sphereIdx = FetchIndex(sphereStart, v_sphereListOffset);
            uint s_sphereIdx = ScalarizeElementIndex(v_sphereIdx, fastPath);
            if (s_sphereIdx == -1)
                break;

            EllipsoidOccluderData s_capsuleData = FetchEllipsoidOccluderData(s_sphereIdx);

            // If current scalar and vector sphere index match, we process the sphere. The v_sphereListOffset for current thread is increased.
            // Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
            // end up with a unique v_sphereIdx value that is smaller than s_sphereIdx hence being stuck in a loop. All the active lanes will not have this problem.
            if (s_sphereIdx >= v_sphereIdx)
            {
                v_sphereListOffset++;

                float4 dirAndLen = GetDataForSphereIntersection(s_capsuleData, posInput.positionWS);

                if (evaluationFlags & CAPSULEOCCLUSIONTYPE_AMBIENT_OCCLUSION)
                {
                    float capsuleAO = EvaluateCapsuleRaytraceOcclusion(s_capsuleData, posInput.positionWS, sampleDirectionDiffuseWS, N, V);
                    ambientOcclusionCurrent = AccumulateCapsuleRaytraceOcclusion(ambientOcclusionCurrent, capsuleAO);
                }

                if (evaluationFlags & CAPSULEOCCLUSIONTYPE_SPECULAR_OCCLUSION)
                {
                    float capsuleSpecOcc = EvaluateCapsuleRaytraceOcclusion(s_capsuleData, posInput.positionWS, sampleDirectionSpecularWS, N, V);
                    specularOcclusionCurrent = AccumulateCapsuleRaytraceOcclusion(specularOcclusionCurrent, capsuleSpecOcc);
                }

                // TODO:
                // if (evaluationFlags & CAPSULEOCCLUSIONTYPE_DIRECTIONAL_SHADOWS)
                // {
                //     float capsuleShadow = EvaluateCapsuleShadow(s_capsuleData, posInput.positionWS, N, dirAndLen, posInput.positionSS);
                //     shadow = AccumulateCapsuleShadow(shadow, capsuleShadow);
                // }
            }
        }


        {
            float3 L = sampleDirectionSpecularWS;
            float3 H = normalize(L + V);
            float NdotL = max(1e-5f, dot(N, L));
            float NdotH = saturate(dot(N, H));
            float VdotH = max(1e-5f, abs(dot(V, H)));
            float NdotV = max(1e-5f, abs(dot(N, V)));

            // https://schuttejoe.github.io/post/ggximportancesamplingpart2/
            // Key for mapping variables from source to variables in our code:
            // wg = N
            // wi = L
            // wo = V
            // wm = H
            //
            // outgoingRadiance(N, V) = integral[incomingRadiance * BRDF(L, V) * NdotL * ddx]
            // BRDF(L, V) = F(VdotH) * D(NdotH) * G2(NdotL, NdotV) / (4.0f * NdotL * NdotV)
            // outgoingRadiance = incomingRadiance * NdotL * F(VdotH) * D(NdotH) * G2(NdotL, NdotV) / (4.0f * NdotL * NdotV) / PDF
            //
            // PDF = D(NdotH) * G1(NdotV) * VdotH / (4.0f * NdotV * VdotH)
            // PDF = D_GGX(NdotH, roughness) * G_MaskingSmithGGX(NdotV, roughness) * VdotH / (4.0f * NdotV * VdotH)
            float D = D_GGX(NdotH, roughness);
            float F = 1.0f;//F_Schlick(0.04f, VdotH);
            float G = G_MaskingSmithGGX(NdotL, roughness) * G_MaskingSmithGGX(NdotV, roughness);
            G /= (4.0 * NdotL * NdotV);

            float brdf = F * D * G * NdotL;
            specularOcclusionCurrent *= ComputePDFInverseGGXDistributionOfVisibleNormals(NdotH, NdotV, roughness);

            specularOcclusionCurrent *= brdf;

            specularOcclusion += specularOcclusionCurrent * SAMPLE_COUNT_INVERSE;
        }

        {
            // No BRDF application or PDF division needed, they cancel eachother out with lambert brdf
            // and cosine weighted sampling.
            // Importance sampling weight for each sample
            // pdf = N.L / PI
            // weight = fr * (N.L) with fr = diffuseAlbedo / PI
            // weight over pdf is:
            // weightOverPdf = (diffuseAlbedo / PI) * (N.L) / (N.L / PI)
            // weightOverPdf = diffuseAlbedo
            // diffuseAlbedo is apply outside the function
            ambientOcclusion += ambientOcclusionCurrent * SAMPLE_COUNT_INVERSE;
        }
    }

    specularOcclusion = PositivePow(specularOcclusion, _CapsuleSpecularOcclusionIntensity);
    ambientOcclusion = 1.0f - PositivePow(ambientOcclusion, _CapsuleAmbientOcclusionIntensity);

}

#endif
