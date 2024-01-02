# Upgrading to version 7.2.0 of the Universal Render Pipeline

On this page, you will find information about upgrading from an older version of the Universal Render Pipeline (URP) to the current version.

## Building your Project for consoles

To build a Project for a console, you need to install an additional package for each platform you want to support.

For more information, refer to the documentation on [Building for Consoles](Building-For-Consoles.md).

## Require Depth Texture
In previous versions of URP, if post-processing was enabled it would cause the pipeline to always require depth. We have improved the post-processing integration to only require depth from the pipeline when Depth of Field, Motion Blur or SMAA effects are enabled. This improves performance in many cases.

Because Cameras that use post-processing no longer require depth by default, you must now manually indicate that Cameras require depth if you are using it for other effects, such as soft particles.

To make all Cameras require depth, enable the the `Depth Texture` option in the [Pipeline Asset](universalrp-asset.md). To make an individual Camera require depth, set `Depth Texture` option to `On` in the [Camera Inspector](camera-component-reference.md).

## Sampling shadows from the Main Light
In previous versions of URP, if shadow cascades were enabled for the main Light, shadows would be resolved in a screen space pass. The pipeline now always resolves shadows while rendering opaque or transparent objects. This allows for consistency and solved many issues regarding shadows.

If have custom HLSL shaders and sample `_ScreenSpaceShadowmapTexture` texture, you must upgrade them to sample shadows by using the `GetMainLight` function instead.

For example:

```
float4 shadowCoord = TransformWorldToShadowCoord(positionWorldSpace);
Light mainLight = GetMainLight(inputData.shadowCoord);

// now you can use shadow to apply realtime occlusion
half shadow = mainLight.shadowAttenuation;
```

You must also define the following in your .shader file to make sure your custom shader can receive shadows correctly:

```
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
```

## Transparent receiving shadows
Transparent objects can now receive shadows when using shadow cascades. You can also optionally disable shadow receiving for transparent to improve performance. To do so, disable `Transparent Receive Shadows` in the Forward Renderer asset.
