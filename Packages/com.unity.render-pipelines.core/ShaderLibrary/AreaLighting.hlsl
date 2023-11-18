#ifndef UNITY_AREA_LIGHTING_INCLUDED
#define UNITY_AREA_LIGHTING_INCLUDED

#define APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
#define APPROXIMATE_SPHERE_LIGHT_NUMERICALLY


// 'sinSqSigma' is the sine^2 of the half-angle subtended by the sphere (aperture) as seen from the shaded point.
// 'cosOmega' is the cosine of the angle between the normal and the direction to the center of the light.
// This function performs horizon clipping.
real DiffuseSphereLightIrradiance(real sinSqSigma, real cosOmega)
{
#ifdef APPROXIMATE_SPHERE_LIGHT_NUMERICALLY
    real x = sinSqSigma;
    real y = cosOmega;

    // Use a numerical fit found in Mathematica. Mean absolute error: 0.00476944.
    // You can use the following Mathematica code to reproduce our results:
    // t = Flatten[Table[{x, y, f[x, y]}, {x, 0, 0.999999, 0.001}, {y, -0.999999, 0.999999, 0.002}], 1]
    // m = NonlinearModelFit[t, x * (y + e) * (0.5 + (y - e) * (a + b * x + c * x^2 + d * x^3)), {a, b, c, d, e}, {x, y}]
    return saturate(x * (0.9245867471551246 + y) * (0.5 + (-0.9245867471551246 + y) * (0.5359050373687144 + x * (-1.0054221851257754 + x * (1.8199061187417047 - x * 1.3172081704209504)))));
#else
    #if 0 // Ref: Area Light Sources for Real-Time Graphics, page 4 (1996).
        real sinSqOmega = saturate(1 - cosOmega * cosOmega);
        real cosSqSigma = saturate(1 - sinSqSigma);
        real sinSqGamma = saturate(cosSqSigma / sinSqOmega);
        real cosSqGamma = saturate(1 - sinSqGamma);

        real sinSigma = sqrt(sinSqSigma);
        real sinGamma = sqrt(sinSqGamma);
        real cosGamma = sqrt(cosSqGamma);

        real sigma = asin(sinSigma);
        real omega = acos(cosOmega);
        real gamma = asin(sinGamma);

        if (omega >= HALF_PI + sigma)
        {
            // Full horizon occlusion (case #4).
            return 0;
        }

        real e = sinSqSigma * cosOmega;

        UNITY_BRANCH
        if (omega < HALF_PI - sigma)
        {
            // No horizon occlusion (case #1).
            return e;
        }
        else
        {
            real g = (-2 * sqrt(sinSqOmega * cosSqSigma) + sinGamma) * cosGamma + (HALF_PI - gamma);
            real h = cosOmega * (cosGamma * sqrt(saturate(sinSqSigma - cosSqGamma)) + sinSqSigma * asin(saturate(cosGamma / sinSigma)));

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
        real cosSqOmega = cosOmega * cosOmega;                     // y^2

        UNITY_BRANCH
        if (cosSqOmega > sinSqSigma)                                // (y^2)>x
        {
            return saturate(sinSqSigma * cosOmega);                 // Clip[x*y,{0,1}]
        }
        else
        {
            real cotSqSigma = rcp(sinSqSigma) - 1;                 // 1/x-1
            real tanSqSigma = rcp(cotSqSigma);                     // x/(1-x)
            real sinSqOmega = 1 - cosSqOmega;                      // 1-y^2

            real w = sinSqOmega * tanSqSigma;                      // (1-y^2)*(x/(1-x))
            real x = -cosOmega * rsqrt(w);                         // -y*Sqrt[(1/x-1)/(1-y^2)]
            real y = sqrt(sinSqOmega * tanSqSigma - cosSqOmega);   // Sqrt[(1-y^2)*(x/(1-x))-y^2]
            real z = y * cotSqSigma;                               // Sqrt[(1-y^2)*(x/(1-x))-y^2]*(1/x-1)

            real a = cosOmega * acos(x) - z;                       // y*ArcCos[-y*Sqrt[(1/x-1)/(1-y^2)]]-Sqrt[(1-y^2)*(x/(1-x))-y^2]*(1/x-1)
            real b = atan(y);                                      // ArcTan[Sqrt[(1-y^2)*(x/(1-x))-y^2]]

            return saturate(INV_PI * (a * sinSqSigma + b));
        }
    #endif
#endif
}

// The output is *not* normalized by the factor of 1/TWO_PI (this is done by the PolygonFormFactor function).
real3 ComputeEdgeFactor(real3 V1, real3 V2)
{
    real subtendedAngle;

    real  V1oV2  = dot(V1, V2);
    real3 V1xV2  = cross(V1, V2);               // Plane normal (tangent to the unit sphere)
    real  sqLen  = saturate(1 - V1oV2 * V1oV2); // length(V1xV2) = abs(sin(angle))
    real  rcpLen = rsqrt(max(FLT_MIN, sqLen));  // Make sure it is finite
#if 0
    real y = rcpLen * acos(V1oV2);
#else
    // Let y[x_] = ArcCos[x] / Sqrt[1 - x^2].
    // Range reduction: since ArcCos[-x] == Pi - ArcCos[x], we only need to consider x on [0, 1].
    real x = abs(V1oV2);
    // Limit[y[x], x -> 1] == 1,
    // Limit[y[x], x -> 0] == Pi/2.
    // The approximation is exact at the endpoints of [0, 1].
    // Max. abs. error on [0, 1] is 1.33e-6 at x = 0.0036.
    // Max. rel. error on [0, 1] is 8.66e-7 at x = 0.0037.
    real y = HALF_PI + x * (-0.99991 + x * (0.783393 + x * (-0.649178 + x * (0.510589 + x * (-0.326137 + x * (0.137528 + x * -0.0270813))))));

    if (V1oV2 < 0)
    {
        y = rcpLen * PI - y;
    }

#endif

    return V1xV2 * y;
}

// Input: 3-5 vertices in the coordinate frame centered at the shaded point.
// Output: signed vector irradiance.
// No horizon clipping is performed.
real3 PolygonFormFactor(real4x3 L, real3 L4, uint n)
{
    // The length cannot be zero since we have already checked
    // that the light has a non-zero effective area,
    // and thus its plane cannot pass through the origin.
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

    // If the magnitudes of a pair of edge factors are
    // nearly the same, catastrophic cancellation may occur:
    // https://en.wikipedia.org/wiki/Catastrophic_cancellation
    // For the same reason, the value of the cross product of two
    // nearly collinear vectors is prone to large errors.
    // Therefore, the algorithm is inherently numerically unstable
    // for area lights that shrink to a line (or a point) after
    // projection onto the unit sphere.
    real3 F  = ComputeEdgeFactor(L[0], L[1]);
          F += ComputeEdgeFactor(L[1], L[2]);
          F += ComputeEdgeFactor(L[2], L[3]);
    if (n >= 4)
          F += ComputeEdgeFactor(L[3], L4);
    if (n == 5)
          F += ComputeEdgeFactor(L4, L[0]);

    return INV_TWO_PI * F; // The output may be projected onto the tangent plane (F.z) to yield signed irradiance.
}

// See "Real-Time Area Lighting: a Journey from Research to Production", slide 102.
// Turns out, despite the authors claiming that this function "calculates an approximation of
// the clipped sphere form factor", that is simply not true.
// First of all, above horizon, the function should then just return 'F.z', which it does not.
// Secondly, if we use the correct function called DiffuseSphereLightIrradiance(), it results
// in severe light leaking if the light is placed vertically behind the camera.
// So this function is clearly a hack designed to work around these problems.
real PolygonIrradianceFromVectorFormFactor(float3 F)
{
#if 1
    float l = length(F);
    return max(0, (l * l + F.z) / (l + 1));
#else
    real sff               = saturate(dot(F, F));
    real sinSqAperture     = sqrt(sff);
    real cosElevationAngle = F.z * rsqrt(sff);

    return DiffuseSphereLightIrradiance(sinSqAperture, cosElevationAngle);
#endif
}

// Expects non-normalized vertex positions.
// Output: F is the signed vector irradiance.
real PolygonIrradiance(real4x3 L, out real3 F)
{
#ifdef APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
    F = PolygonFormFactor(L, real3(0,0,1), 4); // Before horizon clipping.

    return PolygonIrradianceFromVectorFormFactor(F); // Accounts for the horizon.
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
    real3 L4 = L[3];

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

    // 2. Integrate
    F = PolygonFormFactor(L, L4, n); // After the horizon clipping.

    // 3. Compute irradiance
    return max(0, F.z);
#endif
}

// This function assumes that inputs are well-behaved, e.i.
// that the line does not pass through the origin and
// that the light is (at least partially) above the surface.
float I_diffuse_line(float3 C, float3 A, float hl)
{
    // Solve C.z + h * A.z = 0.
    float h = -C.z * rcp(A.z);  // May be Inf, but never NaN

    // Clip the line segment against the z-plane if necessary.
    float h2 = (A.z >= 0) ? max( hl, h)
                          : min( hl, h); // P2 = C + h2 * A
    float h1 = (A.z >= 0) ? max(-hl, h)
                          : min(-hl, h); // P1 = C + h1 * A

    // Normalize the tangent.
    float  as = dot(A, A);      // |A|^2
    float  ar = rsqrt(as);      // 1/|A|
    float  a  = as * ar;        // |A|
    float3 T  = A * ar;         // A/|A|

    // Orthogonal 2D coordinates:
    // P(n, t) = n * N + t * T.
    float  tc = dot(T, C);      // C = n * N + tc * T
    float3 P0 = C - tc * T;     // P(n, 0) = n * N
    float  ns = dot(P0, P0);    // |P0|^2

    float nr = rsqrt(ns);       // 1/|P0|
    float n  = ns * nr;         // |P0|
    float Nz = P0.z * nr;       // N.z = P0.z/|P0|

    // P(n, t) - C = P0 + t * T - P0 - tc * T
    // = (t - tc) * T = h * A = (h * a) * T.
    float t2 = tc + h2 * a;     // P2.t
    float t1 = tc + h1 * a;     // P1.t
    float s2 = ns + t2 * t2;    // |P2|^2
    float s1 = ns + t1 * t1;    // |P1|^2
    float mr = rsqrt(s1 * s2);  // 1/(|P1|*|P2|)
    float r2 = s1 * (mr * mr);  // 1/|P2|^2
    float r1 = s2 * (mr * mr);  // 1/|P1|^2

    // I = (i1 + i2 + i3) / Pi.
    // i1 =  N.z * (P2.t / |P2|^2 - P1.t / |P1|^2).
    // i2 = -T.z * (P2.n / |P2|^2 - P1.n / |P1|^2).
    // i3 =  N.z * ArcCos[Dot[P1, P2] / (|P1| * |P2|)] / |P0|.
    float i12 = (Nz * t2 - (T.z * n)) * r2
              - (Nz * t1 - (T.z * n)) * r1;
    // Guard against numerical errors.
    float dt  = min(1, (ns + t1 * t2) * mr);
    float i3  = acos(dt) * (Nz * nr); // angle * cos(θ) / r^2

    // Guard against numerical errors.
    return INV_PI * max(0, i12 + i3);
}

// Computes 1 / length(mul(transpose(inverse(invM)), normalize(ortho))).
float ComputeLineWidthFactor(float3x3 invM, float3 ortho, float orthoSq)
{
    // transpose(inverse(invM)) = 1 / determinant(invM) * cofactor(invM).
    // Take into account the sparsity of the matrix:
    // {{a,0,b},
    //  {0,c,0},
    //  {d,0,1}}
    float a = invM[0][0];
    float b = invM[0][2];
    float c = invM[1][1];
    float d = invM[2][0];

    float  det = c * (a - b * d);
    float3 X   = float3(c * (ortho.x - d * ortho.z),
                            (ortho.y * (a - b * d)),
                        c * (-b * ortho.x + a * ortho.z));  // mul(cof, ortho)

    // 1 / length(1/s * X) = abs(s) / length(X).
    return abs(det) * rsqrt(dot(X, X) * orthoSq) * orthoSq; // rsqrt(x^2) * x^2 = x
}

float I_ltc_line(float3x3 invM, float3 center, float3 axis, float halfLength)
{
    float3 ortho   = cross(center, axis);
    float  orthoSq = dot(ortho, ortho);

    // Check whether the line passes through the origin.
    bool quit = (orthoSq == 0);

    // Check whether the light is entirely below the surface.
    // We must test twice, since a linear transformation
    // may bring the light above the surface (a side-effect).
    quit = quit || (center.z + halfLength * abs(axis.z) <= 0);

    // Transform into the diffuse configuration.
    // This is a sparse matrix multiplication.
    // Pay attention to the multiplication order
    // (in case your matrices are transposed).
    float3 C = mul(invM, center);
    float3 A = mul(invM, axis);

    // Check whether the light is entirely below the surface.
    // We must test twice, since a linear transformation
    // may bring the light below the surface (as expected).
    quit = quit || (C.z + halfLength * abs(A.z) <= 0);

    float result = 0;

    if (!quit)
    {
        float w = ComputeLineWidthFactor(invM, ortho, orthoSq);

        result = I_diffuse_line(C, A, halfLength) * w;
    }

    return result;
}

#endif // UNITY_AREA_LIGHTING_INCLUDED
