# URP ShaderLab Pass tags

This section contains descriptions of URP-specific ShaderLab Pass tags.

## URP Pass tags: LightMode

The value of this tag lets the pipeline determine which Pass to use when executing different parts of the Render Pipeline.

If you do not set the `LightMode` tag in a Pass, URP uses the `SRPDefaultUnlit` tag value for that Pass.

In URP, the `LightMode` tag can have the following values.

| **Property** | **Description** |
| :--- | :--- |
| **UniversalForward** | The Pass renders object geometry and evaluates all light contributions. URP uses this tag value in the Forward Rendering Path. |
| **Universal2D** | The Pass renders objects and evaluates 2D light contributions. URP uses this tag value in the 2D Renderer. |
| **ShadowCaster** | The Pass renders object depth from the perspective of lights into the Shadow map or a depth texture. |
| **DepthOnly** | The Pass renders only depth information from the perspective of a Camera into a depth texture. |
| **Meta** | Unity executes this Pass only when baking lightmaps in the Unity Editor. Unity strips this Pass from shaders when building a Player. |
| **SRPDefaultUnlit** | Use this `LightMode` tag value to draw an extra Pass when rendering objects. Application example: draw an object outline. This tag value is valid for both the Forward and the Deferred Rendering Paths.<br/>URP uses this tag value as the default value when a Pass does not have a `LightMode` tag. |

> **NOTE**: URP does not support the following LightMode tags: `Always`, `ForwardAdd`, `PrepassBase`, `PrepassFinal`, `Vertex`, `VertexLMRGBM`, `VertexLM`.
