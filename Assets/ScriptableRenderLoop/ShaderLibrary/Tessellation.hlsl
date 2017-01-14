#define TESSELLATION_INTERPOLATE_BARY(name, bary) ouput.name = input0.name * bary.x +  input1.name * bary.y +  input2.name * bary.z

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

// ---- utility functions
float CalcDistanceTessFactor(float3 positionWS, float minDist, float maxDist, float3 cameraPosWS)
{
    float dist = distance(positionWS, cameraPosWS);
    float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0);
    return f;
}

float4 UnityCalcTriEdgeTessFactors(float3 triVertexFactors)
{
    float4 tess;
    tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
    tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
    tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
    tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
    return tess;
}

/*
float UnityCalcEdgeTessFactor(float3 wpos0, float3 wpos1, float edgeLen)
{
    // distance to edge center
    float dist = distance(0.5 * (wpos0 + wpos1), _WorldSpaceCameraPos);
    // length of the edge
    float len = distance(wpos0, wpos1);
    // edgeLen is approximate desired size in pixels
    float f = max(len * _ScreenParams.y / (edgeLen * dist), 1.0);
    return f;
}

float UnityDistanceFromPlane(float3 pos, float4 plane)
{
    float d = dot(float4(pos, 1.0f), plane);
    return d;
}

// Returns true if triangle with given 3 world positions is outside of camera's view frustum.
// cullEps is distance outside of frustum that is still considered to be inside (i.e. max displacement)
bool UnityWorldViewFrustumCull(float3 wpos0, float3 wpos1, float3 wpos2, float cullEps)
{
    float4 planeTest;

    // left
    planeTest.x = ((UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f) +
        ((UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f) +
        ((UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f);
    // right
    planeTest.y = ((UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f) +
        ((UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f) +
        ((UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f);
    // top
    planeTest.z = ((UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f) +
        ((UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f) +
        ((UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f);
    // bottom
    planeTest.w = ((UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f) +
        ((UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f) +
        ((UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f);

    // has to pass all 4 plane tests to be visible
    return !all(planeTest);
}
*/

// ---- functions that compute tessellation factors
// Distance based tessellation:
// Tessellation level is "tess" before "minDist" from camera, and linearly decreases to 1
// up to "maxDist" from camera.
float4 DistanceBasedTess(float3 p0, float3 p1, float3 p2, float minDist, float maxDist, float3 cameraPosWS)
{
    float3 f;
    f.x = CalcDistanceTessFactor(p0, minDist, maxDist, cameraPosWS);
    f.y = CalcDistanceTessFactor(p1, minDist, maxDist, cameraPosWS);
    f.z = CalcDistanceTessFactor(p2, minDist, maxDist, cameraPosWS);
    return UnityCalcTriEdgeTessFactors(f);
}

/*
// Desired edge length based tessellation:
// Approximate resulting edge length in pixels is "edgeLength".
// Does not take viewing FOV into account, just flat out divides factor by distance.
float4 UnityEdgeLengthBasedTess(float4 v0, float4 v1, float4 v2, float edgeLength)
{
    float3 pos0 = mul(unity_ObjectToWorld, v0).xyz;
        float3 pos1 = mul(unity_ObjectToWorld, v1).xyz;
        float3 pos2 = mul(unity_ObjectToWorld, v2).xyz;
        float4 tess;
    tess.x = UnityCalcEdgeTessFactor(pos1, pos2, edgeLength);
    tess.y = UnityCalcEdgeTessFactor(pos2, pos0, edgeLength);
    tess.z = UnityCalcEdgeTessFactor(pos0, pos1, edgeLength);
    tess.w = (tess.x + tess.y + tess.z) / 3.0f;
    return tess;
}

// Same as UnityEdgeLengthBasedTess, but also does patch frustum culling:
// patches outside of camera's view are culled before GPU tessellation. Saves some wasted work.
float4 UnityEdgeLengthBasedTessCull(float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement)
{
    float3 pos0 = mul(unity_ObjectToWorld, v0).xyz;
        float3 pos1 = mul(unity_ObjectToWorld, v1).xyz;
        float3 pos2 = mul(unity_ObjectToWorld, v2).xyz;
        float4 tess;
    if (UnityWorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement))
    {
        tess = 0.0f;
    }
    else
    {
        tess.x = UnityCalcEdgeTessFactor(pos1, pos2, edgeLength);
        tess.y = UnityCalcEdgeTessFactor(pos2, pos0, edgeLength);
        tess.z = UnityCalcEdgeTessFactor(pos0, pos1, edgeLength);
        tess.w = (tess.x + tess.y + tess.z) / 3.0f;
    }
    return tess;
}
*/
