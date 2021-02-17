# Upgrading to version 11.0.x of the Universal Render Pipeline

This page describes how to upgrade from an older version of the Universal Render Pipeline (URP) to version 11.0.x.

## Upgrading from URP 10.0.x–10.2.x

1. The file names of the following Shader Graph shaders were renamed. The new file names do not have spaces:<br/>`Autodesk Interactive`<br/>`Autodesk Interactive Masked`<br/>`Autodesk Interactive Transparent`

    If your code uses the `Shader.Find()` method to search for the shaders, remove spaces from the shader names, for example, `Shader.Find("AutodeskInteractive)`.

## Upgrading from URP 7.2.x and later releases

1. URP 11.x.x does not support the package Post-Processing Stack v2. If your Project uses the package Post-Processing Stack v2, migrate the effects that use that package first.

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


## Upgrading from URP 7.0.x-7.1.x

1. Upgrade to URP 7.2.0 first. Refer to [Upgrading to version 7.2.0 of the Universal Render Pipeline](upgrade-guide-7-2-0).

2. URP 8.x.x does not support the package Post-Processing Stack v2. If your Project uses the package Post-Processing Stack v2, migrate the effects that use that package first.
