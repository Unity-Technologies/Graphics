#ifndef UNITY_AREA_LIGHTING_INCLUDED
#define UNITY_AREA_LIGHTING_INCLUDED

float IntegrateEdge(float3 v1, float3 v2)
{
    float cosTheta = dot(v1, v2);
    // Clamp to avoid artifacts. This particular constant gives the best results.
    cosTheta    = Clamp(cosTheta, -0.9999, 0.9999);
    float theta = FastACos(cosTheta);
    float res = cross(v1, v2).z * theta * rsqrt(1.0f - cosTheta * cosTheta); // optimization from * 1 / sin(theta)

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

    // clip
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

    if (n == 0) return 0;

    // 2. Project onto sphere
    L[0] = normalize(L[0]);
    L[1] = normalize(L[1]);
    L[2] = normalize(L[2]);

    switch (n)
    {
        case 3:
            L[3] = L[0];
            break;
        case 4:
            L[3] = normalize(L[3]);
            L4   = L[0];
            break;
        case 5:
            L[3] = normalize(L[3]);
            L4   = normalize(L4);
            break;
    }

    // 3. Integrate
    float sum = 0;
    sum += IntegrateEdge(L[0], L[1]);
    sum += IntegrateEdge(L[1], L[2]);
    sum += IntegrateEdge(L[2], L[3]);
    if (n >= 4)
        sum += IntegrateEdge(L[3], L4);
    if (n == 5)
        sum += IntegrateEdge(L4, L[0]);

    sum *= INV_TWO_PI; // Normalization

    sum = twoSided ? abs(sum) : max(sum, 0.0);

    return isfinite(sum) ? sum : 0.0;
}

// For polygonal lights.
float LTCEvaluate(float4x3 L, float3 V, float3 N, float NdotV, bool twoSided, float3x3 invM)
{
    // Construct local orthonormal basis around N, aligned with N
    // TODO: it could be stored in PreLightData. All LTC lights compute it more than once!
    // Also consider using 'bsdfData.tangentWS', 'bsdfData.bitangentWS', 'bsdfData.normalWS'.
    float3x3 basis;
    basis[0] = normalize(V - N * NdotV);
    basis[1] = normalize(cross(N, basis[0]));
    basis[2] = N;

    // rotate area light in local basis
    invM = mul(transpose(basis), invM);
    L = mul(L, invM);

    // Polygon radiance in transformed configuration - specular
    return PolygonRadiance(L, twoSided);
}

float LineFpo(float tLDDL, float lrcpD, float rcpD)
{
    // Compute: ((l / d) / (d * d + l * l)) + (1.0 / (d * d)) * atan(l / d).
    return tLDDL + (rcpD * rcpD) * FastATan(lrcpD);
}

float LineFwt(float tLDDL, float l)
{
    // Compute: l * ((l / d) / (d * d + l * l)).
    return l * tLDDL;
}

// Computes the integral of the clamped cosine over the line segment.
// 'l1' and 'l2' define the integration interval.
// 'tangent' is the line's tangent direction.
// 'normal' is the direction orthogonal to the tangent. It is the shortest vector between
// the shaded point and the line, pointing away from the shaded point.
float LineIrradiance(float l1, float l2, float3 normal, float3 tangent)
{   
    float d      = length(normal);
    float l1rcpD = l1 * rcp(d);
    float l2rcpD = l2 * rcp(d);
    float tLDDL1 = l1rcpD / (d * d + l1 * l1);
    float tLDDL2 = l2rcpD / (d * d + l2 * l2);
    float intWt  = LineFwt(tLDDL2, l2) - LineFwt(tLDDL1, l1);
    float intP0  = LineFpo(tLDDL2, l2rcpD, rcp(d)) - LineFpo(tLDDL1, l1rcpD, rcp(d));
    return intP0 * normal.z + intWt * tangent.z;
}

// Computes 1.0 / length(mul(ortho, transpose(inverse(invM)))).
float ComputeLineWidthFactor(float3x3 invM, float3 ortho)
{
    // transpose(inverse(M)) = (1.0 / determinant(M)) * cofactor(M).
    // Take into account that m12 = m21 = m23 = m32 = 0 and m33 = 1.
    float    det = invM._11 * invM._22 - invM._22 * invM._31 * invM._13;
    float3x3 cof = {invM._22, 0.0, -invM._22 * invM._31,
                    0.0, invM._11 - invM._13 * invM._31, 0.0,
                    -invM._13 * invM._22, 0.0, invM._11 * invM._22};

    // 1.0 / length(mul(V, (1.0 / s * M))) = abs(s) / length(mul(V, M)).
    return abs(det) / length(mul(ortho, cof));
}

// For line lights.
float LTCEvaluate(float3 P1, float3 P2, float3 B, float3x3 invM)
{
    // Inverse-transform the endpoints.
    P1 = mul(P1, invM);
    P2 = mul(P2, invM);

    // Terminate the algorithm if both points are below the horizon.
    if (P1.z <= 0.0 && P2.z <= 0.0) return 0.0;

    float width = ComputeLineWidthFactor(invM, B);

    if (P1.z > P2.z)
    {
        // Convention: 'P2' is above 'P1', with the tangent pointing upwards.
        Swap(P1, P2);
    }

    // Recompute the length and the tangent in the new coordinate system.
    float  len = length(P2 - P1);
    float3 T   = normalize(P2 - P1);

    // Clip the part of the light below the horizon.
    if (P1.z <= 0.0)
    {
        // P = P1 + t * T; P.z == 0.
        float t = -P1.z / T.z;
        P1 = float3(P1.xy + t * T.xy, 0.0);

        // Set the length of the visible part of the light.
        len -= t;
    }

    // Compute the normal direction to the line, s.t. it is the shortest vector
    // between the shaded point and the line, pointing away from the shaded point.
    // Can be interpreted as a point on the line, since the shaded point is at the origin.
    float  proj = dot(P1, T);
    float3 P0   = P1 - proj * T;

    // Compute the parameterization: distances from 'P1' and 'P2' to 'P0'.
    float l1 = proj;
    float l2 = l1 + len;

    // Integrate the clamped cosine over the line segment.
    float irradiance = LineIrradiance(l1, l2, P0, T);

    // Guard against numerical precision issues.
    return max(INV_PI * width * irradiance, 0.0);
}

#endif // UNITY_AREA_LIGHTING_INCLUDED
