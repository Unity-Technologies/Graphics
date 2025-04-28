# What's new in version 11

This page contains an overview of new features, improvements, and issues resolved in version 11 of the Visual Effect Graph, embedded in Unity 2021.1.

## Features

The following is a list of features Unity added to version 11 of the Visual Effect Graph, embedded in Unity 2021.1. Each entry includes a summary of the feature and a link to any relevant documentation.

### SRP packages are part of the core

With the release of Unity 2021.1, graphics packages are relocating to the core of Unity. This move simplifies the experience of working with new Unity graphics features, as well as ensuring that your projects are always running on the latest verified graphics code.

For each release of Unity (alpha / beta / patch release) the graphics code is embedded within the main Unity installer. When you install the latest release of Unity, you also get the latest URP, HDRP, Shader Graph, VFX Graph, and more.

Tying graphics packages more closely to the main Unity release allows better testing to ensure that the graphics packages you use have been tested extensively with the version of Unity you have downloaded.

You can also use a local copy or a custom version of the graphics packages by overriding them in the manifest file.

For more information, see the following post on the forum: [SRP v11 beta is available now](https://forum.unity.com/threads/srp-v11-beta-is-available-now.1046539/).

### SkinnedMeshRenderer sampling

<video title="Examples of particles spawning on the surface of SkinnedMeshRenderers. On the left, a character fires a bow where the string and arrow are made of glowing particles. On the right, a walking zombie has tendrils all over its body that drop particles to the ground." src="Images/skinned-mesh-sampling-example.mp4" width="auto" height="auto" autoplay="true" loop="true" controls></video>
> Examples of particles spawning on the surface of SkinnedMeshRenderers. Models and animations from [Mixamo.com](https://www.mixamo.com/).

This version of the Visual Effect Graph adds the ability to sample [SkinnedMeshRenders](https://docs.unity3d.com/ScriptReference/SkinnedMeshRenderer.html). This enables you to retrieve vertex data from skinned geometry and use it for a variety of purposes, such as to spawn particles on animating characters.

For more information about this feature, see [Sample Mesh](Operator-SampleMesh.md).

### Texture2DArray flipbooks

From this version of the Visual Effect Graph, you can use Texture2DArray assets as flipbooks. Using Texture2DArrays prevent texture bleeding between the flipbook frames. To use Texture2DArrays, set the **Flipbook Layout** of an output Context to **Texture 2D Array**. This enables you to assign Texture2DArray assets to the Context's texture ports. Each slice of the texture corresponds to a frame within the flipbook. To play the flipbook, use the [Flipbook Player](Block-FlipbookPlayer.md) Block. To generally interact with a Texture2DArray flipbook, use the **Tex Index** attribute. This is useful if you want to use the flipbook for non-animation purposes, such as using a random flipbook frame per particle.

Many output Contexts share this setting so, for more information, see [Shared output settings and properties](Context-OutputSharedSettings.md).

### Exclude from temporal anti-aliasing

![A scene of a wooden tower exploding in a valley fortress. Left: Uses temporal anti-aliasing (TAA). The embers are harder to see and small ones are not there at all. Center: Excludes visual effects from TAA. All embers are clearer, small embers are still visible, and the rest of the image uses anti-aliasing. Right: Does not use TAA. All embers are clearer, small embers are still visible, but the rest of the image has no anti-aliasing.](Images/banner-exclude-from-taa.png)
> **Left**: Uses temporal anti-aliasing (TAA). The embers are harder to see and small ones are not there at all.<br/>**Center**: Excludes visual effects from TAA. All embers are clearer, small embers are still visible, and the rest of the image uses anti-aliasing.<br/>**Right**: Does not use TAA. All embers are clearer, small embers are still visible, but the rest of the image has no anti-aliasing.

This version of the Visual Effect Graph allows you to exclude visual effects when Unity calculates [temporal anti-aliasing (TAA)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Anti-Aliasing.html%23temporal-anti-aliasing-taa). This is useful because TAA can cause small particles to disappear. TAA is only available in the High Definition Render Pipeline (HDRP) which means this feature is only relevant to HDRP.

Many output Contexts share this setting so, for more information, see [Shared output settings and properties](Context-OutputSharedSettings.md).

### Operators

This version of the Visual Effect Graph introduces new Operators:

- [Sample Point Cache](Operator-SamplePointCache.md)
- [Sample Attribute Map](Operator-SampleAttributeMap.md)

## Improvements

The following is a list of improvements Unity made to the Visual Effect Graph in version 11, embedded in Unity 2021.1. Each entry includes a summary of the improvement and, if relevant, a link to any documentation.

### Mesh sampling improvements

![Five square meshes, each made up of two adjoining triangles, that demonstrate the new sampling methods this version supports. Vertex: Samples from the four corners. Edge: Samples along the edges of the triangles. Surface (random): Samples at random points on the surface. Surface (uniform): Samples in a regular alternating grid pattern across the surface. Surface (barycentric): Samples in a regular grid pattern across the surface.](Images/banner-mesh-sampling-11-improvements.png)

> Examples of the new sampling methods this version supports.

This version of the Visual Effect Graph improves on Mesh sampling so you can now sample from a Mesh's edges and surfaces. Also, all texture coordinate sample outputs now return a Vector4 instead of a Vector2. This enables you to pass on more per-vertex data from the effect to a shader. Lastly, this version added support for storing Colors using floats. Originally the Visual Effect Graph only supported byte storage.

For more information about these improvements, see [Sample Mesh](Operator-SampleMesh.md).

## Issues resolved

For information on issues resolved in version 11 of the Visual Effect Graph, see the [changelog](../changelog/CHANGELOG.html).
