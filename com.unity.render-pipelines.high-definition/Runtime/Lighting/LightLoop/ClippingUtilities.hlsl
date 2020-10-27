#ifndef UNITY_CLIPPINGUTILITIES_INCLUDED
#define UNITY_CLIPPINGUTILITIES_INCLUDED

// (Sep 16, 2020)
// Improve the quality of generated code at the expense of readability.
// Remove when the shader compiler is clever enough to perform this optimization for us.
#define OBTUSE_COMPILER

struct ClipVertex
{
    float4 pt; // Homogeneous coordinate after perspective
    float  bc; // Boundary coordinate with respect to the plane 'p'
};

ClipVertex CreateClipVertex(uint p, float4 v)
{
    bool evenPlane = (p & 1) == 0;

    float c = v[p >> 1];
    float w = v.w;

    ClipVertex cv;

    cv.pt = v;
    cv.bc = evenPlane ? c : w - c; // dot(PlaneEquation, HapVertex);

    return cv;
}

float4 IntersectEdgeAgainstPlane(ClipVertex v0, ClipVertex v1)
{
    float alpha = saturate(v0.bc * rcp(v0.bc - v1.bc)); // Guaranteed to lie between 0 and 1

    return lerp(v0.pt, v1.pt, alpha);
}

void ClipPolygonAgainstPlane(uint p, uint srcBegin, uint srcSize,
                             inout float4 vertRingBuffer[MAX_CLIP_VERTS],
                             out uint dstBegin, out uint dstSize)
{
    dstBegin = srcBegin + srcSize; // Start at the end; we don't use modular arithmetic here
    dstSize  = 0;

    ClipVertex tailVert = CreateClipVertex(p, vertRingBuffer[(srcBegin + srcSize - 1) % MAX_CLIP_VERTS]);

#ifdef OBTUSE_COMPILER
    uint modSrcIdx = srcBegin % MAX_CLIP_VERTS;
    uint modDstIdx = dstBegin % MAX_CLIP_VERTS;
#endif

    for (uint j = srcBegin; j < (srcBegin + srcSize); j++)
    {
    #ifndef OBTUSE_COMPILER
        uint modSrcIdx = j % MAX_CLIP_VERTS;
    #endif
        ClipVertex leadVert = CreateClipVertex(p, vertRingBuffer[modSrcIdx]);

        // Execute Blinn's line clipping algorithm.
        // Classify the line segment. 4 cases:
        // 0. v0 out, v1 out -> add nothing
        // 1. v0 in,  v1 out -> add intersection
        // 2. v0 out, v1 in  -> add intersection, add v1
        // 3. v0 in,  v1 in  -> add v1
        // (bc >= 0) <-> in, (bc < 0) <-> out. Beware of -0.

        if ((tailVert.bc >= 0) != (leadVert.bc >= 0))
        {
            // The line segment is guaranteed to cross the plane.
            float4 clipVert = IntersectEdgeAgainstPlane(tailVert, leadVert);
        #ifndef OBTUSE_COMPILER
            uint modDstIdx = (dstBegin + dstSize++) % MAX_CLIP_VERTS;
        #endif
            vertRingBuffer[modDstIdx] = clipVert;
        #ifdef OBTUSE_COMPILER
            dstSize++;
            modDstIdx++;
            modDstIdx = (modDstIdx == MAX_CLIP_VERTS) ? 0 : modDstIdx;
        #endif
        }

        if (leadVert.bc >= 0)
        {
        #ifndef OBTUSE_COMPILER
            uint modDstIdx = (dstBegin + dstSize++) % MAX_CLIP_VERTS;
        #endif
            vertRingBuffer[modDstIdx] = leadVert.pt;
        #ifdef OBTUSE_COMPILER
            dstSize++;
            modDstIdx++;
            modDstIdx = (modDstIdx == MAX_CLIP_VERTS) ? 0 : modDstIdx;
        #endif
        }

    #ifdef OBTUSE_COMPILER
        modSrcIdx++;
        modSrcIdx = (modSrcIdx == MAX_CLIP_VERTS) ? 0 : modSrcIdx;
    #endif
        tailVert = leadVert; // Avoid recomputation and overwriting the vertex in the ring buffer
    }
}

#endif // UNITY_CLIPPINGUTILITIES_INCLUDED
