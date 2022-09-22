using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    // Injects an external alpha texture into the alpha channel. Used for controlling which pixels will be affected by post processing.
    // Use VolumeComponentDeprecated to hide the component from the volume menu (it's for internal compositor use only)
    [Serializable, HideInInspector]
    internal sealed class AlphaInjection : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        internal class ShaderIDs
        {
            public static readonly int k_AlphaTexture = Shader.PropertyToID("_AlphaTexture");
            public static readonly int k_InputTexture = Shader.PropertyToID("_InputTexture");
        }

        Material m_Material;
        CompositionFilter m_CurrentFilter;

        public bool IsActive(HDCamera hdCamera)
        {
            if (m_Material == null)
                return false;

            hdCamera.camera.gameObject.TryGetComponent<AdditionalCompositorData>(out var layerData);
            if (layerData == null || layerData.layerFilters == null)
                return false;

            int index = layerData.layerFilters.FindIndex(x => x.filterType == CompositionFilter.FilterType.ALPHA_MASK);
            if (index < 0)
                return false;

            // Keep the current filter for the rendering avoiding to re-fetch it later on
            m_CurrentFilter = layerData.layerFilters[index];

            return true;
        }

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

        public override void Setup()
        {
            if (!HDRenderPipeline.isReady)
                return;

            m_Material = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.alphaInjectionPS);
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            Debug.Assert(m_Material != null);

            m_Material.SetTexture(ShaderIDs.k_InputTexture, source);
            m_Material.SetTexture(ShaderIDs.k_AlphaTexture, m_CurrentFilter.alphaMask);

            HDUtils.DrawFullScreen(cmd, m_Material, destination);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_Material);
        }
    }
}
