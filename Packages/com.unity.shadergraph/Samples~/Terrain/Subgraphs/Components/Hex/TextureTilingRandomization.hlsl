
// UV coordinates for the three corners of a triangle (r,g,b)
struct TriangleUV
{
    float2 r;
    float2 g;
    float2 b;
};

// float4 value, sampled from each of the three corners of a triangle (r,g,b)
struct TriangleFloat4
{
    float4 r, g, b;
};

// 
struct TrianglePoint
{
    TriangleUV vertexCoords;    // triangle vertices (in UV space)
    float3 weights;             // barycentric weights (relative to the vertices)
};


TrianglePoint GetGridTriangle(float2 uv)
{
    // this function assumes your UV space is tessellated into triangles
    // in a unit-square tiling pattern like so:
    //
    // uv (0, 1)            uv (1, 1)
    //       c _____________ d
    //        |\            |
    //        |  \          |
    //        |    \        |
    //        |      \      |
    //        |        \    |
    //        |          \  |
    //        |____________\|
    //       a               b
    // uv (0, 0)             uv (1, 0)
    //
    // this function determines the triangle to which the uv point belongs
    //
    // and returns the integer uv coordinates of the triangle vertices in Rcoord/Gcoord/Bcoord
    // as well as the barycentric coordinate weights of the uv point, in RGBweights
    //
    // this method ensures that RGBweights is continuous in UV space
    // and that coords returned are constant except when the corresponding weight is zero
    // in order to do so, it assigns RGB to vertices in a 3 tile repeating pattern
    // The pattern is organized like so (each tile numbered with tile type):
    //  
    //    :   :   :   :   :
    //    B---G---B---R---G ...
    //    | 1 | 2 | 0 | 1 |
    //    G---B---R---G---B ...
    //    | 2 | 0 | 1 | 2 |
    //    B---R---G---B---R ...
    //    | 0 | 1 | 2 | 0 |
    //    R---G---B---R---G ...
    // (0,0)
    //

    float2 f = frac(uv);                // subtile / local uv coordinates in [0,1]
    float2 a = floor(uv);               // coord of vertex 'a' in the tile
    float diag = (f.x + f.y) - 1.0f;    // [-1, 1] diagonal ramp from uv (0,0) to (1,1)
    float triFlip = 0.0f;               // lower triangle
    if (diag > 0.0f)
    {
        f = 1.0f - f.yx;                // in upper triangle, b and c weights are swapped and inverted
        triFlip = 1.0f;                 // upper triangle
    }
    float3 w = float3(abs(diag), f.xy); // vertex weights in abc(lower) or dbc(upper) order

    // tile type (mod 3) determines how to permute RGB vertex assignments around
    float tileType = a.x - a.y;

    // calculate permutation matrix
    float3 p = step((2.0f / 3.0f), frac((float3(0.0f, 1.0f, 2.0f) + tileType) * (1.0f / 3.0f) + (0.5f / 3.0f)));
    float3 pFlip = p * triFlip;

    // type    p            pFlip
    // 0       [0, 0, 1]    [0, 0, f]
    // 1       [0, 1, 0]    [0, f, 0]
    // 2       [1, 0, 0]    [f, 0, 0]

    TrianglePoint tri;
    tri.vertexCoords.r = a + pFlip.zz + p.xy;
    tri.vertexCoords.g = a + pFlip.yy + p.zx;
    tri.vertexCoords.b = a + pFlip.xx + p.yz;

    // permute weights via matrix multiply
    tri.weights.r = dot(p.zxy, w.xyz);
    tri.weights.g = dot(p.yzx, w.xyz);
    tri.weights.b = dot(p.xyz, w.xyz);

/*
    // more understandable permutation (maybe faster?)
    tileType = round(mod(tileType, 3.0f));
    if (tileType == 0.0f)
    {
        rgbWeights = w.xyz;
        rCoord = triFlip.xx;            // a or d
        gCoord = vec2(1.0f, 0.0f);      // b
        bCoord = vec2(0.0f, 1.0f);      // c
    }
    else if (tileType == 1.0f)
    {
        rgbWeights = w.yzx;
        rCoord = vec2(0.0f, 1.0f);          // c
        gCoord = triFlip.xx;                // a or d
        bCoord = vec2(1.0f, 0.0f);          // b
    }
    else // (tileType == 2.0f)
    {
        rgbWeights = w.zxy;
        rCoord = vec2(1.0f, 0.0f);          // b
        gCoord = vec2(0.0f, 1.0f);          // c
        bCoord = triFlip.xx;                // a or d
    }
    rCoord = Rcoord + a;
    gCoord = Gcoord + a;
    bCoord = Bcoord + a;
*/
    return tri;
}


