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

## Updated

### Cloud Layer

When using the cloud layer in combination wiht the physically based sky, the sun light color will now correctly take atmospheric attenuation into account.
Additionally, the sun light color will now always impact the color of the clouds, even if the raymarching is disabled.
Improvements have also been made to the raymarching algorithm to improve scattering, and have more consistent results when changing the number of steps. Depending on your lightig conditions, you may have to tweak the density and exposure sliders to match the visuals with prior HDRP versions.
In the UI, **thickness** and **distortion** fields have been renamed to **density** and **wind**.

![](Images/cl-whats-new.png)
