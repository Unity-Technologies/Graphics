#if !defined(NORMALS_RENDERING_PASS)
#define NORMALS_RENDERING_PASS

half4 NormalsRenderingShared(half4 color, half3 normalTS, half3 tangent, half3 bitangent, half3 normal)
{
    // Account for sprite flip
    uint flipBits = asuint(unity_SpriteProps.x);
    float fx = (flipBits & 1u) ? -1.0f : 1.0f;
    float fy = (flipBits & 2u) ? -1.0f : 1.0f;
    normalTS.xy *= float2(fx, fy);
    half3 normalWS = TransformTangentToWorld(normalTS, half3x3(tangent.xyz, bitangent.xyz, normal.xyz));

    half4 normalColor;
    normalColor.rgb = 0.5 * ((normalWS)+1);
    normalColor.a = color.a;  // used for blending

    return normalColor;
}
#endif
