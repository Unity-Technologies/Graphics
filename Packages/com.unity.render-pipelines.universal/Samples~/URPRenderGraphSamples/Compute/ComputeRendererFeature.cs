using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


// This RendererFeature shows how a compute shader can be used together with RenderGraph.

// What this example doesn't show is that it can run together with render passes. If the
// compute shader is using resources which are also used by render passes then a dependency
// between the passes are created as they would have done for two render passes.
public class ComputeRendererFeature : ScriptableRendererFeature
{
    // We will treat the compute pass as a normal Scriptable Render Pass.
    class ComputePass : ScriptableRenderPass
    {
        // Compute shader.
        ComputeShader cs;

        // Compute buffers:
        GraphicsBuffer inputBuffer;
        GraphicsBuffer outputBuffer;

        // Reflection of the data output. I use a preallocated list to avoid memory
        // allocations each frame.
        int[] outputData = new int[20];

        // Constructor is used to initialize the compute buffers.
        public ComputePass()
        {
            BufferDesc desc = new BufferDesc(20, sizeof(int));
            inputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 20, sizeof(int));
            var list = new List<int>();
            for (int i = 0; i < 20; i++)
            {
                list.Add(i);
            }
            inputBuffer.SetData(list);
            outputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 20, sizeof(int));
            // We don't need to initialize the output normaly with data but I read the
            // buffer from the start when each frame is starting to look at last frames result.
            outputBuffer.SetData(list);
        }

        // Setup function to transfer the compute shader from the renderer feature to
        // the render pass.
        public void Setup(ComputeShader cs)
        {
            this.cs = cs;
        }

        // PassData is used to pass data when recording to the execution of the pass.
        class PassData
        {
            // Compute shader.
            public ComputeShader cs;
            // Buffer handles for the compute buffers.
            public BufferHandle input;
            public BufferHandle output;
        }

        // Records a render graph render pass which blits the BlitData's active texture back to the camera's color attachment.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Last frame data should be done. Retrive the data if valid.
            outputBuffer.GetData(outputData);
            Debug.Log($"Output from compute shader: {string.Join(", ", outputData)}");

            // We need to import buffers when they are created outside of the render graph.
            BufferHandle inputHandle = renderGraph.ImportBuffer(inputBuffer);
            BufferHandle outputHandle = renderGraph.ImportBuffer(outputBuffer);

            // Starts the recording of the render graph pass given the name of the pass
            // and outputting the data used to pass data to the execution of the render function.
            // Notice that we use "AddComputePass" when we are working with compute.
            using (var builder = renderGraph.AddComputePass("ComputePass", out PassData passData))
            {
                // Set the pass data so the data can be transfered from the recording to the execution.
                passData.cs = cs;
                passData.input = inputHandle;
                passData.output = outputHandle;

                // UseBuffer is used to setup render graph dependencies together with read and write flags.
                builder.UseBuffer(passData.input);
                builder.UseBuffer(passData.output, AccessFlags.Write);
                // The execution function is also call SetRenderfunc for compute passes.
                builder.SetRenderFunc((PassData data, ComputeGraphContext cgContext) => ExecutePass(data, cgContext));
            }
        }

        // ExecutePass is the render function set in the render graph recordings.
        // This is good practice to avoid using variables outside of the lambda it is called from.
        // It is static to avoid using member variables which could cause unintended behaviour.
        static void ExecutePass(PassData data, ComputeGraphContext cgContext)
        {
            // Attaches the compute buffers.
            cgContext.cmd.SetComputeBufferParam(data.cs, data.cs.FindKernel("CSMain"), "inputData", data.input);
            cgContext.cmd.SetComputeBufferParam(data.cs, data.cs.FindKernel("CSMain"), "outputData", data.output);
            // Dispaches the compute shader with a given kernel as entrypoint.
            // The amount of thread groups determine how many groups to execute of the kernel.
            cgContext.cmd.DispatchCompute(data.cs, data.cs.FindKernel("CSMain"), 1, 1, 1);
        }
    }

    [SerializeField]
    ComputeShader computeShader;

    ComputePass m_ComputePass;

    /// <inheritdoc/>
    public override void Create()
    {
        // Initialize the compute pass.
        m_ComputePass = new ComputePass();
        // Sets the renderer feature to execute before rendering.
        m_ComputePass.renderPassEvent = RenderPassEvent.BeforeRendering;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Check if the system support compute shaders, if not make an early exit.
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogWarning("Device does not support compute shaders. The pass will be skipped.");
            return;
        }
        // Skip the render pass if the compute shader is null.
        if (computeShader == null)
        {
            Debug.LogWarning("The compute shader is null. The pass will be skipped.");
            return;
        }
        // Call Setup on the render pass and transfer the compute shader.
        m_ComputePass.Setup(computeShader);
        // Enqueue the compute pass.
        renderer.EnqueuePass(m_ComputePass);
    }
}


