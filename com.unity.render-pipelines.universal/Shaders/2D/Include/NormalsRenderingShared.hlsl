#if !defined(NORMALS_RENDERING_PASS)
#define NORMALS_RENDERING_PASS

half4 NormalsRenderingShared(half4 color, half3 normalTS, half3 tangent, half3 bitangent, half3 normal)
{
    half4 normalColor;
    half3 normalWS = TransformTangentToWorld(normalTS, half3x3(tangent.xyz, bitangent.xyz, normal.xyz));

    normalColor.rgb = 0.5 * ((normalWS)+1);
    normalColor.a = color.a;  // used for blending

    return normalColor;
}


#endif
