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
            m_ComputeShader = computeShader;
            renderPassEvent = evt;
        }

        /// <summary>
        /// Disposes used resources.
        /// </summary>
        public void Dispose()
        {
        }

        public void Setup(RTHandle depthBuffer, RTHandle normalBuffer)
        {
            m_DepthTexture = depthBuffer;
            m_NormalTexture = normalBuffer;
        }

        public bool NeedsNormal()
        {
            return ProbeReferenceVolume.probeSamplingDebugData.update != ProbeSamplingDebugUpdate.Never;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (ProbeReferenceVolume.instance.isInitialized)
            {
#if UNITY_EDITOR
                ref CameraData cameraData = ref renderingData.cameraData;

                if (ProbeReferenceVolume.probeSamplingDebugData.camera != cameraData.camera)
                    return;
#endif

                if (ProbeReferenceVolume.probeSamplingDebugData.update != ProbeSamplingDebugUpdate.Never)
                {
                    var cmd = renderingData.commandBuffer;

                    int kernelHandle = m_ComputeShader.FindKernel("ComputePositionNormal");
                    cmd.SetComputeTextureParam(m_ComputeShader, kernelHandle, "_CameraDepthTexture", m_DepthTexture);
                    cmd.SetComputeTextureParam(m_ComputeShader, kernelHandle, "_NormalBufferTexture", m_NormalTexture);
                    cmd.SetComputeVectorParam(m_ComputeShader, "_positionSS", new Vector4(ProbeReferenceVolume.probeSamplingDebugData.coordinates.x, ProbeReferenceVolume.probeSamplingDebugData.coordinates.y, 0.0f, 0.0f));
                    cmd.SetComputeBufferParam(m_ComputeShader, kernelHandle, "_ResultBuffer", ProbeReferenceVolume.probeSamplingDebugData.positionNormalBuffer);
                    cmd.DispatchCompute(m_ComputeShader, kernelHandle, 1, 1, 1);

                    if (ProbeReferenceVolume.probeSamplingDebugData.update == ProbeSamplingDebugUpdate.Once)
                    {
                        ProbeReferenceVolume.probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Never;
                        ProbeReferenceVolume.probeSamplingDebugData.forceScreenCenterCoordinates = false;
                    }
                }
            }
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
            if (ProbeReferenceVolume.instance.isInitialized)
            {
#if UNITY_EDITOR
                ref CameraData cameraData = ref renderingData.cameraData;

                if (ProbeReferenceVolume.probeSamplingDebugData.camera != cameraData.camera)
                    return;
#endif

                if (ProbeReferenceVolume.probeSamplingDebugData.update != ProbeSamplingDebugUpdate.Never)
                {
                    WriteApvPositionNormalDebugBuffer(renderGraph, ProbeReferenceVolume.probeSamplingDebugData.positionNormalBuffer, ProbeReferenceVolume.probeSamplingDebugData.coordinates, depthPyramidBuffer, normalBuffer);

                    if (ProbeReferenceVolume.probeSamplingDebugData.update == ProbeSamplingDebugUpdate.Once)
                    {
                        ProbeReferenceVolume.probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Never;
                        ProbeReferenceVolume.probeSamplingDebugData.forceScreenCenterCoordinates = false;
                    }
                }
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

        // Compute worldspace position and normal at given screenspace clickCoordinates, and write it into given ResultBuffer.
        void WriteApvPositionNormalDebugBuffer(RenderGraph renderGraph, GraphicsBuffer resultBuffer, Vector2 clickCoordinates, TextureHandle depthBuffer, TextureHandle normalBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<WriteApvData>("Debug", out var passData, base.profilingSampler))
            {
                passData.resultBuffer = renderGraph.ImportBuffer(resultBuffer);
                passData.clickCoordinates = clickCoordinates;
                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.computeShader = m_ComputeShader;

                builder.SetRenderFunc(
                    (WriteApvData data, RenderGraphContext ctx) =>
                    {
                        int kernelHandle = data.computeShader.FindKernel("ComputePositionNormal");
                        ctx.cmd.SetComputeTextureParam(data.computeShader, kernelHandle, "_CameraDepthTexture", data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.computeShader, kernelHandle, "_NormalBufferTexture", data.normalBuffer);
                        ctx.cmd.SetComputeVectorParam(data.computeShader, "_positionSS", new Vector4(data.clickCoordinates.x, data.clickCoordinates.y, 0.0f, 0.0f));
                        ctx.cmd.SetComputeBufferParam(data.computeShader, kernelHandle, "_ResultBuffer", data.resultBuffer);
                        ctx.cmd.DispatchCompute(data.computeShader, kernelHandle, 1, 1, 1);
                    });
            }
        }
    }
}
