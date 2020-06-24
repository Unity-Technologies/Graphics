#ifndef CAPSULE_OCCLUSION_COMMON_DEF
#define CAPSULE_OCCLUSION_COMMON_DEF

#if !defined(USE_FPTL_LIGHTLIST) && !defined(USE_CLUSTERED_LIGHTLIST)
    #define USE_FPTL_LIGHTLIST // Use light tiles for contact shadows
#endif
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleOcclusionSystem.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/EllipsoidOccluder.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleOcclusionShaderUtils.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/SphericalGaussian.hlsl"

// --------------------------------------------
// Shader variables
// --------------------------------------------
CBUFFER_START(CapsuleOcclusionConstantBuffer)
    float4 _CapsuleShadowParameters;    // xyz: direction w: cone width to use.      // Soon to be subjected to changes!
CBUFFER_END

TEXTURE3D(_CapsuleShadowLUT);


// StructuredBuffer<OrientedBBox> _CapsuleOccludersBounds;
StructuredBuffer<EllipsoidOccluderData> _CapsuleOccludersDatas;

EllipsoidOccluderData FetchEllipsoidOccluderData(uint index)
{
    return _CapsuleOccludersDatas[index];
}

// --------------------------------------------
// Occluder data helpers
// --------------------------------------------
float3 GetOccluderPositionRWS(EllipsoidOccluderData data)
{
    return data.positionRWS_radius.xyz;
}

float GetOccluderRadius(EllipsoidOccluderData data)
{
    return data.positionRWS_radius.w;
}

float3 GetOccluderDirectionWS(EllipsoidOccluderData data)
{
    return normalize(data.directionWS_influence.xyz);
}

float GetOccluderScaling(EllipsoidOccluderData data)
{
    return length(data.directionWS_influence.xyz);
}

float GetOccluderInfluenceRadiusWS(EllipsoidOccluderData data)
{
    return data.directionWS_influence.w;
}

// --------------------------------------------
// Data preparation functions
// --------------------------------------------
float4 GetDataForSphereIntersection(EllipsoidOccluderData data)
{
    // TODO : Fill with transformations needed so the rest of the code deals with simple spheres.
    // xyz should be un-normalized direction, w should contain the length.
    float3 dir = data.directionWS_influence.xyz;
    float len = data.directionWS_influence.w;
    return float4(dir.x, dir.y, dir.z, len);
}

void ComputeDirectionAndDistanceFromStartAndEnd(float3 start, float3 end, out float3 direction, out float dist)
{
    direction = end - start;
    dist = length(direction);
    direction = (dist > 1e-5f) ? (direction / dist) : float3(0.0f, 1.0f, 0.0f);
}

float ComputeInfluenceFalloff(float dist, float influenceRadius)
{
    // Linear falloff for now. Might want to curve this in the future, similar to punctual lights.
    return 1.0f - saturate(dist / influenceRadius);
}

float ApplyInfluenceFalloff(float occlusion, float influenceFalloff)
{
    return lerp(1.0f, occlusion, influenceFalloff);
}

// --------------------------------------------
// Evaluation functions
// --------------------------------------------
// These functions should evaluate the occlusion types. Note that all of these functions take EllipsoidOccluderData containing the shape to evaluate against
// and a dirAndLength containing the data output by the function GetDataForSphereIntersection()

float EvaluateCapsuleAmbientOcclusion(EllipsoidOccluderData data, float3 positionWS, float3 N, float4 dirAndLength)
{
    float3 dir = GetOccluderDirectionWS(data);
    float proj = dot(positionWS, dir);
    float3 positionCS = positionWS - (proj * dir) + proj * dir / GetOccluderScaling(data);
    proj = dot(GetOccluderPositionRWS(data), dir);
    float3 centerCS = GetOccluderPositionRWS(data) - (proj * dir) + proj * dir / GetOccluderScaling(data);

    // TODO: should also transform the normal
    // IMPORTANT: Remember to modify by intensity modifier here and not after.
    return IQSphereAO(positionCS, N, centerCS, GetOccluderRadius(data));
}

