using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class FinalPostProcessPass : ScriptableRenderPass, IDisposable
    {
        Material m_Material;
        bool m_IsValid;

        Texture2D[] m_FilmGrainTextures;

        public enum SamplingOperation
        {
            Linear,
            Point,
            TaaSharpening,
            FsrSharpening
        }

        // Settings
        public Tonemapping tonemapping { get; set; }
        public FilmGrain filmGrain { get; set; }

        public SamplingOperation samplingOperation { get; set; }
        public HDROutputUtils.Operation hdrOperations { get; set; }
        public bool applySrgbEncoding { get; set; }
        //NOTE: This is used to communicate if FXAA is already done in the previous pass.
        public bool applyFxaa { get; set; }
        public bool resolveToDebugScreen { get; set; }  // TODO: Needed only for the y-flip logic, we should pass the y-flip flag instead.

        // Input
        public TextureHandle sourceTexture { get; set; }
        public TextureHandle overlayUITexture { get; set; }

        public Texture ditherTexture { get; set; }

        // Output
        public TextureHandle destinationTexture { get; set; }

        public FinalPostProcessPass(Shader shader, Texture2D[] filmGrainTextures)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;

            m_FilmGrainTextures = filmGrainTextures;
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

        private class PostProcessingFinalBlitPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal Material material;
            internal UniversalCameraData cameraData;

            internal Tonemapping tonemapping;
            internal SamplingOperation samplingOperation;
            internal HDROutputUtils.Operation hdrOperations;

            internal UberPostProcessPass.FilmGrainParams filmGrain;
            internal UberPostProcessPass.DitheringParams dithering;

            internal bool applySrgbEncoding;
            internal bool applyFxaa;
            internal bool resolveToDebugScreen;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Assertions.Assert.IsTrue(sourceTexture.IsValid(), $"Source texture must be set for FinalPostProcessPass.");
            Assertions.Assert.IsTrue(destinationTexture.IsValid(), $"Destination texture must be set for FinalPostProcessPass.");

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Final blit pass
            using (var builder = renderGraph.AddRasterRenderPass<PostProcessingFinalBlitPassData>("Postprocessing Final Blit Pass", out var passData, ProfilingSampler.Get(URPProfileId.RG_FinalBlit)))
            {
                // FSR and RCAS use global state constants.
                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = destinationTexture;
                builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.Write);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);

                if (overlayUITexture.IsValid())
                    builder.UseTexture(overlayUITexture, AccessFlags.Read);

                passData.material = m_Material;
                passData.cameraData = cameraData;

                passData.tonemapping = tonemapping;
                passData.samplingOperation = samplingOperation;
                passData.hdrOperations = hdrOperations;

                passData.filmGrain.Setup(filmGrain, m_FilmGrainTextures, cameraData.pixelWidth, cameraData.pixelHeight);
                passData.dithering.Setup(ditherTexture, cameraData.pixelWidth, cameraData.pixelHeight);

                passData.applySrgbEncoding = applySrgbEncoding;
                passData.applyFxaa = applyFxaa;
                passData.resolveToDebugScreen = resolveToDebugScreen;

#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    // This is a screen-space pass, make sure foveated rendering is disabled for non-uniform renders
                    bool passSupportsFoveation = !Experimental.Rendering.XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster);
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                }
#endif

                builder.SetRenderFunc(static (PostProcessingFinalBlitPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var material = data.material;
                    var cameraData = data.cameraData;
                    var samplingOperation = data.samplingOperation;
                    var requireHDROutput = PostProcessUtils.RequireHDROutput(cameraData);
                    var isAlphaOutputEnabled = cameraData.isAlphaOutputEnabled;
                    var applyFxaa = data.applyFxaa;
                    var resolveToDebugScreen = data.resolveToDebugScreen;
                    var hdrColorEncoding = data.hdrOperations.HasFlag(HDROutputUtils.Operation.ColorEncoding);
                    var applySrgbEncoding = data.applySrgbEncoding;
                    RTHandle sourceTextureHdl = data.sourceTexture;
                    RTHandle destinationTextureHdl = data.destinationTexture;

                    // Clear shader keywords state
                    material.shaderKeywords = null;

                    switch (samplingOperation)
                    {

                        case SamplingOperation.Point:
                            material.EnableKeyword(ShaderKeywordStrings.PointSampling);
                        break;
                        case SamplingOperation.TaaSharpening:
                            // Reuse RCAS as a standalone sharpening filter for TAA.
                            // If FSR is enabled then it overrides the sharpening/TAA setting and we skip it.
                            material.EnableKeyword(ShaderKeywordStrings.Rcas);
                            FSRUtils.SetRcasConstantsLinear(cmd, data.cameraData.taaSettings.contrastAdaptiveSharpening); // TODO: Global state constants.
                        break;
                        case SamplingOperation.FsrSharpening:
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
                        case SamplingOperation.Linear: goto default;
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

                    //PostProcessUtils.SetGlobalShaderSourceSize(cmd, data.sourceTexture);
                    material.SetVector(ShaderConstants._SourceSize, PostProcessUtils.CalcShaderSourceSize(sourceTextureHdl));

                    bool yFlip = isYFlipRequired(destinationTextureHdl, data.cameraData, data.resolveToDebugScreen);
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Vector4 scaleBias = yFlip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);

                    cmd.SetViewport(data.cameraData.pixelRect);
#if ENABLE_VR && ENABLE_XR_MODULE
                    if (data.cameraData.xr.enabled && data.cameraData.xr.hasValidVisibleMesh)
                    {
                        MaterialPropertyBlock xrPropertyBlock = XRSystemUniversal.GetMaterialPropertyBlock();
                        xrPropertyBlock.SetVector(ShaderConstants._BlitScaleBias, scaleBias);
                        xrPropertyBlock.SetTexture(ShaderConstants._BlitTexture, sourceTextureHdl);

                        data.cameraData.xr.RenderVisibleMeshCustomMaterial(cmd, data.cameraData.xr.occlusionMeshScale, material, xrPropertyBlock, 1, !yFlip);
                    }
                    else
#endif
                        Blitter.BlitTexture(cmd, sourceTextureHdl, scaleBias, material, 0);
                });

                return;
            }
        }

        // TODO: Move yFlip logic to higher level (like OnAfterRendering). The destinationTexture is passed in from the OnAfterRendering.
        // TODO: We should know if the target is going to be the same as xr.renderTarget at that level. All the other data is already available there.
        // TODO: The pass/shader only needs to know whether to apply y-flip or not.
        // TODO: Comparing the xr.renderTarget inside the pass creates a non-modular/global dependency on XR target setup code.
        // TODO: Currently we call this inside the execution delegate/lambda because the texture handles are not yet allocated at record-time.
        static bool isYFlipRequired(RTHandle destinationTexture, UniversalCameraData cameraData, bool resolveToDebugScreen)
        {
            bool isRenderToBackBufferTarget = !cameraData.isSceneViewCamera;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
                isRenderToBackBufferTarget = destinationTexture == cameraData.xr.renderTarget;
#endif
            // HDR debug views force-renders to DebugScreenTexture.
            isRenderToBackBufferTarget &= !resolveToDebugScreen;

            // We y-flip if
            // 1) we are blitting from render texture to back buffer(UV starts at bottom) and
            // 2) renderTexture starts UV at top
            return isRenderToBackBufferTarget && cameraData.targetTexture == null && SystemInfo.graphicsUVStartsAtTop;
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
