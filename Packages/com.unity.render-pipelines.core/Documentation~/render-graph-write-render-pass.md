# Write a render pass using the render graph system in SRP Core

Write a render pass using the render graph system, after you use the `BeginRecording` API to start recording render graph commands.

**Note:** This section is about creating a custom render pipeline. To use the render graph system in a prebuilt Unity pipeline, refer to either [Render graph system in URP](https://docs.unity3d.com/Manual/urp/render-graph.html) or [Render graph system in HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/render-graph-introduction.html).

## Prerequisites

To add a render pass, you must first create a render graph instance and start recording. For more information, refer to [Write a render pipeline with render graph](render-graph-writing-a-render-pipeline.md).

## Write a render pass

1. Create a class or struct that declares the resources the render pass uses. For example:

    ```lang-cs
    class PassData
    {
        public TextureHandle cameraTarget;
        public TextureHandle sourceTexture;
    }
    ```

    Ensure that you declare only the variables that the render pass uses. Adding unnecessary variables can reduce performance.

2. Declare the render pass, for example using the `AddRasterRenderPass` method.

    ```lang-cs
    using (var builder = myRenderGraph.AddRasterRenderPass<PassData>("Example render pass", out var passData))
    {
    }
    ```

    The `builder` variable is an instance of the `IRasterRenderGraphBuilder` interface. This variable is the entry point for configuring the information related to the render pass.

2. Set the resources that the render pass uses. For example:

    ```lang-cs
    using (var builder = myRenderGraph.AddRasterRenderPass<PassData>("Example render pass", out var passData))
    {
        ...

        // Set the source texture as the camera target.
        passData.cameraTarget = myRenderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget, cameraTargetProperties);

        // Set the destination texture as a temporary texture.
        passData.sourceTexture = myRenderGraph.CreateTexture(textureProperties);

    }

    ```

    For more information, refer to [Resources in the render graph system](render-graph-resources.md).

3. Declare the inputs of the render pass using the `UseTexture` API, but don't add commands to command buffers. This is the recording stage.

    ```lang-cs
    builder.UseTexture(passData.cameraTarget, AccessFlags.Read);
    ```

4. Declare the output of the render pass using the `SetRenderAttachment` API. For example:

    ```lang-cs
    builder.SetRenderAttachment(passData.sourceTexture, 0, AccessFlags.Write);
    ```

5. To add commands, declare a rendering function using the `SetRenderFunc` API. Use a static method or a static lambda method. This is the execution stage. For example:

    ```lang-cs
    builder.SetRenderFunc(static (PassData passData, RasterGraphContext context) =>
    {
        context.cmd.ClearRenderTarget(true, true, Color.blue); 
    });
    ```

## Example

The following example sets a temporary texture as the input and the camera target as the output, then clears the camera target to blue. To use the example, add it to a custom render pipeline after the `BeginRecording` API. For more information, refer to [Write a render pipeline with render graph](render-graph-writing-a-render-pipeline.md).

This code example is simplified. It demonstrates the clearest workflow, rather than the most efficient runtime performance.

```c#
using (var builder = myRenderGraph.AddRasterRenderPass<PassData>("Example render pass", out var passData))
{

    RenderTargetInfo cameraTargetProperties = new RenderTargetInfo
    {
        width = cameraToRender.pixelWidth,
        height = cameraToRender.pixelHeight,
        volumeDepth = 1,
        msaaSamples = 1,
        format = GraphicsFormat.R8G8B8A8_UNorm
    };            
    passData.cameraTarget = myRenderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget, cameraTargetProperties);            

    var textureProperties = new TextureDesc(Vector2.one)
    {
        colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
        width = cameraTargetProperties.width,
        height = cameraTargetProperties.height,
        clearBuffer = true,
        clearColor = Color.red,
        name = "My temporary texture"
    };
    
    passData.sourceTexture = myRenderGraph.CreateTexture(textureProperties);

    passData.material = new Material(Shader.Find("Unlit/Texture")); 

    builder.UseTexture(passData.sourceTexture, AccessFlags.Read);
    builder.SetRenderAttachment(passData.cameraTarget, 0, AccessFlags.Write);

    // Make sure the render graph system keeps the render pass, even if it's not used in the final frame.
    // Don't use this in production code, because it prevents the render graph system from removing the render pass if it's not needed.
    builder.AllowPassCulling(false);            

    builder.SetRenderFunc(static (PassData passData, RasterGraphContext context) =>
    {
        // Create a quad mesh
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(0, 0, 0),
            new Vector3(1f, 0, 0),
            new Vector3(0, 1f, 0),
            new Vector3(1f, 1f, 0)
        };
        mesh.vertices = vertices;

        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        mesh.triangles = triangles;

        context.cmd.ClearRenderTarget(true, true, Color.blue); 

        // Pass the source texture to the shader
        passData.material.SetTexture("_MainTex", passData.sourceTexture);

        // Create a transformation matrix for the quad
        Matrix4x4 trs = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 0), Vector3.one);

        // Draw the quad onto the camera target, using the shader and the source texture
        context.cmd.DrawMesh(mesh, trs, passData.material, 0);
    });
}
```