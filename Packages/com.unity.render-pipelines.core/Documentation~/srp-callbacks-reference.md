# Scriptable Render Pipeline callbacks reference

When working with SRP, use these to make Unity call your C# code at specific times.

* [RenderPipeline.Render](xref:UnityEngine.Rendering.RenderPipeline.Render(UnityEngine.Rendering.ScriptableRenderContext,UnityEngine.Camera[])) is the main entry point to the SRP. Unity calls this method automatically. If you are writing a custom render pipeline, this is where you begin to write your code.
* The [RenderPipelineManager](xref:UnityEngine.Rendering.RenderPipelineManager) class has the following events that you can subscribe to, so that you can execute custom code at specific points in the render loop:
    * [beginFrameRendering](xref:UnityEngine.Rendering.RenderPipeline.BeginFrameRendering(UnityEngine.Rendering.ScriptableRenderContext,UnityEngine.Camera[])) - **Note:** This can generate garbage. Use `beginContextRendering` instead.
    * [endFrameRendering](xref:UnityEngine.Rendering.RenderPipeline.EndFrameRendering(UnityEngine.Rendering.ScriptableRenderContext,UnityEngine.Camera[])) - **Note:** This can generate garbage. Use `endContextRendering` instead.
    * [beginContextRendering](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.RenderPipelineManager-beginContextRendering.html)
    * [endContextRendering](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.RenderPipelineManager-endContextRendering.html)
    * [beginCameraRendering](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.RenderPipelineManager-beginCameraRendering.html)
    * [endCameraRendering](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.RenderPipelineManager-endCameraRendering.html)