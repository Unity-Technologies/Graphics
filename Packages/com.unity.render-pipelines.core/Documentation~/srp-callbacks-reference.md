# Use Scriptable Render Pipeline callbacks

To make Unity call your C# code at specific times, use the following APIs.

To write your custom render pipeline code, use the [RenderPipeline.Render](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rendering.RenderPipeline.Render.html) API. This method is the main entry point to the SRP, and Unity calls it automatically.

To execute custom code at specific points in the render loop, subscribe to the following events in the [RenderPipelineManager](xref:UnityEngine.Rendering.RenderPipelineManager) class:

* [beginContextRendering](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rendering.RenderPipelineManager-beginContextRendering.html)
* [endContextRendering](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rendering.RenderPipelineManager-endContextRendering.html)
* [beginCameraRendering](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rendering.RenderPipelineManager-beginCameraRendering.html)
* [endCameraRendering](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rendering.RenderPipelineManager-endCameraRendering.html)
