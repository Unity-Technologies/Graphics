using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class FinalPostProcessPass : PostProcessPass
    {
        Material m_Material;
        Texture2D[] m_FilmGrainTextures;
        bool m_IsValid;

        public enum FilteringOperation
        {
            Linear,
            Point,
            TaaSharpening,
            FsrSharpening
        }

        Texture m_DitherTexture;
        FilteringOperation m_FilteringOperation;
        HDROutputUtils.Operation m_HdrOperations;
        bool m_ApplySrgbEncoding;
        bool m_ApplyFxaa;   // NOTE: This is used to communicate if FXAA is already done in the previous pass.
        bool m_RenderOverlayUI;

        public FinalPostProcessPass(Shader shader, Texture2D[] filmGrainTextures)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = new ProfilingSampler("Blit Final Post Processing");

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
            m_FilmGrainTextures = filmGrainTextures;
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_IsValid = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Setup(Texture ditherTexture, FilteringOperation filteringOperation, HDROutputUtils.Operation hdrOperations, bool applySrgbEncoding, bool applyFxaa, bool renderOverlayUI)
        {
            m_DitherTexture = ditherTexture;
            m_FilteringOperation = filteringOperation;
            m_HdrOperations = hdrOperations;
            m_ApplySrgbEncoding = applySrgbEncoding;
            m_ApplyFxaa = applyFxaa;
            m_RenderOverlayUI = renderOverlayUI;
        }

        private class PostProcessingFinalBlitPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal Material material;
            internal UniversalCameraData cameraData;

            internal Tonemapping tonemapping;
            internal FilteringOperation filteringOperation;
            internal HDROutputUtils.Operation hdrOperations;

            internal UberPostProcessPass.FilmGrainParams filmGrain;
            internal UberPostProcessPass.DitheringParams dithering;

            internal bool applySrgbEncoding;
            internal bool applyFxaa;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!m_IsValid)
                return;

            var tonemapping = volumeStack.GetComponent<Tonemapping>();
            var filmGrain = volumeStack.GetComponent<FilmGrain>();

            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            var sourceTexture = resourceData.cameraColor;
            var destinationTexture = resourceData.backBufferColor; //By definition this pass blits to the backbuffer

            // Final blit pass
            using (var builder = renderGraph.AddRasterRenderPass<PostProcessingFinalBlitPassData>(passName, out var passData, profilingSampler))
            {
                // FSR and RCAS use global state constants.
                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = destinationTexture;
                builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.Write);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);

                // NOTE: Global texture. 'overlayUITexture' is bound by the DrawOffscreenUIPass pass if needed. Not in FinalPostProcessPass.
                if (m_RenderOverlayUI)
                    builder.UseTexture(resourceData.overlayUITexture, AccessFlags.Read);

                passData.material = m_Material;
                passData.cameraData = cameraData;

                passData.tonemapping = tonemapping;
                passData.filteringOperation = m_FilteringOperation;
                passData.hdrOperations = m_HdrOperations;

                passData.filmGrain.Setup(filmGrain, m_FilmGrainTextures, cameraData.pixelWidth, cameraData.pixelHeight);
                passData.dithering.Setup(m_DitherTexture, cameraData.pixelWidth, cameraData.pixelHeight);

                passData.applySrgbEncoding = m_ApplySrgbEncoding;
                passData.applyFxaa = m_ApplyFxaa;

#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    // This is a screen-space pass, make sure foveated rendering is disabled for non-uniform renders
                    bool passSupportsFoveation = !Experimental.Rendering.XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster);
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);

                    // Apply MultiviewRenderRegionsCompatible flag only to the peripheral view in Quad Views
                    if (cameraData.xr.multipassId == 0)
                    {
                        builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                    }
                }
