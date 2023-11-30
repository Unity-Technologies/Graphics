// Uncomment this line to activate the GPUInlineDebugDrawer
// Do not overload the RenderGraph with empty function is not needed.
// #define ENABLE_GPU_INLINE_DEBUG_DRAWER

using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(PackingRules.Exact, false)]
    internal struct GPUInlineDebugDrawerLine
    {
        public Vector4 start;
        public Vector4 end;
        public Color startColor;
        public Color endColor;
    };

#if UNITY_EDITOR
    /// <summary>
    /// This helper allow us to draw debug primitive from Shader 'Inline'.
    ///
    /// To use include the helper on the shader:
    /// #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/GPUInlineDebugDrawer.hlsl"
    ///
    /// And:
    ///
    ///     - Line World Space:
    ///         void GPUInlineDebugDrawer_AddLineWS(float4 start, float4 end, float3 startColor, float3 endColor);
    ///         void GPUInlineDebugDrawer_AddLineWS(float4 start, float4 end, float3 color = float3(1, 0, 0));
    ///         void GPUInlineDebugDrawer_AddLineWS(float3 start, float3 end, float3 color = float3(1, 0, 0));
    ///         void GPUInlineDebugDrawer_AddLineWS(float3 start, float3 end, float3 startColor, float3 endColor);
    ///     - Line Clip Space:
    ///         void GPUInlineDebugDrawer_AddLineCS(float4 start, float4 end, float3 startColor, float3 endColor);
    ///         void GPUInlineDebugDrawer_AddLineCS(float4 start, float4 end, float3 color = float3(1, 0, 0));
    ///         void GPUInlineDebugDrawer_AddLineCS(float3 start, float3 end, float3 color = float3(1, 0, 0));
    ///         void GPUInlineDebugDrawer_AddLineCS(float3 start, float3 end, float3 startColor, float3 endColor);
    ///     - Plot Ring Buffer:
    ///         void GPUInlineDebugDrawer_PlotRingBufferAddFloat(float x)
    ///         will draw a Plot of the Ring Buffer to debug the value x.
    ///         The Value is between 0.0f and 1.0f;
    /// </summary>
    static class GPUInlineDebugDrawer
    {
#if ENABLE_GPU_INLINE_DEBUG_DRAWER
        static BufferHandle lineWSBuffer;
        static BufferHandle lineCSBuffer;
        static GraphicsBuffer lineWSIndirectArgs;
        static GraphicsBuffer lineCSIndirectArgs;

        static GraphicsBuffer plotRingBuffer;
        static GraphicsBuffer plotRingBufferStart;
        static GraphicsBuffer plotRingBufferEnd;

        static BufferHandle plotRingBufferHandle;
        static BufferHandle plotRingBufferStartHandle;
        static BufferHandle plotRingBufferEndHandle;

        static Material m_LineMaterial;
        static int m_LineWSNoDepthTestPassID;
        static int m_LineCSNoDepthTestPassID;
        static int m_PlotRingBufferPassID;

        static uint[] m_IndirectLineArgsDefault = new uint[] {
            2, // vertices per instance
            0, // instance count [Will be overridden]
            0, // byte offset of first vertex
            0  // byte offset of first instance
        };
#endif

        /// <summary>
        /// Global constant used for GPUInlineDebugDrawer
        /// : If changed need to regenerate headers.
        /// </summary>
        [GenerateHLSL(PackingRules.Exact)]
        public enum GPUInlineDebugDrawerParams
        {
            /// <summary>Maximum number of line which can be draw.</summary>
            MaxLines = 4096,
            /// <summary>Maximum size of the ring buffer for the plot.</summary>
            MaxPlotRingBuffer = 256
        };

        /// <summary>
        /// Manually initialize resources needed to render the debug draw.
        /// </summary>
        public static void Initialize()
        {
#if ENABLE_GPU_INLINE_DEBUG_DRAWER
            if (!GraphicsSettings.TryGetRenderPipelineSettings<HDRenderPipelineEditorShaders>(out var defaultShaders))
            {
                Debug.LogWarning($"Unable to initialize {nameof(GPUInlineDebugDrawer)} due to missing {nameof(HDRenderPipelineEditorShaders)}");
                return;
            }

            m_LineMaterial = CoreUtils.CreateEngineMaterial(defaultShaders.gpuInlineDebugDrawerLine);
            m_LineMaterial.SetOverrideTag("RenderType", "Transparent");
            m_LineWSNoDepthTestPassID = m_LineMaterial.FindPass("LineWSNoDepthTest");
            m_LineCSNoDepthTestPassID = m_LineMaterial.FindPass("LineCSNoDepthTest");
            m_PlotRingBufferPassID = m_LineMaterial.FindPass("PlotRingBufferPass");
            lineWSIndirectArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 4, sizeof(int));
            lineCSIndirectArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 4, sizeof(int));

            plotRingBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, (int)GPUInlineDebugDrawerParams.MaxPlotRingBuffer, sizeof(float));
            plotRingBufferStart = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint));
            plotRingBufferEnd = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint));

            float[] defaultData0fs = new float[(int)GPUInlineDebugDrawerParams.MaxPlotRingBuffer];
            System.Array.Clear(defaultData0fs, 0, (int)GPUInlineDebugDrawerParams.MaxPlotRingBuffer);

            uint[] defaultData0u = new uint[1];
            defaultData0u[0] = 0u;

            plotRingBuffer.SetData(defaultData0fs);
            plotRingBufferStart.SetData(defaultData0u);
            plotRingBufferEnd.SetData(defaultData0u);
