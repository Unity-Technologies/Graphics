using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ForwardPassesFeature : ScriptableRendererFeature
{
    const int k_DepthStencilBufferBits = 32;

    private static class Profiling
    {
        private const string k_Name = nameof(ForwardRenderer);
        public static readonly ProfilingSampler createCameraRenderTarget = new ProfilingSampler($"{k_Name}.{nameof(CreateCameraRenderTarget)}");
    }

    // Rendering mode setup from UI.
    internal RenderingMode renderingMode { get { return m_RenderingMode; } }
    // Actual rendering mode, which may be different (ex: wireframe rendering, harware not capable of deferred rendering).
    internal RenderingMode actualRenderingMode { get { return GL.wireframe || m_DeferredLights == null || !m_DeferredLights.IsRuntimeSupportedThisFrame() ? RenderingMode.Forward : this.renderingMode; } }
    internal bool accurateGbufferNormals { get { return m_DeferredLights != null ? m_DeferredLights.AccurateGbufferNormals : false; } }
    ColorGradingLutPass m_ColorGradingLutPass;
    DepthOnlyPass m_DepthPrepass;
    DepthNormalOnlyPass m_DepthNormalPrepass;
    MainLightShadowCasterPass m_MainLightShadowCasterPass;
    AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
    GBufferPass m_GBufferPass;
    CopyDepthPass m_GBufferCopyDepthPass;
    TileDepthRangePass m_TileDepthRangePass;
    TileDepthRangePass m_TileDepthRangeExtraPass; // TODO use subpass API to hide this pass
    DeferredPass m_DeferredPass;
    DrawObjectsPass m_RenderOpaqueForwardOnlyPass;
    DrawObjectsPass m_RenderOpaqueForwardPass;
    DrawSkyboxPass m_DrawSkyboxPass;
    CopyDepthPass m_CopyDepthPass;
    CopyColorPass m_CopyColorPass;
    TransparentSettingsPass m_TransparentSettingsPass;
    DrawObjectsPass m_RenderTransparentForwardPass;
    InvokeOnRenderObjectCallbackPass m_OnRenderObjectCallbackPass;
    PostProcessPass m_PostProcessPass;
    PostProcessPass m_FinalPostProcessPass;
    FinalBlitPass m_FinalBlitPass;
    CapturePass m_CapturePass;
#if ENABLE_VR && ENABLE_XR_MODULE
        XROcclusionMeshPass m_XROcclusionMeshPass;
        CopyDepthPass m_XRCopyDepthPass;
#endif
#if UNITY_EDITOR
        SceneViewDepthCopyPass m_SceneViewDepthCopyPass;
#endif

    RenderTargetHandle m_ActiveCameraColorAttachment;
    RenderTargetHandle m_ActiveCameraDepthAttachment;
    RenderTargetHandle m_CameraColorAttachment;
    RenderTargetHandle m_CameraDepthAttachment;
    RenderTargetHandle m_DepthTexture;
    RenderTargetHandle m_NormalsTexture;
    RenderTargetHandle[] m_GBufferHandles;
    RenderTargetHandle m_OpaqueColor;
    RenderTargetHandle m_AfterPostProcessColor;
    RenderTargetHandle m_ColorGradingLut;
    // For tiled-deferred shading.
    RenderTargetHandle m_DepthInfoTexture;
    RenderTargetHandle m_TileDepthInfoTexture;

    ForwardLights m_ForwardLights;
    DeferredLights m_DeferredLights;
    RenderingMode m_RenderingMode;
    StencilState m_DefaultStencilState;

    // Materials used in URP Scriptable Render Passes
    Material m_BlitMaterial = null;
    Material m_CopyDepthMaterial = null;
    Material m_SamplingMaterial = null;
    Material m_TileDepthInfoMaterial = null;
    Material m_TileDeferredMaterial = null;
    Material m_StencilDeferredMaterial = null;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


