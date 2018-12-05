#ifndef UNITY_AREA_LIGHTING_INCLUDED
#define UNITY_AREA_LIGHTING_INCLUDED

#define APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT  // Define this to replace rectangular area light polygon by an equivalent sphere (i.e. simpler, doesn't require as much effort for clipping)
#define APPROXIMATE_SPHERE_LIGHT_NUMERICALLY    // Define this to use numerical approximation instead of exact computation of sphere clipping with horizon plane

// Not normalized by the factor of 1/TWO_PI.
real3 ComputeEdgeFactor(real3 V1, real3 V2)
{
    real  V1oV2 = dot(V1, V2);
    real3 V1xV2 = cross(V1, V2);
#if 0
    return V1xV2 * (rsqrt(1.0 - V1oV2 * V1oV2) * acos(V1oV2));
#else
    // Approximate: { y = rsqrt(1.0 - V1oV2 * V1oV2) * acos(V1oV2) } on [0, 1].
    // Fit: HornerForm[MiniMaxApproximation[ArcCos[x]/Sqrt[1 - x^2], {x, {0, 1 - $MachineEpsilon}, 6, 0}][[2, 1]]].
    // Maximum relative error: 2.6855360216340534 * 10^-6. Intensities up to 1000 are artifact-free.
    real x = abs(V1oV2);
    real y = 1.5707921083647782 + x * (-0.9995697178013095 + x * (0.778026455830408 + x * (-0.6173111361273548 + x * (0.4202724111150622 + x * (-0.19452783598217288 + x * 0.04232040013661036)))));

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
real IntegrateEdge(real3 V1, real3 V2)
{
    // 'V1' and 'V2' are represented in a coordinate system with N = (0, 0, 1).
    return ComputeEdgeFactor(V1, V2).z;
}

// 'sinSqSigma' is the sine^2 of the real of the opening angle of the sphere as seen from the shaded point.
// 'cosOmega' is the cosine of the angle between the normal and the direction to the center of the light.
// N.b.: this function accounts for horizon clipping.
real DiffuseSphereLightIrradiance(real sinSqSigma, real cosOmega)
{
#ifdef APPROXIMATE_SPHERE_LIGHT_NUMERICALLY
    real x = sinSqSigma;
    real y = cosOmega;

    // Use a numerical fit found in Mathematica. Mean absolute error: 0.00476944.
    // You can use the following Mathematica code to reproduce our results:
    // t = Flatten[Table[{x, y, f[x, y]}, {x, 0, 0.999999, 0.001}, {y, -0.999999, 0.999999, 0.002}], 1]
    // m = NonlinearModelFit[t, x * (y + e) * (0.5 + (y - e) * (a + b * x + c * x^2 + d * x^3)), {a, b, c, d, e}, {x, y}]
    real    fit = saturate(x * (0.9245867471551246 + y) * (0.5 + (-0.9245867471551246 + y) * (0.5359050373687144 + x * (-1.0054221851257754 + x * (1.8199061187417047 - x * 1.3172081704209504)))));

    // BMAYAUX (18/08/27) it appears that the imprecision in fitting leads to light leaking issues
    // This apparently comes from the fact that the transition when the sphere is going below the horizon is a very sensitive area of the 2D table
    //  and both a table precomputation or our numerical fitting fail to capture the necessary precision here so I added back a more rigid control
    //  of the fade that should happen below the horizon...
    //
    // Each of these fade routines is equally interesting
    real    sqCosOmega = cosOmega < 0.0 ? -cosOmega*cosOmega : cosOmega*cosOmega;   // Signed square value
    real    fade = smoothstep(-sinSqSigma, 0, sqCosOmega);    // This one fade for a shorter time <== Prefer this if using smoothstep
//    real    fade = step(-0.05*sinSqSigma, sqCosOmega);        // This one is abrupt but does the job... And less expensive than a smoothstep. Problem is, the 0.05 actually should change depending on the size of the sphere (i.e. the sinSqSigma parameter)

    return fade * fit;

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


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Samples the area light's associated cookie
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
//  cookieIndex, the index of the cookie texture in the Texture2DArray
//  L, the 4 local-space corners of the area light polygon transformed by the LTC M^-1 matrix
//  F, the *normalized* vector irradiance
//
float3   SampleAreaLightCookie( int cookieIndex, float4x3 L, float3 F )
{
    // L[0] = top-right
    // L[1] = bottom-right
    // L[2] = bottom-left
    // L[3] = top-left
    float3  origin = L[2];
    float3  right = L[1] - origin;
    float3  up = L[3] - origin;

    float3  normal = cross( right, up );
    float   sqArea = dot( normal, normal );
            normal *= rsqrt( sqArea );

    // Compute intersection of irradiance vector with the area light plane
    float   hitDistance = dot( origin, normal ) / dot( F, normal );
    float3  hitPosition = hitDistance * normal;
            hitPosition -= origin;  // Relative to bottom-left corner

    // Here, right and up vectors are not necessarily orthonormal
    // We create the orthogonal vector "ortho" by projecting "up" onto the vector orthogonal to "right"
    //  ortho = up - (up.right') * right'
    // Where right' = right / sqrt( dot( right, right ) ), the normalized right vector
    float   recSqLengthRight = 1.0 / dot( right, right );
    float   upRightMixing = dot( up, right );
    float3  ortho = up - upRightMixing * right * recSqLengthRight;

    // The V coordinate along the "up" vector is simply the projection against the ortho vector
    float   v = dot( hitPosition, ortho ) / dot( ortho, ortho );

    // The U coordinate is not only the projection against the right vector
    //  but also the subtraction of the influence of the up vector upon the right vector
    //  (indeed, if the up & right vectors are not orthogonal then a certain amount of
    //  the up coordinate also influences the right coordinate)
    //
    //       |    up
    // ortho ^....*--------*
    //       |   /:       /
    //       |  / :      /
    //       | /  :     /
    //       |/   :    /
    //       +----+-->*----->
    //            : right
    //          mix of up into right that needs to be subtracted from simple projection on right vector
    //
    float   u = (dot( hitPosition, right ) - upRightMixing * v) * recSqLengthRight;
    float2  hitUV = float2( u, v );

    // Assuming the original cosine lobe distribution Do is enclosed in a cone of 90� aperture,
    //  following the idea of orthogonal projection upon the area light's plane we find the intersection
    //  of the cone to be a disk of area PI*d� where d is the hit distance we computed above.
    // We also know the area of the transformed polygon A = sqrt( sqArea ) and we pose the ratio of covered area as PI.d� / A.
    //
    // Knowing the area in square texels of the cookie texture A_sqTexels = texture width * texture height (default is 128x128 square texels)
    //  we can deduce the actual area covered by the cone in square texels as:
    //  A_covered = Pi.d� / A * A_sqTexels
    //
    // From this, we find the mip level as: mip = log2( sqrt( A_covered ) ) = log2( A_covered ) / 2
    // Also, assuming that A_sqTexels is of the form 2^n * 2^n we get the simplified expression: mip = log2( Pi.d� / A ) / 2 + n
    //
//    const float COOKIE_MIPS_COUNT = 7; // Cookie textures are 128x128
    const float COOKIE_MIPS_COUNT = _CookieSizePOT;
//    float   mipLevel = 0.25 * log2( 1e-3 + Sq( PI * hitDistance*hitDistance ) / sqArea ) + COOKIE_MIPS_COUNT;
    float   mipLevel = 0.5 * log2( 1e-8 + PI * hitDistance*hitDistance * rsqrt(sqArea) ) + COOKIE_MIPS_COUNT;

    return SAMPLE_TEXTURE2D_ARRAY_LOD(_CookieTextures, s_trilinear_clamp_sampler, hitUV, cookieIndex, mipLevel).xyz;
}


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// POLYGONAL AREA LIGHTS
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Returns the vector form-factor (i.e. vector irradiance/2PI) of the quadrilateral
// L contains the 4 corners of the quad
// Returns an average direction vector scaled by the scalar form-factor
real3   PolygonFormFactor(real4x3 L)
{
    UNITY_UNROLL
    for (uint i = 0; i < 4; i++)
    {
        L[i] = normalize(L[i]);
    }

    real3 F  = ComputeEdgeFactor( L[0], L[1] );
          F += ComputeEdgeFactor( L[1], L[2] );
          F += ComputeEdgeFactor( L[2], L[3] );
          F += ComputeEdgeFactor( L[3], L[0] );

    return INV_TWO_PI * F;
}

// Expects non-normalized vertex positions.
// L contains the 4 corners of the quad
real PolygonIrradiance(real4x3 L)
{
#ifdef APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
    real3 F = PolygonFormFactor(L);

    // Clamp invalid values to avoid visual artifacts.
    real    f = length(F);
    real    cosOmega   = clamp(F.z / f, -1, 1);
    real    sinSqSigma = min(f, 0.999);

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
    real sum = 0;
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


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// DISK AREA LIGHTS
// From http://blog.selfshadow.com/ltc/webgl/ltc_disk.html
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//

// An extended version of the implementation from "How to solve a cubic equation, revisited" (From http://momentsingraphics.de/?p=105)
real3 SolveCubic(real4 coefficients )
{
    // Normalize the polynomial
//  coefficients.xyz /= coefficients.w; // No use in our case: w=1

    // Divide middle coefficients by three
    coefficients.yz /= 3.0;

    real  A = coefficients.w;
    real  B = coefficients.z;
    real  C = coefficients.y;
    real  D = coefficients.x;

    // Compute the Hessian and the discriminant
    real3 Delta = real3(
        -coefficients.z*coefficients.z + coefficients.y,
        -coefficients.y*coefficients.z + coefficients.x,
        dot(real2(coefficients.z, -coefficients.y), coefficients.xy)
   );

    real  Discriminant = dot(real2(4.0*Delta.x, -Delta.y), Delta.zy);

    real3 RootsA, RootsD;

    real2 xlc, xsc;

    // Algorithm A
    {
        real  A_a = 1.0;
        real  C_a = Delta.x;
        real  D_a = -2.0*B*Delta.x + Delta.y;

        // Take the cubic root of a normalized complex number
        real  Theta = atan2( sqrt(Discriminant), -D_a) / 3.0;

        real  x_1a = 2.0*sqrt( max( 0.0, -C_a ) ) * cos( Theta );
        real  x_3a = 2.0*sqrt( max( 0.0, -C_a ) ) * cos( Theta + (2.0/3.0)*PI );

        real  xl;
        if ((x_1a + x_3a) > 2.0*B)
            xl = x_1a;
        else
            xl = x_3a;

        xlc = real2(xl - B, A);
    }

    // Algorithm D
    {
        real  A_d = D;
        real  C_d = Delta.z;
        real  D_d = -D*Delta.y + 2.0*C*Delta.z;

        // Take the cubic root of a normalized complex number
        real  Theta = atan2( D*sqrt(Discriminant), -D_d ) / 3.0;

        real  x_1d = 2.0*sqrt( max( 0.0, -C_d ) )*cos( Theta );
        real  x_3d = 2.0*sqrt( max( 0.0, -C_d ) )*cos( Theta + (2.0/3.0)*PI );

        real  xs;
        if (x_1d + x_3d < 2.0*C)
            xs = x_1d;
        else
            xs = x_3d;

        xsc = real2(-D, xs + C);
    }

    real  E =  xlc.y*xsc.y;
    real  F = -xlc.x*xsc.y - xlc.y*xsc.x;
    real  G =  xlc.x*xsc.x;

    real2 xmc = real2(C*F - B*G, -B*F + C*E);

    real3  roots = real3(xsc.x/xsc.y, xmc.x/xmc.y, xlc.x/xlc.y);

    if (roots.x < roots.y && roots.x < roots.z)
        roots.xyz = roots.yxz;
    else if (roots.z < roots.x && roots.z < roots.y)
        roots.xyz = roots.xzy;

    return roots;
}

// Returns the vector form-factor (i.e. vector irradiance/2PI) of the disc
// L contains the 4 corners of the quad bounding the elliptical disc, in tangent space
// Returns an average direction vector scaled by the scalar form-factor
real3   DiskFormFactor(real4x3 lightVerts)
{
    // Initalize ellipse in original clamped-cosine space
    real3   C  = 0.5 * (lightVerts[0] + lightVerts[2]);
    real3   V1 = 0.5 * (lightVerts[0] - lightVerts[3]);
    real3   V2 = 0.5 * (lightVerts[3] - lightVerts[2]);

    real3   V3 = cross(V2, V1);         // Normal to ellipse's plane
    if( dot( V3, C) < 0.0 )
        return 0.0;

    // compute eigenvectors of ellipse
    real    a, b;
    real    d11 = dot( V1, V1 );
    real    d22 = dot( V2, V2 );
    real    d12 = dot( V1, V2 );
    real    d11d22 = d11 * d22;
    real    d12d12 = d12 * d12;

//  if ( abs(d12) > 0.0001 * sqrt(d11d22) ) // This produces artifacts due to precision issues
    if ( d12d12 > 0.0001 * d11d22 )
    {
        real    tr = d11 + d22;
        real    det = -d12d12 + d11d22;

        // use sqrt matrix to solve for eigenvalues
        det = sqrt(det);
        real    u = 0.5 * sqrt( max( 0.0, tr - 2.0*det ) );
        real    v = 0.5 * sqrt( max( 0.0, tr + 2.0*det ) );
        real    e_max = Sq( u + v );
        real    e_min = Sq( u - v );

        real3   V1_, V2_;
        if ( d11 > d22 )
        {
            V1_ = d12*V1 + (e_max - d11)*V2;
            V2_ = d12*V1 + (e_min - d11)*V2;
        }
        else
        {
            V1_ = d12*V2 + (e_max - d22)*V1;
            V2_ = d12*V2 + (e_min - d22)*V1;
        }

        a = 1.0 / e_max;
        b = 1.0 / e_min;
        V1 = normalize( V1_ );
        V2 = normalize( V2_ );
    }
    else
    {
        a = 1.0 / dot(V1, V1);
        b = 1.0 / dot(V2, V2);
        V1 *= sqrt(a);
        V2 *= sqrt(b);
    }

    V3 = cross( V1, V2 );
    if ( dot( C, V3 ) <= 0.0 )
        V3 = -V3;

    real    L  = dot(V3, C);
    real    x0 = dot(V1, C) / L;
    real    y0 = dot(V2, C) / L;

    real    E1 = rsqrt(a);
    real    E2 = rsqrt(b);

    a *= L*L;
    b *= L*L;

    real    c0 = a*b;
    real    c1 = a*b*(1.0 + x0*x0 + y0*y0) - a - b;
    real    c2 = 1.0 - a*(1.0 + x0*x0) - b*(1.0 + y0*y0);
    real    c3 = 1.0;

    real3   roots = SolveCubic(real4(c0, c1, c2, c3));
    real    e1 = roots.x;
    real    e2 = roots.y;
    real    e3 = roots.z;

    #if 1
        real    ae2 = a - e2;
        real    be2 = b - e2;
        real3   avgDir = real3( a * x0 * be2, b * y0 * ae2, ae2 * be2 );   // No change except we avoid divisions...
    #else
        real3   avgDir = real3( a*x0/(a - e2), b*y0/(b - e2), 1.0 );
    #endif

    avgDir = normalize( mul( avgDir, real3x3( V1, V2, V3 ) ) );

    real    L1 = sqrt( -e2 / e3 );
    real    L2 = sqrt( -e2 / e1 );

    real    formFactor = L1*L2 * rsqrt( (1.0 + L1*L1) * (1.0 + L2*L2) );

    return formFactor * avgDir;
}

// L contains the 4 corners of the quad bounding the elliptical disc
real   LTCEvaluate_Disk(real4x3 L)
{
    real3   F = DiskFormFactor(L);

    // Clamp invalid values to avoid visual artifacts.
    real    f = length(F);
    real    cosOmega   = clamp(F.z / f, -1, 1);
    real    sqSinSigma = min(f, 0.999);

    return DiffuseSphereLightIrradiance(sqSinSigma, cosOmega);
}


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// LINEAR AREA LIGHTS
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//

real LineFpo(real tLDDL, real lrcpD, real rcpD)
{
    // Compute: ((l / d) / (d * d + l * l)) + (1.0 / (d * d)) * atan(l / d).
    return tLDDL + (rcpD * rcpD) * FastATan(lrcpD);
}

real LineFwt(real tLDDL, real l)
{
    // Compute: l * ((l / d) / (d * d + l * l)).
    return l * tLDDL;
}

// Computes the integral of the clamped cosine over the line segment.
// 'l1' and 'l2' define the integration interval.
// 'tangent' is the line's tangent direction.
// 'normal' is the direction orthogonal to the tangent. It is the shortest vector between
// the shaded point and the line, pointing away from the shaded point.
real LineIrradiance(real l1, real l2, real3 normal, real3 tangent)
{
    real d      = length(normal);
    real l1rcpD = l1 * rcp(d);
    real l2rcpD = l2 * rcp(d);
    real tLDDL1 = l1rcpD / (d * d + l1 * l1);
    real tLDDL2 = l2rcpD / (d * d + l2 * l2);
    real intWt  = LineFwt(tLDDL2, l2) - LineFwt(tLDDL1, l1);
    real intP0  = LineFpo(tLDDL2, l2rcpD, rcp(d)) - LineFpo(tLDDL1, l1rcpD, rcp(d));
    return intP0 * normal.z + intWt * tangent.z;
}

// Computes 1.0 / length(mul(ortho, transpose(inverse(invM)))).
real ComputeLineWidthFactor(real3x3 invM, real3 ortho)
{
    // transpose(inverse(M)) = (1.0 / determinant(M)) * cofactor(M).
    // Take into account that m12 = m21 = m23 = m32 = 0 and m33 = 1.
    real    det = invM._11 * invM._22 - invM._22 * invM._31 * invM._13;
    real3x3 cof = {invM._22, 0.0, -invM._22 * invM._31,
                   0.0, invM._11 - invM._13 * invM._31, 0.0,
                   -invM._13 * invM._22, 0.0, invM._11 * invM._22};

    // 1.0 / length(mul(V, (1.0 / s * M))) = abs(s) / length(mul(V, M)).
    return abs(det) / length(mul(ortho, cof));
}

// For line lights.
real LTCEvaluate(real3 P1, real3 P2, real3 B, real3x3 invM)
{
    real result = 0.0;
    // Inverse-transform the endpoints.
    P1 = mul(P1, invM);
    P2 = mul(P2, invM);

    // Terminate the algorithm if both points are below the horizon.
    if (!(P1.z <= 0.0 && P2.z <= 0.0))
    {
        real width = ComputeLineWidthFactor(invM, B);
    
        if (P1.z > P2.z)
        {
            // Convention: 'P2' is above 'P1', with the tangent pointing upwards.
            Swap(P1, P2);
        }
    
        // Recompute the length and the tangent in the new coordinate system.
        real  len = length(P2 - P1);
        real3 T   = normalize(P2 - P1);
    
        // Clip the part of the light below the horizon.
        if (P1.z <= 0.0)
        {
            // P = P1 + t * T; P.z == 0.
            real t = -P1.z / T.z;
            P1 = real3(P1.xy + t * T.xy, 0.0);
    
            // Set the length of the visible part of the light.
            len -= t;
        }
    
        // Compute the normal direction to the line, s.t. it is the shortest vector
        // between the shaded point and the line, pointing away from the shaded point.
        // Can be interpreted as a point on the line, since the shaded point is at the origin.
        real  proj = dot(P1, T);
        real3 P0   = P1 - proj * T;
    
        // Compute the parameterization: distances from 'P1' and 'P2' to 'P0'.
        real l1 = proj;
        real l2 = l1 + len;
    
        // Integrate the clamped cosine over the line segment.
        real irradiance = LineIrradiance(l1, l2, P0, T);
    
        // Guard against numerical precision issues.
        result = max(INV_PI * width * irradiance, 0.0);
    }
    return result;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// GENERIC EVALUATE (RECTANGLE + DISK + SPHERE LIGHTS)
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
real LTCEvaluate_RectDisk(real4x3 L, bool isRectangleLight)
{
    real3   F;
    if (isRectangleLight)
        F = PolygonFormFactor(L);
    else
        F = DiskFormFactor(L);

    // Clamp invalid values to avoid visual artifacts.
    real    f = length(F);
    real    cosOmega   = clamp(F.z / f, -1, 1);
    real    sqSinSigma = min(f, 0.999);

    return DiffuseSphereLightIrradiance(sqSinSigma, cosOmega);
}

real3   LTCEvaluate_RectDisk(real4x3 L, bool isRectangleLight, int cookieIndex)
{
    real3   F;
    if (isRectangleLight)
        F = PolygonFormFactor(L);
    else
        F = DiskFormFactor(L);

    // Clamp invalid values to avoid visual artifacts.
    real    f = length(F);
            F /= f;
    real    cosOmega   = clamp(F.z, -1, 1);
    real    sqSinSigma = min(f, 0.999);

    float3  irradiance = DiffuseSphereLightIrradiance(sqSinSigma, cosOmega);
    if ( cookieIndex >= 0 )
        irradiance *= SampleAreaLightCookie( cookieIndex, L, F );

    return irradiance;
}

#endif // UNITY_AREA_LIGHTING_INCLUDED
