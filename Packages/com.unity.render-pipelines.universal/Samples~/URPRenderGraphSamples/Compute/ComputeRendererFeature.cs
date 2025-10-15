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
        ComputeShader m_ComputeShader;

        // Compute buffers.
        BufferHandle m_InputBufferHandle;
        BufferHandle m_OutputBufferHandle;

        // Input data for the compute shader.
        private List<int> inputData = new List<int>();

        // Constructor is used to initialize the input data.
        public ComputePass()
        {
            for (int i = 0; i < 20; i++)
            {
                inputData.Add(i);
            }
        }

        // Setup function to transfer the compute shader from the renderer feature to
        // the render pass.
        public void Setup(ComputeShader cs)
        {
            m_ComputeShader = cs;
        }

        // PassData is used to pass data when recording to the execution of the pass.
        class PassData
        {
            // Compute shader.
            public ComputeShader cs;
            
            // Buffer handles for the compute buffers.
            public BufferHandle input;
            public BufferHandle output;
            public List<int> bufferData;
        }

        // ReadbackPassData is used to read data asynchronously from the specified bufferHandle.
        class ReadbackPassData
        {
            public BufferHandle bufferHandle;
        }

        // Records a render graph render pass which blits the BlitData's active texture back to the camera's color attachment.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Create buffers
            var bufferDesc = new BufferDesc()
            {
                name = "InputBuffer",
                count = 20,
                stride = sizeof(int),
                target = GraphicsBuffer.Target.Structured
            };
            m_InputBufferHandle = renderGraph.CreateBuffer(bufferDesc);
            
            bufferDesc.name = "OutputBuffer";
            m_OutputBufferHandle = renderGraph.CreateBuffer(bufferDesc);

            // Starts the recording of the render graph pass given the name of the pass
            // and outputting the data used to pass data to the execution of the render function.
            // Notice that we use "AddComputePass" when we are working with compute.
            using (var builder = renderGraph.AddComputePass("ComputePass", out PassData passData))
            {
                // Set the pass data so the data can be transferred from the recording to the execution.
                passData.cs = m_ComputeShader;
                passData.input = m_InputBufferHandle;
                passData.output = m_OutputBufferHandle;
                passData.bufferData = inputData;
                
                // Log input data in the console to show before and after
                Debug.Log($"Input Data: {string.Join(",", inputData)}");
                
                // UseBuffer is used to set up render graph dependencies together with read and write flags.
                builder.UseBuffer(passData.input, AccessFlags.Read);
                builder.UseBuffer(passData.output, AccessFlags.Write);
                
                // The execution function is also called SetRenderFunc for compute passes.
                builder.SetRenderFunc(static (PassData data, ComputeGraphContext cgContext) => ExecutePass(data, cgContext));
            }

            // Because our BufferHandles are managed by the render graph, we don't have access to the data when the
            // RenderGraph is done executing. We need to add a pass to read from the output buffer if we want to
            // use the output data from the compute shader.
            using (var builder = renderGraph.AddUnsafePass("ReadbackPass", out ReadbackPassData passData))
            {
                builder.AllowPassCulling(false);

                // Which buffer to read from
                passData.bufferHandle = m_OutputBufferHandle;
                builder.UseBuffer(passData.bufferHandle, AccessFlags.Read);
                builder.SetRenderFunc(static (ReadbackPassData data, UnsafeGraphContext ctx) =>
                {
                    ctx.cmd.RequestAsyncReadback(data.bufferHandle, (AsyncGPUReadbackRequest request) =>
                    {
                        var result = request.GetData<int>();
                        Debug.Log($"Output Data: {string.Join(",", result)}");
                    });
                });
            }
        }

        // ExecutePass is the render function set in the render graph recordings.
        // This is good practice to avoid using variables outside of the lambda it is called from.
        // It is static to avoid using member variables which could cause unintended behaviour.
        static void ExecutePass(PassData data, ComputeGraphContext cgContext)
        {
            // Attaches the compute buffers.
            cgContext.cmd.SetBufferData(data.input, data.bufferData);
            cgContext.cmd.SetComputeBufferParam(data.cs, data.cs.FindKernel("CSMain"), "inputData", data.input);
            cgContext.cmd.SetComputeBufferParam(data.cs, data.cs.FindKernel("CSMain"), "outputData", data.output);
            // Dispatches the compute shader with a given kernel as entrypoint.
            // The amount of thread groups determines how many groups to execute of the kernel.
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
        // Check if the system supports compute shaders, if not make an early exit.
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


