---
uid: urp-render-graph-compute-shader-run
---
# Run a compute shader in a render pass

To create a render pass that runs a compute shader, do the following:

1. Set up the render pass to use a compute shader.
2. Add an output buffer.
3. Pass in and execute the compute shader.
4. Get the output data from the output buffer.

To check if a platform supports compute shaders, use the [`SystemInfo.supportsComputeShaders`](https://docs.unity3d.com/ScriptReference/SystemInfo-supportsComputeShaders.html) API 

## Set up the render pass to use a compute shader

When you [create a `ScriptableRenderPass`](render-graph-write-render-pass.md), do the following:

1. Use `AddComputePass` instead of `AddRasterRenderPass`.
2. Use `ComputeGraphContext` instead of `RasterGraphContext`.

For example:

```csharp
class ComputePass : ScriptableRenderPass
{
    ...

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer contextData)
    {
        ...
        // Use AddComputePass instead of AddRasterRenderPass.
        using (var builder = renderGraph.AddComputePass("MyComputePass", out PassData data))
        {
            ...

            // Use ComputeGraphContext instead of RasterGraphContext.
            builder.SetRenderFunc((PassData data, ComputeGraphContext context) => ExecutePass(data, context));

            ...
        }
    }
}

```

## Add an output buffer

To create a buffer the compute shader outputs to, follow these steps:

1. Create a graphics buffer, then add a handle to it in your pass data.

    ```csharp
    // Declare an output buffer
    public GraphicsBuffer outputBuffer;

    // Add a handle to the output buffer in your pass data
    class PassData
    {
        public BufferHandle output;
    }

    // Create the buffer in the render pass constructor
    public ComputePass(ComputeShader computeShader)
    {
        // Create the output buffer as a structured buffer
        // Create the buffer with a length of 5 integers, so the compute shader can output 5 values.
        outputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 5, sizeof(int));
    }
    ```

2. Use the `ImportBuffer` render graph API to convert the buffer to a handle the render graph system can use, then set the `BufferHandle` field in the pass data. For example:

    ```csharp
    BufferHandle outputHandleRG = renderGraph.ImportBuffer(outputBuffer);
    passData.output = outputHandleRG;
    ```


3. Use the `UseBuffer` method to set the buffer as a writeable buffer in the render graph system.

    ```csharp
    builder.UseBuffer(passData.output, AccessFlags.Write);
    ```

## Pass in and execute the compute shader

Follow these steps:

1. Pass the compute shader to the render pass. For example, in a `ScriptableRendererFeature` class, expose a `ComputeShader` property, then pass the compute shader into the render pass class.

2. Add a `ComputeShader` field to your pass data, and set it to the compute shader. For example:

    ```csharp
    // Add a `ComputeShader` field to your pass data
    class PassData
    {
        ...
        public ComputeShader computeShader;
    }

    // Set the `ComputeShader` field to the compute shader
    passData.computeShader = yourComputeShader;
    ```

3. In your `SetRenderFunc` method, use the [`SetComputeBufferParam`](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.SetComputeBufferParam.html) API to attach the buffer to the compute shader. For example:

    ```csharp
    // The first parameter is the compute shader
    // The second parameter is the function that uses the buffer
    // The third parameter is the StructuredBuffer output variable to attach the buffer to
    // The fourth parameter is the handle to the output buffer
    context.cmd.SetComputeBufferParam(passData.computeShader, passData.computeShader.FindKernel("Main"), "outputData", passData.output);
    ```

4. Use the [`DispatchCompute`](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.DispatchCompute.html) API to execute the compute shader.

    ```csharp
    context.cmd.DispatchCompute(passData.computeShader, passData.computeShader.FindKernel("CSMain"), 1, 1, 1);
    ```

## Get the output data from the output buffer

To fetch the data from the output buffer, use the [`GraphicsBuffer.GetData`](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.GetData.html) API.

You can only fetch the data after the render pass executes and the compute shader finishes running.

For example:

```csharp
// Create an array to store the output data
outputData = new int[5];

// Copy the output data from the output buffer to the array
outputBuffer.GetData(outputData);
```

## Example

For a full example, refer to the example called **Compute** in the [render graph system URP package samples](package-samples.md).

## Additional resources

- [Compute shaders](https://docs.unity3d.com/6000.0/Documentation/Manual/class-ComputeShader.html)
- [Writing shaders](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-writing.html)
