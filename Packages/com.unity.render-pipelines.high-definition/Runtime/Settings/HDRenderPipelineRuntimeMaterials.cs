using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Runtime Materials", Order = 1000), HideInInspector]
    class HDRenderPipelineRuntimeMaterials : IRenderPipelineResources
    {
        public int version => 0;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        #region Sky
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/ShaderGraph/PhysicallyBasedSky.shadergraph")]
        private Material m_PBRSkyMaterial;
        public Material pbrSkyMaterial
        {
            get => m_PBRSkyMaterial;
            set => this.SetValueAndNotify(ref m_PBRSkyMaterial, value);
        }
        #endregion

        #region Area Light
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Material/AreaLightViewer.mat")]
        private Material m_AreaLightMaterial; // never referenced but required by area light mesh renderer, otherwise shader is stripped
        public Material areaLightMaterial
        {
            get => m_AreaLightMaterial;
            set => this.SetValueAndNotify(ref m_AreaLightMaterial, value);
        }
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Material/AreaLightCookieViewer.mat")]
        private Material m_AreaLightCookieMaterial; // We also need one for the cookie because the emissive map is a keyword in our Unlit shader.
        public Material areaLightCookieMaterial
        {
            get => m_AreaLightCookieMaterial;
            set => this.SetValueAndNotify(ref m_AreaLightCookieMaterial, value);
        }
        #endregion


    }
}
