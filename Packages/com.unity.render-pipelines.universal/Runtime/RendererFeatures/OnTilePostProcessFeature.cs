using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

/// <summary>
/// The class for the On-Tile Post Processing renderer feature. This renderer feature provides a reduced scope alternative to the built-in URP post-processing features but that can run more optimally on tile-based graphics hardware (most untethered-XR devices)
/// The renderer feature could only be added once. Adding multiple post processing passes is currently not supported.
/// </summary>
[DisallowMultipleRendererFeature("On Tile Post Processing (Untethered XR)")]
public partial class OnTilePostProcessFeature : ScriptableRendererFeature
{
    [SerializeField, HideInInspector]
    PostProcessData m_PostProcessData;

    Shader m_UberPostShader;

    /// <summary>
    /// Specifies at which injection point the pass will be rendered.
    /// </summary>
    RenderPassEvent postProcessingEvent = RenderPassEvent.AfterRenderingPostProcessing-1;

    Material m_OnTilePostProcessMaterial;
    ColorGradingLutPass m_ColorGradingLutPass;
    OnTilePostProcessPass m_OnTilePostProcessPass;

    bool TryLoadResources()
    {
        if (m_UberPostShader == null || m_OnTilePostProcessMaterial == null)
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<OnTilePostProcessResource>(out var resources))
            {
                Debug.LogErrorFormat(
                    $"Couldn't find the required resources for the {nameof(OnTilePostProcessFeature)} render feature.");
                return false;
            }

            m_UberPostShader = resources.uberPostShader;
            m_OnTilePostProcessMaterial = new Material(m_UberPostShader);
        }

        return true;
    }

    /// <inheritdoc/>
    public override void Create()
    {
        if (m_PostProcessData == null)
        {
#if UNITY_EDITOR
            m_PostProcessData = PostProcessData.GetDefaultPostProcessData();
#endif
        }

        if (m_PostProcessData != null)
        {
            m_ColorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingPrePasses, m_PostProcessData);
            m_OnTilePostProcessPass = new OnTilePostProcessPass(m_PostProcessData);
            // On-tile PP requries memoryless intermediate texture to work. In case intermediate texture is not memoryless, on-tile PP will falls back to offtile rendering.
            m_OnTilePostProcessPass.requiresIntermediateTexture = true;
        }
    }

#if ENABLE_VR && ENABLE_XR_MODULE
    bool IsRuntimePlatformUntetheredXR()
    {
        // Return true if the current runtime platform is Android(untethered XR platform)
        return Application.platform == RuntimePlatform.Android;
    }

#if UNITY_EDITOR
    bool IsBuildTargetUntetheredXR()
    {
        // Return true if the current build target is Android(untethered XR platform).
        return EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
    }
#endif
#endif

    /// <inheritdoc/>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        bool useFallback = true;

#if ENABLE_VR && ENABLE_XR_MODULE
        // Enable on-tile post processing when running untethered XR
        if (renderingData.cameraData.xr.enabled && IsRuntimePlatformUntetheredXR())
        {
            useFallback = false;
        }

#if UNITY_EDITOR
        // Enable on-tile post processing when running XR Editor Playmode (build target needs to be Android)
        if (renderingData.cameraData.xr.enabled && IsBuildTargetUntetheredXR())
        {
            useFallback = false;
        }
#endif
#endif

        // Post processing needs to be enabled on the camera
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        // NOTE: Ideally, we check here if the Post Processing is enabled on the UniversalRenderer asset through a public API. In that case, the built in post processing will be enabled.
        // We currently do not have a public API for that, so we use internal API for now
        var universalRenderer = renderer as UniversalRenderer;
        if (universalRenderer.isPostProcessPassRenderGraphActive)
        {
            Debug.LogError("URP renderer(Universal Renderer Data) has post processing enabled, which conflicts with the On-Tile post processing feature. Only one of the post processing should be enabled. On-Tile post processing feature will not be added.");
            return;
        }

        if (m_ColorGradingLutPass == null || m_OnTilePostProcessPass == null)
            return;

        if (!TryLoadResources())
            return;

        var graphicsDeviceType = SystemInfo.graphicsDeviceType;
        var deviceSupportsFbf = graphicsDeviceType == GraphicsDeviceType.Vulkan || graphicsDeviceType == GraphicsDeviceType.Metal || graphicsDeviceType == GraphicsDeviceType.Direct3D12;
        if (!deviceSupportsFbf)
        {
            Debug.LogError("The On-Tile post processing feature is not supported on the graphics devices that don't support frame buffer fetch.");
            return;
        }

        // Internally force the correct mode we require while this is not a public setting.
        UniversalRenderPipeline.renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.PropagateAttachmentOrientation;

        m_ColorGradingLutPass.renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;

        m_OnTilePostProcessPass.Setup(ref m_OnTilePostProcessMaterial);
        m_OnTilePostProcessPass.renderPassEvent = postProcessingEvent;

        if (useFallback)
        {
            // Perform fallback logic to 1. use texture read(off-tile rendering) and 2. disable the UV origin propagation mode.
            m_OnTilePostProcessPass.m_UseTextureReadFallback = true;
            UniversalRenderPipeline.renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.BottomLeft;
        }
        else
        {
            m_OnTilePostProcessPass.m_UseTextureReadFallback = false;
        }

        renderer.EnqueuePass(m_ColorGradingLutPass);
        renderer.EnqueuePass(m_OnTilePostProcessPass);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        // always dispose unmanaged resources
        m_ColorGradingLutPass?.Cleanup();
    }
}
