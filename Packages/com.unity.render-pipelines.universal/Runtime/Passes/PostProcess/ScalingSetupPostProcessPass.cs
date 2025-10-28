using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class ScalingSetupPostProcessPass : PostProcessPass
    {
        public const string k_TargetName = "_ScalingSetupTarget";

        Material m_Material;
        bool m_IsValid;

        HDROutputUtils.Operation m_HdrOperations;

        public ScalingSetupPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = new ProfilingSampler("Setup Final Post Processing");

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_IsValid = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Setup(HDROutputUtils.Operation hdrOperations)
        {
            m_HdrOperations = hdrOperations;
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
            if (!m_IsValid)
                return;

            var tonemapping = volumeStack.GetComponent<Tonemapping>();

            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            var sourceTexture = resourceData.cameraColor;

            var scalingSetupDesc = renderGraph.GetTextureDesc(sourceTexture);
            bool requireHDROutput = PostProcessUtils.RequireHDROutput(cameraData);
            if (!requireHDROutput)
            {
                // Select a UNORM format since we've already performed tonemapping. (Values are in 0-1 range)
                // This improves precision and is required if we want to avoid excessive banding when FSR is in use.
                scalingSetupDesc.format = UniversalRenderPipeline.MakeUnormRenderTextureGraphicsFormat();
            }

            var destinationTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, scalingSetupDesc, k_TargetName, true, FilterMode.Point);

            // Scaled FXAA
            using (var builder = renderGraph.AddRasterRenderPass<PostProcessingFinalSetupPassData>(passName, out var passData, profilingSampler))
            {
                passData.destinationTexture = destinationTexture;
                builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.Write);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);

                passData.material = m_Material;
                passData.cameraData = cameraData;
                passData.tonemapping = tonemapping;
                passData.hdrOperations = m_HdrOperations;

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

            resourceData.cameraColor = destinationTexture;
        }


        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
            public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");
        }
    }
}
