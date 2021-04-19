# **Camera-relative rendering**

Camera-relative rendering allows the High Definition Render Pipeline (HDRP) to render distant GameObjects (with large world space coordinates) in a more robust and numerically stable way compared to the built-in render pipeline.

Absolute precision of floating point numbers decreases as numbers become larger. This means that GameObject coordinates become increasingly less precise the further the GameObject is from the origin of the Scene. The Mesh faces of distant GameObjects that are close to one another may appear in the same place and produce z-fighting artifacts. To fix this issue, camera-relative rendering replaces the world origin with the position of the Camera.

## Using Camera-relative rendering

Camera-relative rendering is enabled by default in the ShaderConfig.cs file (in your Project window go to **Packages > High Definition RP Config > Runtime > ShaderLibrary** and click on **ShaderConfig.cs**). To disable this feature, set `CameraRelativeRendering` to `0`, and then generate Shader includes to update the ShaderConfig.cs.hlsl file (menu: **Edit > Render Pipeline** and click **Generate Shader Includes)**.

## How Camera-relative rendering works

The camera-relative rendering process translates GameObjects and Lights by the negated world space Camera position before any other geometric transformations affect them. It then sets the world space Camera position to 0 and modifies all relevant matrices accordingly.

If you view the source files for pre-built HDRP Shaders, the view and view-projection matrices are Camera-relative, along with Light and surface positions. Most world space positions in HDRP Shaders are also Camera-relative.

**Exception**: `_WorldSpaceCameraPos` is never Camera-relative because HDRP uses it for coordinate space conversion.

## **Examples**

If you enable Camera-relative rendering:

- `GetAbsolutePositionWS(PositionInputs.positionWS)` returns the non-camera-relative world space position.
- `GetAbsolutePositionWS(float3(0, 0, 0))` returns the world space position of the camera equal to `_WorldSpaceCameraPos`.
- `GetCameraRelativePositionWS(_WorldSpaceCameraPos)` returns `float3(0, 0, 0)`.

If you disable Camera-relative rendering:

- `GetAbsolutePositionWS()` and `GetCameraRelativePositionWS()` return the position you pass into them without any modification.

 