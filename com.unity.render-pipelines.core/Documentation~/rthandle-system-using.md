## Using the RTHandle system

This page covers how to use the RTHandle system to manage render textures in your render pipeline. For information about the RTHandle system, see [RTHandle system](rthandle-system.md) and [RTHandle system fundamentals](rthandle-system-fundamentals.md).

### Initializing the RTHandle System

All operations related to `RTHandles` require an instance of the `RTHandleSystem` class. This class contains all the APIs necessary to allocate and release RTHandles as well as setting the **Reference Size** for the frame. This means that you need to create and maintain an instance of `RTHandleSystem` in your render pipeline or make use of the static RTHandles class mentioned later in this section. To create your own instance of `RTHandleSystem`, see the following code sample:

```c#
RTHandleSystem m_RTHandleSystem = new RTHandleSystem();
m_RTHandleSystem.Initialize(Screen.width, Screen.height, scaledRTsupportsMSAA: true, scaledRTMSAASamples: MSAASamples.MSAA4x);
```

When you initialize the system, you need to supply the starting resolution. The above code example uses the width and height of the screen. Since the RTHandle system only reallocates render textures when a camera requires a resolution larger than the current maximum size, the internal `RTHandle` resolution can only increase from the value you pass in here. It is good practice to initialize this resolution to be the resolution of the main display. This means the system does not need to unnecessarily reallocate the render textures (and cause unwanted memory spikes) at the beginning of the application.


If you want to use multi-sample anti-aliasing (MSAA), you need to declare the MSAA sample mode during initialization. In the example code above, the RTHandle system supports MSAA and uses the MSAA4x mode. The RTHandle system allocates all textures with the same number of samples. You can change the sample mode later, but note that this changes the sample mode for all automatically resized textures.


Note that you must only call the `Initialize` function once at the beginning of the application.

After this, you can use the initialized instance to allocate textures.

Since you allocate the majority of `RTHandles` from the same `RTHandleSystem` instance, the RTHandle system also provides a default global instance through the `RTHandles` static class. Rather than maintain your own instance of `RTHandleSystem`, this allows you to use the same API as you get with an instance, but not worry about the lifetime of the instance. Using the static instance, initialization becomes this:

```c#
RTHandles.Initialize(Screen.width, Screen.height, scaledRTsupportsMSAA: true, scaledRTMSAASamples: MSAASamples.MSAA4x);
```

The code examples in the rest of this page use the default global instance.

### Updating the RTHandle System

Before rendering with a camera, you need to set what resolution the RTHandle system uses as a reference size. To do so, call the `SetReferenceSize` function.

```c#
RTHandles.SetReferenceSize(width, hight, msaaSamples);
```

Calling this function has two effects:

1. If the new reference size you provide is bigger than the current one, the RTHandle system reallocates all the render textures internally to match the new size.
2. After that, the RTHandle system updates internal properties that set viewport and render texture scales for when the system uses RTHandles as active render textures.

### Allocating and releasing RTHandles

After you initialize an instance of `RTHandleSystem`, whether this is your own instance or the static default instance, you can use it to allocate RTHandles.

