#if VOLUMETRIC_FOG

using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Volumetric Fog Resources", Order = 1000), HideInInspector]
    internal sealed class VolumetricFogResources : IRenderPipelineResources
    {
        public int version => 0;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField, ResourcePath("Runtime/RendererFeatures/Fog/Shaders/GenerateMaxZ.compute")]
        private ComputeShader m_GenerateMaxZCS;

        public ComputeShader generateMaxZCS
        {
            get => m_GenerateMaxZCS;
            set => this.SetValueAndNotify(ref m_GenerateMaxZCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RendererFeatures/Fog/Shaders/VolumetricLighting.compute")]
        private ComputeShader m_VolumetricLightingCS;

        public ComputeShader volumetricLightingCS
        {
            get => m_VolumetricLightingCS;
            set => this.SetValueAndNotify(ref m_VolumetricLightingCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RendererFeatures/Fog/Shaders/VolumetricLightingFiltering.compute")]
        private ComputeShader m_VolumetricLightingFilteringCS;

        public ComputeShader volumetricLightingFilteringCS
        {
            get => m_VolumetricLightingFilteringCS;
            set => this.SetValueAndNotify(ref m_VolumetricLightingFilteringCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RendererFeatures/Fog/Shaders/VolumeVoxelization.compute")]
        private ComputeShader m_VolumeVoxelizationCS;

        public ComputeShader volumeVoxelizationCS
        {
            get => m_VolumeVoxelizationCS;
            set => this.SetValueAndNotify(ref m_VolumeVoxelizationCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RendererFeatures/Fog/Shaders/ApplyVolumetricFog.shader")]
        private Shader m_ApplyVolumetricFogPS;

        public Shader applyVolumetricFogPS
        {
            get => m_ApplyVolumetricFogPS;
            set => this.SetValueAndNotify(ref m_ApplyVolumetricFogPS, value);
        }

        [SerializeField, ResourcePath("Runtime/RendererFeatures/Fog/Shaders/ScreenSpaceMultipleScattering.shader")]
        private Shader m_ScreenSpaceMultipleScatteringPS;

        public Shader screenSpaceMultipleScatteringPS
        {
            get => m_ScreenSpaceMultipleScatteringPS;
            set => this.SetValueAndNotify(ref m_ScreenSpaceMultipleScatteringPS, value);
        }
    }
}

#endif
