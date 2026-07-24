#if VOLUMETRIC_FOG

using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using SharedIDs = UnityEngine.Rendering.Universal.VolumetricFogRendererFeature.ShaderIDs;

namespace UnityEngine.Rendering.Universal
{
    // Optional spatial denoise: Enabled when the Fog override's denoising mode includes the Gaussian bit (Gaussian or
    // Both). Reads the lighting pass output and either filters in place (when the platform supports RGBA16F as a
    // load-store UAV) or writes to a separate output buffer that takes over as the new vbuffer for the apply pass.
    internal class VolumetricFilteringPass : ScriptableRenderPass
    {
        static class ShaderIDs
        {
            public static readonly int inputTex3D = Shader.PropertyToID("inputTex3D");
            public static readonly int outputTex3D = Shader.PropertyToID("outputTex3D");
        }

        ComputeShader m_Shader;
        int m_Kernel;

        static readonly bool s_NeedsSeparateOutput = !SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.LoadStore);

        class PassData
        {
            public ComputeShader cs;
            public int kernel;
            public LocalKeyword needSeparateOutputKeyword;
            public bool needSeparateOutput;

            public int vbufferW, vbufferH, sliceCount;
            public Vector4 vbufferViewportSize;

            public TextureHandle inputTex3D;   // read or read-write
            public TextureHandle outputTex3D;  // written only when needSeparateOutput
        }

        public VolumetricFilteringPass()
        {
            GraphicsSettings.TryGetRenderPipelineSettings<VolumetricFogResources>(out var resources);
            m_Shader = resources.volumetricLightingFilteringCS;
            if (m_Shader != null)
                m_Kernel = m_Shader.FindKernel("FilterVolumetricLighting");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Shader == null)
                return;

            var fogData = frameData.GetOrCreate<VolumetricFogFrameData>();

            if (!fogData.enableGaussian || !fogData.vbuffer.IsValid())
                return;

            var cs = m_Shader;

            ref readonly var vBufferParams = ref fogData.vBufferParams;

            using (var builder = renderGraph.AddUnsafePass<PassData>("Volumetric Lighting Filter", out var passData))
            {
                passData.cs = cs;
                passData.kernel = m_Kernel;
                passData.needSeparateOutputKeyword = new LocalKeyword(cs, "NEED_SEPARATE_OUTPUT");
                passData.needSeparateOutput = s_NeedsSeparateOutput;

                // Uniforms
                passData.vbufferW = vBufferParams.vbufferW;
                passData.vbufferH = vBufferParams.vbufferH;
                passData.sliceCount = vBufferParams.sliceCount;
                passData.vbufferViewportSize = vBufferParams.viewportSize;

                // Input / output buffers
                passData.inputTex3D = fogData.vbuffer;
                if (s_NeedsSeparateOutput)
                {
                    builder.UseTexture(passData.inputTex3D, AccessFlags.Read);

                    passData.outputTex3D = renderGraph.CreateTexture(new TextureDesc(vBufferParams.vbufferW, vBufferParams.vbufferH)
                    {
                        format = GraphicsFormat.R16G16B16A16_SFloat,
                        dimension = TextureDimension.Tex3D,
                        slices = vBufferParams.sliceCount,
                        enableRandomWrite = true,
                        name = "VBuffer Lighting Filtered"
                    });
                    builder.UseTexture(passData.outputTex3D, AccessFlags.Write);
                }
                else
                {
                    // Filter in place. The kernel reads each voxel's neighbours into LDS once per group before writing back.
                    builder.UseTexture(passData.inputTex3D, AccessFlags.ReadWrite);
                }

                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    var cs = data.cs;
                    int kernel = data.kernel;

                    cmd.SetKeyword(cs, data.needSeparateOutputKeyword, data.needSeparateOutput);

                    cmd.SetComputeVectorParam(cs, SharedIDs._VBufferSize, data.vbufferViewportSize);
                    cmd.SetComputeTextureParam(cs, kernel, ShaderIDs.inputTex3D, data.inputTex3D);
                    if (data.needSeparateOutput)
                        cmd.SetComputeTextureParam(cs, kernel, ShaderIDs.outputTex3D, data.outputTex3D);

                    int dispatchX = CoreUtils.DivRoundUp(data.vbufferW, 8);
                    int dispatchY = CoreUtils.DivRoundUp(data.vbufferH, 8);
                    cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, data.sliceCount);
                });

                // If we wrote to a separate buffer, it becomes the v-buffer the apply pass reads.
                if (s_NeedsSeparateOutput)
                    fogData.vbuffer = passData.outputTex3D;
            }
        }
    }
}

#endif
