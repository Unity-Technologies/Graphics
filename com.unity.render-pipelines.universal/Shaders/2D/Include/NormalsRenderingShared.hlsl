#if !defined(NORMALS_RENDERING_PASS)
#define NORMALS_RENDERING_PASS

float4 NormalsRenderingShared(float4 color, float3 normalTS, float3 tangent, float3 bitangent, float3 normal)
{
    float4 normalColor;
    float3 normalWS = TransformTangentToWorld(normalTS, float3x3(tangent.xyz, bitangent.xyz, normal.xyz));
    float3 normalVS = TransformWorldToViewDir(normalWS);

    
    normalColor.rgb = 0.5 * ((normalVS)+1);
    normalColor.a = color.a;  // used for blending

    return normalColor;
}


#endif