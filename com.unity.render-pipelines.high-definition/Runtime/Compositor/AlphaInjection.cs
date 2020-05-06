using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    [Serializable]
    internal sealed class AlphaInjection : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        internal class ShaderIDs
        {
            public static readonly int k_AlphaTexture = Shader.PropertyToID("_AlphaTexture");
            public static readonly int k_InputTexture = Shader.PropertyToID("_InputTexture");
        }

        Material m_Material;

        public bool IsActive() => m_Material != null;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

        public override void Setup()
        {
            var hdrpAsset = HDRenderPipeline.defaultAsset;
            if (hdrpAsset != null)
                m_Material = CoreUtils.CreateEngineMaterial(hdrpAsset.renderPipelineResources.shaders.alphaInjectionPS);
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            Debug.Assert(m_Material != null);

            //TODO: can we detect this before we get here?
            AdditionalCompositorData layerData = camera.camera.gameObject.GetComponent<AdditionalCompositorData>();
            if (layerData == null || layerData.layerFilters == null)
            {
                HDUtils.BlitCameraTexture(cmd, source, destination);
                return;
            }

            int index = layerData.layerFilters.FindIndex(x => x.filterType == CompositionFilter.FilterType.ALPHA_MASK);
            if (index < 0)
            {
                HDUtils.BlitCameraTexture(cmd, source, destination);
                return;
            }

            var filter = layerData.layerFilters[index];
            m_Material.SetTexture(ShaderIDs.k_InputTexture, source);
            m_Material.SetTexture(ShaderIDs.k_AlphaTexture, filter.alphaMask);

            HDUtils.DrawFullScreen(cmd, m_Material, destination);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_Material);
        }
    }

}
