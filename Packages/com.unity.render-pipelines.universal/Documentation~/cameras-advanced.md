# Clearing, rendering order and overdraw

<a name="clearing"></a>
## Clearing

In the Universal Render Pipeline (URP), Camera clearing behavior depends on the Camera's [Render Type](camera-types-and-render-type.md).

### Base Camera

#### Color buffer

At the start of its render loop, a Camera with the [Base Render Type](camera-types-and-render-type.md) can clear its color buffer to a Skybox, clear its color buffer to a solid color, or use an uninitialized color buffer. You can define this behavior using the **Background Type** property in the [Camera Inspector](camera-component-reference.md) when **Render Type** is set to **Base**.

Note that the contents of the uninitialized color buffer vary by platform. On some platforms, the unitialized color buffer will contain data from the previous frame. On other platforms, the unitialized color buffer will contain unintialized memory. You should choose to use an unitialized color buffer only if your Camera draws to every pixel in the color buffer, and you do not wish to incur the cost of an unnecessary clear operation.

#### Depth buffer

A Base Camera always clears its depth buffer at the start of each render loop.

## Overlay Camera

#### Color buffer

At the start of its render loop, an [Overlay Camera](camera-types-and-render-type.md#overlay-camera) receives a color buffer containing color data from the previous Cameras in the Camera stack. It does not clear the contents of the color buffer.

#### Depth buffer

At the start of its render loop, an Overlay Camera receives a depth buffer containing depth data from the previous Cameras in the Camera Stack. You can define this behavior using the **Clear Depth** property in the [Camera Inspector](camera-component-reference.md) when **Render Type** is set to **Overlay**.

When **Clear Depth** is set to true, the Overlay Camera clears the depth buffer and draws its view to the color buffer on top of any existing color data. When **Clear Depth** is set to false, the Overlay Camera tests against the depth buffer before drawing its view to the color buffer.

<a name="rendering-order"></a>

## Camera culling and rendering order

If your URP scene contains multiple Cameras, Unity performs their culling and rendering operations in a predictable order.

Once per frame, Unity performs the following operations:

1. Unity gets the list of all active [Base Cameras](camera-types-and-render-type.md#base-camera) in the scene.
2. Unity organises the active Base Cameras into 2 groups: Cameras that render their view to Render Textures, and Cameras that render their view to the screen.
3. Unity sorts the Base Cameras that render to Render Textures into **Priority** order, so that Cameras with a higher **Priority** value are drawn last.
4. For each Base Camera that renders to a Render Texture, Unity performs the following steps:
    1. Cull the Base Camera
    2. Render the Base Camera to the Render Texture
    3. For each [Overlay Camera](camera-types-and-render-type.md#overlay-camera) that is part of the Base Camera's [Camera Stack](camera-stacking.md), in the order defined in the Camera Stack:
        1. Cull the Overlay Camera
        2. Render the Overlay Camera to the Render Texture
5. Unity sorts the Base Cameras that render to the screen into **Priority** order, so that Cameras with a higher **Priority** value are drawn last.
6. For each Base Camera that renders to the screen, Unity performs the following steps:
    1. Cull the Base Camera
    2. Render the Base Camera to the screen
    3. For each Overlay Camera that is part of the Base Camera's Camera Stack, in the order defined in the Camera Stack:
        1. Cull the Overlay Camera
        2. Render the Overlay Camera to the screen

Unity can render an Overlay Camera’s view multiple times during a frame - either because the Overlay Camera appears in more than one Camera Stack, or because the Overlay Camera appears in the same Camera Stack more than once. When this happens, Unity does not reuse any element of the culling or rendering operation. The operations are repeated in full, in the order detailed above.

> **Note**: In this version of URP, Overlay Cameras and Camera Stacking are supported only when using the Universal Renderer. Overlay Cameras will not perform any part of their rendering loop if using the 2D Renderer.

<a name="overdraw"></a>
## Overdraw

URP performs several optimizations within a Camera, including rendering order optimizations to reduce overdraw. However, when you use a Camera Stack, you effectively define the order in which those Cameras are rendered. You must therefore be careful not to order the Cameras in a way that causes excessive overdraw.

When multiple Cameras in a Camera Stack render to the same render target, Unity draws each pixel in the render target for each Camera in the Camera Stack. Additionally, if more than one Base Camera or Camera Stack renders to the same area of the same render target, Unity draws any pixels in the overlapping area again, as many times as required by each Base Camera or Camera Stack.

You can use Unity's [Frame Debugger](https://docs.unity3d.com/Manual/FrameDebugger.html), or platform-specific frame capture and debugging tools, to understand where excessive overdraw occurs in your scene. You can then optimize your Camera Stacks accordingly.
