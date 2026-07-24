#if VOLUMETRIC_FOG

using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using SharedIDs = UnityEngine.Rendering.Universal.VolumetricFogRendererFeature.ShaderIDs;

namespace UnityEngine.Rendering.Universal
{
    internal class GenerateMaxZMaskPass : ScriptableRenderPass
    {
        static class ShaderIDs
        {
            public static readonly int _InputTexture = Shader.PropertyToID("_InputTexture");
            public static readonly int _OutputTexture = Shader.PropertyToID("_OutputTexture");
        }

        ComputeShader m_Shader;
        int m_MaxZKernel;
        int m_FinalMaskKernel;
        int m_DilateKernel;

        class GenerateMaxZPassData
        {
            public ComputeShader cs;
            public LocalKeyword disableTexture2DArrayKeyword;
            public bool depthIsArray;
            public int maxZKernel;
            public int finalMaskKernel;
            public int dilateKernel;

            public int intermediateMaskW;
            public int intermediateMaskH;

            public int finalMaskW;
            public int finalMaskH;

            public int viewCount;

            public Vector4 zBufferParams;

            public TextureHandle depthTexture;
            public TextureHandle maxZ8xBuffer;
            public TextureHandle maxZBuffer;
            public TextureHandle dilatedMaxZBuffer;
        }

        public GenerateMaxZMaskPass()
        {
            GraphicsSettings.TryGetRenderPipelineSettings<VolumetricFogResources>(out var resources);
            m_Shader = resources.generateMaxZCS;
            m_MaxZKernel = m_Shader.FindKernel("ComputeMaxZ");
            m_FinalMaskKernel = m_Shader.FindKernel("ComputeFinalMask");
            m_DilateKernel = m_Shader.FindKernel("DilateMask");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var fogData = frameData.GetOrCreate<VolumetricFogFrameData>();

            if (!resourceData.cameraDepthTexture.IsValid())
                return;

            var cs = m_Shader;

            int screenWidth = cameraData.cameraTargetDescriptor.width;
            int screenHeight = cameraData.cameraTargetDescriptor.height;

            int intermediateMaskW = CoreUtils.DivRoundUp(screenWidth, 8);
            int intermediateMaskH = CoreUtils.DivRoundUp(screenHeight, 8);

            int finalMaskW = CoreUtils.DivRoundUp(intermediateMaskW, 2);
            int finalMaskH = CoreUtils.DivRoundUp(intermediateMaskH, 2);

            bool depthIsArray = cameraData.cameraTargetDescriptor.dimension == TextureDimension.Tex2DArray;
            int slices = depthIsArray ? Mathf.Max(cameraData.cameraTargetDescriptor.volumeDepth, 1) : 1;
            var texDimension = depthIsArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D;

            using (var builder = renderGraph.AddUnsafePass<GenerateMaxZPassData>("Generate Max Z Mask", out var passData))
            {
                passData.cs = cs;
                passData.disableTexture2DArrayKeyword = new LocalKeyword(cs, ShaderKeywordStrings.DisableTexture2DXArray);
                passData.depthIsArray = depthIsArray;
                passData.maxZKernel = m_MaxZKernel;
                passData.finalMaskKernel = m_FinalMaskKernel;
                passData.dilateKernel = m_DilateKernel;

                passData.intermediateMaskW = intermediateMaskW;
                passData.intermediateMaskH = intermediateMaskH;
                passData.finalMaskW = finalMaskW;
                passData.finalMaskH = finalMaskH;
                passData.viewCount = slices;

                var camera = cameraData.camera;
                float near = camera.nearClipPlane;
                float far = camera.farClipPlane;
                float invNear = Mathf.Approximately(near, 0.0f) ? 0.0f : 1.0f / near;
                float invFar = Mathf.Approximately(far, 0.0f) ? 0.0f : 1.0f / far;
                float zc0 = 1.0f - far * invNear;
                float zc1 = far * invNear;
                var zBufferParams = new Vector4(zc0, zc1, zc0 * invFar, zc1 * invFar);
                if (SystemInfo.usesReversedZBuffer)
                {
                    zBufferParams.y += zBufferParams.x;
                    zBufferParams.x = -zBufferParams.x;
                    zBufferParams.w += zBufferParams.z;
                    zBufferParams.z = -zBufferParams.z;
                }
                passData.zBufferParams = zBufferParams;

                // Input texture
                passData.depthTexture = resourceData.cameraDepthTexture;
                builder.UseTexture(passData.depthTexture, AccessFlags.Read);

                // Output textures
                passData.maxZ8xBuffer = builder.CreateTransientTexture(
                    new TextureDesc(intermediateMaskW, intermediateMaskH)
                    {
                        format = GraphicsFormat.R32_SFloat,
                        enableRandomWrite = true,
                        slices = slices,
                        dimension = texDimension,
                        name = "MaxZ mask 8x"
                    });

                passData.maxZBuffer = builder.CreateTransientTexture(
                    new TextureDesc(finalMaskW, finalMaskH)
                    {
                        format = GraphicsFormat.R32_SFloat,
                        enableRandomWrite = true,
                        slices = slices,
                        dimension = texDimension,
                        name = "MaxZ mask"
                    });

                passData.dilatedMaxZBuffer = renderGraph.CreateTexture(
                    new TextureDesc(finalMaskW, finalMaskH)
                    {
                        format = GraphicsFormat.R32_SFloat,
                        enableRandomWrite = true,
                        slices = slices,
                        dimension = texDimension,
                        name = "Dilated MaxZ mask"
                    });
                builder.UseTexture(passData.dilatedMaxZBuffer, AccessFlags.ReadWrite);

                builder.SetRenderFunc(static (GenerateMaxZPassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    var cs = data.cs;

                    cmd.SetKeyword(cs, data.disableTexture2DArrayKeyword, !data.depthIsArray);

                    cmd.SetComputeVectorParam(cs, SharedIDs._ZBufferParams, data.zBufferParams);

                    // --- Kernel 1: Downsample 8x8 with max operator ---
                    int kernel = data.maxZKernel;
                    cmd.SetComputeTextureParam(cs, kernel, SharedIDs._CameraDepthTexture, data.depthTexture);
                    cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._OutputTexture, data.maxZ8xBuffer);
                    cmd.DispatchCompute(cs, kernel, data.intermediateMaskW, data.intermediateMaskH, data.viewCount);

                    // --- Kernel 2: Downsample 2x2 to final mask ---
                    kernel = data.finalMaskKernel;
                    cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._InputTexture, data.maxZ8xBuffer);
                    cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._OutputTexture, data.maxZBuffer);

                    int dispatchX = CoreUtils.DivRoundUp(data.finalMaskW, 8);
                    int dispatchY = CoreUtils.DivRoundUp(data.finalMaskH, 8);
                    cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, data.viewCount);

                    // --- Kernel 3: Dilate ---
                    kernel = data.dilateKernel;
                    cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._InputTexture, data.maxZBuffer);
                    cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._OutputTexture, data.dilatedMaxZBuffer);

                    cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, data.viewCount);
                });

                fogData.maxZMaskTexture = passData.dilatedMaxZBuffer;
            }
        }
    }
}

#endif
