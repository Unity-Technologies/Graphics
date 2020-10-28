#ifndef EYE_UTILS_HLSL
#define EYE_UTILS_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

void GetScleraUVLocation(float3 positionOS, out float2 scleraUV)
{
    scleraUV =  positionOS.xy + float2(0.5, 0.5);
}

void GetIrisUVLocation(float3 positionOS, float irisRadius, out float2 irisUV)
{
    float2 irisUVCentered = positionOS.xy / irisRadius;
    irisUV = (irisUVCentered * 0.5 + float2(0.5, 0.5));
}

void DebugSurfaceType(float3 positionOS, float3 eyeColor, float irisRadius, float pupilRadius, bool active, out float3 surfaceColor)
{
    float pixelRadius = length(positionOS.xy);
    bool isSclera = pixelRadius > irisRadius;
    bool isPupil = !isSclera && length(positionOS.xy / irisRadius) < pupilRadius;
    surfaceColor = active ? (isSclera ? 0.0 : (isPupil ? 1.0 : eyeColor)) : eyeColor;
}

void ScleraOrIris_float(float3 positionOS, float irisRadius, out float surfaceType)
{
    float osRadius2 = (positionOS.x * positionOS.x + positionOS.y * positionOS.y);
    surfaceType = osRadius2 > (irisRadius * irisRadius) ? 0.0 : 1.0;
}

void CirclePupilAnimation(float2 irusUV, float pupilRadius, float pupilAperture, float minimalPupilAperture, float maximalPupilAperture, out float2 animatedIrisUV)
{
    // Compute the normalized iris position
    float2 irisUVCentered = (irusUV - 0.5f) * 2.0f;

    // Compute the radius of the point inside the eye
    float localIrisRadius = length(irisUVCentered);

    // First based on the pupil aperture, let's define the new position of the pupil
    float newPupilRadius = pupilAperture > 0.5 ? lerp(pupilRadius, maximalPupilAperture, (pupilAperture - 0.5) * 2.0) : lerp(minimalPupilAperture, pupilRadius, pupilAperture * 2.0);

    // If we are inside the pupil
    float newIrisRadius = localIrisRadius < newPupilRadius ? ((pupilRadius / newPupilRadius) * localIrisRadius) : 1.0 - ((1.0 - pupilRadius) / (1.0 - newPupilRadius)) * (1.0 - localIrisRadius);
    animatedIrisUV = irisUVCentered / localIrisRadius * newIrisRadius;

    // Convert it back to UV space.
    animatedIrisUV = (animatedIrisUV * 0.5 + float2(0.5, 0.5));
}

void CorneaRefraction(float3 positionOS, float3 viewDirectionOS, float3 corneaNormalOS, float corneaIOR, float irisPlaneOffset, out float3 refractedPositionOS)
{
    // Compute the refracted 
    float eta = 1.0 / (corneaIOR);
    corneaNormalOS = normalize(corneaNormalOS);
    viewDirectionOS = -normalize(viewDirectionOS);
    float3 refractedViewDirectionOS = refract(viewDirectionOS, corneaNormalOS, eta);

    // Find the distance to intersection point
    float t = -(positionOS.z + irisPlaneOffset) / refractedViewDirectionOS.z;

    // Output the refracted point in OS
    refractedPositionOS = float3(refractedViewDirectionOS.z < 0 ? positionOS.xy + refractedViewDirectionOS.xy * t: float2(1.5, 1.5), 0.0);
}

void IrisOutOfBoundColorClamp(float2 irisUV, float3 irisColor, float3 colorClamp, out float3 outputColor)
{
    outputColor = (irisUV.x < 0.0 || irisUV.y < 0.0 || irisUV.x > 1.0 || irisUV.y > 1.0) ? colorClamp : irisColor;
}

void IrisOffset(float2 irisUV, float2 irisOffset, out float2 displacedIrisUV)
{
    displacedIrisUV = (irisUV + irisOffset);
}

void IrisLimbalRing(float2 irisUV, float3 viewOS, float limbalRingSize, float limbalRingFade, float limbalRingItensity, out float limbalRingFactor)
{
    float NdotV = dot(float3(0.0, 0.0, 1.0), viewOS);

    // Compute the normalized iris position
    float2 irisUVCentered = (irisUV - 0.5f) * 2.0f;

    // Compute the radius of the point inside the eye
    float localIrisRadius = length(irisUVCentered);
    limbalRingFactor = localIrisRadius > (1.0 - limbalRingSize) ? lerp(0.1, 1.0, saturate(1.0 - localIrisRadius) / limbalRingSize) : 1.0;
    limbalRingFactor = PositivePow(limbalRingFactor, limbalRingItensity);
    limbalRingFactor = lerp(limbalRingFactor, PositivePow(limbalRingFactor, limbalRingFade), 1.0 - NdotV);
}

void ScleraLimbalRing(float3 positionOS, float3 viewOS, float irisRadius, float limbalRingSize, float limbalRingFade, float limbalRingItensity, out float limbalRingFactor)
{
    float NdotV = dot(float3(0.0, 0.0, 1.0), viewOS);
    // Compute the radius of the point inside the eye
    float scleraRadius = length(positionOS.xy);
    limbalRingFactor = scleraRadius > irisRadius ? (scleraRadius > (limbalRingSize + irisRadius) ? 1.0 : lerp(0.5, 1.0, (scleraRadius - irisRadius) / (limbalRingSize))) : 1.0;
    limbalRingFactor = PositivePow(limbalRingFactor, limbalRingItensity);
    limbalRingFactor = lerp(limbalRingFactor, PositivePow(limbalRingFactor, limbalRingFade), 1.0 - NdotV);
}

void ScleraIrisBlend(float3 scleraColor, float3 scleraNormal, float scleraSmoothness,
                            float3 irisColor, float3 irisNormal, float corneaSmoothness,
                            float irisRadius, 
                            float3 positionOS,
                            float diffusionProfileSclera, float diffusionProfileIris,
                            out float3 eyeColor, out float surfaceMask,
                            out float3 diffuseNormal, out float3 specularNormal, out float eyeSmoothness, out float surfaceDiffusionProfile)
{
    float osRadius = length(positionOS.xy);
    float innerBlendRegionRadius = irisRadius - 0.02;
    float outerBlendRegionRadius = irisRadius + 0.02;
    float blendLerpFactor = 1.0 - (osRadius - irisRadius) / (0.04);
    blendLerpFactor = pow(blendLerpFactor, 8.0);
    blendLerpFactor = 1.0 - blendLerpFactor;
    surfaceMask = (osRadius > outerBlendRegionRadius) ? 0.0 : ((osRadius < irisRadius) ? 1.0 : (lerp(1.0, 0.0, blendLerpFactor)));
    eyeColor = lerp(scleraColor, irisColor, surfaceMask);
    diffuseNormal = lerp(scleraNormal, irisNormal, surfaceMask);
    specularNormal = lerp(scleraNormal, float3(0.0, 0.0, 1.0), surfaceMask);
    eyeSmoothness = lerp(scleraSmoothness, corneaSmoothness, surfaceMask);
    surfaceDiffusionProfile = lerp(diffusionProfileSclera, diffusionProfileIris, floor(surfaceMask));
}

#endif // EYE_UTILS_HLSL