# URP ShaderLab Pass tags

This section contains descriptions of URP-specific ShaderLab Pass tags.

## URP Pass tags: LightMode

The value of this tag lets the pipeline determine which Pass to use when executing different parts of the Render Pipeline.

If you do not set the `LightMode` tag in a Pass, URP uses the `SRPDefaultUnlit` tag value for that Pass.

In URP, the `LightMode` tag can have the following values.

| **Property** | **Description** |
| :--- | :--- |
| **UniversalForward** | The Pass renders object geometry and evaluates all light contributions. URP uses this tag value in the Forward Rendering Path. |
| **UniversalGBuffer** | The Pass renders object geometry without evaluating any light contribution. URP uses this tag value in the Deferred Rendering Path. |
| **UniversalForwardOnly** | The Pass renders object geometry and evaluates all light contributions, similarly to when **LightMode** has the **UniversalForward** value. The difference from **UniversalForward** is that URP can use the Pass for both the Forward and the Deferred Rendering Paths.<br/>Use this value if a certain Pass must render objects with the Forward Rendering Path when URP is using the Deferred Rendering Path. For example, use this tag if URP renders a Scene using the Deferred Rendering Path and the Scene contains objects with shader data that does not fit the GBuffer, such as Clear Coat normals.<br/>If a shader must render in both the Forward and the  Deferred Rendering Paths, declare two Passes with the `UniversalForward` and `UniversalGBuffer` tag values. If a shader must render using the Forward Rendering Path regardless of the Rendering Path that the URP Renderer uses, declare only a Pass with the `LightMode` tag set to `UniversalForwardOnly`. |
| **Universal2D** | The Pass renders objects and evaluates 2D light contributions. URP uses this tag value in the 2D Renderer. |
| **ShadowCaster** | The Pass renders object depth from the perspective of lights into the Shadow map or a depth texture. |
| **DepthOnly** | The Pass renders only depth information from the perspective of a Camera into a depth texture. |
| **Meta** | Unity executes this Pass only when baking lightmaps in the Unity Editor. Unity strips this Pass from shaders when building a Player. |
| **SRPDefaultUnlit** | Use this `LightMode` tag value to draw an extra Pass when rendering objects. Application example: draw an object outline. This tag value is valid for both the Forward and the Deferred Rendering Paths.<br/>URP uses this tag value as the default value when a Pass does not have a `LightMode` tag. |

> **NOTE**: URP does not support the following LightMode tags: `Always`, `ForwardAdd`, `PrepassBase`, `PrepassFinal`, `Vertex`, `VertexLMRGBM`, `VertexLM`.
