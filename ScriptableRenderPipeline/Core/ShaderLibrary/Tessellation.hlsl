#define TESSELLATION_INTERPOLATE_BARY(name, bary) ouput.name = input0.name * bary.x +  input1.name * bary.y +  input2.name * bary.z

// p0, p1, p2 triangle world position
// p0, p1, p2 triangle world vertex normal
float3 PhongTessellation(float3 positionWS, float3 p0, float3 p1, float3 p2, float3 n0, float3 n1, float3 n2, float3 baryCoords, float shape)
{
    float3 c0 = ProjectPointOnPlane(positionWS, p0, n0);
    float3 c1 = ProjectPointOnPlane(positionWS, p1, n1);
    float3 c2 = ProjectPointOnPlane(positionWS, p2, n2);

    float3 phongPositionWS = baryCoords.x * c0 + baryCoords.y * c1 + baryCoords.z * c2;

    return lerp(positionWS, phongPositionWS, shape);
}

// Reference: http://twvideo01.ubm-us.net/o1/vault/gdc10/slides/Bilodeau_Bill_Direct3D11TutorialTessellation.pdf

// Compute both screen and distance based adaptation - return factor between 0 and 1
float3 GetScreenSpaceTessFactor(float3 p0, float3 p1, float3 p2, float4x4 viewProjectionMatrix, float4 screenSize, float triangleSize)
{
    // Get screen space adaptive scale factor
    float2 edgeScreenPosition0 = ComputeNormalizedDeviceCoordinates(p0, viewProjectionMatrix) * screenSize.xy;
    float2 edgeScreenPosition1 = ComputeNormalizedDeviceCoordinates(p1, viewProjectionMatrix) * screenSize.xy;
    float2 edgeScreenPosition2 = ComputeNormalizedDeviceCoordinates(p2, viewProjectionMatrix) * screenSize.xy;

    float EdgeScale = 1.0 / triangleSize; // Edge size in reality, but name is simpler
    float3 tessFactor;
    tessFactor.x = saturate(distance(edgeScreenPosition1, edgeScreenPosition2) * EdgeScale);
    tessFactor.y = saturate(distance(edgeScreenPosition0, edgeScreenPosition2) * EdgeScale);
    tessFactor.z = saturate(distance(edgeScreenPosition0, edgeScreenPosition1) * EdgeScale);

    return tessFactor;
}

float3 GetDistanceBasedTessFactor(float3 p0, float3 p1, float3 p2, float3 cameraPosWS, float tessMinDist, float tessMaxDist)
{
    float3 edgePosition0 = 0.5 * (p1 + p2);
    float3 edgePosition1 = 0.5 * (p0 + p2);
    float3 edgePosition2 = 0.5 * (p0 + p1);

    // In case camera-relative rendering is enabled, 'cameraPosWS' is statically known to be 0,
    // so the compiler will be able to optimize distance() to length().
    float dist0 = distance(edgePosition0, cameraPosWS);
    float dist1 = distance(edgePosition1, cameraPosWS);
    float dist2 = distance(edgePosition2, cameraPosWS);

    // The saturate will handle the produced NaN in case min == max
    float fadeDist = tessMaxDist - tessMinDist;
    float3 tessFactor;
    tessFactor.x = saturate(1.0 - (dist0 - tessMinDist) / fadeDist);
    tessFactor.y = saturate(1.0 - (dist1 - tessMinDist) / fadeDist);
    tessFactor.z = saturate(1.0 - (dist2 - tessMinDist) / fadeDist);

    return tessFactor;
}

float4 CalcTriTessFactorsFromEdgeTessFactors(float3 triVertexFactors)
{
    float4 tess;
    tess.x = triVertexFactors.x;
    tess.y = triVertexFactors.y;
    tess.z = triVertexFactors.z;
    tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0;

    return tess;
}
