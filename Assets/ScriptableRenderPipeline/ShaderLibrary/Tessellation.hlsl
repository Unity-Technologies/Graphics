#define TESSELLATION_INTERPOLATE_BARY(name, bary) ouput.name = input0.name * bary.x +  input1.name * bary.y +  input2.name * bary.z

// TODO: Move in geomtry.hlsl
float3 ProjectPointOnPlane(float3 position, float3 planePosition, float3 planeNormal)
{
    return position - (dot(position - planePosition, planeNormal) * planeNormal);
}

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

// Return true if the triangle must be culled
// backFaceCullEpsilon is the threshold of the dot product between view and normal ( < 0 mean we cull)
bool BackFaceCullTriangle(float3 p0, float3 p1, float3 p2, float backFaceCullEpsilon, float3 cameraPosWS)
{
    float3 edge0 = p1 - p0;
    float3 edge2 = p2 - p0;

    float3 N = normalize(cross(edge0, edge2));
    float3 midpoint = (p0 + p1 + p2) / 3.0;
    float3 V = normalize(cameraPosWS - midpoint);

    return (dot(V, N) < backFaceCullEpsilon) ? true : false;
}

float2 GetScreenSpacePosition(float3 positionWS, float4x4 viewProjectionMatrix, float4 screenParams)
{
    float4 positionCS = mul(viewProjectionMatrix, float4(positionWS, 1.0));
    float2 positionSS = positionCS.xy / positionCS.w;

    // TODO: Check if we need to invert y
    return (positionSS * 0.5 + 0.5) * float2(screenParams.x, -screenParams.y);
}

// Compute both screen and distance based adaptation - return factor between 0 and 1
float3 GetScreenSpaceTessFactor(float3 p0, float3 p1, float3 p2, float4x4 viewProjectionMatrix, float4 screenParams, float triangleSize)
{
    // Get screen space adaptive scale factor
    float2 edgeScreenPosition0 = GetScreenSpacePosition(p0, viewProjectionMatrix, screenParams);
    float2 edgeScreenPosition1 = GetScreenSpacePosition(p1, viewProjectionMatrix, screenParams);
    float2 edgeScreenPosition2 = GetScreenSpacePosition(p2, viewProjectionMatrix, screenParams);

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

    // TODO: Move to camera relative and change distance to length
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

float4 CalcTriEdgeTessFactors(float3 triVertexFactors)
{
    float4 tess;
    tess.x = triVertexFactors.x;
    tess.y = triVertexFactors.y;
    tess.z = triVertexFactors.z;
    tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0;

    return tess;
}

// TODO: Move in geomtry.hlsl
float DistanceFromPlane(float3 pos, float4 plane)
{
    float d = dot(float4(pos, 1.0), plane);
    return d;
}

// Returns true if triangle with given 3 world positions is outside of camera's view frustum.
// cullEps is distance outside of frustum that is still considered to be inside (i.e. max displacement)
bool WorldViewFrustumCull(float3 p0, float3 p1, float3 p2, float cullEps, float4 cameraWorldClipPlanes[4])
{
    float4 planeTest;

    // left
    planeTest.x =   ((DistanceFromPlane(p0, cameraWorldClipPlanes[0]) > -cullEps) ? 1.0 : 0.0) +
                    ((DistanceFromPlane(p1, cameraWorldClipPlanes[0]) > -cullEps) ? 1.0 : 0.0) +
                    ((DistanceFromPlane(p2, cameraWorldClipPlanes[0]) > -cullEps) ? 1.0 : 0.0);
    // right
    planeTest.y =   ((DistanceFromPlane(p0, cameraWorldClipPlanes[1]) > -cullEps) ? 1.0 : 0.0) +
                    ((DistanceFromPlane(p1, cameraWorldClipPlanes[1]) > -cullEps) ? 1.0 : 0.0) +
                    ((DistanceFromPlane(p2, cameraWorldClipPlanes[1]) > -cullEps) ? 1.0 : 0.0);
    // top
    planeTest.z =   ((DistanceFromPlane(p0, cameraWorldClipPlanes[2]) > -cullEps) ? 1.0 : 0.0) +
                    ((DistanceFromPlane(p1, cameraWorldClipPlanes[2]) > -cullEps) ? 1.0 : 0.0) +
                    ((DistanceFromPlane(p2, cameraWorldClipPlanes[2]) > -cullEps) ? 1.0 : 0.0);
    // bottom
    planeTest.w =   ((DistanceFromPlane(p0, cameraWorldClipPlanes[3]) > -cullEps) ? 1.0 : 0.0) +
                    ((DistanceFromPlane(p1, cameraWorldClipPlanes[3]) > -cullEps) ? 1.0 : 0.0) +
                    ((DistanceFromPlane(p2, cameraWorldClipPlanes[3]) > -cullEps) ? 1.0 : 0.0);

    // has to pass all 4 plane tests to be visible
    return !all(planeTest);
}
