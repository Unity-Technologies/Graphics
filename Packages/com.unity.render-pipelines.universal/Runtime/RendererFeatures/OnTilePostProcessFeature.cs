using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

/// <summary>
/// The class for the On-Tile Post Processing renderer feature. This renderer feature provides a reduced scope alternative to the built-in URP post-processing features but that can run more optimally on tile-based graphics hardware (most untethered-XR devices)
/// The renderer feature could only be added once. Adding multiple post processing passes is currently not supported.
/// </summary>
[DisallowMultipleRendererFeature("On Tile Post Processing")]
public partial class OnTilePostProcessFeature : ScriptableRendererFeature
{
    [SerializeField, HideInInspector]
    PostProcessData m_PostProcessData;

    /// <summary>
    /// Specifies at which injection point the pass will be rendered.
    /// </summary>
    RenderPassEvent postProcessingEvent = RenderPassEvent.AfterRenderingPostProcessing-1;

    Material m_OnTilePostProcessMaterial;
    ColorGradingLutPass m_ColorGradingLutPass;
    OnTilePostProcessPass m_OnTilePostProcessPass;

    bool TryLoadResources()
    {
        if (m_OnTilePostProcessMaterial == null)
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<OnTilePostProcessResource>(out var resources))
            {
                Debug.LogErrorFormat(
                    $"Couldn't find the required resources for the {nameof(OnTilePostProcessFeature)} render feature.");
                return false;
            }

            var uberPostShader = resources.uberPostShader;

            if (uberPostShader == null || !uberPostShader.isSupported)
            {
                Debug.LogErrorFormat(
                    $"Couldn't not create a supported shader for {nameof(OnTilePostProcessFeature)} render feature.");
                return false;
            }

            m_OnTilePostProcessMaterial = new Material(uberPostShader);
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

        if (m_PostProcessData == null)        
        {            
            Debug.LogError($"{nameof(OnTilePostProcessFeature)} does not have a valid postProcessData instance.");
            return;
        }        
        
        m_ColorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingPrePasses, m_PostProcessData);
        m_OnTilePostProcessPass = new OnTilePostProcessPass(m_PostProcessData);
        // On-tile PP requires memoryless intermediate texture to work. In case intermediate texture is not memoryless, on-tile PP will falls back to off-tile rendering.
        m_OnTilePostProcessPass.requiresIntermediateTexture = true;       

        supportedRenderingFeatures.supportsHDR = true;
        supportedRenderingFeatures.postProcessing = true;
    }

    /// <inheritdoc/>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Post processing needs to be enabled on the camera.
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        // NOTE: Ideally, we check here if the Post Processing is enabled on the UniversalRenderer asset through a public API. In that case, the built in post processing will be enabled.
        // We currently do not have a public API for that, so we use internal API for now.
        var universalRenderer = renderer as UniversalRenderer;
        if (universalRenderer.postProcessEnabled)
        {
            Debug.LogError("URP renderer(Universal Renderer Data) has post processing enabled, which conflicts with the On-Tile post processing feature. Only one of the post processing should be enabled. On-Tile post processing feature will not be added.");
            return;
        }

        if (m_ColorGradingLutPass == null || m_OnTilePostProcessPass == null)
            return;

        if (!TryLoadResources())
            return;

        m_ColorGradingLutPass.renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;

        m_OnTilePostProcessPass.Setup(ref m_OnTilePostProcessMaterial);
        m_OnTilePostProcessPass.renderPassEvent = postProcessingEvent;
        m_OnTilePostProcessPass.m_UseTextureReadFallback = !universalRenderer.useTileOnlyMode;

        renderer.EnqueuePass(m_ColorGradingLutPass);
        renderer.EnqueuePass(m_OnTilePostProcessPass);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        // Always dispose unmanaged resources.
        m_ColorGradingLutPass?.Cleanup();
    }
}
