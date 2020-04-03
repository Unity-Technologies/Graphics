#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

void GetScleraUVLocation_float(float3 positionOS, out float2 scleraUV)
{
    scleraUV =  positionOS.xy + float2(0.5, 0.5);
}

void GetIrisUVLocation_float(float3 positionOS, float irisRadius, out float2 irisUV)
{
    float2 irisUVCentered = positionOS.xy / irisRadius;
    irisUV = (irisUVCentered * 0.5 + float2(0.5, 0.5));
}

void ScleraOrCornea01_float(float3 positionOS, out float surfaceType)
{
    surfaceType = positionOS.z > 0.0 ? 1.0 : 0.0;
}

void ScleraOrIris01_float(float3 positionOS, float irisRadius, out float surfaceType)
{
    float osRadius2 = (positionOS.x * positionOS.x + positionOS.y * positionOS.y);
    surfaceType = osRadius2 > (irisRadius * irisRadius) ? 0.0 : 1.0;
}

void CirclePupilAnimation_float(float2 irusUV, float pupilRadius, float pupilAperture, float minimalPupilAperture, float maximalPupilAperture, out float2 animatedIrisUV)
{
    // Compute the normalized iris position
    float2 irisUVCentered = (irusUV - 0.5f) * 2.0f;

    // Compute the radius of the point inside the eye
    float localIrisRadius = length(irisUVCentered);

    // Define the relative position of the point w/r to the pupil position
    //float relativePointPosition = localIrisRadius > pupilRadius ? (localIrisRadius - pupilRadius) / (1.0 - pupilRadius) : (pupilRadius - localIrisRadius) / (pupilRadius);

    // First based on the pupil aperture, let's define the new position of the pupil
    float newPupilRadius = pupilAperture > 0.5 ? lerp(pupilRadius, maximalPupilAperture, (pupilAperture - 0.5) * 2.0) : lerp(minimalPupilAperture, pupilRadius, pupilAperture * 2.0);

    // If we are inside the pupil
    if (localIrisRadius < newPupilRadius)
    {
        float newIrisRadius = ((pupilRadius / newPupilRadius) * localIrisRadius);
        animatedIrisUV = irisUVCentered / localIrisRadius * newIrisRadius;
    }
    else
    {
        float newIrisRadius = 1.0 - ((1.0 - pupilRadius) / (1.0 - newPupilRadius)) * (1.0 - localIrisRadius);
        animatedIrisUV = irisUVCentered / localIrisRadius * newIrisRadius;
    }

    // Convert it back to UV space.
    animatedIrisUV = (animatedIrisUV * 0.5 + float2(0.5, 0.5));
}

void CorneaRefraction_float(float3 positionOS, float3 corneaNormalOS, float corneaIOR, out float2 refractedPositionOS)
{
    // Compute the refracted 
    float3 viewPositionOS = TransformWorldToObject(GetCameraRelativePositionWS(_WorldSpaceCameraPos));
    float3 viewDirectionOS = normalize(positionOS - viewPositionOS);
    float eta = 1.0 / corneaIOR;
    float3 refractedViewDirectionOS = refract(viewDirectionOS, corneaNormalOS, eta);

    // Find the distance to intersection point
    float t = - positionOS.z / refractedViewDirectionOS.z;

    // Output the refracted point in OS
    refractedPositionOS = positionOS + refractedViewDirectionOS * t;
}


// float pupilRadius, float maxPupilAperture, float pupilAperture, 