// I stubbed out this version as a reference for myself while the work was being done by others. Keeping here as a reference in case we need it,
// but leaving the above version as the standard so that I do not interfere with others work.
float EvaluateCapsuleAmbientOcclusionNick(EllipsoidOccluderData data, float3 positionWS, float3 N, float4 dirAndLength)
{
    // TODO: Can combine distance falloff math with IQSphereAO math.
    float3 occluderFromSurfaceDirectionWS;
    float occluderFromSurfaceDistance;
    ComputeDirectionAndDistanceFromStartAndEnd(positionWS, GetOccluderPositionRWS(data), occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);

    float occluderSphereRadius = GetOccluderRadius(data) * 0.9;
    float occlusion = 1.0f - IQSphereAO(positionWS, N, GetOccluderPositionRWS(data), occluderSphereRadius);
    occlusion = ApplyInfluenceFalloff(occlusion, ComputeInfluenceFalloff(occluderFromSurfaceDistance, GetOccluderInfluenceRadiusWS(data)));

    return occlusion;
}

SphericalGaussian SphericalGaussianFromEllipsoidOccluderData(EllipsoidOccluderData data, float3 occluderFromSurfaceDirectionWS, float occluderFromSurfaceDistance)
{
   
    float3 occluderAxisMajor = GetOccluderDirectionWS(data);
    float occluderRadiusMinor = GetOccluderRadius(data);
    float occluderRadiusMajor = occluderRadiusMinor * GetOccluderScaling(data);
    float occluderRadiusAverage = (occluderRadiusMinor + occluderRadiusMajor) * 0.5f;
    float occluderRadiusProjectedAverage = lerp(occluderRadiusAverage, occluderRadiusMinor, abs(dot(occluderAxisMajor, occluderFromSurfaceDirectionWS)));

    // tan(theta) == opposite / adjacent
    // theta == atan(opposite / adjacent)
    // cosTheta == cos(atan(opposite / adjacent))
    // cosTheta == cos(atan(occluderRadiusProjectedAverage / occluderFromSurfaceDistance))
    // cos(atan(x / y)) == rsqrt(x^2 / y^2 + 1)
    // cosTheta == rsqrt(occluderRadiusProjectedAverage^2 / occluderFromSurfaceDistance^2 + 1)
    float cosTheta = rsqrt((occluderRadiusProjectedAverage * occluderRadiusProjectedAverage) / (occluderFromSurfaceDistance * occluderFromSurfaceDistance) + 1.0f);
    float amplitude = 1.0f;
    float epsilon = 0.1f;
    const float SHARPNESS_MAX = 10000.0f;
    float sharpness = min(SHARPNESS_MAX, SphericalGaussianSharpnessFromAngleAndLogThreshold(cosTheta, log(amplitude), log(epsilon)));
    // float sharpness = min(SHARPNESS_MAX, occluderFromSurfaceDistance * occluderFromSurfaceDistance / (occluderRadiusProjectedAverage * occluderRadiusProjectedAverage));

    SphericalGaussian res;
    res.amplitude = 1.0f; // TODO: Could wire up a per-occluder intensity here if we wanted to allow artists to approximate transparent occluders.
    res.normal = occluderFromSurfaceDirectionWS;
    res.sharpness = sharpness;

    return res;
}