#endif
        }

        /// <summary>
        /// Manually release resources.
        /// </summary>
        public static void Dispose()
        {
#if ENABLE_GPU_INLINE_DEBUG_DRAWER
            void TryFreeBuffer(ref GraphicsBuffer resource)
            {
                if (resource != null)
                {
                    resource.Dispose();
                    resource = null;
                }
            }

            TryFreeBuffer(ref lineWSIndirectArgs);
            TryFreeBuffer(ref lineCSIndirectArgs);

            TryFreeBuffer(ref plotRingBuffer);
            TryFreeBuffer(ref plotRingBufferStart);
            TryFreeBuffer(ref plotRingBufferEnd);
#endif
        }

        class GPUInlineDebugDrawerData
        {
            public BufferHandle lineWSBuffer;
            public BufferHandle lineCSBuffer;
            public BufferHandle lineWSIndirectArgs;
            public BufferHandle lineCSIndirectArgs;
            public BufferHandle plotRingBuffer;
            public BufferHandle plotRingBufferStart;
            public BufferHandle plotRingBufferEnd;
            public Vector2 mousePosition;
            public Material lineMaterial;
            public int lineWSNoDepthTestPassID;
            public int lineCSNoDepthTestPassID;
            public int plotRingBufferPassID;
        }

        /// <summary>
        /// Bind resources used during the whole frame.
        /// </summary>
        /// <param name="cam">Camera used to retrive the size of the screen to use properly mouse position.</param>
        /// <param name="renderGraph">Current RenderGraph</param>
        static public void BindProducers(HDCamera cam, RenderGraph renderGraph)
        {
#if ENABLE_GPU_INLINE_DEBUG_DRAWER
            using (var builder = renderGraph.AddRenderPass<GPUInlineDebugDrawerData>("GPUInlineDebugDrawer_BindProducers", out var passData))
            {
                int maxLines = (int)GPUInlineDebugDrawerParams.MaxLines;

                lineWSBuffer = renderGraph.CreateBuffer(new BufferDesc(maxLines, Marshal.SizeOf(typeof(GPUInlineDebugDrawerLine)), GraphicsBuffer.Target.Append)
                { name = "GPUInlineDebugDrawerLineWS" });
                lineCSBuffer = renderGraph.CreateBuffer(new BufferDesc(maxLines, Marshal.SizeOf(typeof(GPUInlineDebugDrawerLine)), GraphicsBuffer.Target.Append)
                { name = "GPUInlineDebugDrawerLineCS" });

                passData.lineWSBuffer = builder.WriteBuffer(lineWSBuffer);
                passData.lineCSBuffer = builder.WriteBuffer(lineCSBuffer);

                plotRingBufferHandle = renderGraph.ImportBuffer(plotRingBuffer);
                plotRingBufferStartHandle = renderGraph.ImportBuffer(plotRingBufferStart);
                plotRingBufferEndHandle = renderGraph.ImportBuffer(plotRingBufferEnd);

                passData.plotRingBuffer = builder.WriteBuffer(plotRingBufferHandle);
                passData.plotRingBufferStart = builder.WriteBuffer(plotRingBufferStartHandle);
                passData.plotRingBufferEnd = builder.WriteBuffer(plotRingBufferEndHandle);

                passData.mousePosition = new Vector2(Mathf.Round(Event.current.mousePosition.x), Mathf.Round(cam.actualHeight - 1 - Event.current.mousePosition.y));

                builder.SetRenderFunc(
                    (GPUInlineDebugDrawerData data, Rendering.RenderGraphModule.RenderGraphContext ctx) =>
                    {
                        ((GraphicsBuffer)data.lineWSBuffer).SetCounterValue(0u);
                        ((GraphicsBuffer)data.lineCSBuffer).SetCounterValue(0u);

                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._GPUInlineDebugDrawerLinesWSProduce, data.lineWSBuffer);
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._GPUInlineDebugDrawerLinesCSProduce, data.lineCSBuffer);

                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._GPUInlineDebugDrawer_PlotRingBuffer, data.plotRingBuffer);
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._GPUInlineDebugDrawer_PlotRingBufferStart, data.plotRingBufferStart);
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._GPUInlineDebugDrawer_PlotRingBufferEnd, data.plotRingBufferEnd);

                        ctx.cmd.SetGlobalVector(HDShaderIDs._GPUInlineDebugDrawerMousePos, data.mousePosition);
                    });
            }
