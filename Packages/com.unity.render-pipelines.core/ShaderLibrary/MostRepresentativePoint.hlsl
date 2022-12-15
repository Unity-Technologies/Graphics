#ifndef UNITY_AREA_MRP_INCLUDED
#define UNITY_AREA_MRP_INCLUDED

// Ref: Moving Frostbite to PBR (Listing 11).
// Returns the solid angle of a rectangle at the point.
float SolidAngleRectangle(float3 positionWS, float4x3 lightVerts)
{
    float3 v0 = lightVerts[0] - positionWS;
    float3 v1 = lightVerts[1] - positionWS;
    float3 v2 = lightVerts[2] - positionWS;
    float3 v3 = lightVerts[3] - positionWS;

    float3 n0 = normalize(cross(v0, v1));
    float3 n1 = normalize(cross(v1, v2));
    float3 n2 = normalize(cross(v2, v3));
    float3 n3 = normalize(cross(v3, v0));

    float g0 = FastACos(dot(-n0, n1));
    float g1 = FastACos(dot(-n1, n2));
    float g2 = FastACos(dot(-n2, n3));
    float g3 = FastACos(dot(-n3, n0));

    return g0 + g1 + g2 + g3 - TWO_PI;
}

// Optimized (and approximate) solid angle routine. Doesn't handle the horizon.
float SolidAngleRightPyramid(float positionWS, float lightPositionWS, float halfWidth, float halfHeight)
{
    const float a = halfWidth;
    const float b = halfHeight;
    const float h = length(positionWS - lightPositionWS);

    return 4.0 * FastASin(a * b / sqrt (( a * a + h * h) * (b * b + h * h) ));
}

float FlatAngleSegment(float3 positionWS, float3 lightP1, float3 lightP2)
{
    float3 v0 = normalize(lightP1 - positionWS);
    float3 v1 = normalize(lightP2 - positionWS);

    return FastACos(dot(v0,v1));
}

// Ref: Moving Frostbite to PBR (Appendix E, Listing E.2)
// Returns the closest point to a rectangular shape defined by right and up (and the rect extents).
float3 ClosestPointRectangle(float3 positionWS, float3 planeOrigin, float3 left, float3 up, float halfWidth, float halfHeight)
{
    float3 dir = positionWS - planeOrigin;

    // Project into the 2D light plane.
    float2 dist2D = float2(dot(dir, left), dot(dir, up));

    // Clamp within the rectangle.
    const float2 halfSize = float2(halfWidth, halfHeight);
    dist2D = clamp(dist2D, -halfSize, halfSize);

    // Compute the new world position.
    return planeOrigin + dist2D.x * left + dist2D.y * up;
}

// Ref: Moving Frostbite to PBR (Listing 13)
float3 ClosestPointLine(float3 a, float3 b, float3 c)
{
    float3 ab = b - a;
    float t = dot(c - a, ab) / dot(ab, ab);
    return a + t * ab;
}

float3 ClosestPointSegment(float3 a, float3 b, float3 c)
{
    float3 ab = b - a;
    float t = dot(c - a, ab) / dot(ab, ab);
    return a + saturate(t) * ab;
}

#endif // UNITY_AREA_MRP_INCLUDED
