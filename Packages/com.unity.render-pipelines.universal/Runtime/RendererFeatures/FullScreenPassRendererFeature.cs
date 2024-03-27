using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// FullScreenPass is a renderer feature used to change screen appearance such as post processing effect. This implementation
/// lets it's user create an effect with minimal code involvement.
/// </summary>
[URPHelpURL("renderer-features/renderer-feature-full-screen-pass")]
public class FullScreenPassRendererFeature : ScriptableRendererFeature
{
    /// <summary>
    /// An injection point for the full screen pass. This is similar to RenderPassEvent enum but limits to only supported events.
    /// </summary>
    public enum InjectionPoint
    {
        /// <summary>
        /// Inject a full screen pass before transparents are rendered
        /// </summary>
        BeforeRenderingTransparents = RenderPassEvent.BeforeRenderingTransparents,
        /// <summary>
        /// Inject a full screen pass before post processing is rendered
        /// </summary>
        BeforeRenderingPostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,
        /// <summary>
        /// Inject a full screen pass after post processing is rendered
        /// </summary>
        AfterRenderingPostProcessing = RenderPassEvent.AfterRenderingPostProcessing
    }

    /// <summary>
    /// Material the Renderer Feature uses to render the effect.
    /// </summary>
    public Material passMaterial;
    /// <summary>
    /// Selection for when the effect is rendered.
    /// </summary>
    public InjectionPoint injectionPoint = InjectionPoint.AfterRenderingPostProcessing;
    /// <summary>
    /// One or more requirements for pass. Based on chosen flags certain passes will be added to the pipeline.
    /// </summary>
    public ScriptableRenderPassInput requirements = ScriptableRenderPassInput.Color;
    /// <summary>
    /// An index that tells renderer feature which pass to use if passMaterial contains more than one. Default is 0.
    /// We draw custom pass index entry with the custom dropdown inside FullScreenPassRendererFeatureEditor that sets this value.
    /// Setting it directly will be overridden by the editor class.
    /// </summary>
    [HideInInspector]
    public int passIndex = 0;

    private FullScreenRenderPass fullScreenPass;
    private bool requiresColor;
    private bool injectedBeforeTransparents;

    /// <inheritdoc/>
    public override void Create()
    {
        fullScreenPass = new FullScreenRenderPass();
        fullScreenPass.renderPassEvent = (RenderPassEvent)injectionPoint;

        // This copy of requirements is used as a parameter to configure input in order to avoid copy color pass
        ScriptableRenderPassInput modifiedRequirements = requirements;

        requiresColor = (requirements & ScriptableRenderPassInput.Color) != 0;
        injectedBeforeTransparents = injectionPoint <= InjectionPoint.BeforeRenderingTransparents;

        if (requiresColor && !injectedBeforeTransparents)
        {
            // Removing Color flag in order to avoid unnecessary CopyColor pass
            // Does not apply to before rendering transparents, due to how depth and color are being handled until
            // that injection point.
            modifiedRequirements ^= ScriptableRenderPassInput.Color;
        }
        fullScreenPass.ConfigureInput(modifiedRequirements);
    }

    internal override bool RequireRenderingLayers(bool isDeferred, bool needsGBufferAccurateNormals, out RenderingLayerUtils.Event atEvent, out RenderingLayerUtils.MaskSize maskSize)
    {
        atEvent = RenderingLayerUtils.Event.Opaque;
        maskSize = RenderingLayerUtils.MaskSize.Bits8;
        return false;
    }

    /// <inheritdoc/>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview
            || renderingData.cameraData.cameraType == CameraType.Reflection
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;

        if (passMaterial == null)
        {
            Debug.LogWarningFormat("Missing Post Processing effect Material. {0} Fullscreen pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
            return;
        }
        fullScreenPass.Setup(passMaterial, passIndex, requiresColor, injectedBeforeTransparents, "FullScreenPassRendererFeature", renderingData);

        renderer.EnqueuePass(fullScreenPass);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        fullScreenPass.Dispose();
    }