#endif
        }

        /// <summary>
        /// Do the proper draw, must be called at the end of the frame, after the PostProcess.
        /// </summary>
        /// <param name="renderGraph">Current RenderGraph</param>
        static public void Draw(RenderGraph renderGraph)
        {
#if ENABLE_GPU_INLINE_DEBUG_DRAWER
            using (var builder = renderGraph.AddRenderPass<GPUInlineDebugDrawerData>("GPUInlineDebugDrawer_Draws", out var passData))
            {
                passData.lineWSBuffer = builder.ReadBuffer(lineWSBuffer);
                passData.lineCSBuffer = builder.ReadBuffer(lineCSBuffer);
                lineWSIndirectArgs.SetData(m_IndirectLineArgsDefault);
                lineCSIndirectArgs.SetData(m_IndirectLineArgsDefault);
                passData.lineWSIndirectArgs = builder.WriteBuffer(renderGraph.ImportBuffer(lineWSIndirectArgs));
                passData.lineCSIndirectArgs = builder.WriteBuffer(renderGraph.ImportBuffer(lineCSIndirectArgs));

                passData.plotRingBuffer = builder.ReadBuffer(plotRingBufferHandle);
                passData.plotRingBufferStart = builder.ReadBuffer(plotRingBufferStartHandle);
                passData.plotRingBufferEnd = builder.ReadBuffer(plotRingBufferEndHandle);

                passData.lineMaterial = m_LineMaterial;
                passData.lineWSNoDepthTestPassID = m_LineWSNoDepthTestPassID;
                passData.lineCSNoDepthTestPassID = m_LineCSNoDepthTestPassID;
                passData.plotRingBufferPassID = m_PlotRingBufferPassID;

                builder.SetRenderFunc(
                    (GPUInlineDebugDrawerData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.CopyCounterValue(data.lineWSBuffer, data.lineWSIndirectArgs, sizeof(uint));
                        ctx.cmd.CopyCounterValue(data.lineCSBuffer, data.lineCSIndirectArgs, sizeof(uint));

                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._GPUInlineDebugDrawerLinesWSConsume, data.lineWSBuffer);
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._GPUInlineDebugDrawerLinesCSConsume, data.lineCSBuffer);

                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._GPUInlineDebugDrawer_PlotRingBufferRead, data.plotRingBuffer);
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._GPUInlineDebugDrawer_PlotRingBufferStartRead, data.plotRingBufferStart);
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._GPUInlineDebugDrawer_PlotRingBufferEndRead, data.plotRingBufferEnd);

                        // Draw World Space Lines
                        ctx.cmd.DrawProceduralIndirect(Matrix4x4.identity, data.lineMaterial, data.lineWSNoDepthTestPassID, MeshTopology.Lines, data.lineWSIndirectArgs);
                        // Draw Clip Space Lines
                        ctx.cmd.DrawProceduralIndirect(Matrix4x4.identity, data.lineMaterial, data.lineCSNoDepthTestPassID, MeshTopology.Lines, data.lineCSIndirectArgs);
                        // Draw Plot Ring Buffer
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.lineMaterial, data.plotRingBufferPassID, MeshTopology.LineStrip, (int)GPUInlineDebugDrawerParams.MaxPlotRingBuffer + 5);
                    });
            }
#endif
        }
    };
#endif
}