AnisotropicSphericalGaussian AnisotropicSphericalGaussianFromEllipsoidOccluderData(EllipsoidOccluderData data, float3 occluderFromSurfaceDirectionWS, float occluderFromSurfaceDistance)
{
   
    float3 occluderAxisMajor = GetOccluderDirectionWS(data);
    float occluderRadiusMinor = GetOccluderRadius(data);
    float occluderRadiusMajor = occluderRadiusMinor * GetOccluderScaling(data);

    // TODO: Be sure to verify the normal direction here. The sign of the normal matters! Backfacing ASGs are lerped out.
    //
    // Build an orthonormal frame for our ASG that points in the direction of our occluder,
    // and whos tangent is aligned with the occluder major axis projected onto the hemisphere.
    // TODO: May need to perform abs(axisMajorTNormalScalar) around all uses.
    float3 normal = occluderFromSurfaceDirectionWS;
    float axisMajorTNormalScalar = dot(occluderAxisMajor, occluderFromSurfaceDirectionWS);
    float3 axisMajorTNormal = normal * axisMajorTNormalScalar;
    // Guard against degenerate case where major axis direction and normal are aligned.
    float3 up = (abs(normal.y) < 0.999f) ? float3(0.0f, 1.0f, 0.0f) : float3(1.0f, 0.0f, 0.0f);
    float3 tangent = (abs(axisMajorTNormalScalar) < 0.999f)
        ? normalize(occluderAxisMajor - axisMajorTNormal)
        : normalize(cross(up, normal));
    float3 bitangent = normalize(cross(tangent, normal));

    // TODO: project
    float occluderRadiusMajorProjected = lerp(occluderRadiusMajor, occluderRadiusMinor, (axisMajorTNormalScalar));

    // tan(theta) == opposite / adjacent
    // theta == atan(opposite / adjacent)
    // cosTheta == cos(atan(opposite / adjacent))
    // cosTheta == cos(atan(occluderRadiusProjectedAverage / occluderFromSurfaceDistance))
    // cos(atan(x / y)) == rsqrt(x^2 / y^2 + 1)
    // cosTheta == rsqrt(occluderRadiusProjectedAverage^2 / occluderFromSurfaceDistance^2 + 1)
    float cosThetaRadiusMinor = rsqrt((occluderRadiusMinor * occluderRadiusMinor) / (occluderFromSurfaceDistance * occluderFromSurfaceDistance) + 1.0f);
    float cosThetaRadiusMajor = rsqrt((occluderRadiusMajorProjected * occluderRadiusMajorProjected) / (occluderFromSurfaceDistance * occluderFromSurfaceDistance) + 1.0f);
    float amplitude = 1.0f;
    float epsilon = 0.5f;
    const float SHARPNESS_MAX = 10000.0f;
    float sharpnessY = min(SHARPNESS_MAX, 0.5f * SphericalGaussianSharpnessFromAngleAndLogThreshold(cosThetaRadiusMinor, log(amplitude), log(epsilon)));
    float sharpnessX = min(SHARPNESS_MAX, 0.5f * SphericalGaussianSharpnessFromAngleAndLogThreshold(cosThetaRadiusMajor, log(amplitude), log(epsilon)));

    AnisotropicSphericalGaussian res;
    res.amplitude = 1.0f; // TODO: Could wire up a per-occluder intensity here if we wanted to allow artists to approximate transparent occluders.
    res.sharpness = float2(sharpnessX, sharpnessY);
    res.normal = normal;
    res.tangent = tangent;
    res.bitangent = bitangent;

    return res;
}

float EvaluateCapsuleAmbientOcclusionSphericalGaussianReference(EllipsoidOccluderData data, float3 positionWS, float3 N, float3 V, float roughness, float4 dirAndLength)
{
    float3 occluderFromSurfaceDirectionWS;
    float occluderFromSurfaceDistance;
    ComputeDirectionAndDistanceFromStartAndEnd(positionWS, GetOccluderPositionRWS(data), occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);

#if 1
    AnisotropicSphericalGaussian asgOccluder = AnisotropicSphericalGaussianFromEllipsoidOccluderData(data, occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);
    SphericalGaussian sgDiffuseBRDF = SphericalGaussianFromDiffuseBRDFApproximate(N);
    float occlusion = 1.0f - AnisotropicSphericalGaussianInnerProductSG(asgOccluder, sgDiffuseBRDF);
#else
    SphericalGaussian sgOccluder = SphericalGaussianFromEllipsoidOccluderData(data, occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);
    float occlusion = 1.0f - SphericalGaussianAndProjectedAreaProductIntegralApproximateHill(sgOccluder, N) / PI;
    // float occlusion = 1.0f - SphericalGaussianAndProjectedAreaProductIntegralApproximateMeder(sgOccluder, N);// * PI;
#endif

    occlusion = ApplyInfluenceFalloff(occlusion, ComputeInfluenceFalloff(occluderFromSurfaceDistance, GetOccluderInfluenceRadiusWS(data)));

    return occlusion;
}

// TODO: Get the visiblity terms from our BRDF code, do not reimplement here.
float GGX_V1(in float m2, in float nDotX)
{
    return 1.0f / (nDotX + sqrt(m2 + (1 - m2) * nDotX * nDotX));
}

// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-4-specular-lighting-from-an-sg-light-source/
float EvaluateCapsuleSpecularOcclusionSGOccluderSGBRDF(EllipsoidOccluderData data, float3 positionWS, float3 N, float3 V, float roughness, float4 dirAndLength)
{
    float3 occluderFromSurfaceDirectionWS;
    float occluderFromSurfaceDistance;
    ComputeDirectionAndDistanceFromStartAndEnd(positionWS, GetOccluderPositionRWS(data), occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);

    SphericalGaussian sgOccluder = SphericalGaussianFromEllipsoidOccluderData(data, occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);
    
    // TODO: Warping, and majority of specular BRDF evaluation can happen once, rather than per occluder.
    SphericalGaussian sgNDFHS = SphericalGaussianFromNDFApproximate(N, roughness);
    SphericalGaussian sgNDFWS = SphericalGaussianWarpWSFromHS(sgNDFHS, V);

    // Closed form convolution of occluder SG approximation and specular BRDF NDF term SG approximation.
    float specular = SphericalGaussianInnerProduct(sgNDFWS, sgOccluder);

    // Parameters needed for the evaluating the specular brdf visibility term.
    float m2 = roughness * roughness;
    float NdotL = saturate(dot(N, sgNDFWS.normal));
    float NdotV = max(1e-5f, abs(dot(N, V)));
    float3 H = normalize(sgNDFWS.normal + V);
    float NdotH = saturate(dot(sgNDFWS.normal, H));
 
    // Visibility term evaluated at the center of our warped BRDF lobe.
    specular *= GGX_V1(m2, NdotL) * GGX_V1(m2, NdotV);
 
    // // Fresnel evaluated at the center of our warped BRDF lobe.
    // const float F0 = 0.9f;// 0.04f; // TODO: Pass in F0, or evaluate F0 on the fly when compositing this buffer in the light loop.
    // specular *= pow((1.0f - NdotH), 5) * (1.0f - F0) + F0;
 
    // Cosine term evaluated at the center of the BRDF lobe
    specular *= NdotL;

    float occlusion = 1.0f - saturate(specular);

    occlusion = ApplyInfluenceFalloff(occlusion, ComputeInfluenceFalloff(occluderFromSurfaceDistance, GetOccluderInfluenceRadiusWS(data)));

    return occlusion;
}

// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-4-specular-lighting-from-an-sg-light-source/
float EvaluateCapsuleSpecularOcclusionASGOccluderSGBRDF(EllipsoidOccluderData data, float3 positionWS, float3 N, float3 V, float roughness, float4 dirAndLength)
{
    float3 occluderFromSurfaceDirectionWS;
    float occluderFromSurfaceDistance;
    ComputeDirectionAndDistanceFromStartAndEnd(positionWS, GetOccluderPositionRWS(data), occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);

    AnisotropicSphericalGaussian asgOccluder = AnisotropicSphericalGaussianFromEllipsoidOccluderData(data, occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);
    
    // TODO: Warping, and majority of specular BRDF evaluation can happen once, rather than per occluder.
    SphericalGaussian sgNDFHS = SphericalGaussianFromNDFApproximate(N, roughness);
    SphericalGaussian sgNDFWS = SphericalGaussianWarpWSFromHS(sgNDFHS, V);

    // Closed form convolution of occluder ASG approximation and specular BRDF NDF term SG approximation.
    float specular = AnisotropicSphericalGaussianInnerProductSG(asgOccluder, sgNDFWS);

    // Parameters needed for the evaluating the specular brdf visibility term.
    float m2 = roughness * roughness;
    float NdotL = saturate(dot(N, sgNDFWS.normal));
    float NdotV = max(1e-5f, abs(dot(N, V)));
    float3 H = normalize(sgNDFWS.normal + V);
    float NdotH = saturate(dot(sgNDFWS.normal, H));
 
    // Visibility term evaluated at the center of our warped BRDF lobe.
    specular *= GGX_V1(m2, NdotL) * GGX_V1(m2, NdotV);
 
    // // Fresnel evaluated at the center of our warped BRDF lobe.
    // const float F0 = 0.9f;// 0.04f; // TODO: Pass in F0, or evaluate F0 on the fly when compositing this buffer in the light loop.
    // specular *= pow((1.0f - NdotH), 5) * (1.0f - F0) + F0;
 
    // Cosine term evaluated at the center of the BRDF lobe
    specular *= NdotL;

    float occlusion = 1.0f - saturate(specular);

    occlusion = ApplyInfluenceFalloff(occlusion, ComputeInfluenceFalloff(occluderFromSurfaceDistance, GetOccluderInfluenceRadiusWS(data)));

    return occlusion;
}

// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-4-specular-lighting-from-an-sg-light-source/
float EvaluateCapsuleSpecularOcclusionSGOccluderASGBRDF(EllipsoidOccluderData data, float3 positionWS, float3 N, float3 V, float roughness, float4 dirAndLength)
{
    float3 occluderFromSurfaceDirectionWS;
    float occluderFromSurfaceDistance;
    ComputeDirectionAndDistanceFromStartAndEnd(positionWS, GetOccluderPositionRWS(data), occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);

    SphericalGaussian sgOccluder = SphericalGaussianFromEllipsoidOccluderData(data, occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);
    
    // TODO: Warping, and majority of specular BRDF evaluation can happen once, rather than per occluder.
    SphericalGaussian sgNDFHS = SphericalGaussianFromNDFApproximate(N, roughness);
    AnisotropicSphericalGaussian asgNDFWS = AnisotropicSphericalGaussianWarpWSFromHS(sgNDFHS, V);

    // Closed form convolution of occluder SG approximation and specular BRDF NDF term SG approximation.
    float specular = AnisotropicSphericalGaussianInnerProductSG(asgNDFWS, sgOccluder);

    // Parameters needed for the evaluating the specular brdf visibility term.
    float m2 = roughness * roughness;
    float NdotL = saturate(dot(N, asgNDFWS.normal));
    float NdotV = max(1e-5f, abs(dot(N, V)));
    float3 H = normalize(asgNDFWS.normal + V);
    float NdotH = saturate(dot(asgNDFWS.normal, H));
 
    // Visibility term evaluated at the center of our warped BRDF lobe.
    specular *= GGX_V1(m2, NdotL) * GGX_V1(m2, NdotV);
 
    // // Fresnel evaluated at the center of our warped BRDF lobe.
    // const float F0 = 0.9f;// 0.04f; // TODO: Pass in F0, or evaluate F0 on the fly when compositing this buffer in the light loop.
    // specular *= pow((1.0f - NdotH), 5) * (1.0f - F0) + F0;
 
    // Cosine term evaluated at the center of the BRDF lobe
    specular *= NdotL;

    float occlusion = 1.0f - saturate(specular);

    occlusion = ApplyInfluenceFalloff(occlusion, ComputeInfluenceFalloff(occluderFromSurfaceDistance, GetOccluderInfluenceRadiusWS(data)));

    return occlusion;
}

// https://mynameismjp.wordpress.com/2016/10/09/sg-series-part-4-specular-lighting-from-an-sg-light-source/
float EvaluateCapsuleSpecularOcclusionASGOccluderASGBRDF(EllipsoidOccluderData data, float3 positionWS, float3 N, float3 V, float roughness, float4 dirAndLength)
{
    float3 occluderFromSurfaceDirectionWS;
    float occluderFromSurfaceDistance;
    ComputeDirectionAndDistanceFromStartAndEnd(positionWS, GetOccluderPositionRWS(data), occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);

    AnisotropicSphericalGaussian asgOccluder = AnisotropicSphericalGaussianFromEllipsoidOccluderData(data, occluderFromSurfaceDirectionWS, occluderFromSurfaceDistance);
    
    // TODO: Warping, and majority of specular BRDF evaluation can happen once, rather than per occluder.
    SphericalGaussian sgNDFHS = SphericalGaussianFromNDFApproximate(N, roughness);
    AnisotropicSphericalGaussian asgNDFWS = AnisotropicSphericalGaussianWarpWSFromHS(sgNDFHS, V);

    // Closed form convolution of occluder SG approximation and specular BRDF NDF term SG approximation.
    float specular = AnisotropicSphericalGaussianInnerProductASG(asgNDFWS, asgOccluder);

    // Parameters needed for the evaluating the specular brdf visibility term.
    float m2 = roughness * roughness;
    float NdotL = saturate(dot(N, asgNDFWS.normal));
    float NdotV = max(1e-5f, abs(dot(N, V)));
    float3 H = normalize(asgNDFWS.normal + V);
    float NdotH = saturate(dot(asgNDFWS.normal, H));
 
    // Visibility term evaluated at the center of our warped BRDF lobe.
    specular *= GGX_V1(m2, NdotL) * GGX_V1(m2, NdotV);
 
    // // Fresnel evaluated at the center of our warped BRDF lobe.
    // const float F0 = 0.9f;// 0.04f; // TODO: Pass in F0, or evaluate F0 on the fly when compositing this buffer in the light loop.
    // specular *= pow((1.0f - NdotH), 5) * (1.0f - F0) + F0;
 
    // Cosine term evaluated at the center of the BRDF lobe
    specular *= NdotL;

    float occlusion = 1.0f - saturate(specular);

    occlusion = ApplyInfluenceFalloff(occlusion, ComputeInfluenceFalloff(occluderFromSurfaceDistance, GetOccluderInfluenceRadiusWS(data)));

    return occlusion;
}

