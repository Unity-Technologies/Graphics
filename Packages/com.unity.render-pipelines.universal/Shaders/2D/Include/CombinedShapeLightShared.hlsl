#ifndef COMBINED_SHAPE_LIGHT_PASS
#define COMBINED_SHAPE_LIGHT_PASS

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightVariables.hlsl"
#if defined(DEBUG_DISPLAY)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
#endif

half4 CombinedShapeLightShared(in SurfaceData2D surfaceData, in InputData2D inputData)
{
    #if defined(DEBUG_DISPLAY)
    half4 debugColor = 0;

    if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
    {
        return debugColor;
    }
    #endif

    half alpha = surfaceData.alpha;
    half4 color = half4(surfaceData.albedo, alpha);
    const half4 mask = surfaceData.mask;
    half2 lightingUV = inputData.lightingUV;

#if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION)
    // When this pass renders straight to the backbuffer (2D renderer with no intermediate color
    // target), Vulkan swapchain pre-transform has rotated clip space, so lightingUV is a physical
    // screen coordinate. The shape light textures are offscreen and so are still in the logical
    // orientation - undo the rotation before sampling them. lightingUV comes from ComputeScreenPos()
    // followed by the perspective divide, so this is the positionNDC overload. UUM-147359.
    lightingUV = half2(RemovePretransformRotation(float2(lightingUV)));
#endif

    if (alpha == 0.0)
        discard;

#if USE_SHAPE_LIGHT_TYPE_0
    half4 shapeLight0 = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, lightingUV);

    if (any(_ShapeLightMaskFilter0))
    {
        half4 processedMask = (1 - _ShapeLightInvertedFilter0) * mask + _ShapeLightInvertedFilter0 * (1 - mask);
        shapeLight0 *= dot(processedMask, _ShapeLightMaskFilter0);
    }

    half4 shapeLight0Modulate = shapeLight0 * _ShapeLightBlendFactors0.x;
    half4 shapeLight0Additive = shapeLight0 * _ShapeLightBlendFactors0.y;
#else
    half4 shapeLight0Modulate = 0;
    half4 shapeLight0Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_1
    half4 shapeLight1 = SAMPLE_TEXTURE2D(_ShapeLightTexture1, sampler_ShapeLightTexture1, lightingUV);

    if (any(_ShapeLightMaskFilter1))
    {
        half4 processedMask = (1 - _ShapeLightInvertedFilter1) * mask + _ShapeLightInvertedFilter1 * (1 - mask);
        shapeLight1 *= dot(processedMask, _ShapeLightMaskFilter1);
    }

    half4 shapeLight1Modulate = shapeLight1 * _ShapeLightBlendFactors1.x;
    half4 shapeLight1Additive = shapeLight1 * _ShapeLightBlendFactors1.y;
#else
    half4 shapeLight1Modulate = 0;
    half4 shapeLight1Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_2
    half4 shapeLight2 = SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, lightingUV);

    if (any(_ShapeLightMaskFilter2))
    {
        half4 processedMask = (1 - _ShapeLightInvertedFilter2) * mask + _ShapeLightInvertedFilter2 * (1 - mask);
        shapeLight2 *= dot(processedMask, _ShapeLightMaskFilter2);
    }

    half4 shapeLight2Modulate = shapeLight2 * _ShapeLightBlendFactors2.x;
    half4 shapeLight2Additive = shapeLight2 * _ShapeLightBlendFactors2.y;
#else
    half4 shapeLight2Modulate = 0;
    half4 shapeLight2Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_3
    half4 shapeLight3 = SAMPLE_TEXTURE2D(_ShapeLightTexture3, sampler_ShapeLightTexture3, lightingUV);

    if (any(_ShapeLightMaskFilter3))
    {
        half4 processedMask = (1 - _ShapeLightInvertedFilter3) * mask + _ShapeLightInvertedFilter3 * (1 - mask);
        shapeLight3 *= dot(processedMask, _ShapeLightMaskFilter3);
    }

    half4 shapeLight3Modulate = shapeLight3 * _ShapeLightBlendFactors3.x;
    half4 shapeLight3Additive = shapeLight3 * _ShapeLightBlendFactors3.y;
#else
    half4 shapeLight3Modulate = 0;
    half4 shapeLight3Additive = 0;
#endif

    half4 finalOutput;
#if !USE_SHAPE_LIGHT_TYPE_0 && !USE_SHAPE_LIGHT_TYPE_1 && !USE_SHAPE_LIGHT_TYPE_2 && ! USE_SHAPE_LIGHT_TYPE_3
    finalOutput = color;
#else
    half4 finalModulate = shapeLight0Modulate + shapeLight1Modulate + shapeLight2Modulate + shapeLight3Modulate;
    half4 finalAdditve = shapeLight0Additive + shapeLight1Additive + shapeLight2Additive + shapeLight3Additive;
    finalOutput = _HDREmulationScale * (color * finalModulate + finalAdditve);
#endif

    finalOutput.a = alpha;

    return max(0, finalOutput);
}
#endif
