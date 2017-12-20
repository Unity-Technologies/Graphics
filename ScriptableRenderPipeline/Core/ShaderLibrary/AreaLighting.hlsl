#ifndef UNITY_AREA_LIGHTING_INCLUDED
#define UNITY_AREA_LIGHTING_INCLUDED

#define APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
#define APPROXIMATE_SPHERE_LIGHT_NUMERICALLY

// Not normalized by the factor of 1/TWO_PI.
REAL3 ComputeEdgeFactor(REAL3 V1, REAL3 V2)
{
    REAL  V1oV2 = dot(V1, V2);
    REAL3 V1xV2 = cross(V1, V2);
#if 0
    return V1xV2 * (rsqrt(1.0 - V1oV2 * V1oV2) * acos(V1oV2));
#else
    // Approximate: { y = rsqrt(1.0 - V1oV2 * V1oV2) * acos(V1oV2) } on [0, 1].
    // Fit: HornerForm[MiniMaxApproximation[ArcCos[x]/Sqrt[1 - x^2], {x, {0, 1 - $MachineEpsilon}, 6, 0}][[2, 1]]].
    // Maximum relative error: 2.6855360216340534 * 10^-6. Intensities up to 1000 are artifact-free.
    REAL x = abs(V1oV2);
    REAL y = 1.5707921083647782 + x * (-0.9995697178013095 + x * (0.778026455830408 + x * (-0.6173111361273548 + x * (0.4202724111150622 + x * (-0.19452783598217288 + x * 0.04232040013661036)))));

    if (V1oV2 < 0)
    {
        // Undo range reduction.
        y = PI * rsqrt(saturate(1 - V1oV2 * V1oV2)) - y;
    }

    return V1xV2 * y;
#endif
}

// Not normalized by the factor of 1/TWO_PI.
// Ref: Improving radiosity solutions through the use of analytically determined form-factors.
REAL IntegrateEdge(REAL3 V1, REAL3 V2)
{
    // 'V1' and 'V2' are represented in a coordinate system with N = (0, 0, 1).
    return ComputeEdgeFactor(V1, V2).z;
}

