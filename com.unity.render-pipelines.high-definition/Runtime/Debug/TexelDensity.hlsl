#ifdef DEBUG_DISPLAY

#ifndef UNITY_DEBUG_TEXEL_DENSITY_INCLUDED
#define UNITY_DEBUG_TEXEL_DENSITY_INCLUDED

static const float checkerboardDark = 0.6;
static const float checkerboardBright = 0.8;

float DrawCheckerboard(float2 pixelPosition, uint checkerboardSize)
{
    // Draws a checkerboard based on the absolute pixel positions (pixelPosition = uv * texSize)
    uint2 checkerboardIndexes = pixelPosition / checkerboardSize;
    return ((checkerboardIndexes.x) % 2) == ((checkerboardIndexes.y) % 2) ? checkerboardBright : checkerboardDark;
}

float3 CalculateTexelDensityDebugColor
(
    float texelDensity,
    float targetDensity,
    float log2stepsLimit,
    float3 targetColor,
    float3 lowerColor,
    float3 higherColor
)
{
    // Validation:
    // <-1: Lower density than the minimum allowed
    // [-1,0): Low density
    // 0: Target density
    // (0,1]: High density
    // >1: Higher density than the maximum allowed
    float validation = log2(texelDensity / targetDensity) / log2stepsLimit;

    float3 debugColor = targetColor;
    if (validation < 0)
        debugColor = lerp(debugColor, lowerColor, saturate(-validation));
    else
        debugColor = lerp(debugColor, higherColor, saturate(validation));
    return debugColor;
}

float3 DebugTexelDensityColor
(
    float3 worldPosition,
    float2 uv,
    float2 texDimension
)
{
    // CB Constants
    float targetDensity = _DebugTexelDensityTarget.w;
    float log2stepsLimit = _DebugTexelDensityLower.w;
    uint checkerboardPixels = _DebugTexelDensityHigher.w;
    float3 targetTDColor = _DebugTexelDensityTarget.rgb;
    float3 lowerTDColor = _DebugTexelDensityLower.rgb;
    float3 higherTDColor = _DebugTexelDensityHigher.rgb;

    // Texel density calculation
    float3 worldDeltaX = ddx(worldPosition);
    float3 worldDeltaY = ddy(worldPosition);
    float worldSurface = length(worldDeltaX) * length(worldDeltaY);

    float2 texelDeltaX = ddx(uv) * texDimension;
    float2 texelDeltaY = ddy(uv) * texDimension;
    float texelSurface = length(texelDeltaX) * length(texelDeltaY);

    float texelDensity = sqrt(texelSurface / worldSurface);

    // Colors
    float3 debugColor = CalculateTexelDensityDebugColor(
        texelDensity,
        targetDensity,
        log2stepsLimit,
        targetTDColor,
        lowerTDColor,
        higherTDColor);

    float checkerboard = DrawCheckerboard(uv * texDimension, checkerboardPixels);

    return debugColor * checkerboard;
}

#endif // UNITY_DEBUG_TEXEL_DENSITY_INCLUDED
#endif // DEBUG_DISPLAY