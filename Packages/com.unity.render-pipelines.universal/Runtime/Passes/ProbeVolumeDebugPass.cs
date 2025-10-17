using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Uses a compute shader to capture the depth and normal of the pixel under the cursor.
    /// </summary>
    internal class ProbeVolumeDebugPass : ScriptableRenderPass
    {
        ComputeShader m_ComputeShader;

        /// <summary>
        /// Creates a new <c>ProbeVolumeDebugPass</c> instance.
        /// </summary>
        public ProbeVolumeDebugPass(RenderPassEvent evt, ComputeShader computeShader)
        {
            profilingSampler = new ProfilingSampler("Dispatch APV Debug");
            renderPassEvent = evt;
            m_ComputeShader = computeShader;
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
        /// <param name="frameData"></param>
        /// <param name="depthPyramidBuffer"></param>
        /// <param name="normalBuffer"></param>
        internal void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle depthPyramidBuffer, TextureHandle normalBuffer)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (!ProbeReferenceVolume.instance.isInitialized)
                return;

            if (ProbeReferenceVolume.instance.GetProbeSamplingDebugResources(cameraData.camera, out var resultBuffer, out Vector2 coords))
            {
                using (var builder = renderGraph.AddComputePass<WriteApvData>(passName, out var passData, profilingSampler))
                {
                    passData.clickCoordinates = coords;
                    passData.computeShader = m_ComputeShader;

                    passData.resultBuffer = renderGraph.ImportBuffer(resultBuffer);
                    passData.depthBuffer = depthPyramidBuffer;
                    passData.normalBuffer = normalBuffer;

                    builder.UseBuffer(passData.resultBuffer, AccessFlags.Write);
                    builder.UseTexture(passData.depthBuffer, AccessFlags.Read);
                    builder.UseTexture(passData.normalBuffer, AccessFlags.Read);

                    builder.SetRenderFunc((WriteApvData data, ComputeGraphContext ctx) =>
                    {
                        int kernel = data.computeShader.FindKernel("ComputePositionNormal");

                        ctx.cmd.SetComputeTextureParam(data.computeShader, kernel, "_CameraDepthTexture", data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.computeShader, kernel, "_NormalBufferTexture", data.normalBuffer);
                        ctx.cmd.SetComputeVectorParam(data.computeShader, "_positionSS", new Vector4(data.clickCoordinates.x, data.clickCoordinates.y, 0.0f, 0.0f));
                        ctx.cmd.SetComputeBufferParam(data.computeShader, kernel, "_ResultBuffer", data.resultBuffer);
                        ctx.cmd.DispatchCompute(data.computeShader, kernel, 1, 1, 1);
                    });
                }
            }
        }
    }
}
