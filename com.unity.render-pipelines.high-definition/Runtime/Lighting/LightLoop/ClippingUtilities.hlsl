#ifndef UNITY_CLIPPINGUTILITIES_INCLUDED
#define UNITY_CLIPPINGUTILITIES_INCLUDED

float2 ClosestPointAaBb(float2 pt, float2 aaBbMinPt, float2 aaBbMaxPt)
{
    return clamp(pt, aaBbMinPt, aaBbMaxPt);
}

float SqDistToClosestPointAaBb(float2 pt, float2 aaBbMinPt, float2 aaBbMaxPt)
{
    float2 qt = ClosestPointAaBb(pt, aaBbMinPt, aaBbMaxPt);

    return dot(pt - qt, pt - qt);
}

struct ClipVertex
{
    float4 pt; // Homogeneous coordinate after perspective
    float  bc; // Boundary coordinate with respect to the plane 'p'
};

float4 CreateClipPlane(uint p, float3 cubeMin, float3 cubeMax)
{
    bool   evenPlane = (p & 1) == 0;
    float  s         = evenPlane ? 1 : -1;
    float3 distances = evenPlane ? cubeMin : cubeMax;
    float  d         = distances[p / 2];

    float3 n;
#if 0
    n        = 0;
    n[p / 2] = s; // Doesn't compile...
#else
    n.x = (0 == (p / 2)) ? s : 0;
    n.y = (1 == (p / 2)) ? s : 0;
    n.z = (2 == (p / 2)) ? s : 0;
#endif

    return float4(n, -s * d); // Normal points inwards
}

ClipVertex CreateClipVertex(float4 vert, float4 plane)
{
    ClipVertex cv;

    cv.pt = vert;
    cv.bc = dot(vert, plane);

    return cv;
}

float4 IntersectEdgeAgainstPlane(ClipVertex v0, ClipVertex v1)
{
    float alpha = saturate(v0.bc * rcp(v0.bc - v1.bc)); // Guaranteed to lie between 0 and 1

    return lerp(v0.pt, v1.pt, alpha);
}

#ifdef INCLUDE_SPECIFIC_CLIPPING_CODE
// The code below is specific to binned lighting.

// (Sep 16, 2020)
// Improve the quality of generated code at the expense of readability.
// Remove when the shader compiler is clever enough to perform this optimization for us.
#define OBTUSE_COMPILER

// Clipping a plane by a cube may produce a hexagon (6-gon).
// Clipping a hexagon by 4 planes (substitute a quad for the plane) may produce a decagon (10-gon).
#define MAX_CLIP_VERTS     (10)
#define NUM_VERTS          (8)
#define NUM_FACES          (6)
#define NUM_PLANES         (6)
#define THREADS_PER_GROUP  (64)
#define THREADS_PER_ENTITY (4) // Set to 1 for debugging
#define ENTITIES_PER_GROUP (THREADS_PER_GROUP / THREADS_PER_ENTITY)
#define VERTS_PER_GROUP    (NUM_VERTS * ENTITIES_PER_GROUP)
#define VERTS_PER_THREAD   (NUM_VERTS / THREADS_PER_ENTITY)
#define FACES_PER_THREAD   (DIV_ROUND_UP(NUM_FACES, THREADS_PER_ENTITY))
#define VERTS_PER_FACE     (4)

// All planes and faces are always in the standard order (see below).
// Near and far planes are swapped in the case of Z-reversal, but it does not change the algorithm.
#define FACE_LEFT   (1 << 0) // -X
#define FACE_RIGHT  (1 << 1) // +X
#define FACE_BOTTOM (1 << 2) // -Y
#define FACE_TOP    (1 << 3) // +Y
#define FACE_FRONT  (1 << 4) // -Z
#define FACE_BACK   (1 << 5) // +Z
#define FACE_MASK   ((1 << NUM_FACES) - 1)
#define PLANE_MASK  FACE_MASK // 6 culling planes, the same order

float3 GenerateVertexOfStandardCube(uint v)
{
    float3 p;

    p.x = ((v & 1) == 0) ? -1 : 1; // FACE_LEFT   : FACE_RIGHT
    p.y = ((v & 2) == 0) ? -1 : 1; // FACE_BOTTOM : FACE_TOP
    p.z = ((v & 4) == 0) ? -1 : 1; // FACE_FRONT  : FACE_BACK

    return p;
}