    internal class FullScreenRenderPass : ScriptableRenderPass
    {
        private static Material s_PassMaterial;
        private int m_PassIndex;
        private bool m_RequiresColor;
        private bool m_IsBeforeTransparents;
        private PassData m_PassData;
        private ProfilingSampler m_ProfilingSampler;
        private RTHandle m_CopiedColor;
        private static readonly int m_BlitTextureShaderID = Shader.PropertyToID("_BlitTexture");

        public void Setup(Material mat, int index, bool requiresColor, bool isBeforeTransparents, string featureName, in RenderingData renderingData)
        {
            s_PassMaterial = mat;
            m_PassIndex = index;
            m_RequiresColor = requiresColor;
            m_IsBeforeTransparents = isBeforeTransparents;
            m_ProfilingSampler ??= new ProfilingSampler(featureName);

            ReAllocate(renderingData.cameraData.cameraTargetDescriptor);

            m_PassData ??= new PassData();
        }

        internal void ReAllocate(RenderTextureDescriptor desc)
        {
            desc.depthBufferBits = (int)DepthBits.None;
            RenderingUtils.ReAllocateIfNeeded(ref m_CopiedColor, desc, name: "_FullscreenPassColorCopy");
        }

        public void Dispose()
        {
            m_CopiedColor?.Release();
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // FullScreenPass manages its own RenderTarget.
            // ResetTarget here so that ScriptableRenderer's active attachement can be invalidated when processing this ScriptableRenderPass.
            ResetTarget();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            var cmd = renderingData.commandBuffer;
            // ExecutePass(m_PassData, renderingData.cameraData, renderingData.commandBuffer);
            if (s_PassMaterial == null)
            {
                // should not happen as we check it in feature
                return;
            }

            if (cameraData.isPreviewCamera)
            {
                return;
            }

            using (new ProfilingScope(cmd, profilingSampler))
            {
                if (m_RequiresColor)
                {
                    // For some reason BlitCameraTexture(cmd, dest, dest) scenario (as with before transparents effects) blitter fails to correctly blit the data
                    // Sometimes it copies only one effect out of two, sometimes second, sometimes data is invalid (as if sampling failed?).
                    // Adding RTHandle in between solves this issue.
                    var source = m_IsBeforeTransparents ? cameraData.renderer.GetCameraColorBackBuffer(cmd) : cameraData.renderer.cameraColorTargetHandle;

                    Blitter.BlitCameraTexture(cmd, source, m_CopiedColor);
                    s_PassMaterial.SetTexture(m_BlitTextureShaderID, m_CopiedColor);
                }

                CoreUtils.SetRenderTarget(cmd, cameraData.renderer.GetCameraColorBackBuffer(cmd));
                CoreUtils.DrawFullScreen(cmd, s_PassMaterial);
            }

        }

        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {

            UniversalRenderer renderer = (UniversalRenderer) renderingData.cameraData.renderer;
            var colorCopyDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            colorCopyDescriptor.depthBufferBits = (int) DepthBits.None;
            TextureHandle copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_FullscreenPassColorCopy", false);

            if (m_RequiresColor)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("CustomPostPro_ColorPass", out var passData, m_ProfilingSampler))
                {
                     passData.source = builder.UseTexture(renderer.activeColorTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                     passData.copiedColor = builder.UseTextureFragment(copiedColor, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                     builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                     {
                            Blitter.BlitTexture(rgContext.cmd, data.source, new Vector4(1, 1, 0, 0), 0.0f, false);
                     });
                }
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("CustomPostPro_FullScreenPass", out var passData, m_ProfilingSampler))
            {
                passData.passIndex = m_PassIndex;

                if (m_RequiresColor)
                    passData.copiedColor = builder.UseTexture(copiedColor, IBaseRenderGraphBuilder.AccessFlags.Read);

                passData.source = builder.UseTextureFragment(renderer.activeColorTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    Blitter.BlitTexture(rgContext.cmd, data.copiedColor, new Vector4(1, 1, 0, 0), s_PassMaterial, data.passIndex);
                });
            }
        }

        private class PassData
        {
            internal Material effectMaterial;
            internal int passIndex;
            internal TextureHandle source;
            public TextureHandle copiedColor;
        }
    }

}