// 'sinSqSigma' is the sine^2 of the REAL of the opening angle of the sphere as seen from the shaded point.
// 'cosOmega' is the cosine of the angle between the normal and the direction to the center of the light.
// N.b.: this function accounts for horizon clipping.
REAL DiffuseSphereLightIrradiance(REAL sinSqSigma, REAL cosOmega)
{
#ifdef APPROXIMATE_SPHERE_LIGHT_NUMERICALLY
    REAL x = sinSqSigma;
    REAL y = cosOmega;

    // Use a numerical fit found in Mathematica. Mean absolute error: 0.00476944.
    // You can use the following Mathematica code to reproduce our results:
    // t = Flatten[Table[{x, y, f[x, y]}, {x, 0, 0.999999, 0.001}, {y, -0.999999, 0.999999, 0.002}], 1]
    // m = NonlinearModelFit[t, x * (y + e) * (0.5 + (y - e) * (a + b * x + c * x^2 + d * x^3)), {a, b, c, d, e}, {x, y}]
    return saturate(x * (0.9245867471551246 + y) * (0.5 + (-0.9245867471551246 + y) * (0.5359050373687144 + x * (-1.0054221851257754 + x * (1.8199061187417047 - x * 1.3172081704209504)))));
#else
    #if 0 // Ref: Area Light Sources for Real-Time Graphics, page 4 (1996).
        REAL sinSqOmega = saturate(1 - cosOmega * cosOmega);
        REAL cosSqSigma = saturate(1 - sinSqSigma);
        REAL sinSqGamma = saturate(cosSqSigma / sinSqOmega);
        REAL cosSqGamma = saturate(1 - sinSqGamma);

        REAL sinSigma = sqrt(sinSqSigma);
        REAL sinGamma = sqrt(sinSqGamma);
        REAL cosGamma = sqrt(cosSqGamma);

        REAL sigma = asin(sinSigma);
        REAL omega = acos(cosOmega);
        REAL gamma = asin(sinGamma);

        if (omega >= HALF_PI + sigma)
        {
            // Full horizon occlusion (case #4).
            return 0;
        }

        REAL e = sinSqSigma * cosOmega;

        [branch]
        if (omega < HALF_PI - sigma)
        {
            // No horizon occlusion (case #1).
            return e;
        }
        else
        {
            REAL g = (-2 * sqrt(sinSqOmega * cosSqSigma) + sinGamma) * cosGamma + (HALF_PI - gamma);
            REAL h = cosOmega * (cosGamma * sqrt(saturate(sinSqSigma - cosSqGamma)) + sinSqSigma * asin(saturate(cosGamma / sinSigma)));

            if (omega < HALF_PI)
            {
                // Partial horizon occlusion (case #2).
                return saturate(e + INV_PI * (g - h));
            }
            else
            {
                // Partial horizon occlusion (case #3).
                return saturate(INV_PI * (g + h));
            }
        }
    #else // Ref: Moving Frostbite to Physically Based Rendering, page 47 (2015, optimized).
        REAL cosSqOmega = cosOmega * cosOmega;                     // y^2

        [branch]
        if (cosSqOmega > sinSqSigma)                                // (y^2)>x
        {
            return saturate(sinSqSigma * cosOmega);                 // Clip[x*y,{0,1}]
        }
        else
        {
            REAL cotSqSigma = rcp(sinSqSigma) - 1;                 // 1/x-1
            REAL tanSqSigma = rcp(cotSqSigma);                     // x/(1-x)
            REAL sinSqOmega = 1 - cosSqOmega;                      // 1-y^2

            REAL w = sinSqOmega * tanSqSigma;                      // (1-y^2)*(x/(1-x))
            REAL x = -cosOmega * rsqrt(w);                         // -y*Sqrt[(1/x-1)/(1-y^2)]
            REAL y = sqrt(sinSqOmega * tanSqSigma - cosSqOmega);   // Sqrt[(1-y^2)*(x/(1-x))-y^2]
            REAL z = y * cotSqSigma;                               // Sqrt[(1-y^2)*(x/(1-x))-y^2]*(1/x-1)

            REAL a = cosOmega * acos(x) - z;                       // y*ArcCos[-y*Sqrt[(1/x-1)/(1-y^2)]]-Sqrt[(1-y^2)*(x/(1-x))-y^2]*(1/x-1)
            REAL b = atan(y);                                      // ArcTan[Sqrt[(1-y^2)*(x/(1-x))-y^2]]

            return saturate(INV_PI * (a * sinSqSigma + b));
        }
    #endif
#endif
}

// Expects non-normalized vertex positions.
REAL PolygonIrradiance(REAL4x3 L)
{
#ifdef APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
    [unroll]
    for (uint i = 0; i < 4; i++)
    {
        L[i] = normalize(L[i]);
    }

    REAL3 F = REAL3(0, 0, 0);

    [unroll]
    for (uint edge = 0; edge < 4; edge++)
    {
        REAL3 V1 = L[edge];
        REAL3 V2 = L[(edge + 1) % 4];

        F += INV_TWO_PI * ComputeEdgeFactor(V1, V2);
    }

    // Clamp invalid values to avoid visual artifacts.
    REAL f2         = saturate(dot(F, F));
    REAL sinSqSigma = min(sqrt(f2), 0.999);
    REAL cosOmega   = clamp(F.z * rsqrt(f2), -1, 1);

    return DiffuseSphereLightIrradiance(sinSqSigma, cosOmega);
#else
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
    REAL3 L4 = L[3];

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
    REAL sum = 0;
    sum += IntegrateEdge(L[0], L[1]);
    sum += IntegrateEdge(L[1], L[2]);
    sum += IntegrateEdge(L[2], L[3]);
    if (n >= 4)
        sum += IntegrateEdge(L[3], L4);
    if (n == 5)
        sum += IntegrateEdge(L4, L[0]);

    sum *= INV_TWO_PI; // Normalization

    sum = max(sum, 0.0);

    return isfinite(sum) ? sum : 0.0;
#endif
}

