# **Post-processing: propagating Not a Number or Infinite values**

Not a Number (NaN) and Infinite (Inf) values occur when shader operations produce an undefined result. Visually, they manifest as purely black or white pixels.

Example operations that can lead to a NaN/Inf are:

- Performing a square root (sqrt) or logarithm (log/log2) on any negative number produces a NaN.
- Performing a modulo operation, A % B, where A is infinite or B is 0 produces a NaN. 
- Dividing any number by 0 produces an Inf (for example, normalizing a vector with a length of 0).

Aside from shader operations, uninitialized memory can also contain/produce NaNs or Infs. On some platforms, the pixel values on newly created [Render Textures](https://docs.unity3d.com/Manual/class-RenderTexture.html) are not initialized with a value of 0. This means that if you access new Render Textures before writing to or clearing them, they might generate a NaN/Inf. As a general rule, you should always either clear your Render Textures before you use them, or write to every value you want to read from before you read from them. 

## Propagating NaNs/Infs

Any operation that has a NaN or an Inf as an operand also produces a NaN/Inf as a result. This is important in the High Definition Render Pipeline (HDRP), because any filtering or blur HDRP performs that involves NaN/Inf values spreads the invalid values further. 

A commonly reported issue in HDRP is that [bloom](Post-Processing-Bloom.md) generates a black screen. The bloom effect does not produce NaNs/Infs itself, but it spreads a NaN/Inf generated elsewhere across the screen. This happens because to calculate bloom, HDRP downsamples/filters the scene color and then upsamples it back to the desired resolution. If there is a single NaN/Inf on the screen, the downsample process spreads the invalid value until it covers the whole texture, and the subsequent upsampling therefore contains only NaN values, which leads to the full screen being black. 

For example, see a NaN caused by a material issue that spreads to the whole scene when HDRP calculates bloom:

![](Images/Post-processingPropagatingNaNsExample1.png)

![](Images/Post-processingPropagatingNaNsExample2.png)

![](Images/Post-processingPropagatingNaNsExample3.png)

![](Images/Post-processingPropagatingNaNsExample4.png)

A similar issue occurs when HDRP generates color pyramids for use by features such as [screen-space reflection](Override-Screen-Space-Reflection.md), [screen-space refraction](Override-Screen-Space-Refraction.md), and distortion. 

If you disable bloom and the screen stops being black, the cause of the black screen is likely because a single NaN/Inf pixel is present, but not really visible, and bloom propagated it across the whole screen. It is **not** because bloom created the invalid values.

## Fixing NaNs and Infs

The best way to stop bloom or other HDRP features from propagating NaN/Inf values is to fix the source of the NaN/Inf values. For information on how to do this, see [finding NaNs and Infs](#finding-nans-and-infs).

If you are unable to fix the source of the NaN/Inf values, [HDRP Cameras](HDRP-Camera.md) include a feature which replaces NaN and Inf values with a black pixel. This stops effects like bloom from propagating NaN/Inf values, but is a fairly resource intensive process. To enable this feature, select a Camera and, in the Inspector, enable the **Stop NaNs** checkbox. Note that you should only enable this feature if you are unable to fix the root cause of the NaN/Inf values.

### Finding NaNs and Infs

To find the root cause of a NaN/Inf, HDRP includes a debug mode which displays pixels that contain NaNs/Infs in a recognizable color. To use this debug mode:

1. Open the Render Pipeline Debug window (menu: **Window > Render Pipeline > Render Pipeline Debug**).
2. Go to **Rendering** and set **Fullscreen Debug Mode** to **NanTracker**.

This helps you to see if there are actually NaNs/Infs on screen and which material causes them. However, if you need more information, such as which particular draw call causes the issue, you can use frame debugging tools such as [RenderDoc](https://renderdoc.org/). For information on how to use RenderDoc to capture frames in Unity, see [RenderDoc integration](https://docs.unity3d.com/Manual/RenderDocIntegration.html).

A common situation that leads to NaNs is when a mesh is imported with ill-defined normals, like normals equal to the zero vector.
To find these normals, you can use one of the normal visualization modes in the [Material panel](Render-Pipeline-Debug-Window.md#material-panel) of the Render Pipeline Debug window.

#### RenderDoc

After you capture a frame, RenderDoc can display pixels with a NaN/Inf value as pure red, which helps you to find NaN/Inf values because it is much easier to see than the standard white/black pixels that HDRP renders for invalid values. To do this, in the Texture Viewer, open the **Overlay** drop-down and click the **NaN/Inf/-ve Display** option.

![](Images/Post-processingPropagatingNaNsRenderDoc.png)


Now that it is easier to see the NaN/Inf values, you can start to look for them. If they are still not obvious, you can look at the bloom dispatches to see where bloom propagates the NaN/Inf pixels from, then pinpoint the exact pixel/s responsible. Taking the example images in the [propagating NaNs/Infs section](#propagating-nans/infs), you can see by how bloom expands the NaN/Inf values that the source is around the center of the screen, on the sphere's Material.

After you find which Materials/shaders produce the NaNs/Infs, you can then debug them to find out which operation actually causes the invalid values.
