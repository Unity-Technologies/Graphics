using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    [Serializable, VolumeComponentMenu("Post-processing/Custom/AlphaIjection")]
    internal sealed class AlphaInjection : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        internal class ShaderIDs
        {
            public static readonly int _AlphaTexture = Shader.PropertyToID("_AlphaTexture");
            public static readonly int _InputTexture = Shader.PropertyToID("_InputTexture");
        }

        Material m_Material;

        public bool IsActive() => m_Material != null;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

        public override void Setup()
        {
            if (Shader.Find("Hidden/Shader/AlphaInjection") != null)
                m_Material = new Material(Shader.Find("Hidden/Shader/AlphaInjection"));
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            Debug.Assert(m_Material != null);

            //TODO: can we detect this before we get here?
            AdditionalCompositorData layerData = camera.camera.gameObject.GetComponent<AdditionalCompositorData>();
            if (layerData == null || layerData.m_layerFilters == null)
            {
                HDUtils.BlitCameraTexture(cmd, source, destination);
                return;
            }

            int indx = layerData.m_layerFilters.FindIndex(x => x.m_Type == (int)CompositionFilter.FilterType.ALPHA_MASK);
            if (indx < 0)
            {
                HDUtils.BlitCameraTexture(cmd, source, destination);
                return;
            }

            var filter = layerData.m_layerFilters[indx];
            m_Material.SetTexture(ShaderIDs._InputTexture, source);
            m_Material.SetTexture(ShaderIDs._AlphaTexture, filter.m_AlphaMask);

            HDUtils.DrawFullScreen(cmd, m_Material, destination);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_Material);
        }
    }

}