float EvaluateCapsuleSpecularOcclusion(EllipsoidOccluderData data, float3 positionWS, float3 N, float3 V, float roughness, float4 dirAndLength)
{
    // return EvaluateCapsuleAmbientOcclusionSphericalGaussianReference(data, positionWS, N, V, roughness, dirAndLength);
    // return EvaluateCapsuleAmbientOcclusion(data, positionWS, N, dirAndLength);
#if 0
    return EvaluateCapsuleSpecularOcclusionSGOccluderSGBRDF(data, positionWS, N, V, roughness, dirAndLength);
#elif 1
    return EvaluateCapsuleSpecularOcclusionASGOccluderSGBRDF(data, positionWS, N, V, roughness, dirAndLength);
#elif 0
    return EvaluateCapsuleSpecularOcclusionSGOccluderASGBRDF(data, positionWS, N, V, roughness, dirAndLength);
#elif 0
    return EvaluateCapsuleSpecularOcclusionASGOccluderASGBRDF(data, positionWS, N, V, roughness, dirAndLength);
#endif
}

float EvaluateCapsuleShadow(EllipsoidOccluderData data, float3 positionWS, float3 N, float4 dirAndLength)
{
    // For now assuming just directional light.
    return 1.0f;
}

// --------------------------------------------
// Accumulation functions
// --------------------------------------------
// Functions used to accumulate results coming from different capsules.
// Min should be a safe bet, but abstract away in case more complex accumulation is required.

float AccumulateCapsuleAmbientOcclusion(float prevAO, float capsuleAO)
{
    return max(prevAO, capsuleAO);
    return min(prevAO, capsuleAO);
}

float AccumulateCapsuleSpecularOcclusion(float prevSpecOcc, float capsuleSpecOcc)
{
    return prevSpecOcc * capsuleSpecOcc;
}

float AccumulateCapsuleShadow(float prevShadow, float capsuleShadow)
{
    return min(prevShadow, capsuleShadow);
}

// --------------------------------------------
// Main evaluation function
// --------------------------------------------
// This is the main loop through the capsule data. To change the intersection behaviour just modify the functions above.
// Should be responsability of the caller to avoid calling this when evaluationFlags == CAPSULEOCCLUSIONTYPE_NONE

void EvaluateCapsuleOcclusion(uint evaluationFlags,
                              PositionInputs posInput,
                              float3 N,
                              float3 V,
                              float roughness,
                              inout float ambientOcclusion,
                              inout float specularOcclusion,
                              inout float shadow)
{
    uint sphereCount, sphereStart;

#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    GetCountAndStart(posInput, LIGHTCATEGORY_CAPSULE_OCCLUDER, sphereStart, sphereCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
    sphereCount = /* TO ADD FIXED COUNT */ ; 
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

            float4 dirAndLen = GetDataForSphereIntersection(s_capsuleData);

            if (evaluationFlags & CAPSULEOCCLUSIONTYPE_AMBIENT_OCCLUSION)
            {
                float capsuleAO = EvaluateCapsuleAmbientOcclusion(s_capsuleData, posInput.positionWS, N, dirAndLen);
                ambientOcclusion = AccumulateCapsuleAmbientOcclusion(ambientOcclusion, capsuleAO);
            }

            if (evaluationFlags & CAPSULEOCCLUSIONTYPE_SPECULAR_OCCLUSION)
            {
                float capsuleSpecOcc = EvaluateCapsuleSpecularOcclusion(s_capsuleData, posInput.positionWS, N, V, roughness, dirAndLen);
                specularOcclusion = AccumulateCapsuleSpecularOcclusion(specularOcclusion, capsuleSpecOcc);
            }

            if (evaluationFlags & CAPSULEOCCLUSIONTYPE_DIRECTIONAL_SHADOWS)
            {
                float capsuleShadow = EvaluateCapsuleShadow(s_capsuleData, posInput.positionWS, N, dirAndLen);
                shadow = AccumulateCapsuleShadow(shadow, capsuleShadow);
            }
        }
    }
}

#endif
