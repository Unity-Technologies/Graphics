# What's new in HDRP version 14 / Unity 2022.2

This page contains an overview of new features, improvements, and issues resolved in version 14 of the High Definition Render Pipeline (HDRP), embedded in Unity 2022.2.

## Added

### Material Samples Transparency Scenes

![](Images/HDRP-MaterialSample-ShadowsTransparency.png)

These new scenes include examples and informations on how to setup properly transparents in your projects using different rendering methods (Rasterization, Ray Tracing, Path Tracing).
To take advantage of all the content of the sample, a GPU that supports [Ray Tracing](Ray-Tracing-Getting-Started.md) is needed.

### Ray Tracing Acceleration Structure Culling

In order to control the cost of building the ray tracing acceleration structure, new parameters have been added to the [Ray Tracing Settings](Ray-Tracing-Settings.md) volume component that allow you to define the algorithm that is going to perform the culling.
![](Images/new-ray-tracing-culling-mode.png)

HDRP can either extend the camera frustum, perform sphere culling or skip the culling step.
![](Images/RayTracingSettings_extended_frustum.gif)

### Fullscreen Shader Graph

![](Images/HDRP-Fullscreen-Frost-Effect.png)

HDRP 14.0 introduces a new material type in ShaderGraph to create fullscreen effects.
Shaders of the fullscreen type can be used in fullscreen custom passes, custom post processes and C# scripting.

For more details on how to use fulscreen shaders, see [FullScreen Shader Graph](Fullscreen-Shader-Graph.md).

## Updated

### Cloud Layer

When using the cloud layer in combination wiht the physically based sky, the sun light color will now correctly take atmospheric attenuation into account.
Additionally, the sun light color will now always impact the color of the clouds, even if the raymarching is disabled.
Improvements have also been made to the raymarching algorithm to improve scattering, and have more consistent results when changing the number of steps. Depending on your lightig conditions, you may have to tweak the density and exposure sliders to match the visuals with prior HDRP versions.
In the UI, **thickness** and **distortion** fields have been renamed to **density** and **wind**.

![](Images/cl-whats-new.png)

### Renderer bounds access in ShaderGraph

The [Object Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@13.1/manual/Object-Node.html) in Shader Graph has been updated to give access to the bounds of the current object being rendered. This information can be useful to compute refraction effect and such. Note that these abounds are available in world space.