void GetGridTriangle_float(float2 uv, out float2 uvR, out float2 uvG, out float2 uvB, out float3 weights)
{
    TrianglePoint p = GetGridTriangle(uv);
    uvR = p.vertexCoords.r;
    uvG = p.vertexCoords.g;
    uvB = p.vertexCoords.b;
    weights = p.weights;
}


void GetGridTriangle_half(half2 uv, out half2 uvR, out half2 uvG, out half2 uvB, out half3 weights)
{
    TrianglePoint p = GetGridTriangle(uv);
    uvR = p.vertexCoords.r;
    uvG = p.vertexCoords.g;
    uvB = p.vertexCoords.b;
    weights = p.weights;
}



float2 UVToHexGridUV(float2 uv, float hexSize)
{
    // sqrt32 is the height (altitude) of an equilateral triangle with side length 1.0
    float sqrt32 = sqrt(3.0f) / 2.0f;

    float2 hexUV;
    hexUV.y = uv.y * (1.0f / sqrt32);
    hexUV.x = uv.x - uv.y * (0.5f / sqrt32);
    hexUV /= hexSize;
    return hexUV;
}

float2 HexGridUVToUV(float2 uv, float hexSize)
{
    // sqrt32 is the height (altitude) of an equilateral triangle with side length 1.0
    float sqrt32 = sqrt(3.0f) / 2.0f;

    uv *= hexSize;
    uv.x += uv.y * 0.5f;
    uv.y *= sqrt32;

    return uv;
}


TriangleUV TriangleHexGridUVsToUV(TriangleUV uv, float hexSize)
{
    // sqrt32 is the height (altitude) of an equilateral triangle with side length 1.0
    float sqrt32 = sqrt(3.0f) / 2.0f;

    uv.r *= hexSize;
    uv.g *= hexSize;
    uv.b *= hexSize;

    uv.r.x += uv.r.y * 0.5f;
    uv.g.x += uv.g.y * 0.5f;
    uv.b.x += uv.b.y * 0.5f;

    uv.r.y *= sqrt32;
    uv.g.y *= sqrt32;
    uv.b.y *= sqrt32;

    return uv;
}


TrianglePoint GetHexGridTriangle(float2 uv, float hexSize, bool returnHexGridUVs)
{
    // this is the same as the above function, but for a regular hexagonal grid
    // all it does is convert the uv space to a tiled hexagon space UV, then call the function above
    // this transform places hexagon centers at integer coordinates by skewing the UV space
    float2 hexUV = UVToHexGridUV(uv, hexSize);

    TrianglePoint tri = GetGridTriangle(hexUV);

    // default coords are unique integer coords (from the skewed triangle space)
    // this will correct them to return original uv space coords
    if (!returnHexGridUVs)
    {
        tri.vertexCoords = TriangleHexGridUVsToUV(tri.vertexCoords, hexSize);
    }

    return tri;
}


void GetHexGridTriangle_float(float2 uv, float hexSize, bool returnHexGridUVs, out float2 uvR, out float2 uvG, out float2 uvB, out float3 weights)
{
    TrianglePoint p = GetHexGridTriangle(uv, hexSize, returnHexGridUVs);
    uvR = p.vertexCoords.r;
    uvG = p.vertexCoords.g;
    uvB = p.vertexCoords.b;
    weights = p.weights;
}


