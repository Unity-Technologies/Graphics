# Introduction to resources in the render graph system in SRP Core

To declare a resource and use it in a render pass, for example a texture or a list of objects to render, call render graph APIs that return a handle to the resource. You can't directly allocate or dispose of resources yourself.

The render graph system only allocates the memory for a resource just before the first render pass that writes to it, and releases the memory after the last render pass that reads it. This lets render graph manage memory in the most efficient way possible, and avoid allocating memory for resources that aren't used in a frame.

You can create resources in two ways:

- Create a resource inside a render pass. These are called internal or transient resources. The render graph system handles the lifetime of these resources. You can't access them outside of the render graph instance, or pass them between frames or different render graphs.  

    For example:

    ```c#
    TextureHandle tempTexture = myRenderGraph.CreateTexture(textureProperties);
    ```

    For more information, refer to [Create a texture in the render graph system](render-graph-create-a-texture.md).

- Import an existing resource from outside a render pass, for example the camera back buffer, or textures you want a render graph to use across multiple frames. These are called imported or external resources. You manage their lifetime yourself. 

    For example:

    ```c#
    TextureHandle cameraColorBuffer = myRenderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget, cameraTargetProperties);            
    ```

    Imported resources are useful when the resource is used in one of two possible render passes. For example, using the color buffer in either a deferred lighting pass or a forward lighting pass.

    For more information, refer to [Import a texture into the render graph system](render-graph-import-a-texture.md).

Each API requires a descriptor structure as a parameter. The properties in these structures are similar to the properties in the resources they represent (respectively [RTHandle](rthandle-system.md), [ComputeBuffer](https://docs.unity3d.com/ScriptReference/ComputeBuffer.html), and [RendererLists](https://docs.unity3d.com/ScriptReference/Rendering.RendererList.html)).

For the complete list of APIs, refer to the [API documentation](../api/UnityEngine.Rendering.RenderGraphModule.RenderGraph.html).