There are three main ways to allocate an `RTHandle`. They all use the same `Alloc` method on the RTHandleSystem instance. Most of the parameters of these functions are the same as the regular Unity RenderTexture ones, so for more information see the [RenderTexture API documentation](https://docs.unity3d.com/ScriptReference/RenderTexture.html). This section focuses on the parameters that relate to the size of the `RTHandle`:

- **Vector2 scaleFactor**: This variant requires a constant 2D scale for width and height. The RTHandle system uses this to calculate the resolution of the texture against the maximum reference size. For example, a scale of (1.0f, 1.0f) generates a full-screen texture. A scale of (0.5f 0.5f) generates a quarter-resolution texture.
- **ScaleFunc scaleFunc**: For cases when you don't want to use a constant scale to calculate the size of an `RTHandle`, you can provide a functor that calculates the size of the texture. The functor should take a Vector2Int as a parameter, which is the maximum reference size, and return a Vector2Int, which represents the size you want the texture to be.
- **Int width, int height**: This is for fixed-size textures. If you allocate a texture like this, it behaves like any regular RenderTexture.

There are also overrides that create RTHandles from [RenderTargetIdentifier](https://docs.unity3d.com/ScriptReference/Rendering.RenderTargetIdentifier.html)*,* [RenderTextures](https://docs.unity3d.com/ScriptReference/RenderTexture.html), or [Textures](https://docs.unity3d.com/Manual/Textures.html)*.* These are useful when you want to use the RTHandle API to interact with all your textures, even though the texture might not be an actual `RTHandle`*.*

The following code sample contains example uses of the `Alloc` function: 

```c#
// Simple Scale
RTHandle simpleScale = RTHandles.Alloc(Vector2.one, depthBufferBits: DepthBits.Depth32, dimension: TextureDimension.Tex2D, name: "CameraDepthStencil");

// Functor
Vector2Int ComputeRTHandleSize(Vector2Int screenSize)
{
    return DoSpecificResolutionComputation(screenSize);
}

RTHandle rtHandleUsingFunctor = RTHandles.Alloc(ComputeRTHandleSize, colorFormat: GraphicsFormat.R32_SFloat, dimension: TextureDimension.Tex2D);

// Fixed size
RTHandle fixedSize = RTHandles.Alloc(256, 256, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex2D);
```

When you no longer need a particular RTHande, you can release it. To do this, call the `Release` method.

```c#
myRTHandle.Release();
```

## Using RTHandles

After you allocate an RTHandle, you can use it exactly like a regular RenderTexture. There are implicit conversions to `RenderTargetIdentifier` as well as `RenderTexture` you can even use them with regular related Unity APIs.


However, when you use the RTHandle system, the actual resolution of the `RTHandle` might be different from the current resolution. For example, if the main camera renders at 1920x1080 and a secondary camera renders at 512x512, all RTHandle resolutions are based on the 1920x1080 resolution, even when rendering at lower resolutions. Because of this, take care when you set an RTHandle up as a render target. There are a number of APIs available in the [CoreUtils](../api/UnityEngine.Rendering.CoreUtils.html) class to help you with this. For example:

```c#
public static void SetRenderTarget(CommandBuffer cmd, RTHandle buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = -1)
```

This function sets the `RTHandle` as the active render target but also sets up the viewport based on the scale of the `RTHandle` and the current reference size, not the maximum size.


For example, when the reference size is 512x512, even if the maximum size is 1920x1080, a texture of scale (1.0f, 1.0f) uses the 512x512 size and therefore a 512x512 viewport. A (0.5f, 0.5f) scaled texture sets a viewport of 256x256 and so on. This means that, when using these helper functions, the RTHandle system generates the correct viewport based on the `RTHandle` parameters.

This example is one of many different overrides for the `SetRenderTarget` function. For the full list of overrides, see the [documentation](../api/UnityEngine.Rendering.CoreUtils.html#UnityEngine_Rendering_CoreUtils_SetRenderTarget_CommandBuffer_RenderTargetIdentifier_RenderBufferLoadAction_RenderBufferStoreAction_RenderTargetIdentifier_RenderBufferLoadAction_RenderBufferStoreAction_UnityEngine_Rendering_ClearFlag_).

## Using RTHandles in shaders

Usually, when sampling from a full-screen render texture in a shader, UVs span the whole 0 to 1 range. This is no longer always the case with `RTHandles`. It is possible that the current rendering only occurs in a partial viewport. To take this into account, you need to apply a scale to UVs when you sample `RTHandles` that use a scale. All the information necessary to handle `RTHandles` specificity inside shaders is in the `RTHandeProperties` structure that the `RTHandleSystem` instance provides. To access it, use:

```c#
RTHandleProperties rtHandleProperties = RTHandles.rtHandleProperties;
```

This structure contains the following properties:

```c#
public struct RTHandleProperties
{
    public Vector2Int previousViewportSize;
    public Vector2Int previousRenderTargetSize;
    public Vector2Int currentViewportSize;
    public Vector2Int currentRenderTargetSize;
	public Vector4 rtHandleScale;
}
```

It provides:

- The current viewport size. This is the reference size you set for rendering.
- The current render target size. This is the actual size of the render texture based on the maximum reference size.
- The `rtHandleScale`. This is the scale to apply to full-screen UVs to sample an RTHandle.

Values for previous frames are also available. For more information, see [Camera specific RTHandles](#camera-specific-rthandles). Generally, the most important property in this structure is `rtHandleScale`. It allows you to scale full-screen UV coordinates and use the result to sample an RTHandle. For example:

```c#
float2 scaledUVs = fullScreenUVs * rtHandleScale.xy;
```

However, because the partial viewport always starts at (0, 0), when you use integer pixel coordinates within the viewport to load content from a texture, there is no need to rescale them.

Another important thing to consider is that, when rendering a full-screen quad into a partial viewport, there is no benefit from standard UV addressing mechanisms such as wrap or clamp. This is because the texture might be bigger than the viewport. For this reason, take care when sampling pixels outside of the viewport.

### Custom SRP specific information

There are no shader constants provided by default with SRP. So, when you use RTHandles with your own SRP, you need to provide these constants to their shaders themselves.

## Camera specific RTHandles

Most of the render textures that a rendering loop uses can be shared by all cameras. As long as their content does not need to carry from one frame to another, this is fine. However, some render textures need persistence. A good example of this is using the main color buffer in subsequent frames for Temporal Anti-aliasing. This means that the camera cannot share its RTHandle with other cameras. Most of the time, this also means that these RTHandles need to be at least double-buffered (written to in the current frame, read from the previous frame). To address this problem, the RTHandle system includes `BufferedRTHandleSystems`. 

A `BufferedRTHandleSystem` is an `RTHandleSystem` that can multi-buffer RTHandles. The idea is to identify a buffer by a unique id and provide APIs to allocate a number of instances of the same buffer and retrieve them from previous frames. These are **history buffers**. Usually, you need to allocate one `BufferedRTHandleSystem` for each camera. Each one owns their camera-specific RTHandles.

One of the other consequences is that these history buffers don’t need to be allocated for every camera. For example, if a camera does not need Temporal Anti-aliasing, you can save the memory for it. Another consequence is that the system only allocates history buffers at the resolution of the camera. If the main camera is 1920x1080 and another camera renders in 256x256 and needs a history color buffer, this one only uses a 256x256 buffer and not a 1920*1080 buffer as the non-camera specific RTHandles would. To create an instance of a `BufferedRTHandleSystem`, see the following code sample:

```c#
BufferedRTHandleSystem  m_HistoryRTSystem = new BufferedRTHandleSystem();
```

To allocate an `RTHandle` using a `BufferedRTHandleSystem`, the process is different from a normal `RTHandleSystem`:

```c#
public void AllocBuffer(int bufferId, Func<RTHandleSystem, int, RTHandle> allocator, int bufferCount);
```

The `bufferId` is a unique id the system uses to identify the buffer. The allocator is a function you provide to actually allocate the `RTHandles` when needed (all instances are not allocated upfront), and the `bufferCount` is the number of instances requested.

From there, you can retrieve each `RTHandle` by its id and instance index like so:

```c#
public RTHandle GetFrameRT(int bufferId, int frameIndex);
```

The frame index is between zero and the number of buffers minus one. Zero always represents the current frame buffer, one the previous frame buffer, two the one before that, and so on.

To release a buffered RTHandle, call the `Release` function on the `BufferedRTHandleSystem`, passing in the id of the buffer to release:

```c#
public void ReleaseBuffer(int bufferId);
```

In the same way that you provide the reference size for regular `RTHandleSystems`, you need to do the same for each instance of `BufferedRTHandleSystem`*.*

```c#
public void SwapAndSetReferenceSize(int width, int height, MSAASamples msaaSamples);
```

This works the same way as regular RTHandleSystem but it also swaps the buffers internally so that the 0 index for `GetFrameRT` still references the current frame buffer. This slightly different way of handling camera-specific buffers also has implications when writing shader code.

With a multi-buffered approach like this, it means that `RTHandles` from a previous frame may have a different size to the one from the current frame (for example, this can happen with dynamic resolution or even just when resizing the window in the editor). This means that when you access a buffered `RTHandle` from a previous frame, you need to scale it accordingly. The scale used to do this is in `RTHandleProperties.rtHandleScale.zw`. It is used exactly the same way as `xy` for regular RTHandles. This is also the reason why `RTHandleProperties` contains the viewport and resolution of the previous frame. It can be useful when doing computation with history buffers.

## Dynamic Resolution

One of the byproducts of the RTHandle System design is that it can also be used to simulate software dynamic resolution. Since the current resolution of the camera is not directly correlated to the actual render texture objects, you can provide any resolution you want at the beginning of the frame and all render textures scale accordingly.

## Reset Reference Size

Sometimes, you might need to render to a higher resolution than normal for a short period of time. If your application does not require this resolution anymore, the additional memory allocated is wasted. To avoid that, you can reset the current maximum resolution of an `RTHandleSystem` like so:

```c#
RTHandles.ResetReferenceSize(newWidth, newHeight);
```

This forces the RTHandle system to reallocate all RTHandles to the new provided size. This is the only way to shrink down the size of `RTHandles`.