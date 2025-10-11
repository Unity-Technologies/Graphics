using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class ScalingSetupPostProcessPass : ScriptableRenderPass, IDisposable
    {
        public const string k_TargetName = "_ScalingSetupTarget";

        Material m_Material;
        bool m_IsValid;

        // Settings
        public Tonemapping tonemapping { get; set; }

        public HDROutputUtils.Operation hdrOperations { get; set; }

        // Input
        public TextureHandle sourceTexture { get; set; }
        // Output
        public TextureHandle destinationTexture { get; set; }


        public ScalingSetupPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid()
        {
            return m_IsValid;
        }

        private class PostProcessingFinalSetupPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal Material material;
            internal UniversalCameraData cameraData;
            internal Tonemapping tonemapping;
            internal HDROutputUtils.Operation hdrOperations;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Assertions.Assert.IsTrue(sourceTexture.IsValid(), $"Source texture must be set for ScalingSetupPostProcessPass.");
            Assertions.Assert.IsTrue(destinationTexture.IsValid(), $"Destination texture must be set for ScalingSetupPostProcessPass.");

            // Scaled FXAA
            using (var builder = renderGraph.AddRasterRenderPass<PostProcessingFinalSetupPassData>("Postprocessing Final Setup Pass", out var passData, ProfilingSampler.Get(URPProfileId.RG_FinalSetup)))
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                passData.destinationTexture = destinationTexture;
                builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.Write);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);

                passData.material = m_Material;
                passData.cameraData = cameraData;
                passData.tonemapping = tonemapping;
                passData.hdrOperations = hdrOperations;

                builder.SetRenderFunc(static (PostProcessingFinalSetupPassData data, RasterGraphContext context) =>
                {
                    UniversalCameraData cameraData = data.cameraData;
                    Material material = data.material;
                    material.shaderKeywords = null;

                    bool hdrColorEncoding = data.hdrOperations.HasFlag(HDROutputUtils.Operation.ColorEncoding);
                    bool isFxaaEnabled = PostProcessUtils.IsFxaaEnabled(cameraData);
                    bool isFsrEnabled = PostProcessUtils.IsFsrEnabled(cameraData);

                    if (isFxaaEnabled)
                        material.EnableKeyword(ShaderKeywordStrings.Fxaa);

                    if (isFsrEnabled)
                        material.EnableKeyword( hdrColorEncoding ? ShaderKeywordStrings.Gamma20AndHDRInput : ShaderKeywordStrings.Gamma20);

                    if (hdrColorEncoding)
                        PostProcessUtils.SetupHDROutput(material, cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, data.tonemapping, data.hdrOperations, cameraData.rendersOverlayUI);

                    if (data.cameraData.isAlphaOutputEnabled)
                        CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, cameraData.isAlphaOutputEnabled);

                    material.SetVector(ShaderConstants._SourceSize, PostProcessUtils.CalcShaderSourceSize(data.sourceTexture));

                    const bool isFinalPass = false; // This is a pass just before final pass. Viewport must match intermediate target.
                    PostProcessUtils.ScaleViewportAndBlit(context, data.sourceTexture, data.destinationTexture, data.cameraData, data.material, isFinalPass);
                });
            }
        }


        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
            public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");
        }
    }
}
