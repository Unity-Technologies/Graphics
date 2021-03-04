#ifndef TESSELLATION_CGINC_INCLUDED
#define TESSELLATION_CGINC_INCLUDED

#include "UnityShaderVariables.cginc"

// ---- utility functions

float UnityCalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess)
{
    float3 wpos = mul(unity_ObjectToWorld,vertex).xyz;
    float dist = distance (wpos, _WorldSpaceCameraPos);
    float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
    return f;
}

float4 UnityCalcTriEdgeTessFactors (float3 triVertexFactors)
{
    float4 tess;
    tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
    tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
    tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
    tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
    return tess;
}

float UnityCalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen)
{
    // distance to edge center
    float dist = distance (0.5 * (wpos0+wpos1), _WorldSpaceCameraPos);
    // length of the edge
    float len = distance(wpos0, wpos1);
    // edgeLen is approximate desired size in pixels
    float f = max(len * _ScreenParams.y / (edgeLen * dist), 1.0);
    return f;
}

float UnityDistanceFromPlane (float3 pos, float4 plane)
{
    float d = dot (float4(pos,1.0f), plane);
    return d;
}


// Returns true if triangle with given 3 world positions is outside of camera's view frustum.
// cullEps is distance outside of frustum that is still considered to be inside (i.e. max displacement)
bool UnityWorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps)
{
    float4 planeTest;

    // left
    planeTest.x = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[0]) > -cullEps) ? 1.0f : 0.0f );
    // right
    planeTest.y = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[1]) > -cullEps) ? 1.0f : 0.0f );
    // top
    planeTest.z = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[2]) > -cullEps) ? 1.0f : 0.0f );
    // bottom
    planeTest.w = (( UnityDistanceFromPlane(wpos0, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos1, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f ) +
                  (( UnityDistanceFromPlane(wpos2, unity_CameraWorldClipPlanes[3]) > -cullEps) ? 1.0f : 0.0f );

    // has to pass all 4 plane tests to be visible
    return !all (planeTest);
}



// ---- functions that compute tessellation factors


// Distance based tessellation:
// Tessellation level is "tess" before "minDist" from camera, and linearly decreases to 1
// up to "maxDist" from camera.
float4 UnityDistanceBasedTess (float4 v0, float4 v1, float4 v2, float minDist, float maxDist, float tess)
{
    float3 f;
    f.x = UnityCalcDistanceTessFactor (v0,minDist,maxDist,tess);
    f.y = UnityCalcDistanceTessFactor (v1,minDist,maxDist,tess);
    f.z = UnityCalcDistanceTessFactor (v2,minDist,maxDist,tess);

    return UnityCalcTriEdgeTessFactors (f);
}

// Desired edge length based tessellation:
// Approximate resulting edge length in pixels is "edgeLength".
// Does not take viewing FOV into account, just flat out divides factor by distance.
float4 UnityEdgeLengthBasedTess (float4 v0, float4 v1, float4 v2, float edgeLength)
{
    float3 pos0 = mul(unity_ObjectToWorld,v0).xyz;
    float3 pos1 = mul(unity_ObjectToWorld,v1).xyz;
    float3 pos2 = mul(unity_ObjectToWorld,v2).xyz;
    float4 tess;
    tess.x = UnityCalcEdgeTessFactor (pos1, pos2, edgeLength);
    tess.y = UnityCalcEdgeTessFactor (pos2, pos0, edgeLength);
    tess.z = UnityCalcEdgeTessFactor (pos0, pos1, edgeLength);
    tess.w = (tess.x + tess.y + tess.z) / 3.0f;
    return tess;
}


// Same as UnityEdgeLengthBasedTess, but also does patch frustum culling:
// patches outside of camera's view are culled before GPU tessellation. Saves some wasted work.
float4 UnityEdgeLengthBasedTessCull (float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement)
{
    float3 pos0 = mul(unity_ObjectToWorld,v0).xyz;
    float3 pos1 = mul(unity_ObjectToWorld,v1).xyz;
    float3 pos2 = mul(unity_ObjectToWorld,v2).xyz;
    float4 tess;

    if (UnityWorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement))
    {
        tess = 0.0f;
    }
    else
    {
        tess.x = UnityCalcEdgeTessFactor (pos1, pos2, edgeLength);
        tess.y = UnityCalcEdgeTessFactor (pos2, pos0, edgeLength);
        tess.z = UnityCalcEdgeTessFactor (pos0, pos1, edgeLength);
        tess.w = (tess.x + tess.y + tess.z) / 3.0f;
    }
    return tess;
}



#endif // TESSELLATION_CGINC_INCLUDED
