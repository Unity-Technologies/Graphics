#if UNITY_EDITOR
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Editor Materials", Order = 1000), HideInInspector]
    class HDRenderPipelineEditorMaterials : IRenderPipelineResources
    {
        public int version => 0;

        [SerializeField]
        [ResourcePath("Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat")]
        private Material m_DefaultMaterial = null;

        public virtual Material defaultMaterial
        {
            get => m_DefaultMaterial;
            set => this.SetValueAndNotify(ref m_DefaultMaterial, value, nameof(m_DefaultMaterial));
        }

        [SerializeField]
        [ResourcePath("Runtime/RenderPipelineResources/Material/DefaultHDParticleMaterial.mat")]
        private Material m_DefaultParticleMaterial = null;

        public virtual Material defaultParticleMaterial
        {
            get => m_DefaultParticleMaterial;
            set => this.SetValueAndNotify(ref m_DefaultParticleMaterial, value, nameof(m_DefaultParticleMaterial));
        }

        [SerializeField]
        [ResourcePath("Runtime/RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat")]
        private Material m_DefaultTerrainMaterial = null;

        public virtual Material defaultTerrainMaterial
        {
            get => m_DefaultTerrainMaterial;
            set => this.SetValueAndNotify(ref m_DefaultTerrainMaterial, value, nameof(m_DefaultTerrainMaterial));
        }

        [SerializeField]
        [ResourcePath("Runtime/RenderPipelineResources/Material/DefaultHDMirrorMaterial.mat")]
        private Material m_DefaultMirrorMat = null;

        public virtual Material defaultMirrorMaterial
        {
            get => m_DefaultMirrorMat;
            set => this.SetValueAndNotify(ref m_DefaultMirrorMat, value, nameof(m_DefaultMirrorMat));
        }

        [SerializeField]
        [ResourcePath("Runtime/RenderPipelineResources/Material/DefaultHDDecalMaterial.mat")]
        private Material m_DefaultDecalMat = null;

        public virtual Material defaultDecalMaterial
        {
            get => m_DefaultDecalMat;
            set => this.SetValueAndNotify(ref m_DefaultDecalMat, value, nameof(m_DefaultDecalMat));
        }

        [SerializeField]
        [ResourcePath("Editor/RenderPipelineResources/Material/GUITextureBlit2SRGB.mat")]
        private Material m_GUITextureBlit2SRGB = null;

        public virtual Material GUITextureBlit2SRGB
        {
            get => m_GUITextureBlit2SRGB;
            set => this.SetValueAndNotify(ref m_GUITextureBlit2SRGB, value, nameof(m_GUITextureBlit2SRGB));
        }
    }
}
#endif