float3 GenerateVertexOfCustomCube(uint v, float3 cubeMin, float3 cubeMax)
{
    float3 p;

    p.x = ((v & 1) == 0) ? cubeMin.x : cubeMax.x; // FACE_LEFT   : FACE_RIGHT
    p.y = ((v & 2) == 0) ? cubeMin.y : cubeMax.y; // FACE_BOTTOM : FACE_TOP
    p.z = ((v & 4) == 0) ? cubeMin.z : cubeMax.z; // FACE_FRONT  : FACE_BACK

    return p;
}

// All vertices are always in the standard order (see below).
uint GetFaceMaskOfVertex(uint v)
{
    // 0: (-1, -1, -1) -> { FACE_LEFT  | FACE_BOTTOM | FACE_FRONT }
    // 1: (+1, -1, -1) -> { FACE_RIGHT | FACE_BOTTOM | FACE_FRONT }
    // 2: (-1, +1, -1) -> { FACE_LEFT  | FACE_TOP    | FACE_FRONT }
    // 3: (+1, +1, -1) -> { FACE_RIGHT | FACE_TOP    | FACE_FRONT }
    // 4: (-1, -1, +1) -> { FACE_LEFT  | FACE_BOTTOM | FACE_BACK  }
    // 5: (+1, -1, +1) -> { FACE_RIGHT | FACE_BOTTOM | FACE_BACK  }
    // 6: (-1, +1, +1) -> { FACE_LEFT  | FACE_TOP    | FACE_BACK  }
    // 7: (+1, +1, +1) -> { FACE_RIGHT | FACE_TOP    | FACE_BACK  }
    // ((v & 1) == 0) ? 1 : 2) | ((v & 2) == 0) ? 4 : 8) | ((v & 4) == 0) ? 16 : 32)
    uint f = (FACE_LEFT   << BitFieldExtract(v, 0, 1))
           | (FACE_BOTTOM << BitFieldExtract(v, 1, 1))
           | (FACE_FRONT  << BitFieldExtract(v, 2, 1));

    return f;
};

// A list of vertices for each face (CCW order w.r.t. its normal, starting from the LSB).
#define VERT_LIST_LEFT   ((4) << 9 | (6) << 6 | (2) << 3 | (0) << 0)
#define VERT_LIST_RIGHT  ((3) << 9 | (7) << 6 | (5) << 3 | (1) << 0)
#define VERT_LIST_BOTTOM ((1) << 9 | (5) << 6 | (4) << 3 | (0) << 0)
#define VERT_LIST_TOP    ((6) << 9 | (7) << 6 | (3) << 3 | (2) << 0)
#define VERT_LIST_FRONT  ((2) << 9 | (3) << 6 | (1) << 3 | (0) << 0)
#define VERT_LIST_BACK   ((5) << 9 | (7) << 6 | (6) << 3 | (4) << 0)

uint GetVertexListOfFace(uint f)
{
    // Warning: don't add 'static' here unless you want really bad code gen.
    const uint3 allVertLists = uint3((VERT_LIST_RIGHT << 12) | VERT_LIST_LEFT,
                                     (VERT_LIST_TOP   << 12) | VERT_LIST_BOTTOM,
                                     (VERT_LIST_BACK  << 12) | VERT_LIST_FRONT);

    return BitFieldExtract(allVertLists[f >> 1], 12 * (f & 1), 12);
}

groupshared uint gs_BehindMasksOfVerts[VERTS_PER_GROUP]; // 6 planes each (HLSL does not support small data types)

// Returns 'true' if it manages to cull the face.
uint TryCullFace(uint f, uint baseVertexOffset)
{
    uint cullMaskOfFace = PLANE_MASK; // Initially behind
    uint vertListOfFace = GetVertexListOfFace(f);

    for (uint j = 0; j < VERTS_PER_FACE; j++)
    {
        uint v = BitFieldExtract(vertListOfFace, 3 * j, 3);
        // Non-zero if ALL the vertices are behind the same plane(s).
        cullMaskOfFace &= gs_BehindMasksOfVerts[baseVertexOffset + v];
    }

    return (cullMaskOfFace != 0);
}

