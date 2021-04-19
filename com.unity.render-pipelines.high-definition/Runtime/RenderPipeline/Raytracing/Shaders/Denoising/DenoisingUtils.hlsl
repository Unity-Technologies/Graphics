
#define MAX_REPROJECTION_DISTANCE 0.1
#define MAX_PIXEL_TOLERANCE 4

float ComputeMaxReprojectionWorldRadius(float3 positionWS, float3 normalWS, float pixelSpreadAngleTangent, float maxDistance, float pixelTolerance)
{
    const float3 viewWS = GetWorldSpaceNormalizeViewDir(positionWS);
    float parallelPixelFootPrint = pixelSpreadAngleTangent * length(positionWS);
    float realPixelFootPrint = parallelPixelFootPrint / abs(dot(normalWS, viewWS));
    return max(maxDistance, realPixelFootPrint * pixelTolerance);
}

float ComputeMaxReprojectionWorldRadius(float3 positionWS, float3 normalWS, float pixelSpreadAngleTangent)
{
    return ComputeMaxReprojectionWorldRadius(positionWS, normalWS, pixelSpreadAngleTangent, MAX_REPROJECTION_DISTANCE, MAX_PIXEL_TOLERANCE);
}
