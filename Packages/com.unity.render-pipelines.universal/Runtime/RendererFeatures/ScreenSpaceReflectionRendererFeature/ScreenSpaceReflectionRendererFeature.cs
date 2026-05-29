#if URP_SCREEN_SPACE_REFLECTION
namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// The class for the SSR renderer feature.
    /// </summary>
    [SupportedOnRenderer(typeof(UniversalRendererData))]
    [DisallowMultipleRendererFeature("Screen Space Reflection")]
    [Tooltip("The Screen Space Reflection produces realtime reflections without the need for reflection probes.")]
    public class ScreenSpaceReflectionRendererFeature : ScriptableRendererFeature
    {
        /// <summary>Whether to apply screen space reflections after the opaque pass or before the opaque pass.</summary>
        [Tooltip("Whether to apply screen space reflections after the opaque pass or before the opaque pass. Enabling this feature may improve performance on low-end platforms, but will result in less physically correct reflections.")]
        public bool afterOpaque;

        ScreenSpaceReflectionDepthNormalOnlyTransparentPass m_TransparentDepthNormalPass = null;
        ScreenSpaceReflectionPass m_SSRPass = null;

        Shader m_Shader = null;
        Material m_Material = null;

        Shader m_BlitShader = null;
        Material m_BlitMaterial = null;

        /// <inheritdoc/>
        public override void Create()
        {
            if (m_SSRPass == null)
                m_SSRPass = new ScreenSpaceReflectionPass();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            m_SSRPass?.Dispose();
            m_TransparentDepthNormalPass = null;
            m_SSRPass = null;
            CoreUtils.Destroy(m_Material);
            CoreUtils.Destroy(m_BlitMaterial);
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var universalRenderingData = renderingData.universalRenderingData;
            var universalCameraData = renderingData.frameData.Get<UniversalCameraData>();

            if (UniversalRenderer.IsOffscreenDepthTexture(universalCameraData))
                return;

            // Currently no orthographic support, so ignore these cameras.
            if (universalCameraData.camera.orthographic)
                return;

            var settings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflectionVolumeSettings>();
            if (settings.mode == ScreenSpaceReflectionVolumeSettings.ReflectionMode.Disabled)
                return;

            if (!TryPrepareResources(universalRenderingData.transparentLayerMask, settings))
                return;

            if (settings.marchingMethod.value == ScreenSpaceReflectionVolumeSettings.MarchingMethod.Hierarchical && !SystemInfo.supportsComputeShaders)
                Debug.LogWarning("Screen Space Reflection settings are incompatible with the current platform. Linear Marching Method must be used on platforms without computer shader support. Falling back to Linear marching.");

            bool shouldAdd = m_SSRPass.Setup(renderer, m_Material, m_BlitMaterial, afterOpaque, universalRenderingData, universalCameraData.cameraType);
            if (shouldAdd)
            {
                if (settings.ShouldRenderTransparents())
                {
                    var renderPassEvent = afterOpaque ? RenderPassEvent.AfterRenderingTransparents : RenderPassEvent.AfterRenderingPrePasses;
                    m_TransparentDepthNormalPass.UpdateRenderPassEvent(renderPassEvent);
                    renderer.EnqueuePass(m_TransparentDepthNormalPass);
                }

                renderer.EnqueuePass(m_SSRPass);
            }
        }

        bool TryPrepareResources(LayerMask transparentLayerMask, ScreenSpaceReflectionVolumeSettings settings)
        {
            if (settings.ShouldRenderTransparents())
            {
                if (m_TransparentDepthNormalPass == null)
                    m_TransparentDepthNormalPass = new(RenderPassEvent.AfterRenderingPrePasses, RenderQueueRange.transparent, transparentLayerMask);
            }

            if (m_Shader == null || m_BlitShader == null)
            {
                if (!GraphicsSettings.TryGetRenderPipelineSettings<ScreenSpaceReflectionPersistentResources>(out var ssrPersistentResources))
                {
                    Debug.LogErrorFormat(
                        $"Couldn't find the required resources for the {nameof(ScreenSpaceReflectionRendererFeature)}. If this exception appears in the Player, make sure at least one {nameof(ScreenSpaceReflectionRendererFeature)} is enabled or adjust your stripping settings.");
                    return false;
                }

                m_Shader = ssrPersistentResources.Shader;
                m_BlitShader = ssrPersistentResources.BlitShader;
            }

            if (m_Material == null && m_Shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(m_Shader);

            if (m_BlitMaterial == null && m_BlitShader != null)
                m_BlitMaterial = CoreUtils.CreateEngineMaterial(m_BlitShader);

            if (m_Material == null || m_BlitMaterial == null)
            {
                Debug.LogError($"{GetType().Name}.AddRenderPasses(): Missing material. {name} render pass will not be added.");
                return false;
            }

            return true;

        }
    }
}
#endif
