using System;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    // Custom post-processing pass that performs chroma keying
    // Shader adapted from: https://github.com/keijiro/ProcAmp
    // Use HideInInspector to hide the component from the volume menu (it's for internal use only)
    [Serializable, HideInInspector]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    internal sealed class ChromaKeying : CustomPostProcessVolumeComponent, IPostProcessComponent, ICompositionFilterComponent
    {
        internal class ShaderIDs
        {
            public static readonly int k_KeyColor = Shader.PropertyToID("_KeyColor");
            public static readonly int k_KeyParams = Shader.PropertyToID("_KeyParams");
            public static readonly int k_InputTexture = Shader.PropertyToID("_InputTexture");
        }

        public BoolParameter activate = new BoolParameter(false);
        Material m_Material;
        CompositionFilter m_CurrentFilter;

        #region ICompositionFilterComponent

        CompositionFilter.FilterType ICompositionFilterComponent.compositionFilterType => CompositionFilter.FilterType.CHROMA_KEYING;
        CompositionFilter ICompositionFilterComponent.currentCompositionFilter
        {
            get => m_CurrentFilter;
            set => m_CurrentFilter = value;
        }

        #endregion

        public bool IsActive() => m_Material != null && activate.value;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

        public override void Setup()
        {
            if (!HDRenderPipeline.isReady)
                return;

            var runtimeShaders = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeShaders>();
            m_Material = CoreUtils.CreateEngineMaterial(runtimeShaders.chromaKeyingPS);
        }

        public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
        {
            Debug.Assert(m_Material != null);

            Vector4 keyParams;
            keyParams.x = m_CurrentFilter.keyThreshold;
            keyParams.y = m_CurrentFilter.keyTolerance;
            keyParams.z = m_CurrentFilter.spillRemoval;
            keyParams.w = 1.0f;

            m_Material.SetVector(ShaderIDs.k_KeyColor, m_CurrentFilter.maskColor);
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
