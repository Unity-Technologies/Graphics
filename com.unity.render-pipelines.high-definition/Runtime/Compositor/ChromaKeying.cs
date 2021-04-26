using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    // Custom post-processing pass that performs chroma keying
    // Shader adapted from: https://github.com/keijiro/ProcAmp
    // Use HideInInspector to hide the component from the volume menu (it's for internal use only)
    [Serializable, HideInInspector]
    internal sealed class ChromaKeying : CustomPostProcessVolumeComponent, IPostProcessComponent
    {
        internal class ShaderIDs
        {
            public static readonly int k_KeyColor = Shader.PropertyToID("_KeyColor");
            public static readonly int k_KeyParams = Shader.PropertyToID("_KeyParams");
            public static readonly int k_InputTexture = Shader.PropertyToID("_InputTexture");
        }

        public BoolParameter activate = new BoolParameter(false);
        Material m_Material;

        public bool IsActive() => m_Material != null;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

        public override void Setup()
        {
            if (!HDRenderPipeline.isReady)
                return;

            m_Material = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.chromaKeyingPS);
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            Debug.Assert(m_Material != null);

            AdditionalCompositorData layerData = null;
            camera.camera.gameObject.TryGetComponent<AdditionalCompositorData>(out layerData);

            if (activate.value == false || layerData == null || layerData.layerFilters == null)
            {
                HDUtils.BlitCameraTexture(cmd, source, destination);
                return;
            }

            int index = layerData.layerFilters.FindIndex(x => x.filterType == CompositionFilter.FilterType.CHROMA_KEYING);
            if (index < 0)
            {
                HDUtils.BlitCameraTexture(cmd, source, destination);
                return;
            }

            var filter = layerData.layerFilters[index];
            Vector4 keyParams;
            keyParams.x = filter.keyThreshold;
            keyParams.y = filter.keyTolerance;
            keyParams.z = filter.spillRemoval;
            keyParams.w = 1.0f;

            m_Material.SetVector(ShaderIDs.k_KeyColor, filter.maskColor);
            m_Material.SetVector(ShaderIDs.k_KeyParams, keyParams);
            m_Material.SetTexture(ShaderIDs.k_InputTexture, source);
            HDUtils.DrawFullScreen(cmd, m_Material, destination);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_Material);
        }
    }
}
