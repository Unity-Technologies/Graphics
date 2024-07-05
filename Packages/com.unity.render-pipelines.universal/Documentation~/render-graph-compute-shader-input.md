---
uid: urp-render-graph-compute-shader-input
---

## Create input data for a compute shader

When you [run a compute shader in a render pass](render-graph-compute-shader-run.md), you can allocate a buffer to provide input data for the compute shader.

Follow these steps:

1. Create a graphics buffer, then add a handle to it in your pass data. For example:

    ```csharp
    // Declare an input buffer
    public GraphicsBuffer inputBuffer;

    // Add a handle to the input buffer in your pass data
    class PassData
    {
        ...
        public BufferHandle input;
    }

    // Create the buffer in the render pass constructor
    public ComputePass(ComputeShader computeShader)
    {
        // Create the input buffer as a structured buffer
        // Create the buffer with a length of 5 integers, so you can input 5 values.
        inputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 5, sizeof(int));
    }
    ```

2. Set the data in the buffer. For example:

    ```csharp
    var inputValues = new List<int> { 1, 2, 3, 4, 5 };
    inputBuffer.SetData(inputValues);
    ```

3. Use the `ImportBuffer` render graph API to convert the buffer to a handle the render graph system can use, then set the `BufferHandle` field in the pass data. For example:

    ```csharp
    BufferHandle inputHandleRG = renderGraph.ImportBuffer(inputBuffer);
    passData.input = inputHandleRG;
    ```
    
4. Use the `UseBuffer` method to set the buffer as a readable buffer in the render graph system. For example:

    ```csharp
    builder.UseBuffer(passData.input, AccessFlags.Read);
    ```
5. In your `SetRenderFunc` method, use the [`SetComputeBufferParam`](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.SetComputeBufferParam.html) API to attach the buffer to the compute shader. For example:

    ```csharp
    // The first parameter is the compute shader
    // The second parameter is the function that uses the buffer
    // The third parameter is the RWStructuredBuffer input variable to attach the buffer to
    // The fourth parameter is the handle to the input buffer
    context.cmd.SetComputeBufferParam(passData.computeShader, passData.computeShader.FindKernel("Main"), "inputData", passData.input);
    ```

## Example 

For a full example, refer to the example called **Compute** in the [Universal Render Pipeline (URP) package samples](package-samples.md).

## Additional resources

- [Compute shaders](https://docs.unity3d.com/6000.0/Documentation/Manual/class-ComputeShader.html)
- [Writing shaders](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-writing.html)
