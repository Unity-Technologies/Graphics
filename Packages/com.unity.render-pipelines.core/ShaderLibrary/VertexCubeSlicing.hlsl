#ifndef VERTEX_CUBE_SLICING
# define VERTEX_CUBE_SLICING

// Box - plane slicing from 'A Vertex Program for Efficient Box-Plane Intersection'
// Optimized with 'A COMPARISON OF GPU BOX-PLANE INTERSECTION ALGORITHMS FOR DIRECT VOLUME RENDERING'

static const int sequence[16] = {
    0, 1, 2, 3, 4, 5, 6, 7,
    1, 2, 3, 0, 7, 4, 5, 6,
};

static const int v1[24] = {
    0, 1, 4, -1,
    1, 0, 1, 4,
    0, 2, 5, -1,
    2, 0, 2, 5,
    0, 3, 6, -1,
    3, 0, 3, 6
};

static const int v2[24] = {
    1, 4, 7, -1,
    5, 1, 4, 7,
    2, 5, 7, -1,
    6, 2, 5, 7,
    3, 6, 7, -1,
    4, 3, 6, 7
};

#ifdef GET_CUBE_VERTEX_POSITION
float3 ComputeCubeSliceVertexPositionRWS(float3 cameraViewDirection, float planeDistance, int vertexId)
{
    float3 position = 0;

    for (int i = 0; i < 4; ++i)
    {
        int vidx1 = sequence[int(v1[vertexId * 4 + i])];
        int vidx2 = sequence[int(v2[vertexId * 4 + i])];
        float3 vecV1 = GET_CUBE_VERTEX_POSITION(vidx1);
        float3 vecV2 = GET_CUBE_VERTEX_POSITION(vidx2);

        float3 intersection = 0;
        if (RayPlaneSegmentIntersect(vecV1, vecV2 - vecV1, cameraViewDirection, planeDistance, intersection))
        {
            position = intersection;
            break;
        }
    }

    return position;
}
#endif // GET_CUBE_VERTEX_POSITION

#endif // VERTEX_CUBE_SLICING
