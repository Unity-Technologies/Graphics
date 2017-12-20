#define TESSELLATION_INTERPOLATE_BARY(name, bary) ouput.name = input0.name * bary.x +  input1.name * bary.y +  input2.name * bary.z

// p0, p1, p2 triangle world position
// p0, p1, p2 triangle world vertex normal
REAL3 PhongTessellation(REAL3 positionWS, REAL3 p0, REAL3 p1, REAL3 p2, REAL3 n0, REAL3 n1, REAL3 n2, REAL3 baryCoords, REAL shape)
{
    REAL3 c0 = ProjectPointOnPlane(positionWS, p0, n0);
    REAL3 c1 = ProjectPointOnPlane(positionWS, p1, n1);
    REAL3 c2 = ProjectPointOnPlane(positionWS, p2, n2);

    REAL3 phongPositionWS = baryCoords.x * c0 + baryCoords.y * c1 + baryCoords.z * c2;

    return lerp(positionWS, phongPositionWS, shape);
}

// Reference: http://twvideo01.ubm-us.net/o1/vault/gdc10/slides/Bilodeau_Bill_Direct3D11TutorialTessellation.pdf

// Compute both screen and distance based adaptation - return factor between 0 and 1
REAL3 GetScreenSpaceTessFactor(REAL3 p0, REAL3 p1, REAL3 p2, REAL4x4 viewProjectionMatrix, REAL4 screenSize, REAL triangleSize)
{
    // Get screen space adaptive scale factor
    REAL2 edgeScreenPosition0 = ComputeNormalizedDeviceCoordinates(p0, viewProjectionMatrix) * screenSize.xy;
    REAL2 edgeScreenPosition1 = ComputeNormalizedDeviceCoordinates(p1, viewProjectionMatrix) * screenSize.xy;
    REAL2 edgeScreenPosition2 = ComputeNormalizedDeviceCoordinates(p2, viewProjectionMatrix) * screenSize.xy;

    REAL EdgeScale = 1.0 / triangleSize; // Edge size in reality, but name is simpler
    REAL3 tessFactor;
    tessFactor.x = saturate(distance(edgeScreenPosition1, edgeScreenPosition2) * EdgeScale);
    tessFactor.y = saturate(distance(edgeScreenPosition0, edgeScreenPosition2) * EdgeScale);
    tessFactor.z = saturate(distance(edgeScreenPosition0, edgeScreenPosition1) * EdgeScale);

    return tessFactor;
}

REAL3 GetDistanceBasedTessFactor(REAL3 p0, REAL3 p1, REAL3 p2, REAL3 cameraPosWS, REAL tessMinDist, REAL tessMaxDist)
{
    REAL3 edgePosition0 = 0.5 * (p1 + p2);
    REAL3 edgePosition1 = 0.5 * (p0 + p2);
    REAL3 edgePosition2 = 0.5 * (p0 + p1);

    // In case camera-relative rendering is enabled, 'cameraPosWS' is statically known to be 0,
    // so the compiler will be able to optimize distance() to length().
    REAL dist0 = distance(edgePosition0, cameraPosWS);
    REAL dist1 = distance(edgePosition1, cameraPosWS);
    REAL dist2 = distance(edgePosition2, cameraPosWS);

    // The saturate will handle the produced NaN in case min == max
    REAL fadeDist = tessMaxDist - tessMinDist;
    REAL3 tessFactor;
    tessFactor.x = saturate(1.0 - (dist0 - tessMinDist) / fadeDist);
    tessFactor.y = saturate(1.0 - (dist1 - tessMinDist) / fadeDist);
    tessFactor.z = saturate(1.0 - (dist2 - tessMinDist) / fadeDist);

    return tessFactor;
}

REAL4 CalcTriTessFactorsFromEdgeTessFactors(REAL3 triVertexFactors)
{
    REAL4 tess;
    tess.x = triVertexFactors.x;
    tess.y = triVertexFactors.y;
    tess.z = triVertexFactors.z;
    tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0;

    return tess;
}
