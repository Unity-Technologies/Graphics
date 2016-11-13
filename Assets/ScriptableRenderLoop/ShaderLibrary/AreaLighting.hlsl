#ifndef UNITY_AREA_LIGHTING_INCLUDED
#define UNITY_AREA_LIGHTING_INCLUDED

float IntegrateEdge(float3 v1, float3 v2)
{
    float cosTheta = dot(v1, v2);
    // TODO: Explain the 0.9999 <= precision is important!
    cosTheta = Clamp(cosTheta, -0.9999, 0.9999);

    // TODO: Experiment with fastAcos
    float theta = acos(cosTheta);
    float res = cross(v1, v2).z * theta / sin(theta);

    return res;
}

// Baum's equation
// Expects non-normalized vertex positions
float PolygonRadiance(float4x3 L, bool twoSided)
{
    // 1. ClipQuadToHorizon

    // detect clipping config	
    uint config = 0;
    if (L[0].z > 0) config += 1;
    if (L[1].z > 0) config += 2;
    if (L[2].z > 0) config += 4;
    if (L[3].z > 0) config += 8;

    // The fifth vertex for cases when clipping cuts off one corner.
    // Due to a compiler bug, copying L into a vector array with 5 rows
    // messes something up, so we need to stick with the matrix + the L4 vertex.
    float3 L4 = L[3];

    // This switch is surprisingly fast. Tried replacing it with a lookup array of vertices.
    // Even though that replaced the switch with just some indexing and no branches, it became
    // way, way slower - mem fetch stalls?

    uint n = 0;
    switch (config)
    {
    case 0: // clip all
        break;

    case 1: // V1 clip V2 V3 V4
        n = 3;
        L[1] = -L[1].z * L[0] + L[0].z * L[1];
        L[2] = -L[3].z * L[0] + L[0].z * L[3];
        break;

    case 2: // V2 clip V1 V3 V4
        n = 3;
        L[0] = -L[0].z * L[1] + L[1].z * L[0];
        L[2] = -L[2].z * L[1] + L[1].z * L[2];
        break;

    case 3: // V1 V2 clip V3 V4
        n = 4;
        L[2] = -L[2].z * L[1] + L[1].z * L[2];
        L[3] = -L[3].z * L[0] + L[0].z * L[3];
        break;

    case 4: // V3 clip V1 V2 V4
        n = 3;
        L[0] = -L[3].z * L[2] + L[2].z * L[3];
        L[1] = -L[1].z * L[2] + L[2].z * L[1];
        break;

    case 5: // V1 V3 clip V2 V4: impossible
        break;

    case 6: // V2 V3 clip V1 V4
        n = 4;
        L[0] = -L[0].z * L[1] + L[1].z * L[0];
        L[3] = -L[3].z * L[2] + L[2].z * L[3];
        break;

    case 7: // V1 V2 V3 clip V4
        n = 5;
        L4 = -L[3].z * L[0] + L[0].z * L[3];
        L[3] = -L[3].z * L[2] + L[2].z * L[3];
        break;

    case 8: // V4 clip V1 V2 V3
        n = 3;
        L[0] = -L[0].z * L[3] + L[3].z * L[0];
        L[1] = -L[2].z * L[3] + L[3].z * L[2];
        L[2] = L[3];
        break;

    case 9: // V1 V4 clip V2 V3
        n = 4;
        L[1] = -L[1].z * L[0] + L[0].z * L[1];
        L[2] = -L[2].z * L[3] + L[3].z * L[2];
        break;

    case 10: // V2 V4 clip V1 V3: impossible
        break;

    case 11: // V1 V2 V4 clip V3
        n = 5;
        L[3] = -L[2].z * L[3] + L[3].z * L[2];
        L[2] = -L[2].z * L[1] + L[1].z * L[2];
        break;

    case 12: // V3 V4 clip V1 V2
        n = 4;
        L[1] = -L[1].z * L[2] + L[2].z * L[1];
        L[0] = -L[0].z * L[3] + L[3].z * L[0];
        break;

    case 13: // V1 V3 V4 clip V2
        n = 5;
        L[3] = L[2];
        L[2] = -L[1].z * L[2] + L[2].z * L[1];
        L[1] = -L[1].z * L[0] + L[0].z * L[1];
        break;

    case 14: // V2 V3 V4 clip V1
        n = 5;
        L4 = -L[0].z * L[3] + L[3].z * L[0];
        L[0] = -L[0].z * L[1] + L[1].z * L[0];
        break;

    case 15: // V1 V2 V3 V4
        n = 4;
        break;
    }

    if (n == 0)
        return 0;
    if (n == 3)
        L[3] = L[0];
    if (n == 4)
        L4 = L[0];

    // 2. Project onto sphere
    L[0] = normalize(L[0]);
    L[1] = normalize(L[1]);
    L[2] = normalize(L[2]);
    L[3] = normalize(L[3]);
    L4 = normalize(L4);

    // 3. Integrate
    float sum = 0;
    sum += IntegrateEdge(L[0], L[1]);
    sum += IntegrateEdge(L[1], L[2]);
    sum += IntegrateEdge(L[2], L[3]);
    if (n >= 4)
        sum += IntegrateEdge(L[3], L4);
    if (n == 5)
        sum += IntegrateEdge(L4, L[0]);

    return twoSided > 0.0 ? abs(sum) : max(0.0, sum);
}

float LTCEvaluate(float3 V, float3 N, float3x3 minV, float4x3 L, bool twoSided)
{
    // Construct local orthonormal basis around N, aligned with N
    float3x3 basis;
    basis[0] = normalize(V - N * dot(V, N));
    basis[1] = normalize(cross(N, basis[0]));
    basis[2] = N;

    // rotate area light in local basis
    minV = mul(transpose(basis), minV);
    L = mul(L, minV);

    // Polygon radiance in transformed configuration - specular
    return PolygonRadiance(L, twoSided);
}

#endif // UNITY_AREA_LIGHTING_INCLUDED