void GetHexGridTriangle_half(half2 uv, float hexSize, bool returnHexGridUVs, out half2 uvR, out half2 uvG, out half2 uvB, out half3 weights)
{
    TrianglePoint p = GetHexGridTriangle(uv, hexSize, returnHexGridUVs);
    uvR = p.vertexCoords.r;
    uvG = p.vertexCoords.g;
    uvB = p.vertexCoords.b;
    weights = p.weights;
}


float3 BlumBlumShub3(float3 v)
{
    return frac(v * v * 251.0f); // *v * 251.0f);
}


// optimized for good random results when sampled on an integer grid
float3 GridRandom3(float2 p)
{
    float3 q = float3(
        dot(p, float2(0.894549f, 0.293639f)),
        dot(p, float2(0.442711f, -0.703029f)),
        dot(p, float2(0.335439f, 0.735791f)));

    q = frac(q);
    q += q.yzx * q.zxy;
    q = frac(q * 251.419329184f);

    // additional randomization, if necessary for extremely large grids
    // q = BlumBlumShub3(q);

    return q;
}

void GridRandom3_float(float2 p, out float3 random3)
{
    random3 = GridRandom3(p);
}


float ApplyDetailContrast(float weight, float detail, float detailContrast)
{
    float result = max(0.01f * weight, detailContrast * (weight + detail) + 1.0f - (detail + detailContrast));
    return result;
}


float2 RandomTransformProjectedUV(float3 worldPosition, float2 hashCoord, float3 projDirection)
{
    float2 worldXZ = worldPosition.xz;

    // if we assume worldXZ is correct, and projDirection is the normal, we can approximate world position
    float worldY = -worldXZ.x * projDirection.x / projDirection.y +
                   -worldXZ.y * projDirection.z / projDirection.y;

    float3 worldPos = float3(worldXZ.x, worldY, worldXZ.y);

    float3 hash = GridRandom3(hashCoord);

    // random rotation
    float2 rot = float2(1.0f, 0.0f);

    // construct projection matrix
    float3 projF = normalize(projDirection);					// projection direction
    float3 projU = normalize(cross(projF, float3(rot.x, 0.0f, rot.y)));		// U direction is defined by rotation
    float3 projV = cross(projU, projF);										// V direction (don't have to normalize)

    float scale = lerp(0.8f, 1.2f, hash.z);

    float2 uv;
    uv.x = dot(projU, worldPos) * scale;
    uv.y = dot(projV, worldPos) * scale;

    // randomize uv offset
    uv += hash.xy * 10.0f;

    return uv;
}

void RandomTransformProjectedUV_float(float3 worldPosition, float2 hashCoord, float3 projDirection, out float2 outUV)
{
    outUV = RandomTransformProjectedUV(worldPosition, hashCoord, projDirection);
}

void RandomTransformProjectedUV_half(half3 worldPosition, half2 hashCoord, half3 projDirection, out half2 outUV)
{
    outUV = RandomTransformProjectedUV(worldPosition, hashCoord, projDirection);
}

// random3 should be random numbers between 0 and 1, used to drive the transform
// the values should remain constant in areas where you wish to have the same random transform
float2 RandomScaleOffsetUV(float2 uv, float scaleMin, float scaleMax, float3 random3)
{
    // randomize scale   
    float scale = lerp(scaleMin, scaleMax, random3.z);
    uv *= scale;

	// randomize uv offset
	uv += random3.xy;

    return uv;
}

void RandomScaleOffsetUV_float(float2 uv, float2 scaleRange, float3 random3, out float2 outUV)
{
    outUV = RandomScaleOffsetUV(uv, scaleRange.x, scaleRange.y, random3);
}