void ClipPolygonAgainstPlane(float4 clipPlane, uint srcBegin, uint srcSize,
                             inout float4 vertRingBuffer[MAX_CLIP_VERTS],
                             out uint dstBegin, out uint dstSize)
{
    dstBegin = srcBegin + srcSize; // Start at the end; we don't use modular arithmetic here
    dstSize  = 0;

    ClipVertex tailVert = CreateClipVertex(vertRingBuffer[(srcBegin + srcSize - 1) % MAX_CLIP_VERTS], clipPlane);

#ifdef OBTUSE_COMPILER
    uint modSrcIdx = srcBegin % MAX_CLIP_VERTS;
    uint modDstIdx = dstBegin % MAX_CLIP_VERTS;
#endif

    for (uint j = srcBegin; j < (srcBegin + srcSize); j++)
    {
    #ifndef OBTUSE_COMPILER
        uint modSrcIdx = j % MAX_CLIP_VERTS;
    #endif
        ClipVertex leadVert = CreateClipVertex(vertRingBuffer[modSrcIdx], clipPlane);

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

// 5 arrays * 128 elements * 4 bytes each = 2560 bytes.
groupshared float gs_HapVertsX[VERTS_PER_GROUP];
groupshared float gs_HapVertsY[VERTS_PER_GROUP];
groupshared float gs_HapVertsZ[VERTS_PER_GROUP];
groupshared float gs_HapVertsW[VERTS_PER_GROUP];

// Returns 'true' if the face has been entirely clipped.
bool ClipFaceAgainstCube(uint f, float3 cubeMin, float3 cubeMax, uint baseVertexOffset,
                         out uint srcBegin, out uint srcSize,
                         out float4 vertRingBuffer[MAX_CLIP_VERTS])
{
    srcBegin = 0;
    srcSize  = VERTS_PER_FACE;

    uint clipMaskOfFace = 0; // Initially in front
    uint vertListOfFace = GetVertexListOfFace(f);

    for (uint j = 0; j < VERTS_PER_FACE; j++)
    {
        uint v = BitFieldExtract(vertListOfFace, 3 * j, 3);
        // Non-zero if ANY of the vertices are behind the same plane(s).
        clipMaskOfFace |= gs_BehindMasksOfVerts[baseVertexOffset + v];

        // Not all edges may require clipping. However, filtering the vertex list
        // is somewhat expensive, so we currently don't do it.
        vertRingBuffer[j].x = gs_HapVertsX[baseVertexOffset + v];
        vertRingBuffer[j].y = gs_HapVertsY[baseVertexOffset + v];
        vertRingBuffer[j].z = gs_HapVertsZ[baseVertexOffset + v];
        vertRingBuffer[j].w = gs_HapVertsW[baseVertexOffset + v];
    }

    // Sutherland-Hodgeman polygon clipping algorithm.
    // It works by clipping the entire polygon against one clipping plane at a time.
    while ((clipMaskOfFace != 0) && (srcSize != 0))
    {
        uint p = firstbitlow(clipMaskOfFace);

        float4 clipPlane = CreateClipPlane(p, cubeMin, cubeMax);

        uint dstBegin, dstSize;
        ClipPolygonAgainstPlane(clipPlane, srcBegin, srcSize, vertRingBuffer, dstBegin, dstSize);

        srcBegin = dstBegin;
        srcSize  = dstSize;

        clipMaskOfFace ^= 1 << p; // Clear the bit to continue using firstbitlow()
    }

    return srcSize == 0;
}

void UpdateAaBb(uint srcBegin, uint srcSize, float4 vertRingBuffer[MAX_CLIP_VERTS],
                bool isOrthoProj, float4x4 invProjMat,
                inout float4 ndcAaBbMinPt, inout float4 ndcAaBbMaxPt)
{
#ifdef OBTUSE_COMPILER
    uint modSrcIdx = srcBegin % MAX_CLIP_VERTS;
#endif
    for (uint j = srcBegin; j < (srcBegin + srcSize); j++)
    {
    #ifndef OBTUSE_COMPILER
        uint modSrcIdx = j % MAX_CLIP_VERTS;
    #endif
        float4 hapVertCS  = vertRingBuffer[modSrcIdx];
        float3 rapVertCS  = hapVertCS.xyz * rcp(hapVertCS.w);
        float3 rapVertNDC = float3(rapVertCS.xy * 0.5 + 0.5, rapVertCS.z);
        float  rbpVertVSz = hapVertCS.w;

        if (isOrthoProj) // Must replace (w = 1)
        {
            rbpVertVSz = dot(invProjMat[2], hapVertCS);
        }

        ndcAaBbMinPt = min(ndcAaBbMinPt, float4(rapVertNDC, rbpVertVSz));
        ndcAaBbMaxPt = max(ndcAaBbMaxPt, float4(rapVertNDC, rbpVertVSz));
    #ifdef OBTUSE_COMPILER
        modSrcIdx++;
        modSrcIdx = (modSrcIdx == MAX_CLIP_VERTS) ? 0 : modSrcIdx;
    #endif
    }
}
#endif // INCLUDE_SPECIFIC_CLIPPING_CODE

#endif // UNITY_CLIPPINGUTILITIES_INCLUDED
