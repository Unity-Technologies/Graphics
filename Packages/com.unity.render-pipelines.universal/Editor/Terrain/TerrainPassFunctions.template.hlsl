// Both functions below are called from outside FEATURES_GRAPH_VERTEX, so the two terrain pass
// templates include this file unconditionally, after the pass structs. Do not move it behind the
// graphVertex feature gate. Keep this comment free of the template command character as well: the
// preprocessor reads commands inside comments and splices an error marker into the shader.

void ApplyTerrainInstancing(inout Attributes input)
{
    float4 positionOS = float4(input.positionOS, 1.0);
    #if defined(VARYINGS_NEED_TEXCOORD0) && defined(ATTRIBUTES_NEED_NORMAL)
        TerrainInstancing(positionOS, input.normalOS, input.uv0.xy);
    #elif defined(VARYINGS_NEED_TEXCOORD0)
        float3 normal = float3(0, 0, 0);
        TerrainInstancing(positionOS, normal, input.uv0.xy);
    #elif defined(ATTRIBUTES_NEED_NORMAL)
        TerrainInstancing(positionOS, input.normalOS);
    #else
        TerrainInstancing(positionOS);
    #endif
    input.positionOS = positionOS.xyz;
}

float4 ConstructTerrainTangent(float3 normal, float3 positiveZ)
{
    // Consider a flat terrain. It should have tangent be (1, 0, 0) and bitangent be (0, 0, 1) as the UV of the terrain grid mesh is a scale of the world XZ position.
    // In CreateTangentToWorld function (in SpaceTransform.hlsl), it is cross(normal, tangent) * sgn for the bitangent vector.
    // It is not true in a left-handed coordinate system for the terrain bitangent, if we provide 1 as the tangent.w. It would produce (0, 0, -1) instead of (0, 0, 1).
    // Also terrain's tangent calculation was wrong in a left handed system because cross((0,0,1), terrainNormalOS) points to the wrong direction as negative X.
    // Therefore all the 4 xyzw components of the tangent needs to be flipped to correct the tangent frame.
    // (See TerrainLitData.hlsl - GetSurfaceAndBuiltinData)
    float3 tangent = cross(normal, positiveZ);
    return float4(tangent, -1);
}
