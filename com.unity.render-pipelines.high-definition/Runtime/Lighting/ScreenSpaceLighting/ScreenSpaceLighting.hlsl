// Performs fading at the edge of the screen.
float EdgeOfScreenFade(float2 coordNDC, float fadeRcpLength)
{
    float2 coordCS = coordNDC * 2 - 1;
    float2 t = Remap10(abs(coordCS), fadeRcpLength, fadeRcpLength);
    return Smoothstep01(t.x) * Smoothstep01(t.y);
}

// Function that unpacks and evaluates the clear coat mask
// packedMask must be the value of GBuffer2 alpha.
// Caution: This need to be in sync with Lit.hlsl code
bool HasClearCoatMask(float4 packedMask)
{
    // We use a texture to identify if we use a clear coat constant for perceptualRoughness for SSR or use value from normal buffer.
    // When we use a forward material we can output the normal and perceptualRoughness for the coat for SSR, so we simply bind a black 1x1 texture
    // When we use deferred material we need to bind the gbuffer2 and read the coat mask
    float coatMask;
    uint materialFeatureId;
    UnpackFloatInt8bit(packedMask.a, 8, coatMask, materialFeatureId);
    return coatMask > 0.001; // If coat mask is positive, it mean we use clear coat
}
