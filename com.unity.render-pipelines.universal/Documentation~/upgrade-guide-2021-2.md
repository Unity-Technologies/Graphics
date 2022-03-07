# Upgrading to version 12 of the Universal Render Pipeline

This page describes how to upgrade from an older version of the Universal Render Pipeline (URP) to version 12.0.x.

For information on converting assets made for a Built-in Render Pipeline project to assets compatible with URP, see the page [Render Pipeline Converter](features/rp-converter.md).

## Upgrading from URP 11.x.x

* The Forward Renderer asset is renamed to the Universal Renderer asset. When you open an existing project in the Unity Editor containing URP 12, Unity updates the existing Forward Renderer assets to Universal Renderer assets.

* The Universal Renderer asset contains the property **Rendering Path** that lets you select the Forward or the Deferred Rendering Path.

* The method `ClearFlag.Depth` does not implicitly clear the Stencil buffer anymore. Use the new method `ClearFlag.Stencil`.

* URP 12 implements the [Render Pipeline Converter](features/rp-converter.md) feature. This feature replaces the asset upgrade functions that were previously available at **Edit > Render Pipeline > Universal Render Pipeline > Upgrade...**

## Upgrading from URP 10.0.x–10.2.x

1. The file names of the following Shader Graph shaders were renamed. The new file names do not have spaces:<br/>`Autodesk Interactive`<br/>`Autodesk Interactive Masked`<br/>`Autodesk Interactive Transparent`

    If your code uses the `Shader.Find()` method to search for the shaders, remove spaces from the shader names, for example, `Shader.Find("AutodeskInteractive)`.

## Upgrading from URP 7.2.x and later releases

1. URP 12.x.x does not support the package Post-Processing Stack v2. If your Project uses the package Post-Processing Stack v2, migrate the effects that use that package first.

### DepthNormals Pass

Starting from version 10.0.x, URP can generate a normal texture called `_CameraNormalsTexture`. To render to this texture in your custom shader, add a Pass with the name `DepthNormals`. For example, see the implementation in `Lit.shader`.

### Screen Space Ambient Occlusion (SSAO)

URP 10.0.x implements the Screen Space Ambient Occlusion (SSAO) effect.

If you intend to use the SSAO effect with your custom shaders, consider the following entities related to SSAO:

* The `_SCREEN_SPACE_OCCLUSION` keyword.

* `Input.hlsl` contains the new declaration `float2  normalizedScreenSpaceUV` in the `InputData` struct.

* `Lighting.hlsl` contains the `AmbientOcclusionFactor` struct with the variables for calculating indirect and direct occlusion:

    ```c++
    struct AmbientOcclusionFactor
    {
        half indirectAmbientOcclusion;
        half directAmbientOcclusion;
    };
    ```

* `Lighting.hlsl` contains the following function for sampling the SSAO texture:

    ```c++
    half SampleAmbientOcclusion(float2 normalizedScreenSpaceUV)
    ```

* `Lighting.hlsl` contains the following function:

    ```c++
    AmbientOcclusionFactor GetScreenSpaceAmbientOcclusion(float2
    normalizedScreenSpaceUV)
    ```

To support SSAO in custom shader, add the `DepthNormals` Pass and the `_SCREEN_SPACE_OCCLUSION` keyword the the shader. For example, see `Lit.shader`.

If your custom shader implements custom lighting functions, use the function `GetScreenSpaceAmbientOcclusion(float2 normalizedScreenSpaceUV)` to get the `AmbientOcclusionFactor` value for your lighting calculations.

### Shadow Normal Bias

In 11.0.x the formula used to apply Shadow Normal Bias has been slightly fix in order to work better with punctual lights.
As a result, to match exactly shadow outlines from earlier revisions, the parameter might to be adjusted in some scenes. Typically, using 1.4 instead of 1.0 for a Directional light is usually enough.

### Intermediate Texture

Previously, URP would force rendering to go through an intermediate renderer if the Renderer had any Renderer Features active. On some platforms, this has significant performance implications. Due to that, Renderer Features are now expected to declare their inputs using `ScriptableRenderPass.ConfigureInput`. This information is used to decide automatically whether rendering via an intermediate texture is necessary.

For compatibility reasons, a new property **Intermediate Texture** has been added to the Universal Renderer. This allows for either using the new behaviour, or to force the use of an intermediate texture. The latter should only be used if a Renderer Feature does not declare its inputs using `ScriptableRenderPass.ConfigureInput`.

All existing Universal Renderer assets that were using any Renderer Features (excluding those included with URP) are upgraded to force the use of an intermediate texture, such that existing setups will continue to work correctly. Any newly created Universal Renderer assets will default to the new behaviour.

## Upgrading from URP 7.0.x-7.1.x

1. Upgrade to URP 7.2.0 first. Refer to [Upgrading to version 7.2.0 of the Universal Render Pipeline](upgrade-guide-7-2-0.md).

2. URP 8.x.x does not support the package Post-Processing Stack v2. If your Project uses the package Post-Processing Stack v2, migrate the effects that use that package first.

## Upgrading from LWRP to 12.x.x

* There is no direct upgrade path from LWRP to URP 12.x.x. Follow the steps to upgrade LWRP to URP 11.x.x first, and then upgrade from URP 11.x.x to URP 12.x.x.
