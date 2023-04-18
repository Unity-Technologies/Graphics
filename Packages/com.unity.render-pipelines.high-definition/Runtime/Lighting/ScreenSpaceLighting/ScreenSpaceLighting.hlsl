#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialGBufferMacros.hlsl"

// Performs fading at the edge of the screen.
float EdgeOfScreenFade(float2 coordNDC, float fadeRcpLength)
{
    float2 coordCS = coordNDC * 2 - 1;
    float2 t = Remap10(abs(coordCS), fadeRcpLength, fadeRcpLength);
    return Smoothstep01(t.x) * Smoothstep01(t.y);
}

// Coat mask is 5 bits for most material types
// But it's 4 bit for transmission, and unavailable for colored transmission
float UnpackCoatMask(float4 inGBuffer2)
{
    uint coatAndMode = UnpackByte(inGBuffer2.a);
    uint materialFeatureId = coatAndMode & 0x7;
    bool hasTransmissionTint = coatAndMode & 0x8;
    bool translucent = materialFeatureId == GBUFFER_LIT_TRANSMISSION;
    return translucent ? (hasTransmissionTint ? 0.0 : UnpackUIntToFloat(coatAndMode, 4, 4)) : UnpackUIntToFloat(coatAndMode, 3, 5);
}

// Function that unpacks and evaluates the clear coat mask
// packedMask must be the value of GBuffer2 alpha.
// Caution: This need to be in sync with Lit.hlsl code
bool HasClearCoatMask(float4 packedMask)
{
    // We use a texture to identify if we use a clear coat constant for perceptualRoughness for SSR or use value from normal buffer.
    // When we use a forward material we can output the normal and perceptualRoughness for the coat for SSR, so we simply bind a black 1x1 texture
    // When we use deferred material we need to bind the gbuffer2 and read the coat mask
    return UnpackCoatMask(packedMask) > 0.001;// If coat mask is positive, it mean we use clear coat
}
