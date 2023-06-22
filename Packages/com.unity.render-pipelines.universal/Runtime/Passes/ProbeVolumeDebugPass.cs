using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Uses a compute shader to capture the depth and normal of the pixel under the cursor.
    /// </summary>
    internal partial class ProbeVolumeDebugPass : ScriptableRenderPass
    {
        ComputeShader m_ComputeShader;
        RTHandle m_DepthTexture;
        RTHandle m_NormalTexture;

        /// <summary>
        /// Creates a new <c>ProbeVolumeDebugPass</c> instance.
        /// </summary>
        public ProbeVolumeDebugPass(RenderPassEvent evt, ComputeShader computeShader)
        {
            base.profilingSampler = new ProfilingSampler(nameof(ProbeVolumeDebugPass));
            renderPassEvent = evt;
            m_ComputeShader = computeShader;
        }

        public void Setup(RTHandle depthBuffer, RTHandle normalBuffer)
        {
            m_DepthTexture = depthBuffer;
            m_NormalTexture = normalBuffer;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!ProbeReferenceVolume.instance.isInitialized)
                return;

            ref CameraData cameraData = ref renderingData.cameraData;
            if (ProbeReferenceVolume.instance.GetProbeSamplingDebugResources(cameraData.camera, out var resultBuffer, out Vector2 coords))
            {
                OutputPositionAndNormal(renderingData.commandBuffer, m_ComputeShader, resultBuffer, coords, m_DepthTexture, m_NormalTexture);
            }
        }


        class WriteApvData
        {
            public ComputeShader computeShader;
            public BufferHandle resultBuffer;
            public Vector2 clickCoordinates;
            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
        }

        /// <summary>
        /// Render graph entry point
        /// </summary>
        /// <param name="renderGraph"></param>
        /// <param name="renderingData"></param>
        /// <param name="depthPyramidBuffer"></param>
        /// <param name="normalBuffer"></param>
        internal void Render(RenderGraph renderGraph, ref RenderingData renderingData, TextureHandle depthPyramidBuffer, TextureHandle normalBuffer)
        {
            if (!ProbeReferenceVolume.instance.isInitialized)
                return;

            ref CameraData cameraData = ref renderingData.cameraData;
            if (ProbeReferenceVolume.instance.GetProbeSamplingDebugResources(cameraData.camera, out var resultBuffer, out Vector2 coords))
            {
                using (var builder = renderGraph.AddRenderPass<WriteApvData>("Debug", out var passData, base.profilingSampler))
                {
                    passData.resultBuffer = renderGraph.ImportBuffer(resultBuffer);
                    passData.clickCoordinates = coords;
                    passData.depthBuffer = builder.ReadTexture(depthPyramidBuffer);
                    passData.normalBuffer = builder.ReadTexture(normalBuffer);
                    passData.computeShader = m_ComputeShader;

                    builder.SetRenderFunc((WriteApvData data, RenderGraphContext ctx) =>
                        {
                            OutputPositionAndNormal(ctx.cmd, data.computeShader, data.resultBuffer, data.clickCoordinates, data.depthBuffer, data.normalBuffer);
                        });
                }
            }
        }

        static void OutputPositionAndNormal(CommandBuffer cmd, ComputeShader compute, GraphicsBuffer resultBuffer, Vector2 clickCoordinates, RenderTargetIdentifier depthBuffer, RenderTargetIdentifier normalBuffer)
        {
            int kernel = compute.FindKernel("ComputePositionNormal");

            cmd.SetComputeTextureParam(compute, kernel, "_CameraDepthTexture", depthBuffer);
            cmd.SetComputeTextureParam(compute, kernel, "_NormalBufferTexture", normalBuffer);
            cmd.SetComputeVectorParam(compute, "_positionSS", new Vector4(clickCoordinates.x, clickCoordinates.y, 0.0f, 0.0f));
            cmd.SetComputeBufferParam(compute, kernel, "_ResultBuffer", resultBuffer);
            cmd.DispatchCompute(compute, kernel, 1, 1, 1);
        }
    }
}
