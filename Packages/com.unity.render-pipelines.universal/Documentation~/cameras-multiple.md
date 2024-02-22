# Use multiple cameras

In the Universal Render Pipeline (URP), you can use multiple cameras to work with multiple camera outputs and targets, as well as different output resolutions and post-processing effects.

> [!NOTE]
> If you use multiple cameras, it might make rendering slower. An active camera adds extra calls to the rendering loop even if it renders nothing.

![An example of the effect camera stacking can produce in URP](Images/camera-stacking-example.png)<br/>*An example of the effect camera stacking can produce in URP.*

You can combine these ways of working for more complex effects. For example, you can define two Camera Stacks, and then set each of those to Camera Stacks that render to a different area of the same render target.

For information on Camera rendering order when working with multiple Cameras, refer to [Understand camera render order](cameras-advanced.md).

| Page | Description |
|-|-|
| [Set up a camera stack](camera-stacking.md)| Stack cameras to layer the outputs of multiple cameras into a single combined output. Camera stacking allows you to create effects such as 3D models in a 2D UI, or the cockpit of a vehicle.|
| [Set up split-screen rendering](rendering-to-the-same-render-target.md) | Render multiple camera outputs to a single render target to create effects such as split screen rendering. |
| [Apply different post processing effects to separate cameras](cameras/apply-different-post-proc-to-cameras.md) | Apply different post-processing setups to individual cameas within a scene. |
| [Render a camera's output to a Render Texture](rendering-to-a-render-texture.md) | Render to a Render Texture to create effects such as in-game CCTV monitors. |