REAL LineFpo(REAL tLDDL, REAL lrcpD, REAL rcpD)
{
    // Compute: ((l / d) / (d * d + l * l)) + (1.0 / (d * d)) * atan(l / d).
    return tLDDL + (rcpD * rcpD) * FastATan(lrcpD);
}

REAL LineFwt(REAL tLDDL, REAL l)
{
    // Compute: l * ((l / d) / (d * d + l * l)).
    return l * tLDDL;
}

// Computes the integral of the clamped cosine over the line segment.
// 'l1' and 'l2' define the integration interval.
// 'tangent' is the line's tangent direction.
// 'normal' is the direction orthogonal to the tangent. It is the shortest vector between
// the shaded point and the line, pointing away from the shaded point.
REAL LineIrradiance(REAL l1, REAL l2, REAL3 normal, REAL3 tangent)
{
    REAL d      = length(normal);
    REAL l1rcpD = l1 * rcp(d);
    REAL l2rcpD = l2 * rcp(d);
    REAL tLDDL1 = l1rcpD / (d * d + l1 * l1);
    REAL tLDDL2 = l2rcpD / (d * d + l2 * l2);
    REAL intWt  = LineFwt(tLDDL2, l2) - LineFwt(tLDDL1, l1);
    REAL intP0  = LineFpo(tLDDL2, l2rcpD, rcp(d)) - LineFpo(tLDDL1, l1rcpD, rcp(d));
    return intP0 * normal.z + intWt * tangent.z;
}

// Computes 1.0 / length(mul(ortho, transpose(inverse(invM)))).
REAL ComputeLineWidthFactor(REAL3x3 invM, REAL3 ortho)
{
    // transpose(inverse(M)) = (1.0 / determinant(M)) * cofactor(M).
    // Take into account that m12 = m21 = m23 = m32 = 0 and m33 = 1.
    REAL    det = invM._11 * invM._22 - invM._22 * invM._31 * invM._13;
    REAL3x3 cof = {invM._22, 0.0, -invM._22 * invM._31,
                    0.0, invM._11 - invM._13 * invM._31, 0.0,
                    -invM._13 * invM._22, 0.0, invM._11 * invM._22};

    // 1.0 / length(mul(V, (1.0 / s * M))) = abs(s) / length(mul(V, M)).
    return abs(det) / length(mul(ortho, cof));
}

// For line lights.
REAL LTCEvaluate(REAL3 P1, REAL3 P2, REAL3 B, REAL3x3 invM)
{
    // Inverse-transform the endpoints.
    P1 = mul(P1, invM);
    P2 = mul(P2, invM);

    // Terminate the algorithm if both points are below the horizon.
    if (P1.z <= 0.0 && P2.z <= 0.0) return 0.0;

    REAL width = ComputeLineWidthFactor(invM, B);

    if (P1.z > P2.z)
    {
        // Convention: 'P2' is above 'P1', with the tangent pointing upwards.
        Swap(P1, P2);
    }

    // Recompute the length and the tangent in the new coordinate system.
    REAL  len = length(P2 - P1);
    REAL3 T   = normalize(P2 - P1);

    // Clip the part of the light below the horizon.
    if (P1.z <= 0.0)
    {
        // P = P1 + t * T; P.z == 0.
        REAL t = -P1.z / T.z;
        P1 = REAL3(P1.xy + t * T.xy, 0.0);

        // Set the length of the visible part of the light.
        len -= t;
    }

    // Compute the normal direction to the line, s.t. it is the shortest vector
    // between the shaded point and the line, pointing away from the shaded point.
    // Can be interpreted as a point on the line, since the shaded point is at the origin.
    REAL  proj = dot(P1, T);
    REAL3 P0   = P1 - proj * T;

    // Compute the parameterization: distances from 'P1' and 'P2' to 'P0'.
    REAL l1 = proj;
    REAL l2 = l1 + len;

    // Integrate the clamped cosine over the line segment.
    REAL irradiance = LineIrradiance(l1, l2, P0, T);

    // Guard against numerical precision issues.
    return max(INV_PI * width * irradiance, 0.0);
}

#endif // UNITY_AREA_LIGHTING_INCLUDED
