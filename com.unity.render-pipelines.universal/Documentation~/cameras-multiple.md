# Working with multiple cameras

In the Universal Render Pipeline (URP), you can work with multiple Cameras to:

* [Stack Cameras](camera-stacking.md) to layer the outputs of multiple Cameras into a single combined output. Camera Stacking allows you to create effects such as 3D models in a 2D UI, or the cockpit of a vehicle.
* [Render multiple Base Cameras or Camera Stacks to the same render target](rendering-to-the-same-render-target.md). This allows you to create effects such as split screen rendering.
* [Render a Base Camera or Camera Stack to a Render Texture](rendering-to-a-render-texture.md). Rendering to a Render Texture allows you to create effects such as CCTV monitors.

![Camera Stacking in URP](Images/camera-stacking-example.png)

You can combine these ways of working for more complex effects. For example, you can define two Camera Stacks, and then set each of those to Camera Stacks that render to a different area of the same render target.

For information on Camera rendering order when working with multiple Cameras, see [Rendering order and overdraw](cameras-advanced.md).