#endif

                builder.SetRenderFunc(static (PostProcessingFinalBlitPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var material = data.material;
                    var cameraData = data.cameraData;
                    var filteringOperation = data.filteringOperation;
                    var requireHDROutput = PostProcessUtils.RequireHDROutput(cameraData);
                    var isAlphaOutputEnabled = cameraData.isAlphaOutputEnabled;
                    var applyFxaa = data.applyFxaa;
                    var hdrColorEncoding = data.hdrOperations.HasFlag(HDROutputUtils.Operation.ColorEncoding);
                    var applySrgbEncoding = data.applySrgbEncoding;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Clear shader keywords state
                    material.shaderKeywords = null;

                    switch (filteringOperation)
                    {

                        case FilteringOperation.Point:
                            material.EnableKeyword(ShaderKeywordStrings.PointSampling);
                        break;
                        case FilteringOperation.TaaSharpening:
                            // Reuse RCAS as a standalone sharpening filter for TAA.
                            // If FSR is enabled then it overrides the sharpening/TAA setting and we skip it.
                            material.EnableKeyword(ShaderKeywordStrings.Rcas);
                            FSRUtils.SetRcasConstantsLinear(cmd, data.cameraData.taaSettings.contrastAdaptiveSharpening); // TODO: Global state constants.
                        break;
                        case FilteringOperation.FsrSharpening:
                            // RCAS
                            // Use the override value if it's available, otherwise use the default.
                            float sharpness = data.cameraData.fsrOverrideSharpness ? data.cameraData.fsrSharpness : FSRUtils.kDefaultSharpnessLinear;

                            // Set up the parameters for the RCAS pass unless the sharpness value indicates that it wont have any effect.
                            if (data.cameraData.fsrSharpness > 0.0f)
                            {
                                // RCAS is performed during the final post blit, but we set up the parameters here for better logical grouping.
                                material.EnableKeyword(requireHDROutput ? ShaderKeywordStrings.EasuRcasAndHDRInput : ShaderKeywordStrings.Rcas);
                                FSRUtils.SetRcasConstantsLinear(cmd, sharpness);    // TODO: Global state constants.
                            }
                        break;
                        case FilteringOperation.Linear: goto default;
                        default:
                        break;
                    }

                    if (isAlphaOutputEnabled)
                        CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, isAlphaOutputEnabled);

                    if(data.filmGrain.IsActive())
                        data.filmGrain.Apply(material);

                    if(data.dithering.IsActive())
                        data.dithering.Apply(material);

                    // FXAA could have been already applied by a previous pass.
                    if (applyFxaa)
                        material.EnableKeyword(ShaderKeywordStrings.Fxaa);

                    if (applySrgbEncoding)
                        material.EnableKeyword(ShaderKeywordStrings.LinearToSRGBConversion);

                    if(hdrColorEncoding)
                        PostProcessUtils.SetupHDROutput(material, cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, data.tonemapping, data.hdrOperations, cameraData.rendersOverlayUI);

                    material.SetVector(ShaderConstants._SourceSize, PostProcessUtils.CalcShaderSourceSize(sourceTextureHdl));

                    Vector4 scaleBias = RenderingUtils.GetFinalBlitScaleBias(context, data.sourceTexture, data.destinationTexture);
                    cmd.SetViewport(data.cameraData.pixelRect);
#if ENABLE_VR && ENABLE_XR_MODULE
                    if (data.cameraData.xr.enabled && data.cameraData.xr.hasValidVisibleMesh)
                    {
                        MaterialPropertyBlock xrPropertyBlock = XRSystemUniversal.GetMaterialPropertyBlock();
                        xrPropertyBlock.SetVector(ShaderConstants._BlitScaleBias, scaleBias);
                        xrPropertyBlock.SetTexture(ShaderConstants._BlitTexture, sourceTextureHdl);

                        data.cameraData.xr.RenderVisibleMeshCustomMaterial(cmd, data.cameraData.xr.occlusionMeshScale, material, xrPropertyBlock, 1, context.GetTextureUVOrigin(in data.sourceTexture) == context.GetTextureUVOrigin(in data.destinationTexture));
                    }
                    else
#endif
                        Blitter.BlitTexture(cmd, sourceTextureHdl, scaleBias, material, 0);
                });
            }

            resourceData.SwitchActiveTexturesToBackbuffer();
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
            public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");

#if ENABLE_VR && ENABLE_XR_MODULE
            public static readonly int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
            public static readonly int _BlitTexture = Shader.PropertyToID("_BlitTexture");
#endif
        }
    }
}