// random3 should be random numbers between 0 and 1, used to drive the transform
// the values should remain constant in areas where you wish to have the same random transform
float2 RandomScaleOffsetRotateUV(float2 uv, float scaleMin, float scaleMax, float rotationMinDegreees, float rotationMaxDegrees, float3 random3)
{
	// randomize scale
	float scale = lerp(scaleMin, scaleMax, random3.z);

	// randomize uv rotation
	random3.z = frac(random3.z * 16.0f);   // TODO: generate another random number (here we're just using the low bits of random3.z, so it's somewhat correlated with scale)
	float degrees = lerp(rotationMinDegreees, rotationMaxDegrees, random3.z);
	float radians = degrees * (2.0f * 3.14159265f) / 360.0f;
	float rx = cos(radians) * scale;
	float ry = sin(radians) * scale;
	float2 rot = float2(rx, ry);
	uv = float2(dot(uv, rot), dot(uv, float2(-rot.y, rot.x)));

	// randomize uv offset
	uv += random3.xy;

	return uv;
}


void RandomScaleOffsetRotateUV_float(float2 uv, float2 scaleRange, float2 rotationRangeDegrees, float3 random3, out float2 outUV)
{
	outUV = RandomScaleOffsetRotateUV(uv, scaleRange.x, scaleRange.y, rotationRangeDegrees.x, rotationRangeDegrees.y, random3);
}


TriangleFloat4 SampleTriangleTextures(TEXTURE2D_PARAM(textureName, samplerName), TriangleUV uv)
{
    TriangleFloat4 result;
    result.r = SAMPLE_TEXTURE2D(textureName, samplerName, uv.r);
    result.g = SAMPLE_TEXTURE2D(textureName, samplerName, uv.g);
    result.b = SAMPLE_TEXTURE2D(textureName, samplerName, uv.b);
    return result;
}


float4 BlendWithTriangleWeights(float3 triWeights, TriangleFloat4 values)
{
    return (triWeights.r * values.r +
            triWeights.g * values.g +
            triWeights.b * values.b);
}


float4 SelectNearestVertexValue(float3 triWeights, TriangleFloat4 values)
{
    triWeights = pow(triWeights, 40.0f);
    triWeights = triWeights / dot(triWeights, float3(1.0f, 1.0f, 1.0f));
    return BlendWithTriangleWeights(triWeights, values);
}


TriangleUV TriangleCoordsToUV(TriangleUV triCoords, float4 coordToUVScaleOffset)
{
    TriangleUV uv;
    uv.r = triCoords.r * coordToUVScaleOffset.xy + coordToUVScaleOffset.zw;
    uv.g = triCoords.g * coordToUVScaleOffset.xy + coordToUVScaleOffset.zw;
    uv.b = triCoords.b * coordToUVScaleOffset.xy + coordToUVScaleOffset.zw;
    return uv;
}


float4 SampleTextureRandomizedHexGrid(TEXTURE2D_PARAM(textureName, samplerName), float2 uv, float hexSize, float edgeContrast)
{
    // we want UVs in hexGrid UV space, as they are integer and more stable as hash random seeds
    TrianglePoint tri = GetHexGridTriangle(uv, hexSize, true);

    // randomize transform at each vertex (identified by coord)
    TriangleUV triUV;
    triUV.r = RandomScaleOffsetUV(uv, 0.8f, 1.2f, GridRandom3(tri.vertexCoords.r));
    triUV.g = RandomScaleOffsetUV(uv, 0.8f, 1.2f, GridRandom3(tri.vertexCoords.g));
    triUV.b = RandomScaleOffsetUV(uv, 0.8f, 1.2f, GridRandom3(tri.vertexCoords.b));

    // (optional) contrast enhance the blend -- should ideally turn this off at distance to avoid aliasing
    tri.weights = pow(tri.weights, edgeContrast);

    // normalize weights.  Not necessary if detail texture and contrast enhance are disabled.
    tri.weights = tri.weights / dot(tri.weights, 1.0f);

    // sample material for each triangle corner
    TriangleFloat4 triValues = SampleTriangleTextures(TEXTURE2D_ARGS(textureName, samplerName), triUV);

    // blend the material samples based on the weights
    float4 result = BlendWithTriangleWeights(tri.weights, triValues);

    return result;
}

