#ifndef UNITY_REFRACTION_INCLUDED
#define UNITY_REFRACTION_INCLUDED

//-----------------------------------------------------------------------------
// Util refraction
//-----------------------------------------------------------------------------

struct RefractionModelResult
{
    float       distance;       // length of the transmission during refraction through the shape
    float3      positionWS;     // out ray position
    float3      rayWS;          // out ray direction
};

RefractionModelResult RefractionModelSphere(float3 V, float3 positionWS, float3 normalWS, float ior, float thickness)
{
    // Sphere shape model:
    //  We approximate locally the shape of the object as sphere, that is tangent to the shape.
    //  The sphere has a diameter of {thickness}
    //  The center of the sphere is at {positionWS} - {normalWS} * {thickness}
    //
    //  So the light is refracted twice: in and out of the tangent sphere

    // First refraction (tangent sphere in)
    // Refracted ray
    float3 R1 = refract(-V, normalWS, 1.0 / ior);
    // Center of the tangent sphere
    float3 C = positionWS - normalWS * thickness * 0.5;

    // Second refraction (tangent sphere out)
    float NoR1 = dot(normalWS, R1);
    // Optical depth within the sphere
    float distance = -NoR1 * thickness;
    // Out hit point in the tangent sphere
    float3 P1 = positionWS + R1 * distance;
    // Out normal
    float3 N1 = normalize(C - P1);
    // Out refracted ray
    float3 R2 = refract(R1, N1, ior);
    float N1oR2 = dot(N1, R2);
    float VoR1 = dot(V, R1);

    RefractionModelResult result;
    result.distance = distance;
    result.positionWS = P1;
    result.rayWS = R2;

    return result;
}

RefractionModelResult RefractionModelPlane(float3 V, float3 positionWS, float3 normalWS, float ior, float thickness)
{
    // Plane shape model:
    //  We approximate locally the shape of the object as a plane with normal {normalWS} at {positionWS}
    //  with a thickness {thickness}

    // Refracted ray
    float3 R = refract(-V, normalWS, 1.0 / ior);

    // Optical depth within the thin plane
    float distance = thickness / dot(R, -normalWS);

    RefractionModelResult result;
    result.distance = distance;
    result.positionWS = positionWS + R * distance;
    result.rayWS = -V;

    return result;
}
#endif
