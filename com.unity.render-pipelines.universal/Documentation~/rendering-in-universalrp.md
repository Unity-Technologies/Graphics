#Rendering in the Universal Render Pipeline

The Universal Render Pipeline (UniversalRP) renders Scenes using the:

- Forward renderer
- [Shading models](shading-model.md) for shaders shipped with UniversalRP
- Camera
- [UniversalRP Asset](universalrp-asset.md)

In the Forward renderer, UniversalRP implements a rendering loop that tells Unity how to render a frame.



![The UniversalRP Forward rendering loop](Images/Graphics/Rendering_Flowchart.png)



When the [render pipeline is active in Graphics Settings](configuring-universalrp-for-use.md), Unity uses UniversalRP to render all Cameras in your Project, including game and Scene view cameras, Reflection Probes, and the preview windows in your Inspectors. 

The UniversalRP renderer executes a Camera loop for each Camera, which performs the following steps:

1. Culls rendered objects in your Scene
2. Builds data for the renderer
3. Executes a renderer that outputs an image to the framebuffer. 

For more information about each step, see [Camera loop](#Steps-in-the-camera-loop).

UniversalRP provides callbacks that you can use to execute code at the beginning and end of the rendering loop, as well at the beginning and end of each Camera loop. 

## Camera loop 

The Camera loop performs the following steps:

| Step                         | Description                                                  |
| ---------------------------- | ------------------------------------------------------------ |
| __Setup Culling Parameters__ | Configures parameters that determine how the culling system culls Lights and shadows. You can override this part of the render pipeline with a custom renderer. |
| __Culling__                  | Uses the culling parameters from the previous step to compute a list of visible renderers, shadow casters, and Lights that are visible to the Camera. Culling parameters and Camera [layer distances](https://docs.unity3d.com/ScriptReference/Camera-layerCullDistances.html) affect culling and rendering performance. |
| __Build Rendering Data__     | Catches information based on the culling output, quality settings from the [UniversalRP Asset](universalrp-asset.md), [Camera](camera.md), and the current running platform to build the `RenderingData`. The rendering data tells the renderer the amount of rendering work and quality required for the Camera and the currently chosen platform. |
| __Setup Renderer__           | Builds a list of render passes, and queues them for execution according to the rendering data. You can override this part of the render pipeline with a custom renderer. |
| __Execute Renderer__         | Executes each render pass in the queue. The renderer outputs the Camera image to the framebuffer. |

