using System;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    // Injects an external alpha texture into the alpha channel. Used for controlling which pixels will be affected by post processing.
    // Use HideInInspector to hide the component from the volume menu (it's for internal use only)
    [Serializable, HideInInspector]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    internal sealed class AlphaInjection : CustomPostProcessVolumeComponent, IPostProcessComponent, ICompositionFilterComponent
    {
        internal class ShaderIDs
        {
            public static readonly int k_AlphaTexture = Shader.PropertyToID("_AlphaTexture");
            public static readonly int k_InputTexture = Shader.PropertyToID("_InputTexture");
        }

        Material m_Material;
        CompositionFilter m_CurrentFilter;

        #region ICompositionFilterComponent

        CompositionFilter.FilterType ICompositionFilterComponent.compositionFilterType => CompositionFilter.FilterType.ALPHA_MASK;
        CompositionFilter ICompositionFilterComponent.currentCompositionFilter
        {
            get => m_CurrentFilter;
            set => m_CurrentFilter = value;
        }

        #endregion

        public bool IsActive() => m_Material != null;

        public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

        public override void Setup()
        {
            if (!HDRenderPipeline.isReady)
                return;

            var runtimeShaders = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeShaders>();
            m_Material = CoreUtils.CreateEngineMaterial(runtimeShaders.alphaInjectionPS);
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
