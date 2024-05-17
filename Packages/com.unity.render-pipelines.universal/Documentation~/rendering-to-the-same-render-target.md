# Set up split-screen rendering

In the Universal Render Pipeline (URP), multiple [Base Cameras](camera-types-and-render-type.md#base-camera) or [Camera Stacks](camera-stacking.md) can render to the same render target. This allows you to create effects such as split screen rendering.

If more than one Base Camera or Camera Stack renders to the same area of a render target, Unity draws each pixel in the overlapping area multiple times. Unity draws the Base Camera or Camera Stack with the highest priority last, on top of the previously drawn pixels. For more information on the camera render order optimization, refer to [Understand camera render order](cameras-advanced.md).

You use the Base Camera's **Viewport Rect** property to define the area of the render target to render to. For more information on viewport coordinates, refer to the [Unity Manual](https://docs.unity3d.com/Manual/class-Camera.html) and [API documentation](https://docs.unity3d.com/ScriptReference/Camera-rect.html).

## Setting up split screen rendering

![Setting up split screen rendering in URP](Images/camera-split-screen-viewport.png)

1. Create a Camera in your scene. Its **Render Mode** defaults to **Base**, making it a Base Camera.
2. Select the Camera. In the Inspector, scroll to the Output section. Change the values for **Viewport rect** to the following:
    * X: 0
    * Y: 0
    * W: 0.5
    * H: 1
3. Create another Camera in your scene. Its **Render Mode** defaults to **Base**, making it a Base Camera.
4. Select the Camera. In the Inspector, scroll to the Output section. Change the values for **Viewport rect** to the following:
    * X: 0.5
    * Y: 0
    * W: 0.5
    * H: 1

Unity renders the first Camera to the left-hand side of the screen, and the second Camera to the right-hand side of the screen.

You can change the Viewport rect for a Camera in a script by setting its `rect` property, like this:

```c#
myUniversalAdditionalCameraData.rect = new Rect(0.5f, 0f, 0.5f, 0f);
```
