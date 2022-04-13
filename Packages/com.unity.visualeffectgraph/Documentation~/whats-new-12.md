# What's new in version 12

This page contains an overview of new features, improvements, and issues resolved in version 12 of the Visual Effect Graph, embedded in Unity 2021.2.

## Features

The following is a list of features Unity added to version 12 of the Visual Effect Graph, embedded in Unity 2021.12 Each entry includes a summary of the feature and a link to any relevant documentation.

### Fixed Lit Output for the Universal Render Pipeline (URP)

![](Images/banner-urp-fixed-lit-output.png)

This version of the Visual Effect Graph adds support for lit outputs in the Universal Render Pipeline (URP). You can use this to create effects that can respond to the lighting in the scene.

### 2D Renderer Support (Compute Capable Devices)

![](Images/banner-2d-renderer-support.png)

In this version, the Visual Effect Graph has added support for the Universal Render Pipeline’s (URP) 2D Renderer. This means that you can now render effects in a 2D project and sort them along with sprites in your scene.

For more information, see [Rendering in the Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest?/manual/rendering-in-universalrp.html).

### Bounds Helpers

![](Images/banner-bounds-helpers.png)

This version of the Visual Effect Graph adds helpers for setting and working with bounds. You can now record bounds in the Target GameObject window to ensure a more accurate fit. You can also set bounds automatically to make sure they are always visible.

This feature helps you create accurate bounds that match their systems so that effects don’t unexpectedly disappear when the camera moves.

For more information, see [Visual effect bounds](visual-effect-bounds.md).

### Graphics/Compute Buffer Support

![](Images/banner-graphics-compute-buffer-support.png)

VFX version 21.2 also adds support for Graphics/Compute buffers. This makes it easier to handle and transfer large amounts of data to a Visual Effect Graph. This is particularly useful for tracking multiple GameObject positions in your graph.

This feature requires C# knowledge to set and handle Graphics Buffers.

### Signed Distance Field Baker

![](Images/banner-sdf-baker.png)

This version includes the new Signed Distance Field (SDF) Bake Tool. To access it, navigate to **Window > Visual Effects > Utilities > SDF Bake Tool**. You can use this tool to quickly turn meshes and prefabs into SDF assets which you can use to create various effects, such as custom collisions, or to make particles conform to a particular shape.

For more information, see [Signed Distance Fields in the Visual Effect Graph](sdf-in-vfx-graph.md).
