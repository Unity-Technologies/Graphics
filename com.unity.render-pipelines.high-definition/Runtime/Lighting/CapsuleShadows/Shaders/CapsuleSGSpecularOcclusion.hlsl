#ifndef  CAPSULE_SPECULAR_OCCLUSION
#define CAPSULE_SPECULAR_OCCLUSION

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/Shaders/CapsuleOcclusionData.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/Shaders/SphericalGaussian.hlsl"

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

    // Lerping between minor and major axis based on projection is a reasonable approximation for more distant occluders,
    // but breaks down when occluders major axis approaches contact with surface, as this significant increase in solid angle is not
    // fully taken into account.
    // In many situations, preserving the major axis size regardless of projection produces results that are closer to the ground truth,
    // particularly near contact. The downside of doing this is that when looking straight down the major axis, we still see stretching that we should not.
    // for now, simply apply an adhoc rescaling of the projected area to approximately handle both cases.
    float occluderRadiusMajorProjected = lerp(occluderRadiusMajor, occluderRadiusMinor, smoothstep(0.9f, 1.0f, abs(axisMajorTNormalScalar)));
    // float occluderRadiusMajorProjected = lerp(occluderRadiusMajor, occluderRadiusMinor, abs(axisMajorTNormalScalar));


    // tan(theta) == opposite / adjacent
    // theta == atan(opposite / adjacent)
    // cosTheta == cos(atan(opposite / adjacent))
    // cosTheta == cos(atan(occluderRadiusProjectedAverage / occluderFromSurfaceDistance))
    // cos(atan(x / y)) == rsqrt(x^2 / y^2 + 1)
    // cosTheta == rsqrt(occluderRadiusProjectedAverage^2 / occluderFromSurfaceDistance^2 + 1)
    //
    // Ultimately, the output of SphericalGaussianSharpnessFromAngleAndLogThreshold(cosTheta, log(amplitude), log(epsilon)))
    // ends up looking like an inverted parabola when graphed over occluderFromSurfaceDistance.
    // In the future, we should just fit a polynomial for all this math that returns sharpness for a given occluderRadius / occluderDistance.
    //
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
    float D = SphericalGaussianInnerProduct(sgNDFWS, sgOccluder);

    // Parameters needed for the evaluating the specular brdf visibility term.
    float m2 = roughness * roughness;
    float NdotL = saturate(dot(N, sgNDFWS.normal));
    float NdotV = max(1e-5f, abs(dot(N, V)));
    float3 H = normalize(sgNDFWS.normal + V);
    float NdotH = saturate(dot(sgNDFWS.normal, H));

    // Remaining BRDF terms evaluated at the center of our warped BRDF lobe as an approximation.
    // specular *= GGX_V1(m2, NdotL) * GGX_V1(m2, NdotV);
    float G = G_MaskingSmithGGX(NdotL, roughness) * G_MaskingSmithGGX(NdotV, roughness);
    G /= (4.0 * NdotL * NdotV);
    float F = 1.0f;
    float specular = F * D * G * NdotL;
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
    float D = AnisotropicSphericalGaussianInnerProductSG(asgOccluder, sgNDFWS);

    // Parameters needed for the evaluating the specular brdf visibility term.
    float m2 = roughness * roughness;
    float NdotL = saturate(dot(N, sgNDFWS.normal));
    float NdotV = max(1e-5f, abs(dot(N, V)));
    float3 H = normalize(sgNDFWS.normal + V);
    float NdotH = saturate(dot(sgNDFWS.normal, H));

    // Remaining BRDF terms evaluated at the center of our warped BRDF lobe as an approximation.
    // Note G_MaskingSmithGGX() implementation was causing presion issues where NdotV approaches zero.
    // Using the visibility variants which do not exhibit these precision issues. 
    float G = GGX_V1(m2, NdotL) * GGX_V1(m2, NdotV);
    // specular *= GGX_V1(m2, NdotL) * GGX_V1(m2, NdotV);
    // float G = G_MaskingSmithGGX(NdotL, roughness) * G_MaskingSmithGGX(NdotV, roughness);
    // G /= (4.0 * NdotL * NdotV);
    float F = 1.0f;
    float specular = F * D * G * NdotL;
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
    float D = AnisotropicSphericalGaussianInnerProductSG(asgNDFWS, sgOccluder);

    // Parameters needed for the evaluating the specular brdf visibility term.
    float m2 = roughness * roughness;
    float NdotL = saturate(dot(N, asgNDFWS.normal));
    float NdotV = max(1e-5f, abs(dot(N, V)));
    float3 H = normalize(asgNDFWS.normal + V);
    float NdotH = saturate(dot(asgNDFWS.normal, H));

    // Remaining BRDF terms evaluated at the center of our warped BRDF lobe as an approximation.
    // Note G_MaskingSmithGGX() implementation was causing presion issues where NdotV approaches zero.
    // Using the visibility variants which do not exhibit these precision issues. 
    float G = GGX_V1(m2, NdotL) * GGX_V1(m2, NdotV);
    // float G = G_MaskingSmithGGX(NdotL, roughness) * G_MaskingSmithGGX(NdotV, roughness);
    // G /= (4.0 * NdotL * NdotV);
    float F = 1.0f;
    float specular = F * D * G * NdotL;
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
    float D = AnisotropicSphericalGaussianInnerProductASG(asgNDFWS, asgOccluder);

    // Parameters needed for the evaluating the specular brdf visibility term.
    float m2 = roughness * roughness;
    float NdotL = saturate(dot(N, asgNDFWS.normal));
    float NdotV = max(1e-5f, abs(dot(N, V)));
    float3 H = normalize(asgNDFWS.normal + V);
    float NdotH = saturate(dot(asgNDFWS.normal, H));

    // Remaining BRDF terms evaluated at the center of our warped BRDF lobe as an approximation.
    // specular *= GGX_V1(m2, NdotL) * GGX_V1(m2, NdotV);
    float G = G_MaskingSmithGGX(NdotL, roughness) * G_MaskingSmithGGX(NdotV, roughness);
    G /= (4.0 * NdotL * NdotV);
    float F = 1.0f;
    float specular = F * D * G * NdotL;
    float occlusion = 1.0f - saturate(specular);

    occlusion = ApplyInfluenceFalloff(occlusion, ComputeInfluenceFalloff(occluderFromSurfaceDistance, GetOccluderInfluenceRadiusWS(data)));

    return occlusion;
}

#endif
